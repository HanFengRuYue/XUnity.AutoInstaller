using System;
using System.IO;
using System.Threading.Tasks;
using XUnity_AutoInstaller.Models;
using XUnity_AutoInstaller.Utils;

namespace XUnity_AutoInstaller.Services;

/// <summary>
/// 卸载服务
/// 负责 BepInEx 和 XUnity 的完整卸载流程
/// </summary>
public class UninstallationService
{
    private readonly LogWriter? _logger;

    public UninstallationService(LogWriter? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// 执行完整卸载流程
    /// </summary>
    /// <param name="gamePath">游戏路径</param>
    /// <param name="options">卸载选项</param>
    /// <param name="progress">进度报告</param>
    /// <returns>是否成功卸载</returns>
    public async Task<bool> UninstallAsync(string gamePath, UninstallOptions options, IProgress<(int percentage, string message)>? progress = null)
    {
        try
        {
            progress?.Report((0, "开始卸载..."));
            _logger?.Info("开始卸载流程");

            // 1. 验证游戏路径
            if (!PathHelper.IsValidGameDirectory(gamePath))
            {
                throw new Exception("无效的游戏目录");
            }

            // 2. 检测安装状态
            progress?.Report((10, "检测安装状态..."));
            var status = GameDetectionService.DetectInstallationStatus(gamePath);

            if (!status.IsBepInExInstalled && !status.IsXUnityInstalled)
            {
                throw new Exception("未检测到 BepInEx 或 XUnity 安装，无需卸载");
            }

            _logger?.Info($"检测到安装状态 - BepInEx: {status.IsBepInExInstalled}, XUnity: {status.IsXUnityInstalled}");

            // 3. 删除 BepInEx 目录
            progress?.Report((30, "删除 BepInEx 目录..."));
            await DeleteBepInExDirectory(gamePath);

            // 4. 删除 winhttp.dll（BepInEx 入口点）
            progress?.Report((60, "删除 BepInEx 入口文件..."));
            DeleteWinhttpDll(gamePath);

            // 5. 删除 doorstop_config.ini
            progress?.Report((70, "删除 Doorstop 配置..."));
            DeleteDoorstopConfig(gamePath);

            // 6. 删除其他相关文件
            progress?.Report((80, "删除其他相关文件..."));
            DeleteOtherFiles(gamePath);

            // 7. 验证卸载完成
            progress?.Report((90, "验证卸载结果..."));
            VerifyUninstallation(gamePath);

            progress?.Report((100, "卸载完成!"));
            _logger?.Success("卸载流程完成");
            return true;
        }
        catch (Exception ex)
        {
            _logger?.Error($"卸载失败: {ex.Message}");
            progress?.Report((0, $"卸载失败: {ex.Message}"));
            throw;
        }
    }

    /// <summary>
    /// 删除 BepInEx 目录
    /// </summary>
    private async Task DeleteBepInExDirectory(string gamePath)
    {
        await Task.Run(() =>
        {
            try
            {
                var bepinexPath = PathHelper.GetBepInExPath(gamePath);

                if (Directory.Exists(bepinexPath))
                {
                    _logger?.Info($"删除 BepInEx 目录: {bepinexPath}");
                    FileSystemService.DeleteDirectoryRecursive(bepinexPath);
                    _logger?.Success("BepInEx 目录已删除");
                }
                else
                {
                    _logger?.Info("BepInEx 目录不存在，跳过");
                }
            }
            catch (Exception ex)
            {
                _logger?.Error($"删除 BepInEx 目录失败: {ex.Message}");
                throw new Exception($"删除 BepInEx 目录失败: {ex.Message}", ex);
            }
        });
    }

    /// <summary>
    /// 删除 winhttp.dll（BepInEx 入口点）
    /// </summary>
    private void DeleteWinhttpDll(string gamePath)
    {
        try
        {
            var winhttpDll = PathHelper.GetWinhttpDllPath(gamePath);

            if (File.Exists(winhttpDll))
            {
                _logger?.Info($"删除 winhttp.dll: {winhttpDll}");
                File.Delete(winhttpDll);
                _logger?.Success("winhttp.dll 已删除");
            }
            else
            {
                _logger?.Info("winhttp.dll 不存在，跳过");
            }
        }
        catch (Exception ex)
        {
            _logger?.Warning($"删除 winhttp.dll 失败: {ex.Message}");
            // 继续执行，不阻止卸载流程
        }
    }

    /// <summary>
    /// 删除 doorstop_config.ini
    /// </summary>
    private void DeleteDoorstopConfig(string gamePath)
    {
        try
        {
            var doorstopConfig = PathHelper.GetDoorstopConfigPath(gamePath);

            if (File.Exists(doorstopConfig))
            {
                _logger?.Info($"删除 doorstop_config.ini: {doorstopConfig}");
                File.Delete(doorstopConfig);
                _logger?.Success("doorstop_config.ini 已删除");
            }
            else
            {
                _logger?.Info("doorstop_config.ini 不存在，跳过");
            }
        }
        catch (Exception ex)
        {
            _logger?.Warning($"删除 doorstop_config.ini 失败: {ex.Message}");
            // 继续执行，不阻止卸载流程
        }
    }

    /// <summary>
    /// 删除其他 BepInEx 相关文件
    /// </summary>
    private void DeleteOtherFiles(string gamePath)
    {
        try
        {
            // 删除 .doorstop_version 文件（如果存在）
            var doorstopVersion = Path.Combine(gamePath, ".doorstop_version");
            if (File.Exists(doorstopVersion))
            {
                _logger?.Info($"删除 .doorstop_version: {doorstopVersion}");
                File.Delete(doorstopVersion);
            }

            // 删除可能的其他 Doorstop 文件
            var doorstopDll = Path.Combine(gamePath, "doorstop.dll");
            if (File.Exists(doorstopDll))
            {
                _logger?.Info($"删除 doorstop.dll: {doorstopDll}");
                File.Delete(doorstopDll);
            }

            // 删除 changelog.txt（BepInEx 变更日志）
            var changelogPath = Path.Combine(gamePath, "changelog.txt");
            if (File.Exists(changelogPath))
            {
                _logger?.Info($"删除 changelog.txt: {changelogPath}");
                File.Delete(changelogPath);
            }

            _logger?.Success("其他相关文件已清理");
        }
        catch (Exception ex)
        {
            _logger?.Warning($"清理其他文件时发生错误: {ex.Message}");
            // 继续执行，不阻止卸载流程
        }
    }

    /// <summary>
    /// 验证卸载完成
    /// </summary>
    private void VerifyUninstallation(string gamePath)
    {
        try
        {
            var status = GameDetectionService.DetectInstallationStatus(gamePath);

            if (!status.IsBepInExInstalled && !status.IsXUnityInstalled)
            {
                _logger?.Success("验证成功: 所有组件已完全卸载");
            }
            else
            {
                if (status.IsBepInExInstalled)
                {
                    _logger?.Warning("警告: BepInEx 可能未完全卸载");
                }
                if (status.IsXUnityInstalled)
                {
                    _logger?.Warning("警告: XUnity 可能未完全卸载");
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.Warning($"验证卸载状态时发生错误: {ex.Message}");
        }
    }
}
