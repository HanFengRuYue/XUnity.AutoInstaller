namespace XUnity.AutoInstaller.Models;

/// <summary>
/// 游戏信息
/// </summary>
public class GameInfo
{
    /// <summary>
    /// 游戏根目录路径
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 游戏名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 游戏引擎类型
    /// </summary>
    public GameEngine Engine { get; set; } = GameEngine.Unknown;

    /// <summary>
    /// 游戏可执行文件路径
    /// </summary>
    public string? ExecutablePath { get; set; }
}
