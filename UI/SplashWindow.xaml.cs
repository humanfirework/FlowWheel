using System.Reflection;
using System.Windows;

namespace FlowWheel.UI
{
    public partial class SplashWindow : Window
    {
        public SplashWindow()
        {
            InitializeComponent();

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version != null)
            {
                VersionText.Text = $"v{version.Major}.{version.Minor}.{version.Build}";
            }
        }

        public void SetStatus(string text)
        {
            StatusText.Text = text;
        }
    }
}