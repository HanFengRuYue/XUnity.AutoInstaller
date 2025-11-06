using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using XUnity_AutoInstaller.Utils;
using XUnity_AutoInstaller.ViewModels;

namespace XUnity_AutoInstaller.Pages
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsViewModel ViewModel { get; }

        public SettingsPage()
        {
            ViewModel = App.Services.GetRequiredService<SettingsViewModel>();

            this.InitializeComponent();
        }

        private async void OpenCacheFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ViewModel.OpenCacheFolderCommand.Execute(null);
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowErrorAsync(this.XamlRoot, "错误", $"打开缓存文件夹失败: {ex.Message}");
            }
        }

        private async void CleanCacheButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.XamlRoot == null) return;

            var confirmed = await DialogHelper.ShowConfirmAsync(
                this.XamlRoot,
                "确认清理",
                "确定要清理所有下载缓存吗？\n这将删除所有已下载的压缩包。",
                "确定",
                "取消"
            );

            if (confirmed)
            {
                try
                {
                    await ViewModel.CleanCacheCommand.ExecuteAsync(null);
                }
                catch (InvalidOperationException ex)
                {
                    await DialogHelper.ShowSuccessAsync(this.XamlRoot, "成功", ex.Message);
                }
                catch (Exception ex)
                {
                    await DialogHelper.ShowErrorAsync(this.XamlRoot, "错误", $"清理缓存失败: {ex.Message}");
                }
            }
        }

        private async void OpenSettingsFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ViewModel.OpenSettingsFolderCommand.Execute(null);
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowErrorAsync(this.XamlRoot, "错误", $"打开配置文件夹失败: {ex.Message}");
            }
        }

        private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.XamlRoot == null) return;

            try
            {
                await ViewModel.TestConnectionCommand.ExecuteAsync(null);
            }
            catch (InvalidOperationException ex)
            {
                await DialogHelper.ShowErrorAsync(this.XamlRoot, "连接失败", ex.Message);
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowErrorAsync(this.XamlRoot, "测试失败", $"连接测试失败:\n{ex.Message}");
            }
        }

        private async void ResetAllDataButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.XamlRoot == null) return;

            var confirmed = await DialogHelper.ShowConfirmAsync(
                this.XamlRoot,
                "⚠️ 警告",
                "即将删除所有应用数据，包括：\n\n• 所有设置\n• 记住的游戏路径\n\n此操作不可撤销，确定继续吗？",
                "确定删除",
                "取消"
            );

            if (confirmed)
            {
                try
                {
                    await ViewModel.ResetAllDataCommand.ExecuteAsync(null);
                }
                catch (InvalidOperationException ex) when (ex.Message == "RESET_COMPLETE")
                {
                    var restartDialog = new ContentDialog
                    {
                        Title = "数据已重置",
                        Content = "所有数据已重置为默认值\n\n程序需要重启以彻底清除所有数据\n\n是否立即重启程序？",
                        PrimaryButtonText = "立即重启",
                        CloseButtonText = "稍后重启",
                        DefaultButton = ContentDialogButton.Primary,
                        XamlRoot = this.XamlRoot
                    };

                    var restartResult = await restartDialog.ShowAsync();
                    if (restartResult == ContentDialogResult.Primary)
                    {
                        var exePath = Process.GetCurrentProcess().MainModule?.FileName ??
                                     System.Reflection.Assembly.GetExecutingAssembly().Location;
                        Process.Start(exePath);
                        Process.GetCurrentProcess().Kill();
                    }
                }
                catch (Exception ex)
                {
                    await DialogHelper.ShowErrorAsync(this.XamlRoot, "错误", $"重置数据失败: {ex.Message}");
                }
            }
        }
    }
}
