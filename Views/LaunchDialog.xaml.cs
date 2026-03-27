using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Newtonsoft.Json.Linq;

namespace RobloxVault.Views
{
    public partial class LaunchDialog : Window
    {
        public string ResultPlaceId  { get; private set; } = "3016661674";
        public string ResultUsername { get; private set; } = "";
        public bool   JoinByPlayer   { get; private set; } = false;

        private static readonly HttpClient _http = new()
        {
            DefaultRequestHeaders = { { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36" } }
        };

        private bool    _searchMode      = false;
        private string? _resolvedPlaceId = null;
        private CancellationTokenSource? _searchCts;

        // cached rolimons game list
        private static Dictionary<string, string>? _gameCache;
        private static bool _gameCacheLoading = false;

        private static readonly Dictionary<string, string> KnownGames = new()
        {
            { "3016661674", "Rogue Lineage" },
            { "4111023553", "Deepwoken"     },
        };

        private static Color AccentColor =>
            ((SolidColorBrush)Application.Current.Resources["AccentBrush"]).Color;
        private readonly Color _dimColor = (Color)ColorConverter.ConvertFromString("#2A2A32");
        private readonly Color _dimText  = (Color)ColorConverter.ConvertFromString("#7A7A8A");

        public LaunchDialog(string accountName)
        {
            InitializeComponent();
            HeaderText.Text = $"Launch — {accountName}";
            MouseDown += (s, e) => { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); };
            HighlightButton(BtnRogueLineage);

            _ = EnsureGameCacheAsync();
        }


        // fetches the rolimons game list once and caches it for the session, we have to use it since the old robloxx game list api was deleted
        private static async Task<Dictionary<string, string>?> EnsureGameCacheAsync()
        {
            if (_gameCache != null) return _gameCache;
            if (_gameCacheLoading) return null;

            _gameCacheLoading = true;
            try
            {
                // rolimons blocks requests with no user-agent
                var request = new HttpRequestMessage(HttpMethod.Get, "https://api.rolimons.com/games/v1/gamelist");
                request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");

                var response = await _http.SendAsync(request);
                var resp = await response.Content.ReadAsStringAsync();

                var json = JObject.Parse(resp);
                var games = json["games"] as JObject;
                if (games == null) return null;

                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in games.Properties())
                {
                    var name = prop.Value[0]?.ToString();
                    if (!string.IsNullOrEmpty(name))
                        dict[prop.Name] = name;
                }
                _gameCache = dict;
                return _gameCache;
            }
            catch
            {
                return null;
            }
            finally
            {
                _gameCacheLoading = false;
            }
        }

        // searches the cache for the best match to a query string.
        // prioritises exact match → starts-with → contains.
        private static (string placeId, string name)? SearchCache(string query)
        {
            if (_gameCache == null) return null;

            var q = query.Trim().ToLower();

            var exact = _gameCache.FirstOrDefault(kv =>
                kv.Value.ToLower() == q);
            if (exact.Key != null) return (exact.Key, exact.Value);

            var startsWith = _gameCache.FirstOrDefault(kv =>
                kv.Value.ToLower().StartsWith(q));
            if (startsWith.Key != null) return (startsWith.Key, startsWith.Value);

    
            var contains = _gameCache.FirstOrDefault(kv =>
                kv.Value.ToLower().Contains(q));
            if (contains.Key != null) return (contains.Key, contains.Value);

            return null;
        }

        private void SearchMode_Click(object sender, RoutedEventArgs e)
        {
            if (_searchMode) ExitSearchMode();
            else             EnterSearchMode();
        }

