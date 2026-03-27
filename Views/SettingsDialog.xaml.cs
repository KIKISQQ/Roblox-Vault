using System.IO;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Newtonsoft.Json;
using RobloxVault.Models;

namespace RobloxVault.Views
{
    public partial class SettingsDialog : Window
    {
        public AppSettings ResultSettings { get; private set; } = new();
        private string _selectedAccent = "#FF6B35";
        private static readonly HttpClient _http = new();

        public SettingsDialog(AppSettings currentSettings)
        {
            InitializeComponent();
            MouseDown += Window_MouseDown;

            MultiRobloxCheckBox.IsChecked        = currentSettings.MultiRobloxEnabled;
            MultiRobloxWarningCheckBox.IsChecked  = currentSettings.ShowMultiRobloxWarning;
            HideAccountNamesCheckBox.IsChecked    = currentSettings.HideAccountNames;
            HideLaunchInfoCheckBox.IsChecked      = currentSettings.HideLaunchInfo;
            DisplayRobuxCheckBox.IsChecked        = currentSettings.DisplayRobux;
            FpsCapCheckBox.IsChecked              = currentSettings.FpsCapEnabled;
            FpsCapTextBox.Text                    = currentSettings.FpsCapValue.ToString();
            LaunchDelayTextBox.Text               = currentSettings.LaunchDelayMs.ToString();
            AntiAfkCheckBox.IsChecked             = currentSettings.AntiAfkEnabled;
            AntiAfkMinimizeCheckBox.IsChecked     = currentSettings.AntiAfkMinimize;

            DiscordWebhookEnabledCheckBox.IsChecked  = currentSettings.DiscordWebhookEnabled;
            DiscordWebhookUrlBox.Text                = currentSettings.DiscordWebhookUrl;
            DiscordEveryoneCheckBox.IsChecked        = currentSettings.DiscordWebhookEveryone;
            DiscordMessageBox.Text                   = currentSettings.DiscordWebhookMessage;
            ServerRefreshBox.Text                    = currentSettings.ServerInfoRefreshMs.ToString();

            ShowServerInfoCheckBox.IsChecked    = currentSettings.ShowServerInfo;
            ShowPlayerCountCheckBox.IsChecked   = currentSettings.ShowPlayerCount;
            ShowServerRegionCheckBox.IsChecked  = currentSettings.ShowServerRegion;
            ShowServerAgeCheckBox.IsChecked     = currentSettings.ShowServerAge;

            _selectedAccent = currentSettings.AccentColor ?? "#FF6B35";
            CustomHexBox.Text = _selectedAccent;
            ApplyPreview(_selectedAccent);
            HighlightSwatch(_selectedAccent);
        }

        private void GeneralTab_Click(object sender, RoutedEventArgs e)
        {
            GeneralPanel.Visibility = Visibility.Visible;
            DiscordPanel.Visibility = Visibility.Collapsed;
            ServerPanel.Visibility  = Visibility.Collapsed;
            GeneralTabButton.Tag    = "active";
            DiscordTabButton.Tag    = "inactive";
            ServerTabButton.Tag     = "inactive";
            UpdateTabStyles();
        }

        private void DiscordTab_Click(object sender, RoutedEventArgs e)
        {
            GeneralPanel.Visibility = Visibility.Collapsed;
            DiscordPanel.Visibility = Visibility.Visible;
            ServerPanel.Visibility  = Visibility.Collapsed;
            GeneralTabButton.Tag    = "inactive";
            DiscordTabButton.Tag    = "active";
            ServerTabButton.Tag     = "inactive";
            UpdateTabStyles();
        }

        private void ServerTab_Click(object sender, RoutedEventArgs e)
        {
            GeneralPanel.Visibility = Visibility.Collapsed;
            DiscordPanel.Visibility = Visibility.Collapsed;
            ServerPanel.Visibility  = Visibility.Visible;
            GeneralTabButton.Tag    = "inactive";
            DiscordTabButton.Tag    = "inactive";
            ServerTabButton.Tag     = "active";
            UpdateTabStyles();
        }

        private void UpdateTabStyles()
        {
            var accent = (SolidColorBrush)Application.Current.Resources["AccentBrush"];
            var dim    = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#484860"));

            GeneralTabText.Foreground = (string?)GeneralTabButton.Tag == "active" ? accent : dim;
            DiscordTabText.Foreground = (string?)DiscordTabButton.Tag == "active" ? accent : dim;
            ServerTabText.Foreground  = (string?)ServerTabButton.Tag  == "active" ? accent : dim;

            GeneralTabUnderline.Visibility = (string?)GeneralTabButton.Tag == "active"
                ? Visibility.Visible : Visibility.Collapsed;
            DiscordTabUnderline.Visibility = (string?)DiscordTabButton.Tag == "active"
                ? Visibility.Visible : Visibility.Collapsed;
            ServerTabUnderline.Visibility  = (string?)ServerTabButton.Tag  == "active"
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void TestWebhook_Click(object sender, RoutedEventArgs e)
        {
            var url = DiscordWebhookUrlBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(url))
            {
                WebhookStatusText.Text       = "⚠ Enter a webhook URL first";
                WebhookStatusText.Foreground = new SolidColorBrush(Colors.OrangeRed);
                return;
            }

            WebhookStatusText.Text       = "Sending...";
            WebhookStatusText.Foreground = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#484860"));

            try
            {
                var everyone = DiscordEveryoneCheckBox.IsChecked ?? false;
                var message  = DiscordMessageBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(message))
                    message = "Test message from RobloxVault.";

                var content = everyone ? $"@everyone {message}" : message;
                var payload = JsonConvert.SerializeObject(new { content });
                var response = await _http.PostAsync(url,
                    new StringContent(payload, Encoding.UTF8, "application/json"));

                if (response.IsSuccessStatusCode)
                {
                    WebhookStatusText.Text       = "✓ Webhook sent successfully";
                    WebhookStatusText.Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#3DBA6F"));
                }
                else
                {
                    WebhookStatusText.Text       = $"✗ Failed — {(int)response.StatusCode}";
                    WebhookStatusText.Foreground = new SolidColorBrush(Colors.OrangeRed);
                }
            }
            catch
            {
                WebhookStatusText.Text       = "✗ Could not reach webhook URL";
                WebhookStatusText.Foreground = new SolidColorBrush(Colors.OrangeRed);
            }
        }

