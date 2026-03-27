using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RobloxVault.Services
{
    public class AntiAfkService : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const uint WM_KEYDOWN  = 0x0100;
        private const uint WM_KEYUP    = 0x0101;
        private const int  VK_SPACE    = 0x20;
        private const int  SW_MINIMIZE = 6;

        private CancellationTokenSource? _cts;
        private readonly bool _minimize;

        public event Action<TimeSpan>? CountdownTick;

        public AntiAfkService(bool minimize) => _minimize = minimize;

        public void Start()
        {
            _cts = new CancellationTokenSource();
            _ = RunLoop(_cts.Token);
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts = null;
        }

        private async Task RunLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                var target = DateTime.UtcNow.AddMinutes(15);

                while (!ct.IsCancellationRequested)
                {
                    var remaining = target - DateTime.UtcNow;
                    if (remaining <= TimeSpan.Zero) break;
                    CountdownTick?.Invoke(remaining);
                    try { await Task.Delay(1000, ct); }
                    catch (TaskCanceledException) { return; }
                }

                if (ct.IsCancellationRequested) break;

                foreach (var proc in Process.GetProcessesByName("RobloxPlayerBeta"))
                {
                    var hwnd = proc.MainWindowHandle;
                    if (hwnd == IntPtr.Zero) continue;

                    PostMessage(hwnd, WM_KEYDOWN, (IntPtr)VK_SPACE, IntPtr.Zero);
                    await Task.Delay(50, ct);
                    PostMessage(hwnd, WM_KEYUP, (IntPtr)VK_SPACE, IntPtr.Zero);

                    if (_minimize)
                        ShowWindow(hwnd, SW_MINIMIZE);
                }
            }
        }

        public void Dispose() => Stop();
    }
}