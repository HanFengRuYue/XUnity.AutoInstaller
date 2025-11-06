using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using XUnity_AutoInstaller.Models;
using XUnity_AutoInstaller.Services;

namespace XUnity_AutoInstaller.ViewModels;

public partial class VersionManagementViewModel : ObservableObject
{
    private readonly VersionCacheService _versionCacheService;
    private readonly GameStateService _gameStateService;
    private readonly VersionService _versionService;
    private readonly InstallationStateService _installationStateService;

    private List<VersionInfo> _allBepInExVersions = new();
    private List<VersionInfo> _allXUnityVersions = new();

    [ObservableProperty]
    private bool isLoadingInstalled;

    [ObservableProperty]
    private bool isLoadingAvailable;

    [ObservableProperty]
    private bool isInstallationInProgress;

    [ObservableProperty]
    private int selectedPlatformFilterIndex; // 0=全部, 1=Mono, 2=IL2CPP

    [ObservableProperty]
    private int selectedArchitectureFilterIndex; // 0=全部, 1=x64, 2=x86

    [ObservableProperty]
    private int selectedVersionTypeIndex; // 0=全部, 1=正式版, 2=预发布版

    [ObservableProperty]
    private ObservableCollection<InstalledVersionInfo> installedVersions = new();

    [ObservableProperty]
    private ObservableCollection<SnapshotInfo> snapshots = new();

    [ObservableProperty]
    private ObservableCollection<AvailableVersionItem> bepInExVersions = new();

    [ObservableProperty]
    private ObservableCollection<AvailableVersionItem> xUnityVersions = new();

    [ObservableProperty]
    private InstalledVersionInfo? selectedInstalledVersion;

    [ObservableProperty]
    private SnapshotInfo? selectedSnapshot;

    // Visibility properties
    public Visibility BepInExLoadingVisibility => IsLoadingAvailable ? Visibility.Visible : Visibility.Collapsed;
    public Visibility BepInExContentVisibility => !IsLoadingAvailable ? Visibility.Visible : Visibility.Collapsed;
    public Visibility XUnityLoadingVisibility => IsLoadingAvailable ? Visibility.Visible : Visibility.Collapsed;
    public Visibility XUnityContentVisibility => !IsLoadingAvailable ? Visibility.Visible : Visibility.Collapsed;
    public Visibility InstalledLoadingVisibility => IsLoadingInstalled ? Visibility.Visible : Visibility.Collapsed;
    public Visibility InstalledContentVisibility => !IsLoadingInstalled ? Visibility.Visible : Visibility.Collapsed;
    public Visibility EmptyInstalledVisibility => InstalledVersions.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility InstalledListVisibility => InstalledVersions.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

    public VersionManagementViewModel(
        VersionCacheService versionCacheService,
        GameStateService gameStateService,
        VersionService versionService,
        InstallationStateService installationStateService)
    {
        _versionCacheService = versionCacheService;
        _gameStateService = gameStateService;
        _versionService = versionService;
        _installationStateService = installationStateService;

        // 订阅事件
        _gameStateService.GamePathChanged += OnGamePathChanged;
        _versionCacheService.VersionsUpdated += OnVersionsUpdated;
        _installationStateService.InstallationStarted += OnInstallationStarted;
        _installationStateService.InstallationCompleted += OnInstallationCompleted;
        InstalledVersions.CollectionChanged += OnInstalledVersionsCollectionChanged;
    }

    partial void OnIsLoadingAvailableChanged(bool value)
    {
        OnPropertyChanged(nameof(BepInExLoadingVisibility));
        OnPropertyChanged(nameof(BepInExContentVisibility));
        OnPropertyChanged(nameof(XUnityLoadingVisibility));
        OnPropertyChanged(nameof(XUnityContentVisibility));
    }

    partial void OnIsLoadingInstalledChanged(bool value)
    {
        OnPropertyChanged(nameof(InstalledLoadingVisibility));
        OnPropertyChanged(nameof(InstalledContentVisibility));
    }

