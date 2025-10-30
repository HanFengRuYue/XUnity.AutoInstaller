using Microsoft.UI.Xaml;

namespace XUnity.AutoInstaller.Models;

/// <summary>
/// 应用程序设置
/// </summary>
public class AppSettings
{
    /// <summary>
    /// 应用主题（Light/Dark/Default）
    /// </summary>
    public ElementTheme Theme { get; set; } = ElementTheme.Default;

    /// <summary>
    /// 启动时自动检测游戏
    /// </summary>
    public bool AutoDetectGameOnStartup { get; set; } = false;

    /// <summary>
    /// 记住上次选择的游戏路径
    /// </summary>
    public bool RememberLastGamePath { get; set; } = true;

    /// <summary>
    /// 上次选择的游戏路径
    /// </summary>
    public string? LastGamePath { get; set; }

    /// <summary>
    /// 启动时检查应用更新
    /// </summary>
    public bool CheckUpdateOnStartup { get; set; } = false;

    /// <summary>
    /// 下载时显示详细进度
    /// </summary>
    public bool ShowDetailedProgress { get; set; } = true;

    /// <summary>
    /// 默认备份现有安装
    /// </summary>
    public bool DefaultBackupExisting { get; set; } = true;

    /// <summary>
    /// 默认使用推荐配置
    /// </summary>
    public bool DefaultUseRecommendedConfig { get; set; } = true;
}
