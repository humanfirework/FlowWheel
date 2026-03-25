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
        private double _accumulatedHDelta = 0;
        private int _runId = 0;
        private double _filteredSpeedV = 0;
        private double _filteredSpeedH = 0;
        
        private double _currentSpeed = 0;
        private double _currentHSpeed = 0;
        private NativeMethods.POINT _origin;
        private NativeMethods.POINT _current;
        private NativeMethods.POINT _lastPos;
        private long _lastPosTime;
        
        private double _inertiaSpeedV = 0;
        private double _inertiaSpeedH = 0;
        
        private readonly SyncScrollManager _syncManager = new SyncScrollManager();
        private double _readingSpeed = 0;

        public event EventHandler? Stopped;

        public ScrollState CurrentState { get; private set; } = ScrollState.Idle;
        public bool IsSyncEnabled { get; set; } = false;
        
        // 基础设置
        public float Sensitivity { get; set; } = 0.8f;
        public float SensitivityVertical { get; set; } = 1.0f;
        public float SensitivityHorizontal { get; set; } = 0.8f;
        public bool UseIndependentSensitivity { get; set; } = false;
        
        public int Deadzone { get; set; } = 20;
        public int DeadzoneVertical { get; set; } = 20;
        public int DeadzoneHorizontal { get; set; } = 20;
        public bool UseIndependentDeadzone { get; set; } = false;
        
        public int TickRate { get; set; } = 60;
        public int MinStep { get; set; } = 1;
        
        // 高级参数
        public double Friction { get; set; } = 5.0;
        public double InertiaMultiplier { get; set; } = 1.0;
        public double ResponseTime { get; set; } = 0.04;
        public double AxisLockRatio { get; set; } = 1.8;
        public int SoftStartRange { get; set; } = 12;
        
        // 加速度曲线
        public AccelerationCurveType CurveType { get; set; } = AccelerationCurveType.Linear;
        private AppConfig? _config;
        
        // 阅读模式
        public float ReadingModeSpeed { get; set; } = 30.0f;
        public float ReadingModeMaxSpeed { get; set; } = 500.0f;

        public ScrollEngine()
        {
        }
        
        public void ApplyConfig(AppConfig config)
        {
            _config = config;
            Sensitivity = config.Sensitivity;
            SensitivityVertical = config.SensitivityVertical;
            SensitivityHorizontal = config.SensitivityHorizontal;
            UseIndependentSensitivity = config.UseIndependentSensitivity;
            
            Deadzone = config.Deadzone;
            DeadzoneVertical = config.DeadzoneVertical;
            DeadzoneHorizontal = config.DeadzoneHorizontal;
            UseIndependentDeadzone = config.UseIndependentDeadzone;
            
            Friction = config.Friction;
            InertiaMultiplier = config.InertiaMultiplier;
            ResponseTime = config.ResponseTime;
            AxisLockRatio = config.AxisLockRatio;
            SoftStartRange = config.SoftStartRange;
            
            CurveType = config.AccelerationCurve;
            ReadingModeSpeed = config.ReadingModeSpeed;
            ReadingModeMaxSpeed = config.ReadingModeMaxSpeed;
        }
        
        private float GetVerticalSensitivity()
        {
            return UseIndependentSensitivity ? SensitivityVertical : Sensitivity;
        }
        
        private float GetHorizontalSensitivity()
        {
            return UseIndependentSensitivity ? SensitivityHorizontal : Sensitivity;
        }
        
        private int GetVerticalDeadzone()
        {
            return UseIndependentDeadzone ? DeadzoneVertical : Deadzone;
        }
        
        private int GetHorizontalDeadzone()
        {
            return UseIndependentDeadzone ? DeadzoneHorizontal : Deadzone;
        }

        public void StartReadingMode(double initialSpeed)
        {
            int runId;
            lock (_lock)
            {
                CurrentState = ScrollState.ReadingMode;
                _readingSpeed = initialSpeed;
                _currentSpeed = -initialSpeed;
                _currentHSpeed = 0;
                _accumulatedDelta = 0;
                _accumulatedHDelta = 0;
                _filteredSpeedV = _currentSpeed;
                _filteredSpeedH = 0;
                _runId++;
                runId = _runId;
                
                _isRunning = true;
                Task.Run(() => Loop(runId));
            }
        }

        public void AdjustReadingSpeed(double delta)
        {
            lock (_lock)
            {
                if (CurrentState != ScrollState.ReadingMode) return;
                _readingSpeed += delta;
                if (_readingSpeed < 0) _readingSpeed = 0;
                if (_readingSpeed > ReadingModeMaxSpeed) _readingSpeed = ReadingModeMaxSpeed;
                _currentSpeed = -_readingSpeed;
            }
        }

        public void StartDrag(NativeMethods.POINT origin)
        {
            int runId;
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
                _filteredSpeedV = 0;
                _filteredSpeedH = 0;
                _runId++;
                runId = _runId;

                if (IsSyncEnabled)
                {
                    _syncManager.UpdateTargets(origin);
                }

                Task.Run(() => Loop(runId));
            }
        }

        public void ReleaseDrag()
        {
            lock (_lock)
            {
                if (CurrentState != ScrollState.Dragging) return;

                long now = Stopwatch.GetTimestamp();
                double dt = (now - _lastPosTime) / (double)Stopwatch.Frequency;
                
                if (dt > 0.1)
                {
                    _currentSpeed = 0;
                    _currentHSpeed = 0;
                }

                _currentSpeed *= InertiaMultiplier;
                _currentHSpeed *= InertiaMultiplier;

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
            bool wasRunning = _isRunning;
            lock (_lock)
            {
                CurrentState = ScrollState.Idle;
                _isRunning = false;
                _runId++;
                _accumulatedDelta = 0;
                _accumulatedHDelta = 0;
                _filteredSpeedV = 0;
                _filteredSpeedH = 0;
            }
            
            if (wasRunning)
            {
                Stopped?.Invoke(this, EventArgs.Empty);
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
        
        public void Start(NativeMethods.POINT origin)
        {
            StartDrag(origin);
        }

        public void UpdatePosition(NativeMethods.POINT pt)
        {
            UpdateDragPosition(pt);
        }

        private void CalculateSpeed()
        {
            int vDeadzone = GetVerticalDeadzone();
            int hDeadzone = GetHorizontalDeadzone();
            float vSensitivity = GetVerticalSensitivity();
            float hSensitivity = GetHorizontalSensitivity();
            
            // Vertical
            int dy = _current.y - _origin.y;
            int distY = Math.Abs(dy);

            if (distY < vDeadzone)
            {
                _currentSpeed = 0;
            }
            else
            {
                double effective = distY - vDeadzone;
                double maxDistance = 500.0;
                double normalizedInput = Math.Min(effective / maxDistance, 1.0);
                
                double curveOutput = ApplyAccelerationCurve(normalizedInput);
                
                double rawSpeed = curveOutput * maxDistance * vSensitivity;
                
                if (SoftStartRange > 0 && effective < SoftStartRange)
                {
                    double t = effective / SoftStartRange;
                    t = t * t * (3.0 - 2.0 * t);
                    rawSpeed *= t;
                }
                
                if (rawSpeed > 5000) rawSpeed = 5000;

                if (dy > 0)
                {
                    _currentSpeed = -rawSpeed;
                }
                else
                {
                    _currentSpeed = rawSpeed;
                }
            }

            // Horizontal
            int dx = _current.x - _origin.x;
            int distX = Math.Abs(dx);

            if (distX < hDeadzone)
            {
                _currentHSpeed = 0;
            }
            else
            {
                double effective = distX - hDeadzone;
                double maxDistance = 500.0;
                double normalizedInput = Math.Min(effective / maxDistance, 1.0);
                
                double curveOutput = ApplyAccelerationCurve(normalizedInput);
                
                double rawHSpeed = curveOutput * maxDistance * hSensitivity;
                
                if (SoftStartRange > 0 && effective < SoftStartRange)
                {
                    double t = effective / SoftStartRange;
                    t = t * t * (3.0 - 2.0 * t);
                    rawHSpeed *= t;
                }
                
                if (rawHSpeed > 5000) rawHSpeed = 5000;

                if (dx > 0)
                {
                    _currentHSpeed = rawHSpeed; 
                }
                else
                {
                    _currentHSpeed = -rawHSpeed;
                }
            }

            if (AxisLockRatio > 1.0 && distX >= hDeadzone && distY >= vDeadzone)
            {
                if (distY > distX * AxisLockRatio)
                {
                    _currentHSpeed = 0;
                }
                else if (distX > distY * AxisLockRatio)
                {
                    _currentSpeed = 0;
                }
            }
        }
        
        private double ApplyAccelerationCurve(double normalizedInput)
        {
            if (_config == null)
            {
                return normalizedInput;
            }
            
            return AccelerationCurve.ApplyCurve(normalizedInput, CurveType, _config);
        }

        private async Task Loop(int runId)
        {
            long lastTick = Stopwatch.GetTimestamp();
            long intervalTicks = (long)(Stopwatch.Frequency / (double)TickRate);
            if (intervalTicks < 1) intervalTicks = 1;
            long nextTick = lastTick;

            while (true)
            {
                if (!_isRunning || runId != Volatile.Read(ref _runId)) break;

                long currentTick = Stopwatch.GetTimestamp();
                double dt = (currentTick - lastTick) / (double)Stopwatch.Frequency;
                lastTick = currentTick;

                double targetSpeedV = _currentSpeed;
                double targetSpeedH = _currentHSpeed;

                if (CurrentState == ScrollState.InertialScrolling)
                {
                    double frictionFactor = Math.Exp(-Friction * dt);
                    _inertiaSpeedV *= frictionFactor;
                    _inertiaSpeedH *= frictionFactor;

                    targetSpeedV = _inertiaSpeedV;
                    targetSpeedH = _inertiaSpeedH;
                    _filteredSpeedV = targetSpeedV;
                    _filteredSpeedH = targetSpeedH;

                    if (Math.Abs(_inertiaSpeedV) < 10 && Math.Abs(_inertiaSpeedH) < 10)
                    {
                        lock (_lock)
                        {
                            Stop();
                        }
                    }
                }
                else
                {
                    if (ResponseTime > 0.0001 && dt > 0)
                    {
                        double alpha = 1.0 - Math.Exp(-dt / ResponseTime);
                        _filteredSpeedV += (targetSpeedV - _filteredSpeedV) * alpha;
                        _filteredSpeedH += (targetSpeedH - _filteredSpeedH) * alpha;
                        targetSpeedV = _filteredSpeedV;
                        targetSpeedH = _filteredSpeedH;
                    }
                    else
                    {
                        _filteredSpeedV = targetSpeedV;
                        _filteredSpeedH = targetSpeedH;
                    }
                }

                if (Math.Abs(targetSpeedV) > 0.1)
                {
                    _accumulatedDelta += targetSpeedV * dt;

                    int steps = 0;
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

                nextTick += intervalTicks;
                long now = Stopwatch.GetTimestamp();
                long remaining = nextTick - now;
                if (remaining > 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(remaining / (double)Stopwatch.Frequency)).ConfigureAwait(false);
                }
                else if (-remaining > intervalTicks * 5)
                {
                    nextTick = now;
                }
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
                mouseData = (uint)delta,
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
