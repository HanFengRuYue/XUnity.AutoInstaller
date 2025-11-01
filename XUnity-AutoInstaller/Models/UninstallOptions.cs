namespace XUnity_AutoInstaller.Models;

/// <summary>
/// 卸载选项
/// </summary>
public class UninstallOptions
{
    /// <summary>
    /// 是否只卸载 BepInEx（保留 XUnity）
    /// 预留选项，当前版本未使用
    /// </summary>
    public bool UninstallBepInExOnly { get; set; } = false;

    /// <summary>
    /// 是否只卸载 XUnity（保留 BepInEx）
    /// 预留选项，当前版本未使用
    /// </summary>
    public bool UninstallXUnityOnly { get; set; } = false;

    /// <summary>
    /// 是否保留配置文件
    /// 预留选项，当前版本未使用
    /// </summary>
    public bool KeepConfigs { get; set; } = false;
}
