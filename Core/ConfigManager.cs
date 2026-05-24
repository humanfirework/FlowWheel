using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FlowWheel.Core
{
    public class AppProfile
    {
        public string ProcessName { get; set; } = "";
        public float Sensitivity { get; set; } = 0.8f;
        public int Deadzone { get; set; } = 20;
        public bool UseCustomSettings { get; set; } = false;
        
        public override string ToString() => ProcessName;
    }

    public enum PerformanceMode
    {
        PowerSaver,   // 省电模式 - 30Hz
        Balanced,     // 平衡模式 - 60Hz  
        HighPerformance // 高性能模式 - 120Hz
    }

    public enum AccelerationCurveType
    {
        Linear,      // 线性
        Exponential, // 指数
        Logarithmic, // 对数
        Sigmoid,     // S曲线
        Custom       // 自定义
    }

    public class CustomCurvePoint
    {
        public double X { get; set; } // 输入 (0-1)
        public double Y { get; set; } // 输出 (0-1)
        
        public CustomCurvePoint() { }
        
        public CustomCurvePoint(double x, double y)
        {
            X = Math.Clamp(x, 0, 1);
            Y = Math.Clamp(y, 0, 1);
        }
    }

    public class AppConfig : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private string _language = "en-US";
        public string Language 
        { 
            get => _language; 
            set => SetField(ref _language, value); 
        }
        
        // 独立的灵敏度设置
        private float _sensitivity = 0.8f;
        public float Sensitivity 
        { 
            get => _sensitivity; 
            set => SetField(ref _sensitivity, value); 
        }
        
        private float _sensitivityVertical = 1.0f;
        public float SensitivityVertical 
        { 
            get => _sensitivityVertical; 
            set => SetField(ref _sensitivityVertical, value); 
        }
        
        private float _sensitivityHorizontal = 0.8f;
        public float SensitivityHorizontal 
        { 
            get => _sensitivityHorizontal; 
            set => SetField(ref _sensitivityHorizontal, value); 
        }
        
        private bool _useIndependentSensitivity = false;
        public bool UseIndependentSensitivity 
        { 
            get => _useIndependentSensitivity; 
            set => SetField(ref _useIndependentSensitivity, value); 
        }
        
        private int _deadzone = 20;
        public int Deadzone 
        { 
            get => _deadzone; 
            set => SetField(ref _deadzone, value); 
        }
        
        private int _deadzoneVertical = 20;
        public int DeadzoneVertical 
        { 
            get => _deadzoneVertical; 
            set => SetField(ref _deadzoneVertical, value); 
        }
        
        private int _deadzoneHorizontal = 20;
        public int DeadzoneHorizontal 
        { 
            get => _deadzoneHorizontal; 
            set => SetField(ref _deadzoneHorizontal, value); 
        }
        
        private bool _useIndependentDeadzone = false;
        public bool UseIndependentDeadzone 
        { 
            get => _useIndependentDeadzone; 
            set => SetField(ref _useIndependentDeadzone, value); 
        }
        
        private string _triggerKey = "MiddleMouse";
        public string TriggerKey 
        { 
            get => _triggerKey; 
            set => SetField(ref _triggerKey, value); 
        }
        
        private string _triggerMode = "Toggle";
        public string TriggerMode 
        { 
            get => _triggerMode; 
            set => SetField(ref _triggerMode, value); 
        }
        
        private bool _isEnabled = true;
        public bool IsEnabled 
        { 
            get => _isEnabled; 
            set => SetField(ref _isEnabled, value); 
        }
        
        private bool _isSyncScrollEnabled = false;
        public bool IsSyncScrollEnabled 
        { 
            get => _isSyncScrollEnabled; 
            set => SetField(ref _isSyncScrollEnabled, value); 
        }
        
        private bool _isReadingModeEnabled = false;
        public bool IsReadingModeEnabled 
        { 
            get => _isReadingModeEnabled; 
            set => SetField(ref _isReadingModeEnabled, value); 
        }
        
        private bool _isWhitelistMode = false;
        public bool IsWhitelistMode 
        { 
            get => _isWhitelistMode; 
            set => SetField(ref _isWhitelistMode, value); 
        }
        
        private string _toggleHotkey = "Ctrl+Alt+S";
        public string ToggleHotkey 
        { 
            get => _toggleHotkey; 
            set => SetField(ref _toggleHotkey, value); 
        }
        
        private string _readingModeHotkey = "Ctrl+Alt+R";
        public string ReadingModeHotkey 
        { 
            get => _readingModeHotkey; 
            set => SetField(ref _readingModeHotkey, value); 
        }
        
        private int _middleClickDelay = 0;
        public int MiddleClickDelay 
        { 
            get => _middleClickDelay; 
            set => SetField(ref _middleClickDelay, value); 
        }
        
        private bool _isDarkMode = false;
        public bool IsDarkMode 
        { 
            get => _isDarkMode; 
            set => SetField(ref _isDarkMode, value); 
        }
        
        private bool _startupEnabled = false;
        public bool StartupEnabled 
        { 
            get => _startupEnabled; 
            set => SetField(ref _startupEnabled, value); 
        }
        
        private PerformanceMode _performanceMode = PerformanceMode.Balanced;
        public PerformanceMode PerformanceMode 
        { 
            get => _performanceMode; 
            set => SetField(ref _performanceMode, value); 
        }
        
        // 阅读模式设置
        private float _readingModeSpeed = 30.0f;
        public float ReadingModeSpeed 
        { 
            get => _readingModeSpeed; 
            set => SetField(ref _readingModeSpeed, value); 
        }
        
        private float _readingModeMaxSpeed = 500.0f;
        public float ReadingModeMaxSpeed 
        { 
            get => _readingModeMaxSpeed; 
            set => SetField(ref _readingModeMaxSpeed, value); 
        }
        
        // 加速度曲线设置
        private AccelerationCurveType _accelerationCurve = AccelerationCurveType.Linear;
        public AccelerationCurveType AccelerationCurve 
        { 
            get => _accelerationCurve; 
            set => SetField(ref _accelerationCurve, value); 
        }
        
        private double _accelerationExponent = 1.5;
        public double AccelerationExponent 
        { 
            get => _accelerationExponent; 
            set => SetField(ref _accelerationExponent, value); 
        }
        
        private double _accelerationLogBase = 2.0;
        public double AccelerationLogBase 
        { 
            get => _accelerationLogBase; 
            set => SetField(ref _accelerationLogBase, value); 
        }
        
        private double _sigmoidMidpoint = 0.5;
        public double SigmoidMidpoint 
        { 
            get => _sigmoidMidpoint; 
            set => SetField(ref _sigmoidMidpoint, value); 
        }
        
        private double _sigmoidSteepness = 8.0;
        public double SigmoidSteepness 
        { 
            get => _sigmoidSteepness; 
            set => SetField(ref _sigmoidSteepness, value); 
        }
        
        private List<CustomCurvePoint> _customCurvePoints = new List<CustomCurvePoint>
        {
            new CustomCurvePoint(0.0, 0.0),
            new CustomCurvePoint(0.25, 0.15),
            new CustomCurvePoint(0.5, 0.4),
            new CustomCurvePoint(0.75, 0.7),
            new CustomCurvePoint(1.0, 1.0)
        };
        public List<CustomCurvePoint> CustomCurvePoints 
        { 
            get => _customCurvePoints; 
            set { _customCurvePoints = value; OnPropertyChanged(); global::FlowWheel.Core.AccelerationCurve.InvalidateCache(); } 
        }
        
        // 高级参数
        private double _friction = 5.0;
        public double Friction 
        { 
            get => _friction; 
            set => SetField(ref _friction, value); 
        }
        
        private double _inertiaMultiplier = 1.0;
        public double InertiaMultiplier 
        { 
            get => _inertiaMultiplier; 
            set => SetField(ref _inertiaMultiplier, value); 
        }
        
        private double _responseTime = 0.04;
        public double ResponseTime 
        { 
            get => _responseTime; 
            set => SetField(ref _responseTime, value); 
        }
        
        private double _axisLockRatio = 1.8;
        public double AxisLockRatio 
        { 
            get => _axisLockRatio; 
            set => SetField(ref _axisLockRatio, value); 
        }
        
        private int _softStartRange = 12;
        public int SoftStartRange 
        { 
            get => _softStartRange; 
            set => SetField(ref _softStartRange, value); 
        }
        
        private double _maxScrollSpeed = 1500.0;
        public double MaxScrollSpeed 
        { 
            get => _maxScrollSpeed; 
            set => SetField(ref _maxScrollSpeed, value); 
        }
        
        private int _minStep = 15;
        public int MinStep
        {
            get => _minStep;
            set => SetField(ref _minStep, value);
        }
        
        private int _tickRate = 60;
        public int TickRate 
        { 
            get => _tickRate; 
            set => SetField(ref _tickRate, value); 
        }
        
        private bool _breakSpeedLimit = false;
        public bool BreakSpeedLimit 
        { 
            get => _breakSpeedLimit; 
            set => SetField(ref _breakSpeedLimit, value); 
        }
        
        private double _breakSpeedLimitMax = 2000.0;
        public double BreakSpeedLimitMax 
        { 
            get => _breakSpeedLimitMax; 
            set => SetField(ref _breakSpeedLimitMax, value); 
        }
        
        private bool _showAdvancedSettings = false;
        public bool ShowAdvancedSettings 
        { 
            get => _showAdvancedSettings; 
            set => SetField(ref _showAdvancedSettings, value); 
        }
        
        // Custom Icon Settings
        private string _customIconPath = "";
        public string CustomIconPath 
        { 
            get => _customIconPath;
            set => _customIconPath = value ?? "";
        }
        
        private int _iconSize = 48; // Default size
        public int IconSize 
        {
            get => _iconSize;
            set => _iconSize = Math.Clamp(value, 24, 96); // Limit: 24-96 pixels
        }
        
        public List<AppProfile> AppProfiles { get; set; } = new List<AppProfile>
        {
            new AppProfile { ProcessName = "flowwheel" },
            new AppProfile { ProcessName = "csgo" }, 
            new AppProfile { ProcessName = "valorant" },
            new AppProfile { ProcessName = "dota2" },
            new AppProfile { ProcessName = "league of legends" },
            new AppProfile { ProcessName = "overwatch" },
            new AppProfile { ProcessName = "r5apex" }
        };
    }

    public static class ConfigManager
    {
        private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
        public static AppConfig Current { get; private set; } = new AppConfig();
        public static readonly AppConfig Defaults = new AppConfig();

        private static System.Threading.Timer? _debounceTimer;
        private static readonly object _debounceLock = new object();
        private const int DebounceIntervalMs = 300;

        public static void Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    var config = JsonSerializer.Deserialize<AppConfig>(json);
                    if (config != null)
                    {
                        Current = config;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load config: {ex.Message}");
            }
        }

        public static void Save()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(Current, options);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save config: {ex.Message}");
            }
        }

        /// <summary>
        /// Debounced save - delays the actual save by 300ms, resetting the timer on each call.
        /// Ideal for Slider ValueChanged events to avoid I/O storms during drag.
        /// </summary>
        public static void DebouncedSave()
        {
            lock (_debounceLock)
            {
                _debounceTimer?.Dispose();
                _debounceTimer = new System.Threading.Timer(_ =>
                {
                    try
                    {
                        System.Windows.Application.Current?.Dispatcher.InvokeAsync(() => Save());
                    }
                    catch { }
                }, null, DebounceIntervalMs, System.Threading.Timeout.Infinite);
            }
        }
    }
}
