using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using XUnity_AutoInstaller.Models;

namespace XUnity_AutoInstaller.Services;

/// <summary>
/// Interface for fetching version information and downloading files from various sources
/// </summary>
public interface IVersionFetcher
{
    /// <summary>
    /// Get available BepInEx versions
    /// </summary>
    /// <param name="maxCount">Maximum number of versions to retrieve</param>
    /// <returns>List of BepInEx versions</returns>
    Task<List<VersionInfo>> GetBepInExVersionsAsync(int maxCount = 10);

    /// <summary>
    /// Get available XUnity.AutoTranslator versions
    /// </summary>
    /// <param name="maxCount">Maximum number of versions to retrieve</param>
    /// <returns>List of XUnity versions</returns>
    Task<List<VersionInfo>> GetXUnityVersionsAsync(int maxCount = 10);

    /// <summary>
    /// Download a file from the source with progress reporting
    /// </summary>
    /// <param name="url">URL or path to download from</param>
    /// <param name="destinationPath">Local file path to save to</param>
    /// <param name="progress">Progress reporting (0-100%)</param>
    Task DownloadFileAsync(string url, string destinationPath, IProgress<int>? progress = null);

    /// <summary>
    /// Validate that the source is accessible
    /// </summary>
    /// <returns>True if connection successful, false otherwise</returns>
    Task<bool> ValidateConnectionAsync();

    /// <summary>
    /// Get the source type name for display purposes
    /// </summary>
    string SourceName { get; }
}
