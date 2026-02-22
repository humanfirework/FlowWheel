using System;
using System.Windows;
using System.Windows.Controls;
using FlowWheel.Core;
using Button = System.Windows.Controls.Button; // Resolve ambiguity

namespace FlowWheel.UI
{
    public partial class SettingsWindow : Window
    {
        private readonly ScrollEngine _engine;
        private readonly AutoScrollManager _manager;

        private WindowManager _windowManager;

        public SettingsWindow(ScrollEngine engine, AutoScrollManager manager, WindowManager windowManager)
        {
            InitializeComponent();
            _engine = engine;
            _manager = manager;
            _windowManager = windowManager;

            // Init Version Text
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            if (version != null)
            {
                VersionText.Text = $"v{version.Major}.{version.Minor}.{version.Build}";
            }
            
            // Auto Check for Update on Startup
            CheckUpdateOnStartup();

            // Init values from Config
            SpeedSlider.Value = ConfigManager.Current.Sensitivity;
            FrictionSlider.Minimum = 5;
            FrictionSlider.Maximum = 50;
            FrictionSlider.Value = ConfigManager.Current.Deadzone;
            
            EnableCheck.IsChecked = ConfigManager.Current.IsEnabled;
            SyncCheck.IsChecked = ConfigManager.Current.IsSyncScrollEnabled;
            ReadingModeCheck.IsChecked = ConfigManager.Current.IsReadingModeEnabled;
            
            // Set Trigger Mode
            if (ConfigManager.Current.TriggerMode == "Hold")
            {
                RadioHoldDrag.IsChecked = true;
            }
            else
            {
                RadioClickToggle.IsChecked = true;
            }

            // Set Trigger Key Selection
            foreach (ComboBoxItem item in TriggerKeyCombo.Items)
            {
                if (item.Tag.ToString() == ConfigManager.Current.TriggerKey)
                {
                    item.IsSelected = true;
                    break;
                }
            }
            
            // Set Whitelist/Blacklist Mode
            if (ConfigManager.Current.IsWhitelistMode)
            {
                RadioWhitelist.IsChecked = true;
                if (FilterModeHelpText != null) FilterModeHelpText.Text = "Only processes in this list will have auto-scroll enabled.";
            }
            else
            {
                RadioBlacklist.IsChecked = true;
                if (FilterModeHelpText != null) FilterModeHelpText.Text = "Processes in this list will be ignored (auto-scroll disabled).";
            }
            
            // Set Custom Hotkey
            HotkeyInput.Text = ConfigManager.Current.ToggleHotkey;

            // Set Language Selection
            foreach (ComboBoxItem item in LanguageCombo.Items)
            {
                if (item.Tag.ToString() == ConfigManager.Current.Language)
                {
                    item.IsSelected = true;
                    break;
                }
            }

            RefreshBlacklist();
        }

        private async void CheckUpdateOnStartup()
        {
            try
            {
                var (hasUpdate, latestVersion, downloadUrl, releaseNotes) = await UpdateManager.CheckForUpdatesAsync();

                if (hasUpdate)
                {
                     // Show notification or small prompt instead of full blocking modal on startup?
                     // For now, let's just use the same logic but maybe non-blocking if we had a toast system.
                     // But user asked for "Check on startup", so a dialog is acceptable if update exists.
                     // To avoid annoyance, maybe only if it's a new update we haven't ignored? 
                     // For simplicity, just show the dialog.
                    var result = System.Windows.MessageBox.Show(
                        $"A new version {latestVersion} is available!\n\nDo you want to download it now?",
                        "Update Available",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = downloadUrl,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch 
            {
                // Silent fail on startup
            }
        }

        private void RefreshBlacklist()
        {
            BlacklistList.ItemsSource = null;
            BlacklistList.ItemsSource = ConfigManager.Current.AppProfiles;
        }

        private void EditProfile_Click(object sender, RoutedEventArgs e)
        {
            // Per-App Settings Removed as requested
            // Keeping empty method for now or removing button from XAML
        }

        private void RadioFilterMode_Changed(object sender, RoutedEventArgs e)
        {
             if (RadioWhitelist == null) return; // Prevent NullReference during InitializeComponent

             if (RadioWhitelist.IsChecked == true)
             {
                 ConfigManager.Current.IsWhitelistMode = true;
                 if (FilterModeHelpText != null) FilterModeHelpText.Text = "Only processes in this list will have auto-scroll enabled.";
             }
             else
             {
                 ConfigManager.Current.IsWhitelistMode = false;
                 if (FilterModeHelpText != null) FilterModeHelpText.Text = "Processes in this list will be ignored (auto-scroll disabled).";
             }
             ConfigManager.Save();
        }

        private void ApplyHotkey_Click(object sender, RoutedEventArgs e)
        {
            // Deprecated button, logic moved to HotkeyInput events
        }
        
        // --- Hotkey Recorder Logic ---
        private bool _isRecordingHotkey = false;
        private string _tempHotkey = "";

        private void HotkeyInput_GotFocus(object sender, RoutedEventArgs e)
        {
            _isRecordingHotkey = true;
            HotkeyInput.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 248, 255)); // Light blue
            HotkeyInput.Text = "Press keys...";
        }

        private void HotkeyInput_LostFocus(object sender, RoutedEventArgs e)
        {
            _isRecordingHotkey = false;
            HotkeyInput.Background = System.Windows.Media.Brushes.White;
            
            // Restore current if cancelled or empty
            if (string.IsNullOrEmpty(_tempHotkey))
            {
                HotkeyInput.Text = ConfigManager.Current.ToggleHotkey;
            }
        }

        private void HotkeyInput_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!_isRecordingHotkey) return;

            e.Handled = true;