        private void EnterSearchMode()
        {
            _searchMode      = true;
            _resolvedPlaceId = null;

            PlaceIdLabel.Text         = "GAME NAME";
            PlaceIdBorder.BorderBrush = new SolidColorBrush(AccentColor) { Opacity = 0.5 };

            PlaceIdBox.TextChanged -= PlaceIdBox_TextChanged;
            PlaceIdBox.Text         = "";
            PlaceIdBox.TextChanged += PlaceIdBox_TextChanged;

            PlaceIdPlaceholder.Text       = "Enter a game name...";
            PlaceIdPlaceholder.Visibility = Visibility.Visible;
            GameNameText.Text             = "";

            HighlightButton(BtnSearch);
            foreach (var child in PresetPanel.Children)
                if (child is Button btn && btn != BtnSearch)
                    DimButton(btn);

            PlaceIdBox.Focus();
        }

        private void ExitSearchMode()
        {
            _searchMode      = false;
            _resolvedPlaceId = null;
            _searchCts?.Cancel();

            PlaceIdLabel.Text         = "PLACE ID";
            PlaceIdBorder.BorderBrush = new SolidColorBrush(
                (Color)ColorConverter.ConvertFromString("#252530"));

            PlaceIdBox.TextChanged -= PlaceIdBox_TextChanged;
            PlaceIdBox.Text         = "3016661674";
            PlaceIdBox.TextChanged += PlaceIdBox_TextChanged;

            PlaceIdPlaceholder.Visibility = Visibility.Collapsed;
            GameNameText.Text             = "Rogue Lineage";
            GameNameText.Foreground       = new SolidColorBrush(AccentColor);

            DimButton(BtnSearch);
            HighlightButton(BtnRogueLineage);
        }

        private void PlaceIdBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (PresetPanel == null || GameNameText == null) return;

            var input = PlaceIdBox.Text.Trim();

            PlaceIdPlaceholder.Visibility = string.IsNullOrEmpty(input)
                ? Visibility.Visible : Visibility.Collapsed;

            _searchCts?.Cancel();
            _resolvedPlaceId = null;

            if (string.IsNullOrEmpty(input))
            {
                GameNameText.Text = "";
                if (!_searchMode) ClearAllHighlights();
                return;
            }

            if (_searchMode)
            {
                GameNameText.Text       = _gameCache == null ? "Loading game list..." : "Searching...";
                GameNameText.Foreground = new SolidColorBrush(_dimText);
                _searchCts = new CancellationTokenSource();
                _ = SearchByNameAsync(input, _searchCts.Token);
                return;
            }

            if (!input.All(char.IsDigit))
            {
                GameNameText.Text       = "⚠ Place IDs are numbers only";
                GameNameText.Foreground = new SolidColorBrush(Colors.OrangeRed);
                ClearAllHighlights();
                return;
            }

            if (KnownGames.TryGetValue(input, out var knownName))
            {
                GameNameText.Text       = knownName;
                GameNameText.Foreground = new SolidColorBrush(AccentColor);
                HighlightButtonById(input);
            }
            else if (input.Length > 4)
            {
                GameNameText.Text       = "Looking up...";
                GameNameText.Foreground = new SolidColorBrush(_dimText);
                ClearAllHighlights();
                _ = TryFetchGameNameByIdAsync(input);
            }
            else
            {
                GameNameText.Text = "";
                ClearAllHighlights();
            }
        }


        public bool OpenBrowser { get; private set; } = false;

        private void OpenBrowserButton_Click(object sender, RoutedEventArgs e)
        {
            OpenBrowser = true;
            DialogResult = true;
        }
        
        private async Task SearchByNameAsync(string query, CancellationToken ct)
        {
            try
            {
                await Task.Delay(350, ct);
                if (ct.IsCancellationRequested) return;

                var cache = await EnsureGameCacheAsync();

                if (ct.IsCancellationRequested) return;

                if (cache == null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        GameNameText.Text       = "⚠ Could not load game list";
                        GameNameText.Foreground = new SolidColorBrush(Colors.OrangeRed);
                    });
                    return;
                }

                var result = SearchCache(query);

