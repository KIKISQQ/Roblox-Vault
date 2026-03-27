using System.Windows;
using System.Windows.Input;

namespace RobloxVault.Views
{
    public partial class AddAccountDialog : Window
    {
        public string ResultDisplayName { get; private set; } = "";
        public string ResultCookie { get; private set; } = "";

        public AddAccountDialog()
        {
            InitializeComponent();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var cookie = CookieBox.Text.Trim();
            if (string.IsNullOrEmpty(cookie))
            {
                HintText.Text = "⚠ Cookie is required.";
                HintText.Foreground = System.Windows.Media.Brushes.OrangeRed;
                return;
            }
            ResultDisplayName = DisplayNameBox.Text.Trim();
            ResultCookie = cookie;
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
        private void CloseButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}
