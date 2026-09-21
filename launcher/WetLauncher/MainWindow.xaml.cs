using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Wet.Launcher
{
    public partial class MainWindow : Window
    {
        private readonly string _root;
        private bool _importing;
        private bool _dragging;
        private Point _dragCursor;
        private Point _dragWindow;

        public MainWindow()
        {
            InitializeComponent();
            _root = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            PopulateSettings();
            WireKeybindMouse();
            RefreshStatus();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            BeginWindowDrag(e);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (IsInteractive(e.OriginalSource as DependencyObject))
                return;
            BeginWindowDrag(e);
        }

        private void BeginWindowDrag(MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;
            if (IsInteractive(e.OriginalSource as DependencyObject))
                return;

            _dragging = true;
            _dragCursor = PointToScreen(e.GetPosition(this));
            _dragWindow = new Point(Left, Top);
            CaptureMouse();
            e.Handled = true;
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragging || e.LeftButton != MouseButtonState.Pressed)
                return;

            Point cursor = PointToScreen(e.GetPosition(this));
            Matrix toDip = GetDeviceToDip();
            Vector delta = toDip.Transform(cursor) - toDip.Transform(_dragCursor);
            Left = _dragWindow.X + delta.X;
            Top = _dragWindow.Y + delta.Y;
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            EndWindowDrag();
        }

        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            EndWindowDrag();
            base.OnLostMouseCapture(e);
        }

        private void EndWindowDrag()
        {
            if (!_dragging)
                return;
            _dragging = false;
            if (IsMouseCaptured)
                ReleaseMouseCapture();
        }

        private static Matrix GetDeviceToDip()
        {
            var source = PresentationSource.FromVisual(Application.Current.MainWindow);
            if (source != null && source.CompositionTarget != null)
                return source.CompositionTarget.TransformFromDevice;
            return Matrix.Identity;
        }

        private static bool IsInteractive(DependencyObject source)
        {
            while (source != null)
            {
                var element = source as FrameworkElement;
                if (element != null && (element.Name == "MinimizeLabel" || element.Name == "CloseLabel"))
                    return true;
                if (source is Button || source is TextBox || source is ComboBox ||
                    source is ComboBoxItem || source is CheckBox || source is TabItem ||
                    source is System.Windows.Controls.Primitives.ScrollBar)
                    return true;
                source = VisualTreeHelper.GetParent(source);
            }
            return false;
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void MinimizeLabel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            EndWindowDrag();
            WindowState = WindowState.Minimized;
        }

        private void CloseLabel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            EndWindowDrag();
            Close();
        }

        private string GameDir
        {
            get { return Path.Combine(FindProjectRoot(), "game"); }
        }

        private string FindProjectRoot()
        {
            string dir = _root;
            for (int i = 0; i < 8 && !string.IsNullOrEmpty(dir); i++)
            {
                if (File.Exists(Path.Combine(dir, "CMakeLists.txt")) &&
                    Directory.Exists(Path.Combine(dir, "game")))
                    return dir;
                dir = Path.GetDirectoryName(dir);
            }
            return _root;
        }

        private void PopulateSettings()
        {
            double hz = DisplayRefresh.DetectHz();
            FrameRateCombo.ItemsSource = new List<string>
            {
                string.Format(CultureInfo.InvariantCulture, "Match Display ({0:0} Hz)", hz),
                "60 Hz",
                "120 Hz",
                "144 Hz",
                "Unlocked"
            };
            FrameRateCombo.SelectedIndex = 0;

            ResolutionCombo.ItemsSource = new List<string>
            {
                "Auto (match monitor)",
                "1080p (1920 x 1080)",
                "2K (2560 x 1440)",
                "4K (3840 x 2160)"
            };
            ResolutionCombo.SelectedIndex = 1;

            QualityCombo.ItemsSource = new List<string>
            {
                "Performance",
                "Balanced (recommended)",
                "Quality",
                "Ultra"
            };
            QualityCombo.SelectedIndex = 1;

            AACombo.ItemsSource = new List<string> { "Off", "FXAA", "FXAA Extreme" };
            AACombo.SelectedIndex = 1;
            AnisoCombo.ItemsSource = new List<string> { "Default", "4x", "8x", "16x" };
            AnisoCombo.SelectedIndex = 3;
            PostCombo.ItemsSource = new List<string> { "Bilinear", "Sharpen" };
            PostCombo.SelectedIndex = 0;

            SensitivityCombo.ItemsSource = new List<string>
            {
                "1.0", "1.5", "2.0 (recommended)", "2.5", "3.0", "4.0", "5.0", "6.0", "8.0"
            };
            SensitivityCombo.SelectedIndex = 2;

            ResetKeybinds();
        }

        private void QualityCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AACombo == null || AnisoCombo == null || PostCombo == null) return;
            int quality = QualityCombo.SelectedIndex;
            if (quality <= 0)
            {
                AACombo.SelectedIndex = 0;
                AnisoCombo.SelectedIndex = 1;
                PostCombo.SelectedIndex = 0;
            }
            else if (quality == 1)
            {
                AACombo.SelectedIndex = 1;
                AnisoCombo.SelectedIndex = 3;
                PostCombo.SelectedIndex = 0;
            }
            else if (quality == 2)
            {
                AACombo.SelectedIndex = 2;
                AnisoCombo.SelectedIndex = 3;
                PostCombo.SelectedIndex = 1;
            }
            else
            {
                AACombo.SelectedIndex = 2;
                AnisoCombo.SelectedIndex = 3;
                PostCombo.SelectedIndex = 1;
            }
        }

        private void ResetKeybinds_Click(object sender, RoutedEventArgs e)
        {
            ResetKeybinds();
        }

        private void ResetKeybinds()
        {
            if (KeyA == null) return;
            KeyA.Text = "Space";
            KeyB.Text = "F";
            KeyX.Text = "C";
            KeyY.Text = "E";
            KeyLS.Text = "Q";
            KeyRS.Text = "RMB";
            KeyLT.Text = "LMB";
            KeyRT.Text = "LMB";
            KeyLUp.Text = "W";
            KeyLDown.Text = "S";
            KeyLLeft.Text = "A";
            KeyLRight.Text = "D";
            KeyLPress.Text = "X";
            KeyRUp.Text = "Up";
            KeyRDown.Text = "Down";
            KeyRLeft.Text = "Left";
            KeyRRight.Text = "Right";
            KeyRPress.Text = "R";
            KeyStart.Text = "Escape";
            KeyBack.Text = "Tab";
            KeyDUp.Text = "Shift+Up";
            KeyDDown.Text = "Shift+Down";
            KeyDLeft.Text = "Shift+Left";
            KeyDRight.Text = "Shift+Right";
        }

        private void WireKeybindMouse()
        {
            var boxes = new[]
            {
                KeyA, KeyB, KeyX, KeyY, KeyLS, KeyRS, KeyLT, KeyRT,
                KeyLUp, KeyLDown, KeyLLeft, KeyLRight, KeyLPress,
                KeyRUp, KeyRDown, KeyRLeft, KeyRRight, KeyRPress,
                KeyStart, KeyBack, KeyDUp, KeyDDown, KeyDLeft, KeyDRight
            };
            foreach (var box in boxes)
            {
                if (box != null)
                    box.PreviewMouseDown += KeyField_PreviewMouseDown;
            }
        }

        private void KeyField_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var box = sender as TextBox;
            if (box == null || !box.IsKeyboardFocusWithin)
                return;
            string name = null;
            if (e.ChangedButton == MouseButton.Left) name = "LMB";
            else if (e.ChangedButton == MouseButton.Right) name = "RMB";
            else if (e.ChangedButton == MouseButton.Middle) name = "MMB";
            if (name == null)
                return;
            box.Text = name;
            e.Handled = true;
        }

        private void KeyField_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            var box = sender as TextBox;
            if (box == null) return;
            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            string name = KeyName(key);
            if (name == null) return;
            if (name == "Shift" || name == "Control" || name == "Alt")
            {
                box.Text = name;
                return;
            }
            string prefix = "";
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && name != "Shift") prefix += "Shift+";
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && name != "Control") prefix += "Control+";
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt) && name != "Alt") prefix += "Alt+";
            box.Text = prefix + name;
        }

        private static string KeyName(Key key)
        {
            if (key == Key.LeftShift || key == Key.RightShift) return "Shift";
            if (key == Key.LeftCtrl || key == Key.RightCtrl) return "Control";
            if (key == Key.LeftAlt || key == Key.RightAlt) return "Alt";
            if (key == Key.Space) return "Space";
            if (key == Key.Escape) return "Escape";
            if (key == Key.Tab) return "Tab";
            if (key == Key.Up) return "Up";
            if (key == Key.Down) return "Down";
            if (key == Key.Left) return "Left";
            if (key == Key.Right) return "Right";
            if (key >= Key.A && key <= Key.Z) return key.ToString();
            if (key >= Key.D0 && key <= Key.D9) return ((int)(key - Key.D0)).ToString();
            if (key == Key.OemMinus) return "-";
            if (key == Key.OemPlus) return "=";
            return null;
        }

        private static string Quote(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Space";
            string trimmed = value.Trim();
            if (trimmed.IndexOf(' ') >= 0 || trimmed.IndexOf('+') >= 0)
                return "\"" + trimmed + "\"";
            return trimmed;
        }

        private void RefreshStatus()
        {
            bool hasIso = IsoImporter.HasGameFiles(GameDir);
            bool hasExe = File.Exists(FindGameExe());

            if (!hasIso)
            {
                SetStatus("WAITING FOR ISO", "#D4B06A");
                StatusText.Text = "Add your WET Xbox 360 ISO to begin.";
                NoteText.Text = "Click ADD ISO and pick a .iso dumped from a disc you own.";
                PlayButton.IsEnabled = false;
                return;
            }

            if (!hasExe)
            {
                SetStatus("NEEDS BUILD", "#E6A23C");
                StatusText.Text = "ISO imported. Click PLAY to build and start WET.";
                NoteText.Text = "First launch compiles the PC port. That can take a while.";
                PlayButton.IsEnabled = true;
                return;
            }

            SetStatus("READY TO PLAY", "#3DDC84");
            StatusText.Text = "WET is installed and ready to launch.";
            NoteText.Text = "Game files and wet.exe found.";
            PlayButton.IsEnabled = true;
        }

        private void SetStatus(string label, string color)
        {
            if (StatusBadge != null)
                StatusBadge.Text = label;
            if (StatusDot != null)
            {
                var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
                StatusDot.Fill = brush;
                if (StatusBadge != null)
                    StatusBadge.Foreground = brush;
            }
        }

        private string FindGameExe()
        {
            string root = FindProjectRoot();
            var candidates = new[]
            {
                Path.Combine(root, "wet.exe"),
                Path.Combine(root, "out", "build", "win-amd64-release", "wet.exe"),
                Path.Combine(_root, "wet.exe"),
            };
            foreach (var path in candidates)
            {
                if (File.Exists(path))
                    return path;
            }
            return Path.Combine(root, "wet.exe");
        }

        private void ImportIso_Click(object sender, RoutedEventArgs e)
        {
            if (_importing) return;

            if (IsoImporter.FindExtractor(_root) == null)
            {
                MessageBox.Show(
                    "extract-xiso.exe was not found next to the launcher.",
                    "WET", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var dialog = new System.Windows.Forms.OpenFileDialog
            {
                Filter = "Xbox 360 ISO (*.iso)|*.iso|All files (*.*)|*.*",
                Title = "Select WET Xbox 360 ISO",
            };
            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            if (IsoImporter.HasGameFiles(GameDir))
            {
                if (MessageBox.Show("Replace the existing game files?", "WET",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                    return;
            }

            _importing = true;
            ImportIsoButton.IsEnabled = false;
            PlayButton.IsEnabled = false;
            IsoProgress.Visibility = Visibility.Visible;
            NoteText.Text = "Extracting ISO...";

            string iso = dialog.FileName;
            string gameDir = GameDir;
            Task.Factory.StartNew(new Action(() =>
            {
                var result = IsoImporter.Extract(iso, gameDir, msg =>
                {
                    Dispatcher.Invoke(new Action(() => { NoteText.Text = msg; }));
                });

                Dispatcher.Invoke(new Action(() =>
                {
                    _importing = false;
                    ImportIsoButton.IsEnabled = true;
                    IsoProgress.Visibility = Visibility.Collapsed;
                    if (!result.Success)
                    {
                        MessageBox.Show("ISO import failed:\n" + result.Error, "WET",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else if (result.TitleId == 0x454108CF)
                    {
                        MessageBox.Show(
                            "That ISO is Dante's Inferno, not WET.\n\nUse this launcher with a WET Xbox 360 ISO.",
                            "Wrong game", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        MessageBox.Show(
                            "WET ISO imported.\n\nClick PLAY to build and start the game. The first compile can take a while.",
                            "WET", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    RefreshStatus();
                }));
            }));
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            string exe = FindGameExe();
            if (!File.Exists(exe))
            {
                string root = FindProjectRoot();
                string buildScript = Path.Combine(root, "build.ps1");
                if (!File.Exists(buildScript))
                {
                    MessageBox.Show("WET is imported, but the PC build is not ready yet.\n\nWait for the first compile to finish, then click PLAY again.",
                        "WET", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                PlayButton.IsEnabled = false;
                StatusText.Text = "Building the PC port...";
                NoteText.Text = "Compiling wet.exe. Keep this window open.";
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = "-NoProfile -ExecutionPolicy Bypass -File \"" + buildScript + "\"",
                        WorkingDirectory = root,
                        UseShellExecute = true,
                    });
                }
                catch (Exception ex)
                {
                    PlayButton.IsEnabled = true;
                    MessageBox.Show("Could not start the build:\n" + ex.Message, "WET",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                return;
            }

            string gameDir = GameDir;
            var args = new List<string>();
            args.Add("--game_data_root=\"" + gameDir + "\"");
            args.Add("--input_backend=" + ((SdlControllerCheck.IsChecked ?? true) ? "sdl" : "xinput"));
            args.Add("--fullscreen=" + ((FullscreenCheck.IsChecked ?? true) ? "true" : "false"));
            args.Add("--show_fps_overlay=" + ((FpsOverlayCheck.IsChecked ?? false) ? "true" : "false"));

            int res = ResolutionCombo.SelectedIndex;
            string resFlag = null;
            if (res == 0) resFlag = DisplayRefresh.AutoResolutionFlag();
            else if (res == 1) resFlag = "1080p";
            else if (res == 2) resFlag = "1440p";
            else if (res == 3) resFlag = "4k";
            if (resFlag == "1080p")
            {
                args.Add("--resolution=1080p");
                args.Add("--window_width=1920");
                args.Add("--window_height=1080");
                args.Add("--resolution_scale=1");
            }
            else if (resFlag == "1440p")
            {
                args.Add("--resolution=1440p");
                args.Add("--window_width=2560");
                args.Add("--window_height=1440");
                args.Add("--resolution_scale=2");
            }
            else if (resFlag == "4k")
            {
                args.Add("--resolution=4k");
                args.Add("--window_width=3840");
                args.Add("--window_height=2160");
                args.Add("--resolution_scale=3");
            }

            int fps = FrameRateCombo.SelectedIndex;
            string mode = "auto";
            if (fps == 1) mode = "60";
            else if (fps == 2) mode = "120";
            else if (fps == 3) mode = "144";
            else if (fps == 4) mode = "unlocked";
            args.Add("--framerate_mode=" + mode);

            bool vsync = (VSyncCheck.IsChecked ?? true) && mode != "unlocked";
            if (mode == "auto") vsync = true;
            args.Add("--vsync=" + (vsync ? "true" : "false"));
            args.Add("--d3d12_host_vsync=" + (vsync ? "true" : "false"));

            double hz = DisplayRefresh.Resolve(mode);
            args.Add("--video_mode_refresh_rate=" + hz.ToString("0", CultureInfo.InvariantCulture));
            args.Add("--target_fps=" + (mode == "unlocked" ? "0" : hz.ToString("0", CultureInfo.InvariantCulture)));
            args.Add("--d3d12_allow_variable_refresh_rate_and_tearing=true");

            string[] aa = { "none", "fxaa", "fxaa_extreme" };
            string[] aniso = { "-1", "4", "8", "16" };
            int aaIndex = Math.Max(0, AACombo.SelectedIndex);
            int anisoIndex = Math.Max(0, AnisoCombo.SelectedIndex);
            args.Add("--swap_post_effect=" + aa[Math.Min(aaIndex, aa.Length - 1)]);
            args.Add("--anisotropic_override=" + aniso[Math.Min(anisoIndex, aniso.Length - 1)]);
            args.Add("--present_effect=bilinear");
            if (PostCombo.SelectedIndex > 0)
                args.Add("--present_dither=true");

            args.Add("--mnk_mode=" + ((MnkCheck.IsChecked ?? true) ? "true" : "false"));
            args.Add("--mnk_mouse=" + ((MouseLookCheck.IsChecked ?? true) ? "true" : "false"));
            string[] sens = { "1.0", "1.5", "2.0", "2.5", "3.0", "4.0", "5.0", "6.0", "8.0" };
            int sensIndex = Math.Max(0, SensitivityCombo.SelectedIndex);
            args.Add("--mnk_sensitivity=" + sens[Math.Min(sensIndex, sens.Length - 1)]);

            args.Add("--keybind_a=" + Quote(KeyA.Text));
            args.Add("--keybind_b=" + Quote(KeyB.Text));
            args.Add("--keybind_x=" + Quote(KeyX.Text));
            args.Add("--keybind_y=" + Quote(KeyY.Text));
            args.Add("--keybind_left_shoulder=" + Quote(KeyLS.Text));
            args.Add("--keybind_right_shoulder=" + Quote(KeyRS.Text));
            args.Add("--keybind_left_trigger=" + Quote(KeyLT.Text));
            args.Add("--keybind_right_trigger=" + Quote(KeyRT.Text));
            args.Add("--keybind_lstick_up=" + Quote(KeyLUp.Text));
            args.Add("--keybind_lstick_down=" + Quote(KeyLDown.Text));
            args.Add("--keybind_lstick_left=" + Quote(KeyLLeft.Text));
            args.Add("--keybind_lstick_right=" + Quote(KeyLRight.Text));
            args.Add("--keybind_lstick_press=" + Quote(KeyLPress.Text));
            args.Add("--keybind_rstick_up=" + Quote(KeyRUp.Text));
            args.Add("--keybind_rstick_down=" + Quote(KeyRDown.Text));
            args.Add("--keybind_rstick_left=" + Quote(KeyRLeft.Text));
            args.Add("--keybind_rstick_right=" + Quote(KeyRRight.Text));
            args.Add("--keybind_rstick_press=" + Quote(KeyRPress.Text));
            args.Add("--keybind_dpad_up=" + Quote(KeyDUp.Text));
            args.Add("--keybind_dpad_down=" + Quote(KeyDDown.Text));
            args.Add("--keybind_dpad_left=" + Quote(KeyDLeft.Text));
            args.Add("--keybind_dpad_right=" + Quote(KeyDRight.Text));
            args.Add("--keybind_back=" + Quote(KeyBack.Text));
            args.Add("--keybind_start=" + Quote(KeyStart.Text));

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = string.Join(" ", args),
                    WorkingDirectory = Path.GetDirectoryName(exe),
                    UseShellExecute = false,
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not start WET:\n" + ex.Message, "WET",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public static class DisplayRefresh
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DevMode mode);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct DevMode
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
        }

        public static double DetectHz()
        {
            try
            {
                var mode = new DevMode();
                mode.dmSize = (short)Marshal.SizeOf(typeof(DevMode));
                if (EnumDisplaySettings(null, -1, ref mode) &&
                    mode.dmDisplayFrequency > 1 && mode.dmDisplayFrequency < 1000)
                    return mode.dmDisplayFrequency;
            }
            catch
            {
            }
            return 60;
        }

        public static void DetectSize(out int width, out int height)
        {
            width = 1920;
            height = 1080;
            try
            {
                var mode = new DevMode();
                mode.dmSize = (short)Marshal.SizeOf(typeof(DevMode));
                if (EnumDisplaySettings(null, -1, ref mode))
                {
                    if (mode.dmPelsWidth > 0) width = mode.dmPelsWidth;
                    if (mode.dmPelsHeight > 0) height = mode.dmPelsHeight;
                }
            }
            catch
            {
            }
        }

        public static string AutoResolutionFlag()
        {
            int width, height;
            DetectSize(out width, out height);
            if (height >= 2000 || width >= 3200) return "4k";
            if (height >= 1300 || width >= 2400) return "1440p";
            return "1080p";
        }

        public static double Resolve(string mode)
        {
            if (mode == "60") return 60;
            if (mode == "120") return 120;
            if (mode == "144") return 144;
            return DetectHz();
        }
    }

    public class IsoImportResult
    {
        public bool Success;
        public string Error;
        public uint TitleId;
    }

    public static class IsoImporter
    {
        public static bool HasGameFiles(string gameDirectory)
        {
            return !string.IsNullOrEmpty(FindXex(gameDirectory));
        }

        public static string FindXex(string root)
        {
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                return null;
            string direct = Path.Combine(root, "default.xex");
            if (File.Exists(direct))
                return direct;
            try
            {
                foreach (var file in Directory.GetFiles(root, "default.xex", SearchOption.AllDirectories))
                    return file;
            }
            catch
            {
            }
            return null;
        }

        public static string FindExtractor(string installDirectory)
        {
            var paths = new[]
            {
                Path.Combine(installDirectory, "extract-xiso.exe"),
                Path.Combine(installDirectory, "..", "..", "..", "tools", "extract-xiso", "extract-xiso.exe"),
            };
            foreach (var path in paths)
            {
                string full = Path.GetFullPath(path);
                if (File.Exists(full))
                    return full;
            }
            return null;
        }

        public static IsoImportResult Extract(string isoPath, string gameDirectory, Action<string> progress)
        {
            var result = new IsoImportResult();
            try
            {
                string extractor = FindExtractor(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
                if (extractor == null)
                {
                    result.Error = "extract-xiso.exe not found.";
                    return result;
                }

                Directory.CreateDirectory(Path.GetDirectoryName(gameDirectory) ?? gameDirectory);
                string temp = gameDirectory + ".tmp-" + Guid.NewGuid().ToString("N");
                Directory.CreateDirectory(temp);

                if (progress != null)
                    progress("Extracting ISO...");

                var psi = new ProcessStartInfo
                {
                    FileName = extractor,
                    Arguments = "-x -d \"" + temp + "\" -s \"" + isoPath + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };

                using (var process = Process.Start(psi))
                {
                    process.OutputDataReceived += (s, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data) && progress != null)
                            progress(e.Data);
                    };
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        TryDelete(temp);
                        result.Error = "extract-xiso failed (" + process.ExitCode + ").";
                        return result;
                    }
                }

                string xex = FindXex(temp);
                if (xex == null)
                {
                    TryDelete(temp);
                    result.Error = "No default.xex in that ISO.";
                    return result;
                }

                string xexDir = Path.GetDirectoryName(xex);
                if (!string.Equals(xexDir, temp, StringComparison.OrdinalIgnoreCase))
                {
                    string flat = temp + "-flat";
                    Directory.Move(xexDir, flat);
                    TryDelete(temp);
                    temp = flat;
                    xex = Path.Combine(temp, "default.xex");
                }

                result.TitleId = ReadTitleId(xex);

                if (Directory.Exists(gameDirectory))
                    TryDelete(gameDirectory);
                Directory.Move(temp, gameDirectory);
                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                return result;
            }
        }

        private static uint ReadTitleId(string xexPath)
        {
            try
            {
                using (var fs = File.OpenRead(xexPath))
                using (var reader = new BinaryReader(fs))
                {
                    if (ReadU32(reader) != 0x58455832)
                        return 0;
                    fs.Position = 0x14;
                    uint count = ReadU32(reader);
                    for (int i = 0; i < count && i < 64; i++)
                    {
                        fs.Position = 0x18 + i * 8;
                        uint key = ReadU32(reader);
                        uint value = ReadU32(reader);
                        if (key == 0x00040006 && value + 0x10 < fs.Length)
                        {
                            fs.Position = value + 0x0C;
                            return ReadU32(reader);
                        }
                    }
                }
            }
            catch
            {
            }
            return 0;
        }

        private static uint ReadU32(BinaryReader reader)
        {
            byte[] b = reader.ReadBytes(4);
            if (b.Length < 4) return 0;
            return (uint)((b[0] << 24) | (b[1] << 16) | (b[2] << 8) | b[3]);
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
            }
            catch
            {
            }
        }
    }
}
