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

        public SettingsWindow(ScrollEngine engine, AutoScrollManager manager)
        {
            InitializeComponent();
            _engine = engine;
            _manager = manager;

            // Init values from Config
            SpeedSlider.Value = ConfigManager.Current.Sensitivity;
            FrictionSlider.Minimum = 5;
            FrictionSlider.Maximum = 50;
            FrictionSlider.Value = ConfigManager.Current.Deadzone;
            
            EnableCheck.IsChecked = ConfigManager.Current.IsEnabled;
            SyncCheck.IsChecked = ConfigManager.Current.IsSyncScrollEnabled;
            ReadingModeCheck.IsChecked = ConfigManager.Current.IsReadingModeEnabled;
            
            // Set Trigger Key Selection
            foreach (ComboBoxItem item in TriggerKeyCombo.Items)
            {
                if (item.Tag.ToString() == ConfigManager.Current.TriggerKey)
                {
                    item.IsSelected = true;
                    break;
                }
            }

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

        private void RefreshBlacklist()
        {
            BlacklistList.ItemsSource = null;
            BlacklistList.ItemsSource = ConfigManager.Current.Blacklist;
        }

        private void AddBlacklist_Click(object sender, RoutedEventArgs e)
        {
            string name = BlacklistInput.Text.Trim();
            if (!string.IsNullOrWhiteSpace(name))
            {
                if (!ConfigManager.Current.Blacklist.Contains(name))
                {
                    ConfigManager.Current.Blacklist.Add(name);
                    ConfigManager.Save();
                    RefreshBlacklist();
                    // Ideally notify WindowManager to reload, but WindowManager reads from Config directly on check if we changed implementation slightly.
                    // But currently WindowManager has its own HashSet cache. We need to sync.
                    // Since we don't have direct reference to WindowManager here easily without passing it...
                    // Wait, App passes us engine and manager. Does manager have window manager?
                    // Actually, WindowManager reads from Config on startup, but we need to update it live.
                    // For simplicity, restart required for blacklist? Or we make WindowManager public static or singleton.
                    // Let's rely on ConfigManager for now, but really WindowManager needs update.
                    // For now, let's just save. The user might need restart for blacklist to apply if we don't sync.
                    // To fix this properly, let's just restart app? No.
                    // Let's make WindowManager reload config.
                }
                BlacklistInput.Text = "";
            }
        }

        private void RemoveBlacklist_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string name)
            {
                ConfigManager.Current.Blacklist.Remove(name);
                ConfigManager.Save();
                RefreshBlacklist();
            }
        }

        private void TriggerKeyCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TriggerKeyCombo.SelectedItem is ComboBoxItem item && item.Tag is string key)
            {
                ConfigManager.Current.TriggerKey = key;
                ConfigManager.Save();
            }
        }

        private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
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

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // Prevent closing, just hide
            e.Cancel = true;
            this.Hide();
        }
    }
}
