using EmuWPF.ECores;
using Microsoft.Win32;
using NAudio.CoreAudioApi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColor = System.Windows.Media.Color;

namespace EmuWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>



    public partial class MainWindow : Window
    {
        private N64EMU? _activeEmu;
        private NESEMU? _activeNES = null;
        private SNESEMU? _activeSNES = null;
        private DolphinEMU? _activeDolphin = null;
        private NESEmuInp _nesControls = NESEmuInp.Load();
        private SNESEmuInp _snesControls = SNESEmuInp.Load();
        private DolphinEmuInp _dolphinControls = DolphinEmuInp.Load();

        private bool _isLaunching = false;

        [DllImport("user32.dll")] static extern IntPtr SetFocus(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
        [DllImport("user32.dll")] private static extern uint GetCurrentThreadId();
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
        [DllImport("user32.dll")] private static extern IntPtr SetActiveWindow(IntPtr hWnd);
        [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool MoveWindow(IntPtr hWnd, int x, int y, int w, int h, bool repaint);
        [DllImport("user32.dll")] private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);
        [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [DllImport("shell32.dll")] private static extern IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

        [DllImport("user32.dll")] private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [StructLayout(LayoutKind.Sequential)]

        private struct APPBARDATA
        {
            public uint cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public uint uEdge;
            public System.Drawing.Rectangle rc;
            public int lParam;
        }
        private struct POINT { public int X; public int Y; }

        private string _currentSystem = "";
        private readonly string _gamesFolder;
        private AppSettings _settings = AppSettings.Load();
        private N64EmuInp _n64Controls = N64EmuInp.Load();
        private TextBox? _activeBindBox = null;

        private void ShowTaskbar(bool show)
        {
            IntPtr taskbar = FindWindow("Shell_TrayWnd", null);
            IntPtr start = FindWindow("Button", null);
            ShowWindow(taskbar, show ? 1 : 0);
            ShowWindow(start, show ? 1 : 0);
        }

        private void SetTaskbarAutoHide(bool autoHide)
        {
            var data = new APPBARDATA();
            data.cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf(data);
            uint state = (uint)SHAppBarMessage(0x00000004, ref data); // ABM_GETSTATE

            if (autoHide)
                state |= 0x01;  // ABS_AUTOHIDE
            else
                state &= ~0x01u;

            data.lParam = (int)state;
            SHAppBarMessage(0x00000005, ref data); // ABM_SETSTATE
        }

        public MainWindow()
        {
            InitializeComponent();
            LoadSettingsIntoUI();
            this.ResizeMode = ResizeMode.NoResize;


           
            _gamesFolder = System.IO.Path.Combine(
    AppContext.BaseDirectory, "Games");

            if (!Directory.Exists(_gamesFolder))
                Directory.CreateDirectory(_gamesFolder);

            LoadGames();
        }
        private void LoadGames()
        {
            GameGrid.Children.Clear();

            var extensions = new[]
{
    ".nes",
    ".sfc", ".smc",
    ".n64", ".z64", ".v64",
    ".iso", ".gcm", ".rvz", ".gcz", ".ciso",
    ".wbfs", ".wia", ".dol"
};

            if (!Directory.Exists(_gamesFolder))
            {
                NoGamesText.Visibility = Visibility.Visible;
                return;
            }

            var files = Directory.GetFiles(_gamesFolder)
                                 .Where(f => extensions.Contains(System.IO.Path.GetExtension(f).ToLowerInvariant()))
                                 .OrderBy(f => System.IO.Path.GetFileNameWithoutExtension(f));

            foreach (var file in files)
            {
                var btn = CreateGameButton(file);
                GameGrid.Children.Add(btn);
            }

            NoGamesText.Visibility = GameGrid.Children.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void NavBtn_Click(object sender, RoutedEventArgs e)
        {
            // Clear selection tags
            BHome.Tag = "0";
            BLibrary.Tag = "0";
            BEmuCores.Tag = "0";
            BUpdates.Tag = "0";
            BSettings.Tag = "0";
            BNP.Tag = "0";

            if (sender is System.Windows.Controls.Button btn)
            {
                // Mark clicked button as selected
                btn.Tag = "1";

                // Hide all pages
                PageHome.Visibility = Visibility.Collapsed;
                PageLibrary.Visibility = Visibility.Collapsed;
                PageEmucores.Visibility = Visibility.Collapsed;
                PageUpdates.Visibility = Visibility.Collapsed;
                PageNowPlaying.Visibility = Visibility.Collapsed;
                PageSettings.Visibility = Visibility.Collapsed;

                // Show the page for the clicked button
                if (btn == BHome) PageHome.Visibility = Visibility.Visible;
                else if (btn == BLibrary)
                {
                    // refresh library when shown
                    LoadGames();
                    PageLibrary.Visibility = Visibility.Visible;
                }
                else if (btn == BEmuCores) PageEmucores.Visibility = Visibility.Visible;
                else if (btn == BUpdates) PageUpdates.Visibility = Visibility.Visible;
                else if (btn == BSettings) PageSettings.Visibility = Visibility.Visible;
                else if (btn == BNP) PageNowPlaying.Visibility = Visibility.Visible;

                try
                {
                    if (_activeDolphin != null || _activeEmu != null ||
        _activeNES != null || _activeSNES != null)
                    {
                        if (btn == BNP)
                            SetActiveEmuVisibility(true);
                        else
                            SetActiveEmuVisibility(false);
                    }
                }
                catch { /* best-effort */ }
            }
        }

        private void SetActiveEmuVisibility(bool visible)
        {
            _activeEmu?.SetEmbeddedVisibility(visible);
            _activeNES?.SetEmbeddedVisibility(visible);
            _activeSNES?.SetEmbeddedVisibility(visible);
            _activeDolphin?.SetEmbeddedVisibility(visible);
        }

        private void FilterBtn_Click(object sender, RoutedEventArgs e)
        {
            BtnFilterAll.Tag = "0";
            BtnFilterNES.Tag = "0";
            BtnFilterSNES.Tag = "0";
            BtnFilterN64.Tag = "0";
            BtnFilterGCN.Tag = "0";
            BtnFilterWii.Tag = "0";

            var btn = (Button)sender;
            btn.Tag = "1";

            // Get which system was selected
            string filter = btn.Name switch
            {
                "BtnFilterNES" => "nes",
                "BtnFilterSNES" => "snes",
                "BtnFilterN64" => "n64",
                "BtnFilterGCN" => "gcn",
                "BtnFilterWii" => "wii",
                _ => "all"
            };

            ApplyLibraryFilter(filter);
        }

        private void BtnAddGame_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"Games folder: {_gamesFolder}", "Debug");
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Add Game to Library",
                Multiselect = true,
                Filter =
                    "All Supported ROMs|*.nes;*.sfc;*.smc;*.n64;*.z64;*.v64;*.iso;*.gcm;*.rvz;*.gcz;*.ciso;*.wbfs;*.wia;*.dol|" +
                    "NES ROMs|*.nes|" +
                    "SNES ROMs|*.sfc;*.smc|" +
                    "N64 ROMs|*.n64;*.z64;*.v64|" +
                    "GameCube ROMs|*.iso;*.gcm;*.rvz;*.gcz;*.ciso|" +
                    "Wii ROMs|*.wbfs;*.wia;*.dol|" +
                    "All Files|*.*"
            };

            if (dialog.ShowDialog() != true) return;

            int added = 0;
            int skipped = 0;

            foreach (var sourcePath in dialog.FileNames)
            {
                var fileName = System.IO.Path.GetFileName(sourcePath);
                var destPath = System.IO.Path.Combine(_gamesFolder, fileName);

                // Already in the Games folder — no need to copy
                if (string.Equals(
                    System.IO.Path.GetDirectoryName(sourcePath),
                    _gamesFolder,
                    StringComparison.OrdinalIgnoreCase))
                {
                    skipped++;
                    continue;
                }

                // Duplicate already exists
                if (File.Exists(destPath))
                {
                    skipped++;
                    continue;
                }

                try
                {
                    File.Copy(sourcePath, destPath, overwrite: false);
                    added++;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to add {fileName}:\n{ex.Message}", "Error");
                }
            }

            // Also make sure LoadGames picks up Wii extensions
            LoadGames();

            // Reapply active filter
            string activeFilter = "all";
            if (BtnFilterNES.Tag?.ToString() == "1") activeFilter = "nes";
            if (BtnFilterSNES.Tag?.ToString() == "1") activeFilter = "snes";
            if (BtnFilterN64.Tag?.ToString() == "1") activeFilter = "n64";
            if (BtnFilterGCN.Tag?.ToString() == "1") activeFilter = "gcn";
            if (BtnFilterWii.Tag?.ToString() == "1") activeFilter = "wii";
            ApplyLibraryFilter(activeFilter);

            var msg = $"{added} game{(added != 1 ? "s" : "")} added.";
            if (skipped > 0) msg += $"\n{skipped} skipped (already in library).";
            if (added > 0 || skipped > 0) MessageBox.Show(msg, "Games Added");
        }

        private void ApplyLibraryFilter(string filter)
        {
            foreach (UIElement child in GameGrid.Children)
            {
                if (child is not Button btn) continue;
                if (btn.Tag is not string path) continue;

                var ext = System.IO.Path.GetExtension(path).ToLower();

                bool show = filter switch
                {
                    "nes" => ext is ".nes",
                    "snes" => ext is ".sfc" or ".smc",
                    "n64" => ext is ".n64" or ".z64" or ".v64",
                    "gcn" => ext is ".iso" or ".gcm" or ".rvz" or ".gcz" or ".ciso",
                    "wii" => ext is ".wbfs" or ".wia" or ".dol",
                    _ => true  // "all" shows everything
                };

                btn.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void EmuFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;

            _currentSystem = btn.Tag?.ToString() ?? "";
            EmucoreSystemTitle.Text = $"{_currentSystem} — EmuCore Settings";

            CoreDropdown.Items.Clear();
            var cores = _currentSystem switch
            {
                "NES" => new[] { "FCEUX", "Nestopia UE", "Mesen" },
                "SNES" => new[] { "Snes9x", "bsnes", "Mesen-S" },
                "N64" => new[] { "Mupen64Plus", "Project64", "ParaLLEl" },
                "GameCube" => new[] { "Dolphin" },
                "Wii" => new[] { "Dolphin" },
                _ => new[] { "Unknown" }
            };

            foreach (var core in cores)
                CoreDropdown.Items.Add(core);

            CoreDropdown.SelectedIndex = 0;

            EmucoresFolderView.Visibility = Visibility.Collapsed;
            EmucoreSettingsView.Visibility = Visibility.Visible;
        }

        private void EmucoreBack_Click(object sender, RoutedEventArgs e)
        {
            EmucoreSettingsView.Visibility = Visibility.Collapsed;
            EmucoresFolderView.Visibility = Visibility.Visible;
        }

        private void AddCore_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CustomCorePath.Text))
            {
                MessageBox.Show("Please browse to a core .exe first.", "No file selected");
                return;
            }

            var name = System.IO.Path.GetFileNameWithoutExtension(CustomCorePath.Text);

            // Avoid duplicates
            foreach (var item in CoreDropdown.Items)
                if (item?.ToString() == name) return;

            CoreDropdown.Items.Add(name);
            CoreDropdown.SelectedItem = name;
            CustomCorePath.Text = "";
        }

        private Button CreateGameButton(string filePath)
        {
            var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            var ext = System.IO.Path.GetExtension(filePath).TrimStart('.').ToUpperInvariant();

            var btn = new Button
            {
                Tag = filePath,
                Width = 160,
                Height = 110,
                Margin = new Thickness(0, 0, 12, 12),
                Padding = new Thickness(8),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };

            // Apply a style if available (SecondaryBtn defined in XAML resources)
            try
            {
                var style = (Style)FindResource("SecondaryBtn");
                if (style != null) btn.Style = style;
            }
            catch
            {
                // ignore if style not found
            }

            var sp = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };

            var title = new TextBlock
            {
                Text = fileName,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                Width = 140,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 6, 0, 2)
            };
            // match colors used elsewhere
            try
            {
                title.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8E8F0"));
            }
            catch
            {
                title.Foreground = Brushes.LightGray;
            }

            var meta = new TextBlock
            {
                Text = ext,
                FontSize = 11,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9090B0")),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            sp.Children.Add(title);
            sp.Children.Add(meta);

            btn.Content = sp;
            btn.Click += GameButton_Click;

            return btn;
        }

        // NEW: Resize the main window so EmuHost can be exactly emuPixelWidth x emuPixelHeight (pixels).
        // Uses DPI to convert pixels -> WPF device-independent units and accounts for sidebar width,
        // header height and a small chrome margin. Clamped to the work area.
        private void ResizeWindowForEmuHost(int emuPixelWidth, int emuPixelHeight)
        {
            var dpi = VisualTreeHelper.GetDpi(this);
            var wa = SystemParameters.WorkArea;

            NowPlayingHeader.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double headerDip = NowPlayingHeader.DesiredSize.Height;
            if (headerDip <= 0) headerDip = 45;

            const double sidebarWidth = 200.0;

            // Hard limits — never exceed these
            double maxW = wa.Width;
            double maxH = wa.Height;

            // Convert to DIPs
            double dipW = emuPixelWidth / dpi.DpiScaleX;
            double dipH = emuPixelHeight / dpi.DpiScaleY;

            // Available game area
            double maxGameW = maxW - sidebarWidth;
            double maxGameH = maxH - headerDip;

            // Scale down preserving aspect ratio
            double scale = Math.Min(maxGameW / dipW, maxGameH / dipH);
            if (scale < 1.0)
            {
                dipW *= scale;
                dipH *= scale;
            }

            double targetW = sidebarWidth + dipW;
            double targetH = headerDip + dipH;

            // Final hard clamp — safety net
            targetW = Math.Min(targetW, maxW);
            targetH = Math.Min(targetH, maxH);

            MessageBox.Show(
    $"targetW: {targetW}\n" +
    $"targetH: {targetH}\n" +
    $"wa.Width: {wa.Width}\n" +
    $"wa.Height: {wa.Height}\n" +
    $"dipW: {dipW}\n" +
    $"dipH: {dipH}\n" +
    $"scale: {scale}\n" +
    $"headerDip: {headerDip}",
    "Debug");

            this.Width = targetW;
            this.Height = targetH;

            this.Left = Math.Max(wa.Left, (wa.Width - targetW) / 2 + wa.Left);
            this.Top = Math.Max(wa.Top, (wa.Height - targetH) / 2 + wa.Top);

            this.UpdateLayout();
            MessageBox.Show($"Actual after set: {this.ActualWidth} x {this.ActualHeight}", "After Resize");
        }

        private (int x, int y, int w, int h) GetEmuHostPixelRect()
        {
            var dpi = VisualTreeHelper.GetDpi(this);
            int w = (int)(EmuHost.ActualWidth * dpi.DpiScaleX);
            int h = (int)(EmuHost.ActualHeight * dpi.DpiScaleY);

            // Get EmuHost position relative to main window client area
            var topLeft = EmuHost.PointToScreen(new Point(0, 0));
            var windowOrigin = this.PointToScreen(new Point(0, 0));

            int x = (int)(topLeft.X - windowOrigin.X);
            int y = (int)(topLeft.Y - windowOrigin.Y);

            return (x, y, w, h);
        }

        private void GameButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_isLaunching) return;
            _isLaunching = true;

            if (sender is System.Windows.Controls.Button b && b.Tag is string path)
            {
                var ext = System.IO.Path.GetExtension(path).ToLower();
                var name = System.IO.Path.GetFileNameWithoutExtension(path);

                bool isN64 = ext is ".n64" or ".z64" or ".v64";
                bool isNES = ext is ".nes";
                bool isSNES = ext is ".sfc" or ".smc";
                bool isGCN = ext is ".iso" or ".gcm" or ".rvz" or ".gcz" or ".ciso";
                bool isWii = ext is ".wbfs" or ".wia" or ".dol";
                BNP.Visibility = Visibility.Visible;
                        NowPlayingTitle.Text = name;

                        BHome.Tag = "0";
                        BLibrary.Tag = "0";
                        BEmuCores.Tag = "0";
                        BUpdates.Tag = "0";
                        BSettings.Tag = "0";
                        BNP.Tag = "1";

                        // Resize window so EmuHost area matches 640x480 pixels
                        // Do this before making PageNowPlaying visible so layout is correct.
                        var resParts = _settings.Resolution.Split('x');
                        double emuW = int.TryParse(resParts[0], out int rw) ? rw : 800;
                        double emuH = int.TryParse(resParts.Length > 1 ? resParts[1] : "", out int rh) ? rh : 600;
                        ResizeWindowForEmuHost((int)emuW, (int)emuH);
                        this.ResizeMode = ResizeMode.NoResize;

                        PageLibrary.Visibility = Visibility.Collapsed;
                        PageNowPlaying.Visibility = Visibility.Visible;

                        // Force layout so EmuHost has correct ActualWidth/Height
                        PageNowPlaying.UpdateLayout();
                        EmuHost.UpdateLayout();
                        
                        
                        var dpi = VisualTreeHelper.GetDpi(this);
                        var wa = SystemParameters.WorkArea;

                        NowPlayingHeader.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                        double headerDip = NowPlayingHeader.DesiredSize.Height > 0
                            ? NowPlayingHeader.DesiredSize.Height : 45;

                        double maxGameW = (wa.Width - 200) * dpi.DpiScaleX;
                        double maxGameH = (wa.Height - headerDip) * dpi.DpiScaleY;

                        double scale = Math.Min(maxGameW / emuW, maxGameH / emuH);
                        if (scale < 1.0) { emuW *= scale; emuH *= scale; }

                        var screenMode = _settings.Fullscreen ? "--fullscreen" : "--windowed";
                        var args = $"{screenMode} --resolution {(int)emuW}x{(int)emuH}";

                if (isN64)
                {
                    _activeEmu = new N64EMU();
                    System.Threading.Tasks.Task.Run(() =>
                    {
                        _activeEmu.Launch(path, args);
                        Dispatcher.Invoke(() =>
                        {
                            PageNowPlaying.UpdateLayout();
                            EmuHost.UpdateLayout();
                            var mainHwnd = new WindowInteropHelper(this).Handle;
                            var (x, y, w, h) = GetEmuHostPixelRect();
                            _activeEmu.EmbedAt(mainHwnd, x, y, w, h);
                            _isLaunching = false;
                        });
                    });
                }
                else if (isNES)
                {
                    SetTaskbarAutoHide(true);  // hide taskbar
                    this.WindowStyle = WindowStyle.None;
                    this.WindowState = WindowState.Normal;
                    this.ResizeMode = ResizeMode.NoResize;
                    this.Top = 0;
                    this.Left = 0;
                    this.Width = SystemParameters.PrimaryScreenWidth;
                    this.Height = SystemParameters.PrimaryScreenHeight;

                    PageLibrary.Visibility = Visibility.Collapsed;
                    PageNowPlaying.Visibility = Visibility.Visible;

                    System.Threading.Thread.Sleep(100);
                    PageNowPlaying.UpdateLayout();
                    EmuHost.UpdateLayout();

                    _activeNES = new NESEMU();

                    System.Threading.Tasks.Task.Run(() =>
                    {
                        _activeNES.Launch(path);

                        Dispatcher.Invoke(() =>
                        {
                            PageNowPlaying.UpdateLayout();
                            EmuHost.UpdateLayout();

                            var mainHwnd = new WindowInteropHelper(this).Handle;
                            var (x, y, w, h) = GetEmuHostPixelRect();

                            _activeNES.EmbedAt(mainHwnd, x, y, w, h);

                            var refocusTimer = new System.Windows.Threading.DispatcherTimer();
                            refocusTimer.Interval = TimeSpan.FromMilliseconds(200);
                            int refocusCount = 0;
                            refocusTimer.Tick += (s, args) =>
                            {
                                if (_activeNES?.GameWindowHandle != IntPtr.Zero)
                                {
                                    SetFocus(_activeNES.GameWindowHandle);
                                    SetForegroundWindow(_activeNES.GameWindowHandle);
                                    ShowTaskbar(false);
                                }
                                refocusCount++;
                                if (refocusCount >= 15) refocusTimer.Stop();
                                SetTaskbarAutoHide(true);
                            };
                            refocusTimer.Start();
                            _isLaunching = false;
                        });
                    });
                    this.WindowStyle = WindowStyle.None;
                }
                else if (isSNES)
                {
                    _activeSNES = new SNESEMU();
                    System.Threading.Tasks.Task.Run(() =>
                    {
                        _activeSNES.Launch(path);
                        Dispatcher.Invoke(() =>
                        {
                            PageNowPlaying.UpdateLayout();
                            EmuHost.UpdateLayout();
                            var mainHwnd = new WindowInteropHelper(this).Handle;
                            var (x, y, w, h) = GetEmuHostPixelRect();
                            _activeSNES.EmbedAt(mainHwnd, x, y, w, h);
                            _isLaunching = false;
                        });
                    });
                }
                else if (isGCN || isWii)
                {
                    _activeDolphin = new DolphinEMU();
                    System.Threading.Tasks.Task.Run(() =>
                    {
                        _activeDolphin.Launch(path);
                        Dispatcher.Invoke(() =>
                        {
                            PageNowPlaying.UpdateLayout();
                            EmuHost.UpdateLayout();
                            var mainHwnd = new WindowInteropHelper(this).Handle;
                            var (x, y, w, h) = GetEmuHostPixelRect();
                            _activeDolphin.EmbedAt(mainHwnd, x, y, w, h);
                            MessageBox.Show(
    $"GameHwnd: {_activeDolphin.GameWindowHandle}\n" +
    $"ProcessId: {_activeDolphin.ProcessId}",
    "Dolphin Debug");

                            var refocusTimer = new System.Windows.Threading.DispatcherTimer();
                            refocusTimer.Interval = TimeSpan.FromMilliseconds(200);
                            int refocusCount = 0;
                            refocusTimer.Tick += (s, args) =>
                            {
                                if (_activeDolphin?.ProcessId > 0)
                                    _activeDolphin.RefreshGameHandle(_activeDolphin.ProcessId);

                                var hwnd = _activeDolphin?.GameWindowHandle ?? IntPtr.Zero;
                                if (hwnd != IntPtr.Zero)
                                {
                                    SetActiveWindow(hwnd);
                                    SetForegroundWindow(hwnd);
                                    SetFocus(hwnd);
                                }
                                refocusCount++;
                                if (refocusCount >= 15) refocusTimer.Stop();
                            };
                            refocusTimer.Start();
                            _isLaunching = false;
                        });
                    });
                }
            }
            else
            {
                _isLaunching = false;
            }
        }

        private void BtnStopEmu_Click(object sender, RoutedEventArgs e)
        {
            _activeEmu?.Stop();
            _activeDolphin?.Stop();
            _activeNES?.Stop();
            _activeSNES?.Stop();
            _activeEmu = null;
            _isLaunching = false;

            // Hide Now Playing from sidebar
            BNP.Visibility = Visibility.Collapsed;
            BNP.Tag = "0";

            // Go back to Library
            PageNowPlaying.Visibility = Visibility.Collapsed;
            PageLibrary.Visibility = Visibility.Visible;
            BLibrary.Tag = "1";
            ShowTaskbar(true);

        }

        private void BrowseCore_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select Emulator Executable",
                Filter = "Executable|*.exe|All Files|*.*"
            };

            if (dialog.ShowDialog() == true)
                CustomCorePath.Text = dialog.FileName;
        }

        private void EmuHost_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

            IntPtr hwnd = IntPtr.Zero;

            if (_activeEmu != null) hwnd = _activeEmu.GameWindowHandle;
            else if (_activeNES != null) hwnd = _activeNES.GameWindowHandle;
            else if (_activeSNES != null) hwnd = _activeSNES.GameWindowHandle;
            else if (_activeDolphin != null) hwnd = _activeDolphin.GameWindowHandle;

            if (hwnd != IntPtr.Zero)
            {
                SetForegroundWindow(hwnd);
                SetFocus(hwnd);
            }
        }

        // Intercept Windows messages to catch focus changes
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source?.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_ACTIVATE = 0x0006;
            const int WM_SETFOCUS = 0x0007;
            const int WM_MOUSEACTIVATE = 0x0021;
            const int MA_NOACTIVATE = 3;

            try
            {
                IntPtr gameHwnd = GetActiveEmuHandle();

                if (gameHwnd != IntPtr.Zero)
                {
                    if (msg == WM_SETFOCUS || msg == WM_ACTIVATE)
                    {
                        SetActiveWindow(gameHwnd);
                        SetForegroundWindow(gameHwnd);
                        SetFocus(gameHwnd);
                    }

                    if (msg == WM_MOUSEACTIVATE)
                    {
                        handled = true;
                        return new IntPtr(MA_NOACTIVATE);
                    }
                }
            }
            catch { }

            return IntPtr.Zero;
        }


        private IntPtr GetActiveEmuHandle()
        {
            if (_activeEmu != null) return _activeEmu.GameWindowHandle;
            if (_activeNES != null) return _activeNES.GameWindowHandle;
            if (_activeSNES != null) return _activeSNES.GameWindowHandle;
            if (_activeDolphin != null) return _activeDolphin.GameWindowHandle;
            return IntPtr.Zero;
        }


        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ShowTaskbar(true);
            try
            {
                _activeEmu?.Stop();
                _activeNES?.Stop();
                _activeSNES?.Stop();
                _activeDolphin?.Stop();
            }
            catch { }
        }

        //SETTINGS
        // ── Settings load / save ─────────────────────────────────────────────────

        private void LoadSettingsIntoUI()
        {
            TxtAccentColour.Text = _settings.AccentColour;
            TxtBgColour.Text = _settings.BackgroundColour;
            SliderVolume.Value = _settings.Volume;
            ChkMute.IsChecked = _settings.Muted;
            ChkFullscreen.IsChecked = _settings.Fullscreen;

            CmbResolution.SelectedIndex = _settings.Resolution switch
            {
                "640x480" => 0,
                "800x600" => 1,
                "1280x720" => 2,
                "1920x1080" => 3,
                _ => 1
            };

            // Apply saved theme on startup
            ApplyAccentColour(_settings.AccentColour);
            ApplyBackgroundColour(_settings.BackgroundColour);
            ApplyVolume(_settings.Volume, _settings.Muted);
        }

        private void BtnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            _settings.AccentColour = TxtAccentColour.Text;
            _settings.BackgroundColour = TxtBgColour.Text;
            _settings.Volume = (int)SliderVolume.Value;
            _settings.Muted = ChkMute.IsChecked == true;
            _settings.Fullscreen = ChkFullscreen.IsChecked == true;
            _settings.Resolution = (CmbResolution.SelectedItem as
                                          System.Windows.Controls.ComboBoxItem)
                                          ?.Content?.ToString() ?? "800x600";
            _settings.Save();
            MessageBox.Show("Settings saved!", "Saved");
        }

        // ── Theme ────────────────────────────────────────────────────────────────

        private void ApplyAccentColour(string hex)
        {
            try
            {
                var colour = (System.Windows.Media.Color)
                    System.Windows.Media.ColorConverter.ConvertFromString(hex);
                var brush = new System.Windows.Media.SolidColorBrush(colour);

                // Update accent preview swatch
                AccentPreview.Background = brush;

                // Push new colour into App resources so all styles pick it up
                Application.Current.Resources["BrCyan"] = brush;
                Application.Current.Resources["ColCyan"] = colour;

                // Update sidebar highlight background with transparency
                var dimColour = System.Windows.Media.Color.FromArgb(38, colour.R, colour.G, colour.B);
                Application.Current.Resources["BrCyanDim"] =
                    new System.Windows.Media.SolidColorBrush(dimColour);

                var borderColour = System.Windows.Media.Color.FromArgb(64, colour.R, colour.G, colour.B);
                Application.Current.Resources["BrBorderMid"] =
                    new System.Windows.Media.SolidColorBrush(borderColour);

                var softBorder = System.Windows.Media.Color.FromArgb(31, colour.R, colour.G, colour.B);
                Application.Current.Resources["BrBorder"] =
                    new System.Windows.Media.SolidColorBrush(softBorder);
            }
            catch
            {
                MessageBox.Show("Invalid hex colour. Use format #RRGGBB", "Invalid Colour");
            }
        }

        private void ApplyBackgroundColour(string hex)
        {
            try
            {
                var colour = (System.Windows.Media.Color)
                    System.Windows.Media.ColorConverter.ConvertFromString(hex);
                var brush = new System.Windows.Media.SolidColorBrush(colour);

                BgPreview.Background = brush;
                this.Background = brush;
            }
            catch
            {
                MessageBox.Show("Invalid hex colour. Use format #RRGGBB", "Invalid Colour");
            }
        }

        private void BtnApplyAccent_Click(object sender, RoutedEventArgs e)
            => ApplyAccentColour(TxtAccentColour.Text);

        private void BtnApplyBg_Click(object sender, RoutedEventArgs e)
            => ApplyBackgroundColour(TxtBgColour.Text);

        // ── Audio ────────────────────────────────────────────────────────────────

        private void ApplyVolume(int volume, bool muted)
        {
            try
            {
                var device = new MMDeviceEnumerator()
                    .GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);

                device.AudioEndpointVolume.MasterVolumeLevelScalar = volume / 100f;
                device.AudioEndpointVolume.Mute = muted;
            }
            catch { }
        }

        private void SliderVolume_Changed(object sender,
            RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtVolume == null) return;
            int vol = (int)SliderVolume.Value;
            TxtVolume.Text = vol.ToString();

            // Apply live as slider moves
            ApplyVolume(vol, ChkMute.IsChecked == true);
        }

        private void ChkMute_Changed(object sender, RoutedEventArgs e)
        {
            ApplyVolume((int)SliderVolume.Value, ChkMute.IsChecked == true);
        }

        // ── Resolution / Fullscreen (used at launch time in N64EMU) ─────────────

        public string GetLaunchArgs()
        {
            var res = (CmbResolution.SelectedItem as
                              System.Windows.Controls.ComboBoxItem)
                              ?.Content?.ToString() ?? "800x600";
            var parts = _settings.Resolution.Split('x');
            var w = parts.Length > 0 ? parts[0] : "800";
            var h = parts.Length > 1 ? parts[1] : "600";
            var screenMode = _settings.Fullscreen ? "--fullscreen" : "--windowed";

            return $"{screenMode} --resolution {w}x{h}";
        }

        private void EmuHost_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_activeEmu == null || _activeEmu.GameWindowHandle == IntPtr.Zero) return;
            var (x, y, w, h) = GetEmuHostPixelRect();
            _activeEmu.ResizeTo(x, y, w, h);
        }

        // ── EmuCores tab switching ───────────────────────────────────────────────

        private void EmucoreTab_Click(object sender, RoutedEventArgs e)
        {
            TabBtnCore.Tag = "0";
            TabBtnControls.Tag = "0";

            var btn = (Button)sender;
            btn.Tag = "1";

            TabCore.Visibility = btn == TabBtnCore
                ? Visibility.Visible : Visibility.Collapsed;
            TabControls.Visibility = btn == TabBtnControls
                ? Visibility.Visible : Visibility.Collapsed;

            if (btn == TabBtnControls)
            {
                switch (_currentSystem)
                {
                    case "NES": LoadNESControlsIntoUI(); break;
                    case "SNES": LoadSNESControlsIntoUI(); break;
                    case "N64": LoadControlsIntoUI(); break;
                    case "GameCube":
                    case "Wii": LoadDolphinControlsIntoUI(); break;
                }
            }
        }

        // ── Load / Save controls UI ──────────────────────────────────────────────

        private void LoadControlsIntoUI()
        {

            N64ControlsPanel.Visibility = Visibility.Visible;
            NESControlsPanel.Visibility = Visibility.Collapsed;
            SNESControlsPanel.Visibility = Visibility.Collapsed;
            DolphinControlsPanel.Visibility = Visibility.Collapsed;


            ToggleController.IsChecked = _n64Controls.UseController;
            BindStickUp.Text = _n64Controls.StickUp;
            BindStickDown.Text = _n64Controls.StickDown;
            BindStickLeft.Text = _n64Controls.StickLeft;
            BindStickRight.Text = _n64Controls.StickRight;
            BindA.Text = _n64Controls.A;
            BindB.Text = _n64Controls.B;
            BindZ.Text = _n64Controls.Z;
            BindStart.Text = _n64Controls.Start;
            BindL.Text = _n64Controls.L;
            BindR.Text = _n64Controls.R;
            BindCUp.Text = _n64Controls.CUp;
            BindCDown.Text = _n64Controls.CDown;
            BindCLeft.Text = _n64Controls.CLeft;
            BindCRight.Text = _n64Controls.CRight;
            BindDUp.Text = _n64Controls.DUp;
            BindDDown.Text = _n64Controls.DDown;
            BindDLeft.Text = _n64Controls.DLeft;
            BindDRight.Text = _n64Controls.DRight;
        }

        private void SaveControlsFromUI()
        {
            _n64Controls.StickUp = BindStickUp.Text;
            _n64Controls.StickDown = BindStickDown.Text;
            _n64Controls.StickLeft = BindStickLeft.Text;
            _n64Controls.StickRight = BindStickRight.Text;
            _n64Controls.A = BindA.Text;
            _n64Controls.B = BindB.Text;
            _n64Controls.Z = BindZ.Text;
            _n64Controls.Start = BindStart.Text;
            _n64Controls.L = BindL.Text;
            _n64Controls.R = BindR.Text;
            _n64Controls.CUp = BindCUp.Text;
            _n64Controls.CDown = BindCDown.Text;
            _n64Controls.CLeft = BindCLeft.Text;
            _n64Controls.CRight = BindCRight.Text;
            _n64Controls.DUp = BindDUp.Text;
            _n64Controls.DDown = BindDDown.Text;
            _n64Controls.DLeft = BindDLeft.Text;
            _n64Controls.DRight = BindDRight.Text;
        }

        private void BtnSaveControls_Click(object sender, RoutedEventArgs e)
        {
            switch (_currentSystem)
            {
                case "NES":
                    SaveNESControlsFromUI();
                    _nesControls.Save();
                    var nesDir = FindEmulatorExeDir("NES");
                    if (nesDir != null) _nesControls.ApplyToFCEUX(nesDir);
                    MessageBox.Show("Controls saved!", "Saved");
                    MessageBox.Show($"NES dir: {nesDir ?? "NULL"}", "Debug");
                    if (nesDir != null)
                    {
                        var cfgPath = System.IO.Path.Combine(nesDir, "fceux.cfg");
                        MessageBox.Show($"CFG exists: {System.IO.File.Exists(cfgPath)}\nCFG path: {cfgPath}", "Debug");
                        _nesControls.ApplyToFCEUX(nesDir);
                        MessageBox.Show("ApplyToFCEUX complete", "Debug");
                    }
                    break;
                case "SNES":
                    SaveSNESControlsFromUI();
                    _snesControls.Save();
                    var snesCfgDir = FindEmulatorConfigDir("SNES");
                    if (snesCfgDir != null) _snesControls.ApplyToSnes9x(snesCfgDir);
                    break;
                case "N64":
                    SaveControlsFromUI();
                    _n64Controls.Save();
                    var mupenDir = FindMupenConfigDir();
                    if (mupenDir != null) _n64Controls.ApplyToMupen(mupenDir);
                    break;
                case "GameCube":
                case "Wii":
                    SaveDolphinControlsFromUI();
                    _dolphinControls.Save();
                    var dolphinDir = FindEmulatorConfigDir("Dolphin");
                    MessageBox.Show($"Dolphin config: {dolphinDir ?? "NULL"}", "Debug");
                    if (dolphinDir != null) _dolphinControls.ApplyToDolphin(dolphinDir);
                    break;
            }
            MessageBox.Show("Controls saved!", "Saved");
        }

        private string? FindEmulatorExeDir(string system)
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                string candidate = system switch
                {
                    "NES" => System.IO.Path.Combine(dir.FullName, "ECores", "NES", "FCEUX", "fceux64.exe"),
                    "SNES" => System.IO.Path.Combine(dir.FullName, "ECores", "SNES", "Snes9x", "snes9x.exe"),
                    _ => ""
                };

                if (File.Exists(candidate))
                    return System.IO.Path.GetDirectoryName(candidate);

                dir = dir.Parent;
            }
            return null;
        }

        private string? FindEmulatorConfigDir(string system)
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            if (system == "Dolphin")
            {
                var d = new DirectoryInfo(AppContext.BaseDirectory);
                while (d != null)
                {
                    var path = System.IO.Path.Combine(d.FullName, "ECores", "GC-WII", "Dolphin-x64", "User", "Config");
                    if (Directory.Exists(path)) return path;
                    d = d.Parent;
                }

                // Documents fallback
                var localDocs = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Documents", "Dolphin Emulator", "Config");
                if (Directory.Exists(localDocs)) return localDocs;

                var docsPath = System.IO.Path.Combine(docs, "Dolphin Emulator", "Config");
                if (Directory.Exists(docsPath)) return docsPath;

                return null;
            }

            return system switch
            {
                "NES" => System.IO.Path.Combine(AppContext.BaseDirectory, "ECores", "NES", "FCEUX"),
                "SNES" => System.IO.Path.Combine(docs, "Snes9x"),
                _ => null
            };
        }

        private void BtnResetControls_Click(object sender, RoutedEventArgs e)
        {
            switch (_currentSystem)
            {
                case "NES":
                    _nesControls = new NESEmuInp();
                    LoadNESControlsIntoUI();
                    break;
                case "SNES":
                    _snesControls = new SNESEmuInp();
                    LoadSNESControlsIntoUI();
                    break;
                case "N64":
                    _n64Controls = new N64EmuInp();
                    LoadControlsIntoUI();
                    break;
                case "GameCube":
                case "Wii":
                    _dolphinControls = new DolphinEmuInp();
                    LoadDolphinControlsIntoUI();
                    break;
            }
        }

        // ── Key binding capture ──────────────────────────────────────────────────

        private void BindBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox tb) return;
            _activeBindBox = tb;
            tb.Text = "Press a key…";
            tb.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#00F0FF"));
        }

        private void BindBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBox tb) return;
            _activeBindBox = null;
            tb.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#9090B0"));
        }

        private void BindBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox tb) return;
            e.Handled = true;

            // Convert WPF key to a clean name
            var keyName = ConvertKeyToName(e.Key);
            if (string.IsNullOrEmpty(keyName)) return;

            tb.Text = keyName;
            tb.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#9090B0"));

            
        }

        private void ToggleNunchuck_Changed(object sender, RoutedEventArgs e)
        {
            _dolphinControls.UseNunchuck = ToggleNunchuck.IsChecked == true;
            if (NunchuckSection == null) return;
            NunchuckSection.Visibility = _dolphinControls.UseNunchuck
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private static string ConvertKeyToName(Key key) => key switch
        {
            Key.A => "A",
            Key.B => "B",
            Key.C => "C",
            Key.D => "D",
            Key.E => "E",
            Key.F => "F",
            Key.G => "G",
            Key.H => "H",
            Key.I => "I",
            Key.J => "J",
            Key.K => "K",
            Key.L => "L",
            Key.M => "M",
            Key.N => "N",
            Key.O => "O",
            Key.P => "P",
            Key.Q => "Q",
            Key.R => "R",
            Key.S => "S",
            Key.T => "T",
            Key.U => "U",
            Key.V => "V",
            Key.W => "W",
            Key.X => "X",
            Key.Y => "Y",
            Key.Z => "Z",
            Key.Up => "Up",
            Key.Down => "Down",
            Key.Left => "Left",
            Key.Right => "Right",
            Key.Enter => "Enter",
            Key.Space => "Space",
            Key.LeftShift => "LeftShift",
            Key.RightShift => "RightShift",
            Key.LeftCtrl => "LeftCtrl",
            Key.RightCtrl => "RightCtrl",
            Key.LeftAlt => "LeftAlt",
            Key.Tab => "Tab",
            Key.Back => "Back",
            Key.F1 => "F1",
            Key.F2 => "F2",
            Key.F3 => "F3",
            Key.F4 => "F4",
            Key.F5 => "F5",
            _ => ""
        };

        private void ToggleController_Changed(object sender, RoutedEventArgs e)
        {
            _n64Controls.UseController = ToggleController.IsChecked == true;
        }

        // ── Find Mupen config directory ──────────────────────────────────────────

        private string? FindMupenConfigDir()
        {
            // Mupen64Plus stores config in AppData\Roaming\mupen64plus (lowercase)
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var mupen = System.IO.Path.Combine(appData, "mupen64plus");
            if (Directory.Exists(mupen)) return mupen;

            // Fallback — exe folder
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = System.IO.Path.Combine(dir.FullName, "ECores", "N64", "MP64", "Release");
                if (Directory.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }
            return null;
        }

        public class Mupen64Plus
        {
            [DllImport("mupen64plus.dll")]
            static extern int CoreStartup(
                int apiVersion,
                int coreVersion,
                string name,
                string configDir,
                string dataDir,
                string context,
                IntPtr debugCallback,
                IntPtr contextPtr
            );
        }

        // ── NES ──────────────────────────────────────────────────────────────────

        private void LoadNESControlsIntoUI()
        {
            N64ControlsPanel.Visibility = Visibility.Collapsed;
            NESControlsPanel.Visibility = Visibility.Visible;
            SNESControlsPanel.Visibility = Visibility.Collapsed;
            DolphinControlsPanel.Visibility = Visibility.Collapsed;

            NESBindUp.Text = _nesControls.Up;
            NESBindDown.Text = _nesControls.Down;
            NESBindLeft.Text = _nesControls.Left;
            NESBindRight.Text = _nesControls.Right;
            NESBindA.Text = _nesControls.A;
            NESBindB.Text = _nesControls.B;
            NESBindStart.Text = _nesControls.Start;
            NESBindSelect.Text = _nesControls.Select;
        }

        private void SaveNESControlsFromUI()
        {
            _nesControls.Up = NESBindUp.Text;
            _nesControls.Down = NESBindDown.Text;
            _nesControls.Left = NESBindLeft.Text;
            _nesControls.Right = NESBindRight.Text;
            _nesControls.A = NESBindA.Text;
            _nesControls.B = NESBindB.Text;
            _nesControls.Start = NESBindStart.Text;
            _nesControls.Select = NESBindSelect.Text;
        }

        // ── SNES ─────────────────────────────────────────────────────────────────

        private void LoadSNESControlsIntoUI()
        {
            N64ControlsPanel.Visibility = Visibility.Collapsed;
            NESControlsPanel.Visibility = Visibility.Collapsed;
            SNESControlsPanel.Visibility = Visibility.Visible;
            DolphinControlsPanel.Visibility = Visibility.Collapsed;

            SNESBindUp.Text = _snesControls.Up;
            SNESBindDown.Text = _snesControls.Down;
            SNESBindLeft.Text = _snesControls.Left;
            SNESBindRight.Text = _snesControls.Right;
            SNESBindA.Text = _snesControls.A;
            SNESBindB.Text = _snesControls.B;
            SNESBindX.Text = _snesControls.X;
            SNESBindY.Text = _snesControls.Y;
            SNESBindL.Text = _snesControls.L;
            SNESBindR.Text = _snesControls.R;
            SNESBindStart.Text = _snesControls.Start;
            SNESBindSelect.Text = _snesControls.Select;
        }

        private void SaveSNESControlsFromUI()
        {
            _snesControls.Up = SNESBindUp.Text;
            _snesControls.Down = SNESBindDown.Text;
            _snesControls.Left = SNESBindLeft.Text;
            _snesControls.Right = SNESBindRight.Text;
            _snesControls.A = SNESBindA.Text;
            _snesControls.B = SNESBindB.Text;
            _snesControls.X = SNESBindX.Text;
            _snesControls.Y = SNESBindY.Text;
            _snesControls.L = SNESBindL.Text;
            _snesControls.R = SNESBindR.Text;
            _snesControls.Start = SNESBindStart.Text;
            _snesControls.Select = SNESBindSelect.Text;
        }

        // ── Dolphin ───────────────────────────────────────────────────────────────

        private void LoadDolphinControlsIntoUI()
        {
            N64ControlsPanel.Visibility = Visibility.Collapsed;
            NESControlsPanel.Visibility = Visibility.Collapsed;
            SNESControlsPanel.Visibility = Visibility.Collapsed;
            DolphinControlsPanel.Visibility = Visibility.Visible;

            GCBindStickUp.Text = _dolphinControls.GCStickUp;
            GCBindStickDown.Text = _dolphinControls.GCStickDown;
            GCBindStickLeft.Text = _dolphinControls.GCStickLeft;
            GCBindStickRight.Text = _dolphinControls.GCStickRight;
            GCBindA.Text = _dolphinControls.GCA;
            GCBindB.Text = _dolphinControls.GCB;
            GCBindX.Text = _dolphinControls.GCX;
            GCBindY.Text = _dolphinControls.GCY;
            GCBindZ.Text = _dolphinControls.GCZ;
            GCBindL.Text = _dolphinControls.GCL;
            GCBindR.Text = _dolphinControls.GCR;
            GCBindStart.Text = _dolphinControls.GCStart;

            WiiBindUp.Text = _dolphinControls.WiiUp;
            WiiBindDown.Text = _dolphinControls.WiiDown;
            WiiBindLeft.Text = _dolphinControls.WiiLeft;
            WiiBindRight.Text = _dolphinControls.WiiRight;
            WiiBindA.Text = _dolphinControls.WiiA;
            WiiBindB.Text = _dolphinControls.WiiB;
            WiiBind1.Text = _dolphinControls.Wii1;
            WiiBind2.Text = _dolphinControls.Wii2;
            WiiBindPlus.Text = _dolphinControls.WiiPlus;
            WiiBindMinus.Text = _dolphinControls.WiiMinus;
            WiiBindHome.Text = _dolphinControls.WiiHome;
            NunchuckBindZ.Text = _dolphinControls.NunchuckZ;
            NunchuckBindUp.Text = _dolphinControls.NunchuckUp;
            NunchuckBindDown.Text = _dolphinControls.NunchuckDown;
            NunchuckBindLeft.Text = _dolphinControls.NunchuckLeft;
            NunchuckBindRight.Text = _dolphinControls.NunchuckRight;
        }

        private void SaveDolphinControlsFromUI()
        {
            _dolphinControls.GCStickUp = GCBindStickUp.Text;
            _dolphinControls.GCStickDown = GCBindStickDown.Text;
            _dolphinControls.GCStickLeft = GCBindStickLeft.Text;
            _dolphinControls.GCStickRight = GCBindStickRight.Text;
            _dolphinControls.GCA = GCBindA.Text;
            _dolphinControls.GCB = GCBindB.Text;
            _dolphinControls.GCX = GCBindX.Text;
            _dolphinControls.GCY = GCBindY.Text;
            _dolphinControls.GCZ = GCBindZ.Text;
            _dolphinControls.GCL = GCBindL.Text;
            _dolphinControls.GCR = GCBindR.Text;
            _dolphinControls.GCStart = GCBindStart.Text;


            ToggleNunchuck.IsChecked = _dolphinControls.UseNunchuck;
            NunchuckSection.Visibility = _dolphinControls.UseNunchuck
                ? Visibility.Visible : Visibility.Collapsed;
            _dolphinControls.WiiUp = WiiBindUp.Text;
            _dolphinControls.WiiDown = WiiBindDown.Text;
            _dolphinControls.WiiLeft = WiiBindLeft.Text;
            _dolphinControls.WiiRight = WiiBindRight.Text;
            _dolphinControls.WiiA = WiiBindA.Text;
            _dolphinControls.WiiB = WiiBindB.Text;
            _dolphinControls.Wii1 = WiiBind1.Text;
            _dolphinControls.Wii2 = WiiBind2.Text;
            _dolphinControls.WiiPlus = WiiBindPlus.Text;
            _dolphinControls.WiiMinus = WiiBindMinus.Text;
            _dolphinControls.WiiHome = WiiBindHome.Text;
            NunchuckBindZ.Text = _dolphinControls.NunchuckZ;
            NunchuckBindUp.Text = _dolphinControls.NunchuckUp;
            NunchuckBindDown.Text = _dolphinControls.NunchuckDown;
            NunchuckBindLeft.Text = _dolphinControls.NunchuckLeft;
            NunchuckBindRight.Text = _dolphinControls.NunchuckRight;
        }

        // Ensure this method exists — populates GameGrid from _gamesFolder.

    }
}