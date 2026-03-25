using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using FlowWheel.Core;
using FlowWheel.UI.Controls;
using MediaColor = System.Windows.Media.Color;
using WpfApplication = System.Windows.Application;
using WpfMessageBox = System.Windows.MessageBox;
using Button = System.Windows.Controls.Button;
using Brush = System.Windows.Media.Brush;
using Point = System.Windows.Point;

namespace FlowWheel.UI
{
    public partial class SettingsWindow : Window
    {
        private readonly ScrollEngine _engine;
        private readonly AutoScrollManager _manager;
        private readonly MouseHook? _mouseHook;
        private readonly KeyboardHook? _keyboardHook;
        private WindowManager _windowManager;
        private bool _isDarkMode = false;
        private string _currentPage = "General";
        private bool _isNavigating = false;
        private bool _isListening = false;

        public SettingsWindow(ScrollEngine engine, AutoScrollManager manager, WindowManager windowManager, MouseHook? mouseHook = null, KeyboardHook? keyboardHook = null)
        {
            InitializeComponent();
            _engine = engine;
            _manager = manager;
            _windowManager = windowManager;
            _mouseHook = mouseHook;
            _keyboardHook = keyboardHook;

            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            if (version != null)
            {
                VersionText.Text = $"Version {version.Major}.{version.Minor}.{version.Build}";
                VersionLabel.Text = $"v{version.Major}.{version.Minor}.{version.Build}";
            }

            InitializeValues();
            SetupNavigation();
            SetupEventHandlers();
            SetupDarkMode();
            CheckUpdateOnStartup();
        }

        private void InitializeValues()
        {
            SpeedSlider.Value = ConfigManager.Current.Sensitivity;
            DeadzoneSlider.Value = ConfigManager.Current.Deadzone;
            
            // 独立灵敏度设置
            IndependentSensitivityToggle.IsOn = ConfigManager.Current.UseIndependentSensitivity;
            VerticalSpeedSlider.Value = ConfigManager.Current.SensitivityVertical;
            HorizontalSpeedSlider.Value = ConfigManager.Current.SensitivityHorizontal;
            UpdateSensitivityPanelVisibility();
            
            // 阅读模式设置
            ReadingSpeedSlider.Value = ConfigManager.Current.ReadingModeSpeed;
            ReadingMaxSpeedSlider.Value = ConfigManager.Current.ReadingModeMaxSpeed;
            
            // 加速度曲线设置
            foreach (ComboBoxItem item in AccelerationCurveCombo.Items)
            {
                if (item.Tag?.ToString() == ConfigManager.Current.AccelerationCurve.ToString())
                {
                    item.IsSelected = true;
                    break;
                }
            }
            ExponentSlider.Value = ConfigManager.Current.AccelerationExponent;
            LogBaseSlider.Value = ConfigManager.Current.AccelerationLogBase;
            SigmoidMidpointSlider.Value = ConfigManager.Current.SigmoidMidpoint;
            SigmoidSteepnessSlider.Value = ConfigManager.Current.SigmoidSteepness;
            
            if (CurveEditorControl != null)
            {
                CurveEditorControl.CurvePoints = ConfigManager.Current.CustomCurvePoints;
                CurveEditorControl.CurveType = ConfigManager.Current.AccelerationCurve;
                CurveEditorControl.Config = ConfigManager.Current;
            }
            
            // 高级参数设置
            AdvancedSettingsToggle.IsOn = ConfigManager.Current.ShowAdvancedSettings;
            FrictionSlider.Value = ConfigManager.Current.Friction;
            InertiaSlider.Value = ConfigManager.Current.InertiaMultiplier;
            ResponseTimeSlider.Value = ConfigManager.Current.ResponseTime * 1000;
            AxisLockSlider.Value = ConfigManager.Current.AxisLockRatio;
            SoftStartSlider.Value = ConfigManager.Current.SoftStartRange;
            UpdateAdvancedParamsVisibility();
            UpdateCurveParamsVisibility();
            
            EnableToggle.IsOn = ConfigManager.Current.IsEnabled;
            StartupToggle.IsOn = ConfigManager.Current.StartupEnabled;
            SyncToggle.IsOn = ConfigManager.Current.IsSyncScrollEnabled;
            ReadingModeToggle.IsOn = ConfigManager.Current.IsReadingModeEnabled;
            
            if (ConfigManager.Current.TriggerMode == "Hold")
                RadioHoldDrag.IsChecked = true;
            else
                RadioClickToggle.IsChecked = true;

            TriggerKeyInput.Text = ConfigManager.Current.TriggerKey;

            if (ConfigManager.Current.IsWhitelistMode)
            {
                RadioWhitelist.IsChecked = true;
                FilterModeHelpText.Text = "Only processes in this list will have auto-scroll enabled.";
            }
            else
            {
                RadioBlacklist.IsChecked = true;
                FilterModeHelpText.Text = "Processes in this list will be ignored (auto-scroll disabled).";
            }
            
            AppStatusToggle.IsOn = ConfigManager.Current.IsEnabled;

            foreach (ComboBoxItem item in LanguageCombo.Items)
            {
                if (item.Tag?.ToString() == ConfigManager.Current.Language)
                {
                    item.IsSelected = true;
                    break;
                }
            }

            foreach (ComboBoxItem item in PerformanceModeCombo.Items)
            {
                if (item.Tag?.ToString() == ConfigManager.Current.PerformanceMode.ToString())
                {
                    item.IsSelected = true;
                    break;
                }
            }

            CustomIconPathInput.Text = ConfigManager.Current.CustomIconPath;
            IconSizeSlider.Value = ConfigManager.Current.IconSize;

            RefreshBlacklist();
        }

        private void SetupNavigation()
        {
            NavGeneral.Checked += (s, e) => NavigateTo("General");
            NavBehavior.Checked += (s, e) => NavigateTo("Behavior");
            NavFilter.Checked += (s, e) => NavigateTo("Filter");
            NavShortcuts.Checked += (s, e) => NavigateTo("Shortcuts");
            NavAbout.Checked += (s, e) => NavigateTo("About");
        }