    partial void OnSelectedPlatformFilterIndexChanged(int value)
    {
        ApplyFilters();
    }

    partial void OnSelectedArchitectureFilterIndexChanged(int value)
    {
        ApplyFilters();
    }

    partial void OnSelectedVersionTypeIndexChanged(int value)
    {
        ApplyFilters();
    }

    private void OnInstalledVersionsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(EmptyInstalledVisibility));
        OnPropertyChanged(nameof(InstalledListVisibility));
    }

    public async Task InitializeAsync()
    {
        LoadVersionsFromCache();

        if (_gameStateService.HasValidGamePath())
        {
            await RefreshInstalledVersionsAsync();
        }
    }

    private void LoadVersionsFromCache()
    {
        _allBepInExVersions = _versionCacheService.GetBepInExVersions();
        _allXUnityVersions = _versionCacheService.GetXUnityVersions();

        if (_allBepInExVersions.Count > 0 || _allXUnityVersions.Count > 0)
        {
            ApplyFilters();
            LogService.Instance.Log($"从缓存加载版本列表: BepInEx {_allBepInExVersions.Count} 个, XUnity {_allXUnityVersions.Count} 个", LogLevel.Info, "[版本管理]");
        }
        else
        {
            LogService.Instance.Log("版本缓存为空，请点击【刷新可用版本】按钮", LogLevel.Info, "[版本管理]");
        }
    }

    [RelayCommand]
    private async Task RefreshInstalledVersionsAsync()
    {
        if (!_gameStateService.HasValidGamePath())
        {
            LogService.Instance.Log("游戏路径未设置", LogLevel.Warning, "[版本管理]");
            return;
        }

        try
        {
            IsLoadingInstalled = true;

            var gamePath = _gameStateService.CurrentGamePath;
            LogService.Instance.Log($"检查游戏路径: {gamePath}", LogLevel.Info, "[版本管理]");

            InstalledVersions.Clear();
            Snapshots.Clear();

            // 获取当前安装的版本
            var installedList = _versionService.GetInstalledVersions(gamePath);
            foreach (var version in installedList)
            {
                InstalledVersions.Add(version);
            }

            // TODO: 加载快照功能待实现（BackupService 不存在）
            // var snapshotList = await backupService.ListBackupsAsync(gamePath);
            // foreach (var snapshot in snapshotList.OrderByDescending(s => s.CreatedAt))
            // {
            //     Snapshots.Add(snapshot);
            // }

            LogService.Instance.Log($"已加载 {InstalledVersions.Count} 个已安装版本和 {Snapshots.Count} 个快照", LogLevel.Info, "[版本管理]");
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"加载已安装版本失败: {ex.Message}", LogLevel.Error, "[版本管理]");
        }
        finally
        {
            IsLoadingInstalled = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAvailableVersionsAsync()
    {
        try
        {
            IsLoadingAvailable = true;
            LogService.Instance.Log("开始刷新可用版本列表...", LogLevel.Info, "[版本管理]");

            // 触发缓存刷新（会从 GitHub/Mirror 获取最新数据）
            await _versionCacheService.RefreshAsync();

            // 重新加载缓存数据
            _allBepInExVersions = _versionCacheService.GetBepInExVersions();
            _allXUnityVersions = _versionCacheService.GetXUnityVersions();

            ApplyFilters();

            LogService.Instance.Log($"刷新成功: BepInEx {_allBepInExVersions.Count} 个, XUnity {_allXUnityVersions.Count} 个", LogLevel.Info, "[版本管理]");
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"刷新版本列表失败: {ex.Message}", LogLevel.Error, "[版本管理]");
        }
        finally
        {
            IsLoadingAvailable = false;
        }
    }

    [RelayCommand]
    private async Task DownloadVersionAsync(AvailableVersionItem item)
    {
        if (item == null || IsInstallationInProgress) return;

        try
        {
            LogService.Instance.Log($"开始下载: {item.Version.Name} {item.Version.Version}", LogLevel.Info, "[版本管理]");

            var progress = new Progress<int>(percentage =>
            {
                LogService.Instance.Log($"下载进度: {percentage}%", LogLevel.Info, "[版本管理]");
            });

            await _versionService.DownloadVersionAsync(item.Version, progress);

            LogService.Instance.Log("下载成功！", LogLevel.Info, "[版本管理]");

            // 刷新UI（更新缓存状态）
            ApplyFilters();
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"下载失败: {ex.Message}", LogLevel.Error, "[版本管理]");
        }
    }

    [RelayCommand(CanExecute = nameof(CanUninstall))]
    private async Task UninstallAsync()
    {
        if (SelectedInstalledVersion == null) return;

        var gamePath = _gameStateService.CurrentGamePath;
        if (string.IsNullOrEmpty(gamePath)) return;

        try
        {
            LogService.Instance.Log("开始卸载...", LogLevel.Info, "[版本管理]");

            var logger = new Utils.LogWriter(null, null!);
            var uninstallService = new UninstallationService(logger);

            var success = await uninstallService.UninstallAsync(gamePath);

            if (success)
            {
                LogService.Instance.Log("卸载成功！", LogLevel.Info, "[版本管理]");
                await RefreshInstalledVersionsAsync();
            }
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"卸载失败: {ex.Message}", LogLevel.Error, "[版本管理]");
        }
    }

    private bool CanUninstall()
    {
        return SelectedInstalledVersion != null && !IsInstallationInProgress;
    }

    [RelayCommand(CanExecute = nameof(CanRestoreSnapshot))]
    private async Task RestoreSnapshotAsync()
    {
        if (SelectedSnapshot == null) return;

        // TODO: BackupService 不存在，快照还原功能待实现
        LogService.Instance.Log("快照还原功能待实现", LogLevel.Warning, "[版本管理]");
        await Task.CompletedTask;
    }

    private bool CanRestoreSnapshot()
    {
        return SelectedSnapshot != null && !IsInstallationInProgress;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteSnapshot))]
    private async Task DeleteSnapshotAsync()
    {
        if (SelectedSnapshot == null) return;

        // TODO: BackupService 不存在，快照删除功能待实现
        LogService.Instance.Log("快照删除功能待实现", LogLevel.Warning, "[版本管理]");
        await Task.CompletedTask;
    }

    private bool CanDeleteSnapshot()
    {
        return SelectedSnapshot != null;
    }

    private void ApplyFilters()
    {
        var selectedPlatform = SelectedPlatformFilterIndex switch
        {
            1 => "Mono",
            2 => "IL2CPP",
            _ => "全部"
        };

        var selectedArch = SelectedArchitectureFilterIndex switch
        {
            1 => "x64",
            2 => "x86",
            _ => "全部"
        };

        var showPrerelease = SelectedVersionTypeIndex != 1; // 0=全部, 1=正式版, 2=预发布版
        var onlyPrerelease = SelectedVersionTypeIndex == 2;

        // 筛选 BepInEx
        var filteredBepInEx = _allBepInExVersions.Where(v =>
        {
            // 平台筛选
            if (selectedPlatform == "Mono" && (v.TargetPlatform == Platform.IL2CPP_x64 || v.TargetPlatform == Platform.IL2CPP_x86))
                return false;
            if (selectedPlatform == "IL2CPP" && v.TargetPlatform != Platform.IL2CPP_x64 && v.TargetPlatform != Platform.IL2CPP_x86)
                return false;

            // 架构筛选
            if (selectedArch == "x64" && v.TargetPlatform != Platform.x64 && v.TargetPlatform != Platform.IL2CPP_x64)
                return false;
            if (selectedArch == "x86" && v.TargetPlatform != Platform.x86 && v.TargetPlatform != Platform.IL2CPP_x86)
                return false;

            // 版本类型筛选
            if (onlyPrerelease && !v.IsPrerelease)
                return false;
            if (!showPrerelease && v.IsPrerelease)
                return false;

            return true;
        }).OrderByDescending(v => v.ReleaseDate).ToList();

        // 筛选 XUnity
        var filteredXUnity = _allXUnityVersions.Where(v =>
        {
            // 平台筛选
            if (selectedPlatform == "Mono" && (v.TargetPlatform == Platform.IL2CPP_x64 || v.TargetPlatform == Platform.IL2CPP_x86))
                return false;
            if (selectedPlatform == "IL2CPP" && v.TargetPlatform != Platform.IL2CPP_x64 && v.TargetPlatform != Platform.IL2CPP_x86)
                return false;

            // 版本类型筛选
            if (onlyPrerelease && !v.IsPrerelease)
                return false;
            if (!showPrerelease && v.IsPrerelease)
                return false;

            return true;
        }).OrderByDescending(v => v.ReleaseDate).ToList();

        UpdateVersionsUI(filteredBepInEx, filteredXUnity);
    }

    private void UpdateVersionsUI(List<VersionInfo> bepinexList, List<VersionInfo> xunityList)
    {
        BepInExVersions.Clear();
        foreach (var version in bepinexList)
        {
            var platformStr = version.TargetPlatform?.ToString() ?? "未知";
            var dateStr = version.ReleaseDate.ToString("yyyy-MM-dd");
            var displayText = $"{version.Name} {version.Version} ({platformStr}) - {dateStr}";

            var item = new AvailableVersionItem
            {
                Version = version,
                DisplayText = displayText,
                IsCached = Utils.PathHelper.IsCachedVersion(version)
            };
            BepInExVersions.Add(item);
        }

        XUnityVersions.Clear();
        foreach (var version in xunityList)
        {
            var variantText = version.TargetPlatform == Platform.IL2CPP_x64 || version.TargetPlatform == Platform.IL2CPP_x86
                ? " (IL2CPP)"
                : " (Mono)";
            var dateStr = version.ReleaseDate.ToString("yyyy-MM-dd");
            var displayText = $"{version.Name} {version.Version}{variantText} - {dateStr}";

            var item = new AvailableVersionItem
            {
                Version = version,
                DisplayText = displayText,
                IsCached = Utils.PathHelper.IsCachedVersion(version)
            };
            XUnityVersions.Add(item);
        }

        LogService.Instance.Log($"显示 {BepInExVersions.Count} 个 BepInEx 版本，{XUnityVersions.Count} 个 XUnity 版本", LogLevel.Debug, "[版本管理]");
    }

    private void OnGamePathChanged(object? sender, string? gamePath)
    {
        UninstallCommand.NotifyCanExecuteChanged();
        RestoreSnapshotCommand.NotifyCanExecuteChanged();

        if (_gameStateService.HasValidGamePath())
        {
            _ = RefreshInstalledVersionsAsync();
        }
    }

    private void OnVersionsUpdated(object? sender, VersionsUpdatedEventArgs e)
    {
        _allBepInExVersions = e.BepInExVersions;
        _allXUnityVersions = e.XUnityVersions;
        ApplyFilters();
    }

    private void OnInstallationStarted(object? sender, EventArgs e)
    {
        IsInstallationInProgress = true;
        UninstallCommand.NotifyCanExecuteChanged();
        RestoreSnapshotCommand.NotifyCanExecuteChanged();
    }

    private void OnInstallationCompleted(object? sender, bool success)
    {
        IsInstallationInProgress = false;
        UninstallCommand.NotifyCanExecuteChanged();
        RestoreSnapshotCommand.NotifyCanExecuteChanged();

        if (success && _gameStateService.HasValidGamePath())
        {
            _ = RefreshInstalledVersionsAsync();
        }
    }

    public void Cleanup()
    {
        _gameStateService.GamePathChanged -= OnGamePathChanged;
        _versionCacheService.VersionsUpdated -= OnVersionsUpdated;
        _installationStateService.InstallationStarted -= OnInstallationStarted;
        _installationStateService.InstallationCompleted -= OnInstallationCompleted;
        InstalledVersions.CollectionChanged -= OnInstalledVersionsCollectionChanged;
    }
}
