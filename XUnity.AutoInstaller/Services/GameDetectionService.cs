using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using XUnity.AutoInstaller.Models;
using XUnity.AutoInstaller.Utils;

namespace XUnity.AutoInstaller.Services;

/// <summary>
/// 游戏检测服务
/// 负责检测游戏路径、引擎类型和安装状态
/// </summary>
public class GameDetectionService
{
    /// <summary>
    /// 检测游戏引擎类型
    /// </summary>
    public static GameEngine DetectGameEngine(string gamePath)
    {
        if (!Directory.Exists(gamePath))
        {
            return GameEngine.Unknown;
        }

        // 检查是否为 IL2CPP（查找 GameAssembly.dll）
        var gameAssemblyPath = Path.Combine(gamePath, "GameAssembly.dll");
        if (File.Exists(gameAssemblyPath))
        {
            return GameEngine.UnityIL2CPP;
        }

        // 检查是否为 Unity Mono（查找 UnityPlayer.dll 和 Managed 文件夹）
        var unityPlayerPath = Path.Combine(gamePath, "UnityPlayer.dll");
        var managedPath = Path.Combine(gamePath, $"{Path.GetFileName(gamePath)}_Data", "Managed");

        if (File.Exists(unityPlayerPath) || Directory.Exists(managedPath))
        {
            return GameEngine.UnityMono;
        }

        // 搜索 *_Data 文件夹（Unity 标准结构）
        var dataFolders = Directory.GetDirectories(gamePath, "*_Data", SearchOption.TopDirectoryOnly);
        if (dataFolders.Length > 0)
        {
            var managedFolder = Path.Combine(dataFolders[0], "Managed");
            if (Directory.Exists(managedFolder))
            {
                return GameEngine.UnityMono;
            }
        }

        return GameEngine.Unknown;
    }

    /// <summary>
    /// 获取游戏信息
    /// </summary>
    public static GameInfo GetGameInfo(string gamePath)
    {
        var gameInfo = new GameInfo
        {
            Path = gamePath,
            Name = Path.GetFileName(gamePath),
            Engine = DetectGameEngine(gamePath)
        };

        // 查找游戏可执行文件
        var exeFiles = Directory.GetFiles(gamePath, "*.exe", SearchOption.TopDirectoryOnly);
        if (exeFiles.Length > 0)
        {
            // 优先选择不包含 "UnityCrashHandler" 和 "Uninstall" 的可执行文件
            gameInfo.ExecutablePath = exeFiles
                .Where(f => !f.Contains("UnityCrashHandler", StringComparison.OrdinalIgnoreCase) &&
                           !f.Contains("Uninstall", StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault() ?? exeFiles[0];
        }

        return gameInfo;
    }

    /// <summary>
    /// 检测 BepInEx 和 XUnity 的安装状态
    /// </summary>
    public static InstallationStatus DetectInstallationStatus(string gamePath)
    {
        var status = new InstallationStatus();

        // 检测 BepInEx
        var bepinexPath = PathHelper.GetBepInExPath(gamePath);
        var winhttpDll = PathHelper.GetWinhttpDllPath(gamePath);

        if (Directory.Exists(bepinexPath) && File.Exists(winhttpDll))
        {
            status.IsBepInExInstalled = true;
            status.BepInExVersion = DetectBepInExVersion(gamePath);
            status.BepInExPlatform = DetectBepInExPlatform(gamePath);
        }

        // 检测 XUnity.AutoTranslator
        var xunityPath = PathHelper.GetXUnityPath(gamePath);
        if (Directory.Exists(xunityPath))
        {
            status.IsXUnityInstalled = true;
            status.XUnityVersion = DetectXUnityVersion(gamePath);
            status.TranslationEngine = DetectTranslationEngine(gamePath);
        }

        return status;
    }

    /// <summary>
    /// 检测 BepInEx 版本
    /// </summary>
    private static string? DetectBepInExVersion(string gamePath)
    {
        try
        {
            // 方法 1: 从 BepInEx.dll 读取文件版本
            var bepinexDll = Path.Combine(PathHelper.GetBepInExCorePath(gamePath), "BepInEx.dll");
            if (File.Exists(bepinexDll))
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(bepinexDll);
                if (!string.IsNullOrEmpty(versionInfo.ProductVersion))
                {
                    return versionInfo.ProductVersion;
                }
            }

            // 方法 2: 从 changelog.txt 读取版本
            var changelogPath = Path.Combine(PathHelper.GetBepInExPath(gamePath), "changelog.txt");
            if (File.Exists(changelogPath))
            {
                var firstLine = File.ReadLines(changelogPath).FirstOrDefault();
                if (!string.IsNullOrEmpty(firstLine))
                {
                    return firstLine.Trim();
                }
            }
        }
        catch
        {
            // 忽略错误
        }

        return "Unknown";
    }

    /// <summary>
    /// 检测 BepInEx 平台
    /// </summary>
    private static string? DetectBepInExPlatform(string gamePath)
    {
        var engine = DetectGameEngine(gamePath);
        return engine switch
        {
            GameEngine.UnityIL2CPP => "IL2CPP",
            GameEngine.UnityMono => "Mono",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// 检测 XUnity.AutoTranslator 版本
    /// </summary>
    private static string? DetectXUnityVersion(string gamePath)
    {
        try
        {
            var xunityDll = Path.Combine(PathHelper.GetXUnityPath(gamePath), "XUnity.AutoTranslator.Plugin.Core.dll");
            if (File.Exists(xunityDll))
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(xunityDll);
                if (!string.IsNullOrEmpty(versionInfo.ProductVersion))
                {
                    return versionInfo.ProductVersion;
                }
            }
        }
        catch
        {
            // 忽略错误
        }

        return "Unknown";
    }

    /// <summary>
    /// 检测翻译引擎
    /// </summary>
    private static string? DetectTranslationEngine(string gamePath)
    {
        try
        {
            var configPath = PathHelper.GetXUnityConfigFile(gamePath);
            if (File.Exists(configPath))
            {
                var configData = IniParser.Parse(configPath);
                return IniParser.GetValue(configData, "Service", "Endpoint", "GoogleTranslate");
            }
        }
        catch
        {
            // 忽略错误
        }

        return "GoogleTranslate";
    }

    /// <summary>
    /// 自动检测系统中的游戏
    /// </summary>
    /// <returns>找到的游戏列表</returns>
    public static List<GameInfo> AutoDetectGames()
    {
        var games = new List<GameInfo>();
        var searchedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 获取所有可能的游戏目录
        var gameDirs = PathHelper.FindGameDirectories();

        foreach (var dir in gameDirs)
        {
            if (!Directory.Exists(dir))
            {
                continue;
            }

            try
            {
                // 遍历子目录
                foreach (var subDir in Directory.GetDirectories(dir))
                {
                    if (searchedPaths.Contains(subDir))
                    {
                        continue;
                    }

                    searchedPaths.Add(subDir);

                    // 检查是否为有效游戏目录
                    if (PathHelper.IsValidGameDirectory(subDir))
                    {
                        var gameInfo = GetGameInfo(subDir);
                        if (gameInfo.Engine != GameEngine.Unknown)
                        {
                            games.Add(gameInfo);
                        }
                    }
                }
            }
            catch
            {
                // 忽略访问被拒绝等错误
                continue;
            }
        }

        return games;
    }
}
