using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using System.IO;
using RobloxVault.Models;
using RobloxVault.Services;
using RobloxVault.ViewModels;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;

namespace RobloxVault.Views
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm = new();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = _vm;
            _vm.PropertyChanged += Vm_PropertyChanged;
            SectionsList.ItemsSource  = _vm.Sections;
            AccountsPanel.ItemsSource = _vm.FilteredAccounts;
            _vm.FilteredAccounts.CollectionChanged += (s, e) => UpdateEmptyState();
            _vm.Accounts.CollectionChanged += (s, e) => AccountCountBadge.Text = _vm.Accounts.Count.ToString();
            AccountCountBadge.Text = _vm.Accounts.Count.ToString();
            UpdateEmptyState();
            ApplyAccentColor(_vm.Settings.AccentColor ?? "#FF6B35");

            foreach (var s in _vm.Sections)
                s.IsSelected = false;
        }
        
        private void UpdateEmptyState()
        {
            EmptyState.Visibility = _vm.FilteredAccounts.Count == 0
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_vm.StatusMessage) || e.PropertyName == nameof(_vm.Settings))
                StatusBar.Text = _vm.Settings.HideLaunchInfo ? string.Empty : _vm.StatusMessage;
            if (e.PropertyName == nameof(_vm.IsLoading))
            {
                LoadingOverlay.Visibility = _vm.IsLoading ? Visibility.Visible : Visibility.Collapsed;
                LoadingText.Text          = _vm.StatusMessage;
            }
        }

        private static void ApplyAccentColor(string hex)
        {
            try
            {
                var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
                Application.Current.Resources["AccentBrush"] = brush;
            }
            catch { }
        }

        private async void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is not RobloxAccount acc) return;
            var dialog = new LaunchDialog(acc.DisplayName) { Owner = this };
            if (dialog.ShowDialog() != true) return;
            if (dialog.OpenBrowser)
                await _vm.OpenInBrowserAsync(acc);
            else if (dialog.JoinByPlayer)
                await _vm.JoinPlayerAsync(acc, dialog.ResultUsername);
            else
                await _vm.LaunchAccountAsync(acc, dialog.ResultPlaceId);
        }

        private async void AddAccountButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddAccountDialog { Owner = this };
            if (dialog.ShowDialog() == true)
                await _vm.AddAccountAsync(dialog.ResultDisplayName, dialog.ResultCookie);
        }

        private void AccountCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not Border && e.OriginalSource is not TextBlock tb)
                return;
            var source = e.OriginalSource as DependencyObject;
            while (source != null)
            {
                if (source is Button) return;
                source = VisualTreeHelper.GetParent(source);
            }

            if (((FrameworkElement)sender).DataContext is not RobloxAccount acc) return;
            _vm.ToggleSelection(acc);
            UpdateLaunchSelectedVisibility();
        }


        private void CustomCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border border) return;
            if (border.DataContext is not CustomCard card) return;
            e.Handled = true;
            card.IsActive = !card.IsActive;
            ApplyCustomCardStyle(border, card);
            _vm.SaveAccounts();
        }
    

        private void CustomCard_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border border) return;
            if (border.DataContext is not CustomCard card) return;
            e.Handled = true;

            DependencyObject? parent = border;
            RobloxAccount? acc = null;
            while (parent != null)
            {
                parent = VisualTreeHelper.GetParent(parent);
                if (parent is FrameworkElement fe && fe.DataContext is RobloxAccount found)
                {
                    acc = found;
                    break;
                }
            }
            if (acc == null) return;

            var menu = new ContextMenu();
            menu.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A20"));

            var delete = new MenuItem { Header = "🗑  Remove Card", Foreground = System.Windows.Media.Brushes.OrangeRed };
            delete.Click += (s, _) =>
            {
                acc.CustomCards.Remove(card);
                acc.NotifyCardsChanged();
                _vm.SaveAccounts();
            };

            menu.Items.Add(delete);
            border.ContextMenu = menu;
            border.ContextMenu.IsOpen = true;
        }

        private static void ApplyCustomCardStyle(Border border, CustomCard card)
        {
            try
            {
                var color  = (Color)ColorConverter.ConvertFromString(card.Color);
                var brush  = new SolidColorBrush(color);
                var text   = border.Child as TextBlock;

                if (card.IsActive)
                {
                    border.Background      = brush;
                    border.BorderBrush     = brush;
                    if (text != null) text.Foreground = Brushes.White;
                }
                else
                {
                    var dim = Color.FromArgb(30, color.R, color.G, color.B);
                    border.Background      = new SolidColorBrush(dim);
                    border.BorderBrush     = brush;
                    if (text != null) text.Foreground = brush;
                }
            }
            catch { }
        }

        private void CustomCard_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Border border && border.DataContext is CustomCard card)
                ApplyCustomCardStyle(border, card);
        }
        
        private void UpdateLaunchSelectedVisibility()
        {
            LaunchSelectedButton.Visibility = _vm.HasSelection
                ? Visibility.Visible : Visibility.Collapsed;
            LaunchSelectedCount.Text = _vm.SelectedAccounts.Count > 0
                ? $"Launch ({_vm.SelectedAccounts.Count})" : "Launch Selected";
        }

        private async void LaunchSelectedButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_vm.HasSelection) return;

            var first = _vm.SelectedAccounts.First();
            var dialog = new LaunchDialog(
                _vm.SelectedAccounts.Count == 1
                    ? first.DisplayName
                    : $"{_vm.SelectedAccounts.Count} accounts") { Owner = this };

            if (dialog.ShowDialog() != true) return;

            var accounts = _vm.SelectedAccounts.ToList();
            _vm.ClearSelection();
            UpdateLaunchSelectedVisibility();

            foreach (var acc in accounts)
            {
                if (dialog.JoinByPlayer)
                    await _vm.JoinPlayerAsync(acc, dialog.ResultUsername);
                else
                    await _vm.LaunchAccountAsync(acc, dialog.ResultPlaceId);

                if (accounts.Count > 1)
                    await Task.Delay(_vm.Settings.LaunchDelayMs);
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is not RobloxAccount acc) return;

            if (_vm.IsRLSectionActive)
            {
                var dialog = new EditAccountDialog(acc) { Owner = this };
                if (dialog.ShowDialog() == true)
                {
                _vm.EditAccount(acc, dialog.ResultDisplayName, dialog.ResultDescription, dialog.ResultCards.ToList());
                acc.NotifyCardsChanged();
                }
            }
            else
            {
                var dialog = new EditAccountDialog(acc) { Owner = this };
                if (dialog.ShowDialog() == true)
            _vm.EditAccount(acc, dialog.ResultDisplayName, dialog.ResultDescription, dialog.ResultCards.ToList());
            }
        }

        private void CopyCookieButton_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is RobloxAccount acc)
                _vm.CopyCookie(acc);
        }

        private void CopyUsernameButton_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is RobloxAccount acc)
                _vm.CopyUsername(acc);
        }

        private void PinButton_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is RobloxAccount acc)
                _vm.TogglePin(acc);
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is RobloxAccount acc)
                await _vm.RefreshPresenceAsync(acc);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (((FrameworkElement)sender).DataContext is not RobloxAccount acc) return;
            var result = MessageBox.Show(
                $"Remove \"{acc.DisplayName}\" from Vault?\n\nThis cannot be undone.",
                "Remove Account", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
                _vm.DeleteAccount(acc);
        }

        private async void RefreshAllButton_Click(object sender, RoutedEventArgs e)
        {
            StatusBar.Text = "Refreshing all presences...";
            await _vm.RefreshAllPresencesAsync();
            StatusBar.Text = "✓ Presences updated";
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
            => _vm.SearchText = SearchBox.Text;

        private ServerInfoWindow? _serverInfoWindow;


        private async void ImportAccountManagerButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new OpenFileDialog
            {
                Title  = "Select AccountData.json",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
            };
            if (picker.ShowDialog() != true) return;

            string json;
            try   { json = File.ReadAllText(picker.FileName); }
            catch { MessageBox.Show("Could not read the file.", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error); return; }

            var entries = AccountManagerImportService.Parse(json);
            if (entries.Count == 0)
            {
                MessageBox.Show("No valid accounts found in that file.", "Import", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                $"Found {entries.Count} account(s) in the file.\n\nImport them now? Sections will be created automatically from groups.",
                "Import from Account Manager",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            var (imported, failed) = await _vm.ImportFromAccountManagerAsync(entries);

            var summary = $"✓ {imported} account(s) imported successfully.";
            if (failed.Count > 0)
                summary += $"\n\n✗ {failed.Count} failed:\n" + string.Join("\n", failed.Select(f => $"  • {f}"));

            MessageBox.Show(summary, "Import Complete",
                MessageBoxButton.OK,
                failed.Count > 0 ? MessageBoxImage.Warning : MessageBoxImage.Information);

            AccountCountBadge.Text = _vm.Accounts.Count.ToString();
            UpdateEmptyState();
        }


        private void RLTag_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border border) return;
            if (border.DataContext is not RobloxAccount acc) return;
            if (border.Tag is not string tag) return;
            e.Handled = true;
            _vm.ToggleRLTag(acc, tag);
        }


        private void AllAccounts_Click(object sender, MouseButtonEventArgs e)
        {
            _vm.SelectedSection = null;
            foreach (var s in _vm.Sections)
                s.IsSelected = false;
            AllAccountsItem.Background = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#1A1A20"));
        }

        private void SectionItem_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is AccountSection section)
            {
                _vm.SelectedSection = section;
                AllAccountsItem.Background = Brushes.Transparent;
            }
        }

        private void AddSection_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new RenameSectionDialog("", isNew: true) { Owner = this };
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.ResultName))
                _vm.AddSection(dialog.ResultName);
        }

        private void SectionOptions_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not AccountSection section) return;

            var menu = new ContextMenu();
            menu.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A20"));

            var rename = new MenuItem { Header = "✏  Rename", Foreground = System.Windows.Media.Brushes.White };
            rename.Click += (s, _) =>
            {
                var d = new RenameSectionDialog(section.Name) { Owner = this };
                if (d.ShowDialog() == true && !string.IsNullOrWhiteSpace(d.ResultName))
                    _vm.RenameSection(section, d.ResultName);
            };
            menu.Items.Add(rename);

            if (!section.IsRLTemplate)
            {
                var delete = new MenuItem { Header = "🗑  Delete Section", Foreground = System.Windows.Media.Brushes.OrangeRed };
                delete.Click += (s, _) =>
                {
                    var r = MessageBox.Show(
                        $"Delete section \"{section.Name}\"?\nAccounts will NOT be deleted.",
                        "Delete Section", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (r == MessageBoxResult.Yes)
                        _vm.DeleteSection(section);
                };
                menu.Items.Add(new Separator());
                menu.Items.Add(delete);
            }

            btn.ContextMenu = menu;
            btn.ContextMenu.IsOpen = true;
        }

        // SETTINGS 
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SettingsDialog(_vm.Settings) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                _vm.Settings = dialog.ResultSettings;
                ApplyAccentColor(_vm.Settings.AccentColor ?? "#FF6B35");

                if (_vm.Settings.AntiAfkEnabled)
                    _vm.StartAntiAfk();
                else
                    _vm.StopAntiAfk();

                var multiOk = LaunchService.UpdateMultiRoblox(_vm.Settings.MultiRobloxEnabled);
                if (!multiOk && _vm.Settings.MultiRobloxEnabled && _vm.Settings.ShowMultiRobloxWarning)
                    StatusBar.Text = "⚠ Multi-Roblox mutex not available. Close Roblox and restart the app.";
                else
                    StatusBar.Text = "Settings saved";

                _vm.SaveSettings();

                if (_vm.Settings.ShowServerInfo && _serverInfoWindow == null)
                {
                    var acc = _vm.Accounts.FirstOrDefault();
                    if (acc != null)
                    {
                        _serverInfoWindow = new ServerInfoWindow(acc, () => _vm.Settings, acc.LastPlaceId);
                        _serverInfoWindow.Closed += (s, _) => _serverInfoWindow = null;
                        _serverInfoWindow.Show();
                    }
                }
                else if (!_vm.Settings.ShowServerInfo && _serverInfoWindow != null)
                {
                    _serverInfoWindow.Close();
                    _serverInfoWindow = null;
                }
            }
        }

        private void ClearDataButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Delete all saved accounts, sections, and settings? This cannot be undone.",
                "Confirm Clear All Data", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                _vm.ClearAllData();
                AccountsPanel.ItemsSource = _vm.FilteredAccounts;
                _vm.FilteredAccounts.CollectionChanged += (s, ev) => UpdateEmptyState();
                UpdateEmptyState();
                AccountCountBadge.Text = _vm.Accounts.Count.ToString();
                StatusBar.Text = "All local data cleared";
            }
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
            => WindowState = WindowState.Minimized;

        private void CloseButton_Click(object sender, RoutedEventArgs e)
            => Application.Current.Shutdown();
    }
}