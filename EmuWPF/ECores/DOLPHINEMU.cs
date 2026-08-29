using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;

namespace EmuWPF.ECores
{
    class DolphinEMU
    {
        [DllImport("user32.dll")] static extern IntPtr SetParent(IntPtr child, IntPtr parent);
        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")] static extern bool MoveWindow(IntPtr hWnd, int x, int y, int w, int h, bool repaint);
        [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int cmd);
        [DllImport("user32.dll")] static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);
        [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] static extern IntPtr SetFocus(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern IntPtr SetActiveWindow(IntPtr hWnd);
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
        [DllImport("user32.dll")] static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
        [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hWnd);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;
        const int GWL_STYLE = -16;
        const int WS_CHILD = 0x40000000;

        private Process? _process;
        private IntPtr _gameHwnd = IntPtr.Zero;
        private IntPtr _mainHwnd = IntPtr.Zero;

        public string Name => "Dolphin";
        public IntPtr GameWindowHandle => _gameHwnd;
        public int ProcessId => _process?.Id ?? 0;

        public void Launch(string gamePath, string extraArgs = "")
        {
            TryTerminateOtherEmulators();

            var exePath = FindEmulatorExecutable();
            if (exePath == null)
            {
                MessageBox.Show("Dolphin not found.", "Missing Emulator");
                return;
            }

            _process = Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = $"--exec=\"{gamePath}\" {extraArgs}",
                WorkingDirectory = Path.GetDirectoryName(exePath)!,
                UseShellExecute = true
            });

            if (_process == null) return;

            // Hide console
            System.Threading.Tasks.Task.Run(() =>
            {
                int pid = _process.Id;
                for (int attempt = 0; attempt < 40; attempt++)
                {
                    Thread.Sleep(150);
                    bool found = false;
                    EnumWindows((hWnd, _) =>
                    {
                        GetWindowThreadProcessId(hWnd, out uint wPid);
                        if (wPid != (uint)pid) return true;
                        var sb = new StringBuilder(256);
                        GetClassName(hWnd, sb, 256);
                        if (sb.ToString() == "ConsoleWindowClass")
                        {
                            ShowWindow(hWnd, SW_HIDE);
                            found = true;
                            return false;
                        }
                        return true;
                    }, IntPtr.Zero);
                    if (found) break;
                }
            });

            // Wait for main window
            IntPtr hwnd = IntPtr.Zero;
            for (int i = 0; i < 100; i++)
            {
                Thread.Sleep(100);
                try { _process.Refresh(); } catch { break; }
                hwnd = _process.MainWindowHandle;
                if (hwnd != IntPtr.Zero) break;
            }

            if (hwnd == IntPtr.Zero)
                hwnd = FindWindow(null, "Dolphin");

            if (hwnd == IntPtr.Zero) return;

            _mainHwnd = hwnd;

            // Wait for game to start rendering
            Thread.Sleep(3000);

            // Find render window — try by class first
            IntPtr renderHwnd = IntPtr.Zero;
            int pid2 = _process.Id;

            EnumWindows((hWnd, _) =>
            {
                GetWindowThreadProcessId(hWnd, out uint wPid);
                if (wPid != (uint)pid2) return true;
                if (!IsWindowVisible(hWnd)) return true;
                if (hWnd == _mainHwnd) return true;

                var sb = new StringBuilder(256);
                GetClassName(hWnd, sb, 256);
                var cls = sb.ToString();

                if (cls.Contains("Qt") || cls.Contains("Render") || cls.Contains("GLFW"))
                {
                    renderHwnd = hWnd;
                    return false;
                }
                return true;
            }, IntPtr.Zero);

