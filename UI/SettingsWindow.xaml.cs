using System;
using System.Windows;
using System.Windows.Controls;
using FlowWheel.Core;

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

            // Init values
            SpeedSlider.Value = _engine.Sensitivity;
            FrictionSlider.Minimum = 5;
            FrictionSlider.Maximum = 50;
            FrictionSlider.Value = _engine.Deadzone;
            
            EnableCheck.IsChecked = _manager.IsEnabled;
            
            // Set initial language selection based on something? 
            // Default is English in ComboBox (Index 0).
            // If we persist settings, we would load it here.
        }

        private void LanguageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageCombo.SelectedItem is ComboBoxItem item && item.Tag is string langCode)
            {
                LanguageManager.SetLanguage(langCode);
            }
        }

        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_engine != null)
            {
                _engine.Sensitivity = (float)e.NewValue;
                if (SpeedValueText != null) SpeedValueText.Text = $"{_engine.Sensitivity:F1}x";
            }
        }

        private void FrictionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_engine != null)
            {
                _engine.Deadzone = (int)e.NewValue;
                if (FrictionValueText != null) FrictionValueText.Text = $"{_engine.Deadzone}px";
            }
        }

        private void EnableCheck_Changed(object sender, RoutedEventArgs e)
        {
            if (_manager != null)
            {
                _manager.IsEnabled = EnableCheck.IsChecked ?? true;
            }
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
