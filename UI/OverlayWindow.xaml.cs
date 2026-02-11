using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using FlowWheel.Core;

namespace FlowWheel.UI
{
    public partial class OverlayWindow : Window
    {
        public OverlayWindow()
        {
            InitializeComponent();
            LoadCustomIcon();
        }

        private void LoadCustomIcon()
        {
            try
            {
                // Look for Assets/anchor.png relative to executable
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "anchor.png");
                if (File.Exists(path))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(path, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    CustomAnchorImage.Source = bitmap;
                    CustomAnchorImage.Visibility = Visibility.Visible;
                    DefaultAnchor.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                // Fallback to default
                System.Diagnostics.Debug.WriteLine($"Failed to load custom icon: {ex.Message}");
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            
            // Make the window "Transparent" to input (Click-through)
            // We only want to show visuals, not intercept clicks (the Hook does that)
            var hwnd = new WindowInteropHelper(this).Handle;
            int exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, exStyle | NativeMethods.WS_EX_TRANSPARENT | NativeMethods.WS_EX_TOOLWINDOW);
        }

        public void ShowAnchor(double x, double y)
        {
            // Position the anchor center at x, y
            Canvas.SetLeft(Anchor, x - Anchor.Width / 2);
            Canvas.SetTop(Anchor, y - Anchor.Height / 2);
            Anchor.Visibility = Visibility.Visible;
            
            // Reset visibility
            CenterGraphic.Opacity = 1.0;
            if (CustomAnchorImage.Visibility == Visibility.Visible) CustomAnchorImage.Opacity = 1.0;
            ReadingIcon.Visibility = Visibility.Collapsed;
            CenterGraphic.Visibility = Visibility.Visible;
            
            // Reset arrows
            if (ArrowUp != null) ArrowUp.Visibility = Visibility.Collapsed;
            if (ArrowDown != null) ArrowDown.Visibility = Visibility.Collapsed;
            if (ArrowLeft != null) ArrowLeft.Visibility = Visibility.Collapsed;
            if (ArrowRight != null) ArrowRight.Visibility = Visibility.Collapsed;

            this.Show();
        }

        public void SetReadingMode(bool enabled)
        {
            if (enabled)
            {
                CenterGraphic.Visibility = Visibility.Collapsed;
                ReadingIcon.Visibility = Visibility.Visible;
                // Hide arrows in reading mode
                UpdateDirection(false, false, false, false);
            }
            else
            {
                ReadingIcon.Visibility = Visibility.Collapsed;
                CenterGraphic.Visibility = Visibility.Visible;
            }
        }

        public void UpdateDirection(bool up, bool down, bool left, bool right)
        {
            if (DefaultAnchor.Visibility == Visibility.Visible)
            {
                if (ArrowUp != null) ArrowUp.Visibility = up ? Visibility.Visible : Visibility.Collapsed;
                if (ArrowDown != null) ArrowDown.Visibility = down ? Visibility.Visible : Visibility.Collapsed;
                if (ArrowLeft != null) ArrowLeft.Visibility = left ? Visibility.Visible : Visibility.Collapsed;
                if (ArrowRight != null) ArrowRight.Visibility = right ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public void UpdateDistance(double distance)
        {
            // If distance > 40, fade out center
            double opacity = 1.0;
            if (distance > 40)
            {
                // Linear fade from 40 to 100
                opacity = 1.0 - (distance - 40) / 60.0;
                if (opacity < 0.2) opacity = 0.2;
            }
            
            CenterGraphic.Opacity = opacity;
            if (CustomAnchorImage.Visibility == Visibility.Visible) CustomAnchorImage.Opacity = opacity;
        }

        public void HideAnchor()
        {
            Anchor.Visibility = Visibility.Collapsed;
            this.Hide();
        }
    }
}
