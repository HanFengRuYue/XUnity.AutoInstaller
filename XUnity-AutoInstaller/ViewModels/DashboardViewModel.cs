using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using XUnity_AutoInstaller.Models;
using XUnity_AutoInstaller.Services;
using XUnity_AutoInstaller.Utils;

namespace XUnity_AutoInstaller.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly GameStateService _gameStateService;
    private readonly InstallationStateService _installationStateService;

    [ObservableProperty]
    private string gamePath = string.Empty;

    [ObservableProperty]
    private string bepInExStatus = "未安装";

    [ObservableProperty]
    private string bepInExVersion = "--";

    [ObservableProperty]
    private string bepInExPlatform = "--";

    [ObservableProperty]
    private string xUnityStatus = "未安装";

    [ObservableProperty]
    private string xUnityVersion = "--";

    [ObservableProperty]
    private string xUnityEngine = "--";

    [ObservableProperty]
    private bool isQuickInstallButtonEnabled;

    [ObservableProperty]
    private bool isUninstallButtonEnabled;

    [ObservableProperty]
    private bool isInstalling;

    [ObservableProperty]
    private int installProgress;

    [ObservableProperty]
    private string installStatusMessage = string.Empty;

    [ObservableProperty]
    private string progressPercentText = "0%";

    // Visibility properties
    public Visibility ProgressPanelVisibility => IsInstalling ? Visibility.Visible : Visibility.Collapsed;

    // Status colors (to be bound from code-behind or use converters)
    public Brush BepInExStatusColor { get; private set; } = null!;
    public Brush XUnityStatusColor { get; private set; } = null!;

    public DashboardViewModel(
        GameStateService gameStateService,
        InstallationStateService installationStateService)
    {
        _gameStateService = gameStateService;
        _installationStateService = installationStateService;

        _gameStateService.GamePathChanged += OnGamePathChanged;
        _installationStateService.InstallationStarted += OnInstallationStarted;
        _installationStateService.InstallationCompleted += OnInstallationCompleted;
        _installationStateService.ProgressChanged += OnProgressChanged;

        LoadCurrentGamePath();
    }

    partial void OnIsInstallingChanged(bool value)
    {
        OnPropertyChanged(nameof(ProgressPanelVisibility));
        QuickInstallCommand.NotifyCanExecuteChanged();
        UninstallCommand.NotifyCanExecuteChanged();
    }

    partial void OnInstallProgressChanged(int value)
    {
        ProgressPercentText = $"{value}%";
    }

    public void Initialize()
    {
        LoadCurrentGamePath();
    }

    private void LoadCurrentGamePath()
    {
        var path = _gameStateService.CurrentGamePath;
        if (!string.IsNullOrEmpty(path))
        {
            GamePath = path;
            RefreshStatus();
            IsQuickInstallButtonEnabled = true;
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
                RefreshStatus();
            }
            else
            {
                throw new InvalidOperationException("所选目录不是有效的游戏目录（未找到可执行文件）");
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanQuickInstall))]
    private async Task QuickInstallAsync()
    {
        var gamePath = _gameStateService.CurrentGamePath;
        if (string.IsNullOrEmpty(gamePath))
        {
            throw new InvalidOperationException("请先选择游戏路径");
        }

        try
        {
            IsInstalling = true;
            LogService.Instance.Log($"开始一键安装到: {gamePath}", LogLevel.Info, "[Dashboard]");

            // 检测游戏引擎
            var gameInfo = GameDetectionService.GetGameInfo(gamePath);
            var targetPlatform = gameInfo.Engine == GameEngine.UnityIL2CPP ? Platform.IL2CPP_x64 : Platform.x64;

            LogService.Instance.Log($"检测到游戏引擎: {gameInfo.Engine}，目标平台: {targetPlatform}", LogLevel.Info, "[Dashboard]");

            // 创建默认安装选项
            var options = new InstallOptions
            {
                TargetPlatform = targetPlatform,
                BackupExisting = true,
                CleanOldVersion = false,
                LaunchGameToGenerateConfig = true,
                ConfigGenerationTimeout = 60,
                BepInExVersion = null,
                XUnityVersion = null
            };

            // 创建安装服务
            var logger = new LogWriter(null, null!);
            var installService = new InstallationService(logger);

            // 创建进度报告
            var progress = _installationStateService.CreateProgressReporter();

            // 执行安装
            var success = await installService.InstallAsync(gamePath, options, progress);

            if (success)
            {
                LogService.Instance.Log("一键安装成功完成！", LogLevel.Info, "[Dashboard]");
                RefreshStatus();
            }
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"一键安装失败: {ex.Message}", LogLevel.Error, "[Dashboard]");
            throw;
        }
        finally
        {
            IsInstalling = false;
        }
    }

    private bool CanQuickInstall()
    {
        return !IsInstalling && _gameStateService.HasValidGamePath();
    }

    [RelayCommand(CanExecute = nameof(CanUninstall))]
    private async Task UninstallAsync()
    {
        var gamePath = _gameStateService.CurrentGamePath;
        if (string.IsNullOrEmpty(gamePath))
        {
            throw new InvalidOperationException("请先选择游戏路径");
        }

        // 检测安装状态
        var status = GameDetectionService.DetectInstallationStatus(gamePath);
        if (!status.IsBepInExInstalled && !status.IsXUnityInstalled)
        {
            throw new InvalidOperationException("未检测到 BepInEx 或 XUnity 安装");
        }

        try
        {
            IsInstalling = true;
            LogService.Instance.Log($"开始卸载: {gamePath}", LogLevel.Info, "[Dashboard]");

            var logger = new LogWriter(null, null!);
            var uninstallService = new UninstallationService(logger);

            var progress = _installationStateService.CreateProgressReporter();

            var success = await uninstallService.UninstallAsync(gamePath, progress);

            if (success)
            {
                LogService.Instance.Log("卸载成功完成！", LogLevel.Info, "[Dashboard]");
                RefreshStatus();
            }
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"卸载失败: {ex.Message}", LogLevel.Error, "[Dashboard]");
            throw;
        }
        finally
        {
            IsInstalling = false;
        }
    }

    private bool CanUninstall()
    {
        return !IsInstalling && IsUninstallButtonEnabled;
    }

    [RelayCommand]
    private void OpenGameFolder()
    {
        var gamePath = _gameStateService.CurrentGamePath;
        if (string.IsNullOrEmpty(gamePath))
        {
            throw new InvalidOperationException("请先选择游戏路径");
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = gamePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"打开文件夹失败: {ex.Message}", LogLevel.Error, "[Dashboard]");
            throw;
        }
    }

    [RelayCommand]
    private void RefreshStatus()
    {
        var gamePath = _gameStateService.CurrentGamePath;
        if (string.IsNullOrEmpty(gamePath))
        {
            return;
        }

        var status = GameDetectionService.DetectInstallationStatus(gamePath);

        // Update BepInEx status
        if (status.IsBepInExInstalled)
        {
            BepInExStatus = "已安装";
            BepInExVersion = status.BepInExVersion ?? "Unknown";
            BepInExPlatform = status.BepInExPlatform ?? "Unknown";
            BepInExStatusColor = (Brush)Application.Current.Resources["SystemFillColorSuccessBrush"];
        }
        else
        {
            BepInExStatus = "未安装";
            BepInExVersion = "--";
            BepInExPlatform = "--";
            BepInExStatusColor = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
        }

        // Update XUnity status
        if (status.IsXUnityInstalled)
        {
            XUnityStatus = "已安装";
            XUnityVersion = status.XUnityVersion ?? "Unknown";
            XUnityEngine = status.TranslationEngine ?? "Unknown";
            XUnityStatusColor = (Brush)Application.Current.Resources["SystemFillColorSuccessBrush"];
        }
        else
        {
            XUnityStatus = "未安装";
            XUnityVersion = "--";
            XUnityEngine = "--";
            XUnityStatusColor = (Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
        }

        // Update button states
        IsQuickInstallButtonEnabled = true;
        IsUninstallButtonEnabled = status.IsBepInExInstalled || status.IsXUnityInstalled;

        // Notify property changes for colors
        OnPropertyChanged(nameof(BepInExStatusColor));
        OnPropertyChanged(nameof(XUnityStatusColor));

        // Notify command states
        QuickInstallCommand.NotifyCanExecuteChanged();
        UninstallCommand.NotifyCanExecuteChanged();
    }

    private void OnGamePathChanged(object? sender, string? gamePath)
    {
        if (!string.IsNullOrEmpty(gamePath))
        {
            GamePath = gamePath;
            RefreshStatus();
            IsQuickInstallButtonEnabled = true;
        }
    }

    private void OnInstallationStarted(object? sender, EventArgs e)
    {
        IsInstalling = true;
    }

    private void OnInstallationCompleted(object? sender, bool success)
    {
        IsInstalling = false;
        RefreshStatus();
    }

    private void OnProgressChanged(object? sender, (int progress, string message) e)
    {
        InstallProgress = e.progress;
        InstallStatusMessage = e.message;
    }

    public void Cleanup()
    {
        _gameStateService.GamePathChanged -= OnGamePathChanged;
        _installationStateService.InstallationStarted -= OnInstallationStarted;
        _installationStateService.InstallationCompleted -= OnInstallationCompleted;
        _installationStateService.ProgressChanged -= OnProgressChanged;
    }
}