        private void SetupEventHandlers()
        {
            EnableToggle.IsOnChanged += (s, e) => 
            {
                ConfigManager.Current.IsEnabled = EnableToggle.IsOn;
                if (_manager != null) _manager.IsEnabled = EnableToggle.IsOn;
                ConfigManager.Save();
                // Sync with AppStatusToggle
                if (AppStatusToggle.IsOn != EnableToggle.IsOn)
                    AppStatusToggle.IsOn = EnableToggle.IsOn;
            };

            StartupToggle.IsOnChanged += (s, e) =>
            {
                ConfigManager.Current.StartupEnabled = StartupToggle.IsOn;
                SetStartup(StartupToggle.IsOn);
                ConfigManager.Save();
            };

            SyncToggle.IsOnChanged += (s, e) => 
            {
                ConfigManager.Current.IsSyncScrollEnabled = SyncToggle.IsOn;
                if (_engine != null) _engine.IsSyncEnabled = SyncToggle.IsOn;
                ConfigManager.Save();
            };

            ReadingModeToggle.IsOnChanged += (s, e) => 
            {
                ConfigManager.Current.IsReadingModeEnabled = ReadingModeToggle.IsOn;
                ConfigManager.Save();
            };

            AppStatusToggle.IsOnChanged += (s, e) =>
            {
                ConfigManager.Current.IsEnabled = AppStatusToggle.IsOn;
                if (_manager != null) _manager.IsEnabled = AppStatusToggle.IsOn;
                ConfigManager.Save();
                // Sync with EnableToggle
                if (EnableToggle.IsOn != AppStatusToggle.IsOn)
                    EnableToggle.IsOn = AppStatusToggle.IsOn;
            };

            RadioClickToggle.Checked += (s, e) => { ConfigManager.Current.TriggerMode = "Toggle"; ConfigManager.Save(); };
            RadioHoldDrag.Checked += (s, e) => { ConfigManager.Current.TriggerMode = "Hold"; ConfigManager.Save(); };

            RadioBlacklist.Checked += RadioFilterMode_Changed;
            RadioWhitelist.Checked += RadioFilterMode_Changed;
        }

        private void NavigateTo(string page)
        {
            if (PageGeneral == null || _isNavigating || _currentPage == page) return;
            
            _isNavigating = true;
            
            // 确定导航方向
            var pageOrder = new[] { "General", "Behavior", "Filter", "Shortcuts", "About" };
            int oldIndex = Array.IndexOf(pageOrder, _currentPage);
            int newIndex = Array.IndexOf(pageOrder, page);
            bool goingForward = newIndex > oldIndex;
            
            // 获取当前和目标页面
            var oldPage = GetPage(_currentPage);
            var newPage = GetPage(page);
            
            if (oldPage == null || newPage == null)
            {
                _isNavigating = false;
                return;
            }
            
            // 准备新页面位置
            var slideOffset = 50.0;
            if (goingForward)
            {
                newPage.RenderTransform = new TranslateTransform(slideOffset, 0);
                newPage.Opacity = 0;
            }
            else
            {
                newPage.RenderTransform = new TranslateTransform(-slideOffset, 0);
                newPage.Opacity = 0;
            }
            
            newPage.Visibility = Visibility.Visible;
            
            // 创建动画
            var duration = TimeSpan.FromMilliseconds(250);
            var ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            
            // 旧页面滑出动画
            var oldSlideOut = new DoubleAnimation
            {
                To = goingForward ? -slideOffset : slideOffset,
                Duration = duration,
                EasingFunction = ease
            };
            
            var oldFadeOut = new DoubleAnimation
            {
                To = 0,
                Duration = duration,
                EasingFunction = ease
            };
            
            // 新页面滑入动画
            var newSlideIn = new DoubleAnimation
            {
                To = 0,
                Duration = duration,
                EasingFunction = ease
            };
            
            var newFadeIn = new DoubleAnimation
            {
                To = 1,
                Duration = duration,
                EasingFunction = ease
            };
            
            // 动画完成后清理
            oldSlideOut.Completed += (s, e) =>
            {
                oldPage.Visibility = Visibility.Collapsed;
                oldPage.RenderTransform = new TranslateTransform(0, 0);
                oldPage.Opacity = 1;
                _isNavigating = false;
            };
            
            // 启动动画
            if (oldPage.RenderTransform is TranslateTransform oldTransform)
            {
                oldTransform.BeginAnimation(TranslateTransform.XProperty, oldSlideOut);
            }
            oldPage.BeginAnimation(OpacityProperty, oldFadeOut);
            
            if (newPage.RenderTransform is TranslateTransform newTransform)
            {
                newTransform.BeginAnimation(TranslateTransform.XProperty, newSlideIn);
            }
            newPage.BeginAnimation(OpacityProperty, newFadeIn);
            
            _currentPage = page;
            PageTitle.Text = FindResource($"Nav{page}") as string ?? page;
        }
        
        private StackPanel? GetPage(string page)
        {
            return page switch
            {
                "General" => PageGeneral,
                "Behavior" => PageBehavior,
                "Filter" => PageFilter,
                "Shortcuts" => PageShortcuts,
                "About" => PageAbout,
                _ => null
            };
        }

        private bool _isAnimating = false; // 防止重复点击

