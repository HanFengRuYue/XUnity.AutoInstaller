using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Octokit;
using XUnity.AutoInstaller.Models;
using PackageType = XUnity.AutoInstaller.Models.PackageType;

namespace XUnity.AutoInstaller.Services;

/// <summary>
/// GitHub API 客户端
/// 封装 Octokit 用于获取 BepInEx 和 XUnity 的版本信息
/// </summary>
public class GitHubApiClient
{
    private readonly GitHubClient _client;
    private static readonly HttpClient _httpClient = new HttpClient();

    public GitHubApiClient()
    {
        _client = new GitHubClient(new ProductHeaderValue("XUnity-AutoInstaller"));
    }

    /// <summary>
    /// 获取 BepInEx 所有版本
    /// </summary>
    public async Task<List<VersionInfo>> GetBepInExVersionsAsync()
    {
        try
        {
            var releases = await _client.Repository.Release.GetAll("BepInEx", "BepInEx");
            var versions = new List<VersionInfo>();

            foreach (var release in releases)
            {
                // 查找 x86 和 x64 平台的 asset
                foreach (var asset in release.Assets)
                {
                    if (!asset.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    Platform? platform = null;
                    var name = asset.Name.ToLowerInvariant();

                    // 跳过 IL2CPP 版本（这些版本将从 builds.bepinex.dev 获取）
                    if (name.Contains("il2cpp"))
                    {
                        continue;
                    }

                    // 识别 Mono 平台
                    if (name.Contains("x64") || name.Contains("win_x64"))
                    {
                        platform = Platform.x64;
                    }
                    else if (name.Contains("x86") || name.Contains("win_x86"))
                    {
                        platform = Platform.x86;
                    }

                    if (platform.HasValue)
                    {
                        versions.Add(new VersionInfo
                        {
                            Name = $"BepInEx {release.TagName} ({platform.Value})",
                            Version = release.TagName,
                            ReleaseDate = release.PublishedAt?.DateTime ?? DateTime.Now,
                            FileSize = asset.Size,
                            DownloadUrl = asset.BrowserDownloadUrl,
                            IsPrerelease = release.Prerelease,
                            PackageType = PackageType.BepInEx,
                            TargetPlatform = platform
                        });
                    }
                }
            }

            return versions;
        }
        catch (Exception ex)
        {
            throw new Exception($"获取 BepInEx 版本失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 获取 XUnity.AutoTranslator 所有版本
    /// </summary>
    public async Task<List<VersionInfo>> GetXUnityVersionsAsync()
    {
        try
        {
            var releases = await _client.Repository.Release.GetAll("bbepis", "XUnity.AutoTranslator");
            var versions = new List<VersionInfo>();

            foreach (var release in releases)
            {
                // 查找主要的 ZIP 包
                var mainAsset = release.Assets.FirstOrDefault(a =>
                    a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
                    !a.Name.Contains("ReiPatcher", StringComparison.OrdinalIgnoreCase));

                if (mainAsset != null)
                {
                    versions.Add(new VersionInfo
                    {
                        Name = $"XUnity.AutoTranslator {release.TagName}",
                        Version = release.TagName,
                        ReleaseDate = release.PublishedAt?.DateTime ?? DateTime.Now,
                        FileSize = mainAsset.Size,
                        DownloadUrl = mainAsset.BrowserDownloadUrl,
                        IsPrerelease = release.Prerelease,
                        PackageType = PackageType.XUnity,
                        TargetPlatform = null // XUnity 与平台无关
                    });
                }
            }

            return versions;
        }
        catch (Exception ex)
        {
            throw new Exception($"获取 XUnity 版本失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 获取最新的 BepInEx 版本（指定平台）
    /// </summary>
    public async Task<VersionInfo?> GetLatestBepInExVersionAsync(Platform platform, bool includePrerelease = false)
    {
        var versions = await GetBepInExVersionsAsync();
        return versions
            .Where(v => v.TargetPlatform == platform && (includePrerelease || !v.IsPrerelease))
            .OrderByDescending(v => v.ReleaseDate)
            .FirstOrDefault();
    }

    /// <summary>
    /// 获取最新的 XUnity 版本
    /// </summary>
    public async Task<VersionInfo?> GetLatestXUnityVersionAsync(bool includePrerelease = false)
    {
        var versions = await GetXUnityVersionsAsync();
        return versions
            .Where(v => includePrerelease || !v.IsPrerelease)
            .OrderByDescending(v => v.ReleaseDate)
            .FirstOrDefault();
    }

    /// <summary>
    /// 下载文件到指定路径
    /// </summary>
    /// <param name="url">下载 URL</param>
    /// <param name="destinationPath">目标文件路径</param>
    /// <param name="progress">进度报告（0-100）</param>
    public async Task DownloadFileAsync(string url, string destinationPath, IProgress<int>? progress = null)
    {
        try
        {
            // 确保目标目录存在
            var directory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 下载文件
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            var canReportProgress = totalBytes != -1 && progress != null;

            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(destinationPath, System.IO.FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

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
        }
        catch (Exception ex)
        {
            throw new Exception($"下载文件失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 检查 API 速率限制
    /// </summary>
    public async Task<(int Limit, int Remaining, DateTime Reset)> GetRateLimitAsync()
    {
        try
        {
            var rateLimit = await _client.Miscellaneous.GetRateLimits();
            var coreLimit = rateLimit.Resources.Core;

            return (coreLimit.Limit, coreLimit.Remaining, coreLimit.Reset.DateTime);
        }
        catch
        {
            return (60, 60, DateTime.Now.AddHours(1)); // 默认未认证限制
        }
    }
}