            // Use render window if found, otherwise use main window
            _gameHwnd = renderHwnd != IntPtr.Zero ? renderHwnd : hwnd;
        }

        public void EmbedAt(IntPtr parentHwnd, int x, int y, int width, int height)
        {
            if (_gameHwnd == IntPtr.Zero) return;

            // Hide main UI if different from game window
            if (_mainHwnd != IntPtr.Zero && _mainHwnd != _gameHwnd)
                ShowWindow(_mainHwnd, SW_HIDE);

            SetWindowLong(_gameHwnd, GWL_STYLE, WS_CHILD);
            SetParent(_gameHwnd, parentHwnd);
            MoveWindow(_gameHwnd, x, y, width, height, true);
            ShowWindow(_gameHwnd, SW_SHOW);
            SetForegroundWindow(_gameHwnd);
            SetFocus(_gameHwnd);

        }

        public void RefreshGameHandle(int pid)
        {
            if (pid == 0) return;
            EnumWindows((hWnd, _) =>
            {
                GetWindowThreadProcessId(hWnd, out uint wPid);
                if (wPid != (uint)pid) return true;
                if (!IsWindowVisible(hWnd)) return true;
                if (hWnd == _mainHwnd) return true;

                var sb = new StringBuilder(256);
                GetClassName(hWnd, sb, 256);
                var cls = sb.ToString();

                if (cls.Contains("Qt") || cls.Contains("Render"))
                {
                    _gameHwnd = hWnd;
                    return false;
                }
                return true;
            }, IntPtr.Zero);
        }

        public void ResizeTo(int x, int y, int width, int height)
        {
            if (_gameHwnd == IntPtr.Zero) return;
            MoveWindow(_gameHwnd, x, y, width, height, true);
        }

        public void SetEmbeddedVisibility(bool visible)
        {
            if (_gameHwnd == IntPtr.Zero) return;
            ShowWindow(_gameHwnd, visible ? SW_SHOW : SW_HIDE);
        }

        public void Stop()
        {
            try
            {
                _gameHwnd = IntPtr.Zero;
                _mainHwnd = IntPtr.Zero;

                if (_process != null && !_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                    _process.WaitForExit(3000);
                    _process = null;
                }

                foreach (var proc in Process.GetProcessesByName("Dolphin"))
                {
                    try
                    {
                        proc.Kill(entireProcessTree: true);
                        proc.WaitForExit(2000);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void HideConsoleWindowsForPid(int pid)
        {
            EnumWindows((hWnd, _) =>
            {
                try
                {
                    GetWindowThreadProcessId(hWnd, out uint wPid);
                    if (wPid != (uint)pid) return true;
                    var sb = new StringBuilder(256);
                    GetClassName(hWnd, sb, 256);
                    if (sb.ToString() == "ConsoleWindowClass")
                        ShowWindow(hWnd, SW_HIDE);
                }
                catch { }
                return true;
            }, IntPtr.Zero);
        }

        private void TryTerminateOtherEmulators()
        {
            var known = new[] { "fceux", "fceux64", "nestopia", "snes9x",
                                "bsnes", "mupen64plus-ui-console",
                                "mupen64plus", "dolphin" };
            foreach (var proc in Process.GetProcesses())
            {
                try
                {
                    if (known.Contains(proc.ProcessName.ToLower()))
                    {
                        proc.Kill(entireProcessTree: true);
                        proc.WaitForExit(2000);
                    }
                }
                catch { }
            }
        }

        private static string? FindEmulatorExecutable()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var gcwii = Path.Combine(dir.FullName, "ECores", "GC-WII", "Dolphin-x64", "Dolphin.exe");
                if (File.Exists(gcwii)) return gcwii;

                var gcn = Path.Combine(dir.FullName, "ECores", "GC-WII", "Dolphin-x64", "Dolphin.exe");
                if (File.Exists(gcn)) return gcn;

                var wii = Path.Combine(dir.FullName, "ECores", "GC-WII", "Dolphin-x64", "Dolphin.exe");
                if (File.Exists(wii)) return wii;

                dir = dir.Parent;
            }
            return null;
        }
    }
}