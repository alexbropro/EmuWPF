using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Threading.Tasks;

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

        public async Task<IntPtr> LaunchHiddenAsync(string gamePath)
        {
            await Task.Run(TryTerminateOtherEmulators);

            var exePath = FindEmulatorExecutable();
            if (exePath == null)
            {
                MessageBox.Show("Snes9x not found.\n\nPlace snes9x-x64.exe in:\nECores\\SNES\\snes9x\\",
                    "Missing Emulator");
                return IntPtr.Zero;
            }

            EnsureSnes9xConfig(exePath);

            using var catcher = new WindowCatcher();   // hook goes in BEFORE the process starts

            _process = Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = $"\"{gamePath}\"",
                WorkingDirectory = Path.GetDirectoryName(exePath)!,
                UseShellExecute = false,
                CreateNoWindow = false
            });
            if (_process == null)
            {
                MessageBox.Show("Failed to start Snes9x.", "Launch Error");
                return IntPtr.Zero;
            }

            catcher.Watch(_process);
            _gameHwnd = await catcher.WaitAsync(_process);
            return _gameHwnd;
        }

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
                UseShellExecute = false,
                CreateNoWindow = false
            });

            if (_process == null) return;

            int pid = _process.Id;

            System.Threading.Tasks.Task.Run(() =>
            {
                for (int i = 0; i < 8; i++)
                {
                    Thread.Sleep(15);
                    HideConsoleWindowsForPid(pid);
                }
            });

            // Check every 10ms (was 50ms), check first then wait. Same 3s timeout.
            IntPtr hwnd = IntPtr.Zero;
            for (int i = 0; i < 300; i++)
            {
                try { _process.Refresh(); } catch { break; }
                hwnd = _process.MainWindowHandle;
                if (hwnd != IntPtr.Zero) break;
                Thread.Sleep(5);
            }

            if (hwnd == IntPtr.Zero) hwnd = FindWindow(null, "Snes9x 1.62.3");
            if (hwnd == IntPtr.Zero) hwnd = FindWindow(null, "Snes9x-x64");
            if (hwnd == IntPtr.Zero) hwnd = FindWindow("Snes9xClass", null);

            if (hwnd == IntPtr.Zero) return;
            
            _gameHwnd = hwnd;
            
           
            System.Threading.Tasks.Task.Run(() =>
            {
                // Check every 20ms (was 200ms), check first then wait. Same 4s timeout.
                for (int i = 0; i < 200; i++)
                {
                    IntPtr snesHwnd = FindWindow(null, "Snes9x 1.62.3");
                    if (snesHwnd != IntPtr.Zero)
                    {
                        const int GWL_EXSTYLE = -20;
                        const int WS_EX_TOOLWINDOW = 0x00000080;
                        SetWindowLong(snesHwnd, GWL_EXSTYLE, WS_EX_TOOLWINDOW);
                        break;
                    }
                    System.Threading.Thread.Sleep(5);
                }
            });
            Thread.Sleep(800);
            ShowWindow(_gameHwnd, SW_SHOW);
        }

        public void EmbedAt1(IntPtr parentHwnd, int x, int y, int width, int height)
        {
            //old EMBED FOR REFERENCE
            if (_gameHwnd == IntPtr.Zero) return;

            const int GWL_STYLE = -16;
            const int GWL_EXSTYLE = -20;
            const int WS_CHILD = 0x40000000;
            const int WS_VISIBLE = 0x10000000;
            const int WS_CLIPSIBLINGS = 0x04000000;
            const int WS_CLIPCHILDREN = 0x02000000;
            const int WS_EX_TOOLWINDOW = 0x00000080;
            const int WS_EX_NOACTIVATE = 0x08000000;

            SetParent(_gameHwnd, parentHwnd);
            SetWindowLong(_gameHwnd, GWL_STYLE, WS_CHILD | WS_VISIBLE | WS_CLIPSIBLINGS | WS_CLIPCHILDREN);
            SetWindowLong(_gameHwnd, GWL_EXSTYLE, WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE);
            MoveWindow(_gameHwnd, x, y, width, height, true);
            ShowWindow(_gameHwnd, SW_SHOW);
            SetForegroundWindow(_gameHwnd);
            SetFocus(_gameHwnd);
        }

        const int WS_VISIBLE = 0x10000000;

        public void EmbedAt(IntPtr parentHwnd, int x, int y, int width, int height)
        {
            if (_gameHwnd == IntPtr.Zero) return;

            SetWindowLong(_gameHwnd, GWL_STYLE, WS_CHILD | WS_VISIBLE);
            SetParent(_gameHwnd, parentHwnd);
            MoveWindow(_gameHwnd, x, y, width, height, true);

            // Only now does it become visible, already in position
            WindowCatcher.Reveal(_gameHwnd);

            // 1px nudge forces the renderer to resize its output after un-hiding
            MoveWindow(_gameHwnd, x, y, width, height - 1, true);
            MoveWindow(_gameHwnd, x, y, width, height, true);

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

                foreach (var proc in Process.GetProcessesByName("snes9x-x64"))
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
            var known = new[] { "fceux", "fceux64", "nestopia", "snes9x", "snes9x-x64",
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
                var snes9x = Path.Combine(dir.FullName, "ECores", "SNES", "snes9x", "snes9x-x64.exe");
                if (File.Exists(snes9x)) return snes9x;
                dir = dir.Parent;
            }
            return null;
        }

        /// <summary>
        /// Snes9x reopens its window wherever it was last. Put it far off-screen
        /// so it's never seen before EmbedAt moves it into place, and make sure
        /// it isn't maximised or fullscreen.
        /// </summary>
        private static void EnsureSnes9xConfig(string exePath)
        {
            var confPath = Path.Combine(Path.GetDirectoryName(exePath)!, "snes9x.conf");
            if (!File.Exists(confPath)) return;   // first run: Snes9x creates it

            try
            {
                var lines = File.ReadAllLines(confPath);

                void Set(string key, string value)
                {
                    for (int i = 0; i < lines.Length; i++)
                    {
                        // Lines look like:  "         Window:Left   = 0   # comment"
                        string trimmed = lines[i].TrimStart();
                        if (!trimmed.StartsWith(key)) continue;

                        string rest = trimmed.Substring(key.Length);
                        if (!rest.TrimStart().StartsWith("=")) continue;

                        int eq = lines[i].IndexOf('=');
                        int hash = lines[i].IndexOf('#', eq);
                        string comment = hash >= 0 ? "    " + lines[i].Substring(hash) : "";
                        lines[i] = lines[i].Substring(0, eq + 1) + " " + value + comment;
                        return;
                    }
                }

                Set("Window:Left", "-20000");
                Set("Window:Top", "-20000");
                Set("Window:Maximized", "FALSE");
                Set("Fullscreen:Enabled", "FALSE");
                Set("Fullscreen:FullscreenOnOpen", "FALSE");

                File.WriteAllLines(confPath, lines);
            }
            catch { /* best-effort: the window catcher still hides it */ }
        }
    }
}