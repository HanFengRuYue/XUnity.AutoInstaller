namespace XUnity_AutoInstaller.Models;

/// <summary>
/// Represents the source for downloading BepInEx and XUnity versions
/// </summary>
public enum DownloadSourceType
{
    /// <summary>
    /// Official GitHub repositories (default)
    /// </summary>
    GitHub = 0,

    /// <summary>
    /// Mirror website via WebDAV
    /// </summary>
    Mirror = 1
}
