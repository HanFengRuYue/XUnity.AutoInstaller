using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Threading.Tasks;
using XUnity_AutoInstaller.Services;
using XUnity_AutoInstaller.Utils;
using XUnity_AutoInstaller.ViewModels;

namespace XUnity_AutoInstaller.Pages
{
    public sealed partial class InstallPage : Page
    {
        public InstallViewModel ViewModel { get; }

        public InstallPage()
        {
            // 从 DI 容器获取 ViewModel
            ViewModel = App.Services.GetRequiredService<InstallViewModel>();

            this.InitializeComponent();

            // 设置默认值（WinUI3 RadioButtons 需要显式设置 SelectedIndex）
            VersionModeRadio.SelectedIndex = 0; // 自动推荐模式
            PlatformComboBox.SelectedIndex = 0; // x64

            this.Loaded += OnPageLoaded;
            this.Unloaded += OnPageUnloaded;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            // 页面加载完成后移除事件处理器
            this.Loaded -= OnPageLoaded;
        }

        private void OnPageUnloaded(object sender, RoutedEventArgs e)
        {
            // 清理事件订阅
            ViewModel.Cleanup();
            this.Unloaded -= OnPageUnloaded;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var gamePath = GameStateService.Instance.CurrentGamePath;
            await ViewModel.InitializeAsync(gamePath);
        }

        private async void StartInstallButton_Click(object sender, RoutedEventArgs e)
        {
            // 验证并显示对话框
            var gamePath = GameStateService.Instance.CurrentGamePath;
            if (string.IsNullOrEmpty(gamePath))
            {
                await ShowErrorAsync("游戏路径未设置，请先在首页选择游戏目录");
                return;
            }

            if (ViewModel.IsManualMode)
            {
                if (ViewModel.SelectedBepInExItem == null)
                {
                    await ShowErrorAsync("请选择 BepInEx 版本");
                    return;
                }
                if (ViewModel.SelectedXUnityItem == null)
                {
                    await ShowErrorAsync("请选择 XUnity 版本");
                    return;
                }
            }

            // 执行安装
            await ViewModel.StartInstallCommand.ExecuteAsync(null);

            // 根据结果显示对话框
            if (!ViewModel.IsInstalling)
            {
                // 安装完成，检查是否成功
                var stateService = InstallationStateService.Instance;
                // 简单检查：如果没有错误日志，认为成功
                if (ViewModel.InstallStatusMessage.Contains("成功") || !ViewModel.InstallStatusMessage.Contains("失败"))
                {
                    await ShowSuccessAsync("安装成功完成！");
                }
            }
        }

        private async Task ShowErrorAsync(string message)
        {
            if (this.XamlRoot == null)
            {
                DispatcherQueue.TryEnqueue(async () => await ShowErrorAsync(message));
                return;
            }

            await DialogHelper.ShowErrorAsync(this.XamlRoot, "错误", message);
        }

        private async Task ShowSuccessAsync(string message)
        {
            if (this.XamlRoot == null)
            {
                DispatcherQueue.TryEnqueue(async () => await ShowSuccessAsync(message));
                return;
            }

            await DialogHelper.ShowSuccessAsync(this.XamlRoot, "成功", message);
        }
    }
}
