using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using XUnity_AutoInstaller.Models;
using XUnity_AutoInstaller.Utils;

namespace XUnity_AutoInstaller.Services;

/// <summary>
/// 安装服务
/// 负责协调 BepInEx 和 XUnity 的安装流程
/// </summary>
public class InstallationService
{
    private readonly VersionService _versionService;
    private readonly LogWriter? _logger;
    private static readonly SemaphoreSlim _installLock = new(1, 1);

    public InstallationService(LogWriter? logger = null)
    {
        _versionService = new VersionService();
        _logger = logger;
    }

    /// <summary>
    /// 执行完整安装流程
    /// </summary>
    public async Task<bool> InstallAsync(string gamePath, InstallOptions options, IProgress<(int percentage, string message)>? progress = null)
    {
        // 防止并发安装
        bool lockAcquired = await _installLock.WaitAsync(0);
        if (!lockAcquired)
        {
            throw new InvalidOperationException("已有安装任务正在进行中，请稍候");
        }

        try
        {
            progress?.Report((0, "开始安装..."));
            _logger?.Info("开始安装流程");

            // 1. 验证游戏路径
            if (!PathHelper.IsValidGameDirectory(gamePath))
            {
                throw new Exception("无效的游戏目录");
            }

            // 2. 检测游戏引擎
            var gameInfo = GameDetectionService.GetGameInfo(gamePath);
            _logger?.Info($"检测到游戏引擎: {gameInfo.Engine}");

            if (gameInfo.Engine == GameEngine.Unknown)
            {
                throw new Exception("无法识别游戏引擎类型");
            }

            // 3. 备份现有安装
            if (options.BackupExisting)
            {
                progress?.Report((10, "备份现有文件..."));
                await BackupExistingInstallation(gamePath);
            }

            // 4. 清理旧版本
            if (options.CleanOldVersion)
            {
                progress?.Report((20, "清理旧版本..."));
                CleanOldInstallation(gamePath);
            }

            // 5. 安装 BepInEx
            progress?.Report((30, "下载 BepInEx..."));
            await InstallBepInExAsync(gamePath, options, progress);

            // 6. 安装 XUnity
            progress?.Report((60, "下载 XUnity.AutoTranslator..."));
            await InstallXUnityAsync(gamePath, options, progress);

            // 7. 启动游戏生成配置文件
            if (options.LaunchGameToGenerateConfig)
            {
                progress?.Report((87, "启动游戏生成配置文件..."));
                _logger?.Info("准备启动游戏生成BepInEx配置文件");

                var gameLauncher = new GameLauncherService(_logger);
                bool configGenerated = await gameLauncher.LaunchGameAndWaitForConfigGenerationAsync(
                    gamePath,
                    options.ConfigGenerationTimeout,
                    new Progress<(int percentage, string message)>(p =>
                    {
                        // 将配置生成进度映射到总进度的87-95%区间
                        int mappedProgress = 87 + (int)(p.percentage * 0.08);
                        progress?.Report((mappedProgress, p.message));
                    }));

                if (configGenerated)
                {
                    _logger?.Success("配置文件生成成功");
                }
                else
                {
                    _logger?.Warning("配置文件生成失败或超时，您可能需要手动运行游戏一次");
                    // 不阻止安装流程继续
                }
            }

            progress?.Report((100, "安装完成!"));
            _logger?.Success("安装流程完成");
            return true;
        }
        catch (Exception ex)
        {
            _logger?.Error($"安装失败: {ex.Message}");
            progress?.Report((0, $"安装失败: {ex.Message}"));
            throw;
        }
        finally
        {
            _installLock.Release();
        }
    }

