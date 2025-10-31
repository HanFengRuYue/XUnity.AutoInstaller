using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using XUnity.AutoInstaller.Models;
using XUnity.AutoInstaller.Services;
using XUnity.AutoInstaller.Utils;

namespace XUnity.AutoInstaller.Pages
{
    public sealed partial class DashboardPage : Page
    {
        private readonly GameStateService _gameStateService;

        public DashboardPage()
        {
            this.InitializeComponent();
            _gameStateService = GameStateService.Instance;
            QuickInstallButton.IsEnabled = false;

            // Subscribe to GamePathChanged event
            _gameStateService.GamePathChanged += OnGamePathChanged;

            // Load current game path if available
            LoadCurrentGamePath();
        }

        private void OnGamePathChanged(object? sender, string? gamePath)
        {
            // Update UI on dispatcher queue
            DispatcherQueue.TryEnqueue(() =>
            {
                LoadCurrentGamePath();
            });
        }

        private void LoadCurrentGamePath()
        {
            var gamePath = _gameStateService.CurrentGamePath;
            if (!string.IsNullOrEmpty(gamePath))
            {
                GamePathTextBox.Text = gamePath;
                RefreshStatus();
                QuickInstallButton.IsEnabled = true;
            }
        }

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FolderPicker();
                picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
                picker.FileTypeFilter.Add("*");

