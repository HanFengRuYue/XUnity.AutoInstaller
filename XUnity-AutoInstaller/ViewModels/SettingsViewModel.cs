using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using XUnity_AutoInstaller.Models;
using XUnity_AutoInstaller.Services;
using XUnity_AutoInstaller.Utils;

namespace XUnity_AutoInstaller.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private AppSettings _currentSettings;
    private bool _isInitializing = true;

    [ObservableProperty]
    private int themeSelectedIndex;

    [ObservableProperty]
    private bool rememberLastGamePath;

    [ObservableProperty]
    private bool showDetailedProgress;

    [ObservableProperty]
    private bool defaultBackupExisting;

    [ObservableProperty]
    private int downloadSourceSelectedIndex;

    [ObservableProperty]
    private string connectionStatus = "未测试";

    [ObservableProperty]
    private string cacheSize = "计算中...";

    [ObservableProperty]
    private string cachePath = "加载中...";

    [ObservableProperty]
    private string settingsPath = "加载中...";

    [ObservableProperty]
    private string appVersion = "加载中...";

    [ObservableProperty]
    private bool isTestingConnection;

    public SettingsViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        _currentSettings = _settingsService.LoadSettings();

        LoadSettingsToUI();
        LoadCacheInfo();
        LoadSettingsInfo();

        AppVersion = $"版本 {SettingsService.GetAppVersion()}";

        _isInitializing = false;
    }

    partial void OnThemeSelectedIndexChanged(int value)
    {
        if (_isInitializing) return;

        var newTheme = value switch
        {
            1 => ElementTheme.Light,
            2 => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        _currentSettings.Theme = newTheme;
        SaveSettings();
        SettingsService.ApplyTheme(newTheme);
    }

    partial void OnRememberLastGamePathChanged(bool value)
    {
        if (_isInitializing) return;
        _currentSettings.RememberLastGamePath = value;
        SaveSettings();
    }

    partial void OnShowDetailedProgressChanged(bool value)
    {
        if (_isInitializing) return;
        _currentSettings.ShowDetailedProgress = value;
        SaveSettings();
    }

    partial void OnDefaultBackupExistingChanged(bool value)
    {
        if (_isInitializing) return;
        _currentSettings.DefaultBackupExisting = value;
        SaveSettings();
    }

    partial void OnDownloadSourceSelectedIndexChanged(int value)
    {
        if (_isInitializing) return;

        _currentSettings.DownloadSource = value == 1 ? DownloadSourceType.Mirror : DownloadSourceType.GitHub;
        SaveSettings();

        var sourceName = value == 1 ? "镜像网站" : "GitHub 官方";
        LogService.Instance.Log($"下载源已切换为: {sourceName}", LogLevel.Info, "[Settings]");
        ConnectionStatus = "未测试（请点击测试连接按钮）";
    }

    private void LoadSettingsToUI()
    {
        ThemeSelectedIndex = _currentSettings.Theme switch
        {
            ElementTheme.Light => 1,
            ElementTheme.Dark => 2,
            _ => 0
        };

        RememberLastGamePath = _currentSettings.RememberLastGamePath;
        ShowDetailedProgress = _currentSettings.ShowDetailedProgress;
        DefaultBackupExisting = _currentSettings.DefaultBackupExisting;
        DownloadSourceSelectedIndex = _currentSettings.DownloadSource == DownloadSourceType.Mirror ? 1 : 0;
    }

    private void LoadCacheInfo()
    {
        try
        {
            var cachePath = PathHelper.GetTempDownloadDirectory();
            CachePath = cachePath;

            if (Directory.Exists(cachePath))
            {
                var cacheFiles = Directory.GetFiles(cachePath, "*.zip", SearchOption.TopDirectoryOnly);
                long totalSize = cacheFiles.Sum(f => new FileInfo(f).Length);
                CacheSize = $"{FormatFileSize(totalSize)} ({cacheFiles.Length} 个文件)";
            }
            else
            {
                CacheSize = "0 B (0 个文件)";
            }
        }
        catch (Exception ex)
        {
            CachePath = "无法访问";
            CacheSize = $"错误: {ex.Message}";
        }
    }

    private void LoadSettingsInfo()
    {
        SettingsPath = SettingsService.GetSettingsPath();
    }

    private void SaveSettings()
    {
        try
        {
            _settingsService.SaveSettings(_currentSettings);
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"自动保存设置失败: {ex.Message}", LogLevel.Error, "[Settings]");
        }
    }

    private string FormatFileSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes} B";
        else if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:F2} KB";
        else if (bytes < 1024 * 1024 * 1024)
            return $"{bytes / (1024.0 * 1024.0):F2} MB";
        else
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
    }

    [RelayCommand]
    private void OpenCacheFolder()
    {
        try
        {
            var cachePath = PathHelper.GetTempDownloadDirectory();
            Process.Start(new ProcessStartInfo
            {
                FileName = cachePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"打开缓存文件夹失败: {ex.Message}", LogLevel.Error, "[Settings]");
            throw;
        }
    }

    [RelayCommand]
    private async Task CleanCacheAsync()
    {
        try
        {
            var cachePath = PathHelper.GetTempDownloadDirectory();
            if (Directory.Exists(cachePath))
            {
                var cacheFiles = Directory.GetFiles(cachePath, "*.zip", SearchOption.TopDirectoryOnly);

                int deletedCount = 0;
                foreach (var file in cacheFiles)
                {
                    try
                    {
                        File.Delete(file);
                        deletedCount++;
                    }
                    catch { }
                }

                LogService.Instance.Log($"缓存清理完成，删除了 {deletedCount} 个文件", LogLevel.Info, "[Settings]");

                LoadCacheInfo();

                // Throw custom exception with deleted count for UI to handle
                throw new InvalidOperationException($"已清理 {deletedCount} 个缓存文件");
            }
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"清理缓存失败: {ex.Message}", LogLevel.Error, "[Settings]");
            throw;
        }
    }

    [RelayCommand]
    private void OpenSettingsFolder()
    {
        try
        {
            var settingsPath = SettingsService.GetAppDataPath();
            if (!Directory.Exists(settingsPath))
            {
                Directory.CreateDirectory(settingsPath);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = settingsPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"打开配置文件夹失败: {ex.Message}", LogLevel.Error, "[Settings]");
            throw;
        }
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        try
        {
            IsTestingConnection = true;
            ConnectionStatus = "正在测试连接...";

            IVersionFetcher client = DownloadSourceSelectedIndex == 1
                ? new WebDAVMirrorClient()
                : new GitHubAtomFeedClient();

            bool isConnected = await client.ValidateConnectionAsync();

            if (isConnected)
            {
                ConnectionStatus = $"✓ 连接成功 ({client.SourceName})";
                LogService.Instance.Log($"连接测试成功: {client.SourceName}", LogLevel.Info, "[Settings]");
            }
            else
            {
                ConnectionStatus = $"✗ 连接失败 ({client.SourceName})";
                LogService.Instance.Log($"连接测试失败: {client.SourceName}", LogLevel.Warning, "[Settings]");
                throw new InvalidOperationException($"无法连接到 {client.SourceName}\n\n请检查网络连接或尝试其他下载源。");
            }

            if (client is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        catch (Exception ex)
        {
            ConnectionStatus = "✗ 测试出错";
            LogService.Instance.Log($"连接测试出错: {ex.Message}", LogLevel.Error, "[Settings]");
            throw;
        }
        finally
        {
            IsTestingConnection = false;
        }
    }

    [RelayCommand]
    private async Task ResetAllDataAsync()
    {
        try
        {
            var settingsPath = SettingsService.GetAppDataPath();

            if (Directory.Exists(settingsPath))
            {
                Directory.Delete(settingsPath, recursive: true);
            }

            LogService.Instance.Log("所有应用数据已重置", LogLevel.Info, "[Settings]");

            _isInitializing = true;
            _currentSettings = new AppSettings();
            LoadSettingsToUI();
            LoadCacheInfo();
            LoadSettingsInfo();
            _isInitializing = false;

            SettingsService.ApplyTheme(_currentSettings.Theme);

            // Throw to signal code-behind to handle restart dialog
            throw new InvalidOperationException("RESET_COMPLETE");
        }
        catch (Exception ex)
        {
            if (ex.Message != "RESET_COMPLETE")
            {
                LogService.Instance.Log($"重置数据失败: {ex.Message}", LogLevel.Error, "[Settings]");
            }
            throw;
        }
    }
}
