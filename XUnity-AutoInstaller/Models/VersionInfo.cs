using System;

namespace XUnity_AutoInstaller.Models;

/// <summary>
/// 版本信息
/// </summary>
public class VersionInfo
{
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 版本号（例如: v5.4.22）
    /// </summary>
    public string Version { get; set; } = string.Empty;

    public DateTime ReleaseDate { get; set; }

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }

    public string DownloadUrl { get; set; } = string.Empty;

    public bool IsPrerelease { get; set; }

    public PackageType PackageType { get; set; }

    /// <summary>
    /// 适用平台（可能为空）
    /// </summary>
    public Platform? TargetPlatform { get; set; }
}
