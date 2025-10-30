namespace XUnity.AutoInstaller.Models;

/// <summary>
/// 目标平台架构
/// </summary>
public enum Platform
{
    /// <summary>
    /// x86 32位
    /// </summary>
    x86,

    /// <summary>
    /// x64 64位
    /// </summary>
    x64,

    /// <summary>
    /// IL2CPP x86 32位
    /// </summary>
    IL2CPP_x86,

    /// <summary>
    /// IL2CPP x64 64位
    /// </summary>
    IL2CPP_x64,

    /// <summary>
    /// ARM64 架构
    /// </summary>
    ARM64
}
