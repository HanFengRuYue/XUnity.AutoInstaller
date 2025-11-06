using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using XUnity_AutoInstaller.Models;
using XUnity_AutoInstaller.Services;
using XUnity_AutoInstaller.Utils;

namespace XUnity_AutoInstaller.ViewModels;

public partial class ConfigViewModel : ObservableObject
{
    private readonly GameStateService _gameStateService;

    [ObservableProperty]
    private BepInExConfig? bepInExConfig;

    [ObservableProperty]
    private XUnityConfig? xUnityConfig;

    [ObservableProperty]
    private bool isLocked = true;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string gamePath = string.Empty;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private bool hasError;

    public Visibility LockedPanelVisibility => IsLocked ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ConfigContentVisibility => !IsLocked && !IsLoading ? Visibility.Visible : Visibility.Collapsed;
    public Visibility LoadingVisibility => IsLoading ? Visibility.Visible : Visibility.Collapsed;

    public ConfigViewModel(GameStateService gameStateService)
    {
        _gameStateService = gameStateService;
        _gameStateService.GamePathChanged += OnGamePathChanged;
    }

    partial void OnIsLockedChanged(bool value)
    {
        OnPropertyChanged(nameof(LockedPanelVisibility));
        OnPropertyChanged(nameof(ConfigContentVisibility));
        SaveCommand.NotifyCanExecuteChanged();
        ResetCommand.NotifyCanExecuteChanged();
        OpenConfigFolderCommand.NotifyCanExecuteChanged();
    }

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(LoadingVisibility));
        OnPropertyChanged(nameof(ConfigContentVisibility));
    }

    public async Task InitializeAsync()
    {
        var path = _gameStateService.CurrentGamePath;
        if (!string.IsNullOrEmpty(path))
        {
            GamePath = path;
            await LoadConfigurationAsync();
        }
        else
        {
            IsLocked = true;
        }
    }

    [RelayCommand]
    private async Task BrowseGamePathAsync()
    {
        var path = await DialogHelper.PickFolderAsync();
        if (path != null)
        {
            if (PathHelper.IsValidGameDirectory(path))
            {
                _gameStateService.SetGamePath(path, saveToSettings: true);
                GamePath = path;
                await LoadConfigurationAsync();
            }
            else
            {
                IsLocked = true;
                throw new InvalidOperationException("所选目录不是有效的游戏目录");
            }
        }
    }

    [RelayCommand]
    private async Task LoadConfigurationAsync()
    {
        var gamePath = _gameStateService.CurrentGamePath;
        if (string.IsNullOrEmpty(gamePath) || !ConfigurationService.ValidateGamePath(gamePath))
        {
            IsLocked = true;
            HasError = true;
            ErrorMessage = "无效的游戏路径或 BepInEx 未安装。请先在安装页面安装 BepInEx。";
            LogService.Instance.Log(ErrorMessage, LogLevel.Warning, "[Config]");
            return;
        }

        IsLocked = false;

        try
        {
            IsLoading = true;
            HasError = false;

            await Task.Run(() =>
            {
                var bepinex = ConfigurationService.LoadBepInExConfig(gamePath);
                var xunity = ConfigurationService.LoadXUnityConfig(gamePath);

                BepInExConfig = bepinex;
                XUnityConfig = xunity;
            });
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"加载配置失败: {ex.Message}";
            LogService.Instance.Log($"加载配置失败: {ex.Message}", LogLevel.Error, "[Config]");
            throw;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        var gamePath = _gameStateService.CurrentGamePath;
        if (string.IsNullOrEmpty(gamePath))
        {
            throw new InvalidOperationException("游戏路径未设置");
        }

        if (BepInExConfig == null || XUnityConfig == null)
        {
            throw new InvalidOperationException("配置未加载");
        }

        try
        {
            ConfigurationService.SaveBepInExConfig(gamePath, BepInExConfig);
            ConfigurationService.SaveXUnityConfig(gamePath, XUnityConfig);
            LogService.Instance.Log("配置保存成功", LogLevel.Info, "[Config]");
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"保存配置失败: {ex.Message}", LogLevel.Error, "[Config]");
            throw;
        }
    }

    private bool CanSave() => !IsLocked && BepInExConfig != null && XUnityConfig != null;

    [RelayCommand]
    private async Task CancelAsync()
    {
        await LoadConfigurationAsync();
    }

    [RelayCommand(CanExecute = nameof(CanReset))]
    private void Reset()
    {
        BepInExConfig = BepInExConfig.CreateDefault();
        XUnityConfig = XUnityConfig.CreateRecommended();
        LogService.Instance.Log("配置已重置为默认值", LogLevel.Info, "[Config]");
    }

    private bool CanReset() => !IsLocked;

    [RelayCommand(CanExecute = nameof(CanOpenConfigFolder))]
    private void OpenConfigFolder()
    {
        var gamePath = _gameStateService.CurrentGamePath;
        if (string.IsNullOrEmpty(gamePath))
        {
            throw new InvalidOperationException("游戏路径未设置");
        }

        try
        {
            var configPath = PathHelper.GetBepInExConfigPath(gamePath);

            if (!System.IO.Directory.Exists(configPath))
            {
                throw new InvalidOperationException("配置文件夹不存在");
            }

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = configPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"打开文件夹失败: {ex.Message}", LogLevel.Error, "[Config]");
            throw;
        }
    }

    private bool CanOpenConfigFolder() => !IsLocked;

    private void OnGamePathChanged(object? sender, string? newPath)
    {
        if (!string.IsNullOrEmpty(newPath))
        {
            GamePath = newPath;
            _ = LoadConfigurationAsync();
        }
    }

    public void Cleanup()
    {
        _gameStateService.GamePathChanged -= OnGamePathChanged;
    }
}
