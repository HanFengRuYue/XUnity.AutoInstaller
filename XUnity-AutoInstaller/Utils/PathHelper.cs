using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using XUnity_AutoInstaller.Models;

namespace XUnity_AutoInstaller.Utils;

/// <summary>
/// 路径辅助工具类
/// </summary>
public static class PathHelper
{
    /// <summary>
    /// 常见游戏平台的安装路径
    /// </summary>
    public static readonly string[] CommonGamePaths = new[]
    {
        @"C:\Program Files (x86)\Steam\steamapps\common",
        @"C:\Program Files\Steam\steamapps\common",
        @"D:\SteamLibrary\steamapps\common",
        @"E:\SteamLibrary\steamapps\common",
        @"C:\Program Files\Epic Games",
        @"C:\Program Files (x86)\GOG Galaxy\Games",
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Programs\Common")
    };

    public static string GetBepInExPath(string gamePath) => Path.Combine(gamePath, "BepInEx");

    public static string GetBepInExConfigPath(string gamePath) => Path.Combine(GetBepInExPath(gamePath), "config");

    public static string GetBepInExPluginsPath(string gamePath) => Path.Combine(GetBepInExPath(gamePath), "plugins");

    public static string GetBepInExCorePath(string gamePath) => Path.Combine(GetBepInExPath(gamePath), "core");

    public static string GetXUnityPath(string gamePath) => Path.Combine(GetBepInExPluginsPath(gamePath), "XUnity.AutoTranslator");

    public static string GetBepInExConfigFile(string gamePath) => Path.Combine(GetBepInExConfigPath(gamePath), "BepInEx.cfg");

    public static string GetXUnityConfigFile(string gamePath) => Path.Combine(GetBepInExConfigPath(gamePath), "AutoTranslatorConfig.ini");

    /// <summary>
    /// BepInEx 入口点
    /// </summary>
    public static string GetWinhttpDllPath(string gamePath) => Path.Combine(gamePath, "winhttp.dll");

    public static string GetDoorstopConfigPath(string gamePath) => Path.Combine(gamePath, "doorstop_config.ini");

    /// <summary>
    /// 尝试从注册表获取 Steam 安装路径
    /// </summary>
    public static string? GetSteamPath()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            return key?.GetValue("SteamPath") as string;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 搜索所有可能的游戏目录
    /// </summary>
    /// <returns>找到的游戏目录列表</returns>
    public static List<string> FindGameDirectories()
    {
        var directories = new List<string>();

        // 添加 Steam 路径
        var steamPath = GetSteamPath();
        if (!string.IsNullOrEmpty(steamPath))
        {
            var commonPath = Path.Combine(steamPath, "steamapps", "common");
            if (Directory.Exists(commonPath))
            {
                directories.Add(commonPath);
            }
        }

        // 添加其他常见路径
        foreach (var path in CommonGamePaths)
        {
            if (Directory.Exists(path))
            {
                directories.Add(path);
            }
        }

        return directories;
    }

    /// <summary>
    /// 验证是否为有效的游戏目录（包含可执行文件）
    /// </summary>
    public static bool IsValidGameDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            return false;
        }

        // 检查是否包含 .exe 文件
        var exeFiles = Directory.GetFiles(path, "*.exe", SearchOption.TopDirectoryOnly);
        return exeFiles.Length > 0;
    }

    /// <summary>
    /// 获取下载缓存目录（AppData）
    /// </summary>
    public static string GetTempDownloadDirectory()
    {
        // 使用 AppData 而非临时目录，便于管理和持久化
        var cachePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "XUnity.AutoInstaller",
            "Cache");
        Directory.CreateDirectory(cachePath);
        return cachePath;
    }

    public static bool IsCachedVersion(VersionInfo version)
    {
        try
        {
            var tempDir = GetTempDownloadDirectory();
            var fileName = Path.GetFileName(new Uri(version.DownloadUrl).LocalPath);
            var downloadPath = Path.Combine(tempDir, fileName);
            return File.Exists(downloadPath);
        }
        catch
        {
            return false;
        }
    }

    public static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    public static string GetFontCachePath()
    {
        var fontCachePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "XUnity.AutoInstaller",
            "Cache",
            "Fonts");
        Directory.CreateDirectory(fontCachePath);
        return fontCachePath;
    }

    /// <summary>
    /// BepInEx\fonts
    /// </summary>
    public static string GetGameFontPath(string gamePath)
    {
        return Path.Combine(GetBepInExPath(gamePath), "fonts");
    }

    public static bool IsFontCached(string fileName)
    {
        try
        {
            var cachePath = GetFontCachePath();
            var filePath = Path.Combine(cachePath, fileName);
            return File.Exists(filePath);
        }
        catch
        {
            return false;
        }
    }

    public static bool IsFontInstalledInGame(string gamePath, string fileName)
    {
        try
        {
            var gameFontPath = GetGameFontPath(gamePath);
            var filePath = Path.Combine(gameFontPath, fileName);
            return File.Exists(filePath);
        }
        catch
        {
            return false;
        }
    }
}
