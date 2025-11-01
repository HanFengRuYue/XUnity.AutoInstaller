using System.Text.Json.Serialization;
using XUnity_AutoInstaller.Models;

namespace XUnity_AutoInstaller.Services;

/// <summary>
/// JSON serializer context for source generation to eliminate trimming warnings.
/// Provides compile-time serialization for AppSettings and SnapshotInfo types.
/// </summary>
[JsonSerializable(typeof(AppSettings))]
[JsonSerializable(typeof(SnapshotInfo))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
internal partial class AppJsonSerializerContext : JsonSerializerContext
{
}
