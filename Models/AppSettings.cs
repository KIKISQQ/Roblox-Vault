using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RobloxVault.Models
{
    public class AppSettings : INotifyPropertyChanged
    {
        private bool _multiRobloxEnabled;
        private bool _showMultiRobloxWarning;
        private bool _hideAccountNames;
        private bool _hideLaunchInfo;
        private bool _displayRobux;
        private bool _fpsCapEnabled;
        private int  _fpsCapValue = 60;
        private bool _antiAfkEnabled;
        private bool _antiAfkMinimize;
        private int  _launchDelayMs = 5000;
        private bool _discordWebhookEnabled;
        private string _discordWebhookUrl = "";
        private bool _discordWebhookEveryone;
        public bool ShowServerInfo   { get; set; } = false;
        public bool ShowPlayerCount  { get; set; } = true;
        public bool ShowServerRegion { get; set; } = true;
        public bool ShowServerAge    { get; set; } = true;
        public int ServerInfoRefreshMs { get; set; } = 5000;

        private string _discordWebhookMessage = "An account has disconnected from Roblox.";


        public int LaunchDelayMs
        {
            get => _launchDelayMs;
            set { _launchDelayMs = value; OnPropertyChanged(); }
        }

        public bool MultiRobloxEnabled
        {
            get => _multiRobloxEnabled;
            set { _multiRobloxEnabled = value; OnPropertyChanged(); }
        }

        public bool ShowMultiRobloxWarning
        {
            get => _showMultiRobloxWarning;
            set { _showMultiRobloxWarning = value; OnPropertyChanged(); }
        }

        public bool HideAccountNames
        {
            get => _hideAccountNames;
            set { _hideAccountNames = value; OnPropertyChanged(); }
        }

        public bool HideLaunchInfo
        {
            get => _hideLaunchInfo;
            set { _hideLaunchInfo = value; OnPropertyChanged(); }
        }

        public bool DisplayRobux
        {
            get => _displayRobux;
            set { _displayRobux = value; OnPropertyChanged(); }
        }

        public bool FpsCapEnabled
        {
            get => _fpsCapEnabled;
            set { _fpsCapEnabled = value; OnPropertyChanged(); }
        }

        public int FpsCapValue
        {
            get => _fpsCapValue;
            set { _fpsCapValue = value; OnPropertyChanged(); }
        }

        public bool AntiAfkEnabled
        {
            get => _antiAfkEnabled;
            set { _antiAfkEnabled = value; OnPropertyChanged(); }
        }

        public bool AntiAfkMinimize
        {
            get => _antiAfkMinimize;
            set { _antiAfkMinimize = value; OnPropertyChanged(); }
        }

        public bool DiscordWebhookEnabled
        {
            get => _discordWebhookEnabled;
            set { _discordWebhookEnabled = value; OnPropertyChanged(); }
        }

        public string DiscordWebhookUrl
        {
            get => _discordWebhookUrl;
            set { _discordWebhookUrl = value; OnPropertyChanged(); }
        }

        public bool DiscordWebhookEveryone
        {
            get => _discordWebhookEveryone;
            set { _discordWebhookEveryone = value; OnPropertyChanged(); }
        }

        public string DiscordWebhookMessage
        {
            get => _discordWebhookMessage;
            set { _discordWebhookMessage = value; OnPropertyChanged(); }
        }

        public string AccentColor { get; set; } = "#6f00ff";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}