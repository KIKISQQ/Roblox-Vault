using System.IO;
using Newtonsoft.Json;
using RobloxVault.Models;

namespace RobloxVault.Services
{
    public class AccountStorageService
    {
        private readonly string _filePath;

        public AccountStorageService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "RobloxVault");
            Directory.CreateDirectory(dir);
            _filePath = Path.Combine(dir, "accounts.json");
        }

        public List<RobloxAccount> Load()
        {
            if (!File.Exists(_filePath)) return new List<RobloxAccount>();
            try
            {
                var json = File.ReadAllText(_filePath);
                return JsonConvert.DeserializeObject<List<RobloxAccount>>(json) ?? new List<RobloxAccount>();
            }
            catch
            {
                return new List<RobloxAccount>();
            }
        }

        public void Save(IEnumerable<RobloxAccount> accounts)
        {
            var json = JsonConvert.SerializeObject(accounts, Formatting.Indented);
            File.WriteAllText(_filePath, json);
        }

        public void Clear()
        {
            if (File.Exists(_filePath))
                File.Delete(_filePath);
        }
    }
}
