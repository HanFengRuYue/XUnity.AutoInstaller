using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Text.Json;
using XUnity.AutoInstaller.Models;
using System.Reflection;
using Microsoft.Win32;

namespace XUnity.AutoInstaller.Services;

/// <summary>
/// 设置服务
/// 负责加载和保存应用程序设置 (使用注册表存储，适用于 unpackaged 应用)
/// </summary>
public class SettingsService
{
    private static readonly string THEME_KEY = "AppTheme";
    private static readonly string REMEMBER_PATH_KEY = "RememberLastGamePath";
    private static readonly string LAST_GAME_PATH_KEY = "LastGamePath";
    private static readonly string SHOW_DETAILED_PROGRESS_KEY = "ShowDetailedProgress";
    private static readonly string DEFAULT_BACKUP_KEY = "DefaultBackupExisting";
    private static readonly string DEFAULT_RECOMMENDED_CONFIG_KEY = "DefaultUseRecommendedConfig";
    private static readonly string GITHUB_TOKEN_KEY = "GitHubToken";

    // Registry path for unpackaged app settings
    private const string REGISTRY_PATH = @"SOFTWARE\XUnity.AutoInstaller";

    private readonly RegistryKey _settingsKey;

    public SettingsService()
    {
        // For unpackaged apps, use registry to store settings
        // Create or open registry key for application settings
        _settingsKey = Registry.CurrentUser.CreateSubKey(REGISTRY_PATH, true)
            ?? throw new InvalidOperationException("Failed to create registry key for settings");
    }

    /// <summary>
    /// 加载设置
    /// </summary>
    public AppSettings LoadSettings()
    {
        return new AppSettings
        {
            Theme = LoadTheme(),
            RememberLastGamePath = LoadBool(REMEMBER_PATH_KEY, true),
            LastGamePath = LoadString(LAST_GAME_PATH_KEY),
            ShowDetailedProgress = LoadBool(SHOW_DETAILED_PROGRESS_KEY, true),
            DefaultBackupExisting = LoadBool(DEFAULT_BACKUP_KEY, true),
            DefaultUseRecommendedConfig = LoadBool(DEFAULT_RECOMMENDED_CONFIG_KEY, true),
            GitHubToken = LoadString(GITHUB_TOKEN_KEY)
        };
    }

    /// <summary>
    /// 保存设置
    /// </summary>
    public void SaveSettings(AppSettings settings)
    {
        SaveTheme(settings.Theme);
        SaveBool(REMEMBER_PATH_KEY, settings.RememberLastGamePath);
        SaveString(LAST_GAME_PATH_KEY, settings.LastGamePath);
        SaveBool(SHOW_DETAILED_PROGRESS_KEY, settings.ShowDetailedProgress);
        SaveBool(DEFAULT_BACKUP_KEY, settings.DefaultBackupExisting);
        SaveBool(DEFAULT_RECOMMENDED_CONFIG_KEY, settings.DefaultUseRecommendedConfig);
        SaveString(GITHUB_TOKEN_KEY, settings.GitHubToken);
    }

    /// <summary>
    /// 加载主题设置
    /// </summary>
    private ElementTheme LoadTheme()
    {
        var value = _settingsKey.GetValue(THEME_KEY);
        if (value is int themeInt && Enum.IsDefined(typeof(ElementTheme), themeInt))
        {
            return (ElementTheme)themeInt;
        }
        return ElementTheme.Default;
    }

    /// <summary>
    /// 保存主题设置
    /// </summary>
    private void SaveTheme(ElementTheme theme)
    {
        _settingsKey.SetValue(THEME_KEY, (int)theme, RegistryValueKind.DWord);
    }

    /// <summary>
    /// 加载布尔值设置
    /// </summary>
    private bool LoadBool(string key, bool defaultValue)
    {
        var value = _settingsKey.GetValue(key);
        if (value is int intValue)
        {
            return intValue != 0;
        }
        return defaultValue;
    }

    /// <summary>
    /// 保存布尔值设置
    /// </summary>
    private void SaveBool(string key, bool value)
    {
        _settingsKey.SetValue(key, value ? 1 : 0, RegistryValueKind.DWord);
    }

    /// <summary>
    /// 加载字符串设置
    /// </summary>
    private string? LoadString(string key)
    {
        var value = _settingsKey.GetValue(key);
        return value as string;
    }

    /// <summary>
    /// 保存字符串设置
    /// </summary>
    private void SaveString(string key, string? value)
    {
        if (value != null)
        {
            _settingsKey.SetValue(key, value, RegistryValueKind.String);
        }
        else
        {
            _settingsKey.DeleteValue(key, false);
        }
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
