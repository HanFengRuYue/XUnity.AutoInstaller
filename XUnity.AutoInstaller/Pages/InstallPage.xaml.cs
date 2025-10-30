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
        private string? _gamePath;
        private bool _isInstalling = false;
        private readonly VersionService _versionService;
        private List<VersionInfo> _bepinexVersions = new();
        private List<VersionInfo> _xunityVersions = new();

        public InstallPage()
        {
            this.InitializeComponent();

            _versionService = new VersionService();

            // 设置默认值
            PlatformComboBox.SelectedIndex = 0; // x64
            BackupCheckBox.IsChecked = true;
            AutoConfigCheckBox.IsChecked = true;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is string gamePath)
            {
                _gamePath = gamePath;
                AppendLog($"游戏路径: {gamePath}");

                // 检测游戏引擎并推荐平台
                var gameInfo = GameDetectionService.GetGameInfo(gamePath);
                AppendLog($"检测到游戏引擎: {gameInfo.Engine}");

                if (gameInfo.Engine == GameEngine.UnityIL2CPP)
                {
                    PlatformComboBox.SelectedIndex = 2; // IL2CPP x64
                    AppendLog("推荐平台: IL2CPP x64");
                }
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

                AppendLog("正在加载可用版本列表...");

                // 并行加载BepInEx和XUnity版本
                var bepinexTask = _versionService.GetAllAvailableVersionsAsync(PackageType.BepInEx, includePrerelease: false);
                var xunityTask = _versionService.GetAllAvailableVersionsAsync(PackageType.XUnity, includePrerelease: false);

                await Task.WhenAll(bepinexTask, xunityTask);

                _bepinexVersions = bepinexTask.Result;
                _xunityVersions = xunityTask.Result;

                AppendLog($"已加载 {_bepinexVersions.Count} 个 BepInEx 版本和 {_xunityVersions.Count} 个 XUnity 版本");

                // 更新ComboBox
                UpdateBepInExVersionComboBox();
                UpdateXUnityVersionComboBox();
            }
            catch (Exception ex)
            {
                AppendLog($"加载版本列表失败: {ex.Message}");
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

            if (string.IsNullOrEmpty(_gamePath))
            {
                await ShowErrorAsync("游戏路径未设置");
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

                AppendLog($"开始安装到: {_gamePath}");
                AppendLog($"平台: {options.TargetPlatform}");
                AppendLog($"版本模式: {(VersionModeRadio.SelectedIndex == 0 ? "自动推荐" : "手动选择")}");
                if (!string.IsNullOrEmpty(options.BepInExVersion))
                {
                    AppendLog($"BepInEx 版本: {options.BepInExVersion}");
                }
                if (!string.IsNullOrEmpty(options.XUnityVersion))
                {
                    AppendLog($"XUnity 版本: {options.XUnityVersion}");
                }
                AppendLog($"备份现有: {options.BackupExisting}");
                AppendLog($"清理旧版本: {options.CleanOldVersion}");
                AppendLog($"使用推荐配置: {options.UseRecommendedConfig}");

                // 创建日志记录器
                var logger = new LogWriter(AppendLog, DispatcherQueue);

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
                var success = await installService.InstallAsync(_gamePath, options, progress);

                if (success)
                {
                    AppendLog("");
                    AppendLog("========================================");
                    AppendLog("安装成功完成！");
                    AppendLog("========================================");
                    AppendLog("你现在可以启动游戏并享受自动翻译功能。");
                    AppendLog("");

                    await ShowSuccessAsync("安装成功完成！");
                }
            }
            catch (Exception ex)
            {
                AppendLog("");
                AppendLog("========================================");
                AppendLog($"安装失败: {ex.Message}");
                AppendLog("========================================");
                AppendLog("");

                await ShowErrorAsync($"安装失败: {ex.Message}");
            }
            finally
            {
                _isInstalling = false;
                StartInstallButton.IsEnabled = true;
                InstallProgressRing.IsActive = false;
            }
        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            LogTextBlock.Text = string.Empty;
        }

        /// <summary>
        /// 向日志添加一行文本
        /// </summary>
        private void AppendLog(string message)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var logLine = $"[{timestamp}] {message}\n";

            DispatcherQueue.TryEnqueue(() =>
            {
                LogTextBlock.Text += logLine;
                // 自动滚动到底部
                LogScrollViewer.ChangeView(null, LogScrollViewer.ScrollableHeight, null);
            });
        }

        private async Task ShowErrorAsync(string message)
        {
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
