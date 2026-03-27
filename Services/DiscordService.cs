using System.Diagnostics;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using RobloxVault.Models;

// NOTE:
// I wrote this in a rush to get a release out, so it's not the cleanest, but it works fine.
// Feel free to change the implementation as you see fit just keep the same public interface since its used in MainViewModel.cs

namespace RobloxVault.Services
{
    public class DiscordService : IDisposable
    {
        private readonly AppSettings _settings;
        private static readonly HttpClient _http = new();
        private CancellationTokenSource? _cts;

        private readonly HashSet<int> _watchedPids = new();

        // Maps ProcessId -> account display name
        private readonly Dictionary<int, string> _pidToAccount = new();

        public DiscordService(AppSettings settings)
        {
            _settings = settings;
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            _ = WatchLoop(_cts.Token);
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts = null;
            _watchedPids.Clear();
            _pidToAccount.Clear();
        }

        public void RegisterLaunch(string displayName, int pid)
        {
            _pidToAccount[pid] = displayName;
        }
        

        private async Task WatchLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var current = Process.GetProcessesByName("RobloxPlayerBeta");
                    var currentPids = current.Select(p => p.Id).ToHashSet();

                  foreach (var proc in current)
                    {
                        if (_watchedPids.Add(proc.Id))
                        {
                            System.Diagnostics.Debug.WriteLine($"[DiscordService] Now watching pid {proc.Id}");
                            _ = WatchProcess(proc, ct);
                        }
                    }

                    _watchedPids.RemoveWhere(pid => !currentPids.Contains(pid));
                }
                catch { }

                try { await Task.Delay(3000, ct); }
                catch (TaskCanceledException) { break; }
            }
        }

    private async Task WatchProcess(Process proc, CancellationToken ct)
    {
        try
        {
            var pid = proc.Id;
            System.Diagnostics.Debug.WriteLine($"[DiscordService] Waiting for pid {pid} to exit...");

            proc.EnableRaisingEvents = true;
            var tcs = new TaskCompletionSource<bool>();
            proc.Exited += (s, e) => tcs.TrySetResult(true);

            if (proc.HasExited)
                tcs.TrySetResult(true);

            using (ct.Register(() => tcs.TrySetCanceled()))
                await tcs.Task;

            if (ct.IsCancellationRequested) return;

            System.Diagnostics.Debug.WriteLine($"[DiscordService] Pid {pid} exited, sending webhook...");

            if (!_settings.DiscordWebhookEnabled) return;
            if (string.IsNullOrWhiteSpace(_settings.DiscordWebhookUrl)) return;

            _pidToAccount.TryGetValue(pid, out var accountName);
            _pidToAccount.Remove(pid);

            await SendWebhookAsync(accountName);
        }
        catch (TaskCanceledException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DiscordService] WatchProcess error: {ex.Message}");
        }
    }


        private async Task SendWebhookAsync(string? accountName)
        {
            try
            {
                var message = _settings.DiscordWebhookMessage;
                if (string.IsNullOrWhiteSpace(message))
                    message = "A Roblox instance has disconnected.";

                // say account name if we know it
                if (!string.IsNullOrWhiteSpace(accountName))
                    message = $"{message}\n**Account:** {accountName}";

                var content = _settings.DiscordWebhookEveryone
                    ? $"@everyone {message}"
                    : message;

                var payload = JsonConvert.SerializeObject(new { content });
                await _http.PostAsync(
                    _settings.DiscordWebhookUrl,
                    new StringContent(payload, Encoding.UTF8, "application/json"));
            }
            catch { }
        }

        public void Dispose() => Stop();
    }
}