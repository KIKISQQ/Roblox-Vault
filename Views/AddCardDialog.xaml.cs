using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RobloxVault.Models;

namespace RobloxVault.Views
{
    public partial class AddCardDialog : Window
    {
        public CustomCard? ResultCard { get; private set; }
        private string _selectedColor = "#8B5CF6";

        private readonly CustomCard? _editCard;

        public AddCardDialog(CustomCard? editCard = null)
        {
            InitializeComponent();
            _editCard = editCard;

            if (editCard != null)
            {
                CardNameBox.Text  = editCard.Name;
                _selectedColor    = editCard.Color;
                HexBox.Text       = editCard.Color;
                ApplyPreview(editCard.Color);
                HighlightSwatch(editCard.Color);
            }
            else
            {
                HexBox.Text = _selectedColor;
                ApplyPreview(_selectedColor);
                HighlightSwatch(_selectedColor);
            }
        }

        private void Swatch_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border b && b.Tag is string hex)
            {
                _selectedColor = hex;
                HexBox.Text    = hex;
                ApplyPreview(hex);
                HighlightSwatch(hex);
            }
        }

        private void HexBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var text = HexBox.Text.Trim();
            if (!text.StartsWith("#")) text = "#" + text;
            if (text.Length == 7 && IsValidHex(text))
            {
                _selectedColor = text;
                ApplyPreview(text);
                HighlightSwatch(text);
            }
        }

        private void ApplyPreview(string hex)
        {
            try
            {
                ColorPreview.Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(hex));
            }
            catch { }
        }

        private void HighlightSwatch(string hex)
        {
            foreach (var child in SwatchPanel.Children)
            {
                if (child is Border outer && outer.Tag is string tag)
                {
                    if (outer.Child is Border inner)
                    {
                        bool match = string.Equals(tag, hex, StringComparison.OrdinalIgnoreCase);
                        inner.BorderThickness = match ? new Thickness(2) : new Thickness(0);
                    }
                }
            }
        }

        private static bool IsValidHex(string hex)
        {
            if (hex.Length != 7 || hex[0] != '#') return false;
            foreach (char c in hex.Substring(1))
                if (!Uri.IsHexDigit(c)) return false;
            return true;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var name = CardNameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                CardNameBox.Focus();
                return;
            }

            if (_editCard != null)
            {
                _editCard.Name  = name;
                _editCard.Color = _selectedColor;
                ResultCard = _editCard;
            }
            else
            {
                ResultCard = new CustomCard
                {
                    Name  = name,
                    Color = _selectedColor
                };
            }

            DialogResult = true;
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}