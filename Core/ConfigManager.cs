using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FlowWheel.Core
{
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
        public List<string> Blacklist { get; set; } = new List<string>
        {
            "flowwheel",
            "csgo", 
            "valorant",
            "dota2",
            "league of legends",
            "overwatch",
            "r5apex"
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
