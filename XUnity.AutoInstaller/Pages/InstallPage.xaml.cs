using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XUnity.AutoInstaller.Models;
using XUnity.AutoInstaller.Services;
using XUnity.AutoInstaller.Utils;

namespace XUnity.AutoInstaller.Pages
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
        private bool _isInstalling = false;
        private readonly VersionService _versionService;
        private List<VersionInfo> _bepinexVersions = new();
        private List<VersionInfo> _xunityVersions = new();

        public InstallPage()
        {
            this.InitializeComponent();

            _gameStateService = GameStateService.Instance;
            _versionService = new VersionService();

            // 设置默认值
            PlatformComboBox.SelectedIndex = 0; // x64
            BackupCheckBox.IsChecked = true;
            AutoConfigCheckBox.IsChecked = true;
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
        }

        private async void VersionModeRadio_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VersionModeRadio.SelectedIndex == 0)
            {
                // 自动推荐模式
                BepInExVersionComboBox.IsEnabled = false;
                XUnityVersionComboBox.IsEnabled = false;
            }
            else
            {
                // 手动选择模式
                BepInExVersionComboBox.IsEnabled = true;
                XUnityVersionComboBox.IsEnabled = true;

                // 如果版本列表为空，加载版本列表
                if (_bepinexVersions.Count == 0 || _xunityVersions.Count == 0)
                {
                    await LoadVersionsAsync();
                }
            }
        }

        private async void PlatformComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 如果是手动模式，重新加载BepInEx版本列表（因为平台改变）
            if (VersionModeRadio.SelectedIndex == 1 && _bepinexVersions.Count > 0)
            {
                UpdateBepInExVersionComboBox();
            }
        }

        /// <summary>
        /// 加载可用版本列表
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

                LogService.Instance.Log("正在加载可用版本列表...", LogLevel.Info, "[安装]");

                // 并行加载BepInEx和XUnity版本
                var bepinexTask = _versionService.GetAllAvailableVersionsAsync(PackageType.BepInEx, includePrerelease: false);
                var xunityTask = _versionService.GetAllAvailableVersionsAsync(PackageType.XUnity, includePrerelease: false);

                await Task.WhenAll(bepinexTask, xunityTask);

                _bepinexVersions = bepinexTask.Result;
                _xunityVersions = xunityTask.Result;

                LogService.Instance.Log($"已加载 {_bepinexVersions.Count} 个 BepInEx 版本和 {_xunityVersions.Count} 个 XUnity 版本", LogLevel.Info, "[安装]");

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
        /// 更新XUnity版本ComboBox
        /// </summary>
        private void UpdateXUnityVersionComboBox()
        {
            var displayItems = _xunityVersions
                .OrderByDescending(v => v.ReleaseDate)
                .Select(v => new VersionDisplayItem
                {
                    VersionInfo = v,
                    Display = $"{v.Version} ({v.ReleaseDate:yyyy-MM-dd})"
                })
                .ToList();

            XUnityVersionComboBox.ItemsSource = displayItems;
            if (displayItems.Count > 0)
            {
                XUnityVersionComboBox.SelectedIndex = 0; // 选择最新版本
            }
        }

        private async void StartInstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isInstalling)
            {
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
                    CreateShortcut = CreateShortcutCheckBox.IsChecked == true,
                    UseRecommendedConfig = AutoConfigCheckBox.IsChecked == true
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
                LogService.Instance.Log($"使用推荐配置: {options.UseRecommendedConfig}", LogLevel.Info, "[安装]");

                // 创建日志记录器（现在内部使用LogService）
                var logger = new LogWriter(null, DispatcherQueue);

                // 创建安装服务
                var installService = new InstallationService(logger);

                // 创建进度报告
                var progress = new Progress<(int percentage, string message)>(p =>
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        InstallProgressBar.Value = p.percentage;
                        ProgressText.Text = p.message;
                        ProgressPercentText.Text = $"{p.percentage}%";
                    });
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

                    await ShowSuccessAsync("安装成功完成！");
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.Log("", LogLevel.Error, "[安装]");
                LogService.Instance.Log("========================================", LogLevel.Error, "[安装]");
                LogService.Instance.Log($"安装失败: {ex.Message}", LogLevel.Error, "[安装]");
                LogService.Instance.Log("========================================", LogLevel.Error, "[安装]");
                LogService.Instance.Log("", LogLevel.Error, "[安装]");

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

            var dialog = new ContentDialog
            {
                Title = "错误",
                Content = message,
                CloseButtonText = "确定",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private async Task ShowSuccessAsync(string message)
        {
            // 如果 XamlRoot 为 null,延迟到下一个 UI 周期
            if (this.XamlRoot == null)
            {
                DispatcherQueue.TryEnqueue(async () => await ShowSuccessAsync(message));
                return;
            }

            var dialog = new ContentDialog
            {
                Title = "成功",
                Content = message,
                CloseButtonText = "确定",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
