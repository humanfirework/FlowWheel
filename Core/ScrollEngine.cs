using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace FlowWheel.Core
{
    public enum ScrollState
    {
        Idle,
        Dragging,
        InertialScrolling,
        ReadingMode
    }

    public class ScrollEngine
    {
        private volatile bool _isRunning = false;
        private readonly object _lock = new object();
        private double _accumulatedDelta = 0;

        public ScrollState CurrentState { get; private set; } = ScrollState.Idle;
        
        // Settings
        public float Sensitivity { get; set; } = 0.5f; // Speed multiplier
        public int Deadzone { get; set; } = 20; // Pixels
        public int TickRate { get; set; } = 120; // Updates per second
        public int MinStep { get; set; } = 1; // Minimum delta to send (fix for Explorer/Win32)
        public double Friction { get; set; } = 5.0; // Friction factor

        // Current State
        private double _currentSpeed = 0; // Delta per second
        private double _currentHSpeed = 0; // Horizontal Speed
        private double _accumulatedHDelta = 0; // Horizontal Accumulator
        private NativeMethods.POINT _origin;
        private NativeMethods.POINT _current;
        private NativeMethods.POINT _lastPos; // For calculating throw velocity
        private long _lastPosTime; // Timestamp for velocity calculation

        // Inertia
        private double _inertiaSpeedV = 0;
        private double _inertiaSpeedH = 0;

        private readonly SyncScrollManager _syncManager = new SyncScrollManager();
        public bool IsSyncEnabled { get; set; } = false;

        // Reading Mode
        private double _readingSpeed = 0; // Pixels per second

        public ScrollEngine()
        {
        }

        public void StartReadingMode(double initialSpeed)
        {
            lock (_lock)
            {
                CurrentState = ScrollState.ReadingMode;
                _readingSpeed = initialSpeed;
                _currentSpeed = -initialSpeed; // Negative is down (usually)
                _currentHSpeed = 0;
                _accumulatedDelta = 0;
                
                if (!_isRunning)
                {
                    _isRunning = true;
                    Task.Run(Loop);
                }
            }
        }

        public void AdjustReadingSpeed(double delta)
        {
            lock (_lock)
            {
                if (CurrentState != ScrollState.ReadingMode) return;
                _readingSpeed += delta;
                if (_readingSpeed < 0) _readingSpeed = 0; // Don't reverse, just stop
                if (_readingSpeed > 1000) _readingSpeed = 1000;
                _currentSpeed = -_readingSpeed;
            }
        }

        public void StartDrag(NativeMethods.POINT origin)
        {
            lock (_lock)
            {
                if (_isRunning && CurrentState == ScrollState.ReadingMode) return;

                CurrentState = ScrollState.Dragging;
                _isRunning = true;
                _origin = origin;
                _current = origin;
                _lastPos = origin;
                _lastPosTime = Stopwatch.GetTimestamp();
                
                _currentSpeed = 0;
                _accumulatedDelta = 0;
                _currentHSpeed = 0;
                _accumulatedHDelta = 0;

                if (IsSyncEnabled)
                {
                    _syncManager.UpdateTargets(origin);
                }

                Task.Run(Loop);
            }
        }

        public void ReleaseDrag()
        {
            lock (_lock)
            {
                if (CurrentState != ScrollState.Dragging) return;

                // Calculate release velocity
                long now = Stopwatch.GetTimestamp();
                double dt = (now - _lastPosTime) / (double)Stopwatch.Frequency;
                
                if (dt > 0.1) // If held still for too long, no inertia
                {
                    _currentSpeed = 0;
                    _currentHSpeed = 0;
                }

                // Trigger inertia if speed is significant
                if (Math.Abs(_currentSpeed) > 100 || Math.Abs(_currentHSpeed) > 100)
                {
                    CurrentState = ScrollState.InertialScrolling;
                    _inertiaSpeedV = _currentSpeed;
                    _inertiaSpeedH = _currentHSpeed;
                }
                else
                {
                    Stop();
                }
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                CurrentState = ScrollState.Idle;
                _isRunning = false;
            }
        }

        public void UpdateDragPosition(NativeMethods.POINT pt)
        {
            if (CurrentState != ScrollState.Dragging) return;

            long now = Stopwatch.GetTimestamp();
            _lastPos = _current;
            _lastPosTime = now;
            _current = pt;
            
            CalculateSpeed();
        }
        
        // Legacy support for click-toggle mode
        public void Start(NativeMethods.POINT origin)
        {
            StartDrag(origin);
        }

        public void UpdatePosition(NativeMethods.POINT pt)
        {
            UpdateDragPosition(pt);
        }
        // End legacy support

        private void CalculateSpeed()
        {
            // Vertical
            int dy = _current.y - _origin.y;
            int distY = Math.Abs(dy);

            // ... (Calculation logic remains similar but simplified for 1:1 feel if needed)
            // For now, keep the deadzone/sensitivity logic as it maps distance to speed well for "Joystick" style
            // For "Grab & Throw" style (iPhone style), we actually need distance to map to position delta, not speed.
            // But FlowWheel's core identity is "Joystick" style (middle click auto scroll).
            // So "Grab" here means "Grab the Joystick Handle", not "Grab the Page".
            // The user requested "Grab & Throw", which implies inertia. 
            // So we keep the Joystick logic (Distance = Speed), but add Inertia on Release.

            if (distY < Deadzone)
            {
                _currentSpeed = 0;
            }
            else
            {
                double rawSpeed = (distY - Deadzone) * Sensitivity;
                if (rawSpeed > 5000) rawSpeed = 5000;

                if (dy > 0) // Mouse Down -> Scroll Down (Negative)
                {
                    _currentSpeed = -rawSpeed;
                }
                else // Mouse Up -> Scroll Up (Positive)
                {
                    _currentSpeed = rawSpeed;
                }
            }

            // Horizontal
            int dx = _current.x - _origin.x;
            int distX = Math.Abs(dx);

            if (distX < Deadzone)
            {
                _currentHSpeed = 0;
            }
            else
            {
                double rawHSpeed = (distX - Deadzone) * Sensitivity;
                if (rawHSpeed > 5000) rawHSpeed = 5000;

                if (dx > 0) // Mouse Right -> Scroll Right
                {
                    _currentHSpeed = rawHSpeed; 
                }
                else // Mouse Left -> Scroll Left
                {
                    _currentHSpeed = -rawHSpeed;
                }
            }
        }

        private async Task Loop()
        {
            long lastTick = Stopwatch.GetTimestamp();
            double interval = 1.0 / TickRate;

            while (_isRunning)
            {
                long currentTick = Stopwatch.GetTimestamp();
                double dt = (currentTick - lastTick) / (double)Stopwatch.Frequency;
                lastTick = currentTick;

                double targetSpeedV = _currentSpeed;
                double targetSpeedH = _currentHSpeed;

                // Handle Inertia
                if (CurrentState == ScrollState.InertialScrolling)
                {
                    // Apply friction
                    double frictionFactor = Math.Exp(-Friction * dt);
                    _inertiaSpeedV *= frictionFactor;
                    _inertiaSpeedH *= frictionFactor;

                    targetSpeedV = _inertiaSpeedV;
                    targetSpeedH = _inertiaSpeedH;

                    // Stop if too slow
                    if (Math.Abs(_inertiaSpeedV) < 10 && Math.Abs(_inertiaSpeedH) < 10)
                    {
                        lock (_lock)
                        {
                            Stop();
                        }
                    }
                }

                if (Math.Abs(targetSpeedV) > 0.1)
                {
                    // Add delta for this frame
                    _accumulatedDelta += targetSpeedV * dt;

                    int steps = 0;
                    // Compatibility Fix: Some apps (Explorer) ignore small deltas (e.g. < 10 or 30).
                    // We accumulate until we reach MinStep.
                    if (Math.Abs(_accumulatedDelta) >= MinStep)
                    {
                        steps = (int)_accumulatedDelta;
                        _accumulatedDelta -= steps;
                    }

                    if (steps != 0)
                    {
                        SendScrollEvent(steps, false);
                    }
                }
                else
                {
                    _accumulatedDelta = 0;
                }

                // Horizontal Processing
                if (Math.Abs(targetSpeedH) > 0.1)
                {
                    _accumulatedHDelta += targetSpeedH * dt;
                    int hSteps = 0;
                    if (Math.Abs(_accumulatedHDelta) >= MinStep)
                    {
                        hSteps = (int)_accumulatedHDelta;
                        _accumulatedHDelta -= hSteps;
                    }

                    if (hSteps != 0)
                    {
                        SendScrollEvent(hSteps, true);
                    }
                }
                else
                {
                    _accumulatedHDelta = 0;
                }

                int delayMs = (int)(interval * 1000);
                if (delayMs < 1) delayMs = 1;
                await Task.Delay(delayMs);
            }
        }

        private void SendScrollEvent(int delta, bool isHorizontal)
        {
            NativeMethods.INPUT[] inputs = new NativeMethods.INPUT[1];
            inputs[0].type = NativeMethods.INPUT_MOUSE;
            inputs[0].mi = new NativeMethods.MOUSEINPUT
            {
                dx = 0,
                dy = 0,
                mouseData = (uint)delta, // Cast acts as signed short representation
                dwFlags = isHorizontal ? (uint)NativeMethods.MOUSEEVENTF_HWHEEL : (uint)NativeMethods.MOUSEEVENTF_WHEEL,
                time = 0,
                dwExtraInfo = MouseHook.INJECTED_SIGNATURE
            };

            NativeMethods.SendInput(1, inputs, Marshal.SizeOf(typeof(NativeMethods.INPUT)));

            if (IsSyncEnabled)
            {
                _syncManager.Scroll(delta, isHorizontal);
            }
        }
    }
}
