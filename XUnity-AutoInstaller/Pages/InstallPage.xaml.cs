using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XUnity_AutoInstaller.Models;
using XUnity_AutoInstaller.Services;
using XUnity_AutoInstaller.Utils;

namespace XUnity_AutoInstaller.Pages
{
    /// <summary>
    /// 用于ComboBox显示的版本包装类
    /// </summary>
    public class VersionDisplayItem
    {
        public VersionInfo VersionInfo { get; set; } = null!;
        public string Display { get; set; } = string.Empty;
    }

    public sealed partial class InstallPage : Page
    {
        private readonly GameStateService _gameStateService;
        private readonly VersionCacheService _versionCacheService;
        private bool _isInstalling = false;
        private readonly VersionService _versionService;
        private List<VersionInfo> _bepinexVersions = new();
        private List<VersionInfo> _xunityVersions = new();

        public InstallPage()
        {
            this.InitializeComponent();

            _gameStateService = GameStateService.Instance;
            _versionCacheService = VersionCacheService.Instance;
            _versionService = new VersionService();

            // 订阅版本更新事件
            _versionCacheService.VersionsUpdated += OnVersionsUpdated;

            // 设置默认值
            // FIX: WinUI3中RadioButton的IsChecked不会自动设置RadioButtons的SelectedIndex
            VersionModeRadio.SelectedIndex = 0; // 自动推荐模式
            PlatformComboBox.SelectedIndex = 0; // x64
            BackupCheckBox.IsChecked = true;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Get game path from GameStateService
            var gamePath = _gameStateService.CurrentGamePath;
            if (!string.IsNullOrEmpty(gamePath))
            {
                LogService.Instance.Log($"游戏路径: {gamePath}", LogLevel.Info, "[安装]");

                // 检测游戏引擎并推荐平台
                var gameInfo = GameDetectionService.GetGameInfo(gamePath);
                LogService.Instance.Log($"检测到游戏引擎: {gameInfo.Engine}", LogLevel.Info, "[安装]");

                if (gameInfo.Engine == GameEngine.UnityIL2CPP)
                {
                    PlatformComboBox.SelectedIndex = 2; // IL2CPP x64
                    LogService.Instance.Log("推荐平台: IL2CPP x64", LogLevel.Info, "[安装]");
                }
            }
            else
            {
                LogService.Instance.Log("警告: 未设置游戏路径，请先在首页选择游戏目录", LogLevel.Warning, "[安装]");
            }

            // 使用DispatcherQueue延迟执行，确保UI完全加载后再加载推荐版本
            DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, async () =>
            {
                LogService.Instance.Log("页面导航完成，准备加载推荐版本...", LogLevel.Info, "[安装]");

                // 给UI一点时间完成渲染
                await Task.Delay(100);

                // 检查是否为自动推荐模式
                if (VersionModeRadio?.SelectedIndex == 0)
                {
                    LogService.Instance.Log("检测到自动推荐模式，开始加载推荐版本", LogLevel.Info, "[安装]");
                    await LoadAndDisplayRecommendedVersionsAsync();
                }
                else
                {
                    LogService.Instance.Log($"当前为手动选择模式 (SelectedIndex={VersionModeRadio?.SelectedIndex})", LogLevel.Debug, "[安装]");
                }
            });
        }

