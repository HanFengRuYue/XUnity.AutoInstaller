using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XUnity.AutoInstaller.Pages;

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

            // Enable custom title bar
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            // 设置窗口初始大小
            this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1200, 800));

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
    }
}
