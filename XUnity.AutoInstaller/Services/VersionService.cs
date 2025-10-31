using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using XUnity.AutoInstaller.Models;
using XUnity.AutoInstaller.Utils;

namespace XUnity.AutoInstaller.Services;

/// <summary>
/// 版本管理服务
/// 负责管理已安装版本和可用版本
/// </summary>
public class VersionService
{
    private readonly SettingsService _settingsService;
    private readonly BepInExBuildsApiClient _buildsClient;
    private GitHubApiClient? _githubClient;

    public VersionService()
    {
        _settingsService = new SettingsService();
        _buildsClient = new BepInExBuildsApiClient();
    }

    /// <summary>
    /// 获取或创建 GitHubApiClient 实例，使用配置的 Token
    /// </summary>
    private GitHubApiClient GetGitHubApiClient()
    {
        if (_githubClient == null)
        {
            var settings = _settingsService.LoadSettings();
            _githubClient = new GitHubApiClient(settings.GitHubToken);
        }
        return _githubClient;
    }

    /// <summary>
    /// 获取所有可用版本（BepInEx 和 XUnity）
    /// </summary>
    public async Task<List<VersionInfo>> GetAllAvailableVersionsAsync(PackageType? filterType = null, bool includePrerelease = false)
    {
        var versions = new List<VersionInfo>();

        try
        {
            if (filterType == null || filterType == PackageType.BepInEx)
            {
                // 从 GitHub 获取 Mono 版本
                var monoVersions = await GetGitHubApiClient().GetBepInExVersionsAsync();
                versions.AddRange(monoVersions.Where(v => includePrerelease || !v.IsPrerelease));
                LogService.Instance.Log($"从 GitHub 获取 {monoVersions.Count} 个 Mono 版本", LogLevel.Debug, "[VersionService]");

                // 从 builds.bepinex.dev 获取 IL2CPP 版本
                try
                {
                    LogService.Instance.Log("开始获取 IL2CPP 版本...", LogLevel.Debug, "[VersionService]");
                    var il2cppVersions = await _buildsClient.GetIL2CPPVersionsAsync();
                    LogService.Instance.Log($"获取到 {il2cppVersions.Count} 个 IL2CPP 版本", LogLevel.Debug, "[VersionService]");

                    // IL2CPP 版本都是预览版，根据 includePrerelease 过滤
                    if (includePrerelease)
                    {
                        versions.AddRange(il2cppVersions);
                        LogService.Instance.Log($"添加了 {il2cppVersions.Count} 个 IL2CPP 版本到列表", LogLevel.Debug, "[VersionService]");
                    }
                    else
                    {
                        LogService.Instance.Log("includePrerelease=false，跳过 IL2CPP 版本", LogLevel.Debug, "[VersionService]");
                    }
                }
                catch (Exception ex)
                {
                    // 如果获取 IL2CPP 版本失败，不影响整体流程
                    LogService.Instance.Log($"获取 IL2CPP 版本失败: {ex.Message}", LogLevel.Error, "[VersionService]");
                    LogService.Instance.Log($"详细错误: {ex}", LogLevel.Error, "[VersionService]");
                }
            }

            if (filterType == null || filterType == PackageType.XUnity)
            {
                var xunityVersions = await GetGitHubApiClient().GetXUnityVersionsAsync();
                versions.AddRange(xunityVersions.Where(v => includePrerelease || !v.IsPrerelease));
            }

            return versions.OrderByDescending(v => v.ReleaseDate).ToList();
        }
        catch (Exception ex)
        {
            throw new Exception($"获取可用版本失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 获取推荐的版本组合（最新稳定版）
    /// </summary>
    public async Task<(VersionInfo? BepInEx, VersionInfo? XUnity)> GetRecommendedVersionsAsync(Platform platform)
    {
        try
        {
            VersionInfo? bepinex;

            // IL2CPP 平台从 builds.bepinex.dev 获取
            if (platform == Platform.IL2CPP_x86 || platform == Platform.IL2CPP_x64)
            {
                var il2cppVersions = await _buildsClient.GetIL2CPPVersionsAsync();
                bepinex = il2cppVersions
                    .Where(v => v.TargetPlatform == platform)
                    .OrderByDescending(v => v.ReleaseDate)
                    .FirstOrDefault();
            }
            else
            {
                // Mono 平台从 GitHub 获取
                bepinex = await GetGitHubApiClient().GetLatestBepInExVersionAsync(platform, includePrerelease: false);
            }

            var xunity = await GetGitHubApiClient().GetLatestXUnityVersionAsync(includePrerelease: false);

            return (bepinex, xunity);
        }
        catch (Exception ex)
        {
            throw new Exception($"获取推荐版本失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 下载版本到临时目录
    /// </summary>
    /// <param name="version">版本信息</param>
    /// <param name="progress">下载进度（0-100）</param>
    /// <returns>下载文件的路径</returns>
    public async Task<string> DownloadVersionAsync(VersionInfo version, IProgress<int>? progress = null)
    {
        try
        {
            var tempDir = PathHelper.GetTempDownloadDirectory();
            var fileName = Path.GetFileName(new Uri(version.DownloadUrl).LocalPath);
            var downloadPath = Path.Combine(tempDir, fileName);

            // 如果文件已存在，先删除
            if (File.Exists(downloadPath))
            {
                File.Delete(downloadPath);
            }

            await GetGitHubApiClient().DownloadFileAsync(version.DownloadUrl, downloadPath, progress);

            return downloadPath;
        }
        catch (Exception ex)
        {
            throw new Exception($"下载版本失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 获取已安装的版本信息
    /// </summary>
    public List<InstalledVersionInfo> GetInstalledVersions(string gamePath)
    {
        var versions = new List<InstalledVersionInfo>();

        try
        {
            // 检查 BepInEx
            var status = GameDetectionService.DetectInstallationStatus(gamePath);

            if (status.IsBepInExInstalled)
            {
                versions.Add(new InstalledVersionInfo
                {
                    PackageType = PackageType.BepInEx,
                    Version = status.BepInExVersion ?? "Unknown",
                    InstallDate = GetBepInExInstallDate(gamePath),
                    Platform = status.BepInExPlatform ?? "Unknown",
                    IsActive = true
                });
            }

            if (status.IsXUnityInstalled)
            {
                versions.Add(new InstalledVersionInfo
                {
                    PackageType = PackageType.XUnity,
                    Version = status.XUnityVersion ?? "Unknown",
                    InstallDate = GetXUnityInstallDate(gamePath),
                    Platform = "All",
                    IsActive = true
                });
            }

            return versions;
        }
        catch
        {
            return versions;
        }
    }

    /// <summary>
    /// 获取 BepInEx 安装日期
    /// </summary>
    private DateTime GetBepInExInstallDate(string gamePath)
    {
        try
        {
            var winhttpDll = PathHelper.GetWinhttpDllPath(gamePath);
            if (File.Exists(winhttpDll))
            {
                return File.GetCreationTime(winhttpDll);
            }
        }
        catch { }

        return DateTime.Now;
    }

    /// <summary>
    /// 获取 XUnity 安装日期
    /// </summary>
    private DateTime GetXUnityInstallDate(string gamePath)
    {
        try
        {
            var xunityPath = PathHelper.GetXUnityPath(gamePath);
            if (Directory.Exists(xunityPath))
            {
                return Directory.GetCreationTime(xunityPath);
            }
        }
        catch { }

        return DateTime.Now;
    }

    /// <summary>
    /// 卸载指定包
    /// </summary>
    public void UninstallPackage(string gamePath, PackageType packageType)
    {
        try
        {
            if (packageType == PackageType.BepInEx)
            {
                // 卸载 BepInEx（同时会卸载 XUnity）
                var bepinexPath = PathHelper.GetBepInExPath(gamePath);
                var winhttpDll = PathHelper.GetWinhttpDllPath(gamePath);
                var doorstopConfig = PathHelper.GetDoorstopConfigPath(gamePath);

                if (Directory.Exists(bepinexPath))
                {
                    Directory.Delete(bepinexPath, true);
                }

                if (File.Exists(winhttpDll))
                {
                    File.Delete(winhttpDll);
                }

                if (File.Exists(doorstopConfig))
                {
                    File.Delete(doorstopConfig);
                }
            }
            else if (packageType == PackageType.XUnity)
            {
                // 只卸载 XUnity
                var xunityPath = PathHelper.GetXUnityPath(gamePath);
                var xunityConfig = PathHelper.GetXUnityConfigFile(gamePath);

                if (Directory.Exists(xunityPath))
                {
                    Directory.Delete(xunityPath, true);
                }

                if (File.Exists(xunityConfig))
                {
                    File.Delete(xunityConfig);
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"卸载失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 比较版本号
    /// </summary>
    public static int CompareVersions(string version1, string version2)
    {
        // 移除 'v' 前缀
        version1 = version1.TrimStart('v');
        version2 = version2.TrimStart('v');

        try
        {
            var v1 = new Version(version1);
            var v2 = new Version(version2);
            return v1.CompareTo(v2);
        }
        catch
        {
            // 如果无法解析为 Version，使用字符串比较
            return string.Compare(version1, version2, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// 创建当前安装的快照备份
    /// </summary>
    public async Task<string> CreateSnapshotAsync(string gamePath, string snapshotName)
    {
        try
        {
            var bepinexPath = PathHelper.GetBepInExPath(gamePath);
            if (!Directory.Exists(bepinexPath))
            {
                throw new Exception("未找到 BepInEx 安装");
            }

            // 创建快照目录：游戏路径下的 BepInEx_Snapshots
            var snapshotBaseDir = Path.Combine(gamePath, "BepInEx_Snapshots");
            Directory.CreateDirectory(snapshotBaseDir);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var snapshotDir = Path.Combine(snapshotBaseDir, $"{snapshotName}_{timestamp}");

            // 备份 BepInEx 目录
            await Task.Run(() => FileSystemService.CopyDirectory(bepinexPath, Path.Combine(snapshotDir, "BepInEx")));

            // 备份 winhttp.dll
            var winhttpDll = PathHelper.GetWinhttpDllPath(gamePath);
            if (File.Exists(winhttpDll))
            {
                File.Copy(winhttpDll, Path.Combine(snapshotDir, "winhttp.dll"));
            }

            // 备份 doorstop_config.ini
            var doorstopConfig = PathHelper.GetDoorstopConfigPath(gamePath);
            if (File.Exists(doorstopConfig))
            {
                File.Copy(doorstopConfig, Path.Combine(snapshotDir, "doorstop_config.ini"));
            }

            // 创建快照信息文件
            var snapshotInfo = new
            {
                Name = snapshotName,
                CreatedAt = DateTime.Now,
                BepInExVersion = GameDetectionService.DetectInstallationStatus(gamePath).BepInExVersion,
                XUnityVersion = GameDetectionService.DetectInstallationStatus(gamePath).XUnityVersion
            };

            var infoPath = Path.Combine(snapshotDir, "snapshot.json");
            await File.WriteAllTextAsync(infoPath, System.Text.Json.JsonSerializer.Serialize(snapshotInfo, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

            return snapshotDir;
        }
        catch (Exception ex)
        {
            throw new Exception($"创建快照失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 获取所有快照
    /// </summary>
    public List<SnapshotInfo> GetSnapshots(string gamePath)
    {
        var snapshots = new List<SnapshotInfo>();

        try
        {
            var snapshotBaseDir = Path.Combine(gamePath, "BepInEx_Snapshots");
            if (!Directory.Exists(snapshotBaseDir))
            {
                return snapshots;
            }

            foreach (var dir in Directory.GetDirectories(snapshotBaseDir))
            {
                try
                {
                    var infoPath = Path.Combine(dir, "snapshot.json");
                    if (File.Exists(infoPath))
                    {
                        var json = File.ReadAllText(infoPath);
                        var info = System.Text.Json.JsonSerializer.Deserialize<SnapshotInfo>(json);
                        if (info != null)
                        {
                            info.Path = dir;
                            snapshots.Add(info);
                        }
                    }
                    else
                    {
                        // 兼容旧快照（没有info文件）
                        snapshots.Add(new SnapshotInfo
                        {
                            Name = Path.GetFileName(dir),
                            CreatedAt = Directory.GetCreationTime(dir),
                            Path = dir
                        });
                    }
                }
                catch { }
            }

            return snapshots.OrderByDescending(s => s.CreatedAt).ToList();
        }
        catch
        {
            return snapshots;
        }
    }

    /// <summary>
    /// 从快照恢复
    /// </summary>
    public async Task RestoreSnapshotAsync(string gamePath, string snapshotPath)
    {
        try
        {
            if (!Directory.Exists(snapshotPath))
            {
                throw new Exception("快照不存在");
            }

            // 先卸载当前版本
            UninstallPackage(gamePath, PackageType.BepInEx);

            // 恢复 BepInEx 目录
            var bepinexBackup = Path.Combine(snapshotPath, "BepInEx");
            if (Directory.Exists(bepinexBackup))
            {
                await Task.Run(() => FileSystemService.CopyDirectory(bepinexBackup, PathHelper.GetBepInExPath(gamePath)));
            }

            // 恢复 winhttp.dll
            var winhttpBackup = Path.Combine(snapshotPath, "winhttp.dll");
            if (File.Exists(winhttpBackup))
            {
                File.Copy(winhttpBackup, PathHelper.GetWinhttpDllPath(gamePath), true);
            }

            // 恢复 doorstop_config.ini
            var doorstopBackup = Path.Combine(snapshotPath, "doorstop_config.ini");
            if (File.Exists(doorstopBackup))
            {
                File.Copy(doorstopBackup, PathHelper.GetDoorstopConfigPath(gamePath), true);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"恢复快照失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 删除快照
    /// </summary>
    public void DeleteSnapshot(string snapshotPath)
    {
        try
        {
            if (Directory.Exists(snapshotPath))
            {
                Directory.Delete(snapshotPath, true);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"删除快照失败: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// 已安装版本信息
/// </summary>
public class InstalledVersionInfo
{
    public PackageType PackageType { get; set; }
    public string Version { get; set; } = string.Empty;
    public DateTime InstallDate { get; set; }
    public string Platform { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

/// <summary>
/// 快照信息
/// </summary>
public class SnapshotInfo
{
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? BepInExVersion { get; set; }
    public string? XUnityVersion { get; set; }
    public string Path { get; set; } = string.Empty;
}
