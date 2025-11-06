namespace XUnity_AutoInstaller.Models;

/// <summary>
/// 游戏信息
/// </summary>
public class GameInfo
{
    public string Path { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public GameEngine Engine { get; set; } = GameEngine.Unknown;

    public string? ExecutablePath { get; set; }
}
