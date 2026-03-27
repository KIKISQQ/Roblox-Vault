using System.IO;
using Newtonsoft.Json;
using RobloxVault.Models;

namespace RobloxVault.Services
{
    public class SettingsService
    {
        private readonly string _filePath;

        public SettingsService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "RobloxVault");
            Directory.CreateDirectory(dir);
            _filePath = Path.Combine(dir, "settings.json");
        }

        public AppSettings Load()
        {
            if (!File.Exists(_filePath))
                return new AppSettings
                {
                    MultiRobloxEnabled    = false,
                    ShowMultiRobloxWarning = true,
                    HideAccountNames      = false,
                    HideLaunchInfo        = false
                };

            try
            {
                var json = File.ReadAllText(_filePath);
                return JsonConvert.DeserializeObject<AppSettings>(json) ?? new AppSettings
                {
                    MultiRobloxEnabled    = false,
                    ShowMultiRobloxWarning = true,
                    HideAccountNames      = false,
                    HideLaunchInfo        = false
                };
            }
            catch
            {
                return new AppSettings { MultiRobloxEnabled = false };
            }
        }

        public void Save(AppSettings settings)
        {
            var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(_filePath, json);
        }

        public void Clear()
        {
            if (File.Exists(_filePath))
                File.Delete(_filePath);
        }
    }
}