                // 获取主窗口 HWND（WinUI3 必需）
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                var folder = await picker.PickSingleFolderAsync();
                if (folder != null)
                {
                    var gamePath = folder.Path;

                    // 验证游戏目录
                    if (PathHelper.IsValidGameDirectory(gamePath))
                    {
                        _gameStateService.SetGamePath(gamePath, saveToSettings: true);
                        GamePathTextBox.Text = gamePath;
                        RefreshStatus();

                        StatusInfoBar.Severity = InfoBarSeverity.Success;
                        StatusInfoBar.Title = "成功";
                        StatusInfoBar.Message = "游戏路径已设置";
                        StatusInfoBar.IsOpen = true;
                    }
                    else
                    {
                        StatusInfoBar.Severity = InfoBarSeverity.Warning;
                        StatusInfoBar.Title = "警告";
                        StatusInfoBar.Message = "所选目录不是有效的游戏目录（未找到可执行文件）";
                        StatusInfoBar.IsOpen = true;
                    }
                }
            }
            catch (Exception ex)
            {
                StatusInfoBar.Severity = InfoBarSeverity.Error;
                StatusInfoBar.Title = "错误";
                StatusInfoBar.Message = $"选择文件夹失败: {ex.Message}";
                StatusInfoBar.IsOpen = true;
            }
        }


        private async void QuickInstallButton_Click(object sender, RoutedEventArgs e)
        {
            var gamePath = _gameStateService.CurrentGamePath;
            if (string.IsNullOrEmpty(gamePath))
            {
                StatusInfoBar.Severity = InfoBarSeverity.Warning;
                StatusInfoBar.Title = "警告";
                StatusInfoBar.Message = "请先选择游戏路径";
                StatusInfoBar.IsOpen = true;
                return;
            }

            // 直接使用默认选项开始安装
            await PerformQuickInstallAsync(gamePath);
        }

        private async Task PerformQuickInstallAsync(string gamePath)
        {
            try
            {
                // 禁用一键安装按钮，显示进度UI
                QuickInstallButton.IsEnabled = false;
                InstallProgressPanel.Visibility = Visibility.Visible;
                InstallProgressRing.IsActive = true;

                LogService.Instance.Log($"开始一键安装到: {gamePath}", LogLevel.Info, "[首页]");

                // 检测游戏引擎并自动选择平台
                var gameInfo = GameDetectionService.GetGameInfo(gamePath);
                var targetPlatform = gameInfo.Engine == GameEngine.UnityIL2CPP ? Platform.IL2CPP_x64 : Platform.x64;

                LogService.Instance.Log($"检测到游戏引擎: {gameInfo.Engine}，目标平台: {targetPlatform}", LogLevel.Info, "[首页]");

                // 创建默认安装选项（自动推荐版本）
                var options = new InstallOptions
                {
                    TargetPlatform = targetPlatform,
                    BackupExisting = true,
                    CleanOldVersion = false,
                    UseRecommendedConfig = true,
                    LaunchGameToGenerateConfig = true,
                    ConfigGenerationTimeout = 60,
                    BepInExVersion = null, // null表示自动选择最新版
                    XUnityVersion = null
                };

                // 创建日志记录器
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
                    LogService.Instance.Log("一键安装成功完成！", LogLevel.Info, "[首页]");

                    StatusInfoBar.Severity = InfoBarSeverity.Success;
                    StatusInfoBar.Title = "成功";
                    StatusInfoBar.Message = "一键安装完成！您现在可以启动游戏并享受自动翻译功能。";
                    StatusInfoBar.IsOpen = true;

                    // 刷新状态显示
                    RefreshStatus();
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.Log($"一键安装失败: {ex.Message}", LogLevel.Error, "[首页]");

                StatusInfoBar.Severity = InfoBarSeverity.Error;
                StatusInfoBar.Title = "错误";
                StatusInfoBar.Message = $"一键安装失败: {ex.Message}";
                StatusInfoBar.IsOpen = true;
            }
            finally
            {
                // 恢复UI状态
                QuickInstallButton.IsEnabled = true;
                InstallProgressRing.IsActive = false;
                // 保持进度面板可见以便用户查看完成状态
            }
        }

        private void OpenGameFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var gamePath = _gameStateService.CurrentGamePath;
            if (string.IsNullOrEmpty(gamePath))
            {
                StatusInfoBar.Severity = InfoBarSeverity.Warning;
                StatusInfoBar.Title = "警告";
                StatusInfoBar.Message = "请先选择游戏路径";
                StatusInfoBar.IsOpen = true;
                return;
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
                StatusInfoBar.Severity = InfoBarSeverity.Error;
                StatusInfoBar.Title = "错误";
                StatusInfoBar.Message = $"打开文件夹失败: {ex.Message}";
                StatusInfoBar.IsOpen = true;
            }
        }

        private void RefreshStatusButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_gameStateService.HasValidGamePath())
            {
                StatusInfoBar.Severity = InfoBarSeverity.Warning;
                StatusInfoBar.Title = "警告";
                StatusInfoBar.Message = "请先选择游戏路径";
                StatusInfoBar.IsOpen = true;
                return;
            }

            RefreshStatus();
            StatusInfoBar.Severity = InfoBarSeverity.Success;
            StatusInfoBar.Title = "成功";
            StatusInfoBar.Message = "状态已刷新";
            StatusInfoBar.IsOpen = true;
        }

        /// <summary>
        /// 刷新安装状态显示
        /// </summary>
        private void RefreshStatus()
        {
            var gamePath = _gameStateService.CurrentGamePath;
            if (string.IsNullOrEmpty(gamePath))
            {
                return;
            }

            var status = GameDetectionService.DetectInstallationStatus(gamePath);

            // 更新 BepInEx 状态
            if (status.IsBepInExInstalled)
            {
                BepInExStatusText.Text = "已安装";
                BepInExStatusText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorSuccessBrush"];
                BepInExVersionText.Text = status.BepInExVersion ?? "Unknown";
                BepInExPlatformText.Text = status.BepInExPlatform ?? "Unknown";
            }
            else
            {
                BepInExStatusText.Text = "未安装";
                BepInExStatusText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                BepInExVersionText.Text = "--";
                BepInExPlatformText.Text = "--";
            }

            // 更新 XUnity 状态
            if (status.IsXUnityInstalled)
            {
                XUnityStatusText.Text = "已安装";
                XUnityStatusText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorSuccessBrush"];
                XUnityVersionText.Text = status.XUnityVersion ?? "Unknown";
                XUnityEngineText.Text = status.TranslationEngine ?? "Unknown";
            }
            else
            {
                XUnityStatusText.Text = "未安装";
                XUnityStatusText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["SystemFillColorCriticalBrush"];
                XUnityVersionText.Text = "--";
                XUnityEngineText.Text = "--";
            }

            // 启用/禁用一键安装按钮
            QuickInstallButton.IsEnabled = true;
        }
    }
}
