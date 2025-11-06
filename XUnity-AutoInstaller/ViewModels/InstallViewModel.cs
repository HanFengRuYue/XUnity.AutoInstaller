using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using XUnity_AutoInstaller.Models;
using XUnity_AutoInstaller.Services;
using XUnity_AutoInstaller.Utils;

namespace XUnity_AutoInstaller.ViewModels
{
    public partial class InstallViewModel : ObservableObject
    {
        private readonly GameStateService _gameStateService;
        private readonly VersionCacheService _versionCacheService;
        private readonly VersionService _versionService;
        private readonly InstallationStateService _installationStateService;

        private List<VersionInfo> _allBepInExVersions = new();
        private List<VersionInfo> _allXUnityVersions = new();

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private bool isInstalling;

        [ObservableProperty]
        private int versionModeIndex; // 0=自动, 1=手动

        [ObservableProperty]
        private int selectedPlatformIndex; // 0=x64, 1=x86, 2=IL2CPP x64, 3=IL2CPP x86

        [ObservableProperty]
        private ObservableCollection<VersionDisplayItem> bepInExItems = new();

        [ObservableProperty]
        private ObservableCollection<VersionDisplayItem> xUnityItems = new();

        [ObservableProperty]
        private VersionDisplayItem? selectedBepInExItem;

        [ObservableProperty]
        private VersionDisplayItem? selectedXUnityItem;

        [ObservableProperty]
        private bool backupExisting = true;

        [ObservableProperty]
        private bool cleanOldVersion;

        [ObservableProperty]
        private bool launchGameToGenerateConfig = true;

        [ObservableProperty]
        private int configGenerationTimeout = 60;

        [ObservableProperty]
        private int installProgress;

        [ObservableProperty]
        private string installStatusMessage = string.Empty;

        [ObservableProperty]
        private bool isBepInExLoading;

        [ObservableProperty]
        private bool isXUnityLoading;

        [ObservableProperty]
        private string bepInExPlaceholder = string.Empty;

        [ObservableProperty]
        private string xUnityPlaceholder = string.Empty;

        // 计算属性
        public bool IsAutoMode => VersionModeIndex == 0;
        public bool IsManualMode => VersionModeIndex == 1;
        public bool AreVersionComboBoxesEnabled => IsManualMode;
        public bool IsPlatformComboBoxEnabled => IsManualMode;

        // Visibility 属性（x:Bind 需要 Visibility 类型，不能直接绑定 bool）
        public Visibility BepInExLoadingVisibility => IsBepInExLoading ? Visibility.Visible : Visibility.Collapsed;
        public Visibility XUnityLoadingVisibility => IsXUnityLoading ? Visibility.Visible : Visibility.Collapsed;
        public Visibility ProgressPanelVisibility => IsInstalling ? Visibility.Visible : Visibility.Collapsed;

        // 进度百分比文本
        public string ProgressPercentText => $"{InstallProgress}%";

        public InstallViewModel(
            GameStateService gameStateService,
            VersionCacheService versionCacheService,
            VersionService versionService,
            InstallationStateService installationStateService)
        {
            _gameStateService = gameStateService;
            _versionCacheService = versionCacheService;
            _versionService = versionService;
            _installationStateService = installationStateService;

            // 订阅事件
            _versionCacheService.VersionsUpdated += OnVersionsUpdated;
            _installationStateService.InstallationStarted += OnInstallationStarted;
            _installationStateService.ProgressChanged += OnProgressChanged;
            _installationStateService.InstallationCompleted += OnInstallationCompleted;
        }

        partial void OnVersionModeIndexChanged(int value)
        {
            OnPropertyChanged(nameof(IsAutoMode));
            OnPropertyChanged(nameof(IsManualMode));
            OnPropertyChanged(nameof(AreVersionComboBoxesEnabled));
            OnPropertyChanged(nameof(IsPlatformComboBoxEnabled));

            // 切换模式时重新加载版本
            _ = OnVersionModeChangedAsync();
        }

        partial void OnSelectedPlatformIndexChanged(int value)
        {
            // 平台改变时重新加载版本
            _ = OnPlatformChangedAsync();
        }

