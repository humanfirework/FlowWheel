using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace FlowWheel.Core
{
    public static class UpdateManager
    {
        private const string GITHUB_API_URL = "https://api.github.com/repos/humanfirework/FlowWheel/releases/latest";

        private static readonly HttpClient _http = CreateHttpClient();

        public sealed class UpdateCheckResult
        {
            public bool HasUpdate { get; init; }
            public string LatestTag { get; init; } = "";
            public Version? LatestVersion { get; init; }
            public Version CurrentVersion { get; init; } = new Version(1, 0, 0);
            public string AssetDownloadUrl { get; init; } = "";
            public string ReleasePageUrl { get; init; } = "";
            public string ReleaseNotes { get; init; } = "";
            public string ErrorMessage { get; init; } = "";
        }

        private sealed class GitHubRelease
        {
            [JsonPropertyName("tag_name")]
            public string TagName { get; set; } = "";

            [JsonPropertyName("html_url")]
            public string HtmlUrl { get; set; } = "";
            
            [JsonPropertyName("body")]
            public string Body { get; set; } = "";

            [JsonPropertyName("draft")]
            public bool Draft { get; set; }

            [JsonPropertyName("assets")]
            public GitHubAsset[]? Assets { get; set; }
        }

        private sealed class GitHubAsset
        {
            [JsonPropertyName("name")]
            public string Name { get; set; } = "";

            [JsonPropertyName("browser_download_url")]
            public string BrowserDownloadUrl { get; set; } = "";
        }

        public static async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken ct = default)
        {
            var current = GetCurrentVersion();

            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Get, GITHUB_API_URL);
                using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);

                if (resp.StatusCode == HttpStatusCode.Forbidden)
                {
                    return new UpdateCheckResult
                    {
                        CurrentVersion = current,
                        ErrorMessage = "GitHub API 请求被拒绝（可能触发限流或网络限制）。"
                    };
                }

                if ((int)resp.StatusCode == 429)
                {
                    return new UpdateCheckResult
                    {
                        CurrentVersion = current,
                        ErrorMessage = "GitHub API 请求过于频繁（HTTP 429）。"
                    };
                }

                resp.EnsureSuccessStatusCode();

                var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                var release = JsonSerializer.Deserialize<GitHubRelease>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (release == null)
                {
                    return new UpdateCheckResult { CurrentVersion = current, ErrorMessage = "无法解析 GitHub Release 数据。" };
                }

                if (release.Draft)
                {
                    return new UpdateCheckResult { CurrentVersion = current, ErrorMessage = "最新 Release 为草稿（draft），已忽略。" };
                }

                var latestTag = (release.TagName ?? "").Trim();
                if (!TryParseVersionFromTag(latestTag, out var latestVer))
                {
                    return new UpdateCheckResult
                    {
                        CurrentVersion = current,
                        LatestTag = latestTag,
                        ReleasePageUrl = release.HtmlUrl ?? "",
                        ReleaseNotes = release.Body ?? "",
                        ErrorMessage = $"无法从 tag 解析版本号：{latestTag}"
                    };
                }

                var assetUrl = FindPreferredAssetUrl(release.Assets);
                var hasUpdate = latestVer > current;

                return new UpdateCheckResult
                {
                    HasUpdate = hasUpdate,
                    LatestTag = latestTag,
                    LatestVersion = latestVer,
                    CurrentVersion = current,
                    AssetDownloadUrl = assetUrl,
                    ReleasePageUrl = release.HtmlUrl ?? "",
                    ReleaseNotes = release.Body ?? ""
                };
            }
            catch (OperationCanceledException)
            {
                return new UpdateCheckResult { CurrentVersion = current, ErrorMessage = "更新检查已取消。" };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Update check failed: {ex}");
                return new UpdateCheckResult { CurrentVersion = current, ErrorMessage = $"更新检查失败：{ex.Message}" };
            }
        }

        private static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(10)
            };

            client.DefaultRequestHeaders.UserAgent.Clear();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("FlowWheel-Updater");

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-GitHub-Api-Version", "2022-11-28");

            return client;
        }

        private static Version GetCurrentVersion()
        {
            var v = Assembly.GetEntryAssembly()?.GetName().Version
                    ?? Assembly.GetExecutingAssembly().GetName().Version;
            return v ?? new Version(1, 0, 0);
        }

        private static bool TryParseVersionFromTag(string tag, out Version version)
        {
            version = new Version(1, 0, 0);
            if (string.IsNullOrWhiteSpace(tag)) return false;

            var s = tag.Trim();
            if (s.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                s = s.Substring(1);

            var dash = s.IndexOf('-');
            if (dash >= 0)
                s = s.Substring(0, dash);

            if (Version.TryParse(s, out var parsed) && parsed != null)
            {
                version = parsed;
                return true;
            }

            return false;
        }

        private static string FindPreferredAssetUrl(GitHubAsset[]? assets)
        {
            if (assets == null || assets.Length == 0) return "";

            foreach (var a in assets)
            {
                if (a?.Name == null) continue;
                if (a.Name.Equals("FlowWheel-Windows-x64.zip", StringComparison.OrdinalIgnoreCase))
                    return a.BrowserDownloadUrl ?? "";
            }

            foreach (var a in assets)
            {
                if (a?.Name == null) continue;
                if (a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    return a.BrowserDownloadUrl ?? "";
            }

            return assets[0]?.BrowserDownloadUrl ?? "";
        }
    }
}
