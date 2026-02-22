using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Reflection;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlowWheel.Core
{
    public class UpdateManager
    {
        private const string GITHUB_API_URL = "https://api.github.com/repos/humanfirework/FlowWheel/releases/latest";

        public class GitHubRelease
        {
            [JsonPropertyName("tag_name")]
            public string TagName { get; set; } = "";

            [JsonPropertyName("html_url")]
            public string HtmlUrl { get; set; } = "";
            
            [JsonPropertyName("body")]
            public string Body { get; set; } = "";
        }

        public static async Task<(bool hasUpdate, string latestVersion, string downloadUrl, string releaseNotes)> CheckForUpdatesAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    // GitHub API requires a User-Agent header
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("FlowWheel-Updater");

                    var response = await client.GetStringAsync(GITHUB_API_URL);
                    var release = JsonSerializer.Deserialize<GitHubRelease>(response);

                    if (release != null)
                    {
                        string latestTag = release.TagName.TrimStart('v');
                        Version latestVersion = Version.Parse(latestTag);
                        Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);

                        if (latestVersion > currentVersion)
                        {
                            return (true, release.TagName, release.HtmlUrl, release.Body);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Update check failed: {ex.Message}");
            }

            return (false, "", "", "");
        }
    }
}
