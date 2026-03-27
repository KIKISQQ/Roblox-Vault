using System.Net.Http;
using Newtonsoft.Json.Linq;
using RobloxVault.Models;
using System.IO;

namespace RobloxVault.Services
{
    public class RobloxApiService
    {
        private readonly HttpClient _http;

        public RobloxApiService()
        {
            _http = new HttpClient();
            _http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
            _http.Timeout = TimeSpan.FromSeconds(15);
        }

        private HttpRequestMessage BuildRequest(HttpMethod method, string url, string cookie)
        {
            var req = new HttpRequestMessage(method, url);
            req.Headers.Add("Cookie", $".ROBLOSECURITY={cookie}");
            return req;
        }

        public async Task<(string username, long userId)?> GetUserInfoAsync(string cookie)
        {
            try
            {
                var req = BuildRequest(HttpMethod.Get, "https://users.roblox.com/v1/users/authenticated", cookie);
                var resp = await _http.SendAsync(req);
                if (!resp.IsSuccessStatusCode) return null;
                var json = JObject.Parse(await resp.Content.ReadAsStringAsync());
                return (json["name"]!.ToString(), json["id"]!.Value<long>());
            }
            catch { return null; }
        }

        public async Task<string?> GetAvatarUrlAsync(long userId)
        {
            try
            {
                var url = $"https://thumbnails.roblox.com/v1/users/avatar-headshot?userIds={userId}&size=150x150&format=Png&isCircular=false";
                var resp = await _http.GetAsync(url);
                if (!resp.IsSuccessStatusCode) return null;
                var json = JObject.Parse(await resp.Content.ReadAsStringAsync());
                return json["data"]?[0]?["imageUrl"]?.ToString();
            }
            catch { return null; }
        }

