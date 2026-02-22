using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace FlowWheel.Core
{
    public class WindowManager
    {
        private readonly Dictionary<string, AppProfile> _appProfiles;
        
        // Simple cache to avoid repeated Process lookups
        // PID -> ProcessName
        private readonly Dictionary<uint, string> _processCache = new Dictionary<uint, string>();
        private readonly Dictionary<uint, DateTime> _cacheTime = new Dictionary<uint, DateTime>();

        public WindowManager()
        {
            // Initial load of config
            _appProfiles = new Dictionary<string, AppProfile>(StringComparer.OrdinalIgnoreCase);
            SyncProfiles();
        }

        public void SyncProfiles()
        {
            _appProfiles.Clear();
            foreach (var item in ConfigManager.Current.AppProfiles)
            {
                _appProfiles[item.ProcessName] = item;
            }
        }

        public void AddProfile(string processName)
        {
            if (!_appProfiles.ContainsKey(processName))
            {
                var profile = new AppProfile { ProcessName = processName };
                _appProfiles.Add(processName, profile);
                ConfigManager.Current.AppProfiles.Add(profile);
                ConfigManager.Save();
            }
        }

        public void RemoveProfile(string processName)
        {
            if (_appProfiles.ContainsKey(processName))
            {
                _appProfiles.Remove(processName);
                ConfigManager.Current.AppProfiles.RemoveAll(p => p.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));
                ConfigManager.Save();
            }
        }

        public AppProfile? GetProfile(string processName)
        {
            if (string.IsNullOrEmpty(processName)) return null;
            if (_appProfiles.TryGetValue(processName, out var profile))
            {
                return profile;
            }
            return null;
        }

        // Returns true if blocked (Blacklist mode) or NOT allowed (Whitelist mode)
        // Returns the active profile if any (for settings override)
        public (bool isBlocked, AppProfile? profile) CheckProcessState(NativeMethods.POINT pt)
        {
            IntPtr hWnd = NativeMethods.WindowFromPoint(pt);
            if (hWnd == IntPtr.Zero) return (false, null);

            NativeMethods.GetWindowThreadProcessId(hWnd, out uint pid);

            string? processName = GetProcessName(pid);
            
            if (string.IsNullOrEmpty(processName)) return (false, null);

            var profile = GetProfile(processName);
            bool exists = profile != null;

            if (ConfigManager.Current.IsWhitelistMode)
            {
                // Whitelist Mode: Block everything NOT in the list
                // EXCEPT FlowWheel itself, which should always be ignored/handled
                if (processName.Equals("flowwheel", StringComparison.OrdinalIgnoreCase)) return (true, null); // Block self
                
                // If in list -> Allowed (Not Blocked)
                // If not in list -> Blocked
                return (!exists, profile);
            }
            else
            {
                // Blacklist Mode: Block only what IS in the list
                return (exists, profile);
            }
        }

        // Legacy wrapper
        public bool IsBlacklisted(NativeMethods.POINT pt)
        {
            return CheckProcessState(pt).isBlocked;
        }

        private string? GetProcessName(uint pid)
        {
            // Check cache (valid for 5 seconds to handle PID reuse reasonably well without thrashing)
            if (_processCache.TryGetValue(pid, out string? cachedName))
            {
                if ((DateTime.Now - _cacheTime[pid]).TotalSeconds < 5)
                {
                    return cachedName;
                }
            }

            try
            {
                // Note: Process.GetProcessById is somewhat heavy. 
                // For a high-performance production app, we might use QueryFullProcessImageName via P/Invoke.
                // But for now, this is sufficient.
                using (var process = Process.GetProcessById((int)pid))
                {
                    string name = process.ProcessName;
                    _processCache[pid] = name;
                    _cacheTime[pid] = DateTime.Now;
                    return name;
                }
            }
            catch
            {
                // Process might have exited or access denied
                return null;
            }
        }
    }
}
