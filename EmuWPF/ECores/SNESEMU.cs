using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;

namespace EmuWPF.ECores
{
    class SNESEMU
    {
        [DllImport("user32.dll")] static extern IntPtr SetParent(IntPtr child, IntPtr parent);
        [DllImport("user32.dll")] static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")] static extern bool MoveWindow(IntPtr hWnd, int x, int y, int w, int h, bool repaint);
        [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int cmd);
        [DllImport("user32.dll")] static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);
        [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] static extern IntPtr SetFocus(IntPtr hWnd);
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);
        [DllImport("user32.dll")] static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;
        const int GWL_STYLE = -16;
        const int WS_CHILD = 0x40000000;

        private Process? _process;
        private IntPtr _gameHwnd = IntPtr.Zero;

        public string Name => "SNES";
        public IntPtr GameWindowHandle => _gameHwnd;

        public void Launch(string gamePath, string extraArgs = "")
        {
            TryTerminateOtherEmulators();

            var exePath = FindEmulatorExecutable();
            if (exePath == null)
            {
                MessageBox.Show("Snes9x not found.\n\nPlace snes9x.exe in:\nECores\\SNES\\Snes9x\\",
                    "Missing Emulator");
                return;
            }

            _process = Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = $"\"{gamePath}\"",
                WorkingDirectory = Path.GetDirectoryName(exePath)!,
                UseShellExecute = true
            });

            if (_process == null) return;

            int pid = _process.Id;

            System.Threading.Tasks.Task.Run(() =>
            {
                for (int i = 0; i < 8; i++)
                {
                    Thread.Sleep(50);
                    HideConsoleWindowsForPid(pid);
                }
            });

            IntPtr hwnd = IntPtr.Zero;
            for (int i = 0; i < 60; i++)
            {
                Thread.Sleep(100);
                try { _process.Refresh(); } catch { break; }
                hwnd = _process.MainWindowHandle;
                if (hwnd != IntPtr.Zero) break;
            }

            if (hwnd == IntPtr.Zero) hwnd = FindWindow(null, "Snes9x");

            if (hwnd == IntPtr.Zero) return;

            _gameHwnd = hwnd;
            Thread.Sleep(500);
        }

        public void EmbedAt(IntPtr parentHwnd, int x, int y, int width, int height)
        {
            if (_gameHwnd == IntPtr.Zero) return;
            SetWindowLong(_gameHwnd, GWL_STYLE, WS_CHILD);
            SetParent(_gameHwnd, parentHwnd);
            MoveWindow(_gameHwnd, x, y, width, height, true);
            ShowWindow(_gameHwnd, SW_SHOW);
            SetForegroundWindow(_gameHwnd);
            SetFocus(_gameHwnd);
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

                if (_process != null && !_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                    _process.WaitForExit(3000);
                    _process = null;
                }

                foreach (var proc in Process.GetProcessesByName("snes9x"))
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
            var known = new[] { "fceux", "nestopia", "snes9x",
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
                var snes9x = Path.Combine(dir.FullName, "ECores", "SNES", "Snes9x", "snes9x.exe");
                if (File.Exists(snes9x)) return snes9x;
                dir = dir.Parent;
            }
            return null;
        }
    }
}