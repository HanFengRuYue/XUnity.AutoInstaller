using System;

namespace XUnity_AutoInstaller.Models;

/// <summary>
/// 版本信息
/// </summary>
public class VersionInfo
{
    /// <summary>
    /// 版本显示名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 版本号（例如: v5.4.22）
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 发布日期
    /// </summary>
    public DateTime ReleaseDate { get; set; }

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 下载 URL
    /// </summary>
    public string DownloadUrl { get; set; } = string.Empty;

    /// <summary>
    /// 是否为预发布版本
    /// </summary>
    public bool IsPrerelease { get; set; }

    /// <summary>
    /// 包类型
    /// </summary>
    public PackageType PackageType { get; set; }

    /// <summary>
    /// 适用平台（可能为空）
    /// </summary>
    public Platform? TargetPlatform { get; set; }
}
