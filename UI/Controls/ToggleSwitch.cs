using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using MediaBrush = System.Windows.Media.Brush;
using MediaColor = System.Windows.Media.Color;
using MediaBrushes = System.Windows.Media.Brushes;
using WpfBorder = System.Windows.Controls.Border;
using WpfControl = System.Windows.Controls.Control;

namespace FlowWheel.UI.Controls
{
    [TemplatePart(Name = "PART_SwitchTrack", Type = typeof(WpfBorder))]
    [TemplatePart(Name = "PART_Thumb", Type = typeof(WpfBorder))]
    [TemplatePart(Name = "PART_Ripple", Type = typeof(Ellipse))]
    public class ToggleSwitch : WpfControl
    {
        private WpfBorder? _track;
        private WpfBorder? _thumb;
        private Ellipse? _ripple;
        private Storyboard? _rippleStoryboard;

        static ToggleSwitch()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ToggleSwitch),
                new FrameworkPropertyMetadata(typeof(ToggleSwitch)));
        }

        public static readonly DependencyProperty IsOnProperty =
            DependencyProperty.Register(nameof(IsOn), typeof(bool), typeof(ToggleSwitch),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsOnChanged));

        public static readonly DependencyProperty OnTextProperty =
            DependencyProperty.Register(nameof(OnText), typeof(string), typeof(ToggleSwitch),
                new PropertyMetadata("ON"));

        public static readonly DependencyProperty OffTextProperty =
            DependencyProperty.Register(nameof(OffText), typeof(string), typeof(ToggleSwitch),
                new PropertyMetadata("OFF"));

        public static readonly DependencyProperty OnColorProperty =
            DependencyProperty.Register(nameof(OnColor), typeof(MediaBrush), typeof(ToggleSwitch),
                new PropertyMetadata(new SolidColorBrush(MediaColor.FromRgb(0, 122, 204))));

        public static readonly DependencyProperty OffColorProperty =
            DependencyProperty.Register(nameof(OffColor), typeof(MediaBrush), typeof(ToggleSwitch),
                new PropertyMetadata(new SolidColorBrush(MediaColor.FromRgb(200, 200, 200))));

        public static readonly DependencyProperty ThumbColorProperty =
            DependencyProperty.Register(nameof(ThumbColor), typeof(MediaBrush), typeof(ToggleSwitch),
                new PropertyMetadata(MediaBrushes.White));

        public static readonly DependencyProperty RippleColorProperty =
            DependencyProperty.Register(nameof(RippleColor), typeof(MediaBrush), typeof(ToggleSwitch),
                new PropertyMetadata(new SolidColorBrush(MediaColor.FromArgb(80, 255, 255, 255))));

        public bool IsOn
        {
            get => (bool)GetValue(IsOnProperty);
            set => SetValue(IsOnProperty, value);
        }

        public string OnText
        {
            get => (string)GetValue(OnTextProperty);
            set => SetValue(OnTextProperty, value);
        }

        public string OffText
        {
            get => (string)GetValue(OffTextProperty);
            set => SetValue(OffTextProperty, value);
        }

        public MediaBrush OnColor
        {
            get => (MediaBrush)GetValue(OnColorProperty);
            set => SetValue(OnColorProperty, value);
        }

        public MediaBrush OffColor
        {
            get => (MediaBrush)GetValue(OffColorProperty);
            set => SetValue(OffColorProperty, value);
        }

        public MediaBrush ThumbColor
        {
            get => (MediaBrush)GetValue(ThumbColorProperty);
            set => SetValue(ThumbColorProperty, value);
        }

        public MediaBrush RippleColor
        {
            get => (MediaBrush)GetValue(RippleColorProperty);
            set => SetValue(RippleColorProperty, value);
        }

        public event RoutedPropertyChangedEventHandler<bool>? IsOnChanged;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _track = GetTemplateChild("PART_SwitchTrack") as WpfBorder;
            _thumb = GetTemplateChild("PART_Thumb") as WpfBorder;
            _ripple = GetTemplateChild("PART_Ripple") as Ellipse;

            if (_track != null)
            {
                _track.MouseLeftButtonDown += OnTrackClick;
            }

            UpdateVisualState(false);
        }

        private void OnTrackClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            IsOn = !IsOn;
            PlayRippleAnimation();
        }

        private static void OnIsOnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ToggleSwitch toggle)
            {
                toggle.UpdateVisualState(true);
                toggle.IsOnChanged?.Invoke(toggle, new RoutedPropertyChangedEventArgs<bool>((bool)e.OldValue, (bool)e.NewValue));
            }
        }

        private void UpdateVisualState(bool animate)
        {
            if (_track == null || _thumb == null) return;

            var duration = animate ? TimeSpan.FromMilliseconds(200) : TimeSpan.Zero;

            var trackColor = IsOn ? OnColor : OffColor;
            var thumbOffset = IsOn ? 20.0 : 0.0;

            if (animate)
            {
                var trackAnim = new ColorAnimation
                {
                    To = ((SolidColorBrush)trackColor).Color,
                    Duration = duration,
                    EasingFunction = new QuadraticEase()
                };

                var thumbAnim = new ThicknessAnimation
                {
                    To = new Thickness(thumbOffset, 2, 0, 2),
                    Duration = duration,
                    EasingFunction = new QuadraticEase()
                };

                if (_track.Background is SolidColorBrush trackBrush && !trackBrush.IsFrozen)
                {
                    trackBrush.BeginAnimation(SolidColorBrush.ColorProperty, trackAnim);
                }
                else
                {
                    var newBrush = new SolidColorBrush(((SolidColorBrush)trackColor).Color);
                    _track.Background = newBrush;
                }

                _thumb.BeginAnimation(MarginProperty, thumbAnim);
            }
            else
            {
                _track.Background = trackColor;
                _thumb.Margin = new Thickness(thumbOffset, 2, 0, 2);
            }
        }

        private void PlayRippleAnimation()
        {
            if (_ripple == null) return;

            _rippleStoryboard?.Stop();

            _ripple.RenderTransform = new ScaleTransform(0, 0);
            _ripple.Opacity = 1;

            var scaleXAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(400))
            {
                EasingFunction = new QuadraticEase()
            };

            var scaleYAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(400))
            {
                EasingFunction = new QuadraticEase()
            };

            var opacityAnim = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300))
            {
                BeginTime = TimeSpan.FromMilliseconds(200)
            };

            _rippleStoryboard = new Storyboard();
            Storyboard.SetTarget(scaleXAnim, _ripple);
            Storyboard.SetTargetProperty(scaleXAnim, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleX)"));
            Storyboard.SetTarget(scaleYAnim, _ripple);
            Storyboard.SetTargetProperty(scaleYAnim, new PropertyPath("(UIElement.RenderTransform).(ScaleTransform.ScaleY)"));
            Storyboard.SetTarget(opacityAnim, _ripple);
            Storyboard.SetTargetProperty(opacityAnim, new PropertyPath(OpacityProperty));

            _rippleStoryboard.Children.Add(scaleXAnim);
            _rippleStoryboard.Children.Add(scaleYAnim);
            _rippleStoryboard.Children.Add(opacityAnim);

            _rippleStoryboard.Begin();
        }
    }
}
