namespace XUnity_AutoInstaller.Models;

/// <summary>
/// 安装状态信息
/// </summary>
public class InstallationStatus
{
    /// <summary>
    /// BepInEx 是否已安装
    /// </summary>
    public bool IsBepInExInstalled { get; set; }

    /// <summary>
    /// BepInEx 版本号
    /// </summary>
    public string? BepInExVersion { get; set; }

    /// <summary>
    /// BepInEx 目标平台
    /// </summary>
    public string? BepInExPlatform { get; set; }

    /// <summary>
    /// XUnity.AutoTranslator 是否已安装
    /// </summary>
    public bool IsXUnityInstalled { get; set; }

    /// <summary>
    /// XUnity.AutoTranslator 版本号
    /// </summary>
    public string? XUnityVersion { get; set; }

    /// <summary>
    /// 翻译引擎名称
    /// </summary>
    public string? TranslationEngine { get; set; }
}
