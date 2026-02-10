using System;
using System.Linq;
using System.Windows;
using Application = System.Windows.Application;

namespace FlowWheel.Core
{
    public static class LanguageManager
    {
        public static event EventHandler? LanguageChanged;

        public static void SetLanguage(string cultureCode)
        {
            var dict = new ResourceDictionary();
            switch (cultureCode)
            {
                case "zh-CN":
                    dict.Source = new Uri("Resources/Languages/zh-CN.xaml", UriKind.Relative);
                    break;
                default:
                    dict.Source = new Uri("Resources/Languages/en-US.xaml", UriKind.Relative);
                    break;
            }

            // Find existing language dictionary and remove it
            // We assume language dict is the one with specific keys, or we track it.
            // Simple way: clear merged dictionaries that look like langs and add new one.
            // But App.xaml might have other resources. 
            // Better: Add to MergedDictionaries. If exists, replace.
            
            // For simplicity in this small app:
            // The Language dictionary will be the LAST one in MergedDictionaries in App.xaml
            
            var appResources = Application.Current.Resources;
            var oldLangDict = appResources.MergedDictionaries.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Languages"));
            
            if (oldLangDict != null)
            {
                appResources.MergedDictionaries.Remove(oldLangDict);
            }
            
            appResources.MergedDictionaries.Add(dict);

            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }
    }
}