        partial void OnIsBepInExLoadingChanged(bool value)
        {
            OnPropertyChanged(nameof(BepInExLoadingVisibility));
        }

        partial void OnIsXUnityLoadingChanged(bool value)
        {
            OnPropertyChanged(nameof(XUnityLoadingVisibility));
        }

        partial void OnIsInstallingChanged(bool value)
        {
            OnPropertyChanged(nameof(ProgressPanelVisibility));
        }

        partial void OnInstallProgressChanged(int value)
        {
            OnPropertyChanged(nameof(ProgressPercentText));
        }

        public async Task InitializeAsync(string gamePath)
        {
            if (!string.IsNullOrEmpty(gamePath))
            {
                LogService.Instance.Log($"游戏路径: {gamePath}", LogLevel.Info, "[安装]");

                // 检测游戏引擎并推荐平台
                var gameInfo = GameDetectionService.GetGameInfo(gamePath);
                LogService.Instance.Log($"检测到游戏引擎: {gameInfo.Engine}", LogLevel.Info, "[安装]");

                if (gameInfo.Engine == GameEngine.UnityIL2CPP)
                {
                    SelectedPlatformIndex = 2; // IL2CPP x64
                    LogService.Instance.Log("推荐平台: IL2CPP x64", LogLevel.Info, "[安装]");
                }
            }
            else
            {
                LogService.Instance.Log("警告: 未设置游戏路径，请先在首页选择游戏目录", LogLevel.Warning, "[安装]");
            }

            // 延迟一点时间后加载推荐版本
            await Task.Delay(100);

            if (IsAutoMode)
            {
                LogService.Instance.Log("检测到自动推荐模式，开始加载推荐版本", LogLevel.Info, "[安装]");
                await LoadRecommendedVersionsAsync();
            }
        }

        private async Task OnVersionModeChangedAsync()
        {
            if (IsAutoMode)
            {
                await LoadRecommendedVersionsAsync();
            }
            else
            {
                // 手动模式：加载完整版本列表
                if (_allBepInExVersions.Count == 0 || _allXUnityVersions.Count == 0)
                {
                    await LoadVersionsAsync();
                }
                else
                {
                    UpdateVersionComboBoxes();
                }
            }
        }

        private async Task OnPlatformChangedAsync()
        {
            if (IsAutoMode)
            {
                await LoadRecommendedVersionsAsync();
            }
            else if (_allBepInExVersions.Count > 0)
            {
                UpdateVersionComboBoxes();
            }
        }

        private void OnVersionsUpdated(object? sender, VersionsUpdatedEventArgs e)
        {
            _allBepInExVersions = e.BepInExVersions;
            _allXUnityVersions = e.XUnityVersions;

            UpdateVersionComboBoxes();

            LogService.Instance.Log($"版本列表已更新: BepInEx {_allBepInExVersions.Count} 个, XUnity {_allXUnityVersions.Count} 个", LogLevel.Info, "[安装]");
        }

        [RelayCommand]
        private async Task LoadVersionsAsync()
        {
            try
            {
                IsBepInExLoading = true;
                IsXUnityLoading = true;

                LogService.Instance.Log("正在从缓存加载版本列表...", LogLevel.Info, "[安装]");

                _allBepInExVersions = _versionCacheService.GetBepInExVersions();
                _allXUnityVersions = _versionCacheService.GetXUnityVersions();

                // 如果缓存为空，等待初始化
                if (_allBepInExVersions.Count == 0 && _allXUnityVersions.Count == 0 && !_versionCacheService.IsInitialized)
                {
                    LogService.Instance.Log("版本缓存尚未初始化，等待初始化完成...", LogLevel.Info, "[安装]");

                    var startTime = DateTime.Now;
                    while (!_versionCacheService.IsInitialized && (DateTime.Now - startTime).TotalSeconds < 10)
                    {
                        await Task.Delay(500);
                        _allBepInExVersions = _versionCacheService.GetBepInExVersions();
                        _allXUnityVersions = _versionCacheService.GetXUnityVersions();

                        if (_allBepInExVersions.Count > 0 || _allXUnityVersions.Count > 0)
                        {
                            break;
                        }
                    }
                }

                LogService.Instance.Log($"已从缓存加载 {_allBepInExVersions.Count} 个 BepInEx 版本和 {_allXUnityVersions.Count} 个 XUnity 版本", LogLevel.Info, "[安装]");

                UpdateVersionComboBoxes();
            }
            catch (Exception ex)
            {
                LogService.Instance.Log($"加载版本列表失败: {ex.Message}", LogLevel.Error, "[安装]");
            }
            finally
            {
                IsBepInExLoading = false;
                IsXUnityLoading = false;
            }
        }

