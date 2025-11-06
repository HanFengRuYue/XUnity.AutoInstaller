using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Threading.Tasks;
using XUnity_AutoInstaller.Utils;
using XUnity_AutoInstaller.ViewModels;

namespace XUnity_AutoInstaller.Pages
{
    public sealed partial class DashboardPage : Page
    {
        public DashboardViewModel ViewModel { get; }

        public DashboardPage()
        {
            ViewModel = App.Services.GetRequiredService<DashboardViewModel>();

            this.InitializeComponent();

            this.Loaded += OnPageLoaded;
            this.Unloaded += OnPageUnloaded;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            ViewModel.Initialize();
            this.Loaded -= OnPageLoaded;
        }

        private void OnPageUnloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.Cleanup();
            this.Unloaded -= OnPageUnloaded;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }

        // Event handlers that show dialogs or handle UI-specific logic
        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await ViewModel.BrowseGamePathCommand.ExecuteAsync(null);

                StatusInfoBar.Severity = InfoBarSeverity.Success;
                StatusInfoBar.Title = "成功";
                StatusInfoBar.Message = "游戏路径已设置";
                StatusInfoBar.IsOpen = true;
            }
            catch (InvalidOperationException ex)
            {
                StatusInfoBar.Severity = InfoBarSeverity.Warning;
                StatusInfoBar.Title = "警告";
                StatusInfoBar.Message = ex.Message;
                StatusInfoBar.IsOpen = true;
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
            try
            {
                await ViewModel.QuickInstallCommand.ExecuteAsync(null);

                StatusInfoBar.Severity = InfoBarSeverity.Success;
                StatusInfoBar.Title = "成功";
                StatusInfoBar.Message = "一键安装完成！您现在可以启动游戏并享受自动翻译功能。";
                StatusInfoBar.IsOpen = true;
            }
            catch (Exception ex)
            {
                StatusInfoBar.Severity = InfoBarSeverity.Error;
                StatusInfoBar.Title = "错误";
                StatusInfoBar.Message = $"一键安装失败: {ex.Message}";
                StatusInfoBar.IsOpen = true;
            }
        }

        private async void UninstallButton_Click(object sender, RoutedEventArgs e)
        {
            // Show confirmation dialog
            var confirmed = await DialogHelper.ShowConfirmAsync(
                this.XamlRoot,
                "确认卸载",
                "即将完全卸载 BepInEx 和 XUnity.AutoTranslator，包括所有配置文件。\n\n此操作不可撤销，是否继续？",
                "卸载",
                "取消"
            );

            if (!confirmed)
            {
                return;
            }

            try
            {
                await ViewModel.UninstallCommand.ExecuteAsync(null);

                StatusInfoBar.Severity = InfoBarSeverity.Success;
                StatusInfoBar.Title = "成功";
                StatusInfoBar.Message = "卸载完成！BepInEx 和 XUnity.AutoTranslator 已完全移除。";
                StatusInfoBar.IsOpen = true;
            }
            catch (InvalidOperationException ex)
            {
                StatusInfoBar.Severity = InfoBarSeverity.Warning;
                StatusInfoBar.Title = "警告";
                StatusInfoBar.Message = ex.Message;
                StatusInfoBar.IsOpen = true;
            }
            catch (Exception ex)
            {
                StatusInfoBar.Severity = InfoBarSeverity.Error;
                StatusInfoBar.Title = "错误";
                StatusInfoBar.Message = $"卸载失败: {ex.Message}";
                StatusInfoBar.IsOpen = true;
            }
        }

        private void OpenGameFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ViewModel.OpenGameFolderCommand.Execute(null);
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
            try
            {
                ViewModel.RefreshStatusCommand.Execute(null);

                StatusInfoBar.Severity = InfoBarSeverity.Success;
                StatusInfoBar.Title = "成功";
                StatusInfoBar.Message = "状态已刷新";
                StatusInfoBar.IsOpen = true;
            }
            catch (Exception ex)
            {
                StatusInfoBar.Severity = InfoBarSeverity.Warning;
                StatusInfoBar.Title = "警告";
                StatusInfoBar.Message = ex.Message;
                StatusInfoBar.IsOpen = true;
            }
        }
    }
}