        private void Swatch_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border b && b.Tag is string hex)
            {
                _selectedAccent = hex;
                CustomHexBox.Text = hex;
                ApplyPreview(hex);
                HighlightSwatch(hex);
            }
        }

        private void CustomHexBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var text = CustomHexBox.Text.Trim();
            if (!text.StartsWith("#")) text = "#" + text;
            if (text.Length == 7 && IsValidHex(text))
            {
                _selectedAccent = text;
                ApplyPreview(text);
                HighlightSwatch(text);
            }
        }

        private void ApplyPreview(string hex)
        {
            try
            {
                var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
                AccentPreviewBorder.Background = brush;
                HeaderAccentDot.Background     = brush;
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

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ResultSettings.MultiRobloxEnabled     = MultiRobloxCheckBox.IsChecked ?? false;
            ResultSettings.ShowMultiRobloxWarning  = MultiRobloxWarningCheckBox.IsChecked ?? false;
            ResultSettings.HideAccountNames        = HideAccountNamesCheckBox.IsChecked ?? false;
            ResultSettings.HideLaunchInfo          = HideLaunchInfoCheckBox.IsChecked ?? false;
            ResultSettings.AccentColor             = _selectedAccent;
            ResultSettings.DisplayRobux            = DisplayRobuxCheckBox.IsChecked ?? false;
            ResultSettings.FpsCapEnabled           = FpsCapCheckBox.IsChecked ?? false;
            ResultSettings.AntiAfkEnabled          = AntiAfkCheckBox.IsChecked ?? false;
            ResultSettings.AntiAfkMinimize         = AntiAfkMinimizeCheckBox.IsChecked ?? false;

            ResultSettings.DiscordWebhookEnabled  = DiscordWebhookEnabledCheckBox.IsChecked ?? false;
            ResultSettings.DiscordWebhookUrl      = DiscordWebhookUrlBox.Text.Trim();
            ResultSettings.DiscordWebhookEveryone = DiscordEveryoneCheckBox.IsChecked ?? false;
            ResultSettings.DiscordWebhookMessage  = DiscordMessageBox.Text.Trim();

            ResultSettings.ShowServerInfo   = ShowServerInfoCheckBox.IsChecked ?? false;
            ResultSettings.ShowPlayerCount  = ShowPlayerCountCheckBox.IsChecked ?? false;
            ResultSettings.ShowServerRegion = ShowServerRegionCheckBox.IsChecked ?? false;
            ResultSettings.ShowServerAge    = ShowServerAgeCheckBox.IsChecked ?? false;

            if (!int.TryParse(FpsCapTextBox.Text, out var fps) || fps <= 0)
                fps = 60;
            ResultSettings.FpsCapValue = fps;
            ApplyFpsCap(ResultSettings.FpsCapEnabled, ResultSettings.FpsCapValue);

            if (!int.TryParse(LaunchDelayTextBox.Text, out var delay) || delay < 0)
                delay = 5000;
            ResultSettings.LaunchDelayMs = delay;

            if (!int.TryParse(ServerRefreshBox.Text, out var refreshMs) || refreshMs < 2000)
                refreshMs = 2000;
            ResultSettings.ServerInfoRefreshMs = refreshMs;

            DialogResult = true;
        }

        private static void ApplyFpsCap(bool enabled, int fps)
        {
            try
            {
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var versionsPath = Path.Combine(localAppData, "Roblox", "Versions");
                if (!Directory.Exists(versionsPath)) return;

                foreach (var versionDir in Directory.GetDirectories(versionsPath))
                {
                    var clientSettingsDir = Path.Combine(versionDir, "ClientSettings");
                    var settingsFile      = Path.Combine(clientSettingsDir, "ClientAppSettings.json");
                    Directory.CreateDirectory(clientSettingsDir);

                    Dictionary<string, object> settings = new();
                    if (File.Exists(settingsFile))
                    {
                        try
                        {
                            var existing = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                                File.ReadAllText(settingsFile));
                            if (existing != null) settings = existing;
                        }
                        catch { }
                    }

                    if (enabled)
                        settings["DFIntTaskSchedulerTargetFps"] = fps;
                    else
                        settings.Remove("DFIntTaskSchedulerTargetFps");

                    File.WriteAllText(settingsFile,
                        JsonConvert.SerializeObject(settings, Formatting.Indented));
                }
            }
            catch { }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
        private void CloseButton_Click(object sender, RoutedEventArgs e)  => DialogResult = false;

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }
    }
}