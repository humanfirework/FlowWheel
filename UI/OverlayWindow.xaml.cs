using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FlowWheel.Core;

// Alias to resolve naming conflicts
using IOPath = System.IO.Path;
using WpfPoint = System.Windows.Point;

namespace FlowWheel.UI
{
    public partial class OverlayWindow : Window
    {
        private double _currentRotation = 0;
        private double _rotationSpeed = 0;
        private bool _isSpinning = false;
        private int _currentIconSize = 48;
        private int _showVersion = 0;
        
        // Use CompositionTarget.Rendering for smoother animation at 60fps
        private Stopwatch _animationStopwatch = new Stopwatch();
        private bool _compositionRenderingSubscribed = false;

        // Controllable breathing storyboard (created in code, not XAML EventTrigger)
        private Storyboard? _breathingStoryboard;
        private bool _breathingActive = false;

        public OverlayWindow()
        {
            InitializeComponent();
            
            // Enable hardware acceleration for smoother rendering
            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.Fant);
            RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
            RenderOptions.SetClearTypeHint(this, ClearTypeHint.Enabled);
            this.UseLayoutRounding = false;
            
            // Create controllable breathing storyboard in code
            var breathingEase = new SineEase { EasingMode = EasingMode.EaseInOut };
            _breathingStoryboard = new Storyboard { RepeatBehavior = RepeatBehavior.Forever };
            _breathingStoryboard.Children.Add(new DoubleAnimation
            {
                From = 0, To = 0.3, Duration = TimeSpan.FromSeconds(2),
                EasingFunction = breathingEase
            });
            _breathingStoryboard.Children.Add(new DoubleAnimation
            {
                From = 0.3, To = 0, Duration = TimeSpan.FromSeconds(2),
                BeginTime = TimeSpan.FromSeconds(2),
                EasingFunction = breathingEase
            });
            Storyboard.SetTargetProperty(_breathingStoryboard, new PropertyPath("Opacity"));
            
            // Apply initial size
            ApplyIconSize(ConfigManager.Current.IconSize);
        }

