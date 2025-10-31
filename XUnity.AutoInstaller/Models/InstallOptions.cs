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

    /// <summary>
    /// 是否在安装后自动启动游戏生成配置文件
    /// </summary>
    public bool LaunchGameToGenerateConfig { get; set; } = true;

    /// <summary>
    /// 配置文件生成超时时间（秒）
    /// </summary>
    public int ConfigGenerationTimeout { get; set; } = 60;
}
