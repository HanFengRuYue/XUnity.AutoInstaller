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

            // 设置窗口初始大小
            this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1200, 800));

            // 默认导航到首页
            NavView.SelectedItem = DashboardNavItem;
            ContentFrame.Navigate(typeof(DashboardPage));
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                // 导航到设置页面
                ContentFrame.Navigate(typeof(SettingsPage));
            }
            else if (args.SelectedItemContainer != null)
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
                }
            }
        }
    }
}