        [RelayCommand]
        private async Task LoadRecommendedVersionsAsync()
        {
            try
            {
                LogService.Instance.Log("开始加载推荐版本...", LogLevel.Info, "[安装]");

                IsBepInExLoading = true;
                IsXUnityLoading = true;

                var versionCounts = _versionCacheService.GetVersionCounts();
                LogService.Instance.Log($"版本缓存状态: 已初始化={_versionCacheService.IsInitialized}, BepInEx={versionCounts.BepInExCount}, XUnity={versionCounts.XUnityCount}", LogLevel.Info, "[安装]");

                // 等待初始化
                if (!_versionCacheService.IsInitialized)
                {
                    LogService.Instance.Log("版本缓存尚未初始化，等待初始化完成...", LogLevel.Info, "[安装]");

                    var startTime = DateTime.Now;
                    while (!_versionCacheService.IsInitialized && (DateTime.Now - startTime).TotalSeconds < 10)
                    {
                        await Task.Delay(500);
                    }

                    if (!_versionCacheService.IsInitialized)
                    {
                        LogService.Instance.Log("等待版本缓存初始化超时", LogLevel.Warning, "[安装]");
                        BepInExPlaceholder = "版本缓存初始化中，请稍后刷新";
                        XUnityPlaceholder = "版本缓存初始化中，请稍后刷新";
                        return;
                    }

                    versionCounts = _versionCacheService.GetVersionCounts();
                    LogService.Instance.Log($"版本缓存已初始化: BepInEx={versionCounts.BepInExCount}, XUnity={versionCounts.XUnityCount}", LogLevel.Info, "[安装]");
                }

                if (versionCounts.BepInExCount == 0)
                {
                    LogService.Instance.Log("版本缓存为空，请在版本管理页面手动刷新", LogLevel.Warning, "[安装]");
                    BepInExPlaceholder = "缓存为空，请在版本管理页面刷新";
                    XUnityPlaceholder = "缓存为空，请在版本管理页面刷新";
                    return;
                }

                var selectedPlatform = SelectedPlatformIndex switch
                {
                    0 => Platform.x64,
                    1 => Platform.x86,
                    2 => Platform.IL2CPP_x64,
                    3 => Platform.IL2CPP_x86,
                    _ => Platform.x64
                };

                LogService.Instance.Log($"当前选择平台: {selectedPlatform}", LogLevel.Debug, "[安装]");

                // 获取推荐版本
                var bepinexRecommended = _versionCacheService.GetLatestBepInExVersion(
                    selectedPlatform,
                    includePrerelease: selectedPlatform == Platform.IL2CPP_x64 || selectedPlatform == Platform.IL2CPP_x86);
                var xunityRecommended = _versionCacheService.GetLatestXUnityVersion(includePrerelease: false);

                LogService.Instance.Log($"推荐版本: BepInEx={bepinexRecommended?.Version ?? "null"}, XUnity={xunityRecommended?.Version ?? "null"}", LogLevel.Info, "[安装]");

                // 更新 BepInEx
                BepInExItems.Clear();
                if (bepinexRecommended != null)
                {
                    var item = new VersionDisplayItem
                    {
                        VersionInfo = bepinexRecommended,
                        Display = $"{bepinexRecommended.Version} ({bepinexRecommended.ReleaseDate:yyyy-MM-dd}) [推荐]"
                    };
                    BepInExItems.Add(item);
                    SelectedBepInExItem = item;
                    LogService.Instance.Log($"已设置BepInEx: {item.Display}", LogLevel.Info, "[安装]");
                }
                else
                {
                    BepInExPlaceholder = $"未找到{selectedPlatform}平台的版本";
                    LogService.Instance.Log($"未找到{selectedPlatform}平台的BepInEx版本", LogLevel.Warning, "[安装]");
                }

                // 更新 XUnity
                XUnityItems.Clear();
                if (xunityRecommended != null)
                {
                    var variantText = xunityRecommended.TargetPlatform == Platform.IL2CPP_x64 || xunityRecommended.TargetPlatform == Platform.IL2CPP_x86
                        ? "IL2CPP"
                        : "Mono";

                    var item = new VersionDisplayItem
                    {
                        VersionInfo = xunityRecommended,
                        Display = $"{xunityRecommended.Version} ({variantText}) ({xunityRecommended.ReleaseDate:yyyy-MM-dd}) [推荐]"
                    };
                    XUnityItems.Add(item);
                    SelectedXUnityItem = item;
                    LogService.Instance.Log($"已设置XUnity: {item.Display}", LogLevel.Info, "[安装]");
                }
                else
                {
                    XUnityPlaceholder = "未找到XUnity版本";
                    LogService.Instance.Log("未找到XUnity版本", LogLevel.Warning, "[安装]");
                }

                LogService.Instance.Log("推荐版本加载完成", LogLevel.Info, "[安装]");
            }
            catch (Exception ex)
            {
                LogService.Instance.Log($"加载推荐版本时发生异常: {ex.Message}", LogLevel.Error, "[安装]");
                LogService.Instance.Log($"异常堆栈: {ex.StackTrace}", LogLevel.Debug, "[安装]");
                BepInExPlaceholder = "加载失败";
                XUnityPlaceholder = "加载失败";
            }
            finally
            {
                IsBepInExLoading = false;
                IsXUnityLoading = false;
            }
        }

