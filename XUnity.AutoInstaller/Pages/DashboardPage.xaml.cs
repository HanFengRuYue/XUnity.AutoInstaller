using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
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


        private void QuickInstallButton_Click(object sender, RoutedEventArgs e)
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

            // 导航到安装页面（不再需要传递参数）
            Frame.Navigate(typeof(InstallPage));
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
