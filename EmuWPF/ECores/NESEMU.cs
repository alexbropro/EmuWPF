using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;

namespace EmuWPF.ECores
{
    class NESEMU
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

        public string Name => "NES";
        public IntPtr GameWindowHandle => _gameHwnd;

        public void Launch(string gamePath, string extraArgs = "")
        {
            TryTerminateOtherEmulators();
            EnsureFCEUXConfig();

            var exePath = FindEmulatorExecutable();
            if (exePath == null)
            {
                MessageBox.Show("FCEUX not found.\n\nPlace fceux64.exe in:\nECores\\NES\\FCEUX\\",
                    "Missing Emulator");
                return;
            }

            _process = Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = $"\"{gamePath}\"",
                WorkingDirectory = Path.GetDirectoryName(exePath)!,
                UseShellExecute = false,
                CreateNoWindow = false
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

            if (hwnd == IntPtr.Zero) hwnd = FindWindow("FCEUXWindowClass", null);
            if (hwnd == IntPtr.Zero) hwnd = FindWindow(null, "FCEUX");

            if (hwnd == IntPtr.Zero) return;
            _gameHwnd = hwnd;

            // Wait for FCEUX to fully settle then force its size
            Thread.Sleep(800);
            MoveWindow(hwnd, 0, 0, 512, 480, true);
            Thread.Sleep(200);
        }

        public void EmbedAt(IntPtr parentHwnd, int x, int y, int width, int height)
        {
            if (_gameHwnd == IntPtr.Zero) return;

            const int GWL_STYLE = -16;
            const int GWL_EXSTYLE = -20;
            const int WS_CHILD = 0x40000000;
            const int WS_VISIBLE = 0x10000000;
            const int WS_CLIPSIBLINGS = 0x04000000;
            const int WS_CLIPCHILDREN = 0x02000000;
            const int WS_EX_TOOLWINDOW = 0x00000080;

            // Set as tool window to hide from taskbar
            SetWindowLong(_gameHwnd, GWL_EXSTYLE, WS_EX_TOOLWINDOW);

            // NES native size at 3x
            int gameW = 768;
            int gameH = 720;

            // Centre within the EmuHost area
            int centreX = x + (width - gameW) / 2;
            int centreY = y + (height - gameH) / 2;

            SetParent(_gameHwnd, parentHwnd);
            SetWindowLong(_gameHwnd, GWL_STYLE, WS_CHILD | WS_VISIBLE | WS_CLIPSIBLINGS | WS_CLIPCHILDREN);
            SetWindowLong(_gameHwnd, GWL_EXSTYLE, 0);
            MoveWindow(_gameHwnd, centreX, centreY, gameW, gameH, true);
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

                foreach (var proc in Process.GetProcessesByName("fceux"))
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
            var known = new[] { "fceux","fceux64", "nestopia", "snes9x",
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
                var fceux = Path.Combine(dir.FullName, "ECores", "NES", "FCEUX", "fceux64.exe");
                if (File.Exists(fceux)) return fceux;
                dir = dir.Parent;
            }
            return null;
        }

        private static void EnsureFCEUXConfig()
        {
            var cfgPath = Path.Combine(
                Path.GetDirectoryName(FindEmulatorExecutable()!)!, "fceux.cfg");

            if (!File.Exists(cfgPath)) return;

            var lines = new System.Collections.Generic.List<string>(
                File.ReadAllLines(cfgPath));

            void SetValue(string key, string value)
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].StartsWith($"\"{key}\"") ||
                        lines[i].StartsWith($"{key} "))
                    {
                        lines[i] = lines[i].StartsWith("\"")
                            ? $"\"{key}\" {value}"
                            : $"{key} {value}";
                        return;
                    }
                }
                lines.Add($"\"{key}\" {value}");
            }

            // Set windowed scale to 3x (3.0 as base64 double = AAAAAAAACEA=)
            SetValue("vmcxs", "3");
            SetValue("vmcys", "3");
            // Disable fullscreen
            SetValue("fs", "0");
            // Disable force integral scales
            SetValue("vmspecial", "0");

            File.WriteAllLines(cfgPath, lines);
        }
    }
}