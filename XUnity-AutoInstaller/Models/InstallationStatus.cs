namespace XUnity_AutoInstaller.Models;

/// <summary>
/// 安装状态信息
/// </summary>
public class InstallationStatus
{
    public bool IsBepInExInstalled { get; set; }

    public string? BepInExVersion { get; set; }

    public string? BepInExPlatform { get; set; }

    public bool IsXUnityInstalled { get; set; }

    public string? XUnityVersion { get; set; }

    public string? TranslationEngine { get; set; }
}
