using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using XUnity_AutoInstaller.Models;
using XUnity_AutoInstaller.Services;
using XUnity_AutoInstaller.Utils;

namespace XUnity_AutoInstaller.Pages
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

            // 加载缓存信息
            LoadCacheInfo();

            // 加载设置文件信息
            LoadSettingsInfo();
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

        /// <summary>
        /// 加载缓存信息
        /// </summary>
        private void LoadCacheInfo()
        {
            try
            {
                var cachePath = PathHelper.GetTempDownloadDirectory();
                CachePathTextBlock.Text = cachePath;

                // 计算缓存大小（所有ZIP文件）
                if (Directory.Exists(cachePath))
                {
                    var cacheFiles = Directory.GetFiles(cachePath, "*.zip", SearchOption.TopDirectoryOnly);
                    long totalSize = cacheFiles.Sum(f => new FileInfo(f).Length);
                    CacheSizeTextBlock.Text = $"{FormatFileSize(totalSize)} ({cacheFiles.Length} 个文件)";
                }
                else
                {
                    CacheSizeTextBlock.Text = "0 B (0 个文件)";
                }
            }
            catch (Exception ex)
            {
                CachePathTextBlock.Text = "无法访问";
                CacheSizeTextBlock.Text = $"错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 加载设置文件信息
        /// </summary>
        private void LoadSettingsInfo()
        {
            SettingsPathTextBlock.Text = SettingsService.GetSettingsPath();
        }

        /// <summary>
        /// 格式化文件大小
        /// </summary>
        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            else if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:F2} KB";
            else if (bytes < 1024 * 1024 * 1024)
                return $"{bytes / (1024.0 * 1024.0):F2} MB";
            else
                return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }

        /// <summary>
        /// 打开缓存文件夹
        /// </summary>
        private void OpenCacheFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var cachePath = PathHelper.GetTempDownloadDirectory();
                Process.Start(new ProcessStartInfo
                {
                    FileName = cachePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                LogService.Instance.Log($"打开缓存文件夹失败: {ex.Message}", LogLevel.Error, "[Settings]");
            }
        }

        /// <summary>
        /// 清理缓存
        /// </summary>
        private async void CleanCacheButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.XamlRoot == null) return;

            // 确认对话框
            var confirmDialog = new ContentDialog
            {
                Title = "确认清理",
                Content = "确定要清理所有下载缓存吗？\n这将删除所有已下载的压缩包。",
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                XamlRoot = this.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    var cachePath = PathHelper.GetTempDownloadDirectory();
                    if (Directory.Exists(cachePath))
                    {
                        var cacheFiles = Directory.GetFiles(cachePath, "*.zip", SearchOption.TopDirectoryOnly);

                        int deletedCount = 0;
                        foreach (var file in cacheFiles)
                        {
                            try
                            {
                                File.Delete(file);
                                deletedCount++;
                            }
                            catch { }
                        }

                        LogService.Instance.Log($"缓存清理完成，删除了 {deletedCount} 个文件", LogLevel.Info, "[Settings]");

                        // 刷新缓存信息显示
                        LoadCacheInfo();

                        // 显示成功消息
                        var successDialog = new ContentDialog
                        {
                            Title = "成功",
                            Content = $"已清理 {deletedCount} 个缓存文件",
                            CloseButtonText = "确定",
                            XamlRoot = this.XamlRoot
                        };
                        await successDialog.ShowAsync();
                    }
                }
                catch (Exception ex)
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "错误",
                        Content = $"清理缓存失败: {ex.Message}",
                        CloseButtonText = "确定",
                        XamlRoot = this.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                }
            }
        }

        /// <summary>
        /// 打开配置文件夹
        /// </summary>
        private void OpenSettingsFolderButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settingsPath = SettingsService.GetAppDataPath();
                if (!Directory.Exists(settingsPath))
                {
                    Directory.CreateDirectory(settingsPath);
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = settingsPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                LogService.Instance.Log($"打开配置文件夹失败: {ex.Message}", LogLevel.Error, "[Settings]");
            }
        }

        /// <summary>
        /// 重置所有数据
        /// </summary>
        private async void ResetAllDataButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.XamlRoot == null) return;

            // 确认对话框
            var confirmDialog = new ContentDialog
            {
                Title = "⚠️ 警告",
                Content = "即将删除所有应用数据，包括：\n\n• 所有设置\n• 记住的游戏路径\n\n此操作不可撤销，确定继续吗？",
                PrimaryButtonText = "确定删除",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    var settingsPath = SettingsService.GetAppDataPath();

                    // 删除整个配置目录
                    if (Directory.Exists(settingsPath))
                    {
                        Directory.Delete(settingsPath, recursive: true);
                    }

                    LogService.Instance.Log("所有应用数据已重置", LogLevel.Info, "[Settings]");

                    // 重新创建并加载默认设置
                    _currentSettings = new AppSettings();
                    LoadSettingsToUI();
                    LoadCacheInfo();
                    LoadSettingsInfo();

                    // 显示成功消息
                    var successDialog = new ContentDialog
                    {
                        Title = "成功",
                        Content = "所有数据已重置为默认值\n\n请点击\"保存设置\"以应用更改",
                        CloseButtonText = "确定",
                        XamlRoot = this.XamlRoot
                    };
                    await successDialog.ShowAsync();
                }
                catch (Exception ex)
                {
                    var errorDialog = new ContentDialog
                    {
                        Title = "错误",
                        Content = $"重置数据失败: {ex.Message}",
                        CloseButtonText = "确定",
                        XamlRoot = this.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                }
            }
        }
    }
}
