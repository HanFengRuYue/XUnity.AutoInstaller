using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Threading.Tasks;
using XUnity_AutoInstaller.Models;
using XUnity_AutoInstaller.Utils;
using XUnity_AutoInstaller.ViewModels;

namespace XUnity_AutoInstaller.Pages
{
    public sealed partial class VersionManagementPage : Page
    {
        public VersionManagementViewModel ViewModel { get; }

        public VersionManagementPage()
        {
            // 从 DI 容器获取 ViewModel
            ViewModel = App.Services.GetRequiredService<VersionManagementViewModel>();

            this.InitializeComponent();

            this.Loaded += OnPageLoaded;
            this.Unloaded += OnPageUnloaded;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= OnPageLoaded;
        }

        private void OnPageUnloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.Cleanup();
            this.Unloaded -= OnPageUnloaded;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await ViewModel.InitializeAsync();
        }

        // 以下方法用于显示确认对话框（ViewModel 无法直接显示对话框）
        private async void UninstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedInstalledVersion == null) return;

            var displayName = $"{ViewModel.SelectedInstalledVersion.PackageType} {ViewModel.SelectedInstalledVersion.Version}";
            var confirmed = await DialogHelper.ShowConfirmAsync(
                this.XamlRoot,
                "确认卸载",
                $"确定要卸载 {displayName} 吗？\n\n这将删除 BepInEx 目录和所有相关文件。",
                "卸载",
                "取消"
            );

            if (confirmed)
            {
                await ViewModel.UninstallCommand.ExecuteAsync(null);
            }
        }

        private async void RestoreSnapshotButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedSnapshot == null) return;

            var confirmed = await DialogHelper.ShowConfirmAsync(
                this.XamlRoot,
                "确认还原",
                $"确定要还原快照 【{ViewModel.SelectedSnapshot.Name}】 吗？\n\n当前安装将被快照内容替换。",
                "还原",
                "取消"
            );

            if (confirmed)
            {
                await ViewModel.RestoreSnapshotCommand.ExecuteAsync(null);
            }
        }

        private async void DeleteSnapshotButton_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedSnapshot == null) return;

            var confirmed = await DialogHelper.ShowConfirmAsync(
                this.XamlRoot,
                "确认删除",
                $"确定要删除快照 【{ViewModel.SelectedSnapshot.Name}】 吗？\n\n此操作无法撤销。",
                "删除",
                "取消"
            );

            if (confirmed)
            {
                await ViewModel.DeleteSnapshotCommand.ExecuteAsync(null);
            }
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is AvailableVersionItem item)
            {
                await ViewModel.DownloadVersionCommand.ExecuteAsync(item);
            }
        }
    }
}