        /// <summary>
        /// Load custom icon from path, or use default if path is empty
        /// </summary>
        public void LoadCustomIcon(string? customPath = null)
        {
            try
            {
                string? path = customPath;
                
                // If no custom path provided, check config
                if (string.IsNullOrEmpty(path))
                {
                    path = ConfigManager.Current.CustomIconPath;
                }
                
                // If still empty, check default Assets/anchor.png
                if (string.IsNullOrEmpty(path))
                {
                    path = IOPath.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "anchor.png");
                }
                
                if (File.Exists(path))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(path, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    CustomAnchorImage.Source = bitmap;
                    CustomAnchorImage.Visibility = Visibility.Visible;
                    WheelIndicatorCanvas.Visibility = Visibility.Collapsed;
                }
                else
                {
                    // Use default wheel icon
                    CustomAnchorImage.Visibility = Visibility.Collapsed;
                    WheelIndicatorCanvas.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load custom icon: {ex.Message}");
                // Fallback to default
                CustomAnchorImage.Visibility = Visibility.Collapsed;
                WheelIndicatorCanvas.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Apply icon size (limited to 24-96 pixels)
        /// </summary>
        public void ApplyIconSize(int size)
        {
            _currentIconSize = Math.Clamp(size, 24, 96);
            
            // Update Anchor size
            Anchor.Width = _currentIconSize;
            Anchor.Height = _currentIconSize;
            
            // Update WheelIndicatorCanvas size
            WheelIndicatorCanvas.Width = _currentIconSize;
            WheelIndicatorCanvas.Height = _currentIconSize;
            
            // Update WheelIndicator size
            WheelIndicator.Width = _currentIconSize;
            WheelIndicator.Height = _currentIconSize;
            
            // Calculate proportional sizes
            double scale = _currentIconSize / 48.0;
            
            // Outer glow (larger than ring for halo effect)
            OuterGlow.Width = _currentIconSize * 1.15;
            OuterGlow.Height = _currentIconSize * 1.15;
            
            // Outer ring (was 40x40 on 48x48 grid)
            OuterRing.Width = _currentIconSize * 0.833;
            OuterRing.Height = _currentIconSize * 0.833;
            
            // Inner ring (was 16x16 on 48x48 grid)
            InnerRing.Width = _currentIconSize * 0.333;
            InnerRing.Height = _currentIconSize * 0.333;
            
            // Center dot (was 5x5 on 48x48 grid)
            CenterDot.Width = _currentIconSize * 0.104;
            CenterDot.Height = _currentIconSize * 0.104;
            
            // Update spinning wheel lines
            UpdateSpinningWheelPaths();
            
            // Reading icon size (was 14x14 on 48x48 grid)
            ReadingIcon.Width = _currentIconSize * 0.292;
            ReadingIcon.Height = _currentIconSize * 0.292;
            
            // Arrow positions using Canvas coordinates
            double centerX = _currentIconSize / 2.0;
            double arrowOffset = _currentIconSize * 0.2; // Offset outside the icon
            
            // ArrowUp: center horizontally, above the icon
            Canvas.SetLeft(ArrowUp, centerX - 6); // 6 = half of arrow width (12)
            Canvas.SetTop(ArrowUp, -10 - arrowOffset + _currentIconSize * 0.1); // Position above
            
            // ArrowDown: center horizontally, below the icon
            Canvas.SetLeft(ArrowDown, centerX - 6);
            Canvas.SetTop(ArrowDown, _currentIconSize + arrowOffset - _currentIconSize * 0.1);
            
            // ArrowLeft: center vertically, left of the icon
            Canvas.SetLeft(ArrowLeft, -10 - arrowOffset + _currentIconSize * 0.1);
            Canvas.SetTop(ArrowLeft, centerX - 6); // 6 = half of arrow height (12)
            
            // ArrowRight: center vertically, right of the icon
            Canvas.SetLeft(ArrowRight, _currentIconSize + arrowOffset - _currentIconSize * 0.1);
            Canvas.SetTop(ArrowRight, centerX - 6);
        }

        private void UpdateSpinningWheelPaths()
        {
            // Set SpinningWheel size to match WheelIndicator, so center is at _currentIconSize / 2
            SpinningWheel.Width = _currentIconSize;
            SpinningWheel.Height = _currentIconSize;
            
            double center = _currentIconSize / 2.0;
            double innerRadius = _currentIconSize * 0.125; // 6/48
            double outerRadius = _currentIconSize * 0.292; // 14/48
            
            // Up
            LineUp.Data = new LineGeometry(
                new WpfPoint(center, center - outerRadius),
                new WpfPoint(center, center - innerRadius));
            
            // Down
            LineDown.Data = new LineGeometry(
                new WpfPoint(center, center + innerRadius),
                new WpfPoint(center, center + outerRadius));
            
            // Left
            LineLeft.Data = new LineGeometry(
                new WpfPoint(center - outerRadius, center),
                new WpfPoint(center - innerRadius, center));
            
            // Right
            LineRight.Data = new LineGeometry(
                new WpfPoint(center + innerRadius, center),
                new WpfPoint(center + outerRadius, center));
            
            // Update rotation center
            if (WheelRotate != null)
            {
                WheelRotate.CenterX = center;
                WheelRotate.CenterY = center;
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, exStyle | NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_TOOLWINDOW);
        }

        private void OnCompositionRendering(object? sender, EventArgs e)
        {
            if (!_isSpinning || SpinningWheel == null) 
            {
                UnsubscribeRendering();
                return;
            }
            
            if (!_animationStopwatch.IsRunning)
            {
                _animationStopwatch.Start();
                return;
            }
            
            var dt = _animationStopwatch.Elapsed.TotalSeconds;
            _animationStopwatch.Restart();
            
            if (dt <= 0 || dt > 0.1) return; // Skip if too much time passed
            
            _currentRotation += _rotationSpeed * dt;
            if (SpinningWheel.RenderTransform is RotateTransform rt)
            {
                rt.Angle = _currentRotation;
            }
        }
        
        private void SubscribeRendering()
        {
            if (!_compositionRenderingSubscribed)
            {
                _compositionRenderingSubscribed = true;
                CompositionTarget.Rendering += OnCompositionRendering;
                _animationStopwatch.Restart();
            }
        }
        
        private void UnsubscribeRendering()
        {
            if (_compositionRenderingSubscribed)
            {
                _compositionRenderingSubscribed = false;
                CompositionTarget.Rendering -= OnCompositionRendering;
                _animationStopwatch.Stop();
            }
        }

        public void ShowAnchor(double x, double y)
        {
            _showVersion++;
            ApplyIconSize(ConfigManager.Current.IconSize);
            LoadCustomIcon();

            // Cancel any pending fade-out animation to prevent it from hiding the anchor
            Anchor.BeginAnimation(OpacityProperty, null);

            Canvas.SetLeft(Anchor, x - Anchor.Width / 2);
            Canvas.SetTop(Anchor, y - Anchor.Height / 2);
            Anchor.Visibility = Visibility.Visible;

            Anchor.Opacity = 0;
            WheelIndicatorCanvas.Opacity = 1.0;
            if (CustomAnchorImage.Visibility == Visibility.Visible)
                CustomAnchorImage.Opacity = 1.0;

            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(280))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            fadeIn.Completed += (s, e) => Anchor.Opacity = 1;
            Anchor.BeginAnimation(OpacityProperty, fadeIn);

            ReadingIcon.Visibility = Visibility.Collapsed;
            OuterRing.Visibility = Visibility.Visible;
            SpinningWheel.Visibility = Visibility.Visible;

            ArrowUp.Visibility = Visibility.Collapsed;
            ArrowDown.Visibility = Visibility.Collapsed;
            ArrowLeft.Visibility = Visibility.Collapsed;
            ArrowRight.Visibility = Visibility.Collapsed;

            if (WheelIndicatorCanvas.Visibility == Visibility.Visible)
            {
                _rotationSpeed = 30;
                _isSpinning = true;
                SubscribeRendering();
            }

            SetBreathingActive(true);

            SetBreakSpeedIndicator(ConfigManager.Current.BreakSpeedLimit);

            this.Show();
        }

        public void SetReadingMode(bool enabled)
        {
            if (enabled)
            {
                OuterRing.Visibility = Visibility.Collapsed;
                SpinningWheel.Visibility = Visibility.Collapsed;
                ReadingIcon.Visibility = Visibility.Visible;
                ReadingSpeedText.Visibility = Visibility.Visible;
                _isSpinning = false;
                UpdateDirection(false, false, false, false);
                PositionReadingSpeedText();
            }
            else
            {
                ReadingIcon.Visibility = Visibility.Collapsed;
                ReadingSpeedText.Visibility = Visibility.Collapsed;
                OuterRing.Visibility = Visibility.Visible;
                SpinningWheel.Visibility = Visibility.Visible;
                _isSpinning = true;
            }
        }

        private void PositionReadingSpeedText()
        {
            double centerX = _currentIconSize / 2.0;
            Canvas.SetLeft(ReadingSpeedText, centerX + _currentIconSize * 0.4);
            Canvas.SetTop(ReadingSpeedText, _currentIconSize * 0.3);
        }

        public void UpdateReadingSpeed(double speed)
        {
            ReadingSpeedText.Text = $"{(int)speed} px/s";
        }

        public void SetBreakSpeedIndicator(bool enabled)
        {
            BreakSpeedIndicator.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
            if (enabled)
            {
                double centerX = _currentIconSize / 2.0;
                Canvas.SetLeft(BreakSpeedIndicator, centerX - 30);
                Canvas.SetTop(BreakSpeedIndicator, -20);

                var pulse = new DoubleAnimationUsingKeyFrames
                {
                    RepeatBehavior = RepeatBehavior.Forever,
                    Duration = TimeSpan.FromSeconds(2)
                };
                pulse.KeyFrames.Add(new DiscreteDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
                pulse.KeyFrames.Add(new DiscreteDoubleKeyFrame(1.1, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.5))));
                pulse.KeyFrames.Add(new DiscreteDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.0))));
                BreakSpeedScale.BeginAnimation(ScaleTransform.ScaleXProperty, pulse);
                BreakSpeedScale.BeginAnimation(ScaleTransform.ScaleYProperty, pulse);
            }
            else
            {
                BreakSpeedScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                BreakSpeedScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            }
        }

        public void UpdateDirection(bool up, bool down, bool left, bool right)
        {
            ArrowUp.Visibility = up ? Visibility.Visible : Visibility.Collapsed;
            ArrowDown.Visibility = down ? Visibility.Visible : Visibility.Collapsed;
            ArrowLeft.Visibility = left ? Visibility.Visible : Visibility.Collapsed;
            ArrowRight.Visibility = right ? Visibility.Visible : Visibility.Collapsed;
            
            bool isScrolling = up || down || left || right;
            SetBreathingActive(!isScrolling);
            
            // Adjust spin speed based on direction (only for default wheel)
            if (WheelIndicatorCanvas.Visibility == Visibility.Visible)
            {
                if (up || down)
                {
                    _rotationSpeed = down ? 180 : -180; // Spin faster when scrolling
                }
                else
                {
                    _rotationSpeed = 30; // Slow idle
                }
            }
        }

        /// <summary>
        /// Controls the breathing animation: active when idle, paused when scrolling.
        /// </summary>
        private void SetBreathingActive(bool active)
        {
            if (BreathingRing == null || _breathingStoryboard == null) return;

            if (active && !_breathingActive)
            {
                _breathingActive = true;
                _breathingStoryboard.Begin(BreathingRing, true); // isControllable = true
            }
            else if (!active && _breathingActive)
            {
                _breathingActive = false;
                _breathingStoryboard.Pause(BreathingRing);
                BreathingRing.Opacity = 0;
            }
        }

        public void UpdateDistance(double distance)
        {
            double opacity = 1.0;

            if (distance < 60)
            {
                // Fade out when close
                opacity = 0.3 + (distance / 60.0) * 0.7;
            }
            else if (distance > 150)
            {
                // Fade out when far
                opacity = 1.0 - (distance - 150) / 400.0;
                if (opacity < 0.3) opacity = 0.3;
            }
            
            WheelIndicatorCanvas.Opacity = opacity;
            if (CustomAnchorImage.Visibility == Visibility.Visible) 
                CustomAnchorImage.Opacity = opacity;
        }

        public void HideAnchor()
        {
            _isSpinning = false;
            UnsubscribeRendering();
            SetBreathingActive(false);

            int hideVersion = _showVersion;

            var fadeOut = new DoubleAnimation(Anchor.Opacity, 0, TimeSpan.FromMilliseconds(150))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            fadeOut.Completed += (s, e) =>
            {
                // If a newer ShowAnchor was called after this HideAnchor, skip hiding
                if (_showVersion != hideVersion) return;
                Anchor.Visibility = Visibility.Collapsed;
                this.Hide();
            };
            Anchor.BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
