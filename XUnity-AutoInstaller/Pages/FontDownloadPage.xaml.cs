using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Threading.Tasks;
using XUnity_AutoInstaller.Models;
using XUnity_AutoInstaller.Services;
using XUnity_AutoInstaller.Utils;
using XUnity_AutoInstaller.ViewModels;

namespace XUnity_AutoInstaller.Pages
{
    public sealed partial class FontDownloadPage : Page
    {
        public FontDownloadViewModel ViewModel { get; }

        public FontDownloadPage()
        {
            ViewModel = App.Services.GetRequiredService<FontDownloadViewModel>();

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

        // Download button click - shows progress dialog
        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not FontResourceInfo fontInfo)
                return;

            ContentDialog? progressDialog = null;
            ProgressBar? progressBar = null;
            TextBlock? progressText = null;

            try
            {
                // Create download progress dialog
                progressBar = new ProgressBar
                {
                    Minimum = 0,
                    Maximum = 100,
                    Value = 0
                };

                progressText = new TextBlock
                {
                    Text = $"正在下载: {fontInfo.DisplayFontName}",
                    TextWrapping = TextWrapping.Wrap
                };

                var dialogContent = new StackPanel
                {
                    Spacing = 16
                };
                dialogContent.Children.Add(progressText);
                dialogContent.Children.Add(progressBar);

                progressDialog = new ContentDialog
                {
                    Title = "下载字体",
                    Content = dialogContent,
                    IsPrimaryButtonEnabled = false,
                    CloseButtonText = "取消",
                    XamlRoot = this.XamlRoot
                };

                // Show dialog without awaiting (non-blocking)
                _ = progressDialog.ShowAsync();

                // Update dialog every 100ms
                var updateTask = Task.Run(async () =>
                {
                    while (ViewModel.IsDownloading)
                    {
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            if (progressBar != null && progressText != null)
                            {
                                progressBar.Value = ViewModel.DownloadProgress;
                                progressText.Text = ViewModel.DownloadStatusMessage;
                            }
                        });
                        await Task.Delay(100);
                    }
                });

                // Execute download command
                await ViewModel.DownloadFontCommand.ExecuteAsync(fontInfo);

                // Wait for update task to finish
                await updateTask;

                // Close dialog
                progressDialog?.Hide();

                // Show success message
                await DialogHelper.ShowSuccessAsync(
                    this.XamlRoot,
                    "下载成功",
                    $"字体 {fontInfo.DisplayFontName} (Unity {fontInfo.UnityVersionForDisplay}) 已下载到缓存"
                );
            }
            catch (Exception ex)
            {
                // Close dialog
                progressDialog?.Hide();

                await DialogHelper.ShowErrorAsync(this.XamlRoot, "下载失败", ex.Message);
            }
        }

        // Install button click - shows config selection dialog
        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not FontResourceInfo fontInfo)
                return;

            try
            {
                // Execute install command
                await ViewModel.InstallFontCommand.ExecuteAsync(fontInfo);

                // Show config selection dialog
                var result = await DialogHelper.ShowDialogAsync(
                    this.XamlRoot,
                    "字体安装成功",
                    "是否立即配置字体设置？",
                    "设置为 Override Font",
                    "设置为 Fallback Font",
                    "稍后配置"
                );

                var gamePath = GameStateService.Instance.CurrentGamePath;

                if (result == ContentDialogResult.Primary)
                {
                    // Set as Override Font
                    bool success = ConfigurationService.UpdateFontConfig(gamePath!, fontInfo.ConfigPath, isOverride: true);

                    if (success)
                    {
                        await DialogHelper.ShowSuccessAsync(
                            this.XamlRoot,
                            "配置成功",
                            $"已将 {fontInfo.DisplayFontName} 设置为 Override Font TextMeshPro"
                        );
                    }
                    else
                    {
                        await DialogHelper.ShowErrorAsync(this.XamlRoot, "配置失败", "更新配置文件时出错，请查看日志");
                    }
                }
                else if (result == ContentDialogResult.Secondary)
                {
                    // Set as Fallback Font
                    bool success = ConfigurationService.UpdateFontConfig(gamePath!, fontInfo.ConfigPath, isOverride: false);

                    if (success)
                    {
                        await DialogHelper.ShowSuccessAsync(
                            this.XamlRoot,
                            "配置成功",
                            $"已将 {fontInfo.DisplayFontName} 设置为 Fallback Font TextMeshPro"
                        );
                    }
                    else
                    {
                        await DialogHelper.ShowErrorAsync(this.XamlRoot, "配置失败", "更新配置文件时出错，请查看日志");
                    }
                }
                else
                {
                    // Configure Later
                    await DialogHelper.ShowDialogAsync(
                        this.XamlRoot,
                        "稍后配置",
                        $"字体路径: {fontInfo.ConfigPath}\n请在配置编辑页面手动设置",
                        string.Empty,
                        string.Empty,
                        "确定"
                    );
                }
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowErrorAsync(this.XamlRoot, "安装失败", ex.Message);
            }
        }
    }
}
