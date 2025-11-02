using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebDav;
using XUnity_AutoInstaller.Models;
using PackageType = XUnity_AutoInstaller.Models.PackageType;

namespace XUnity_AutoInstaller.Services;

/// <summary>
/// WebDAV镜像站客户端
/// 从WebDAV服务器获取版本列表并下载文件
/// </summary>
public class WebDAVMirrorClient : IVersionFetcher, IDisposable
{
    private readonly WebDavClient _webDavClient;
    private readonly HttpClient _httpClient;
    private const string BepInExMirrorUrl = "https://fraxelia.com:60761/BepInEx/";
    private const string XUnityMirrorUrl = "https://fraxelia.com:60761/XUnity/";

    public string SourceName => "Mirror Website";

    public WebDAVMirrorClient()
    {
        var clientParams = new WebDavClientParams
        {
            BaseAddress = new Uri("https://fraxelia.com:60761/"),
            Timeout = TimeSpan.FromSeconds(30)
        };

        _webDavClient = new WebDavClient(clientParams);
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "XUnity-AutoInstaller");

        LogService.Instance.Log("使用 WebDAV 镜像站客户端", LogLevel.Info, "[Mirror]");
    }

    /// <summary>
    /// 获取 BepInEx 所有版本（从 WebDAV 镜像）
    /// </summary>
    public async Task<List<VersionInfo>> GetBepInExVersionsAsync(int maxCount = 10)
    {
        try
        {
            LogService.Instance.Log($"从 WebDAV 镜像获取 BepInEx 版本，限制 {maxCount} 个", LogLevel.Debug, "[Mirror]");

            // 使用 PROPFIND 列出所有文件
            var result = await _webDavClient.Propfind(BepInExMirrorUrl);

            if (!result.IsSuccessful)
            {
                throw new Exception($"WebDAV PROPFIND 失败: {result.StatusCode}");
            }

            var versions = new List<VersionInfo>();

            // 解析文件列表
            var files = result.Resources
                .Where(r => !r.IsCollection && r.DisplayName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(r => r.LastModifiedDate ?? DateTime.MinValue)
                .Take(maxCount * 2); // 获取更多以便过滤

            foreach (var file in files)
            {
                var fileName = file.DisplayName;
                var versionInfo = ParseBepInExFileName(fileName, file);

                if (versionInfo != null)
                {
                    versions.Add(versionInfo);
                }

                // 检查是否已达到所需数量（考虑到每个版本有两个平台）
                if (versions.Count >= maxCount * 2)
                {
                    break;
                }
            }

            LogService.Instance.Log($"从 WebDAV 镜像获取 {versions.Count} 个 BepInEx 版本", LogLevel.Info, "[Mirror]");
            return versions;
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"从 WebDAV 镜像获取 BepInEx 版本失败: {ex.Message}", LogLevel.Error, "[Mirror]");
            throw new Exception($"获取 BepInEx 版本失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 获取 XUnity.AutoTranslator 所有版本（从 WebDAV 镜像）
    /// </summary>
    public async Task<List<VersionInfo>> GetXUnityVersionsAsync(int maxCount = 10)
    {
        try
        {
            LogService.Instance.Log($"从 WebDAV 镜像获取 XUnity 版本，限制 {maxCount} 个", LogLevel.Debug, "[Mirror]");

            // 使用 PROPFIND 列出所有文件
            var result = await _webDavClient.Propfind(XUnityMirrorUrl);

            if (!result.IsSuccessful)
            {
                throw new Exception($"WebDAV PROPFIND 失败: {result.StatusCode}");
            }

            var versions = new List<VersionInfo>();

            // 解析文件列表
            var files = result.Resources
                .Where(r => !r.IsCollection && r.DisplayName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(r => r.LastModifiedDate ?? DateTime.MinValue)
                .Take(maxCount);

            foreach (var file in files)
            {
                var fileName = file.DisplayName;
                var versionInfo = ParseXUnityFileName(fileName, file);

                if (versionInfo != null)
                {
                    versions.Add(versionInfo);
                }
            }

            LogService.Instance.Log($"从 WebDAV 镜像获取 {versions.Count} 个 XUnity 版本", LogLevel.Info, "[Mirror]");
            return versions;
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"从 WebDAV 镜像获取 XUnity 版本失败: {ex.Message}", LogLevel.Error, "[Mirror]");
            throw new Exception($"获取 XUnity 版本失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 解析 BepInEx 文件名
    /// 格式: BepInEx_win_x64_5.4.23.4.zip
    /// </summary>
    private VersionInfo? ParseBepInExFileName(string fileName, WebDavResource resource)
    {
        try
        {
            // 匹配格式: BepInEx_win_{platform}_{version}.zip
            var match = Regex.Match(fileName, @"BepInEx_win_(x86|x64)_(\d+\.\d+\.\d+(?:\.\d+)?(?:-[a-zA-Z0-9]+)?)\.zip", RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                LogService.Instance.Log($"无法解析 BepInEx 文件名: {fileName}", LogLevel.Debug, "[Mirror]");
                return null;
            }

            var platformStr = match.Groups[1].Value.ToLower();
            var version = match.Groups[2].Value;

            // 解析平台
            Platform platform = platformStr switch
            {
                "x64" => Platform.x64,
                "x86" => Platform.x86,
                _ => Platform.x64
            };

            // 判断是否为预览版
            var isPrerelease = version.Contains("-") ||
                               fileName.ToLower().Contains("pre") ||
                               fileName.ToLower().Contains("rc") ||
                               fileName.ToLower().Contains("beta");

            var tag = $"v{version.Split('-')[0]}"; // 移除预发布标签

            // 构造完整下载 URL
            var downloadUrl = $"{BepInExMirrorUrl}{fileName}";

            return new VersionInfo
            {
                Name = $"BepInEx {version} ({platform})",
                Version = tag,
                ReleaseDate = resource.LastModifiedDate ?? DateTime.Now,
                FileSize = resource.ContentLength ?? 0,
                DownloadUrl = downloadUrl,
                IsPrerelease = isPrerelease,
                PackageType = PackageType.BepInEx,
                TargetPlatform = platform
            };
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"解析 BepInEx 文件名失败 ({fileName}): {ex.Message}", LogLevel.Warning, "[Mirror]");
            return null;
        }
    }

    /// <summary>
    /// 解析 XUnity 文件名
    /// 格式: XUnity.AutoTranslator-BepInEx-5.3.0.zip
    /// </summary>
    private VersionInfo? ParseXUnityFileName(string fileName, WebDavResource resource)
    {
        try
        {
            // 匹配格式: XUnity.AutoTranslator-BepInEx-{version}.zip
            var match = Regex.Match(fileName, @"XUnity\.AutoTranslator-BepInEx-(\d+\.\d+\.\d+(?:\.\d+)?(?:-[a-zA-Z0-9]+)?)\.zip", RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                LogService.Instance.Log($"无法解析 XUnity 文件名: {fileName}", LogLevel.Debug, "[Mirror]");
                return null;
            }

            var version = match.Groups[1].Value;

            // 判断是否为预览版
            var isPrerelease = version.Contains("-") ||
                               fileName.ToLower().Contains("pre") ||
                               fileName.ToLower().Contains("rc") ||
                               fileName.ToLower().Contains("beta");

            var tag = $"v{version.Split('-')[0]}"; // 移除预发布标签

            // 构造完整下载 URL
            var downloadUrl = $"{XUnityMirrorUrl}{fileName}";

            return new VersionInfo
            {
                Name = $"XUnity.AutoTranslator {version}",
                Version = tag,
                ReleaseDate = resource.LastModifiedDate ?? DateTime.Now,
                FileSize = resource.ContentLength ?? 0,
                DownloadUrl = downloadUrl,
                IsPrerelease = isPrerelease,
                PackageType = PackageType.XUnity,
                TargetPlatform = null // XUnity 与平台无关
            };
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"解析 XUnity 文件名失败 ({fileName}): {ex.Message}", LogLevel.Warning, "[Mirror]");
            return null;
        }
    }

    /// <summary>
    /// 下载文件到指定路径（通过 HTTP GET，WebDAV 底层使用 HTTP）
    /// </summary>
    public async Task DownloadFileAsync(string url, string destinationPath, IProgress<int>? progress = null)
    {
        const int maxRetries = 3;
        const int retryDelayMs = 1000;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                // 确保目标目录存在
                var directory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 直接使用 HTTP GET 下载（WebDAV 支持 GET）
                using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1;
                var canReportProgress = totalBytes != -1 && progress != null;

                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

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

                LogService.Instance.Log($"从镜像站下载完成: {Path.GetFileName(destinationPath)}", LogLevel.Info, "[Mirror]");
                return; // 下载成功，退出重试循环
            }
            catch (Exception ex)
            {
                // 如果是最后一次尝试，抛出异常
                if (attempt == maxRetries)
                {
                    throw new Exception($"从镜像站下载文件失败（已重试 {maxRetries} 次）: {ex.Message}", ex);
                }

                // 否则记录日志并重试
                LogService.Instance.Log($"下载失败（尝试 {attempt}/{maxRetries}）: {ex.Message}，{retryDelayMs}ms后重试...", LogLevel.Warning, "[Mirror]");
                await Task.Delay(retryDelayMs * attempt); // 递增延迟时间
            }
        }
    }

    /// <summary>
    /// 验证 WebDAV 连接是否可用
    /// </summary>
    public async Task<bool> ValidateConnectionAsync()
    {
        try
        {
            var result = await _webDavClient.Propfind(BepInExMirrorUrl);
            return result.IsSuccessful;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