            // Get Modifiers
            var modifiers = new System.Collections.Generic.List<string>();
            if ((System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) != 0) modifiers.Add("Ctrl");
            if ((System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Alt) != 0) modifiers.Add("Alt");
            if ((System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) != 0) modifiers.Add("Shift");

            // Get Key
            var key = e.Key;
            if (key == System.Windows.Input.Key.System) key = e.SystemKey;

            // Ignore modifier keys themselves
            if (key == System.Windows.Input.Key.LeftCtrl || key == System.Windows.Input.Key.RightCtrl ||
                key == System.Windows.Input.Key.LeftAlt || key == System.Windows.Input.Key.RightAlt ||
                key == System.Windows.Input.Key.LeftShift || key == System.Windows.Input.Key.RightShift)
            {
                return;
            }

            string keyStr = key.ToString();
            
            // Format
            string result = string.Join("+", modifiers);
            if (!string.IsNullOrEmpty(result)) result += "+";
            result += keyStr;

            _tempHotkey = result;
            HotkeyInput.Text = result;
            
            // Auto Save
            ConfigManager.Current.ToggleHotkey = result;
            ConfigManager.Save();
            
            // Move focus away to finish recording
            // Keyboard.ClearFocus(); // Optional
        }

        private void ClearHotkey_Click(object sender, RoutedEventArgs e)
        {
            ConfigManager.Current.ToggleHotkey = "";
            ConfigManager.Save();
            HotkeyInput.Text = "";
            _tempHotkey = "";
        }

        // --- App Filter Logic (Browse & DragDrop) ---

        private void BrowseApp_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Executables (*.exe)|*.exe|All files (*.*)|*.*",
                Title = "Select Application to Filter"
            };

            if (dialog.ShowDialog() == true)
            {
                AddProcessFromPath(dialog.FileName);
            }
        }

        private void BlacklistList_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                foreach (string file in files)
                {
                    if (file.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        AddProcessFromPath(file);
                    }
                }
            }
        }

        private void AddProcessFromPath(string path)
        {
            try
            {
                string processName = System.IO.Path.GetFileNameWithoutExtension(path);
                
                if (!string.IsNullOrWhiteSpace(processName))
                {
                    _windowManager.AddProfile(processName);
                    RefreshBlacklist();
                }
            }
            catch { }
        }

        private void AddBlacklist_Click(object sender, RoutedEventArgs e)
        {
            string processName = BlacklistInput.Text.Trim();
            if (!string.IsNullOrWhiteSpace(processName))
            {
                if (processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    processName = processName.Substring(0, processName.Length - 4);

                _windowManager.AddProfile(processName);
                BlacklistInput.Text = "";
                RefreshBlacklist();
            }
        }

        private void RemoveBlacklist_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string processName)
            {
                _windowManager.RemoveProfile(processName);
                RefreshBlacklist();
            }
        }

        private void TriggerKeyCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TriggerKeyCombo == null) return; // Safety check

            if (TriggerKeyCombo.SelectedItem is ComboBoxItem item && item.Tag is string key)
            {
                ConfigManager.Current.TriggerKey = key;
                ConfigManager.Save();
            }
        }

        private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageCombo == null) return; // Safety check

            if (LanguageCombo.SelectedItem is ComboBoxItem item && item.Tag is string langCode)
            {
                LanguageManager.SetLanguage(langCode);
                ConfigManager.Current.Language = langCode;
                ConfigManager.Save();
            }
        }

        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_engine != null)
            {
                _engine.Sensitivity = (float)e.NewValue;
                if (SpeedValueText != null) SpeedValueText.Text = $"{_engine.Sensitivity:F1}x";
                
                ConfigManager.Current.Sensitivity = _engine.Sensitivity;
                ConfigManager.Save();
            }
        }

        private void FrictionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_engine != null)
            {
                _engine.Deadzone = (int)e.NewValue;
                if (FrictionValueText != null) FrictionValueText.Text = $"{_engine.Deadzone}px";

                ConfigManager.Current.Deadzone = _engine.Deadzone;
                ConfigManager.Save();
            }
        }

        private void EnableCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (_manager != null)
            {
                _manager.IsEnabled = EnableCheck.IsChecked ?? true;
                ConfigManager.Current.IsEnabled = _manager.IsEnabled;
                ConfigManager.Save();
            }
        }

        private void SyncCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (_engine != null)
            {
                bool val = SyncCheck.IsChecked ?? false;
                _engine.IsSyncEnabled = val;
                ConfigManager.Current.IsSyncScrollEnabled = val;
                ConfigManager.Save();
            }
        }

        private void ReadingModeCheck_Changed(object sender, RoutedEventArgs e)
        {
            ConfigManager.Current.IsReadingModeEnabled = ReadingModeCheck.IsChecked ?? true;
            ConfigManager.Save();
        }

        private void RadioMode_Changed(object sender, RoutedEventArgs e)
        {
            if (RadioHoldDrag == null) return; // Prevent NullReference during InitializeComponent

            if (RadioHoldDrag.IsChecked == true)
            {
                ConfigManager.Current.TriggerMode = "Hold";
            }
            else
            {
                ConfigManager.Current.TriggerMode = "Toggle";
            }
            ConfigManager.Save();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;

            try
            {
                var (hasUpdate, latestVersion, downloadUrl, releaseNotes) = await UpdateManager.CheckForUpdatesAsync();

                if (hasUpdate)
                {
                    var result = System.Windows.MessageBox.Show(
                        $"A new version {latestVersion} is available!\n\nDo you want to download it now?",
                        "Update Available",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = downloadUrl,
                            UseShellExecute = true
                        });
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("You are using the latest version.", "Check for Updates", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to check for updates: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Prevent closing, just hide
            e.Cancel = true;
            this.Hide();
        }
    }
}