        public async Task<string> GetPresenceAsync(long userId, string cookie)
        {
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Post, "https://presence.roblox.com/v1/presence/users");
                req.Headers.Add("Cookie", $".ROBLOSECURITY={cookie}");
                req.Content = new StringContent($"{{\"userIds\":[{userId}]}}", System.Text.Encoding.UTF8, "application/json");
                var resp = await _http.SendAsync(req);
                if (!resp.IsSuccessStatusCode) return "Offline";
                var json = JObject.Parse(await resp.Content.ReadAsStringAsync());
                var type = json["userPresences"]?[0]?["userPresenceType"]?.Value<int>() ?? 0;
                return type switch
                {
                    1 => "Online",
                    2 => "In Game",
                    3 => "In Studio",
                    _ => "Offline"
                };
            }
            catch { return "Offline"; }
        }

        public async Task<string?> GetAuthTicketAsync(string cookie, int maxRetries = 5)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                if (i > 0)
                    await Task.Delay(i * 3000);

                try
                {
                    using var http = new HttpClient();
                    http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
                    http.Timeout = TimeSpan.FromSeconds(15);

                    var csrfReq = new HttpRequestMessage(HttpMethod.Post, "https://auth.roblox.com/v2/logout");
                    csrfReq.Headers.Add("Cookie", $".ROBLOSECURITY={cookie}");
                    var csrfResp = await http.SendAsync(csrfReq);

                    if (!csrfResp.Headers.TryGetValues("x-csrf-token", out var tokens)) continue;
                    var csrfToken = tokens.First();

                    var req = new HttpRequestMessage(HttpMethod.Post, "https://auth.roblox.com/v1/authentication-ticket");
                    req.Headers.Add("Cookie", $".ROBLOSECURITY={cookie}");
                    req.Headers.Add("Referer", "https://www.roblox.com");
                    req.Headers.Add("x-csrf-token", csrfToken);
                    req.Content = new StringContent("", System.Text.Encoding.UTF8, "application/json");

                    var resp = await http.SendAsync(req);

                    if ((int)resp.StatusCode == 429) continue;
                    if (!resp.IsSuccessStatusCode) return null;

                    if (resp.Headers.TryGetValues("rbx-authentication-ticket", out var tickets))
                        return tickets.First();
                }
                catch { }
            }

            return null;
        }

        public async Task<long?> GetUserIdByUsernameAsync(string username)
        {
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Post, "https://users.roblox.com/v1/usernames/users");
                req.Content = new StringContent(
                    $"{{\"usernames\":[\"{username}\"],\"excludeBannedUsers\":false}}",
                    System.Text.Encoding.UTF8, "application/json");
                var resp = await _http.SendAsync(req);
                if (!resp.IsSuccessStatusCode) return null;
                var json = JObject.Parse(await resp.Content.ReadAsStringAsync());
                return json["data"]?[0]?["id"]?.Value<long>();
            }
            catch { return null; }
        }

        public async Task<(long placeId, string jobId)?> GetUserGamePresenceAsync(long userId, string cookie)
        {
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Post, "https://presence.roblox.com/v1/presence/users");
                req.Headers.Add("Cookie", $".ROBLOSECURITY={cookie}");
                req.Content = new StringContent(
                    $"{{\"userIds\":[{userId}]}}",
                    System.Text.Encoding.UTF8, "application/json");
                var resp = await _http.SendAsync(req);
                if (!resp.IsSuccessStatusCode) return null;
                var json = JObject.Parse(await resp.Content.ReadAsStringAsync());
                var presence = json["userPresences"]?[0];
                if (presence?["userPresenceType"]?.Value<int>() != 2) return null;
                var placeId = presence?["placeId"]?.Value<long>() ?? 0;
                var jobId = presence?["gameId"]?.ToString() ?? "";
                if (placeId == 0 || string.IsNullOrEmpty(jobId)) return null;
                return (placeId, jobId);
            }
            catch { return null; }
        }

        public async Task<long?> GetRobuxBalanceAsync(string cookie)
        {
            try
            {
                var req = BuildRequest(HttpMethod.Get, "https://economy.roblox.com/v1/user/currency", cookie);
                var resp = await _http.SendAsync(req);
                if (!resp.IsSuccessStatusCode) return null;
                var json = JObject.Parse(await resp.Content.ReadAsStringAsync());
                return json["robux"]?.Value<long>();
            }
            catch { return null; }
        }

        public async Task<string?> GetCurrentJobIdAsync(string cookie, long userId)
        {
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Post, "https://presence.roblox.com/v1/presence/users");
                req.Headers.Add("Cookie", $".ROBLOSECURITY={cookie}");
                req.Content = new StringContent(
                    $"{{\"userIds\":[{userId}]}}",
                    System.Text.Encoding.UTF8, "application/json");

                var response = await _http.SendAsync(req);
                if (!response.IsSuccessStatusCode) return null;

                var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                return json["userPresences"]?[0]?["gameId"]?.ToString();
            }
            catch { return null; }
        }

        public async Task<(int playerCount, int maxPlayers, string region, DateTime created)>
            GetServerInfoAsync(string placeId, string? jobId, string cookie)
        {
            try
            {
                if (string.IsNullOrEmpty(jobId))
                    return (-1, -1, "", default);

                string? cursor = null;
                do
                {
                    var url = $"https://games.roblox.com/v1/games/{placeId}/servers/Public?limit=100"
                            + (cursor != null ? $"&cursor={cursor}" : "");

                    var req = new HttpRequestMessage(HttpMethod.Get, url);
                    req.Headers.Add("Cookie", $".ROBLOSECURITY={cookie}");

                    var response = await _http.SendAsync(req);
                    if (!response.IsSuccessStatusCode) return (-1, -1, "", default);

                    var json = JObject.Parse(await response.Content.ReadAsStringAsync());
                    var servers = json["data"] as JArray;
                    if (servers == null) return (-1, -1, "", default);

                    foreach (var s in servers)
                    {
                        if (s["id"]?.ToString() == jobId)
                        {
                            var players = s["playing"]?.Value<int>() ?? -1;
                            var max     = s["maxPlayers"]?.Value<int>() ?? -1;
                            var ping    = s["ping"]?.Value<int>() ?? -1;
                            return (players, max, ping >= 0 ? $"{ping}" : "", default);
                        }
                    }

                    cursor = json["nextPageCursor"]?.ToString();
                }
                while (!string.IsNullOrEmpty(cursor));

                return (-1, -1, "", default);
            }
            catch { return (-1, -1, "", default); }
        }

        public async Task<bool> LogoutAsync(string cookie)
        {
            if (string.IsNullOrEmpty(cookie)) return false;
            try
            {
                var csrfReq = new HttpRequestMessage(HttpMethod.Post, "https://auth.roblox.com/v2/logout");
                csrfReq.Headers.Add("Cookie", $".ROBLOSECURITY={cookie}");
                var csrfResp = await _http.SendAsync(csrfReq);
                if (!csrfResp.Headers.TryGetValues("x-csrf-token", out var tokens)) return false;
                var csrfToken = tokens.First();

                var req = new HttpRequestMessage(HttpMethod.Post, "https://auth.roblox.com/v2/logout");
                req.Headers.Add("Cookie", $".ROBLOSECURITY={cookie}");
                req.Headers.Add("x-csrf-token", csrfToken);
                req.Headers.Add("Referer", "https://www.roblox.com");
                req.Content = new StringContent("", System.Text.Encoding.UTF8, "application/json");

                var resp = await _http.SendAsync(req);
                return resp.IsSuccessStatusCode;
            }
            catch { return false; }
        }
    }
}