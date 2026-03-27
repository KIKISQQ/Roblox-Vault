using Newtonsoft.Json.Linq;
using RobloxVault.Models;

namespace RobloxVault.Services
{
    public static class AccountManagerImportService
    {
        public record ImportEntry(
            string Cookie,
            string DisplayName,
            string Username,
            string Description,
            string Group
        );

        public static List<ImportEntry> Parse(string json)
        {
            var results = new List<ImportEntry>();
            try
            {
                var arr = JArray.Parse(json);
                foreach (var item in arr)
                {
                    var cookie = item["SecurityToken"]?.ToString();
                    if (string.IsNullOrWhiteSpace(cookie)) continue;

                    var alias       = item["Alias"]?.ToString() ?? "";
                    var username    = item["Username"]?.ToString() ?? "";
                    var description = item["Description"]?.ToString() ?? "";
                    var group       = item["Group"]?.ToString() ?? "";

                    // uses alias as display name if it fails thjen fall back to username
                    var displayName = string.IsNullOrWhiteSpace(alias) ? username : alias;

                    results.Add(new ImportEntry(cookie, displayName, username, description, group));
                }
            }
            catch { }
            return results;
        }
    }
}