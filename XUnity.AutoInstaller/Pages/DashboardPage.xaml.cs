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
        private string? _currentGamePath;

        public DashboardPage()
        {
            this.InitializeComponent();
            QuickInstallButton.IsEnabled = false;
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
                        _currentGamePath = gamePath;
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

        private async void AutoDetectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusInfoBar.Severity = InfoBarSeverity.Informational;
                StatusInfoBar.Title = "提示";
                StatusInfoBar.Message = "正在自动检测游戏...";
                StatusInfoBar.IsOpen = true;

                // 在后台线程运行检测
                var games = await Task.Run(() => GameDetectionService.AutoDetectGames());

                if (games.Count == 0)
                {
                    StatusInfoBar.Severity = InfoBarSeverity.Warning;
                    StatusInfoBar.Title = "未找到游戏";
                    StatusInfoBar.Message = "未在常见位置找到 Unity 游戏，请手动选择游戏目录";
                    StatusInfoBar.IsOpen = true;
                    return;
                }

                // 如果找到多个游戏，显示选择对话框
                if (games.Count == 1)
                {
                    _currentGamePath = games[0].Path;
                    GamePathTextBox.Text = _currentGamePath;
                    RefreshStatus();

                    StatusInfoBar.Severity = InfoBarSeverity.Success;
                    StatusInfoBar.Title = "成功";
                    StatusInfoBar.Message = $"已检测到游戏: {games[0].Name}";
                    StatusInfoBar.IsOpen = true;
                }
                else
                {
                    // 显示选择对话框
                    await ShowGameSelectionDialog(games);
                }
            }
            catch (Exception ex)
            {
                StatusInfoBar.Severity = InfoBarSeverity.Error;
                StatusInfoBar.Title = "错误";
                StatusInfoBar.Message = $"自动检测失败: {ex.Message}";
                StatusInfoBar.IsOpen = true;
            }
        }

        private async Task ShowGameSelectionDialog(List<Models.GameInfo> games)
        {
            var dialog = new ContentDialog
            {
                Title = "选择游戏",
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock
                        {
                            Text = $"检测到 {games.Count} 个游戏，请选择:",
                            Margin = new Thickness(0, 0, 0, 12)
                        }
                    }
                },
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                XamlRoot = this.XamlRoot
            };

            var listView = new ListView
            {
                SelectionMode = ListViewSelectionMode.Single
            };

            foreach (var game in games)
            {
                listView.Items.Add($"{game.Name} ({game.Engine})");
            }

            listView.SelectedIndex = 0;
            ((StackPanel)dialog.Content).Children.Add(listView);

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary && listView.SelectedIndex >= 0)
            {
                var selectedGame = games[listView.SelectedIndex];
                _currentGamePath = selectedGame.Path;
                GamePathTextBox.Text = _currentGamePath;
                RefreshStatus();

                StatusInfoBar.Severity = InfoBarSeverity.Success;
                StatusInfoBar.Title = "成功";
                StatusInfoBar.Message = $"已选择游戏: {selectedGame.Name}";
                StatusInfoBar.IsOpen = true;
            }
        }

        private void QuickInstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentGamePath))
            {
                StatusInfoBar.Severity = InfoBarSeverity.Warning;
                StatusInfoBar.Title = "警告";
                StatusInfoBar.Message = "请先选择游戏路径";
                StatusInfoBar.IsOpen = true;
                return;
            }

            // 导航到安装页面
            Frame.Navigate(typeof(InstallPage), _currentGamePath);
        }

        private void OpenGameFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentGamePath))
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
                    FileName = _currentGamePath,
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
            if (string.IsNullOrEmpty(_currentGamePath))
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
            if (string.IsNullOrEmpty(_currentGamePath))
            {
                return;
            }

            var status = GameDetectionService.DetectInstallationStatus(_currentGamePath);

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
