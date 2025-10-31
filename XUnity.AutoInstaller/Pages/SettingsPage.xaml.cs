using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using XUnity.AutoInstaller.Models;
using XUnity.AutoInstaller.Services;

namespace XUnity.AutoInstaller.Pages
{
    public sealed partial class SettingsPage : Page
    {
        private readonly SettingsService _settingsService;
        private AppSettings _currentSettings;

        public SettingsPage()
        {
            this.InitializeComponent();

            _settingsService = new SettingsService();
            _currentSettings = _settingsService.LoadSettings();

            LoadSettingsToUI();

            // 显示应用版本
            VersionTextBlock.Text = $"版本 {SettingsService.GetAppVersion()}";
        }

        /// <summary>
        /// 加载设置到UI
        /// </summary>
        private void LoadSettingsToUI()
        {
            // 主题
            ThemeRadioButtons.SelectedIndex = _currentSettings.Theme switch
            {
                ElementTheme.Light => 1,
                ElementTheme.Dark => 2,
                _ => 0 // Default
            };

            // 常规设置
            RememberPathCheckBox.IsChecked = _currentSettings.RememberLastGamePath;
            ShowDetailedProgressCheckBox.IsChecked = _currentSettings.ShowDetailedProgress;

            // 默认安装选项
            DefaultBackupCheckBox.IsChecked = _currentSettings.DefaultBackupExisting;
            DefaultRecommendedConfigCheckBox.IsChecked = _currentSettings.DefaultUseRecommendedConfig;

            // GitHub Token
            GitHubTokenPasswordBox.Password = _currentSettings.GitHubToken ?? string.Empty;
        }

        /// <summary>
        /// 从UI保存设置
        /// </summary>
        private void SaveSettingsFromUI()
        {
            // 主题
            _currentSettings.Theme = ThemeRadioButtons.SelectedIndex switch
            {
                1 => ElementTheme.Light,
                2 => ElementTheme.Dark,
                _ => ElementTheme.Default
            };

            // 常规设置
            _currentSettings.RememberLastGamePath = RememberPathCheckBox.IsChecked == true;
            _currentSettings.ShowDetailedProgress = ShowDetailedProgressCheckBox.IsChecked == true;

            // 默认安装选项
            _currentSettings.DefaultBackupExisting = DefaultBackupCheckBox.IsChecked == true;
            _currentSettings.DefaultUseRecommendedConfig = DefaultRecommendedConfigCheckBox.IsChecked == true;

            // GitHub Token
            _currentSettings.GitHubToken = string.IsNullOrWhiteSpace(GitHubTokenPasswordBox.Password) ? null : GitHubTokenPasswordBox.Password.Trim();

            // 保存到本地存储
            _settingsService.SaveSettings(_currentSettings);
        }

        private void ThemeRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded)
            {
                return;
            }

            var newTheme = ThemeRadioButtons.SelectedIndex switch
            {
                1 => ElementTheme.Light,
                2 => ElementTheme.Dark,
                _ => ElementTheme.Default
            };

            // 立即应用主题
            SettingsService.ApplyTheme(newTheme);
        }

        private void SettingChanged(object sender, RoutedEventArgs e)
        {
            // 设置改变时可以添加提示，或自动保存
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.XamlRoot == null) return;

            try
            {
                SaveSettingsFromUI();

                // 显示成功消息
                var dialog = new ContentDialog
                {
                    Title = "成功",
                    Content = "设置已保存",
                    CloseButtonText = "确定",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();

                // 如果输入了Token，显示Token信息提示
                if (!string.IsNullOrWhiteSpace(GitHubTokenPasswordBox.Password))
                {
                    TokenInfoBar.IsOpen = true;
                }
            }
            catch (Exception ex)
            {
                // 显示错误消息
                var dialog = new ContentDialog
                {
                    Title = "错误",
                    Content = $"保存设置失败: {ex.Message}",
                    CloseButtonText = "确定",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.XamlRoot == null) return;

            // 确认对话框
            var confirmDialog = new ContentDialog
            {
                Title = "确认重置",
                Content = "确定要恢复所有设置为默认值吗？",
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                XamlRoot = this.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                // 重置为默认设置
                _currentSettings = new AppSettings();
                LoadSettingsToUI();

                // 应用默认主题
                SettingsService.ApplyTheme(_currentSettings.Theme);

                // 显示提示
                var successDialog = new ContentDialog
                {
                    Title = "成功",
                    Content = "设置已恢复为默认值\n\n请点击\"保存设置\"以应用更改",
                    CloseButtonText = "确定",
                    XamlRoot = this.XamlRoot
                };
                await successDialog.ShowAsync();
            }
        }
    }
}
