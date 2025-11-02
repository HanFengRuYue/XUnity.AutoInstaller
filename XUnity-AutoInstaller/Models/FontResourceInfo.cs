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
    /// Unity 版本（例如: 2018-4-36）
    /// </summary>
    public string UnityVersion { get; set; } = string.Empty;

    /// <summary>
    /// 文件名（例如: SourceHanSans_U2018-4-36，无扩展名）
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// 下载 URL
    /// </summary>
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 最后修改日期
    /// </summary>
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
    /// UI 显示名称
    /// </summary>
    public string DisplayName => $"{FontName} (Unity {UnityVersion})";

    /// <summary>
    /// 格式化的文件大小
    /// </summary>
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

    /// <summary>
    /// 下载按钮是否可见
    /// </summary>
    public Visibility IsDownloadButtonVisible => !IsCached && !IsInstalled ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// 安装按钮是否可见
    /// </summary>
    public Visibility IsInstallButtonVisible => IsCached && !IsInstalled ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// 已安装状态是否可见
    /// </summary>
    public Visibility IsInstalledStatusVisible => IsInstalled ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// 用于配置文件的字体路径（例如: BepInEx\fonts\SourceHanSans_U2018-4-36）
    /// </summary>
    public string ConfigPath => $"BepInEx\\fonts\\{FileName}";
}
