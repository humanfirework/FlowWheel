using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace FlowWheel.Core
{
    public class WindowManager
    {
        private readonly HashSet<string> _blacklist;
        
        // Simple cache to avoid repeated Process lookups
        // PID -> ProcessName
        private readonly Dictionary<uint, string> _processCache = new Dictionary<uint, string>();
        private readonly Dictionary<uint, DateTime> _cacheTime = new Dictionary<uint, DateTime>();

        public WindowManager()
        {
            _blacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "flowwheel", // Don't scroll ourselves (loop safety)
                "csgo", 
                "valorant",
                "dota2",
                "league of legends",
                "overwatch",
                "r5apex"
            };
        }

        public void AddToBlacklist(string processName)
        {
            _blacklist.Add(processName);
        }

        public void RemoveFromBlacklist(string processName)
        {
            _blacklist.Remove(processName);
        }

        public bool IsBlacklisted(NativeMethods.POINT pt)
        {
            IntPtr hWnd = NativeMethods.WindowFromPoint(pt);
            if (hWnd == IntPtr.Zero) return false;

            NativeMethods.GetWindowThreadProcessId(hWnd, out uint pid);

            string? processName = GetProcessName(pid);
            
            if (string.IsNullOrEmpty(processName)) return false;

            return _blacklist.Contains(processName);
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
