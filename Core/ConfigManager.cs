using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

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

    public class AppConfig
    {
        public string Language { get; set; } = "en-US";
        
        // 独立的灵敏度设置
        public float Sensitivity { get; set; } = 0.8f;
        public float SensitivityVertical { get; set; } = 1.0f;   // 垂直方向独立灵敏度
        public float SensitivityHorizontal { get; set; } = 0.8f; // 水平方向独立灵敏度
        public bool UseIndependentSensitivity { get; set; } = false; // 是否使用独立灵敏度
        
        public int Deadzone { get; set; } = 20;
        public int DeadzoneVertical { get; set; } = 20;   // 垂直方向死区
        public int DeadzoneHorizontal { get; set; } = 20; // 水平方向死区
        public bool UseIndependentDeadzone { get; set; } = false; // 是否使用独立死区
        
        public string TriggerKey { get; set; } = "MiddleMouse";
        public string TriggerMode { get; set; } = "Toggle"; // "Toggle" or "Hold"
        public bool IsEnabled { get; set; } = true;
        public bool IsSyncScrollEnabled { get; set; } = false;
        public bool IsReadingModeEnabled { get; set; } = false;
        public bool IsWhitelistMode { get; set; } = false; // New: Whitelist Mode
        public string ToggleHotkey { get; set; } = "Ctrl+Alt+S"; // New: Custom Hotkey
        public bool IsDarkMode { get; set; } = false; // Dark Mode
        public bool StartupEnabled { get; set; } = false; // Auto-start on boot
        public PerformanceMode PerformanceMode { get; set; } = PerformanceMode.Balanced; // Performance mode
        
        // 阅读模式设置
        public float ReadingModeSpeed { get; set; } = 30.0f; // 默认阅读模式速度 (像素/秒)
        public float ReadingModeMaxSpeed { get; set; } = 500.0f; // 阅读模式最大速度
        
        // 加速度曲线设置
        public AccelerationCurveType AccelerationCurve { get; set; } = AccelerationCurveType.Linear;
        public double AccelerationExponent { get; set; } = 1.5; // 指数曲线的指数值
        public double AccelerationLogBase { get; set; } = 2.0; // 对数曲线的底数
        public double SigmoidMidpoint { get; set; } = 0.5; // S曲线中点
        public double SigmoidSteepness { get; set; } = 8.0; // S曲线陡峭度
        public List<CustomCurvePoint> CustomCurvePoints { get; set; } = new List<CustomCurvePoint>
        {
            new CustomCurvePoint(0.0, 0.0),
            new CustomCurvePoint(0.25, 0.15),
            new CustomCurvePoint(0.5, 0.4),
            new CustomCurvePoint(0.75, 0.7),
            new CustomCurvePoint(1.0, 1.0)
        };
        
        // 高级参数
        public double Friction { get; set; } = 5.0; // 摩擦系数 (惯性衰减)
        public double InertiaMultiplier { get; set; } = 1.0; // 惯性大小乘数
        public double ResponseTime { get; set; } = 0.04; // 响应时间
        public double AxisLockRatio { get; set; } = 1.8; // 轴锁定比例
        public int SoftStartRange { get; set; } = 12; // 软启动范围
        public bool ShowAdvancedSettings { get; set; } = false; // 是否显示高级设置
        
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
    }
}
