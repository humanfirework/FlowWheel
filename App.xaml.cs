using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms; // Requires UseWindowsForms=true in csproj
using FlowWheel.Core;
using FlowWheel.UI;
using Application = System.Windows.Application;

namespace FlowWheel
{
    public partial class App : Application
    {
        private NotifyIcon? _notifyIcon;
        private MouseHook? _mouseHook;
        private KeyboardHook? _keyboardHook;
        private ScrollEngine? _scrollEngine;
        private WindowManager? _windowManager;
        private AutoScrollManager? _autoScrollManager;
        private SettingsWindow? _settingsWindow;
        
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            var splash = new SplashWindow();
            splash.Show();

            await Task.Delay(800);

            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            try
            {
                splash.SetStatus("Loading...");
                
                await Task.Run(() => 
                {
                    ConfigManager.Load();
                    _windowManager = new WindowManager();
                    _scrollEngine = new ScrollEngine();
                });

                LanguageManager.SetLanguage(ConfigManager.Current.Language);
                ThemeManager.ApplyTheme(ConfigManager.Current.IsDarkMode);

                if (_scrollEngine != null)
                {
                    _scrollEngine.ApplyConfig(ConfigManager.Current);
                    _scrollEngine.IsSyncEnabled = ConfigManager.Current.IsSyncScrollEnabled;
                    
                    // Set TickRate based on performance mode
                    _scrollEngine.TickRate = ConfigManager.Current.PerformanceMode switch
                    {
                        PerformanceMode.PowerSaver => 30,
                        PerformanceMode.Balanced => 60,
                        PerformanceMode.HighPerformance => 120,
                        _ => 60
                    };
                }

                _mouseHook = new MouseHook();
                try 
                {
                    _keyboardHook = new KeyboardHook();
                }
                catch { }
                
                if (_mouseHook != null && _scrollEngine != null && _windowManager != null)
                {
                    _autoScrollManager = new AutoScrollManager(_mouseHook, _keyboardHook!, _scrollEngine, _windowManager);
                    _autoScrollManager.IsEnabled = ConfigManager.Current.IsEnabled;
                }

                _notifyIcon = new NotifyIcon();
                LoadTrayIcon();
                _notifyIcon.Visible = true;
                _notifyIcon.Text = "FlowWheel";
                _notifyIcon.DoubleClick += (s, args) => ShowSettings();

                UpdateTrayMenu();
                LanguageManager.LanguageChanged += (s, args) => UpdateTrayMenu();

                splash.Close();
            }
            catch (Exception ex)
            {
                splash.Close();
                System.Windows.MessageBox.Show($"Startup Error: {ex.Message}", "FlowWheel Error");
                Shutdown();
            }
        }

        private void LoadTrayIcon()
        {
            if (_notifyIcon == null) return;
            try
            {
                // Try to load app.ico from directory
                string iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (System.IO.File.Exists(iconPath))
                {
                    _notifyIcon.Icon = new Icon(iconPath);
                }
                else
                {
                    // Fallback to embedded icon or System Icon
                    _notifyIcon.Icon = Icon.ExtractAssociatedIcon(System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "") ?? SystemIcons.Application;
                }
            }
            catch
            {
                _notifyIcon.Icon = SystemIcons.Application;
            }
        }

        private void UpdateTrayMenu()
        {
            if (_notifyIcon == null) return;
            
            // Dispose old context menu if exists
            
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add(GetString("TraySettings"), null, (s, args) => ShowSettings());
            contextMenu.Items.Add(GetString("TrayToggle"), null, (s, args) => ToggleEnable());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add(GetString("TrayExit"), null, (s, args) => ExitApp());
            
            _notifyIcon.ContextMenuStrip = contextMenu;
            
            // Update Text
            bool isEnabled = _autoScrollManager?.IsEnabled ?? true;
            _notifyIcon.Text = isEnabled ? GetString("TrayRunning") : GetString("TrayPaused");
        }

        private string GetString(string key)
        {
            try
            {
                return Application.Current.FindResource(key) as string ?? key;
            }
            catch
            {
                return key;
            }
        }

        private void ShowSettings()
        {
            if (_scrollEngine == null || _autoScrollManager == null || _windowManager == null) return;

            if (_settingsWindow == null)
            {
                _settingsWindow = new SettingsWindow(_scrollEngine, _autoScrollManager, _windowManager, _mouseHook, _keyboardHook);
            }
            else
            {
                // Re-create or re-show? Usually re-show is fine, but if it was closed, it might be disposed.
                // SettingsWindow hides on close usually.
                if (!_settingsWindow.IsLoaded)
                {
                    _settingsWindow = new SettingsWindow(_scrollEngine, _autoScrollManager, _windowManager, _mouseHook, _keyboardHook);
                }
            }
            _settingsWindow.Show();
            _settingsWindow.Activate();
        }

        private void ToggleEnable()
        {
            if (_autoScrollManager == null || _notifyIcon == null) return;
            _autoScrollManager.IsEnabled = !_autoScrollManager.IsEnabled;
            ConfigManager.Current.IsEnabled = _autoScrollManager.IsEnabled;
            ConfigManager.Save();
            
            bool isEnabled = _autoScrollManager.IsEnabled;
            _notifyIcon.Text = isEnabled ? GetString("TrayRunning") : GetString("TrayPaused");
            // Only change icon if paused, otherwise revert to App Icon
            if (!isEnabled)
            {
                _notifyIcon.Icon = SystemIcons.Warning;
            }
            else
            {
                LoadTrayIcon();
            }
        }

        private void ExitApp()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            _autoScrollManager?.Dispose(); // Disposes hook and overlay
            _mouseHook?.Dispose();
            _keyboardHook?.Dispose();
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _notifyIcon?.Dispose();
            _autoScrollManager?.Dispose();
            _mouseHook?.Dispose();
            _keyboardHook?.Dispose();
            base.OnExit(e);
        }
    }
}