    /// <summary>
    /// 安装 BepInEx
    /// </summary>
    private async Task InstallBepInExAsync(string gamePath, InstallOptions options, IProgress<(int percentage, string message)>? progress = null)
    {
        try
        {
            // 获取版本
            VersionInfo? version;

            if (!string.IsNullOrEmpty(options.BepInExVersion))
            {
                // 使用指定版本
                _logger?.Info($"使用指定版本: {options.BepInExVersion}");
                var allVersions = await _versionService.GetAllAvailableVersionsAsync(PackageType.BepInEx, includePrerelease: false);
                version = allVersions.FirstOrDefault(v =>
                    v.Version == options.BepInExVersion &&
                    v.TargetPlatform == options.TargetPlatform);

                if (version == null)
                {
                    throw new Exception($"未找到指定的 BepInEx 版本: {options.BepInExVersion} ({options.TargetPlatform})");
                }
            }
            else
            {
                // 使用最新版本
                _logger?.Info("获取最新版本...");
                version = (await _versionService.GetRecommendedVersionsAsync(options.TargetPlatform)).BepInEx;
            }

            if (version == null)
            {
                throw new Exception("未找到合适的 BepInEx 版本");
            }

            _logger?.Info($"下载 BepInEx {version.Version}...");

            // 下载文件
            var downloadProgress = new Progress<int>(p =>
            {
                var overallProgress = 30 + (int)(p * 0.25); // 30-55%
                progress?.Report((overallProgress, $"下载 BepInEx ({p}%)..."));
            });

            var zipPath = await _versionService.DownloadVersionAsync(version, downloadProgress);
            _logger?.Info($"下载完成: {zipPath}");

            // 解压文件
            progress?.Report((55, "解压 BepInEx..."));
            _logger?.Info("解压文件...");

            var extractProgress = new Progress<int>(p =>
            {
                var overallProgress = 55 + (int)(p * 0.05); // 55-60%
                progress?.Report((overallProgress, $"解压 BepInEx ({p}%)..."));
            });

            await FileSystemService.ExtractZipAsync(zipPath, gamePath, extractProgress);
            _logger?.Success("BepInEx 安装完成");
        }
        catch (Exception ex)
        {
            _logger?.Error($"BepInEx 安装失败: {ex.Message}");
            throw new Exception($"BepInEx 安装失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 安装 XUnity.AutoTranslator
    /// </summary>
    private async Task InstallXUnityAsync(string gamePath, InstallOptions options, IProgress<(int percentage, string message)>? progress = null)
    {
        try
        {
            // 获取版本
            VersionInfo? version;

            if (!string.IsNullOrEmpty(options.XUnityVersion))
            {
                // 使用指定版本
                _logger?.Info($"使用指定版本: {options.XUnityVersion}");
                var allVersions = await _versionService.GetAllAvailableVersionsAsync(PackageType.XUnity, includePrerelease: false);
                version = allVersions.FirstOrDefault(v => v.Version == options.XUnityVersion);

                if (version == null)
                {
                    throw new Exception($"未找到指定的 XUnity 版本: {options.XUnityVersion}");
                }
            }
            else
            {
                // 使用最新版本
                _logger?.Info("获取最新版本...");
                version = (await _versionService.GetRecommendedVersionsAsync(Platform.x64)).XUnity;
            }

            if (version == null)
            {
                throw new Exception("未找到合适的 XUnity 版本");
            }

            _logger?.Info($"下载 XUnity {version.Version}...");

            // 下载文件
            var downloadProgress = new Progress<int>(p =>
            {
                var overallProgress = 60 + (int)(p * 0.25); // 60-85%
                progress?.Report((overallProgress, $"下载 XUnity ({p}%)..."));
            });

            var zipPath = await _versionService.DownloadVersionAsync(version, downloadProgress);
            _logger?.Info($"下载完成: {zipPath}");

            // 解压文件直接到游戏目录（保留完整的 BepInEx 目录结构）
            // IMPORTANT: XUnity zip contains BepInEx/core/, BepInEx/plugins/XUnity.ResourceRedirector/,
            // and BepInEx/plugins/XUnity.AutoTranslator/. We must extract directly to preserve this
            // structure. The core folder contains XUnity.Common.dll and other critical dependencies.
            progress?.Report((85, "解压 XUnity..."));
            _logger?.Info("解压文件到游戏目录...");

            var extractProgress = new Progress<int>(p =>
            {
                var overallProgress = 85 + (int)(p * 0.02); // 85-87%
                progress?.Report((overallProgress, $"解压 XUnity ({p}%)..."));
            });

            await FileSystemService.ExtractZipAsync(zipPath, gamePath, extractProgress);

            _logger?.Success("XUnity 安装完成");
        }
        catch (Exception ex)
        {
            _logger?.Error($"XUnity 安装失败: {ex.Message}");
            throw new Exception($"XUnity 安装失败: {ex.Message}", ex);
        }
    }


    /// <summary>
    /// 备份现有安装
    /// </summary>
    private async Task BackupExistingInstallation(string gamePath)
    {
        try
        {
            var bepinexPath = PathHelper.GetBepInExPath(gamePath);

            if (Directory.Exists(bepinexPath))
            {
                _logger?.Info("备份现有 BepInEx...");
                await Task.Run(() => FileSystemService.BackupDirectory(bepinexPath));
                _logger?.Success("备份完成");
            }
        }
        catch (Exception ex)
        {
            _logger?.Warning($"备份失败: {ex.Message}");
            // 备份失败不应阻止安装
        }
    }

    /// <summary>
    /// 清理旧版本安装
    /// </summary>
    private void CleanOldInstallation(string gamePath)
    {
        try
        {
            var bepinexPath = PathHelper.GetBepInExPath(gamePath);
            var winhttpDll = PathHelper.GetWinhttpDllPath(gamePath);

            if (Directory.Exists(bepinexPath))
            {
                _logger?.Info("删除旧版本 BepInEx...");
                FileSystemService.DeleteDirectoryRecursive(bepinexPath);
            }

            if (File.Exists(winhttpDll))
            {
                File.Delete(winhttpDll);
            }

            _logger?.Success("清理完成");
        }
        catch (Exception ex)
        {
            _logger?.Warning($"清理失败: {ex.Message}");
        }
    }
}
