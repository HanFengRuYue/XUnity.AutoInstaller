using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using XUnity_AutoInstaller.Models;
using XUnity_AutoInstaller.Utils;

namespace XUnity_AutoInstaller.Services;

/// <summary>
/// TextMeshPro 字体管理服务
/// 协调字体的获取、下载、安装和状态管理
/// </summary>
public class FontManagementService : IDisposable
{
    private readonly FontMirrorClient _fontMirrorClient;
    private readonly UnityVersionDetector _unityVersionDetector;

    public FontManagementService()
    {
        _fontMirrorClient = new FontMirrorClient();
        _unityVersionDetector = new UnityVersionDetector();

        LogService.Instance.Log("字体管理服务已初始化", LogLevel.Info, "[FontManagement]");
    }

    /// <summary>
    /// 获取所有可用的字体资源（带缓存和安装状态）
    /// </summary>
    public async Task<List<FontResourceInfo>> GetAvailableFontsAsync(string? gamePath = null)
    {
        try
        {
            // 从镜像获取字体列表
            var fonts = await _fontMirrorClient.GetAvailableFontsAsync();

            // 更新每个字体的缓存和安装状态
            foreach (var font in fonts)
            {
                font.IsCached = PathHelper.IsFontCached(font.FileName);

                if (!string.IsNullOrEmpty(gamePath))
                {
                    font.IsInstalled = PathHelper.IsFontInstalledInGame(gamePath, font.FileName);
                }
            }

            LogService.Instance.Log($"获取 {fonts.Count} 个可用字体", LogLevel.Info, "[FontManagement]");

            return fonts;
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"获取可用字体失败: {ex.Message}", LogLevel.Error, "[FontManagement]");
            throw;
        }
    }

    /// <summary>
    /// 获取推荐的字体列表（根据游戏 Unity 版本排序）
    /// </summary>
    public async Task<List<FontResourceInfo>> GetRecommendedFontsAsync(string gamePath)
    {
        try
        {
            // 检测游戏的 Unity 版本
            var unityVersion = await _unityVersionDetector.DetectUnityVersionAsync(gamePath);

            if (string.IsNullOrEmpty(unityVersion))
            {
                LogService.Instance.Log("无法检测 Unity 版本，返回所有字体（按日期排序）", LogLevel.Warning, "[FontManagement]");
                var allFonts = await GetAvailableFontsAsync(gamePath);
                return allFonts.OrderByDescending(f => f.LastModified).ToList();
            }

            LogService.Instance.Log($"检测到游戏 Unity 版本: {unityVersion}，计算字体匹配度", LogLevel.Info, "[FontManagement]");

            // 获取所有字体并计算相似度得分
            var fonts = await GetAvailableFontsAsync(gamePath);

            var fontsWithScore = fonts.Select(f => new
            {
                Font = f,
                Score = _unityVersionDetector.CalculateVersionSimilarity(unityVersion, f.UnityVersion)
            }).ToList();

            // 按相似度排序（相似度高的在前），相同相似度按日期排序
            var sortedFonts = fontsWithScore
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Font.LastModified)
                .Select(x => x.Font)
                .ToList();

            if (fontsWithScore.Any())
            {
                var topFont = fontsWithScore.First();
                LogService.Instance.Log(
                    $"推荐字体: {topFont.Font.DisplayName} (匹配度: {topFont.Score})",
                    LogLevel.Info,
                    "[FontManagement]");
            }

            return sortedFonts;
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"获取推荐字体失败: {ex.Message}", LogLevel.Error, "[FontManagement]");
            throw;
        }
    }

    /// <summary>
    /// 下载字体到缓存目录
    /// </summary>
    public async Task DownloadFontAsync(FontResourceInfo fontInfo, IProgress<int>? progress = null)
    {
        try
        {
            // 检查是否已缓存
            if (fontInfo.IsCached)
            {
                LogService.Instance.Log($"字体已缓存，跳过下载: {fontInfo.DisplayName}", LogLevel.Info, "[FontManagement]");
                return;
            }

            var cachePath = PathHelper.GetFontCachePath();
            var destinationPath = Path.Combine(cachePath, fontInfo.FileName);

            LogService.Instance.Log($"开始下载字体: {fontInfo.DisplayName}", LogLevel.Info, "[FontManagement]");

            await _fontMirrorClient.DownloadFontAsync(fontInfo, destinationPath, progress);

            // 更新缓存状态
            fontInfo.IsCached = true;

            LogService.Instance.Log($"字体下载成功: {fontInfo.DisplayName}", LogLevel.Info, "[FontManagement]");
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"字体下载失败 ({fontInfo.DisplayName}): {ex.Message}", LogLevel.Error, "[FontManagement]");
            throw;
        }
    }

    /// <summary>
    /// 将字体从缓存复制到游戏目录
    /// </summary>
    public async Task InstallFontToGameAsync(string gamePath, FontResourceInfo fontInfo)
    {
        try
        {
            // 检查字体是否已缓存
            if (!fontInfo.IsCached)
            {
                throw new InvalidOperationException("字体尚未下载到缓存，请先下载");
            }

            // 检查字体是否已安装
            if (fontInfo.IsInstalled)
            {
                LogService.Instance.Log($"字体已安装，跳过: {fontInfo.DisplayName}", LogLevel.Info, "[FontManagement]");
                return;
            }

            var cachePath = PathHelper.GetFontCachePath();
            var sourcePath = Path.Combine(cachePath, fontInfo.FileName);

            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException($"缓存中未找到字体文件: {sourcePath}");
            }

            // 创建游戏字体目录
            var gameFontPath = PathHelper.GetGameFontPath(gamePath);
            Directory.CreateDirectory(gameFontPath);

            var destinationPath = Path.Combine(gameFontPath, fontInfo.FileName);

            LogService.Instance.Log($"安装字体到游戏: {fontInfo.DisplayName} -> {gameFontPath}", LogLevel.Info, "[FontManagement]");

            // 复制文件
            await Task.Run(() => File.Copy(sourcePath, destinationPath, overwrite: true));

            // 更新安装状态
            fontInfo.IsInstalled = true;

            LogService.Instance.Log($"字体安装成功: {fontInfo.DisplayName}", LogLevel.Info, "[FontManagement]");
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"字体安装失败 ({fontInfo.DisplayName}): {ex.Message}", LogLevel.Error, "[FontManagement]");
            throw;
        }
    }

    /// <summary>
    /// 从游戏目录卸载字体
    /// </summary>
    public async Task UninstallFontFromGameAsync(string gamePath, FontResourceInfo fontInfo)
    {
        try
        {
            var gameFontPath = PathHelper.GetGameFontPath(gamePath);
            var fontPath = Path.Combine(gameFontPath, fontInfo.FileName);

            if (!File.Exists(fontPath))
            {
                LogService.Instance.Log($"字体未安装，跳过: {fontInfo.DisplayName}", LogLevel.Info, "[FontManagement]");
                return;
            }

            LogService.Instance.Log($"卸载字体: {fontInfo.DisplayName}", LogLevel.Info, "[FontManagement]");

            await Task.Run(() => File.Delete(fontPath));

            // 更新安装状态
            fontInfo.IsInstalled = false;

            LogService.Instance.Log($"字体卸载成功: {fontInfo.DisplayName}", LogLevel.Info, "[FontManagement]");
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"字体卸载失败 ({fontInfo.DisplayName}): {ex.Message}", LogLevel.Error, "[FontManagement]");
            throw;
        }
    }

    /// <summary>
    /// 清除缓存中的所有字体
    /// </summary>
    public async Task ClearCacheAsync()
    {
        try
        {
            var cachePath = PathHelper.GetFontCachePath();

            if (!Directory.Exists(cachePath))
            {
                LogService.Instance.Log("字体缓存目录不存在，无需清除", LogLevel.Info, "[FontManagement]");
                return;
            }

            var files = Directory.GetFiles(cachePath);

            LogService.Instance.Log($"清除 {files.Length} 个缓存字体", LogLevel.Info, "[FontManagement]");

            await Task.Run(() =>
            {
                foreach (var file in files)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        LogService.Instance.Log($"删除缓存文件失败 ({Path.GetFileName(file)}): {ex.Message}", LogLevel.Warning, "[FontManagement]");
                    }
                }
            });

            LogService.Instance.Log("字体缓存清除完成", LogLevel.Info, "[FontManagement]");
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"清除字体缓存失败: {ex.Message}", LogLevel.Error, "[FontManagement]");
            throw;
        }
    }

    /// <summary>
    /// 验证 WebDAV 镜像连接
    /// </summary>
    public async Task<bool> ValidateConnectionAsync()
    {
        try
        {
            return await _fontMirrorClient.ValidateConnectionAsync();
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"验证镜像连接失败: {ex.Message}", LogLevel.Error, "[FontManagement]");
            return false;
        }
    }

    public void Dispose()
    {
        _fontMirrorClient?.Dispose();
    }
}
