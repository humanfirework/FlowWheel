using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using FlowWheel.Core;
using Markdig;
using Microsoft.Web.WebView2.Core;

namespace FlowWheel.UI
{
    public partial class UpdateWindow : Window
    {
        private readonly UpdateManager.UpdateCheckResult _result;

        public bool ShouldDownload { get; private set; }

        public UpdateWindow(UpdateManager.UpdateCheckResult result)
        {
            _result = result;
            InitializeComponent();

            Owner = System.Windows.Application.Current.MainWindow
                    ?? (System.Windows.Application.Current.Windows.Count > 0 ? System.Windows.Application.Current.Windows[0] : null);

            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            VersionInfoText.Text =
                $"{_result.LatestTag}  ·  Current: v{_result.CurrentVersion.Major}.{_result.CurrentVersion.Minor}.{_result.CurrentVersion.Build}";

            if (!string.IsNullOrWhiteSpace(_result.ReleaseNotes))
            {
                await RenderMarkdownAsync(_result.ReleaseNotes);
            }
            else
            {
                ShowFallback("No release notes.");
            }
        }

        private async Task RenderMarkdownAsync(string markdown)
        {
            try
            {
                NotesBrowser.DefaultBackgroundColor = System.Drawing.Color.Transparent;
                await NotesBrowser.EnsureCoreWebView2Async();

                NotesBrowser.CoreWebView2.NavigationStarting += OnNavigationStarting;
                NotesBrowser.CoreWebView2.NewWindowRequested += OnNewWindowRequested;

                string html = BuildHtml(markdown);
                NotesBrowser.NavigateToString(html);
                NotesBrowser.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"WebView2 init failed: {ex}");
                ShowFallback(markdown);
            }
        }

        private void ShowFallback(string text)
        {
            NotesBrowser.Visibility = Visibility.Collapsed;
            FallbackText.Text = text;
            FallbackScroll.Visibility = Visibility.Visible;
        }

        private string BuildHtml(string markdown)
        {
            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UsePipeTables()
                .Build();

            string body = Markdown.ToHtml(markdown, pipeline);
            bool isDark = ConfigManager.Current.IsDarkMode;

            string bg = isDark ? "#1E1E2E" : "#FFFFFF";
            string fg = isDark ? "#FFFFFF" : "#1A1A2E";
            string secondary = isDark ? "#B0B0C0" : "#666680";
            string accent = isDark ? "#4DA6FF" : "#007ACC";
            string codeBg = isDark ? "#252535" : "#F8F8FA";
            string border = isDark ? "#3A3A4A" : "#E0E0E8";

            return $@"<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'>
<meta http-equiv='X-UA-Compatible' content='IE=edge'>
<base href='https://github.com/humanfirework/FlowWheel/' target='_blank'>
<style>
body {{ font-family: 'Segoe UI', 'Microsoft YaHei UI', sans-serif; font-size: 13px; line-height: 1.6; color: {fg}; background: {bg}; padding: 12px 14px; margin: 0; }}
h1, h2, h3, h4, h5, h6 {{ color: {fg}; margin: 16px 0 8px 0; font-weight: 600; }}
h1 {{ font-size: 18px; border-bottom: 1px solid {border}; padding-bottom: 6px; }}
h2 {{ font-size: 16px; }}
h3 {{ font-size: 14px; }}
p {{ margin: 0 0 10px 0; }}
a {{ color: {accent}; text-decoration: none; }}
a:hover {{ text-decoration: underline; }}
ul, ol {{ margin: 0 0 10px 20px; padding-left: 8px; }}
li {{ margin-bottom: 4px; }}
code {{ font-family: Consolas, 'Courier New', monospace; background: {codeBg}; padding: 2px 5px; border-radius: 4px; font-size: 12px; }}
pre {{ background: {codeBg}; padding: 10px; border-radius: 6px; overflow-x: auto; border: 1px solid {border}; }}
pre code {{ background: transparent; padding: 0; }}
blockquote {{ border-left: 3px solid {accent}; margin: 0 0 10px 0; padding-left: 12px; color: {secondary}; }}
hr {{ border: 0; border-top: 1px solid {border}; margin: 12px 0; }}
img {{ max-width: 100%; border-radius: 6px; }}
table {{ border-collapse: collapse; margin-bottom: 10px; width: 100%; }}
th, td {{ border: 1px solid {border}; padding: 6px 8px; text-align: left; }}
th {{ background: {codeBg}; font-weight: 600; }}
</style>
</head>
<body>
{body}
</body>
</html>";
        }

        private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Uri) ||
                e.Uri.StartsWith("about:", StringComparison.OrdinalIgnoreCase) ||
                e.Uri.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            e.Cancel = true;
            OpenExternal(e.Uri);
        }

        private void OnNewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
        {
            e.Handled = true;
            OpenExternal(e.Uri);
        }

        private static void OpenExternal(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            ShouldDownload = true;
            DialogResult = true;
            Close();
        }

        private void LaterButton_Click(object sender, RoutedEventArgs e)
        {
            ShouldDownload = false;
            DialogResult = false;
            Close();
        }
    }
}
