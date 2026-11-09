using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace Mnotas
{
    public static class UpdateChecker
    {
        private const string RepoOwner = "montyxace-sys";
        private const string RepoName = "Mnotas";
        private const string ReleasesUrl = "https://api.github.com/repos/" + RepoOwner + "/" + RepoName + "/releases/latest";

        public static Version CurrentVersion =>
            System.Reflection.Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 2, 1);

        public static async Task<UpdateInfo> CheckAsync()
        {
            try
            {
                using var http = new HttpClient();
                http.DefaultRequestHeaders.Add("User-Agent", "Mnotas-UpdateChecker");
                var json = await http.GetStringAsync(ReleasesUrl);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var tag = root.GetProperty("tag_name").GetString() ?? "";
                var versionStr = tag.TrimStart('v', 'V');
                if (!Version.TryParse(versionStr, out var latest)) return null;

                var htmlUrl = root.GetProperty("html_url").GetString() ?? "";
                var body = root.TryGetProperty("body", out var b) ? (b.GetString() ?? "") : "";

                // Find the exe asset
                string downloadUrl = htmlUrl;
                if (root.TryGetProperty("assets", out var assets))
                {
                    foreach (var a in assets.EnumerateArray())
                    {
                        var name = a.GetProperty("name").GetString() ?? "";
                        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                        {
                            downloadUrl = a.GetProperty("browser_download_url").GetString() ?? downloadUrl;
                            break;
                        }
                    }
                }

                return new UpdateInfo
                {
                    LatestVersion = latest,
                    CurrentVersion = CurrentVersion,
                    IsUpdateAvailable = latest > CurrentVersion,
                    ReleaseUrl = htmlUrl,
                    DownloadUrl = downloadUrl,
                    Notes = body
                };
            }
            catch
            {
                return null;
            }
        }

        public static void ShowUpdateDialog(UpdateInfo info, Window owner = null)
        {
            if (info == null || !info.IsUpdateAvailable) return;
            var msg = $"Hay una nueva versión disponible: {info.LatestVersion}\n" +
                      $"Tu versión actual: {info.CurrentVersion}\n\n" +
                      "¿Quieres descargarla ahora?";
            var result = MessageBox.Show(msg, "Actualización disponible", MessageBoxButton.YesNo, MessageBoxImage.Information);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(info.DownloadUrl) { UseShellExecute = true });
                }
                catch { }
            }
        }
    }

    public class UpdateInfo
    {
        public Version LatestVersion { get; set; }
        public Version CurrentVersion { get; set; }
        public bool IsUpdateAvailable { get; set; }
        public string ReleaseUrl { get; set; }
        public string DownloadUrl { get; set; }
        public string Notes { get; set; }
    }
}
