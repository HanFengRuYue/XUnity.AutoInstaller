using System;
using Microsoft.UI.Xaml;

namespace XUnity_AutoInstaller.Models;

/// <summary>
/// TextMeshPro 字体资源信息
/// </summary>
public class FontResourceInfo
{
    /// <summary>
    /// 字体名称（例如: SourceHanSans）
    /// </summary>
    public string FontName { get; set; } = string.Empty;

    /// <summary>
    /// 字体中文名称（例如: 思源黑体）
    /// </summary>
    public string? ChineseName { get; set; }

    /// <summary>
    /// Unity 版本（例如: 2018-4-36）
    /// </summary>
    public string UnityVersion { get; set; } = string.Empty;

    /// <summary>
    /// 文件名（例如: SourceHanSans_U2018-4-36，无扩展名）
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }

    public DateTime LastModified { get; set; }

    /// <summary>
    /// 是否已缓存（下载到本地缓存目录）
    /// </summary>
    public bool IsCached { get; set; }

    /// <summary>
    /// 是否已安装到游戏（复制到游戏的 BepInEx\fonts 目录）
    /// </summary>
    public bool IsInstalled { get; set; }

    /// <summary>
    /// 是否推荐（Unity 版本完全匹配）
    /// </summary>
    public bool IsRecommended { get; set; }

    /// <summary>
    /// UI 显示名称（优先显示中文名）
    /// </summary>
    public string DisplayName => $"{(string.IsNullOrEmpty(ChineseName) ? FontName : ChineseName)} (Unity {UnityVersion})";

    /// <summary>
    /// 用于显示和排序的字体名称（优先中文名）
    /// </summary>
    public string DisplayFontName => string.IsNullOrEmpty(ChineseName) ? FontName : $"{ChineseName} ({FontName})";

    /// <summary>
    /// 用于排序的字体名称（使用中文名或英文名）
    /// </summary>
    public string SortFontName => string.IsNullOrEmpty(ChineseName) ? FontName : ChineseName;

    /// <summary>
    /// 用于 UI 显示的 Unity 版本（点分格式，例如: 2018.4.36）
    /// </summary>
    public string UnityVersionForDisplay
    {
        get
        {
            if (string.IsNullOrEmpty(UnityVersion))
                return string.Empty;

            return UnityVersion.Replace('-', '.');
        }
    }

    public string FileSizeFormatted
    {
        get
        {
            if (FileSize < 1024)
                return $"{FileSize} B";
            else if (FileSize < 1024 * 1024)
                return $"{FileSize / 1024.0:F2} KB";
            else if (FileSize < 1024 * 1024 * 1024)
                return $"{FileSize / (1024.0 * 1024.0):F2} MB";
            else
                return $"{FileSize / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }
    }

    /// <summary>
    /// Unity 主版本号（用于版本匹配，例如: 2018）
    /// </summary>
    public int UnityMajorVersion
    {
        get
        {
            if (string.IsNullOrEmpty(UnityVersion))
                return 0;

            var parts = UnityVersion.Split('-');
            if (parts.Length > 0 && int.TryParse(parts[0], out int major))
                return major;

            return 0;
        }
    }

    public Visibility IsDownloadButtonVisible => !IsCached && !IsInstalled ? Visibility.Visible : Visibility.Collapsed;

    public Visibility IsInstallButtonVisible => IsCached && !IsInstalled ? Visibility.Visible : Visibility.Collapsed;

    public Visibility IsInstalledStatusVisible => IsInstalled ? Visibility.Visible : Visibility.Collapsed;

    public Visibility RecommendedBadgeVisibility => IsRecommended ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// 用于配置文件的字体路径（例如: BepInEx\fonts\SourceHanSans_U2018-4-36）
    /// </summary>
    public string ConfigPath => $"BepInEx\\fonts\\{FileName}";
}
