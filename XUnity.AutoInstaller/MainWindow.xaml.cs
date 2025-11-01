using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XUnity.AutoInstaller.Pages;
using XUnity.AutoInstaller.Services;

namespace XUnity.AutoInstaller
{
    /// <summary>
    /// 主窗口，包含导航框架
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Set window icon for taskbar and title bar
            string iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "ICON.ico");
            AppWindow.SetIcon(iconPath);

            // Enable custom title bar
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            // 设置窗口初始大小
            this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1200, 800));

            // 订阅安装状态事件
            var stateService = InstallationStateService.Instance;
            stateService.InstallationStarted += OnInstallationStarted;
            stateService.ProgressChanged += OnProgressChanged;
            stateService.InstallationCompleted += OnInstallationCompleted;

            // 默认导航到首页
            NavView.SelectedItem = DashboardNavItem;
            ContentFrame.Navigate(typeof(DashboardPage));
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItemContainer != null)
            {
                var selectedTag = args.SelectedItemContainer.Tag?.ToString();

                switch (selectedTag)
                {
                    case "Dashboard":
                        ContentFrame.Navigate(typeof(DashboardPage));
                        break;
                    case "Install":
                        ContentFrame.Navigate(typeof(InstallPage));
                        break;
                    case "Config":
                        ContentFrame.Navigate(typeof(ConfigPage));
                        break;
                    case "Version":
                        ContentFrame.Navigate(typeof(VersionManagementPage));
                        break;
                    case "Log":
                        ContentFrame.Navigate(typeof(LogPage));
                        break;
                    case "Settings":
                        ContentFrame.Navigate(typeof(SettingsPage));
                        break;
                }
            }
        }

        private void OnInstallationStarted(object? sender, EventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                GlobalProgressPanel.Visibility = Visibility.Visible;
                GlobalProgressBar.Value = 0;
                GlobalProgressText.Text = "正在安装...";
                GlobalProgressPercentage.Text = "0%";
            });
        }

        private void OnProgressChanged(object? sender, (int progress, string message) e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                GlobalProgressBar.Value = e.progress;
                GlobalProgressText.Text = e.message;
                GlobalProgressPercentage.Text = $"{e.progress}%";
            });
        }

        private void OnInstallationCompleted(object? sender, bool success)
        {
            DispatcherQueue.TryEnqueue(async () =>
            {
                GlobalProgressBar.Value = 100;
                GlobalProgressText.Text = success ? "安装完成！" : "安装失败";
                GlobalProgressPercentage.Text = "100%";

                // 2秒后隐藏进度条
                await Task.Delay(2000);
                GlobalProgressPanel.Visibility = Visibility.Collapsed;
            });
        }
    }
}
