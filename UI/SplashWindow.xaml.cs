using System.Windows;

namespace FlowWheel.UI
{
    public partial class SplashWindow : Window
    {
        public SplashWindow()
        {
            InitializeComponent();
        }

        public void SetStatus(string text)
        {
            StatusText.Text = text;
        }
    }
}