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

        private readonly Dictionary<uint, (string name, DateTime timestamp)> _pidCache = new();
        private readonly object _cacheLock = new();

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

            // Use root window to avoid child-window mismatches (e.g. browser tabs, IME windows)
            IntPtr rootHwnd = NativeMethods.GetAncestor(hWnd, NativeMethods.GA_ROOT);
            if (rootHwnd != IntPtr.Zero)
                hWnd = rootHwnd;

            // Filter out invisible windows (e.g. cloaked UWP windows, background shells)
            if (!NativeMethods.IsWindowVisible(hWnd))
                return (false, null);

            // Ignore tool windows / no-activate utility windows (e.g. on-screen keyboards)
            IntPtr exStyle = (IntPtr)NativeMethods.GetWindowLong(hWnd, NativeMethods.GWL_EXSTYLE);
            if (((uint)exStyle.ToInt64() & NativeMethods.WS_EX_TOOLWINDOW) == NativeMethods.WS_EX_TOOLWINDOW)
                return (false, null);

            NativeMethods.GetWindowThreadProcessId(hWnd, out uint pid);
            if (pid == 0) return (false, null);

            string? processName = GetProcessName(pid);
            
            if (string.IsNullOrEmpty(processName)) return (false, null);

            var profile = GetProfile(processName);
            bool exists = profile != null;

            // FlowWheel itself should never be treated as a target window; ignore self in both modes
            if (processName.Equals("flowwheel", StringComparison.OrdinalIgnoreCase))
                return (true, null);

            if (ConfigManager.Current.IsWhitelistMode)
            {
                // Whitelist Mode: Block everything NOT in the list
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

        private string GetProcessName(uint pid)
        {
            lock (_cacheLock)
            {
                if (_pidCache.TryGetValue(pid, out var entry))
                {
                    // Reduce cache TTL to avoid stale PID reuse issues
                    if ((DateTime.Now - entry.timestamp).TotalSeconds < 2)
                        return entry.name;
                    _pidCache.Remove(pid);
                }
            }

            string name = "";
            IntPtr hProcess = IntPtr.Zero;
            try
            {
                // Use PROCESS_QUERY_LIMITED_INFORMATION: safer, works for elevated/system processes
                hProcess = NativeMethods.OpenProcess(
                    NativeMethods.PROCESS_QUERY_LIMITED_INFORMATION,
                    false, pid);
                if (hProcess != IntPtr.Zero)
                {
                    var sb = new System.Text.StringBuilder(512);
                    uint size = (uint)sb.Capacity;
                    if (NativeMethods.QueryFullProcessImageName(hProcess, 0, sb, ref size))
                    {
                        name = System.IO.Path.GetFileNameWithoutExtension(sb.ToString());
                    }
                }
            }
            catch
            {
                name = "";
            }
            finally
            {
                if (hProcess != IntPtr.Zero)
                    NativeMethods.CloseHandle(hProcess);
            }

            if (string.IsNullOrEmpty(name))
                name = "unknown";

            lock (_cacheLock)
            {
                _pidCache[pid] = (name, DateTime.Now);
            }

            return name;
        }
    }
}
