using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;

namespace EmuWPF.ECores
{
    class N64EMU
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

        public string Name => "Nintendo 64";
        public IntPtr GameWindowHandle => _gameHwnd;

        private static readonly string[] KnownEmulatorExecutableNames = new[]
        {
            "mupen64plus-ui-console.exe",
            "mupen64plus.exe",
            "mupen64plus-qt.exe",
            "dolphin.exe",
            "Glide64mk2.exe",
            "mupen64.exe"
        };

        public void Launch(string gamePath, string extraArgs = "")
        {
            // If other emulator processes are running, stop them first to avoid conflicts.
            TryTerminateOtherEmulators();

            var exePath = FindEmulatorExecutable();
            if (exePath == null)
            {
                MessageBox.Show("Emulator executable not found.", "Missing Emulator");
                return;
            }


            // UseShellExecute MUST be true for OpenGL to work
            _process = Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = $"{extraArgs} \"{gamePath}\"",
                WorkingDirectory = Path.GetDirectoryName(exePath)!,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden // request hidden window when possible
            });

            if (_process == null) return;

            int pid = _process.Id;

            // Try to hide any console immediately and shortly afterwards.
            HideConsoleWindowsForPid(pid);

            // also schedule a short, quick retry loop to catch consoles created a bit later
            System.Threading.Tasks.Task.Run(() =>
            {
                for (int attempt = 0; attempt < 8; attempt++)
                {
                    Thread.Sleep(50); // quick retries
                    HideConsoleWindowsForPid(pid);
                }
            });

            // Wait for the game window handle to appear (shorter waits)
            IntPtr hwnd = IntPtr.Zero;
            for (int i = 0; i < 60; i++) // up to ~3s (60 * 50ms)
            {
                Thread.Sleep(50);
                try { _process.Refresh(); } catch { break; }
                hwnd = _process.MainWindowHandle;
                if (hwnd != IntPtr.Zero) break;
            }

            // Fallback by title
            if (hwnd == IntPtr.Zero)
                hwnd = FindWindow(null, "Glide64mk2");
            if (hwnd == IntPtr.Zero)
                hwnd = FindWindow(null, "MupenPlus64 OpenGL Video Plugin by rice");

            if (hwnd == IntPtr.Zero) return;

            _gameHwnd = hwnd;

            // Let OpenGL fully initialise (reduced from 3s to 1s for faster UX)
            Thread.Sleep(1000);

            System.Threading.Tasks.Task.Run(() =>
            {
                // Wait for Mupen to finish writing its config
                Thread.Sleep(2000);

                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var mupenDir = Path.Combine(appData, "mupen64plus");
                var profilePath = Path.Combine(AppContext.BaseDirectory, "n64_controls.json");

                if (Directory.Exists(mupenDir) && File.Exists(profilePath))
                {
                    try
                    {
                        var json = File.ReadAllText(profilePath);
                        var profile = System.Text.Json.JsonSerializer
                                          .Deserialize<EmuWPF.N64EmuInp>(json);
                        profile?.ApplyToMupen(mupenDir);
                    }
                    catch { }
                }
            });
        }

        private void TryTerminateOtherEmulators()
        {
            try
            {
                var currentPid = Process.GetCurrentProcess().Id;
                var processes = Process.GetProcesses();

                foreach (var proc in processes)
                {
                    try
                    {
                        // Skip current process
                        if (proc.Id == currentPid) continue;

                        string? fileName = null;
                        try
                        {
                            // MainModule may fail under restricted permissions; catch and fallback to ProcessName.
                            fileName = Path.GetFileName(proc.MainModule?.FileName ?? string.Empty)?.ToLowerInvariant();
                        }
                        catch
                        {
                            // ignore; will attempt process.ProcessName below
                        }

                        bool isKnown = false;
                        if (!string.IsNullOrEmpty(fileName))
                        {
                            foreach (var name in KnownEmulatorExecutableNames)
                            {
                                if (fileName == name.ToLowerInvariant())
                                {
                                    isKnown = true;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            // fallback: check process name without extension
                            var procName = proc.ProcessName.ToLowerInvariant();
                            foreach (var name in KnownEmulatorExecutableNames)
                            {
                                var pn = Path.GetFileNameWithoutExtension(name).ToLowerInvariant();
                                if (procName == pn)
                                {
                                    isKnown = true;
                                    break;
                                }
                            }
                        }

                        if (isKnown)
                        {
                            try
                            {
                                // Kill entire tree when available (supported in .NET 8)
                                proc.Kill(entireProcessTree: true);
                                proc.WaitForExit(2000);
                            }
                            catch
                            {
                                // best-effort: ignore individual failures
                            }
                        }
                    }
                    catch
                    {
                        // ignore per-process inspection errors
                    }
                }
            }
            catch
            {
                // overall failure; do nothing (best-effort)
            }
        }

        // New: show/hide embedded game window without killing the process
        public void SetEmbeddedVisibility(bool visible)
        {
            if (_gameHwnd == IntPtr.Zero) return;
            try
            {
                ShowWindow(_gameHwnd, visible ? SW_SHOW : SW_HIDE);
            }
            catch { /* best-effort */ }
        }



        // Best-effort console hide: enumerate windows and hide any with class ConsoleWindowClass that belong to pid.
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
                    {
                        ShowWindow(hWnd, SW_HIDE);
                    }
                }
                catch { }
                return true;
            }, IntPtr.Zero);
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

        public void Stop()
        {
            try
            {
                if (_process != null && !_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                    _process = null;
                    _gameHwnd = IntPtr.Zero;
                }
            }
            catch { }
        }

        private static string? FindEmulatorExecutable()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(
                    dir.FullName, "ECores", "N64", "MP64", "Release",
                    "mupen64plus-ui-console.exe");
                if (File.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }
            return null;
        }
    }
}