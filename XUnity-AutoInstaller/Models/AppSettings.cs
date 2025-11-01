using Microsoft.UI.Xaml;

namespace XUnity_AutoInstaller.Models;

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
    /// 记住上次选择的游戏路径
    /// </summary>
    public bool RememberLastGamePath { get; set; } = true;

    /// <summary>
    /// 上次选择的游戏路径
    /// </summary>
    public string? LastGamePath { get; set; }

    /// <summary>
    /// 下载时显示详细进度
    /// </summary>
    public bool ShowDetailedProgress { get; set; } = true;

    /// <summary>
    /// 默认备份现有安装
    /// </summary>
    public bool DefaultBackupExisting { get; set; } = true;

    /// <summary>
    /// GitHub Personal Access Token (可选，用于提高API速率限制)
    /// 未认证: 60次/小时，认证后: 5000次/小时
    /// </summary>
    public string? GitHubToken { get; set; }
}
