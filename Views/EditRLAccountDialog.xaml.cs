using System.Windows;
using System.Windows.Input;
using RobloxVault.Models;

namespace RobloxVault.Views
{
    public partial class EditRLAccountDialog : Window
    {
        public string ResultDisplayName  { get; private set; } = "";
        public string ResultDescription  { get; private set; } = "";
        public string ResultRLDescription { get; private set; } = "";

        public EditRLAccountDialog(RobloxAccount acc)
        {
            InitializeComponent();
            HeaderText.Text      = $"Edit — {acc.Username}";
            DisplayNameBox.Text  = acc.DisplayName;
            DescriptionBox.Text  = acc.Description;
            RLDescriptionBox.Text = acc.RLDescription;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ResultDisplayName   = DisplayNameBox.Text.Trim();
            ResultDescription   = DescriptionBox.Text.Trim();
            ResultRLDescription = RLDescriptionBox.Text.Trim();
            DialogResult = true;
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
        private void CloseButton_Click(object sender, RoutedEventArgs e)  => DialogResult = false;
    }
}
