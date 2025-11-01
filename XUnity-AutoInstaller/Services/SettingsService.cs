using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Text.Json;
using XUnity_AutoInstaller.Models;
using System.Reflection;
using Microsoft.Win32;

namespace XUnity_AutoInstaller.Services;

/// <summary>
/// 设置服务
/// 负责加载和保存应用程序设置 (使用 JSON 文件存储在 AppData)
/// </summary>
public class SettingsService
{
    // AppData path for settings
    private static readonly string AppDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "XUnity-AutoInstaller");

    private static readonly string SettingsFilePath = Path.Combine(AppDataPath, "settings.json");
    private static readonly string MigrationFlagPath = Path.Combine(AppDataPath, ".migrated");

    /// <summary>
    /// 获取设置文件路径（用于诊断和设置页面显示）
    /// </summary>
    public static string GetSettingsPath() => SettingsFilePath;

    /// <summary>
    /// 获取AppData根目录路径
    /// </summary>
    public static string GetAppDataPath() => AppDataPath;

    // Legacy registry path for migration
    private const string LEGACY_REGISTRY_PATH = @"SOFTWARE\XUnity-AutoInstaller";

    // JSON serialization options
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public SettingsService()
    {
        // Ensure AppData directory exists
        Directory.CreateDirectory(AppDataPath);

        // Perform one-time migration from registry if needed
        if (!File.Exists(MigrationFlagPath))
        {
            MigrateFromRegistry();
        }
    }

    /// <summary>
    /// 加载设置
    /// </summary>
    public AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                return settings ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"加载设置失败: {ex.Message}", LogLevel.Warning, "[Settings]");
        }

        return new AppSettings();
    }

    /// <summary>
    /// 保存设置
    /// </summary>
    public void SaveSettings(AppSettings settings)
    {
        try
        {
            // Write to temporary file first for atomic operation
            var tempPath = SettingsFilePath + ".tmp";
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(tempPath, json);

            // Replace old file with new one atomically
            File.Move(tempPath, SettingsFilePath, overwrite: true);

            LogService.Instance.Log("设置已保存", LogLevel.Debug, "[Settings]");
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"保存设置失败: {ex.Message}", LogLevel.Error, "[Settings]");
            throw;
        }
    }

    /// <summary>
    /// 从注册表迁移设置到 JSON 文件（一次性操作）
    /// </summary>
    private void MigrateFromRegistry()
    {
        try
        {
            using var registryKey = Registry.CurrentUser.OpenSubKey(LEGACY_REGISTRY_PATH);
            if (registryKey != null)
            {
                LogService.Instance.Log("检测到旧版注册表设置，开始迁移...", LogLevel.Info, "[Settings]");

                var settings = new AppSettings
                {
                    Theme = LoadThemeFromRegistry(registryKey),
                    RememberLastGamePath = LoadBoolFromRegistry(registryKey, "RememberLastGamePath", true),
                    LastGamePath = LoadStringFromRegistry(registryKey, "LastGamePath"),
                    ShowDetailedProgress = LoadBoolFromRegistry(registryKey, "ShowDetailedProgress", true),
                    DefaultBackupExisting = LoadBoolFromRegistry(registryKey, "DefaultBackupExisting", true),
                    GitHubToken = LoadStringFromRegistry(registryKey, "GitHubToken")
                };

                // Save to JSON file
                SaveSettings(settings);

                LogService.Instance.Log("设置迁移成功！", LogLevel.Info, "[Settings]");
            }
            else
            {
                LogService.Instance.Log("未检测到旧版设置，使用默认设置", LogLevel.Info, "[Settings]");
            }

            // Create migration flag file
            File.WriteAllText(MigrationFlagPath, DateTime.Now.ToString("O"));
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"设置迁移失败: {ex.Message}", LogLevel.Warning, "[Settings]");
            // Create flag anyway to prevent retry loops
            File.WriteAllText(MigrationFlagPath, DateTime.Now.ToString("O"));
        }
    }

    /// <summary>
    /// 从注册表读取主题
    /// </summary>
    private ElementTheme LoadThemeFromRegistry(RegistryKey key)
    {
        var value = key.GetValue("AppTheme");
        if (value is int themeInt && Enum.IsDefined(typeof(ElementTheme), themeInt))
        {
            return (ElementTheme)themeInt;
        }
        return ElementTheme.Default;
    }

    /// <summary>
    /// 从注册表读取布尔值
    /// </summary>
    private bool LoadBoolFromRegistry(RegistryKey key, string valueName, bool defaultValue)
    {
        var value = key.GetValue(valueName);
        if (value is int intValue)
        {
            return intValue != 0;
        }
        return defaultValue;
    }

    /// <summary>
    /// 从注册表读取字符串
    /// </summary>
    private string? LoadStringFromRegistry(RegistryKey key, string valueName)
    {
        return key.GetValue(valueName) as string;
    }

    /// <summary>
    /// 应用主题
    /// </summary>
    public static void ApplyTheme(ElementTheme theme)
    {
        if (App.MainWindow?.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme;
        }
    }

    /// <summary>
    /// 获取当前应用版本
    /// </summary>
    public static string GetAppVersion()
    {
        // For unpackaged apps, use Assembly version instead of Package.Current
        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version != null ? $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}" : "1.0.0.0";
    }
}