        private void UpdateVersionComboBoxes()
        {
            var selectedPlatform = SelectedPlatformIndex switch
            {
                0 => Platform.x64,
                1 => Platform.x86,
                2 => Platform.IL2CPP_x64,
                3 => Platform.IL2CPP_x86,
                _ => Platform.x64
            };

            // 更新 BepInEx
            var bepinexFiltered = _allBepInExVersions
                .Where(v => v.TargetPlatform == selectedPlatform)
                .OrderByDescending(v => v.ReleaseDate)
                .Select(v => new VersionDisplayItem
                {
                    VersionInfo = v,
                    Display = $"{v.Version} ({v.ReleaseDate:yyyy-MM-dd})"
                })
                .ToList();

            BepInExItems.Clear();
            foreach (var item in bepinexFiltered)
            {
                BepInExItems.Add(item);
            }
            if (BepInExItems.Count > 0)
            {
                SelectedBepInExItem = BepInExItems[0];
            }

            // 更新 XUnity
            bool isIL2CPP = selectedPlatform == Platform.IL2CPP_x64 || selectedPlatform == Platform.IL2CPP_x86;

            var xunityFiltered = _allXUnityVersions
                .Where(v =>
                {
                    if (isIL2CPP)
                    {
                        return v.TargetPlatform == Platform.IL2CPP_x64 || v.TargetPlatform == null;
                    }
                    else
                    {
                        return v.TargetPlatform == null || v.TargetPlatform == Platform.IL2CPP_x64;
                    }
                })
                .OrderByDescending(v =>
                {
                    if (isIL2CPP && v.TargetPlatform == Platform.IL2CPP_x64) return 1;
                    if (!isIL2CPP && v.TargetPlatform == null) return 1;
                    return 0;
                })
                .ThenByDescending(v => v.ReleaseDate)
                .Select(v =>
                {
                    var variantText = v.TargetPlatform == Platform.IL2CPP_x64 || v.TargetPlatform == Platform.IL2CPP_x86
                        ? "IL2CPP"
                        : "Mono";
                    var recommended = (isIL2CPP && v.TargetPlatform == Platform.IL2CPP_x64) ||
                                     (!isIL2CPP && v.TargetPlatform == null)
                        ? " [推荐]"
                        : "";

                    return new VersionDisplayItem
                    {
                        VersionInfo = v,
                        Display = $"{v.Version} ({variantText}){recommended} ({v.ReleaseDate:yyyy-MM-dd})"
                    };
                })
                .ToList();

            XUnityItems.Clear();
            foreach (var item in xunityFiltered)
            {
                XUnityItems.Add(item);
            }
            if (XUnityItems.Count > 0)
            {
                SelectedXUnityItem = XUnityItems[0];
            }
        }

