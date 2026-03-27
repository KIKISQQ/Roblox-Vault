using System.Windows;
using System.Windows.Input;

namespace RobloxVault.Views
{
    public partial class RenameSectionDialog : Window
    {
        public string ResultName { get; private set; } = "";

        public RenameSectionDialog(string currentName, bool isNew = false)
        {
            InitializeComponent();
            NameBox.Text = currentName;
            TitleText.Text = isNew ? "New Section" : "Rename Section";
            Loaded += (s, e) => { NameBox.Focus(); NameBox.SelectAll(); };
            ConfirmBtn.Content = isNew ? "Create" : "Save";
            MouseDown += (s, e) => { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); };
            NameBox.KeyDown += (s, e) => { if (e.Key == Key.Enter) Confirm(); };
        }

        private void Confirm()
        {
            ResultName = NameBox.Text.Trim();
            DialogResult = true;
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e) => Confirm();
        private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
        private void CloseButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}