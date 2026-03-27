using System.IO;
using System.Windows;

namespace RobloxVault
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var playwrightDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ms-playwright");

            bool needsDownload = !Directory.Exists(playwrightDir) ||
                                 !Directory.EnumerateDirectories(playwrightDir).Any();

            if (needsDownload)
            {
                var result = MessageBox.Show(
                    "RobloxVault needs to download Chromium (~150MB) to support the \"Open in Browser\" feature.\n\nThis only happens once. Download now?",
                    "First-Time Setup",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    var splash = new Views.DownloadSplash();
                    splash.Show();
                    splash.Activate();
                    splash.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Render);

                    Task.Run(() =>
                    {
                        Microsoft.Playwright.Program.Main(["install", "chromium"]);
                    }).ContinueWith(_ =>
                    {
                        splash.Dispatcher.Invoke(() => splash.Close());
                    });
                }
            }
        }
    }
}