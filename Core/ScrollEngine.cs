using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace FlowWheel.Core
{
    public class ScrollEngine
    {
        private volatile bool _isRunning = false;
        private readonly object _lock = new object();
        private double _accumulatedDelta = 0;
        
        // Settings
        public float Sensitivity { get; set; } = 0.5f; // Speed multiplier
        public int Deadzone { get; set; } = 20; // Pixels
        public int TickRate { get; set; } = 120; // Updates per second
        public int MinStep { get; set; } = 1; // Minimum delta to send (fix for Explorer/Win32)

        // Current State
        private double _currentSpeed = 0; // Delta per second
        private double _currentHSpeed = 0; // Horizontal Speed
        private double _accumulatedHDelta = 0; // Horizontal Accumulator
        private NativeMethods.POINT _origin;
        private NativeMethods.POINT _current;

        // Inertia
        private bool _isInertiaActive = false;
        private double _inertiaSpeedV = 0;
        private double _inertiaSpeedH = 0;
        private const double Friction = 5.0; // Speed reduction factor

        public ScrollEngine()
        {
        }

        public void Start(NativeMethods.POINT origin)
        {
            lock (_lock)
            {
                if (_isRunning) return;
                _isRunning = true;
                _isInertiaActive = false;
                _origin = origin;
                _current = origin;
                _currentSpeed = 0;
                _accumulatedDelta = 0;
                _currentHSpeed = 0;
                _accumulatedHDelta = 0;
                Task.Run(Loop);
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                // Trigger inertia if speed is significant
                if (Math.Abs(_currentSpeed) > 100 || Math.Abs(_currentHSpeed) > 100)
                {
                    _isInertiaActive = true;
                    _inertiaSpeedV = _currentSpeed;
                    _inertiaSpeedH = _currentHSpeed;
                }
                else
                {
                    _isRunning = false;
                }
            }
        }

        public void UpdatePosition(NativeMethods.POINT pt)
        {
            if (_isInertiaActive) return; // Ignore mouse movement during inertia
            _current = pt;
            CalculateSpeed();
        }

        private void CalculateSpeed()
        {
            // Vertical
            int dy = _current.y - _origin.y;
            int distY = Math.Abs(dy);

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
                if (_isInertiaActive)
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
                            _isRunning = false;
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
        }
    }
}
