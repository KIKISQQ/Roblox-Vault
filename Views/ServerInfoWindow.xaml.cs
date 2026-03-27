using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RobloxVault.Models;
using RobloxVault.Services;
using System.Net.Http;
using System.Text;

namespace RobloxVault.Views
{
    public partial class ServerInfoWindow : Window
    {
        private readonly RobloxAccount  _acc;
        private readonly Func<AppSettings> _getSettings;
        private AppSettings _settings => _getSettings();
        private readonly RobloxApiService _api = new();
        private static readonly HttpClient _http = new();
        private CancellationTokenSource  _cts  = new();
        private string _placeId = "";

        public ServerInfoWindow(RobloxAccount acc, Func<AppSettings> getSettings, string placeId)
        {
            InitializeComponent();
            _acc         = acc;
            _getSettings = getSettings;
            _placeId     = placeId;

            AccountNameText.Text = $"Account: {acc.DisplayName}";
            _ = PollLoop(_cts.Token);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _cts.Cancel();
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _cts.Cancel();
            base.OnClosed(e);
        }

        private async Task PollLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await RefreshAsync();
                await Task.Delay(_getSettings().ServerInfoRefreshMs, ct).ContinueWith(_ => { });            
                }
        }

        private async Task RefreshAsync()
        {
            try
            {
                var cookie = EncryptionService.Decrypt(_acc.EncryptedCookie);

                var req = new HttpRequestMessage(HttpMethod.Post,
                    "https://presence.roblox.com/v1/presence/users");
                req.Headers.Add("Cookie", $".ROBLOSECURITY={cookie}");
                req.Content = new StringContent(
                    $"{{\"userIds\":[{_acc.UserId}]}}",
                    System.Text.Encoding.UTF8, "application/json");

                var response = await _http.SendAsync(req);
                var json = Newtonsoft.Json.Linq.JObject.Parse(
                    await response.Content.ReadAsStringAsync());

                var presence       = json["userPresences"]?[0];
                var jobId          = presence?["gameId"]?.ToString();
                var currentPlaceId = presence?["placeId"]?.ToString();

                bool inGame = !string.IsNullOrEmpty(jobId);

                Dispatcher.Invoke(() => InfoRows.Children.Clear());

                if (!inGame)
                {
                    Dispatcher.Invoke(() =>
                    {
                        AddRow("Status", "Not in a game", "#55557A");
                        LastUpdatedText.Text = $"Updated {DateTime.Now:HH:mm:ss}";
                    });
                    return;
                }

                var servers = await _api.GetServerInfoAsync(currentPlaceId ?? _placeId, jobId, cookie);

                Dispatcher.Invoke(() =>
                {
                    AddRow("Status", "In Game", "#22C55E");

                    if (_settings.ShowPlayerCount && servers.playerCount >= 0)
                        AddRow("Players", $"{servers.playerCount} / {servers.maxPlayers}");

                    if (_settings.ShowServerRegion && !string.IsNullOrEmpty(servers.region))
                        AddRow("Ping", $"{servers.region} ms");

                    if (_settings.ShowServerAge && servers.created != default)
                        AddRow("Server Age", FormatAge(DateTime.UtcNow - servers.created));

                    LastUpdatedText.Text = $"Updated {DateTime.Now:HH:mm:ss}";
                });
            }
            catch { }
        }

        private void AddRow(string label, string value, string valueHex = "#E0E0EE", bool small = false)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var lbl = new TextBlock
            {
                Text              = label,
                Foreground        = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#55557A")),
                FontSize          = 11,
                VerticalAlignment = VerticalAlignment.Top
            };

            var val = new TextBlock
            {
                Text         = value,
                Foreground   = new SolidColorBrush((Color)ColorConverter.ConvertFromString(valueHex)),
                FontSize     = small ? 10 : 12,
                FontWeight   = FontWeights.Medium,
                TextWrapping = TextWrapping.Wrap
            };

            Grid.SetColumn(lbl, 0);
            Grid.SetColumn(val, 1);
            grid.Children.Add(lbl);
            grid.Children.Add(val);
            InfoRows.Children.Add(grid);
        }

        private void Border_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
                DragMove();
        }

        private static string FormatAge(TimeSpan t)
        {
            if (t.TotalDays >= 1)  return $"{(int)t.TotalDays}d {t.Hours}h";
            if (t.TotalHours >= 1) return $"{(int)t.TotalHours}h {t.Minutes}m";
            return $"{t.Minutes}m";
        }
    }
}