namespace XUnity.AutoInstaller.Models;

/// <summary>
/// 安装选项
/// </summary>
public class InstallOptions
{
    /// <summary>
    /// 是否备份现有安装
    /// </summary>
    public bool BackupExisting { get; set; } = true;

    /// <summary>
    /// 是否清理旧版本
    /// </summary>
    public bool CleanOldVersion { get; set; }

    /// <summary>
    /// 是否创建桌面快捷方式
    /// </summary>
    public bool CreateShortcut { get; set; }

    /// <summary>
    /// 是否使用推荐配置
    /// </summary>
    public bool UseRecommendedConfig { get; set; } = true;

    /// <summary>
    /// 目标平台
    /// </summary>
    public Platform TargetPlatform { get; set; } = Platform.x64;

    /// <summary>
    /// BepInEx 版本（null 表示自动选择最新版）
    /// </summary>
    public string? BepInExVersion { get; set; }

    /// <summary>
    /// XUnity 版本（null 表示自动选择最新版）
    /// </summary>
    public string? XUnityVersion { get; set; }
}
