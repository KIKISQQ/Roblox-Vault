using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using RobloxVault.Models;
using RobloxVault.Services;
using System.Net.Http;
using System.Text;
using System.Diagnostics;

namespace RobloxVault.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly AccountStorageService _storage         = new();
        private readonly SectionStorageService _sectionStorage  = new();
        private readonly SettingsService       _settingsService = new();
        private readonly RobloxApiService      _api             = new();

        private DiscordService? _discordService;


        private AntiAfkService? _antiAfk;

        private string          _searchText       = "";
        private string          _statusMessage    = "";
        private bool            _isLoading;
        private AccountSection? _selectedSection;
        private AppSettings     _settings         = new();
        private string          _antiAfkCountdown = "";

        public string AntiAfkCountdown
        {
            get => _antiAfkCountdown;
            set { _antiAfkCountdown = value; OnPropertyChanged(); }
        }

        public ObservableCollection<RobloxAccount>  Accounts         { get; } = new();
        public ObservableCollection<RobloxAccount>  FilteredAccounts { get; } = new();
        public ObservableCollection<AccountSection> Sections         { get; } = new();
        public AppSettings Settings
        {
            get => _settings;
            set { _settings = value; OnPropertyChanged(); }
        }

        public AccountSection? SelectedSection
        {
            get => _selectedSection;
            set
            {
                if (_selectedSection != null) _selectedSection.IsSelected = false;
                _selectedSection = value;
                if (_selectedSection != null) _selectedSection.IsSelected = true;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsRLSectionActive));
                ApplyFilter();
            }
        }

        /// <summary>True when the currently active section is the Rogue Lineage template.</summary>
        public bool IsRLSectionActive => _selectedSection?.IsRLTemplate == true;

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); ApplyFilter(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public MainViewModel()
        {
            Settings = _settingsService.Load();
            bool multiEnabled = LaunchService.UpdateMultiRoblox(Settings.MultiRobloxEnabled);
            if (!multiEnabled && Settings.MultiRobloxEnabled && Settings.ShowMultiRobloxWarning)
                StatusMessage = "⚠ Multi-Roblox currently unavailable. Close Roblox and restart the app.";

            LoadSections();
            EnsureRLTemplateSection();
            LoadAccounts();

            if (Settings.AntiAfkEnabled)
                StartAntiAfk();

            StartDisconnectWatcher();
        }

        // ROGUE LINEAGE

        private void EnsureRLTemplateSection()
        {
            if (Sections.Any(s => s.IsRLTemplate)) return;
            var rlSection = new AccountSection { Name = "Rogue Lineage", IsRLTemplate = true };
            Sections.Insert(0, rlSection);
            SaveSections();
        }

        /// <summary>
        /// toggle a single RL tag (MA / PD / WKA / Lannis / Silver) on an account.
        /// </summary>
        public void ToggleRLTag(RobloxAccount acc, string tag)
        {
            acc.ToggleRLTag(tag);
            SaveAccounts();
        }

        public void SetRLDescription(RobloxAccount acc, string rlDescription)
        {
            acc.RLDescription = rlDescription;
            SaveAccounts();
        }

        // anti-afk

        public void StartAntiAfk()
        {
            _antiAfk?.Stop();
            _antiAfk = new AntiAfkService(Settings.AntiAfkMinimize);
            _antiAfk.CountdownTick += remaining =>
            {
                App.Current.Dispatcher.Invoke(() =>
                    AntiAfkCountdown = $"Next Anti-Afk run: {(int)remaining.TotalMinutes}:{remaining.Seconds:D2}");
            };
            _antiAfk.Start();
        }

        public void StopAntiAfk()
        {
            _antiAfk?.Stop();
            _antiAfk = null;
            AntiAfkCountdown = "";
        }

        // -- discord stuff

       public void StartDisconnectWatcher()
        {
            _discordService?.Stop();
            _discordService = new DiscordService(Settings);
            _discordService.Start();
        }

        public void StopDisconnectWatcher()
        {
            _discordService?.Stop();
            _discordService = null;
        }

        private void LoadSections()
        {
            Sections.Clear();
            foreach (var s in _sectionStorage.Load())
                Sections.Add(s);
        }

        private void LoadAccounts()
        {
            Accounts.Clear();
            foreach (var acc in _storage.Load())
                Accounts.Add(acc);
            ApplyFilter();
            _ = RefreshAllPresencesAsync();
        }

        private void ApplyFilter()
        {
            FilteredAccounts.Clear();
            var q = _searchText.Trim().ToLower();

            var sorted = Accounts
                .Where(acc =>
                {
                    if (SelectedSection != null && !SelectedSection.AccountIds.Contains(acc.Id)) return false;
                    if (!string.IsNullOrEmpty(q) &&
                        !acc.DisplayName.ToLower().Contains(q) &&
                        !acc.Username.ToLower().Contains(q)) return false;
                    return true;
                })
                .OrderByDescending(a => a.IsPinned)
                .ThenBy(a => a.DisplayName);

            foreach (var acc in sorted)
                FilteredAccounts.Add(acc);
        }

        public void SaveAccounts()  => _storage.Save(Accounts);
        public void SaveSections()  => _sectionStorage.Save(Sections);
        public void SaveSettings()  => _settingsService.Save(Settings);

        public AccountSection AddSection(string name)
        {
            var section = new AccountSection { Name = name };
            Sections.Add(section);
            SaveSections();
            return section;
        }

        public void DeleteSection(AccountSection section)
        {
            if (section.IsRLTemplate) return;
            if (SelectedSection == section) SelectedSection = null;
            Sections.Remove(section);
            SaveSections();
        }

        public void RenameSection(AccountSection section, string newName)
        {
            section.Name = newName;
            SaveSections();
        }

        public void ClearAllData()
        {
            StopAntiAfk();
            Accounts.Clear();
            FilteredAccounts.Clear();
            Sections.Clear();
            SaveAccounts();
            SaveSections();
            Settings = new AppSettings();
            SaveSettings();
            _storage.Clear();
            _sectionStorage.Clear();
            _settingsService.Clear();
            StatusMessage = "✓ All data cleared";
            EnsureRLTemplateSection();
        }

        public async Task AddAccountAsync(string displayName, string rawCookie)
        {
            IsLoading = true;
            StatusMessage = "Verifying cookie...";
            try
            {
                var cookie = rawCookie.Trim();
                var info = await _api.GetUserInfoAsync(cookie);
                if (info == null)
                {
                    StatusMessage = "❌ Invalid cookie. Please check and try again.";
                    return;
                }

                var existingAccount = Accounts.FirstOrDefault(a => a.UserId == info.Value.userId);
                if (existingAccount != null)
                {
                    existingAccount.DisplayName     = string.IsNullOrWhiteSpace(displayName) ? info.Value.username : displayName;
                    existingAccount.Username        = info.Value.username;
                    existingAccount.EncryptedCookie = EncryptionService.Encrypt(cookie);
                    existingAccount.AvatarUrl       = await _api.GetAvatarUrlAsync(existingAccount.UserId);

                    if (SelectedSection != null && !SelectedSection.AccountIds.Contains(existingAccount.Id))
                    {
                        SelectedSection.AccountIds.Add(existingAccount.Id);
                        SaveSections();
                    }

                    SaveAccounts();
                    ApplyFilter();
                    StatusMessage = $"✓ Updated {existingAccount.DisplayName}";
                    return;
                }

                StatusMessage = "Fetching account info...";
                var acc = new RobloxAccount
                {
                    DisplayName     = string.IsNullOrWhiteSpace(displayName) ? info.Value.username : displayName,
                    Username        = info.Value.username,
                    UserId          = info.Value.userId,
                    EncryptedCookie = EncryptionService.Encrypt(cookie),
                    AvatarUrl       = await _api.GetAvatarUrlAsync(info.Value.userId)
                };

                StatusMessage      = "Fetching presence...";
                acc.PresenceStatus = await _api.GetPresenceAsync(acc.UserId, cookie);

                if (Settings.DisplayRobux)
                {
                    StatusMessage    = "Fetching Robux balance...";
                    acc.RobuxBalance = await _api.GetRobuxBalanceAsync(cookie) ?? -1;
                }

                Accounts.Add(acc);
                if (SelectedSection != null && !SelectedSection.AccountIds.Contains(acc.Id))
                {
                    SelectedSection.AccountIds.Add(acc.Id);
                    SaveSections();
                }
                SaveAccounts();
                ApplyFilter();
                StatusMessage = $"✓ Added {acc.DisplayName}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Error: {ex.Message}";
                MessageBox.Show($"Add account error:\n{ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }

        public void DeleteAccount(RobloxAccount acc)
        {
            foreach (var s in Sections) s.AccountIds.Remove(acc.Id);
            SaveSections();
            Accounts.Remove(acc);
            FilteredAccounts.Remove(acc);
            SaveAccounts();
            StatusMessage = $"Removed {acc.DisplayName}";
        }

        public void EditAccount(RobloxAccount acc, string displayName, string description, List<CustomCard> cards)
        {
            acc.DisplayName  = string.IsNullOrWhiteSpace(displayName) ? acc.Username : displayName;
            acc.Description  = description;
            acc.CustomCards  = new ObservableCollection<CustomCard>(cards);
            SaveAccounts();
        }
        
        public void CopyCookie(RobloxAccount acc)
        {
            Clipboard.SetText(EncryptionService.Decrypt(acc.EncryptedCookie));
            StatusMessage = $"✓ Cookie copied for {acc.DisplayName}";
        }

        public void CopyUsername(RobloxAccount acc)
        {
            Clipboard.SetText(acc.Username);
            StatusMessage = $"✓ Username copied for {acc.DisplayName}";
        }

        public void TogglePin(RobloxAccount acc)
        {
            acc.IsPinned = !acc.IsPinned;
            SaveAccounts();
            ApplyFilter();
        }

        public async Task LaunchAccountAsync(RobloxAccount acc, string placeId)
        {
            try
            {
                if (Settings.MultiRobloxEnabled)
                {
                    bool multiOk = LaunchService.UpdateMultiRoblox(true);
                    if (!multiOk)
                    {
                        if (Settings.ShowMultiRobloxWarning)
                            StatusMessage = "⚠ Multi-Roblox mutex not available. Close Roblox and restart the app.";
                    }
                    else if (Settings.ShowMultiRobloxWarning && LaunchService.IsRobloxRunning())
                        StatusMessage = "⚠ Multi-Roblox running while Roblox is active; this should work but can still be unreliable.";
                }

                if (!Settings.MultiRobloxEnabled)
                {
                    StatusMessage = "Preparing single-session launch, closing existing Roblox instances...";
                    LaunchService.KillRobloxProcesses();
                    await Task.Delay(800);
                }

                StatusMessage = $"Getting auth ticket for {acc.DisplayName}...";
                var cookie = EncryptionService.Decrypt(acc.EncryptedCookie);
                if (string.IsNullOrEmpty(cookie))
                {
                    MessageBox.Show("Cookie is empty or could not be decrypted.", "Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var ticket = await _api.GetAuthTicketAsync(cookie);
                if (ticket == null)
                {
                    StatusMessage = "❌ Failed to get auth ticket. Cookie may be expired.";
                    MessageBox.Show("Could not get an authentication ticket.\n\nYour cookie may be expired — try re-adding the account.", "Launch Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ClientSettingsService.ApplyFpsCap(Settings.FpsCapEnabled, Settings.FpsCapValue);

                StatusMessage = $"Launching {acc.DisplayName} (id={acc.UserId})...";
                var result = await LaunchService.LaunchWithTicketAsync(ticket, placeId, Settings.MultiRobloxEnabled);

                if (!result.success)
                {
                    StatusMessage = $"❌ Launch failed: {result.message}";
                    MessageBox.Show(result.message, "Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (result.pid > 0)
                    _discordService?.RegisterLaunch(acc.DisplayName, result.pid);

                acc.LastPlaceId = placeId;
                SaveAccounts();
                StatusMessage = $"✓ Launched {acc.DisplayName} (id={acc.UserId})";
                if (Settings.ShowServerInfo)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                    var infoWindow = new Views.ServerInfoWindow(acc, () => Settings, placeId);                        
                    infoWindow.Show();
                    });
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Exception: {ex.Message}";
                MessageBox.Show($"Unexpected error during launch:\n\n{ex}", "Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task JoinPlayerAsync(RobloxAccount acc, string username)
        {
            IsLoading = true;
            try
            {
                StatusMessage = $"Looking up {username}...";
                var userId = await _api.GetUserIdByUsernameAsync(username);
                if (userId == null)
                {
                    MessageBox.Show($"Could not find user \"{username}\".", "User Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    StatusMessage = $"❌ User \"{username}\" not found.";
                    return;
                }

                StatusMessage = $"Getting auth ticket for {acc.DisplayName}...";
                var cookie = EncryptionService.Decrypt(acc.EncryptedCookie);
                var ticket = await _api.GetAuthTicketAsync(cookie);
                if (ticket == null)
                {
                    MessageBox.Show("Could not get an authentication ticket.\n\nYour cookie may be expired — try re-adding the account.", "Launch Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    StatusMessage = "❌ Failed to get auth ticket.";
                    return;
                }

                ClientSettingsService.ApplyFpsCap(Settings.FpsCapEnabled, Settings.FpsCapValue);

                StatusMessage = $"Joining {username}'s server...";
                var (success, message) = await LaunchService.JoinPlayerAsync(ticket, userId.Value);

                StatusMessage = success
                    ? $"✓ Joining {username}'s server as {acc.DisplayName}"
                    : $"❌ {message}";

                if (success)
                {
                    await Task.Delay(3000);
                    var newest = System.Diagnostics.Process.GetProcessesByName("RobloxPlayerBeta")
                        .OrderByDescending(p => p.StartTime)
                        .FirstOrDefault();
                    if (newest != null)
                        _discordService?.RegisterLaunch(acc.DisplayName, newest.Id);
                }

                if (!success)
                    MessageBox.Show(message, "Join Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Exception: {ex.Message}";
                MessageBox.Show($"Unexpected error:\n\n{ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }

        public async Task RefreshPresenceAsync(RobloxAccount acc)
        {
            acc.IsLoadingPresence = true;
            try
            {
                var cookie = EncryptionService.Decrypt(acc.EncryptedCookie);
                acc.PresenceStatus = await _api.GetPresenceAsync(acc.UserId, cookie);
                if (Settings.DisplayRobux)
                    acc.RobuxBalance = await _api.GetRobuxBalanceAsync(cookie) ?? -1;
            }
            catch { }
            finally { acc.IsLoadingPresence = false; }
        }

        public async Task RefreshAllPresencesAsync()
        {
            foreach (var acc in Accounts.ToList())
                await RefreshPresenceAsync(acc);
        }


        public async Task OpenInBrowserAsync(RobloxAccount acc)
        {
            try
            {
                var cookie = EncryptionService.Decrypt(acc.EncryptedCookie);
                StatusMessage = $"Opening browser for {acc.DisplayName}...";

                await Task.Run(async () =>
                {
                    var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
                    var browser = await playwright.Chromium.LaunchAsync(new Microsoft.Playwright.BrowserTypeLaunchOptions
                    {
                        Headless = false,
                        Args = new[] { "--start-maximized" }
                    });

                    var context = await browser.NewContextAsync(new Microsoft.Playwright.BrowserNewContextOptions
                    {
                        ViewportSize = Microsoft.Playwright.ViewportSize.NoViewport
                    });

                    await context.AddCookiesAsync(new[]
                    {
                        new Microsoft.Playwright.Cookie
                        {
                            Name   = ".ROBLOSECURITY",
                            Value  = cookie,
                            Domain = ".roblox.com",
                            Path   = "/",
                            Secure = true,
                            HttpOnly = true,
                            SameSite = Microsoft.Playwright.SameSiteAttribute.None
                        }
                    });

                    var page = await context.NewPageAsync();
                    await page.GotoAsync("https://www.roblox.com/home");

                    while (browser.IsConnected)
                    await Task.Delay(1000);
                });

                StatusMessage = $"✓ Browser closed for {acc.DisplayName}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"❌ Browser error: {ex.Message}";
            }
        }


        public ObservableCollection<RobloxAccount> SelectedAccounts { get; } = new();

        public void ToggleSelection(RobloxAccount acc)
        {
            acc.IsSelected = !acc.IsSelected;
            if (acc.IsSelected)
                SelectedAccounts.Add(acc);
            else
                SelectedAccounts.Remove(acc);
            OnPropertyChanged(nameof(HasSelection));
        }

        public void ClearSelection()
        {
            foreach (var acc in SelectedAccounts)
                acc.IsSelected = false;
            SelectedAccounts.Clear();
            OnPropertyChanged(nameof(HasSelection));
        }

        public bool HasSelection => SelectedAccounts.Count > 0;
                
        public async Task<(int imported, List<string> failed)> ImportFromAccountManagerAsync(
            IEnumerable<AccountManagerImportService.ImportEntry> entries)
        {
            int imported = 0;
            var failed   = new List<string>();

            var existingGroupNames = Sections.Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in entries)
            {
                IsLoading     = true;
                StatusMessage = $"Importing {entry.Username}...";
                try
                {
                    var info = await _api.GetUserInfoAsync(entry.Cookie);
                    if (info == null)
                    {
                        failed.Add($"{entry.Username} — invalid or expired cookie");
                        continue;
                    }

                    if (Accounts.Any(a => a.UserId == info.Value.userId))
                    {
                        failed.Add($"{entry.Username} — already exists");
                        continue;
                    }

                    var acc = new RobloxAccount
                    {
                        DisplayName     = entry.DisplayName,
                        Username        = info.Value.username,
                        UserId          = info.Value.userId,
                        EncryptedCookie = EncryptionService.Encrypt(entry.Cookie),
                        Description     = entry.Description
                    };

                    StatusMessage  = $"Fetching avatar for {entry.Username}...";
                    acc.AvatarUrl  = await _api.GetAvatarUrlAsync(acc.UserId);
                    acc.PresenceStatus = await _api.GetPresenceAsync(acc.UserId, entry.Cookie);

                    Accounts.Add(acc);

                    if (!string.IsNullOrWhiteSpace(entry.Group))
                    {
                        if (!existingGroupNames.Contains(entry.Group))
                        {
                            AddSection(entry.Group);
                            existingGroupNames.Add(entry.Group);
                        }
                        var section = Sections.FirstOrDefault(s =>
                            string.Equals(s.Name, entry.Group, StringComparison.OrdinalIgnoreCase));
                        if (section != null && !section.AccountIds.Contains(acc.Id))
                        {
                            section.AccountIds.Add(acc.Id);
                        }
                    }

                    imported++;
                }
                catch (Exception ex)
                {
                    failed.Add($"{entry.Username} — {ex.Message}");
                }
            }

            SaveAccounts();
            SaveSections();
            ApplyFilter();
            IsLoading     = false;
            StatusMessage = $"✓ Import complete — {imported} imported, {failed.Count} failed";
            return (imported, failed);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}