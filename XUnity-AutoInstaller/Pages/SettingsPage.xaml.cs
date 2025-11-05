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
        private bool _isInitializing = true;

        public SettingsPage()
        {
            this.InitializeComponent();

            _settingsService = new SettingsService();
            _currentSettings = _settingsService.LoadSettings();

            LoadSettingsToUI();

            // 标记初始化完成
            _isInitializing = false;

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

            // 下载源设置（需要在Loaded事件后才能访问，使用IsLoaded检查）
            if (DownloadSourceRadioButtons != null)
            {
                DownloadSourceRadioButtons.SelectedIndex = _currentSettings.DownloadSource == DownloadSourceType.Mirror ? 1 : 0;
            }
        }

        /// <summary>
        /// 从UI保存设置
        /// </summary>
        private void SaveSettingsFromUI()
        {
            try
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

                // 下载源设置
                if (DownloadSourceRadioButtons != null)
                {
                    _currentSettings.DownloadSource = DownloadSourceRadioButtons.SelectedIndex == 1
                        ? DownloadSourceType.Mirror
                        : DownloadSourceType.GitHub;
                }

                // 保存到本地存储
                _settingsService.SaveSettings(_currentSettings);
            }
            catch (Exception ex)
            {
                LogService.Instance.Log($"自动保存设置失败: {ex.Message}", LogLevel.Error, "[Settings]");
            }
        }

        private void ThemeRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded || _isInitializing)
            {
                return;
            }

            var newTheme = ThemeRadioButtons.SelectedIndex switch
            {
                1 => ElementTheme.Light,
                2 => ElementTheme.Dark,
                _ => ElementTheme.Default
            };

            // 更新设置并自动保存
            _currentSettings.Theme = newTheme;
            SaveSettingsFromUI();

            // 立即应用主题
            SettingsService.ApplyTheme(newTheme);
        }

        private void SettingChanged(object sender, RoutedEventArgs e)
        {
            if (_isInitializing)
                return;

            // 自动保存设置
            SaveSettingsFromUI();
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

        private void DownloadSourceRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!this.IsLoaded || DownloadSourceRadioButtons == null || _isInitializing)
            {
                return;
            }

            // 自动保存下载源设置
            SaveSettingsFromUI();

            var sourceName = DownloadSourceRadioButtons.SelectedIndex == 1 ? "镜像网站" : "GitHub 官方";
            LogService.Instance.Log($"下载源已切换为: {sourceName}", LogLevel.Info, "[Settings]");
            ConnectionStatusTextBlock.Text = "未测试（请点击测试连接按钮）";
        }

        /// <summary>
        /// 测试连接
        /// </summary>
        private async void TestConnectionButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.XamlRoot == null || DownloadSourceRadioButtons == null) return;

            try
            {
                ConnectionStatusTextBlock.Text = "正在测试连接...";

                // 根据选择的源创建客户端并测试
                IVersionFetcher client = DownloadSourceRadioButtons.SelectedIndex == 1
                    ? new WebDAVMirrorClient()
                    : new GitHubAtomFeedClient();

                bool isConnected = await client.ValidateConnectionAsync();

                if (isConnected)
                {
                    ConnectionStatusTextBlock.Text = $"✓ 连接成功 ({client.SourceName})";
                    LogService.Instance.Log($"连接测试成功: {client.SourceName}", LogLevel.Info, "[Settings]");
                }
                else
                {
                    ConnectionStatusTextBlock.Text = $"✗ 连接失败 ({client.SourceName})";
                    LogService.Instance.Log($"连接测试失败: {client.SourceName}", LogLevel.Warning, "[Settings]");

                    var errorDialog = new ContentDialog
                    {
                        Title = "连接失败",
                        Content = $"无法连接到 {client.SourceName}\n\n请检查网络连接或尝试其他下载源。",
                        CloseButtonText = "确定",
                        XamlRoot = this.XamlRoot
                    };
                    await errorDialog.ShowAsync();
                }

                // 释放客户端资源
                if (client is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            catch (Exception ex)
            {
                ConnectionStatusTextBlock.Text = "✗ 测试出错";
                LogService.Instance.Log($"连接测试出错: {ex.Message}", LogLevel.Error, "[Settings]");

                var errorDialog = new ContentDialog
                {
                    Title = "测试失败",
                    Content = $"连接测试失败:\n{ex.Message}",
                    CloseButtonText = "确定",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }

        /// <summary>
        /// 重置所有数据
        /// </summary>
        private async void ResetAllDataButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.XamlRoot == null) return;

            // 确认对话框
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
                    var settingsPath = SettingsService.GetAppDataPath();

                    // 删除整个配置目录
                    if (Directory.Exists(settingsPath))
                    {
                        Directory.Delete(settingsPath, recursive: true);
                    }

                    LogService.Instance.Log("所有应用数据已重置", LogLevel.Info, "[Settings]");

                    // 重新创建并加载默认设置
                    _isInitializing = true;
                    _currentSettings = new AppSettings();
                    LoadSettingsToUI();
                    LoadCacheInfo();
                    LoadSettingsInfo();
                    _isInitializing = false;

                    // 应用默认主题
                    SettingsService.ApplyTheme(_currentSettings.Theme);

                    // 显示成功消息并要求重启
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
                        // 重启程序
                        var exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? 
                                     System.Reflection.Assembly.GetExecutingAssembly().Location;
                        System.Diagnostics.Process.Start(exePath);
                        System.Diagnostics.Process.GetCurrentProcess().Kill();
                    }
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