                Dispatcher.Invoke(() =>
                {
                    if (result.HasValue)
                    {
                        _resolvedPlaceId        = result.Value.placeId;
                        GameNameText.Text       = $"{result.Value.name}  ·  ID {result.Value.placeId}";
                        GameNameText.Foreground = new SolidColorBrush(AccentColor);
                    }
                    else
                    {
                        GameNameText.Text       = "No game found";
                        GameNameText.Foreground = new SolidColorBrush(_dimText);
                    }
                });
            }
            catch (TaskCanceledException) { }
        }

        private async Task TryFetchGameNameByIdAsync(string placeId)
        {
            try
            {
                var url  = $"https://games.roblox.com/v1/games/multiget-place-details?placeIds={placeId}";
                var resp = await _http.GetStringAsync(url);
                var arr  = JArray.Parse(resp);
                var name = arr[0]?["name"]?.ToString();
                if (!string.IsNullOrEmpty(name))
                    Dispatcher.Invoke(() =>
                    {
                        GameNameText.Text       = name;
                        GameNameText.Foreground = new SolidColorBrush(AccentColor);
                    });
            }
            catch { }
        }

        private void UsernameBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var hasText = !string.IsNullOrWhiteSpace(UsernameBox.Text);
            UsernamePlaceholder.Visibility = hasText ? Visibility.Collapsed : Visibility.Visible;
            JoinPlayerButton.IsEnabled     = hasText;
        }

        private void HighlightButtonById(string placeId)
        {
            ClearAllHighlights();
            foreach (var child in PresetPanel.Children)
                if (child is Button btn && btn.Tag?.ToString() == placeId)
                    { HighlightButton(btn); break; }
        }

        private void HighlightButton(Button btn)
        {
            btn.BorderBrush = new SolidColorBrush(AccentColor);
            btn.Foreground  = new SolidColorBrush(AccentColor);
        }

        private void DimButton(Button btn)
        {
            btn.BorderBrush = new SolidColorBrush(_dimColor);
            btn.Foreground  = new SolidColorBrush(_dimText);
        }
        
        private void ClearAllHighlights()
        {
            foreach (var child in PresetPanel.Children)
                if (child is Button btn && btn != BtnSearch)
                    DimButton(btn);
        }

        private void PresetGame_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string placeId)
            {
                if (_searchMode) ExitSearchMode();

                _searchCts?.Cancel();
                _resolvedPlaceId = null;

                PlaceIdBox.TextChanged -= PlaceIdBox_TextChanged;
                PlaceIdBox.Text         = placeId;
                PlaceIdBox.TextChanged += PlaceIdBox_TextChanged;

                PlaceIdPlaceholder.Visibility = Visibility.Collapsed;
                GameNameText.Text             = KnownGames.TryGetValue(placeId, out var n) ? n : "";
                GameNameText.Foreground       = new SolidColorBrush(AccentColor);
                ClearAllHighlights();
                HighlightButton(btn);
            }
        }

        private void JoinPlayerButton_Click(object sender, RoutedEventArgs e)
        {
            var username = UsernameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(username)) return;
            ResultUsername = username;
            JoinByPlayer   = true;
            DialogResult   = true;
        }

        private void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            string id;

            if (_searchMode)
            {
                if (string.IsNullOrEmpty(_resolvedPlaceId))
                {
                    GameNameText.Text       = "⚠ No match found — try a different name or paste the Place ID directly";
                    GameNameText.Foreground = new SolidColorBrush(Colors.OrangeRed);
                    return;
                }
                id = _resolvedPlaceId;
            }
            else
            {
                id = PlaceIdBox.Text.Trim();
                if (string.IsNullOrEmpty(id) || !id.All(char.IsDigit))
                {
                    GameNameText.Text       = "⚠ Enter a valid Place ID (numbers only)";
                    GameNameText.Foreground = new SolidColorBrush(Colors.OrangeRed);
                    return;
                }
            }

            ResultPlaceId = id;
            JoinByPlayer  = false;
            DialogResult  = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
        private void CloseButton_Click(object sender, RoutedEventArgs e)  => DialogResult = false;
    }
}