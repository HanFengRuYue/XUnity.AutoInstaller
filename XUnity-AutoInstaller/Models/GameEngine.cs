namespace XUnity_AutoInstaller.Models;

/// <summary>
/// 游戏引擎类型
/// </summary>
public enum GameEngine
{
    /// <summary>
    /// 未知引擎
    /// </summary>
    Unknown,

    /// <summary>
    /// Unity Mono (标准 Unity)
    /// </summary>
    UnityMono,

    /// <summary>
    /// Unity IL2CPP (AOT 编译)
    /// </summary>
    UnityIL2CPP
}