        private void SetupDarkMode()
        {
            _isDarkMode = ConfigManager.Current.IsDarkMode;
            DarkModeToggle.IsOn = _isDarkMode;
            
            // Start decoration animations
            StartCloudAnimations();
            StartStarTwinkleAnimations();
            
            // Apply initial decoration visibility
            UpdateDecorationVisibility(_isDarkMode, false);
            
            // Apply window frame theme
            ThemeManager.SetWindowFrameTheme(this, _isDarkMode);

            DarkModeToggle.IsOnChanged += (s, e) =>
            {
                if (_isAnimating) 
                {
                    DarkModeToggle.IsOn = _isDarkMode; // Restore state
                    return;
                }
                
                _isAnimating = true;
                bool newIsDark = DarkModeToggle.IsOn;
                
                // Get toggle position in window (center point)
                var togglePos = DarkModeToggle.TransformToAncestor(this).Transform(new Point(0, 0));
                var rippleX = togglePos.X + DarkModeToggle.ActualWidth / 2;
                var rippleY = togglePos.Y + DarkModeToggle.ActualHeight / 2;

                // Set water drop color
                var targetColor = newIsDark 
                    ? MediaColor.FromRgb(26, 26, 46) 
                    : MediaColor.FromRgb(245, 245, 247);
                WaterDrop.Fill = new SolidColorBrush(targetColor);

                // Calculate max radius needed to cover entire window
                double maxDistX = Math.Max(rippleX, ActualWidth - rippleX);
                double maxDistY = Math.Max(rippleY, ActualHeight - rippleY);
                double maxRadius = Math.Sqrt(maxDistX * maxDistX + maxDistY * maxDistY) * 2;

                if (newIsDark)
                {
                    // Light -> Dark: Expand from click point
                    DoExpandAnimation(rippleX, rippleY, maxRadius, newIsDark);
                }
                else
                {
                    // Dark -> Light: Contract to click point
                    DoContractAnimation(rippleX, rippleY, maxRadius, newIsDark);
                }
            };
        }
        
        private void UpdateDecorationVisibility(bool isDark, bool animate)
        {
            if (animate)
            {
                var duration = TimeSpan.FromMilliseconds(500);
                
                // Stars fade
                var starsOpacityAnim = new DoubleAnimation
                {
                    To = isDark ? 1 : 0,
                    Duration = duration,
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                StarsCanvas.BeginAnimation(OpacityProperty, starsOpacityAnim);
                
                // Clouds fade
                var cloudsOpacityAnim = new DoubleAnimation
                {
                    To = isDark ? 0 : 0.8,
                    Duration = duration,
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                CloudsCanvas.BeginAnimation(OpacityProperty, cloudsOpacityAnim);
            }
            else
            {
                StarsCanvas.Opacity = isDark ? 1 : 0;
                CloudsCanvas.Opacity = isDark ? 0 : 0.8;
            }
        }
        
        private void StartCloudAnimations()
        {
            // Cloud1 - slow drift from left to right (bottom layer)
            var cloud1Anim = new DoubleAnimation
            {
                From = -150,
                To = 900,
                Duration = TimeSpan.FromSeconds(50),
                RepeatBehavior = RepeatBehavior.Forever
            };
            Cloud1.BeginAnimation(Canvas.LeftProperty, cloud1Anim);
            
            // Cloud2 - medium speed drift (bottom layer)
            var cloud2Anim = new DoubleAnimation
            {
                From = 900,
                To = -150,
                Duration = TimeSpan.FromSeconds(45),
                RepeatBehavior = RepeatBehavior.Forever
            };
            Cloud2.BeginAnimation(Canvas.LeftProperty, cloud2Anim);
            
            // Cloud3 - faster drift (bottom layer)
            var cloud3Anim = new DoubleAnimation
            {
                From = -100,
                To = 900,
                Duration = TimeSpan.FromSeconds(38),
                RepeatBehavior = RepeatBehavior.Forever
            };
            Cloud3.BeginAnimation(Canvas.LeftProperty, cloud3Anim);
            
            // Cloud4 - middle layer cloud
            if (Cloud4 != null)
            {
                var cloud4Anim = new DoubleAnimation
                {
                    From = 850,
                    To = -100,
                    Duration = TimeSpan.FromSeconds(60),
                    RepeatBehavior = RepeatBehavior.Forever
                };
                Cloud4.BeginAnimation(Canvas.LeftProperty, cloud4Anim);
            }
            
            // Cloud5 - middle layer cloud
            if (Cloud5 != null)
            {
                var cloud5Anim = new DoubleAnimation
                {
                    From = -80,
                    To = 850,
                    Duration = TimeSpan.FromSeconds(55),
                    RepeatBehavior = RepeatBehavior.Forever
                };
                Cloud5.BeginAnimation(Canvas.LeftProperty, cloud5Anim);
            }
            
            // Cloud6 - top layer cloud (very slow, subtle)
            if (Cloud6 != null)
            {
                var cloud6Anim = new DoubleAnimation
                {
                    From = 900,
                    To = -100,
                    Duration = TimeSpan.FromSeconds(70),
                    RepeatBehavior = RepeatBehavior.Forever
                };
                Cloud6.BeginAnimation(Canvas.LeftProperty, cloud6Anim);
            }
        }
        
        private void StartStarTwinkleAnimations()
        {
            var stars = new[] { DecoStar1, DecoStar2, DecoStar3, DecoStar4, DecoStar5, DecoStar6, DecoStar7, DecoStar8,
                                DecoStar9, DecoStar10, DecoStar11, DecoStar12, DecoStar13, DecoStar14, DecoStar15,
                                DecoStar16, DecoStar17, DecoStar18, DecoStar19, DecoStar20, DecoStar21, DecoStar22, DecoStar23 };
            var random = new Random();
            
            for (int i = 0; i < stars.Length; i++)
            {
                var star = stars[i];
                if (star == null) continue;
                
                // Random initial delay for each star
                double initialDelay = random.Next(0, 4000) / 1000.0;
                
                // Create twinkling animation with random timing
                var storyboard = new Storyboard();
                
                var opacityAnim = new DoubleAnimationUsingKeyFrames
                {
                    RepeatBehavior = RepeatBehavior.Forever,
                    BeginTime = TimeSpan.FromSeconds(initialDelay)
                };
                
                // Add key frames for twinkling effect
                double cycleDuration = 2.0 + random.NextDouble() * 3.0; // 2-5 seconds cycle
                double baseOpacity = 0.3 + random.NextDouble() * 0.3; // Base opacity varies
                opacityAnim.KeyFrames.Add(new EasingDoubleKeyFrame(baseOpacity, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0))));
                opacityAnim.KeyFrames.Add(new EasingDoubleKeyFrame(Math.Min(1.0, baseOpacity + 0.5), KeyTime.FromTimeSpan(TimeSpan.FromSeconds(cycleDuration * 0.25))));
                opacityAnim.KeyFrames.Add(new EasingDoubleKeyFrame(baseOpacity + 0.2, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(cycleDuration * 0.5))));
                opacityAnim.KeyFrames.Add(new EasingDoubleKeyFrame(Math.Min(1.0, baseOpacity + 0.4), KeyTime.FromTimeSpan(TimeSpan.FromSeconds(cycleDuration * 0.75))));
                opacityAnim.KeyFrames.Add(new EasingDoubleKeyFrame(baseOpacity, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(cycleDuration))));
                
