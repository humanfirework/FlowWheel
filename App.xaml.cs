using System;
using System.Drawing;
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
        
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Ensure app doesn't close when settings window is closed
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Initialize Core Components
            ConfigManager.Load();
            _windowManager = new WindowManager();
            _scrollEngine = new ScrollEngine();
            
            // Apply Config
            LanguageManager.SetLanguage(ConfigManager.Current.Language);
            _scrollEngine.Sensitivity = ConfigManager.Current.Sensitivity;
            _scrollEngine.Deadzone = ConfigManager.Current.Deadzone;
            
            try
            {
                _mouseHook = new MouseHook();
                try 
                {
                    _keyboardHook = new KeyboardHook();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to install keyboard hook: {ex.Message}");
                }
                
                // MouseHook events are now handled by AutoScrollManager
                _autoScrollManager = new AutoScrollManager(_mouseHook, _keyboardHook, _scrollEngine, _windowManager);
                _autoScrollManager.IsEnabled = ConfigManager.Current.IsEnabled;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to install hooks: {ex.Message}\nEnsure you have appropriate permissions.", "FlowWheel Error");
                Shutdown();
                return;
            }

            // Initialize Tray Icon
            _notifyIcon = new NotifyIcon();
            LoadTrayIcon();
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "FlowWheel (Running)";
            _notifyIcon.DoubleClick += (s, args) => ShowSettings();

            UpdateTrayMenu();
            LanguageManager.LanguageChanged += (s, args) => UpdateTrayMenu();
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
            if (_scrollEngine == null || _autoScrollManager == null) return;

            if (_settingsWindow == null)
            {
                _settingsWindow = new SettingsWindow(_scrollEngine, _autoScrollManager);
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