        [RelayCommand(CanExecute = nameof(CanStartInstall))]
        private async Task StartInstallAsync()
        {
            var gamePath = _gameStateService.CurrentGamePath;
            if (string.IsNullOrEmpty(gamePath))
            {
                // 通过事件通知 Page 显示错误对话框
                LogService.Instance.Log("游戏路径未设置", LogLevel.Error, "[安装]");
                return;
            }

            IsInstalling = true;
            _installationStateService.StartInstallation();

            try
            {
                var options = new InstallOptions
                {
                    TargetPlatform = SelectedPlatformIndex switch
                    {
                        0 => Platform.x64,
                        1 => Platform.x86,
                        2 => Platform.IL2CPP_x64,
                        3 => Platform.IL2CPP_x86,
                        _ => Platform.x64
                    },
                    BackupExisting = BackupExisting,
                    CleanOldVersion = CleanOldVersion,
                    LaunchGameToGenerateConfig = LaunchGameToGenerateConfig,
                    ConfigGenerationTimeout = ConfigGenerationTimeout
                };

                // 手动模式：设置版本
                if (IsManualMode)
                {
                    if (SelectedBepInExItem != null)
                    {
                        options.BepInExVersion = SelectedBepInExItem.VersionInfo.Version;
                    }
                    if (SelectedXUnityItem != null)
                    {
                        options.XUnityVersion = SelectedXUnityItem.VersionInfo.Version;
                    }
                }

                LogService.Instance.Log($"开始安装到: {gamePath}", LogLevel.Info, "[安装]");
                LogService.Instance.Log($"平台: {options.TargetPlatform}", LogLevel.Info, "[安装]");
                LogService.Instance.Log($"版本模式: {(IsAutoMode ? "自动推荐" : "手动选择")}", LogLevel.Info, "[安装]");

                var logger = new LogWriter(null, null!);
                var installService = new InstallationService(logger);

                var progress = _installationStateService.CreateProgressReporter();
                var success = await installService.InstallAsync(gamePath, options, progress);

                if (success)
                {
                    LogService.Instance.Log("========================================", LogLevel.Info, "[安装]");
                    LogService.Instance.Log("安装成功完成！", LogLevel.Info, "[安装]");
                    LogService.Instance.Log("========================================", LogLevel.Info, "[安装]");

                    _installationStateService.CompleteInstallation(true);
                }
                else
                {
                    _installationStateService.CompleteInstallation(false);
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.Log($"安装失败: {ex.Message}", LogLevel.Error, "[安装]");
                _installationStateService.CompleteInstallation(false);
            }
            finally
            {
                IsInstalling = false;
            }
        }

        private bool CanStartInstall()
        {
            return !IsInstalling && !_installationStateService.IsInstalling;
        }

        private void OnInstallationStarted(object? sender, EventArgs e)
        {
            IsInstalling = true;
            StartInstallCommand.NotifyCanExecuteChanged();
        }

        private void OnProgressChanged(object? sender, (int progress, string message) e)
        {
            InstallProgress = e.progress;
            InstallStatusMessage = e.message;
        }

        private void OnInstallationCompleted(object? sender, bool success)
        {
            IsInstalling = false;
            StartInstallCommand.NotifyCanExecuteChanged();
        }

        public void Cleanup()
        {
            _versionCacheService.VersionsUpdated -= OnVersionsUpdated;
            _installationStateService.InstallationStarted -= OnInstallationStarted;
            _installationStateService.ProgressChanged -= OnProgressChanged;
            _installationStateService.InstallationCompleted -= OnInstallationCompleted;
        }
    }

    public class VersionDisplayItem
    {
        public VersionInfo VersionInfo { get; set; } = null!;
        public string Display { get; set; } = string.Empty;
    }
}
