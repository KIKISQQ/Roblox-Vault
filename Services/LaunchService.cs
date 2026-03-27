using System.Diagnostics;
using System.IO;

namespace RobloxVault.Services
{
    public class LaunchService
    {
        private static readonly Random _random = new();

        public static void KillRobloxProcesses()
        {
            var names = new[] { "RobloxPlayerBeta", "RobloxPlayer", "Roblox", "RobloxStudioBeta" };
            foreach (var name in names)
            {
                try
                {
                    foreach (var proc in Process.GetProcessesByName(name))
                    {
                        try
                        {
                            proc.Kill();
                            if (!proc.WaitForExit(4000))
                                proc.Close();
                        }
                        catch { }
                    }
                }
                catch { }
            }
        }

        public static void ClearRobloxLocalData()
        {
            try
            {
                var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                var localPath = Path.Combine(local, "Roblox");
                if (Directory.Exists(localPath))
                    try { Directory.Delete(localPath, true); } catch { }

                var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var roamingPath = Path.Combine(roaming, "Roblox");
                if (Directory.Exists(roamingPath))
                    try { Directory.Delete(roamingPath, true); } catch { }
            }
            catch { }
        }

        private static Mutex? _rbxMultiMutex;

        public static bool UpdateMultiRoblox(bool enabled)
        {
            if (enabled)
            {
                if (_rbxMultiMutex != null) return true;
                try
                {
                    _rbxMultiMutex = new Mutex(true, "ROBLOX_singletonMutex");
                    if (!_rbxMultiMutex.WaitOne(TimeSpan.Zero, true))
                    {
                        _rbxMultiMutex.Dispose();
                        _rbxMultiMutex = null;
                        return false;
                    }
                    return true;
                }
                catch
                {
                    _rbxMultiMutex = null;
                    return false;
                }
            }
            else
            {
                if (_rbxMultiMutex != null)
                {
                    try { _rbxMultiMutex.ReleaseMutex(); }
                    catch { }
                    _rbxMultiMutex.Dispose();
                    _rbxMultiMutex = null;
                }
                return true;
            }
        }

        public static bool IsRobloxRunning()
        {
            var names = new[] { "RobloxPlayerBeta", "RobloxPlayer", "Roblox", "RobloxStudioBeta" };
            foreach (var name in names)
                if (Process.GetProcessesByName(name).Any()) return true;
            return false;
        }

        public static async Task<(bool success, string message, int pid)> LaunchWithTicketAsync(
            string ticket, string placeId, bool multiRobloxEnabled = false)
        {
            try
            {
                var launchTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var browserTrackerId = _random.NextInt64(50_000_000_000L, 70_000_000_000L);

                string placeLauncherUrl;
                if (string.IsNullOrEmpty(placeId))
                    placeLauncherUrl = "https://assetgame.roblox.com/game/PlaceLauncher.ashx?request=RequestGame&placeId=0";
                else
                    placeLauncherUrl = $"https://assetgame.roblox.com/game/PlaceLauncher.ashx?request=RequestGame&placeId={placeId}&isPlayTogetherGame=false";

                var uri = string.Format(
                    "roblox-player:1+launchmode:play+gameinfo:{0}+launchtime:{1}+placelauncherurl:{2}+browsertrackerid:{3}+robloxLocale:en_us+gameLocale:en_us",
                    ticket, launchTime, Uri.EscapeDataString(placeLauncherUrl), browserTrackerId);

                Process.Start(new ProcessStartInfo { FileName = uri, UseShellExecute = true });

                await Task.Delay(3000);

                var newest = Process.GetProcessesByName("RobloxPlayerBeta")
                    .OrderByDescending(p => p.StartTime)
                    .FirstOrDefault();

                return (true, "Roblox launched successfully!", newest?.Id ?? -1);
            }
            catch (Exception ex)
            {
                return (false, $"Failed to launch: {ex.Message}", -1);
            }
        }


        public static Task<(bool success, string message)> JoinPlayerAsync(string ticket, long targetUserId)
        {
            try
            {
                var launchTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var browserTrackerId = _random.NextInt64(50_000_000_000L, 70_000_000_000L);

                // RequestFollowUser lets robloxs backend do the place/server resolution.
                // previously i used RequestGameJob with a placeId + jobid but that fails if a game has a subplace or if the target player is in a paid game.
                var placeLauncherUrl =
                    $"https://assetgame.roblox.com/game/PlaceLauncher.ashx?request=RequestFollowUser&userId={targetUserId}";

                var uri = string.Format(
                    "roblox-player:1+launchmode:play+gameinfo:{0}+launchtime:{1}+placelauncherurl:{2}+browsertrackerid:{3}+robloxLocale:en_us+gameLocale:en_us",
                    ticket, launchTime, Uri.EscapeDataString(placeLauncherUrl), browserTrackerId);

                Process.Start(new ProcessStartInfo { FileName = uri, UseShellExecute = true });
                return Task.FromResult((true, "Joined player's server!"));
            }
            catch (Exception ex)
            {
                return Task.FromResult((false, $"Failed to join: {ex.Message}"));
            }
        }

        public static async Task<long?> GetUserIdAsync(string username)
        {
            try
            {
                using var http = new System.Net.Http.HttpClient();
                http.Timeout = TimeSpan.FromSeconds(5);

                var body = System.Text.Json.JsonSerializer.Serialize(new
                {
                    usernames = new[] { username },
                    excludeBannedUsers = false
                });

                var response = await http.PostAsync(
                    "https://users.roblox.com/v1/usernames/users",
                    new System.Net.Http.StringContent(body, System.Text.Encoding.UTF8, "application/json"));

                var json = await response.Content.ReadAsStringAsync();
                var doc = System.Text.Json.JsonDocument.Parse(json);
                var data = doc.RootElement.GetProperty("data");

                if (data.GetArrayLength() == 0)
                    return null;

                return data[0].GetProperty("id").GetInt64();
            }
            catch
            {
                return null;
            }
        }
    }
}