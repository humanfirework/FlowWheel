using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace FlowWheel.Core
{
    public static class ThemeManager
    {
        #region Native Methods for Window Frame Theme
        private enum DwmWindowAttribute : uint
        {
            DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

        [StructLayout(LayoutKind.Sequential)]
        private struct RTL_OSVERSIONINFOEX
        {
            internal uint dwOSVersionInfoSize;
            internal uint dwMajorVersion;
            internal uint dwMinorVersion;
            internal uint dwBuildNumber;
            internal uint dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            internal string szCSDVersion;
        }

        [DllImport("ntdll")]
        private static extern int RtlGetVersion(ref RTL_OSVERSIONINFOEX lpVersionInformation);

        private static Version? _osVersion;
        private static Version GetOSVersion()
        {
            if (_osVersion == null)
            {
                RTL_OSVERSIONINFOEX v = new()
                {
                    dwOSVersionInfoSize = (uint)Marshal.SizeOf<RTL_OSVERSIONINFOEX>()
                };
                if (RtlGetVersion(ref v) == 0)
                {
                    _osVersion = new Version((int)v.dwMajorVersion, (int)v.dwMinorVersion, (int)v.dwBuildNumber);
                }
                else
                {
                    _osVersion = Environment.OSVersion.Version;
                }
            }
            return _osVersion;
        }

        /// <summary>
        /// Sets the window frame theme (dark/light) for the title bar
        /// </summary>
        /// <param name="window">The window to apply the theme to</param>
        /// <param name="darkTheme">True for dark theme, false for light theme</param>
        public static void SetWindowFrameTheme(Window window, bool darkTheme)
        {
            if (GetOSVersion().Build > 22000 && window != null)
            {
                var helper = new WindowInteropHelper(window);
                if (helper.Handle != IntPtr.Zero)
                {
                    SetWindowFrameTheme(helper.Handle, darkTheme);
                }
            }
        }

        /// <summary>
        /// Sets the window frame theme (dark/light) for the title bar
        /// </summary>
        /// <param name="hwnd">The window handle</param>
        /// <param name="darkTheme">True for dark theme, false for light theme</param>
        /// <returns>True if successful</returns>
        public static bool SetWindowFrameTheme(IntPtr hwnd, bool darkTheme)
        {
            if (GetOSVersion().Build > 22000)
            {
                var darkMode = darkTheme ? 1 : 0;
                int result = DwmSetWindowAttribute(hwnd, (int)DwmWindowAttribute.DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));
                return result == 0;
            }
            return false;
        }
        #endregion

        public static void ApplyTheme(bool isDark)
        {
            var appResources = System.Windows.Application.Current.Resources;
            var mergedDictionaries = appResources.MergedDictionaries;

            // Find the existing theme dictionary (if any)
            var themeDictionary = mergedDictionaries.FirstOrDefault(d => 
                d.Source != null && (d.Source.OriginalString.Contains("Themes/Light.xaml") || 
                                     d.Source.OriginalString.Contains("Themes/Dark.xaml")));

            // Create the new theme dictionary
            var newThemeSource = new Uri(isDark ? "Themes/Dark.xaml" : "Themes/Light.xaml", UriKind.Relative);
            var newThemeDictionary = new ResourceDictionary { Source = newThemeSource };

            // Replace or add
            if (themeDictionary != null)
            {
                mergedDictionaries.Remove(themeDictionary);
            }
            mergedDictionaries.Add(newThemeDictionary);
            
            // Update Config
            if (ConfigManager.Current.IsDarkMode != isDark)
            {
                ConfigManager.Current.IsDarkMode = isDark;
                ConfigManager.Save();
            }
        }

        /// <summary>
        /// Applies theme and sets the window frame theme
        /// </summary>
        /// <param name="isDark">True for dark theme</param>
        /// <param name="window">The window to apply frame theme to</param>
        public static void ApplyTheme(bool isDark, Window? window)
        {
            ApplyTheme(isDark);
            if (window != null)
            {
                SetWindowFrameTheme(window, isDark);
            }
        }
    }
}
