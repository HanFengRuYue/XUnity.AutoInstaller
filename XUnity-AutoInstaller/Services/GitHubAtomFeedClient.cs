using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using XUnity_AutoInstaller.Models;
using PackageType = XUnity_AutoInstaller.Models.PackageType;

namespace XUnity_AutoInstaller.Services;

/// <summary>
/// GitHub Atom Feed 客户端
/// 使用 Atom feed 获取版本信息，完全不受速率限制
/// </summary>
public class GitHubAtomFeedClient : IVersionFetcher, IDisposable
{
    private readonly HttpClient _httpClient;
    private const string BepInExFeedUrl = "https://github.com/BepInEx/BepInEx/releases.atom";
    private const string XUnityFeedUrl = "https://github.com/bbepis/XUnity.AutoTranslator/releases.atom";

    public string SourceName => "GitHub Official";

    public GitHubAtomFeedClient()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "XUnity-AutoInstaller");

        LogService.Instance.Log("使用 Atom Feed 客户端（无速率限制）", LogLevel.Info, "[AtomFeed]");
    }

    /// <summary>
    /// 获取 BepInEx 所有版本（从 Atom feed）
    /// </summary>
    /// <param name="maxCount">最多获取的版本数量</param>
    public async Task<List<VersionInfo>> GetBepInExVersionsAsync(int maxCount = 10)
    {
        try
        {
            LogService.Instance.Log($"从 Atom Feed 获取 BepInEx 版本，限制 {maxCount} 个", LogLevel.Debug, "[AtomFeed]");

            var xml = await _httpClient.GetStringAsync(BepInExFeedUrl);
            var entries = ParseAtomFeed(xml);

            var versions = new List<VersionInfo>();

            foreach (var entry in entries.Take(maxCount))
            {
                // 从标题提取版本号，例如 "BepInEx 5.4.23.4" -> "v5.4.23.4"
                var title = entry.Title;
                var versionMatch = System.Text.RegularExpressions.Regex.Match(title, @"(\d+\.\d+\.\d+(?:\.\d+)?)");

                if (!versionMatch.Success)
                {
                    LogService.Instance.Log($"无法解析版本号: {title}", LogLevel.Warning, "[AtomFeed]");
                    continue;
                }

                var version = versionMatch.Groups[1].Value;
                var tag = $"v{version}";

                // 判断是否为预览版
                var isPrerelease = title.ToLower().Contains("pre") ||
                                   title.ToLower().Contains("rc") ||
                                   title.ToLower().Contains("beta") ||
                                   version.Contains("-");

                // 为 x64 和 x86 平台创建版本条目
                var platforms = new[]
                {
                    (Platform.x64, "x64"),
                    (Platform.x86, "x86")
                };

                foreach (var (platform, archName) in platforms)
                {
                    // 构造下载 URL
                    // 格式: https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.4/BepInEx_win_x64_5.4.23.4.zip
                    var fileName = $"BepInEx_win_{archName}_{version}.zip";
                    var downloadUrl = $"https://github.com/BepInEx/BepInEx/releases/download/{tag}/{fileName}";

                    versions.Add(new VersionInfo
                    {
                        Name = $"BepInEx {version} ({platform})",
                        Version = tag,
                        ReleaseDate = entry.Updated,
                        FileSize = 0, // Atom feed 不提供文件大小，需要时可通过 HEAD 请求获取
                        DownloadUrl = downloadUrl,
                        IsPrerelease = isPrerelease,
                        PackageType = PackageType.BepInEx,
                        TargetPlatform = platform
                    });
                }

                LogService.Instance.Log($"解析版本: {version}, 预览版: {isPrerelease}", LogLevel.Debug, "[AtomFeed]");
            }

            LogService.Instance.Log($"从 Atom Feed 获取 {versions.Count} 个 BepInEx 版本", LogLevel.Info, "[AtomFeed]");
            return versions;
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"从 Atom Feed 获取 BepInEx 版本失败: {ex.Message}", LogLevel.Error, "[AtomFeed]");
            throw new Exception($"获取 BepInEx 版本失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 获取 XUnity.AutoTranslator 所有版本（从 Atom feed）
    /// 支持 Mono 和 IL2CPP 两种变体
    /// </summary>
    /// <param name="maxCount">最多获取的版本数量</param>
    public async Task<List<VersionInfo>> GetXUnityVersionsAsync(int maxCount = 10)
    {
        try
        {
            LogService.Instance.Log($"从 Atom Feed 获取 XUnity 版本，限制 {maxCount} 个", LogLevel.Debug, "[AtomFeed]");

            var xml = await _httpClient.GetStringAsync(XUnityFeedUrl);
            var entries = ParseAtomFeed(xml);

            var versions = new List<VersionInfo>();

            foreach (var entry in entries.Take(maxCount))
            {
                // 从标题提取版本号，例如 "v5.5.0" -> "5.5.0"
                var title = entry.Title.TrimStart('v');
                var versionMatch = System.Text.RegularExpressions.Regex.Match(title, @"(\d+\.\d+\.\d+(?:\.\d+)?)");

                if (!versionMatch.Success)
                {
                    LogService.Instance.Log($"无法解析版本号: {title}", LogLevel.Warning, "[AtomFeed]");
                    continue;
                }

                var version = versionMatch.Groups[1].Value;
                var tag = $"v{version}";

                // 判断是否为预览版
                var isPrerelease = title.ToLower().Contains("pre") ||
                                   title.ToLower().Contains("rc") ||
                                   title.ToLower().Contains("beta") ||
                                   version.Contains("-");

                // 尝试从发布页面获取资产链接（支持 Mono 和 IL2CPP）
                var releaseAssets = await GetReleaseAssetsAsync(entry.Link);
                
                // 查找 Mono 版本
                var monoAsset = releaseAssets.FirstOrDefault(a => 
                    a.Contains($"XUnity.AutoTranslator-BepInEx-{version}.zip", StringComparison.OrdinalIgnoreCase) &&
                    !a.Contains("IL2CPP", StringComparison.OrdinalIgnoreCase));
                
                // 查找 IL2CPP 版本
                var il2cppAsset = releaseAssets.FirstOrDefault(a => 
                    a.Contains($"XUnity.AutoTranslator-BepInEx-IL2CPP-{version}.zip", StringComparison.OrdinalIgnoreCase));

                // 添加 Mono 版本
                if (!string.IsNullOrEmpty(monoAsset))
                {
                    versions.Add(new VersionInfo
                    {
                        Name = $"XUnity.AutoTranslator {version} (Mono)",
                        Version = tag,
                        ReleaseDate = entry.Updated,
                        FileSize = 0, // Atom feed 不提供文件大小
                        DownloadUrl = monoAsset,
                        IsPrerelease = isPrerelease,
                        PackageType = PackageType.XUnity,
                        TargetPlatform = null // Mono 版本适用于所有平台
                    });
                    LogService.Instance.Log($"找到 Mono 版本: {version}", LogLevel.Debug, "[AtomFeed]");
                }
                else
                {
                    // 如果无法从发布页面获取，回退到构造 URL
                    var fileName = $"XUnity.AutoTranslator-BepInEx-{version}.zip";
                    var downloadUrl = $"https://github.com/bbepis/XUnity.AutoTranslator/releases/download/{tag}/{fileName}";
                    
                    versions.Add(new VersionInfo
                    {
                        Name = $"XUnity.AutoTranslator {version} (Mono)",
                        Version = tag,
                        ReleaseDate = entry.Updated,
                        FileSize = 0,
                        DownloadUrl = downloadUrl,
                        IsPrerelease = isPrerelease,
                        PackageType = PackageType.XUnity,
                        TargetPlatform = null
                    });
                    LogService.Instance.Log($"使用构造的 Mono URL: {version}", LogLevel.Debug, "[AtomFeed]");
                }

                // 添加 IL2CPP 版本
                if (!string.IsNullOrEmpty(il2cppAsset))
                {
                    versions.Add(new VersionInfo
                    {
                        Name = $"XUnity.AutoTranslator {version} (IL2CPP)",
                        Version = tag,
                        ReleaseDate = entry.Updated,
                        FileSize = 0,
                        DownloadUrl = il2cppAsset,
                        IsPrerelease = isPrerelease,
                        PackageType = PackageType.XUnity,
                        TargetPlatform = Platform.IL2CPP_x64 // 标记为 IL2CPP 平台
                    });
                    LogService.Instance.Log($"找到 IL2CPP 版本: {version}", LogLevel.Debug, "[AtomFeed]");
                }

                LogService.Instance.Log($"解析版本: {version}, 预览版: {isPrerelease}", LogLevel.Debug, "[AtomFeed]");
            }

            LogService.Instance.Log($"从 Atom Feed 获取 {versions.Count} 个 XUnity 版本", LogLevel.Info, "[AtomFeed]");
            return versions;
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"从 Atom Feed 获取 XUnity 版本失败: {ex.Message}", LogLevel.Error, "[AtomFeed]");
            throw new Exception($"获取 XUnity 版本失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 从 GitHub 发布页面获取资产下载链接
    /// </summary>
    private async Task<List<string>> GetReleaseAssetsAsync(string releasePageUrl)
    {
        var assets = new List<string>();
        
        try
        {
            // 从发布页面 URL 提取 API URL
            // 例如: https://github.com/bbepis/XUnity.AutoTranslator/releases/tag/v5.5.0
            // 转换为: https://api.github.com/repos/bbepis/XUnity.AutoTranslator/releases/tags/v5.5.0
            var apiUrl = releasePageUrl.Replace("https://github.com/", "https://api.github.com/repos/")
                                       .Replace("/releases/tag/", "/releases/tags/");
            
            // 添加 User-Agent 头（GitHub API 要求）
            using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            request.Headers.Add("User-Agent", "XUnity-AutoInstaller");
            request.Headers.Add("Accept", "application/vnd.github.v3+json");
            
            var response = await _httpClient.SendAsync(request);
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                // 简单的 JSON 解析：查找 "browser_download_url" 字段
                var downloadUrlPattern = new System.Text.RegularExpressions.Regex(@"""browser_download_url""\s*:\s*""([^""]+)""");
                var matches = downloadUrlPattern.Matches(json);
                
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    if (match.Groups.Count > 1)
                    {
                        var url = match.Groups[1].Value;
                        // 只包含 XUnity.AutoTranslator-BepInEx 相关的 ZIP 文件
                        if (url.Contains("XUnity.AutoTranslator-BepInEx", StringComparison.OrdinalIgnoreCase) &&
                            url.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            assets.Add(url);
                        }
                    }
                }
            }
            else
            {
                LogService.Instance.Log($"无法获取发布资产: {response.StatusCode}", LogLevel.Warning, "[AtomFeed]");
            }
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"获取发布资产失败: {ex.Message}", LogLevel.Warning, "[AtomFeed]");
            // 不抛出异常，允许回退到构造 URL
        }
        
        return assets;
    }

    /// <summary>
    /// 验证连接是否可用
    /// </summary>
    public async Task<bool> ValidateConnectionAsync()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Head, BepInExFeedUrl);
            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 验证下载 URL 是否有效（通过 HEAD 请求）
    /// </summary>
    public async Task<bool> ValidateDownloadUrlAsync(string url)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Head, url);
            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取文件大小（通过 HEAD 请求）
    /// </summary>
    public async Task<long> GetFileSizeAsync(string url)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Head, url);
            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode && response.Content.Headers.ContentLength.HasValue)
            {
                return response.Content.Headers.ContentLength.Value;
            }
        }
        catch
        {
            // 忽略错误
        }

        return 0;
    }

    /// <summary>
    /// 解析 Atom feed XML
    /// </summary>
    private List<AtomEntry> ParseAtomFeed(string xml)
    {
        var entries = new List<AtomEntry>();

        try
        {
            var doc = XDocument.Parse(xml);
            XNamespace atom = "http://www.w3.org/2005/Atom";

            foreach (var entry in doc.Descendants(atom + "entry"))
            {
                var title = entry.Element(atom + "title")?.Value ?? "";
                var updatedStr = entry.Element(atom + "updated")?.Value;
                var link = entry.Element(atom + "link")?.Attribute("href")?.Value ?? "";
                var content = entry.Element(atom + "content")?.Value ?? "";

                DateTime updated = DateTime.Now;
                if (!string.IsNullOrEmpty(updatedStr))
                {
                    DateTime.TryParse(updatedStr, out updated);
                }

                entries.Add(new AtomEntry
                {
                    Title = title,
                    Updated = updated,
                    Link = link,
                    Content = content
                });
            }

            LogService.Instance.Log($"解析到 {entries.Count} 个 Atom 条目", LogLevel.Debug, "[AtomFeed]");
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"解析 Atom feed 失败: {ex.Message}", LogLevel.Error, "[AtomFeed]");
            throw;
        }

        return entries;
    }

    /// <summary>
    /// 下载文件到指定路径
    /// </summary>
    /// <param name="url">下载 URL</param>
    /// <param name="destinationPath">目标文件路径</param>
    /// <param name="progress">进度报告（0-100）</param>
    public async Task DownloadFileAsync(string url, string destinationPath, IProgress<int>? progress = null)
    {
        const int maxRetries = 3;
        const int retryDelayMs = 1000;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                // 确保目标目录存在
                var directory = System.IO.Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }

                // 下载文件（增加超时时间以应对大文件）
                using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1;
                var canReportProgress = totalBytes != -1 && progress != null;

                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new System.IO.FileStream(destinationPath, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None, 8192, true);

                var buffer = new byte[8192];
                long totalRead = 0;
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalRead += bytesRead;

                    if (canReportProgress)
                    {
                        var percentage = (int)((double)totalRead / totalBytes * 100);
                        progress!.Report(percentage);
                    }
                }

                // 验证文件完整性
                if (totalBytes > 0 && totalRead != totalBytes)
                {
                    throw new Exception($"下载不完整: 期望 {totalBytes} 字节，实际 {totalRead} 字节");
                }

                return; // 下载成功，退出重试循环
            }
            catch (Exception ex)
            {
                // 如果是最后一次尝试，抛出异常
                if (attempt == maxRetries)
                {
                    throw new Exception($"下载文件失败（已重试 {maxRetries} 次）: {ex.Message}", ex);
                }

                // 否则记录日志并重试
                LogService.Instance.Log($"下载失败（尝试 {attempt}/{maxRetries}）: {ex.Message}，{retryDelayMs}ms后重试...", LogLevel.Warning, "[AtomFeed]");
                await Task.Delay(retryDelayMs * attempt); // 递增延迟时间
            }
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    /// <summary>
    /// Atom 条目
    /// </summary>
    private class AtomEntry
    {
        public string Title { get; set; } = string.Empty;
        public DateTime Updated { get; set; }
        public string Link { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
