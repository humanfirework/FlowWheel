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
        private readonly KeyboardHook? _keyboardHook;
        private readonly ScrollEngine _engine;
        private readonly WindowManager _windowManager;
        private OverlayWindow? _overlay;
        private long _lastUiUpdateTick = 0;
        private const long UiUpdateInterval = 16 * 10000; // ~16ms in ticks (1 tick = 100ns)
        private long _lastMiddleClickTime = 0;

        private bool _isActive = false;
        private bool _isEnabled = true;

        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        public AutoScrollManager(MouseHook hook, KeyboardHook keyboardHook, ScrollEngine engine, WindowManager windowManager)
        {
            _hook = hook;
            _keyboardHook = keyboardHook;
            _engine = engine;
            _windowManager = windowManager;

            _hook.MouseEvent += OnMouseEvent;
            if (_keyboardHook != null)
            {
                _keyboardHook.KeyboardEvent += OnKeyboardEvent;
            }
            
            // Create Overlay on UI Thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                _overlay = new OverlayWindow();
                // Ensure handle is created
                var _ = new System.Windows.Interop.WindowInteropHelper(_overlay).EnsureHandle();
            });
        }

        private void OnKeyboardEvent(object? sender, KeyboardEventArgs e)
        {
            if (!_isEnabled) return;

            // Only handle Key Down for toggle
            if (e.Message == NativeMethods.WM_KEYDOWN || e.Message == NativeMethods.WM_SYSKEYDOWN)
            {
                string trigger = ConfigManager.Current.TriggerKey;
                bool match = false;

                // VK Codes: Alt=0x12, Ctrl=0x11, Shift=0x10
                if (trigger == "Alt" && (e.VkCode == 0x12 || e.VkCode == 0xA4 || e.VkCode == 0xA5)) match = true;
                else if (trigger == "Ctrl" && (e.VkCode == 0x11 || e.VkCode == 0xA2 || e.VkCode == 0xA3)) match = true;
                else if (trigger == "Shift" && (e.VkCode == 0x10 || e.VkCode == 0xA0 || e.VkCode == 0xA1)) match = true;

                if (match)
                {
                    if (_isActive)
                    {
                        StopAutoScroll();
                    }
                    else
                    {
                        NativeMethods.POINT pt;
                        NativeMethods.GetCursorPos(out pt);
                        StartAutoScroll(pt);
                    }
                    e.Handled = true;
                }
            }
        }

        private void OnMouseEvent(object? sender, MouseEventArgs e)
        {
            if (!_isEnabled) return;

            string trigger = ConfigManager.Current.TriggerKey;
            
            // Handle Triggers
            bool isTrigger = false;
            if (trigger == "MiddleMouse" && e.Message == NativeMethods.WM_MBUTTONDOWN) isTrigger = true;
            else if (trigger == "XButton1" && e.Message == NativeMethods.WM_XBUTTONDOWN && (e.MouseData >> 16) == 1) isTrigger = true;
            else if (trigger == "XButton2" && e.Message == NativeMethods.WM_XBUTTONDOWN && (e.MouseData >> 16) == 2) isTrigger = true;

            if (isTrigger)
            {
                // Double Click Detection for Middle Mouse (Reading Mode)
                if (trigger == "MiddleMouse" && ConfigManager.Current.IsReadingModeEnabled)
                {
                    long now = DateTime.Now.Ticks;
                    long diffMs = (now - _lastMiddleClickTime) / 10000;
                    _lastMiddleClickTime = now;

                    if (diffMs < NativeMethods.GetDoubleClickTime())
                    {
                        // Double Click!
                        if (_engine.IsReadingMode)
                        {
                            StopAutoScroll();
                        }
                        else
                        {
                            StartReadingMode(e.Point);
                        }
                        e.Handled = true;
                        return;
                    }
                }

                if (_isActive)
                {
                    StopAutoScroll();
                }
                else
                {
                    // Check Blacklist
                    if (!_windowManager.IsBlacklisted(e.Point))
                    {
                        StartAutoScroll(e.Point);
                    }
                    else
                    {
                        return; // Let system handle it
                    }
                }
                e.Handled = true;
                return;
            }

            // Handle Wheel for Reading Mode Speed Adjustment
            if (e.Message == NativeMethods.WM_MOUSEWHEEL && _engine.IsReadingMode)
            {
                // MouseData is delta (e.g., 120)
                float delta = e.MouseData;
                // 1 notch (120) -> 20px/sec change
                _engine.AdjustReadingSpeed((delta / 120.0f) * 20.0f);
                e.Handled = true;
                return;
            }

            // Handle Stop Logic (Click any other button)
            if (_isActive)
            {
                if (e.Message == NativeMethods.WM_LBUTTONDOWN || 
                    e.Message == NativeMethods.WM_RBUTTONDOWN || 
                    (e.Message == NativeMethods.WM_MBUTTONDOWN && trigger != "MiddleMouse") ||
                    (e.Message == NativeMethods.WM_XBUTTONDOWN && !isTrigger))
                {
                    StopAutoScroll();
                    e.Handled = true; // Consume the click that stops it? Usually yes.
                }
                else if (e.Message == NativeMethods.WM_MOUSEMOVE)
                {
                    _engine.UpdatePosition(e.Point);
                    UpdateVisuals(e.Point);
                }
            }
        }

        private NativeMethods.POINT _currentOrigin;

        private void StartReadingMode(NativeMethods.POINT origin)
        {
            // If normal mode was just started by first click, we don't need to re-create overlay
            // But we need to update its state
            
            _isActive = true;
            _currentOrigin = origin;

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (_overlay == null) return;
                
                // Ensure correct position/size (in case we switched from idle)
                _overlay.Left = SystemParameters.VirtualScreenLeft;
                _overlay.Top = SystemParameters.VirtualScreenTop;
                _overlay.Width = SystemParameters.VirtualScreenWidth;
                _overlay.Height = SystemParameters.VirtualScreenHeight;

                // DPI
                var source = System.Windows.PresentationSource.FromVisual(_overlay);
                double dpiX = 1.0, dpiY = 1.0;
                if (source != null && source.CompositionTarget != null)
                {
                    dpiX = source.CompositionTarget.TransformToDevice.M11;
                    dpiY = source.CompositionTarget.TransformToDevice.M22;
                }
                
                double logicalX = origin.x / dpiX - SystemParameters.VirtualScreenLeft;
                double logicalY = origin.y / dpiY - SystemParameters.VirtualScreenTop;

                _overlay.ShowAnchor(logicalX, logicalY);
                _overlay.SetReadingMode(true);
            });

            // Start with 30px/sec default
            _engine.StartReadingMode(30);
        }

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
            // Don't update direction visuals in Reading Mode
            if (_engine.IsReadingMode) return;

            long currentTick = DateTime.Now.Ticks;
            if (currentTick - _lastUiUpdateTick < UiUpdateInterval) return;
            _lastUiUpdateTick = currentTick;

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                int dy = current.y - _currentOrigin.y;
                int dx = current.x - _currentOrigin.x;
                int deadzone = _engine.Deadzone;

                // Show arrows only if outside deadzone
                bool up = dy < -deadzone;
                bool down = dy > deadzone;
                bool left = dx < -deadzone;
                bool right = dx > deadzone;

                _overlay?.UpdateDirection(up, down, left, right);
                
                // Update Distance for Opacity
                double distance = Math.Sqrt(dx * dx + dy * dy);
                _overlay?.UpdateDistance(distance);
            }, System.Windows.Threading.DispatcherPriority.Render);
        }

        private void StopAutoScroll()
        {
            _isActive = false;
            _engine.Stop();

            Application.Current.Dispatcher.Invoke(() =>
            {
                _overlay?.SetReadingMode(false);
                _overlay?.HideAnchor();
            });
        }

        public void Dispose()
        {
            _engine.Stop();
            _hook.MouseEvent -= OnMouseEvent;
            if (_keyboardHook != null)
            {
                _keyboardHook.KeyboardEvent -= OnKeyboardEvent;
            }
            Application.Current.Dispatcher.Invoke(() => _overlay?.Close());
        }
    }
}
