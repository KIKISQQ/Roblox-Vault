using System.IO;
using Newtonsoft.Json;
using RobloxVault.Models;

namespace RobloxVault.Services
{
    public class SectionStorageService
    {
        private readonly string _filePath;

        public SectionStorageService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "RobloxVault");
            Directory.CreateDirectory(dir);
            _filePath = Path.Combine(dir, "sections.json");
        }

        public List<AccountSection> Load()
        {
            if (!File.Exists(_filePath)) return new List<AccountSection>();
            try
            {
                var json = File.ReadAllText(_filePath);
                return JsonConvert.DeserializeObject<List<AccountSection>>(json) ?? new List<AccountSection>();
            }
            catch { return new List<AccountSection>(); }
        }

        public void Save(IEnumerable<AccountSection> sections)
        {
            var json = JsonConvert.SerializeObject(sections, Formatting.Indented);
            File.WriteAllText(_filePath, json);
        }

        public void Clear()
        {
            if (File.Exists(_filePath))
                File.Delete(_filePath);
        }
    }
}