                Storyboard.SetTarget(opacityAnim, star);
                Storyboard.SetTargetProperty(opacityAnim, new PropertyPath(OpacityProperty));
                storyboard.Children.Add(opacityAnim);
                
                // Store storyboard reference to prevent GC
                star.Tag = storyboard;
                storyboard.Begin();
            }
        }

        /// <summary>
        /// 扩散动画：从小到大（白天→黑夜）
        /// </summary>
        private void DoExpandAnimation(double x, double y, double maxRadius, bool newIsDark)
        {
            // 先设置椭圆位置（中心点在点击位置）
            // Canvas.SetLeft/Top 设置的是左上角，所以需要偏移
            Canvas.SetLeft(WaterDrop, x);
            Canvas.SetTop(WaterDrop, y);
            
            // 使用 Margin 负值让椭圆中心对齐点击位置
            WaterDrop.Margin = new Thickness(0, 0, 0, 0);
            WaterDrop.Width = 0;
            WaterDrop.Height = 0;

            // 宽高动画
            var sizeAnim = new DoubleAnimation
            {
                From = 0,
                To = maxRadius,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            // Margin 动画，让椭圆中心始终在点击位置
            var marginAnim = new ThicknessAnimation
            {
                From = new Thickness(-0, -0, 0, 0),
                To = new Thickness(-maxRadius / 2, -maxRadius / 2, 0, 0),
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            // 350ms 后切换主题（动画进行中）
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(350)
            };
            timer.Tick += (sender, args) =>
            {
                timer.Stop();
                ConfigManager.Current.IsDarkMode = newIsDark;
                ConfigManager.Save();
                ThemeManager.ApplyTheme(newIsDark);
                ThemeManager.SetWindowFrameTheme(this, newIsDark);
                _isDarkMode = newIsDark;
                // Update decoration visibility
                UpdateDecorationVisibility(newIsDark, true);
            };
            timer.Start();

            sizeAnim.Completed += (sender, args) =>
            {
                WaterDrop.Width = 0;
                WaterDrop.Height = 0;
                WaterDrop.Margin = new Thickness(0);
                _isAnimating = false;
            };

            WaterDrop.BeginAnimation(FrameworkElement.WidthProperty, sizeAnim);
            WaterDrop.BeginAnimation(FrameworkElement.HeightProperty, new DoubleAnimation
            {
                From = 0,
                To = maxRadius,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            });
            WaterDrop.BeginAnimation(FrameworkElement.MarginProperty, marginAnim);
        }

        /// <summary>
        /// 收缩动画：从大到小（黑夜→白天）
        /// </summary>
        private void DoContractAnimation(double x, double y, double maxRadius, bool newIsDark)
        {
            // 先填充整个窗口，然后收缩到点击点
            Canvas.SetLeft(WaterDrop, x);
            Canvas.SetTop(WaterDrop, y);
            WaterDrop.Width = maxRadius;
            WaterDrop.Height = maxRadius;
            WaterDrop.Margin = new Thickness(-maxRadius / 2, -maxRadius / 2, 0, 0);

            // 宽高动画：从大到小
            var sizeAnim = new DoubleAnimation
            {
                From = maxRadius,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            // Margin 动画
            var marginAnim = new ThicknessAnimation
            {
                From = new Thickness(-maxRadius / 2, -maxRadius / 2, 0, 0),
                To = new Thickness(0, 0, 0, 0),
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            // 立即切换主题（因为是从大到小收缩，背景色要先变）
            ConfigManager.Current.IsDarkMode = newIsDark;
            ConfigManager.Save();
            ThemeManager.ApplyTheme(newIsDark);
            ThemeManager.SetWindowFrameTheme(this, newIsDark);
            _isDarkMode = newIsDark;
            // Update decoration visibility
            UpdateDecorationVisibility(newIsDark, true);

            sizeAnim.Completed += (sender, args) =>
            {
                WaterDrop.Width = 0;
                WaterDrop.Height = 0;
                WaterDrop.Margin = new Thickness(0);
                _isAnimating = false;
            };

            WaterDrop.BeginAnimation(FrameworkElement.WidthProperty, sizeAnim);
            WaterDrop.BeginAnimation(FrameworkElement.HeightProperty, new DoubleAnimation
            {
                From = maxRadius,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            });
            WaterDrop.BeginAnimation(FrameworkElement.MarginProperty, marginAnim);
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;
            
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    yield return typedChild;
                    
                foreach (var grandChild in FindVisualChildren<T>(child))
                    yield return grandChild;
            }
        }

        private async void CheckUpdateOnStartup()
        {
            try
            {
                var r = await UpdateManager.CheckForUpdatesAsync();

                if (!string.IsNullOrWhiteSpace(r.ErrorMessage)) return;

                if (r.HasUpdate)
                {
                    var notes = string.IsNullOrWhiteSpace(r.ReleaseNotes)
                        ? ""
                        : "\n\nRelease notes:\n" + Truncate(r.ReleaseNotes, 600);

                    var result = WpfMessageBox.Show(
                        $"A new version {r.LatestTag} is available!\nCurrent: v{r.CurrentVersion.Major}.{r.CurrentVersion.Minor}.{r.CurrentVersion.Build}\n\nDownload now?{notes}",
                        "Update Available",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        var url = !string.IsNullOrWhiteSpace(r.AssetDownloadUrl) ? r.AssetDownloadUrl : r.ReleasePageUrl;
                        if (!string.IsNullOrWhiteSpace(url))
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = url,
                                UseShellExecute = true
                            });
                        }
                    }
                }
            }
            catch { }
        }

        private void RefreshBlacklist()
        {
            BlacklistList.ItemsSource = null;
            BlacklistList.ItemsSource = ConfigManager.Current.AppProfiles;
        }

        private void MinimizeBtn_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        private void RadioFilterMode_Changed(object sender, RoutedEventArgs e)
        {
            if (RadioWhitelist == null) return;

            if (RadioWhitelist.IsChecked == true)
            {
                ConfigManager.Current.IsWhitelistMode = true;
                FilterModeHelpText.Text = "Only processes in this list will have auto-scroll enabled.";
            }
            else
            {
                ConfigManager.Current.IsWhitelistMode = false;
                FilterModeHelpText.Text = "Processes in this list will be ignored (auto-scroll disabled).";
            }
            ConfigManager.Save();
        }

        private void BrowseApp_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Executables (*.exe)|*.exe|All files (*.*)|*.*",
                Title = "Select Application to Filter"
            };

            if (dialog.ShowDialog() == true)
                AddProcessFromPath(dialog.FileName);
        }

        private void BlacklistList_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                foreach (string file in files)
                {
                    if (file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        AddProcessFromPath(file);
                }
            }
        }

        private void AddProcessFromPath(string path)
        {
            try
            {
                string processName = System.IO.Path.GetFileNameWithoutExtension(path);
                if (!string.IsNullOrWhiteSpace(processName))
                {
                    _windowManager.AddProfile(processName);
                    RefreshBlacklist();
                }
            }
            catch { }
        }

        private void AddBlacklist_Click(object sender, RoutedEventArgs e)
        {
            string processName = BlacklistInput.Text.Trim();
            if (!string.IsNullOrWhiteSpace(processName))
            {
                if (processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    processName = processName.Substring(0, processName.Length - 4);

                _windowManager.AddProfile(processName);
                BlacklistInput.Text = "";
                RefreshBlacklist();
            }
        }

        private void RemoveBlacklist_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string processName)
            {
                _windowManager.RemoveProfile(processName);
                RefreshBlacklist();
            }
        }

        private void TriggerKeyInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TriggerKeyInput == null) return;
            
            string key = TriggerKeyInput.Text.Trim();
            if (!string.IsNullOrEmpty(key))
            {
                ConfigManager.Current.TriggerKey = key;
                ConfigManager.Save();
            }
        }

        private void TriggerKeyPreset_Click(object sender, RoutedEventArgs e)
        {
            // Show preset options - for now just toggle between common options
            var presets = new[] { "MiddleMouse", "XButton1", "XButton2", "F1", "F2", "F3", "F4" };
            int currentIndex = Array.IndexOf(presets, ConfigManager.Current.TriggerKey);
            int nextIndex = (currentIndex + 1) % presets.Length;
            
            TriggerKeyInput.Text = presets[nextIndex];
        }

        private void TriggerKeyPreset_Select(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string key)
            {
                TriggerKeyInput.Text = key;
            }
        }

        private void BtnListen_Click(object sender, RoutedEventArgs e)
        {
            if (_isListening)
            {
                // Stop listening
                StopListening();
                return;
            }

            // Start listening
            StartListening();
        }

        private void StartListening()
        {
            if (_mouseHook == null && _keyboardHook == null)
            {
                WpfMessageBox.Show("No input hooks available.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _isListening = true;
            BtnListen.Content = FindResource("ListeningPrompt") ?? "Cancel";
            TriggerKeyInput.Text = "";
            TriggerKeyInput.IsEnabled = false;

            // Subscribe to mouse events
            if (_mouseHook != null)
            {
                _mouseHook.MouseEvent += OnMouseEventForListening;
            }
            
            // Subscribe to keyboard events
            if (_keyboardHook != null)
            {
                _keyboardHook.KeyboardEvent += OnKeyboardEventForListening;
            }
        }

        private void StopListening()
        {
            _isListening = false;
            BtnListen.Content = FindResource("BtnListen") ?? "Listen";
            TriggerKeyInput.IsEnabled = true;

            // Unsubscribe from mouse events
            if (_mouseHook != null)
            {
                _mouseHook.MouseEvent -= OnMouseEventForListening;
            }
            
            // Unsubscribe from keyboard events
            if (_keyboardHook != null)
            {
                _keyboardHook.KeyboardEvent -= OnKeyboardEventForListening;
            }
        }

        private void OnKeyboardEventForListening(object? sender, KeyboardEventArgs e)
        {
            // Only handle key down events
            if (e.Message != NativeMethods.WM_KEYDOWN && e.Message != NativeMethods.WM_SYSKEYDOWN)
                return;
            
            // Ignore modifier keys themselves
            if (e.VkCode == NativeMethods.VK_CONTROL || e.VkCode == NativeMethods.VK_SHIFT || 
                e.VkCode == NativeMethods.VK_MENU || e.VkCode == NativeMethods.VK_LCONTROL ||
                e.VkCode == NativeMethods.VK_RCONTROL || e.VkCode == NativeMethods.VK_LSHIFT ||
                e.VkCode == NativeMethods.VK_RSHIFT || e.VkCode == NativeMethods.VK_LMENU ||
                e.VkCode == NativeMethods.VK_RMENU)
                return;
            
            string? keyName = GetKeyName(e.VkCode);
            
            if (!string.IsNullOrEmpty(keyName))
            {
                // Build combined key name with modifiers
                string fullKeyName = BuildCombinedKeyName(keyName);
                
                // Update UI on dispatcher thread
                Dispatcher.Invoke(() =>
                {
                    TriggerKeyInput.Text = fullKeyName;
                    ConfigManager.Current.TriggerKey = fullKeyName;
                    ConfigManager.Save();
                    StopListening();
                });
            }
        }

        private string? GetKeyName(int vkCode)
        {
            // Map virtual key codes to key names
            return vkCode switch
            {
                // Function keys
                0x70 => "F1",
                0x71 => "F2",
                0x72 => "F3",
                0x73 => "F4",
                0x74 => "F5",
                0x75 => "F6",
                0x76 => "F7",
                0x77 => "F8",
                0x78 => "F9",
                0x79 => "F10",
                0x7A => "F11",
                0x7B => "F12",
                
                // Number keys
                0x30 => "D0",
                0x31 => "D1",
                0x32 => "D2",
                0x33 => "D3",
                0x34 => "D4",
                0x35 => "D5",
                0x36 => "D6",
                0x37 => "D7",
                0x38 => "D8",
                0x39 => "D9",
                
                // Letter keys
                0x41 => "A",
                0x42 => "B",
                0x43 => "C",
                0x44 => "D",
                0x45 => "E",
                0x46 => "F",
                0x47 => "G",
                0x48 => "H",
                0x49 => "I",
                0x4A => "J",
                0x4B => "K",
                0x4C => "L",
                0x4D => "M",
                0x4E => "N",
                0x4F => "O",
                0x50 => "P",
                0x51 => "Q",
                0x52 => "R",
                0x53 => "S",
                0x54 => "T",
                0x55 => "U",
                0x56 => "V",
                0x57 => "W",
                0x58 => "X",
                0x59 => "Y",
                0x5A => "Z",
                
                // Modifier keys
                0xA0 => "LShift",
                0xA1 => "RShift",
                0xA2 => "LCtrl",
                0xA3 => "RCtrl",
                0xA4 => "LAlt",
                0xA5 => "RAlt",
                0x5B => "LWin",
                0x5C => "RWin",
                
                // Other keys
                0x20 => "Space",
                0x0D => "Return",
                0x09 => "Tab",
                0x08 => "Backspace",
                0x2D => "Insert",
                0x2E => "Delete",
                0x23 => "End",
                0x24 => "Home",
                0x21 => "PageUp",
                0x22 => "PageDown",
                0x25 => "Left",
                0x26 => "Up",
                0x27 => "Right",
                0x28 => "Down",
                0x2C => "PrintScreen",
                0x2B => "Execute",
                0x3B => "Help",
                
                // Punctuation
                0xBF => "OemQuestion", // /
                0xC0 => "OemTilde", // `
                0xDB => "OemOpenBrackets", // [
                0xDC => "OemPipe", // \
                0xDD => "OemCloseBrackets", // ]
                0xDE => "OemQuotes", // '
                0xBC => "OemComma", // ,
                0xBE => "OemPeriod", // .
                0xBD => "OemMinus", // -
                0xBB => "OemPlus", // +
                
                // Number pad
                0x60 => "NumPad0",
                0x61 => "NumPad1",
                0x62 => "NumPad2",
                0x63 => "NumPad3",
                0x64 => "NumPad4",
                0x65 => "NumPad5",
                0x66 => "NumPad6",
                0x67 => "NumPad7",
                0x68 => "NumPad8",
                0x69 => "NumPad9",
                0x6A => "NumPadMultiply",
                0x6B => "NumPadAdd",
                0x6D => "NumPadSubtract",
                0x6E => "NumPadDecimal",
                0x6F => "NumPadDivide",
                
                _ => null
            };
        }

        private void OnMouseEventForListening(object? sender, Core.MouseEventArgs e)
        {
            string? keyName = null;

            // Determine which mouse button was pressed
            switch (e.Message)
            {
                case NativeMethods.WM_LBUTTONDOWN:
                    // Left mouse button is not allowed as trigger key
                    return;
                case NativeMethods.WM_RBUTTONDOWN:
                    // Right mouse button is not allowed as trigger key
                    return;
                case NativeMethods.WM_MBUTTONDOWN:
                    keyName = "MiddleMouse";
                    break;
                case NativeMethods.WM_XBUTTONDOWN:
                    // XButton1 or XButton2
                    int xButton = (e.MouseData >> 16);
                    if (xButton == 1)
                        keyName = "XButton1";
                    else if (xButton == 2)
                        keyName = "XButton2";
                    break;
            }

            if (!string.IsNullOrEmpty(keyName))
            {
                // Build combined key name with modifiers
                string fullKeyName = BuildCombinedKeyName(keyName);
                
                // Update UI on dispatcher thread
                Dispatcher.Invoke(() =>
                {
                    TriggerKeyInput.Text = fullKeyName;
                    ConfigManager.Current.TriggerKey = fullKeyName;
                    ConfigManager.Save();
                    StopListening();
                });
            }
        }

        private string BuildCombinedKeyName(string baseKey)
        {
            var modifiers = new System.Collections.Generic.List<string>();
            
            if (NativeMethods.IsCtrlPressed())
                modifiers.Add("Ctrl");
            if (NativeMethods.IsAltPressed())
                modifiers.Add("Alt");
            if (NativeMethods.IsShiftPressed())
                modifiers.Add("Shift");
            
            modifiers.Add(baseKey);
            return string.Join("+", modifiers);
        }

        private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageCombo == null) return;

            if (LanguageCombo.SelectedItem is ComboBoxItem item && item.Tag is string langCode)
            {
                LanguageManager.SetLanguage(langCode);
                ConfigManager.Current.Language = langCode;
                ConfigManager.Save();
            }
        }

        private void PerformanceModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PerformanceModeCombo == null) return;

            if (PerformanceModeCombo.SelectedItem is ComboBoxItem item && item.Tag is string modeStr)
            {
                if (Enum.TryParse<PerformanceMode>(modeStr, out var mode))
                {
                    ConfigManager.Current.PerformanceMode = mode;
                    ConfigManager.Save();
                    
                    // Update ScrollEngine TickRate based on performance mode
                    if (_engine != null)
                    {
                        _engine.TickRate = mode switch
                        {
                            PerformanceMode.PowerSaver => 30,
                            PerformanceMode.Balanced => 60,
                            PerformanceMode.HighPerformance => 120,
                            _ => 60
                        };
                    }
                }
            }
        }

        private void BrowseIcon_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.ico|All files (*.*)|*.*",
                Title = "Select Custom Icon"
            };

            if (dialog.ShowDialog() == true)
            {
                string path = dialog.FileName;
                ConfigManager.Current.CustomIconPath = path;
                ConfigManager.Save();
                CustomIconPathInput.Text = path;
            }
        }

        private void ClearIcon_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.Current.CustomIconPath = "";
            ConfigManager.Save();
            CustomIconPathInput.Text = "";
        }

        private void IconSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int size = (int)e.NewValue;
            ConfigManager.Current.IconSize = size;
            if (IconSizeValueText != null) IconSizeValueText.Text = $"{size}px";
            ConfigManager.Save();
        }

        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_engine != null)
            {
                _engine.Sensitivity = (float)e.NewValue;
                if (SpeedValueText != null) SpeedValueText.Text = $"{_engine.Sensitivity:F1}x";
                
                ConfigManager.Current.Sensitivity = _engine.Sensitivity;
                ConfigManager.Save();
            }
        }
        
        private void IndependentSensitivityToggle_IsOnChanged(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            bool useIndependent = IndependentSensitivityToggle.IsOn;
            ConfigManager.Current.UseIndependentSensitivity = useIndependent;
            if (_engine != null) _engine.UseIndependentSensitivity = useIndependent;
            UpdateSensitivityPanelVisibility();
            ConfigManager.Save();
        }
        
        private void UpdateSensitivityPanelVisibility()
        {
            bool useIndependent = IndependentSensitivityToggle.IsOn;
            UnifiedSensitivityPanel.Visibility = useIndependent ? Visibility.Collapsed : Visibility.Visible;
            SpeedSlider.Visibility = useIndependent ? Visibility.Collapsed : Visibility.Visible;
            IndependentSensitivityPanel.Visibility = useIndependent ? Visibility.Visible : Visibility.Collapsed;
        }
        
        private void AdvancedSettingsToggle_IsOnChanged(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            bool showAdvanced = AdvancedSettingsToggle.IsOn;
            ConfigManager.Current.ShowAdvancedSettings = showAdvanced;
            UpdateAdvancedParamsVisibility();
            ConfigManager.Save();
        }
        
        private void UpdateAdvancedParamsVisibility()
        {
            bool showAdvanced = AdvancedSettingsToggle.IsOn;
            AdvancedParamsPanel.Visibility = showAdvanced ? Visibility.Visible : Visibility.Collapsed;
        }
        
        private void VerticalSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            float value = (float)e.NewValue;
            ConfigManager.Current.SensitivityVertical = value;
            if (_engine != null) _engine.SensitivityVertical = value;
            if (VerticalSpeedValueText != null) VerticalSpeedValueText.Text = $"{value:F1}x";
            ConfigManager.Save();
        }
        
        private void HorizontalSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            float value = (float)e.NewValue;
            ConfigManager.Current.SensitivityHorizontal = value;
            if (_engine != null) _engine.SensitivityHorizontal = value;
            if (HorizontalSpeedValueText != null) HorizontalSpeedValueText.Text = $"{value:F1}x";
            ConfigManager.Save();
        }
        
        private void ReadingSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            float value = (float)e.NewValue;
            ConfigManager.Current.ReadingModeSpeed = value;
            if (_engine != null) _engine.ReadingModeSpeed = value;
            if (ReadingSpeedValueText != null) ReadingSpeedValueText.Text = $"{(int)value} px/s";
            ConfigManager.Save();
        }
        
        private void ReadingMaxSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            float value = (float)e.NewValue;
            ConfigManager.Current.ReadingModeMaxSpeed = value;
            if (_engine != null) _engine.ReadingModeMaxSpeed = value;
            if (ReadingMaxSpeedValueText != null) ReadingMaxSpeedValueText.Text = $"{(int)value} px/s";
            ConfigManager.Save();
        }
        
        private void AccelerationCurveCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AccelerationCurveCombo == null) return;
            
            if (AccelerationCurveCombo.SelectedItem is ComboBoxItem item && item.Tag is string curveStr)
            {
                if (Enum.TryParse<AccelerationCurveType>(curveStr, out var curveType))
                {
                    ConfigManager.Current.AccelerationCurve = curveType;
                    if (_engine != null) _engine.CurveType = curveType;
                    
                    if (CurveEditorControl != null)
                    {
                        CurveEditorControl.CurveType = curveType;
                        CurveEditorControl.CurvePoints = ConfigManager.Current.CustomCurvePoints;
                        CurveEditorControl.Config = ConfigManager.Current;
                    }
                    
                    UpdateCurveParamsVisibility();
                    ConfigManager.Save();
                }
            }
        }
        
        private void UpdateCurveParamsVisibility()
        {
            var curveType = ConfigManager.Current.AccelerationCurve;
            ExponentialParamsPanel.Visibility = curveType == AccelerationCurveType.Exponential ? Visibility.Visible : Visibility.Collapsed;
            LogarithmicParamsPanel.Visibility = curveType == AccelerationCurveType.Logarithmic ? Visibility.Visible : Visibility.Collapsed;
            SigmoidParamsPanel.Visibility = curveType == AccelerationCurveType.Sigmoid ? Visibility.Visible : Visibility.Collapsed;
            CustomCurvePanel.Visibility = curveType == AccelerationCurveType.Custom ? Visibility.Visible : Visibility.Collapsed;
        }
        
        private void ExponentSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double value = e.NewValue;
            ConfigManager.Current.AccelerationExponent = value;
            if (ExponentValueText != null) ExponentValueText.Text = $"{value:F1}";
            ConfigManager.Save();
        }
        
        private void LogBaseSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double value = e.NewValue;
            ConfigManager.Current.AccelerationLogBase = value;
            if (LogBaseValueText != null) LogBaseValueText.Text = $"{value:F1}";
            ConfigManager.Save();
        }
        
        private void SigmoidMidpointSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double value = e.NewValue;
            ConfigManager.Current.SigmoidMidpoint = value;
            if (SigmoidMidpointValueText != null) SigmoidMidpointValueText.Text = $"{value:F2}";
            ConfigManager.Save();
        }
        
        private void SigmoidSteepnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double value = e.NewValue;
            ConfigManager.Current.SigmoidSteepness = value;
            if (SigmoidSteepnessValueText != null) SigmoidSteepnessValueText.Text = $"{value:F1}";
            ConfigManager.Save();
        }
        
        private void CurveEditorControl_CurveChanged(object sender, EventArgs e)
        {
            if (CurveEditorControl?.CurvePoints != null)
            {
                ConfigManager.Current.CustomCurvePoints = CurveEditorControl.CurvePoints;
                ConfigManager.Save();
            }
        }
        
        private void ResetCurve_Click(object sender, RoutedEventArgs e)
        {
            CurveEditorControl?.ResetToDefault();
        }
        
        private void FrictionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double value = e.NewValue;
            ConfigManager.Current.Friction = value;
            if (_engine != null) _engine.Friction = value;
            if (FrictionValueText != null) FrictionValueText.Text = $"{value:F1}";
            ConfigManager.Save();
        }
        
        private void InertiaSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double value = e.NewValue;
            ConfigManager.Current.InertiaMultiplier = value;
            if (_engine != null) _engine.InertiaMultiplier = value;
            if (InertiaValueText != null) InertiaValueText.Text = $"{value:F1}x";
            ConfigManager.Save();
        }
        
        private void ResponseTimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double value = e.NewValue / 1000.0;
            ConfigManager.Current.ResponseTime = value;
            if (_engine != null) _engine.ResponseTime = value;
            if (ResponseTimeValueText != null) ResponseTimeValueText.Text = $"{(int)e.NewValue}ms";
            ConfigManager.Save();
        }
        
        private void AxisLockSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double value = e.NewValue;
            ConfigManager.Current.AxisLockRatio = value;
            if (_engine != null) _engine.AxisLockRatio = value;
            if (AxisLockValueText != null) AxisLockValueText.Text = $"{value:F1}";
            ConfigManager.Save();
        }
        
        private void SoftStartSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            int value = (int)e.NewValue;
            ConfigManager.Current.SoftStartRange = value;
            if (_engine != null) _engine.SoftStartRange = value;
            if (SoftStartValueText != null) SoftStartValueText.Text = $"{value}px";
            ConfigManager.Save();
        }

        private void DeadzoneSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_engine != null)
            {
                _engine.Deadzone = (int)e.NewValue;
                if (DeadzoneValueText != null) DeadzoneValueText.Text = $"{_engine.Deadzone}px";

                ConfigManager.Current.Deadzone = _engine.Deadzone;
                ConfigManager.Save();
            }
        }

        private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;

            try
            {
                var r = await UpdateManager.CheckForUpdatesAsync();

                if (!string.IsNullOrWhiteSpace(r.ErrorMessage))
                {
                    WpfMessageBox.Show(r.ErrorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (r.HasUpdate)
                {
                    var notes = string.IsNullOrWhiteSpace(r.ReleaseNotes)
                        ? ""
                        : "\n\nRelease notes:\n" + Truncate(r.ReleaseNotes, 1200);

                    var result = WpfMessageBox.Show(
                        $"A new version {r.LatestTag} is available!\nCurrent: v{r.CurrentVersion.Major}.{r.CurrentVersion.Minor}.{r.CurrentVersion.Build}\n\nDownload now?{notes}",
                        "Update Available",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        var url = !string.IsNullOrWhiteSpace(r.AssetDownloadUrl) ? r.AssetDownloadUrl : r.ReleasePageUrl;
                        if (!string.IsNullOrWhiteSpace(url))
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = url,
                                UseShellExecute = true
                            });
                        }
                    }
                }
                else
                {
                    WpfMessageBox.Show("You are using the latest version.", "Check for Updates", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                WpfMessageBox.Show($"Failed to check for updates: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }

        private void OpenGitHub_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/humanfirework/FlowWheel",
                UseShellExecute = true
            });
        }

        private void OpenLicense_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://github.com/humanfirework/FlowWheel/blob/main/LICENSE",
                UseShellExecute = true
            });
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s;
            return s.Substring(0, max) + "\n...(truncated)";
        }

        private void SetStartup(bool enable)
        {
            try
            {
                var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", true);
                
                if (key == null) return;

                string appName = "FlowWheel";
                // Use Process.MainModule for reliable path (works with single-file publish)
                string? exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                
                if (string.IsNullOrEmpty(exePath)) 
                {
                    // Fallback to Assembly.Location
                    exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                }
                
                if (string.IsNullOrEmpty(exePath)) return;

                if (enable)
                {
                    key.SetValue(appName, $"\"{exePath}\"");
                }
                else
                {
                    key.DeleteValue(appName, false);
                }
                
                key.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to set startup: {ex.Message}");
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }
    }
}