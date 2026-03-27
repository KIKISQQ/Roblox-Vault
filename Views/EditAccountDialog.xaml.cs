using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RobloxVault.Models;
using System.Collections.ObjectModel;

namespace RobloxVault.Views
{
    public partial class EditAccountDialog : Window
    {
        public string ResultDisplayName { get; private set; } = "";
        public string ResultDescription { get; private set; } = "";
        public ObservableCollection<CustomCard> ResultCards { get; private set; } = new();

        private readonly RobloxAccount _acc;

        public EditAccountDialog(RobloxAccount acc)
        {
            InitializeComponent();
            _acc = acc;
            HeaderText.Text     = $"Edit — {acc.Username}";
            DisplayNameBox.Text = acc.DisplayName;
            DescriptionBox.Text = acc.Description;

            ResultCards = new ObservableCollection<CustomCard>(
            acc.CustomCards.Select(c => new CustomCard
            {
                Id       = c.Id,
                Name     = c.Name,
                Color    = c.Color,
                IsActive = c.IsActive
            }));

            RenderCards();
        }

        // CARDS 

        private void RenderCards()
        {
            CardsPanel.Children.Clear();
            foreach (var card in ResultCards)
            {
                var pill = BuildCardPill(card);
                CardsPanel.Children.Add(pill);
            }
        }

        private Border BuildCardPill(CustomCard card)
        {
            var color = card.Color;

            var text = new TextBlock
            {
                Text       = card.Name,
                FontSize   = 10,
                FontWeight = FontWeights.Bold,
                Padding    = new Thickness(7, 3, 7, 3)
            };

            var border = new Border
            {
                CornerRadius = new CornerRadius(5),
                Margin       = new Thickness(0, 0, 6, 6),
                Cursor       = Cursors.Hand,
                Tag          = card
            };
            border.Child = text;

            ApplyPillStyle(border, text, card);

            // Left click = toggle active
            border.MouseDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left)
                {
                    card.IsActive = !card.IsActive;
                    ApplyPillStyle(border, text, card);
                }
                else if (e.ChangedButton == MouseButton.Right)
                {
                    ShowCardContextMenu(border, card);
                }
            };

            return border;
        }

        private static void ApplyPillStyle(Border border, TextBlock text, CustomCard card)
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(card.Color);
                var brush = new SolidColorBrush(color);

                if (card.IsActive)
                {
                    border.Background   = brush;
                    border.BorderBrush  = brush;
                    border.BorderThickness = new Thickness(1);
                    text.Foreground     = Brushes.White;
                }
                else
                {
                    var dimColor = Color.FromArgb(30, color.R, color.G, color.B);
                    border.Background   = new SolidColorBrush(dimColor);
                    border.BorderBrush  = brush;
                    border.BorderThickness = new Thickness(1);
                    text.Foreground     = brush;
                }
            }
            catch { }
        }

        private void ShowCardContextMenu(Border pill, CustomCard card)
        {
            var menu = new ContextMenu();
            menu.Background = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#1A1A20"));

            var rename = new MenuItem
            {
                Header     = "✏  Rename / Recolor",
                Foreground = Brushes.White
            };
            rename.Click += (s, _) =>
            {
                var dialog = new AddCardDialog(card) { Owner = this };
                if (dialog.ShowDialog() == true)
                {
                    RenderCards();
                }
            };

            var delete = new MenuItem
            {
                Header     = "🗑  Delete",
                Foreground = Brushes.OrangeRed
            };
            delete.Click += (s, _) =>
            {
                ResultCards.Remove(card);
                RenderCards();
            };

            menu.Items.Add(rename);
            menu.Items.Add(new Separator());
            menu.Items.Add(delete);

            pill.ContextMenu = menu;
            pill.ContextMenu.IsOpen = true;
        }

        private void AddCard_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddCardDialog { Owner = this };
            if (dialog.ShowDialog() == true && dialog.ResultCard != null)
            {
                ResultCards.Add(dialog.ResultCard);
                RenderCards();
            }
        }


        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ResultDisplayName = DisplayNameBox.Text.Trim();
            ResultDescription = DescriptionBox.Text.Trim();
            DialogResult      = true;
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
        private void CloseButton_Click(object sender, RoutedEventArgs e)  => DialogResult = false;
    }
}