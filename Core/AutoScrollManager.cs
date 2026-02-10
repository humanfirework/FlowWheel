using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using FlowWheel.Core;
using FlowWheel.UI;
using Application = System.Windows.Application;

namespace FlowWheel.Core
{
    public class AutoScrollManager : IDisposable
    {
        private readonly MouseHook _hook;
        private readonly ScrollEngine _engine;
        private readonly WindowManager _windowManager;
        private OverlayWindow? _overlay;

        private bool _isActive = false;
        private bool _isEnabled = true;

        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        public AutoScrollManager(MouseHook hook, ScrollEngine engine, WindowManager windowManager)
        {
            _hook = hook;
            _engine = engine;
            _windowManager = windowManager;

            _hook.MouseEvent += OnMouseEvent;
            
            // Create Overlay on UI Thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                _overlay = new OverlayWindow();
                // Ensure handle is created
                var _ = new System.Windows.Interop.WindowInteropHelper(_overlay).EnsureHandle();
            });
        }

        private void OnMouseEvent(object? sender, MouseEventArgs e)
        {
            if (!_isEnabled) return;

            switch (e.Message)
            {
                case NativeMethods.WM_MBUTTONDOWN:
                    HandleMiddleClick(e);
                    break;

                case NativeMethods.WM_MOUSEMOVE:
                    if (_isActive)
                    {
                        _engine.UpdatePosition(e.Point);
                        UpdateVisuals(e.Point);
                    }
                    break;

                case NativeMethods.WM_LBUTTONDOWN:
                case NativeMethods.WM_RBUTTONDOWN:
                    if (_isActive && e.Message != NativeMethods.WM_MBUTTONDOWN)
                    {
                        StopAutoScroll();
                        e.Handled = true;
                    }
                    break;
            }
        }

        private void HandleMiddleClick(MouseEventArgs e)
        {
            if (_isActive)
            {
                // Second middle click stops it
                StopAutoScroll();
                e.Handled = true;
                return;
            }

            // Check Blacklist
            if (_windowManager.IsBlacklisted(e.Point))
            {
                return; // Let system handle it
            }

            // Start AutoScroll
            StartAutoScroll(e.Point);
            e.Handled = true;
        }

        private NativeMethods.POINT _currentOrigin;

        private void StartAutoScroll(NativeMethods.POINT origin)
        {
            _isActive = true;
            _currentOrigin = origin;
            
            // Show Visuals
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_overlay == null) return;

                // Ensure the overlay covers the screen
                _overlay.Left = SystemParameters.VirtualScreenLeft;
                _overlay.Top = SystemParameters.VirtualScreenTop;
                _overlay.Width = SystemParameters.VirtualScreenWidth;
                _overlay.Height = SystemParameters.VirtualScreenHeight;

                // Get DPI scale
                var source = System.Windows.PresentationSource.FromVisual(_overlay);
                double dpiX = 1.0, dpiY = 1.0;
                if (source != null && source.CompositionTarget != null)
                {
                    dpiX = source.CompositionTarget.TransformToDevice.M11;
                    dpiY = source.CompositionTarget.TransformToDevice.M22;
                }
                
                // Convert screen coordinates (pixels) to WPF logical units
                double logicalX = origin.x / dpiX - SystemParameters.VirtualScreenLeft;
                double logicalY = origin.y / dpiY - SystemParameters.VirtualScreenTop;

                _overlay.ShowAnchor(logicalX, logicalY);
            });

            // Start Engine
            _engine.Start(origin);
        }

        private void UpdateVisuals(NativeMethods.POINT current)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                int dy = current.y - _currentOrigin.y;
                int dx = current.x - _currentOrigin.x;
                int deadzone = _engine.Deadzone; // Access engine settings

                // Show arrows only if outside deadzone
                bool up = dy < -deadzone;
                bool down = dy > deadzone;
                bool left = dx < -deadzone;
                bool right = dx > deadzone;

                _overlay?.UpdateDirection(up, down, left, right);
            });
        }

        private void StopAutoScroll()
        {
            _isActive = false;
            _engine.Stop();

            Application.Current.Dispatcher.Invoke(() =>
            {
                _overlay?.HideAnchor();
            });
        }

        public void Dispose()
        {
            _engine.Stop();
            _hook.MouseEvent -= OnMouseEvent;
            Application.Current.Dispatcher.Invoke(() => _overlay?.Close());
        }
    }
}