        private async void VersionModeRadio_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VersionModeRadio.SelectedIndex == 0)
            {
                // 自动推荐模式
                BepInExVersionComboBox.IsEnabled = false;
                XUnityVersionComboBox.IsEnabled = false;
                PlatformComboBox.IsEnabled = false; // 自动模式下锁定平台

                // 加载并显示推荐版本
                await LoadAndDisplayRecommendedVersionsAsync();
            }
            else
            {
                // 手动选择模式
                BepInExVersionComboBox.IsEnabled = true;
                XUnityVersionComboBox.IsEnabled = true;
                PlatformComboBox.IsEnabled = true; // 手动模式下允许选择平台

                // 如果版本列表为空，加载版本列表
                if (_bepinexVersions.Count == 0 || _xunityVersions.Count == 0)
                {
                    await LoadVersionsAsync();
                }
            }
        }

        private async void PlatformComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VersionModeRadio.SelectedIndex == 0)
            {
                // 自动推荐模式：平台改变时重新加载推荐版本
                await LoadAndDisplayRecommendedVersionsAsync();
            }
            else
            {
                // 手动模式：重新过滤版本列表
                if (_bepinexVersions.Count > 0)
                {
                    UpdateBepInExVersionComboBox();
                }
                if (_xunityVersions.Count > 0)
                {
                    UpdateXUnityVersionComboBox();
                }
            }
        }

        /// <summary>
        /// 版本更新事件处理
        /// </summary>
        private void OnVersionsUpdated(object? sender, VersionsUpdatedEventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                _bepinexVersions = e.BepInExVersions;
                _xunityVersions = e.XUnityVersions;

                // 更新ComboBox
                UpdateBepInExVersionComboBox();
                UpdateXUnityVersionComboBox();

                LogService.Instance.Log($"版本列表已更新: BepInEx {_bepinexVersions.Count} 个, XUnity {_xunityVersions.Count} 个", LogLevel.Info, "[安装]");
            });
        }

        /// <summary>
        /// 加载可用版本列表（从缓存）
        /// </summary>
        private async Task LoadVersionsAsync()
        {
            try
            {
                // 显示加载指示器
                BepInExVersionLoadingRing.IsActive = true;
                BepInExVersionLoadingRing.Visibility = Visibility.Visible;
                XUnityVersionLoadingRing.IsActive = true;
                XUnityVersionLoadingRing.Visibility = Visibility.Visible;

                LogService.Instance.Log("正在从缓存加载版本列表...", LogLevel.Info, "[安装]");

                // 从缓存获取版本列表
                _bepinexVersions = _versionCacheService.GetBepInExVersions();
                _xunityVersions = _versionCacheService.GetXUnityVersions();

                // 如果缓存为空，等待初始化完成
                if (_bepinexVersions.Count == 0 && _xunityVersions.Count == 0 && !_versionCacheService.IsInitialized)
                {
                    LogService.Instance.Log("版本缓存尚未初始化，等待初始化完成...", LogLevel.Info, "[安装]");

                    // 等待最多 10 秒
                    var startTime = DateTime.Now;
                    while (!_versionCacheService.IsInitialized && (DateTime.Now - startTime).TotalSeconds < 10)
                    {
                        await Task.Delay(500);
                        _bepinexVersions = _versionCacheService.GetBepInExVersions();
                        _xunityVersions = _versionCacheService.GetXUnityVersions();

                        if (_bepinexVersions.Count > 0 || _xunityVersions.Count > 0)
                        {
                            break;
                        }
                    }
                }

                LogService.Instance.Log($"已从缓存加载 {_bepinexVersions.Count} 个 BepInEx 版本和 {_xunityVersions.Count} 个 XUnity 版本", LogLevel.Info, "[安装]");

                // 更新ComboBox
                UpdateBepInExVersionComboBox();
                UpdateXUnityVersionComboBox();
            }
            catch (Exception ex)
            {
                LogService.Instance.Log($"加载版本列表失败: {ex.Message}", LogLevel.Error, "[安装]");
                await ShowErrorAsync($"加载版本列表失败: {ex.Message}");
            }
            finally
            {
                // 隐藏加载指示器
                BepInExVersionLoadingRing.IsActive = false;
                BepInExVersionLoadingRing.Visibility = Visibility.Collapsed;
                XUnityVersionLoadingRing.IsActive = false;
                XUnityVersionLoadingRing.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 更新BepInEx版本ComboBox（根据当前选择的平台过滤）
        /// </summary>
        private void UpdateBepInExVersionComboBox()
        {
            var selectedPlatform = PlatformComboBox.SelectedIndex switch
            {
                0 => Platform.x64,
                1 => Platform.x86,
                2 => Platform.IL2CPP_x64,
                3 => Platform.IL2CPP_x86,
                _ => Platform.x64
            };

            // 过滤出匹配当前平台的版本
            var filteredVersions = _bepinexVersions
                .Where(v => v.TargetPlatform == selectedPlatform)
                .OrderByDescending(v => v.ReleaseDate)
                .Select(v => new VersionDisplayItem
                {
                    VersionInfo = v,
                    Display = $"{v.Version} ({v.ReleaseDate:yyyy-MM-dd})"
                })
                .ToList();

            BepInExVersionComboBox.ItemsSource = filteredVersions;
            if (filteredVersions.Count > 0)
            {
                BepInExVersionComboBox.SelectedIndex = 0; // 选择最新版本
            }
        }

        /// <summary>
        /// 更新XUnity版本ComboBox（根据当前选择的平台过滤和排序）
        /// </summary>
        private void UpdateXUnityVersionComboBox()
        {
            var selectedPlatform = PlatformComboBox.SelectedIndex switch
            {
                0 => Platform.x64,
                1 => Platform.x86,
                2 => Platform.IL2CPP_x64,
                3 => Platform.IL2CPP_x86,
                _ => Platform.x64
            };

            bool isIL2CPP = selectedPlatform == Platform.IL2CPP_x64 || selectedPlatform == Platform.IL2CPP_x86;

            // 根据平台过滤和排序版本
            var filteredVersions = _xunityVersions
                .Where(v => 
                {
                    if (isIL2CPP)
                    {
                        // IL2CPP 平台：优先显示 IL2CPP 变体，但也显示 Mono 变体
                        return v.TargetPlatform == Platform.IL2CPP_x64 || v.TargetPlatform == null;
                    }
                    else
                    {
                        // Mono 平台：优先显示 Mono 变体，但也显示 IL2CPP 变体
                        return v.TargetPlatform == null || v.TargetPlatform == Platform.IL2CPP_x64;
                    }
                })
                .OrderByDescending(v => 
                {
                    // 按平台匹配度排序：匹配的变体优先
                    if (isIL2CPP && v.TargetPlatform == Platform.IL2CPP_x64) return 1;
                    if (!isIL2CPP && v.TargetPlatform == null) return 1;
                    return 0;
                })
                .ThenByDescending(v => v.ReleaseDate)
                .ToList();

            var displayItems = filteredVersions
                .Select(v => 
                {
                    // 显示变体信息
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

            XUnityVersionComboBox.ItemsSource = displayItems;
            if (displayItems.Count > 0)
            {
                XUnityVersionComboBox.SelectedIndex = 0; // 选择推荐版本（已排序）
            }
        }

        /// <summary>
        /// 加载并显示推荐版本（自动推荐模式）
        /// </summary>
        private async Task LoadAndDisplayRecommendedVersionsAsync()
        {
            try
            {
                LogService.Instance.Log("开始加载推荐版本...", LogLevel.Info, "[安装]");

                // 显示加载指示器
                BepInExVersionLoadingRing.IsActive = true;
                BepInExVersionLoadingRing.Visibility = Visibility.Visible;
                XUnityVersionLoadingRing.IsActive = true;
                XUnityVersionLoadingRing.Visibility = Visibility.Visible;

                // 检查版本缓存是否已初始化
                var versionCounts = _versionCacheService.GetVersionCounts();
                LogService.Instance.Log($"版本缓存状态: 已初始化={_versionCacheService.IsInitialized}, BepInEx={versionCounts.BepInExCount}, XUnity={versionCounts.XUnityCount}", LogLevel.Info, "[安装]");

                // 如果缓存尚未初始化，等待初始化完成（最多10秒）
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
                        BepInExVersionComboBox.PlaceholderText = "版本缓存初始化中，请稍后刷新";
                        XUnityVersionComboBox.PlaceholderText = "版本缓存初始化中，请稍后刷新";
                        return;
                    }

                    versionCounts = _versionCacheService.GetVersionCounts();
                    LogService.Instance.Log($"版本缓存已初始化: BepInEx={versionCounts.BepInExCount}, XUnity={versionCounts.XUnityCount}", LogLevel.Info, "[安装]");
                }

                // 如果缓存为空，说明可能网络有问题或尚未刷新
                if (versionCounts.BepInExCount == 0)
                {
                    LogService.Instance.Log("版本缓存为空，请在版本管理页面手动刷新", LogLevel.Warning, "[安装]");
                    BepInExVersionComboBox.PlaceholderText = "缓存为空，请在版本管理页面刷新";
                    XUnityVersionComboBox.PlaceholderText = "缓存为空，请在版本管理页面刷新";
                    return;
                }

                // 获取当前选择的平台
                var selectedPlatform = PlatformComboBox.SelectedIndex switch
                {
                    0 => Platform.x64,
                    1 => Platform.x86,
                    2 => Platform.IL2CPP_x64,
                    3 => Platform.IL2CPP_x86,
                    _ => Platform.x64
                };

                LogService.Instance.Log($"当前选择平台: {selectedPlatform}", LogLevel.Debug, "[安装]");

                // 从缓存获取推荐版本
                var bepinexRecommended = _versionCacheService.GetLatestBepInExVersion(selectedPlatform, includePrerelease: selectedPlatform == Platform.IL2CPP_x64 || selectedPlatform == Platform.IL2CPP_x86);
                var xunityRecommended = _versionCacheService.GetLatestXUnityVersion(includePrerelease: false);

                LogService.Instance.Log($"推荐版本: BepInEx={bepinexRecommended?.Version ?? "null"}, XUnity={xunityRecommended?.Version ?? "null"}", LogLevel.Info, "[安装]");

                // 显示推荐的BepInEx版本
                if (bepinexRecommended != null)
                {
                    var bepinexDisplayItem = new VersionDisplayItem
                    {
                        VersionInfo = bepinexRecommended,
                        Display = $"{bepinexRecommended.Version} ({bepinexRecommended.ReleaseDate:yyyy-MM-dd}) [推荐]"
                    };
                    BepInExVersionComboBox.ItemsSource = new[] { bepinexDisplayItem };
                    BepInExVersionComboBox.SelectedIndex = 0;
                    LogService.Instance.Log($"已设置BepInEx ComboBox: {bepinexDisplayItem.Display}", LogLevel.Info, "[安装]");
                }
                else
                {
                    BepInExVersionComboBox.ItemsSource = null;
                    BepInExVersionComboBox.PlaceholderText = $"未找到{selectedPlatform}平台的版本";
                    LogService.Instance.Log($"未找到{selectedPlatform}平台的BepInEx版本", LogLevel.Warning, "[安装]");
                }

                // 显示推荐的XUnity版本
                if (xunityRecommended != null)
                {
                    // 显示变体信息
                    var variantText = xunityRecommended.TargetPlatform == Platform.IL2CPP_x64 || xunityRecommended.TargetPlatform == Platform.IL2CPP_x86
                        ? "IL2CPP"
                        : "Mono";
                    
                    var xunityDisplayItem = new VersionDisplayItem
                    {
                        VersionInfo = xunityRecommended,
                        Display = $"{xunityRecommended.Version} ({variantText}) ({xunityRecommended.ReleaseDate:yyyy-MM-dd}) [推荐]"
                    };
                    XUnityVersionComboBox.ItemsSource = new[] { xunityDisplayItem };
                    XUnityVersionComboBox.SelectedIndex = 0;
                    LogService.Instance.Log($"已设置XUnity ComboBox: {xunityDisplayItem.Display}", LogLevel.Info, "[安装]");
                }
                else
                {
                    XUnityVersionComboBox.ItemsSource = null;
                    XUnityVersionComboBox.PlaceholderText = "未找到XUnity版本";
                    LogService.Instance.Log("未找到XUnity版本", LogLevel.Warning, "[安装]");
                }

                LogService.Instance.Log("推荐版本加载完成", LogLevel.Info, "[安装]");
            }
            catch (Exception ex)
            {
                LogService.Instance.Log($"加载推荐版本时发生异常: {ex.Message}", LogLevel.Error, "[安装]");
                LogService.Instance.Log($"异常堆栈: {ex.StackTrace}", LogLevel.Debug, "[安装]");
                BepInExVersionComboBox.PlaceholderText = "加载失败";
                XUnityVersionComboBox.PlaceholderText = "加载失败";
            }
            finally
            {
                // 隐藏加载指示器
                BepInExVersionLoadingRing.IsActive = false;
                BepInExVersionLoadingRing.Visibility = Visibility.Collapsed;
                XUnityVersionLoadingRing.IsActive = false;
                XUnityVersionLoadingRing.Visibility = Visibility.Collapsed;
            }
        }

        private async void StartInstallButton_Click(object sender, RoutedEventArgs e)
        {
            var stateService = InstallationStateService.Instance;

            // 检查是否已有安装进行中
            if (stateService.IsInstalling || _isInstalling)
            {
                await ShowErrorAsync("已有安装任务正在进行中，请稍候");
                return;
            }

            var gamePath = _gameStateService.CurrentGamePath;
            if (string.IsNullOrEmpty(gamePath))
            {
                await ShowErrorAsync("游戏路径未设置，请先在首页选择游戏目录");
                return;
            }

            _isInstalling = true;
            StartInstallButton.IsEnabled = false;
            ProgressPanel.Visibility = Visibility.Visible;

            // 通知全局状态服务
            stateService.StartInstallation();

            try
            {
                // 准备安装选项
                var options = new InstallOptions
                {
                    TargetPlatform = PlatformComboBox.SelectedIndex switch
                    {
                        0 => Platform.x64,
                        1 => Platform.x86,
                        2 => Platform.IL2CPP_x64,
                        3 => Platform.IL2CPP_x86,
                        _ => Platform.x64
                    },
                    BackupExisting = BackupCheckBox.IsChecked == true,
                    CleanOldVersion = CleanInstallCheckBox.IsChecked == true,
                    LaunchGameToGenerateConfig = LaunchGameCheckBox.IsChecked == true,
                    ConfigGenerationTimeout = (int)ConfigTimeoutNumberBox.Value
                };

                // 如果是手动选择模式，设置版本
                if (VersionModeRadio.SelectedIndex == 1)
                {
                    // 手动选择模式
                    if (BepInExVersionComboBox.SelectedItem is VersionDisplayItem bepinexItem)
                    {
                        options.BepInExVersion = bepinexItem.VersionInfo.Version;
                    }
                    else
                    {
                        await ShowErrorAsync("请选择 BepInEx 版本");
                        return;
                    }

                    if (XUnityVersionComboBox.SelectedItem is VersionDisplayItem xunityItem)
                    {
                        options.XUnityVersion = xunityItem.VersionInfo.Version;
                    }
                    else
                    {
                        await ShowErrorAsync("请选择 XUnity 版本");
                        return;
                    }
                }

                LogService.Instance.Log($"开始安装到: {gamePath}", LogLevel.Info, "[安装]");
                LogService.Instance.Log($"平台: {options.TargetPlatform}", LogLevel.Info, "[安装]");
                LogService.Instance.Log($"版本模式: {(VersionModeRadio.SelectedIndex == 0 ? "自动推荐" : "手动选择")}", LogLevel.Info, "[安装]");
                if (!string.IsNullOrEmpty(options.BepInExVersion))
                {
                    LogService.Instance.Log($"BepInEx 版本: {options.BepInExVersion}", LogLevel.Info, "[安装]");
                }
                if (!string.IsNullOrEmpty(options.XUnityVersion))
                {
                    LogService.Instance.Log($"XUnity 版本: {options.XUnityVersion}", LogLevel.Info, "[安装]");
                }
                LogService.Instance.Log($"备份现有: {options.BackupExisting}", LogLevel.Info, "[安装]");
                LogService.Instance.Log($"清理旧版本: {options.CleanOldVersion}", LogLevel.Info, "[安装]");
                LogService.Instance.Log($"启动游戏生成配置: {options.LaunchGameToGenerateConfig}", LogLevel.Info, "[安装]");
                if (options.LaunchGameToGenerateConfig)
                {
                    LogService.Instance.Log($"配置生成超时: {options.ConfigGenerationTimeout}秒", LogLevel.Info, "[安装]");
                }

                // 创建日志记录器（现在内部使用LogService）
                var logger = new LogWriter(null, DispatcherQueue);

                // 创建安装服务
                var installService = new InstallationService(logger);

                // 创建组合进度报告（同时更新本地和全局进度）
                var progress = new Progress<(int percentage, string message)>(p =>
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        InstallProgressBar.Value = p.percentage;
                        ProgressText.Text = p.message;
                        ProgressPercentText.Text = $"{p.percentage}%";
                    });

                    // 同时更新全局进度
                    stateService.UpdateProgress(p.percentage, p.message);
                });

                // 执行安装
                var success = await installService.InstallAsync(gamePath, options, progress);

                if (success)
                {
                    LogService.Instance.Log("", LogLevel.Info, "[安装]");
                    LogService.Instance.Log("========================================", LogLevel.Info, "[安装]");
                    LogService.Instance.Log("安装成功完成！", LogLevel.Info, "[安装]");
                    LogService.Instance.Log("========================================", LogLevel.Info, "[安装]");
                    LogService.Instance.Log("你现在可以启动游戏并享受自动翻译功能。", LogLevel.Info, "[安装]");
                    LogService.Instance.Log("", LogLevel.Info, "[安装]");

                    stateService.CompleteInstallation(true);
                    await ShowSuccessAsync("安装成功完成！");
                }
                else
                {
                    stateService.CompleteInstallation(false);
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.Log("", LogLevel.Error, "[安装]");
                LogService.Instance.Log("========================================", LogLevel.Error, "[安装]");
                LogService.Instance.Log($"安装失败: {ex.Message}", LogLevel.Error, "[安装]");
                LogService.Instance.Log("========================================", LogLevel.Error, "[安装]");
                LogService.Instance.Log("", LogLevel.Error, "[安装]");

                stateService.CompleteInstallation(false);
                await ShowErrorAsync($"安装失败: {ex.Message}");
            }
            finally
            {
                _isInstalling = false;
                StartInstallButton.IsEnabled = true;
                InstallProgressRing.IsActive = false;
            }
        }


        private async Task ShowErrorAsync(string message)
        {
            // 如果 XamlRoot 为 null,延迟到下一个 UI 周期
            if (this.XamlRoot == null)
            {
                DispatcherQueue.TryEnqueue(async () => await ShowErrorAsync(message));
                return;
            }

            await DialogHelper.ShowErrorAsync(this.XamlRoot, "错误", message);
        }

        private async Task ShowSuccessAsync(string message)
        {
            // 如果 XamlRoot 为 null,延迟到下一个 UI 周期
            if (this.XamlRoot == null)
            {
                DispatcherQueue.TryEnqueue(async () => await ShowSuccessAsync(message));
                return;
            }

            await DialogHelper.ShowSuccessAsync(this.XamlRoot, "成功", message);
        }
    }
}
