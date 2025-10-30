using Microsoft.UI.Xaml;
using System;
using System.Text.Json;
using Windows.Storage;
using XUnity.AutoInstaller.Models;

namespace XUnity.AutoInstaller.Services;

/// <summary>
/// 设置服务
/// 负责加载和保存应用程序设置
/// </summary>
public class SettingsService
{
    private static readonly string THEME_KEY = "AppTheme";
    private static readonly string REMEMBER_PATH_KEY = "RememberLastGamePath";
    private static readonly string LAST_GAME_PATH_KEY = "LastGamePath";
    private static readonly string SHOW_DETAILED_PROGRESS_KEY = "ShowDetailedProgress";
    private static readonly string DEFAULT_BACKUP_KEY = "DefaultBackupExisting";
    private static readonly string DEFAULT_RECOMMENDED_CONFIG_KEY = "DefaultUseRecommendedConfig";

    private readonly ApplicationDataContainer _localSettings;

    public SettingsService()
    {
        _localSettings = ApplicationData.Current.LocalSettings;
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
            DefaultUseRecommendedConfig = LoadBool(DEFAULT_RECOMMENDED_CONFIG_KEY, true)
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
    }

    /// <summary>
    /// 加载主题设置
    /// </summary>
    private ElementTheme LoadTheme()
    {
        if (_localSettings.Values.TryGetValue(THEME_KEY, out var value))
        {
            if (value is int themeInt && Enum.IsDefined(typeof(ElementTheme), themeInt))
            {
                return (ElementTheme)themeInt;
            }
        }
        return ElementTheme.Default;
    }

    /// <summary>
    /// 保存主题设置
    /// </summary>
    private void SaveTheme(ElementTheme theme)
    {
        _localSettings.Values[THEME_KEY] = (int)theme;
    }

    /// <summary>
    /// 加载布尔值设置
    /// </summary>
    private bool LoadBool(string key, bool defaultValue)
    {
        if (_localSettings.Values.TryGetValue(key, out var value))
        {
            if (value is bool boolValue)
            {
                return boolValue;
            }
        }
        return defaultValue;
    }

    /// <summary>
    /// 保存布尔值设置
    /// </summary>
    private void SaveBool(string key, bool value)
    {
        _localSettings.Values[key] = value;
    }

    /// <summary>
    /// 加载字符串设置
    /// </summary>
    private string? LoadString(string key)
    {
        if (_localSettings.Values.TryGetValue(key, out var value))
        {
            return value as string;
        }
        return null;
    }

    /// <summary>
    /// 保存字符串设置
    /// </summary>
    private void SaveString(string key, string? value)
    {
        if (value != null)
        {
            _localSettings.Values[key] = value;
        }
        else
        {
            _localSettings.Values.Remove(key);
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
        var version = Windows.ApplicationModel.Package.Current.Id.Version;
        return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }
}
