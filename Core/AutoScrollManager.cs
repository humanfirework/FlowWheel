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

        private bool _isActive = false;
        private bool _isEnabled = true;

        private string _triggerBaseKey = "MiddleMouse";
        private bool _triggerNeedsCtrl = false;
        private bool _triggerNeedsShift = false;
        private bool _triggerNeedsAlt = false;
        private int _triggerVkCode = 0;

        private string _lastParsedTriggerKey = "";

        // Singleton timer - reuse with Change() instead of Dispose+new per trigger
        private readonly System.Threading.Timer _middleClickDelayTimer;
        private bool _middleClickPending = false;
        private NativeMethods.POINT _pendingClickPoint;
        private long _pendingClickTime = 0;
        private readonly object _delayLock = new object();

        private readonly HotkeyMatcher _toggleHotkeyMatcher = new HotkeyMatcher();
        private readonly HotkeyMatcher _readingHotkeyMatcher = new HotkeyMatcher();

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

            // Initialize singleton delay timer (never fires until Change() is called)
            _middleClickDelayTimer = new System.Threading.Timer(OnMiddleClickDelayElapsed, null,
                System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);

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
            bool isKeyDown = (e.Message == NativeMethods.WM_KEYDOWN || e.Message == NativeMethods.WM_SYSKEYDOWN);
            bool isKeyUp = (e.Message == NativeMethods.WM_KEYUP || e.Message == NativeMethods.WM_SYSKEYUP);

            if (isKeyDown)
            {
                // 单快捷键切换模式：按下切换启动/停止
                if (IsToggleHotkeyMatch(e.VkCode))
                {
                    if (_isEnabled)
                    {
                        StopAutoScroll();
                        _isEnabled = false;
                        ConfigManager.Current.IsEnabled = false;
                    }
                    else
                    {
                        _isEnabled = true;
                        ConfigManager.Current.IsEnabled = true;
                    }
                    ConfigManager.Save();
                    e.Handled = true;
                    return;
                }
            }

            if (!_isEnabled) return;

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
        }

        private bool IsReadingModeHotkeyMatch(int vkCode)
        {
            return _readingHotkeyMatcher.IsMatch(vkCode, ConfigManager.Current.ReadingModeHotkey);
        }

        private bool IsToggleHotkeyMatch(int vkCode)
        {
            return _toggleHotkeyMatcher.IsMatch(vkCode, ConfigManager.Current.ToggleHotkey);
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
                // 移除双击中键触发阅读模式的逻辑
                // 阅读模式现在仅通过 ReadingModeHotkey (Ctrl+Alt+R) 触发

                if (_engine.CurrentState == ScrollState.ReadingMode)
                {
                    StopAutoScroll();
                    e.Handled = true;
                    return;
                }

                if (_isActive)
                {
                    ExecuteTriggerDown(mode, e.Point);
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

                        _middleClickDelayTimer.Change(delay, System.Threading.Timeout.Infinite);
                    }

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
                            _middleClickDelayTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                            _delayActivated = false;
                            return;  // 延时未触发，DOWN 已传递，UP 也不吞噬
                        }
                        if (_delayActivated)
                        {
                            _delayActivated = false;
                            if (mode == "Hold" && _isDragging)
                            {
                                _isDragging = false;
                                _engine.ReleaseDrag();
                                Application.Current.Dispatcher.InvokeAsync(() => _overlay?.HideAnchor());
                            }
                            e.Handled = true;  // 延时触发时 DOWN 被吞噬，UP 也吞噬
                            return;
                        }
                    }

                    // Toggle 模式下，DOWN 被吞噬（e.Handled=true），UP 也应吞噬以保持配对
                    // 避免应用收到不配对的 UP 事件导致异常行为
                    if (mode == "Toggle")
                    {
                        e.Handled = true;
                        return;
                    }
                }

                if (mode == "Hold" && _isDragging)
                {
                    _isDragging = false;
                    _engine.ReleaseDrag();

                    Application.Current.Dispatcher.InvokeAsync(() => _overlay?.HideAnchor());
                    e.Handled = true;
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
                    // 不设置 e.Handled = true，让点击事件传递给目标窗口
                    // 这样目标窗口能正常获得焦点和响应
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
                    // During inertia or any active state, stop immediately and restart
                    _engine.Stop();
                    _isActive = false;
                    CancelPendingMiddleClick();
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
                        // During inertia, stop and immediately restart so user doesn't have to wait
                        _engine.Stop();
                        _isActive = false;
                        CancelPendingMiddleClick();

                        var (isBlocked2, _) = _windowManager.CheckProcessState(point);
                        if (!isBlocked2)
                        {
                            _engine.Sensitivity = ConfigManager.Current.Sensitivity;
                            _engine.Deadzone = ConfigManager.Current.Deadzone;
                            StartAutoScroll(point);
                        }
                        return;
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
                    _middleClickDelayTimer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
                }
                _delayActivated = false;
            }
        }

        private bool _delayActivated = false;

        private void OnMiddleClickDelayElapsed(object? state)
        {
            lock (_delayLock)
            {
                if (!_middleClickPending) return;
                _middleClickPending = false;
                _delayActivated = true;
            }

            string mode = ConfigManager.Current.TriggerMode;
            if (mode == "Hold")
            {
                SendSyntheticMiddleUp(_pendingClickPoint);
            }

            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                ExecuteTriggerDown(mode, _pendingClickPoint);
            });
        }

        private void SendSyntheticMiddleUp(NativeMethods.POINT pt)
        {
            var input = new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_MOUSE,
                mi = new NativeMethods.MOUSEINPUT
                {
                    dx = pt.x,
                    dy = pt.y,
                    mouseData = 0,
                    dwFlags = NativeMethods.MOUSEEVENTF_MIDDLEUP,
                    time = 0,
                    dwExtraInfo = MouseHook.INJECTED_SIGNATURE
                }
            };
            NativeMethods.SendInput(1, new[] { input }, System.Runtime.InteropServices.Marshal.SizeOf(typeof(NativeMethods.INPUT)));
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
            // Only handle if still active — if _isActive is already false,
            // the user has already re-started and we must not hide the new anchor.
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
            _middleClickDelayTimer.Dispose();
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
