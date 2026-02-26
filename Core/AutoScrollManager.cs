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
        private const long UiUpdateInterval = 50 * 10000; // ~50ms in ticks (1 tick = 100ns) - reduced from 16ms for lower CPU
        private long _lastMiddleClickTime = 0;

        private bool _isActive = false;
        private bool _isEnabled = true;

        // Parsed trigger key components
        private string _triggerBaseKey = "MiddleMouse";
        private bool _triggerNeedsCtrl = false;
        private bool _triggerNeedsShift = false;
        private bool _triggerNeedsAlt = false;
        private int _triggerVkCode = 0; // For keyboard keys like F1-F12

        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        /// <summary>
        /// Parse trigger key string like "MiddleMouse", "Ctrl+MiddleMouse", "Ctrl+Alt+F1"
        /// </summary>
        private void ParseTriggerKey(string triggerKey)
        {
            _triggerNeedsCtrl = false;
            _triggerNeedsShift = false;
            _triggerNeedsAlt = false;
            _triggerVkCode = 0;

            string[] parts = triggerKey.Split('+');
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].Trim();
                if (i < parts.Length - 1)
                {
                    // Modifier key
                    switch (part.ToLower())
                    {
                        case "ctrl":
                        case "control":
                            _triggerNeedsCtrl = true;
                            break;
                        case "shift":
                            _triggerNeedsShift = true;
                            break;
                        case "alt":
                            _triggerNeedsAlt = true;
                            break;
                    }
                }
                else
                {
                    // Base key
                    _triggerBaseKey = part;
                    
                    // Map F keys to VK codes
                    if (part.StartsWith("F", StringComparison.OrdinalIgnoreCase) && int.TryParse(part.Substring(1), out int fNum))
                    {
                        _triggerVkCode = NativeMethods.VK_F1 + fNum - 1;
                    }
                }
            }
        }

        /// <summary>
        /// Check if modifier keys match the trigger requirement
        /// </summary>
        private bool CheckModifiers()
        {
            // If trigger needs modifiers, check if they are pressed
            // If trigger doesn't need modifiers, don't check (allow any modifier state)
            if (_triggerNeedsCtrl || _triggerNeedsAlt || _triggerNeedsShift)
            {
                bool ctrlOk = _triggerNeedsCtrl ? NativeMethods.IsCtrlPressed() : true;
                bool shiftOk = _triggerNeedsShift ? NativeMethods.IsShiftPressed() : true;
                bool altOk = _triggerNeedsAlt ? NativeMethods.IsAltPressed() : true;
                return ctrlOk && shiftOk && altOk;
            }
            return true; // No modifiers required, always return true
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
            _engine.Stopped += OnEngineStopped;
            
            var dispatcher = Application.Current.Dispatcher;
            if (dispatcher.CheckAccess())
            {
                _overlay = new OverlayWindow();
                var _ = new WindowInteropHelper(_overlay).EnsureHandle();
            }
            else
            {
                dispatcher.InvokeAsync(() =>
                {
                    _overlay = new OverlayWindow();
                    var _ = new WindowInteropHelper(_overlay).EnsureHandle();
                });
            }
        }

        private void OnKeyboardEvent(object? sender, KeyboardEventArgs e)
        {
            if (!_isEnabled) return;

            // Parse trigger key for keyboard-based triggers (F keys)
            ParseTriggerKey(ConfigManager.Current.TriggerKey);
            
            // Handle F key triggers (F1-F12)
            if (_triggerVkCode > 0)
            {
                bool isKeyDown = (e.Message == NativeMethods.WM_KEYDOWN || e.Message == NativeMethods.WM_SYSKEYDOWN);
                bool isKeyUp = (e.Message == NativeMethods.WM_KEYUP || e.Message == NativeMethods.WM_SYSKEYUP);
                
                if (e.VkCode == _triggerVkCode && CheckModifiers())
                {
                    if (isKeyDown)
                    {
                        string mode = ConfigManager.Current.TriggerMode;
                        
                        // Stop if Reading Mode is active
                        if (_engine.CurrentState == ScrollState.ReadingMode)
                        {
                            StopAutoScroll();
                            e.Handled = true;
                            return;
                        }
                        
                        if (mode == "Hold")
                        {
                            if (_isActive) StopAutoScroll();
                            
                            NativeMethods.POINT pt;
                            NativeMethods.GetCursorPos(out pt);
                            var (isBlocked, _) = _windowManager.CheckProcessState(pt);
                            if (!isBlocked)
                            {
                                _engine.Sensitivity = ConfigManager.Current.Sensitivity;
                                _engine.Deadzone = ConfigManager.Current.Deadzone;
                                _isDragging = true;
                                StartAutoScroll(pt);
                            }
                        }
                        else // Toggle Mode
                        {
                            ToggleAutoScroll();
                        }
                        e.Handled = true;
                    }
                    else if (isKeyUp && ConfigManager.Current.TriggerMode == "Hold" && _isActive)
                    {
                        // In Hold mode, release on key up
                        if (_engine.CurrentState == ScrollState.Dragging)
                        {
                            _engine.ReleaseDrag();
                            Application.Current.Dispatcher.InvokeAsync(() => _overlay?.HideAnchor());
                        }
                        else
                        {
                            StopAutoScroll();
                        }
                        e.Handled = true;
                    }
                    return;
                }
            }

            // Only handle Key Down for toggle hotkey
            if (e.Message == NativeMethods.WM_KEYDOWN || e.Message == NativeMethods.WM_SYSKEYDOWN)
            {
                // Custom Hotkey Check
                if (IsHotkeyMatch(e.VkCode))
                {
                    ToggleAutoScroll();
                    e.Handled = true;
                    return;
                }
            }
        }

        private bool IsHotkeyMatch(int vkCode)
        {
            string hotkey = ConfigManager.Current.ToggleHotkey; // e.g., "Ctrl+Alt+S"
            if (string.IsNullOrEmpty(hotkey)) return false;

            string[] parts = hotkey.Split('+');
            bool ctrl = false, alt = false, shift = false;
            string key = "";

            foreach (var part in parts)
            {
                var p = part.Trim();
                if (p.Equals("Ctrl", StringComparison.OrdinalIgnoreCase)) ctrl = true;
                else if (p.Equals("Alt", StringComparison.OrdinalIgnoreCase)) alt = true;
                else if (p.Equals("Shift", StringComparison.OrdinalIgnoreCase)) shift = true;
                else key = p;
            }

            // Check Modifiers
            bool isCtrl = (NativeMethods.GetKeyState(0x11) & 0x8000) != 0;
            bool isAlt = (NativeMethods.GetKeyState(0x12) & 0x8000) != 0;
            bool isShift = (NativeMethods.GetKeyState(0x10) & 0x8000) != 0;

            if (ctrl != isCtrl) return false;
            if (alt != isAlt) return false;
            if (shift != isShift) return false;

            // Check Key (Simple mapping for now, assuming letters/numbers match VK codes mostly)
            // A-Z: 65-90
            // 0-9: 48-57
            if (key.Length == 1)
            {
                char k = char.ToUpper(key[0]);
                if (vkCode == (int)k) return true;
            }
            
            // F1-F12: 112-123
            if (key.StartsWith("F") && int.TryParse(key.Substring(1), out int fNum))
            {
                if (vkCode == 111 + fNum) return true;
            }

            return false;
        }

        private void ToggleAutoScroll()
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
        }

        private bool _isDragging = false;

        private void OnMouseEvent(object? sender, MouseEventArgs e)
        {
            if (!_isEnabled) return;

            // Handle Wheel for Reading Mode Speed Adjustment
            if (e.Message == NativeMethods.WM_MOUSEWHEEL && _engine.CurrentState == ScrollState.ReadingMode)
            {
                // MouseData is delta (e.g., 120)
                float delta = e.MouseData;
                // 1 notch (120) -> 20px/sec change
                _engine.AdjustReadingSpeed((delta / 120.0f) * 20.0f);
                e.Handled = true;
                return;
            }

            // Parse trigger key
            ParseTriggerKey(ConfigManager.Current.TriggerKey);
            
            // Handle Triggers
            bool isTriggerDown = false;
            bool isTriggerUp = false;

            // Check base key
            switch (_triggerBaseKey)
            {
                case "MiddleMouse":
                    isTriggerDown = (e.Message == NativeMethods.WM_MBUTTONDOWN);
                    isTriggerUp = (e.Message == NativeMethods.WM_MBUTTONUP);
                    break;
                case "XButton1":
                    isTriggerDown = (e.Message == NativeMethods.WM_XBUTTONDOWN && (e.MouseData >> 16) == 1);
                    isTriggerUp = (e.Message == NativeMethods.WM_XBUTTONUP && (e.MouseData >> 16) == 1);
                    break;
                case "XButton2":
                    isTriggerDown = (e.Message == NativeMethods.WM_XBUTTONDOWN && (e.MouseData >> 16) == 2);
                    isTriggerUp = (e.Message == NativeMethods.WM_XBUTTONUP && (e.MouseData >> 16) == 2);
                    break;
                // LeftMouse and RightMouse are not allowed as trigger keys
            }

            // Check modifiers for trigger down/up
            if (isTriggerDown || isTriggerUp)
            {
                if (!CheckModifiers())
                {
                    isTriggerDown = false;
                    isTriggerUp = false;
                }
            }

            // --- Mode Logic Separation ---
            string mode = ConfigManager.Current.TriggerMode; // "Toggle" or "Hold"
            
            if (isTriggerDown)
            {
                // 1. Reading Mode Check (Always Double Click)
                // Only for MiddleMouse without modifiers
                if (_triggerBaseKey == "MiddleMouse" && !_triggerNeedsCtrl && !_triggerNeedsShift && !_triggerNeedsAlt 
                    && ConfigManager.Current.IsReadingModeEnabled)
                {
                    long now = DateTime.Now.Ticks;
                    long diffMs = (now - _lastMiddleClickTime) / 10000;
                    _lastMiddleClickTime = now;

                    if (diffMs < NativeMethods.GetDoubleClickTime())
                    {
                        if (_engine.CurrentState == ScrollState.ReadingMode)
                            StopAutoScroll();
                        else
                            StartReadingMode(e.Point);
                        
                        e.Handled = true;
                        return;
                    }
                }

                // 2. Stop if Reading Mode is active
                if (_engine.CurrentState == ScrollState.ReadingMode)
                {
                    StopAutoScroll();
                    e.Handled = true;
                    return;
                }

                // 3. Mode Specific Start Logic
                if (mode == "Hold")
                {
                    // Hold Mode: Always Start Drag on Down
                    if (_isActive) 
                    {
                        // If already active (e.g. inertia), restart drag
                        StopAutoScroll(); // Reset
                    }
                    
                    var (isBlocked, profile) = _windowManager.CheckProcessState(e.Point);
                    if (!isBlocked)
                    {
                        // Use global settings only (Per-App removed as requested)
                        _engine.Sensitivity = ConfigManager.Current.Sensitivity;
                        _engine.Deadzone = ConfigManager.Current.Deadzone;

                        _isDragging = true;
                        StartAutoScroll(e.Point);
                        e.Handled = true;
                    }
                }
                else // Toggle Mode
                {
                    // Toggle Mode: Start if idle, Stop if active
                    if (_isActive)
                    {
                        // Fix for Inertia in Toggle Mode:
                        // Instead of abrupt Stop(), we check if we are already in Inertia.
                        // If in Inertia, Stop(). If in Dragging, Start Inertia (ReleaseDrag).
                        
                        if (_engine.CurrentState == ScrollState.Dragging)
                        {
                            // Trigger Inertia
                            _engine.ReleaseDrag();
                            
                            // Visual feedback: Hide anchor but maybe keep some indication? 
                            // For now, let's just hide anchor to mimic "throw"
                            Application.Current.Dispatcher.InvokeAsync(() => _overlay?.HideAnchor());
                        }
                        else
                        {
                            // Already in Inertia or Reading -> Full Stop
                            StopAutoScroll();
                        }
                    }
                    else
                    {
                        var (isBlocked, profile) = _windowManager.CheckProcessState(e.Point);
                        if (!isBlocked)
                        {
                            // Use global settings only
                            _engine.Sensitivity = ConfigManager.Current.Sensitivity;
                            _engine.Deadzone = ConfigManager.Current.Deadzone;

                            StartAutoScroll(e.Point);
                        }
                    }
                    e.Handled = true;
                }
                return;
            }

            if (isTriggerUp)
            {
                if (mode == "Hold" && _isDragging)
                {
                    // Hold Mode: Release -> Throw (Inertia)
                    _isDragging = false;
                    _engine.ReleaseDrag();
                    
                    // Hide anchor immediately for "Throw" feel
                    Application.Current.Dispatcher.InvokeAsync(() => _overlay?.HideAnchor());
                    return;
                }
                // Toggle Mode: Ignore Up
            }

            // Handle Stop Logic (Click any other button)
            if (_isActive)
            {
                // Stop on other clicks
                if (e.Message == NativeMethods.WM_LBUTTONDOWN || 
                    e.Message == NativeMethods.WM_RBUTTONDOWN ||
                    (e.Message == NativeMethods.WM_MBUTTONDOWN && _triggerBaseKey != "MiddleMouse") ||
                    (e.Message == NativeMethods.WM_XBUTTONDOWN && !isTriggerDown))
                {
                    StopAutoScroll();
                    e.Handled = true; 
                    return;
                }
                
                // Mouse Move
                if (e.Message == NativeMethods.WM_MOUSEMOVE)
                {
                    if (_engine.CurrentState == ScrollState.Dragging)
                    {
                        // Update Position
                        // In Hold Mode: _isDragging is true
                        // In Toggle Mode: _isDragging is false (we didn't set it in Toggle start block above, or we should?)
                        // Wait, StartAutoScroll calls _engine.Start(origin) which calls StartDrag.
                        // So Engine is in Dragging state.
                        
                        // We just need to update position.
                        _engine.UpdateDragPosition(e.Point);
                        UpdateVisuals(e.Point);
                    }
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

            Application.Current.Dispatcher.InvokeAsync(() =>
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
            Application.Current.Dispatcher.InvokeAsync(() =>
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
            if (_engine.CurrentState == ScrollState.ReadingMode) return;

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

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _overlay?.SetReadingMode(false);
                _overlay?.HideAnchor();
            });
        }

        private void OnEngineStopped(object? sender, EventArgs e)
        {
            if (_isActive)
            {
                _isActive = false;
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _overlay?.SetReadingMode(false);
                    _overlay?.HideAnchor();
                });
            }
        }

        public void Dispose()
        {
            _engine.Stop();
            _hook.MouseEvent -= OnMouseEvent;
            _engine.Stopped -= OnEngineStopped;
            if (_keyboardHook != null)
            {
                _keyboardHook.KeyboardEvent -= OnKeyboardEvent;
            }
            Application.Current.Dispatcher.InvokeAsync(() => _overlay?.Close());
        }
    }
}
