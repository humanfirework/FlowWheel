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
        private const long UiUpdateInterval = 50 * 10000;
        private long _lastMiddleClickTime = 0;
        private long _readingModeStopTime = 0;

        private bool _isActive = false;
        private bool _isEnabled = true;

        private string _triggerBaseKey = "MiddleMouse";
        private bool _triggerNeedsCtrl = false;
        private bool _triggerNeedsShift = false;
        private bool _triggerNeedsAlt = false;
        private int _triggerVkCode = 0;

        private string _lastParsedTriggerKey = "";

        private System.Threading.Timer? _middleClickDelayTimer = null;
        private bool _middleClickPending = false;
        private NativeMethods.POINT _pendingClickPoint;
        private long _pendingClickTime = 0;
        private readonly object _delayLock = new object();

        public bool IsEnabled
        {
            get => _isEnabled;
            set => _isEnabled = value;
        }

        private void ParseTriggerKey(string triggerKey)
        {
            if (triggerKey == _lastParsedTriggerKey) return;
            _lastParsedTriggerKey = triggerKey;

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
                    string normalizedKey = part.ToLower();

                    if (normalizedKey.StartsWith("f") && int.TryParse(part.Substring(1), out int fNum) && fNum >= 1 && fNum <= 24)
                    {
                        _triggerVkCode = NativeMethods.VK_F1 + fNum - 1;
                        _triggerBaseKey = "F" + fNum;
                    }
                    else if (normalizedKey.Length == 1 && normalizedKey[0] >= 'a' && normalizedKey[0] <= 'z')
                    {
                        _triggerVkCode = (int)char.ToUpper(normalizedKey[0]);
                        _triggerBaseKey = char.ToUpper(normalizedKey[0]).ToString();
                    }
                    else if (normalizedKey.Length == 1 && normalizedKey[0] >= '0' && normalizedKey[0] <= '9')
                    {
                        _triggerVkCode = (int)'0' + (normalizedKey[0] - '0');
                        _triggerBaseKey = "D" + normalizedKey[0];
                    }
                    else
                    {
                        _triggerBaseKey = normalizedKey switch
                        {
                            "middlemouse" => "MiddleMouse",
                            "xbutton1" => "XButton1",
                            "xbutton2" => "XButton2",
                            _ => part
                        };
                    }
                }
            }
        }

        private bool CheckModifiers()
        {
            if (!_triggerNeedsCtrl && !_triggerNeedsAlt && !_triggerNeedsShift)
            {
                return true;
            }

            bool isCtrlPressed = NativeMethods.IsCtrlPressed();
            bool isAltPressed = NativeMethods.IsAltPressed();
            bool isShiftPressed = NativeMethods.IsShiftPressed();

            return _triggerNeedsCtrl == isCtrlPressed
                && _triggerNeedsAlt == isAltPressed
                && _triggerNeedsShift == isShiftPressed;
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

            bool isKeyDown = (e.Message == NativeMethods.WM_KEYDOWN || e.Message == NativeMethods.WM_SYSKEYDOWN);
            bool isKeyUp = (e.Message == NativeMethods.WM_KEYUP || e.Message == NativeMethods.WM_SYSKEYUP);

            if (isKeyDown)
            {
                if (IsReadingModeHotkeyMatch(e.VkCode))
                {
                    if (ConfigManager.Current.IsReadingModeEnabled)
                    {
                        NativeMethods.POINT pt;
                        NativeMethods.GetCursorPos(out pt);
                        var (isBlocked, _) = _windowManager.CheckProcessState(pt);
                        if (!isBlocked)
                        {
                            if (_engine.CurrentState == ScrollState.ReadingMode)
                                StopAutoScroll();
                            else
                                StartReadingMode(pt);
                        }
                    }
                    e.Handled = true;
                    return;
                }
            }

            ParseTriggerKey(ConfigManager.Current.TriggerKey);

            if (_triggerVkCode > 0)
            {
                if (e.VkCode == _triggerVkCode && CheckModifiers())
                {
                    if (isKeyDown)
                    {
                        string mode = ConfigManager.Current.TriggerMode;

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
                                _engine.ApplyConfig(ConfigManager.Current);
                                _isDragging = true;
                                StartAutoScroll(pt);
                            }
                        }
                        else
                        {
                            ToggleAutoScroll();
                        }
                        e.Handled = true;
                    }
                    else if (isKeyUp && ConfigManager.Current.TriggerMode == "Hold" && _isActive)
                    {
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

            if (isKeyDown)
            {
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
            string hotkey = ConfigManager.Current.ToggleHotkey;
            if (string.IsNullOrEmpty(hotkey)) return false;
            return IsHotkeyMatchInternal(hotkey, vkCode);
        }

        private bool IsReadingModeHotkeyMatch(int vkCode)
        {
            string hotkey = ConfigManager.Current.ReadingModeHotkey;
            if (string.IsNullOrEmpty(hotkey)) return false;
            return IsHotkeyMatchInternal(hotkey, vkCode);
        }

        private bool IsHotkeyMatchInternal(string hotkey, int vkCode)
        {
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

            bool isCtrl = (NativeMethods.GetKeyState(0x11) & 0x8000) != 0;
            bool isAlt = (NativeMethods.GetKeyState(0x12) & 0x8000) != 0;
            bool isShift = (NativeMethods.GetKeyState(0x10) & 0x8000) != 0;

            if (ctrl != isCtrl) return false;
            if (alt != isAlt) return false;
            if (shift != isShift) return false;

            if (key.Length == 1)
            {
                char k = char.ToUpper(key[0]);
                if (vkCode == (int)k) return true;
            }

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

            if (e.Message == NativeMethods.WM_MOUSEWHEEL && _engine.CurrentState == ScrollState.ReadingMode)
            {
                float delta = e.MouseData;
                _engine.AdjustReadingSpeed((delta / 120.0f) * 20.0f);
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _overlay?.UpdateReadingSpeed(_engine.GetCurrentReadingSpeed());
                });
                e.Handled = true;
                return;
            }

            ParseTriggerKey(ConfigManager.Current.TriggerKey);

            bool isTriggerDown = false;
            bool isTriggerUp = false;

            switch (_triggerBaseKey)
            {
                case "MiddleMouse":
                    isTriggerDown = (e.Message == NativeMethods.WM_MBUTTONDOWN);
                    isTriggerUp = (e.Message == NativeMethods.WM_MBUTTONUP);
                    break;
                case "XButton1":
                    isTriggerDown = (e.Message == NativeMethods.WM_XBUTTONDOWN && e.MouseData == 1);
                    isTriggerUp = (e.Message == NativeMethods.WM_XBUTTONUP && e.MouseData == 1);
                    break;
                case "XButton2":
                    isTriggerDown = (e.Message == NativeMethods.WM_XBUTTONDOWN && e.MouseData == 2);
                    isTriggerUp = (e.Message == NativeMethods.WM_XBUTTONUP && e.MouseData == 2);
                    break;
            }

            if (isTriggerDown || isTriggerUp)
            {
                if (!CheckModifiers())
                {
                    isTriggerDown = false;
                    isTriggerUp = false;
                }
            }

            string mode = ConfigManager.Current.TriggerMode;

            if (isTriggerDown)
            {
                if (!_triggerNeedsCtrl && !_triggerNeedsShift && !_triggerNeedsAlt
                    && ConfigManager.Current.IsReadingModeEnabled
                    && _triggerBaseKey == "MiddleMouse")
                {
                    long now = DateTime.Now.Ticks;
                    long diffMs = (now - _lastMiddleClickTime) / 10000;
                    _lastMiddleClickTime = now;

                    if (diffMs < NativeMethods.GetDoubleClickTime())
                    {
                        long timeSinceReadingStop = (now - _readingModeStopTime) / 10000;
                        if (timeSinceReadingStop < NativeMethods.GetDoubleClickTime())
                        {
                            e.Handled = true;
                            return;
                        }

                        CancelPendingMiddleClick();

                        var (isBlocked, _) = _windowManager.CheckProcessState(e.Point);
                        if (!isBlocked)
                        {
                            if (_engine.CurrentState == ScrollState.ReadingMode)
                                StopAutoScroll();
                            else
                                StartReadingMode(e.Point);
                        }

                        e.Handled = true;
                        return;
                    }
                }

                if (_engine.CurrentState == ScrollState.ReadingMode)
                {
                    _readingModeStopTime = DateTime.Now.Ticks;
                    StopAutoScroll();
                    e.Handled = true;
                    return;
                }

                int delay = ConfigManager.Current.MiddleClickDelay;
                if (delay > 0 && _triggerBaseKey == "MiddleMouse" && !_triggerNeedsCtrl && !_triggerNeedsShift && !_triggerNeedsAlt)
                {
                    lock (_delayLock)
                    {
                        _middleClickPending = true;
                        _pendingClickPoint = e.Point;
                        _pendingClickTime = DateTime.Now.Ticks;

                        _middleClickDelayTimer?.Dispose();
                        _middleClickDelayTimer = new System.Threading.Timer(_ =>
                        {
                            lock (_delayLock)
                            {
                                if (!_middleClickPending) return;
                                _middleClickPending = false;
                            }

                            Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                ExecuteTriggerDown(mode, _pendingClickPoint);
                            });
                        }, null, delay, System.Threading.Timeout.Infinite);
                    }

                    e.Handled = true;
                    return;
                }

                ExecuteTriggerDown(mode, e.Point);
                e.Handled = true;
                return;
            }

            if (isTriggerUp)
            {
                if (_triggerBaseKey == "MiddleMouse" && !_triggerNeedsCtrl && !_triggerNeedsShift && !_triggerNeedsAlt)
                {
                    lock (_delayLock)
                    {
                        if (_middleClickPending)
                        {
                            _middleClickPending = false;
                            _middleClickDelayTimer?.Dispose();
                            _middleClickDelayTimer = null;
                            return;
                        }
                    }
                }

                if (mode == "Hold" && _isDragging)
                {
                    _isDragging = false;
                    _engine.ReleaseDrag();

                    Application.Current.Dispatcher.InvokeAsync(() => _overlay?.HideAnchor());
                    return;
                }
            }

            if (_isActive)
            {
                if (e.Message == NativeMethods.WM_LBUTTONDOWN ||
                    e.Message == NativeMethods.WM_RBUTTONDOWN ||
                    (e.Message == NativeMethods.WM_MBUTTONDOWN && _triggerBaseKey != "MiddleMouse") ||
                    (e.Message == NativeMethods.WM_XBUTTONDOWN && !isTriggerDown))
                {
                    StopAutoScroll();
                    e.Handled = true;
                    return;
                }

                if (e.Message == NativeMethods.WM_MOUSEMOVE)
                {
                    if (_engine.CurrentState == ScrollState.Dragging)
                    {
                        _engine.UpdateDragPosition(e.Point);
                        UpdateVisuals(e.Point);
                    }
                }
            }
        }

        private void ExecuteTriggerDown(string mode, NativeMethods.POINT point)
        {
            if (mode == "Hold")
            {
                if (_isActive)
                {
                    StopAutoScroll();
                }

                var (isBlocked, _) = _windowManager.CheckProcessState(point);
                if (!isBlocked)
                {
                    _engine.Sensitivity = ConfigManager.Current.Sensitivity;
                    _engine.Deadzone = ConfigManager.Current.Deadzone;

                    _isDragging = true;
                    StartAutoScroll(point);
                }
            }
            else
            {
                if (_isActive)
                {
                    if (_engine.CurrentState == ScrollState.Dragging)
                    {
                        _engine.ReleaseDrag();
                        Application.Current.Dispatcher.InvokeAsync(() => _overlay?.HideAnchor());
                    }
                    else
                    {
                        StopAutoScroll();
                    }
                }
                else
                {
                    var (isBlocked, _) = _windowManager.CheckProcessState(point);
                    if (!isBlocked)
                    {
                        _engine.Sensitivity = ConfigManager.Current.Sensitivity;
                        _engine.Deadzone = ConfigManager.Current.Deadzone;

                        StartAutoScroll(point);
                    }
                }
            }
        }

        private void CancelPendingMiddleClick()
        {
            lock (_delayLock)
            {
                if (_middleClickPending)
                {
                    _middleClickPending = false;
                    _middleClickDelayTimer?.Dispose();
                    _middleClickDelayTimer = null;
                }
            }
        }

        private NativeMethods.POINT _currentOrigin;

        private void StartReadingMode(NativeMethods.POINT origin)
        {
            _isActive = true;
            _currentOrigin = origin;

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (_overlay == null) return;

                _overlay.Left = SystemParameters.VirtualScreenLeft;
                _overlay.Top = SystemParameters.VirtualScreenTop;
                _overlay.Width = SystemParameters.VirtualScreenWidth;
                _overlay.Height = SystemParameters.VirtualScreenHeight;

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

            _engine.StartReadingMode(ConfigManager.Current.ReadingModeSpeed);
        }

        private void StartAutoScroll(NativeMethods.POINT origin)
        {
            _isActive = true;
            _currentOrigin = origin;

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (_overlay == null) return;

                _overlay.Left = SystemParameters.VirtualScreenLeft;
                _overlay.Top = SystemParameters.VirtualScreenTop;
                _overlay.Width = SystemParameters.VirtualScreenWidth;
                _overlay.Height = SystemParameters.VirtualScreenHeight;

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
            });

            _engine.Start(origin);
        }

        private void UpdateVisuals(NativeMethods.POINT current)
        {
            if (_engine.CurrentState == ScrollState.ReadingMode) return;

            long currentTick = DateTime.Now.Ticks;
            if (currentTick - _lastUiUpdateTick < UiUpdateInterval) return;
            _lastUiUpdateTick = currentTick;

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                int dy = current.y - _currentOrigin.y;
                int dx = current.x - _currentOrigin.x;
                int deadzone = _engine.Deadzone;

                bool up = dy < -deadzone;
                bool down = dy > deadzone;
                bool left = dx < -deadzone;
                bool right = dx > deadzone;

                _overlay?.UpdateDirection(up, down, left, right);

                double distance = Math.Sqrt(dx * dx + dy * dy);
                _overlay?.UpdateDistance(distance);
            }, System.Windows.Threading.DispatcherPriority.Render);
        }

        private void StopAutoScroll()
        {
            CancelPendingMiddleClick();
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
                CancelPendingMiddleClick();
                Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _overlay?.SetReadingMode(false);
                    _overlay?.HideAnchor();
                });
            }
        }

        public void Dispose()
        {
            CancelPendingMiddleClick();
            _middleClickDelayTimer?.Dispose();
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
