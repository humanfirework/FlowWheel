using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FlowWheel.Core
{
    public class AppProfile
    {
        public string ProcessName { get; set; } = "";
        public float Sensitivity { get; set; } = 0.5f;
        public int Deadzone { get; set; } = 20;
        public bool UseCustomSettings { get; set; } = false;
        
        public override string ToString() => ProcessName;
    }

    public class AppConfig
    {
        public string Language { get; set; } = "en-US";
        public float Sensitivity { get; set; } = 0.5f;
        public int Deadzone { get; set; } = 20;
        public string TriggerKey { get; set; } = "MiddleMouse";
        public string TriggerMode { get; set; } = "Toggle"; // "Toggle" or "Hold"
        public bool IsEnabled { get; set; } = true;
        public bool IsSyncScrollEnabled { get; set; } = false;
        public bool IsReadingModeEnabled { get; set; } = false;
        public bool IsWhitelistMode { get; set; } = false; // New: Whitelist Mode
        public string ToggleHotkey { get; set; } = "Ctrl+Alt+S"; // New: Custom Hotkey
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
        
        // Legacy Support: Only used for migration if needed, but JSON serializer handles missing properties gracefully.
        // We removed List<string> Blacklist. 
        // Note: Existing config.json might have "Blacklist": ["..."] which will be ignored.
        // We should probably migrate if possible, but for now let's assume fresh start or just overwrite.
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
