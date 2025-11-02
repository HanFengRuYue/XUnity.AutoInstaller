using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XUnity_AutoInstaller.Models;
using XUnity_AutoInstaller.Services;

namespace XUnity_AutoInstaller.Pages
{
    public sealed partial class FontDownloadPage : Page
    {
        private readonly GameStateService _gameStateService;
        private readonly FontManagementService _fontManagementService;
        private List<FontResourceInfo> _availableFonts = new();
        private string? _detectedUnityVersion;

        public FontDownloadPage()
        {
            this.InitializeComponent();
            _gameStateService = GameStateService.Instance;
            _fontManagementService = new FontManagementService();

            // Subscribe to game path changes
            _gameStateService.GamePathChanged += OnGamePathChanged;

            // Use Loaded event pattern to safely access XAML controls
            this.Loaded += OnPageLoaded;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            // Safe to access XAML controls now
            this.Loaded -= OnPageLoaded;

            // Initialize UI
            UpdateGameInfo();

            // Load fonts
            _ = LoadFontsAsync();
        }

        private void OnGamePathChanged(object? sender, string newPath)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                UpdateGameInfo();
                _ = LoadFontsAsync();
            });
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // UI will be updated in OnPageLoaded
        }

        /// <summary>
        /// 更新游戏信息显示
        /// </summary>
        private void UpdateGameInfo()
        {
            if (GamePathTextBlock == null || UnityVersionTextBlock == null)
                return;

            var gamePath = _gameStateService.CurrentGamePath;

            if (string.IsNullOrEmpty(gamePath))
            {
                GamePathTextBlock.Text = "未选择游戏（请在仪表盘页面选择游戏目录）";
                UnityVersionTextBlock.Text = "N/A";
                return;
            }

            GamePathTextBlock.Text = gamePath;
            UnityVersionTextBlock.Text = "检测中...";
        }

        /// <summary>
        /// 加载字体列表
        /// </summary>
        private async Task LoadFontsAsync()
        {
            try
            {
                // Show loading state
                if (LoadingPanel != null && FontListView != null && EmptyFontsPanel != null && ErrorPanel != null)
                {
                    LoadingPanel.Visibility = Visibility.Visible;
                    FontListView.Visibility = Visibility.Collapsed;
                    EmptyFontsPanel.Visibility = Visibility.Collapsed;
                    ErrorPanel.Visibility = Visibility.Collapsed;
                }

                var gamePath = _gameStateService.CurrentGamePath;

                List<FontResourceInfo> fonts;

                if (string.IsNullOrEmpty(gamePath))
                {
                    // No game selected, just get all fonts without installation status
                    fonts = await _fontManagementService.GetAvailableFontsAsync();
                    _detectedUnityVersion = null;

                    if (UnityVersionTextBlock != null)
                    {
                        UnityVersionTextBlock.Text = "N/A";
                    }
                }
                else
                {
                    // Get recommended fonts based on Unity version
                    fonts = await _fontManagementService.GetRecommendedFontsAsync(gamePath);

                    // Update Unity version display
                    if (fonts.Any())
                    {
                        // Try to get detected version from service
                        var detector = new UnityVersionDetector();
                        _detectedUnityVersion = await detector.DetectUnityVersionAsync(gamePath);

                        if (UnityVersionTextBlock != null)
                        {
                            UnityVersionTextBlock.Text = _detectedUnityVersion ?? "未知";
                        }
                    }
                }

                _availableFonts = fonts;

                // Update UI
                if (FontListView != null && LoadingPanel != null && EmptyFontsPanel != null)
                {
                    if (_availableFonts.Count > 0)
                    {
                        FontListView.ItemsSource = _availableFonts;
                        FontListView.Visibility = Visibility.Visible;
                        LoadingPanel.Visibility = Visibility.Collapsed;
                        EmptyFontsPanel.Visibility = Visibility.Collapsed;

                        LogService.Instance.Log($"加载 {_availableFonts.Count} 个字体", LogLevel.Info, "[FontDownload]");
                    }
                    else
                    {
                        EmptyFontsPanel.Visibility = Visibility.Visible;
                        LoadingPanel.Visibility = Visibility.Collapsed;
                        FontListView.Visibility = Visibility.Collapsed;

                        LogService.Instance.Log("未找到可用字体", LogLevel.Warning, "[FontDownload]");
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.Log($"加载字体列表失败: {ex.Message}", LogLevel.Error, "[FontDownload]");

                // Show error state
                if (ErrorPanel != null && ErrorTextBlock != null && LoadingPanel != null && FontListView != null)
                {
                    ErrorPanel.Visibility = Visibility.Visible;
                    ErrorTextBlock.Text = $"加载失败: {ex.Message}";
                    LoadingPanel.Visibility = Visibility.Collapsed;
                    FontListView.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// 刷新按钮点击事件
        /// </summary>
        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadFontsAsync();
        }

        /// <summary>
        /// 下载按钮点击事件
        /// </summary>
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
                    Text = $"正在下载: {fontInfo.DisplayName}",
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

                // Create progress reporter
                var progress = new Progress<int>(percentage =>
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        if (progressBar != null && progressText != null)
                        {
                            progressBar.Value = percentage;
                            progressText.Text = $"正在下载: {fontInfo.DisplayName} ({percentage}%)";
                        }
                    });
                });

                // Download font
                await _fontManagementService.DownloadFontAsync(fontInfo, progress);

                // Close dialog
                progressDialog?.Hide();

                // Show success message
                ShowInfoBar("下载成功", $"字体 {fontInfo.DisplayName} 已下载到缓存", InfoBarSeverity.Success);

                // Refresh list to update button states
                await LoadFontsAsync();
            }
            catch (Exception ex)
            {
                // Close dialog
                progressDialog?.Hide();

                LogService.Instance.Log($"字体下载失败: {ex.Message}", LogLevel.Error, "[FontDownload]");
                ShowInfoBar("下载失败", ex.Message, InfoBarSeverity.Error);
            }
        }

        /// <summary>
        /// 安装按钮点击事件
        /// </summary>
        private async void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not FontResourceInfo fontInfo)
                return;

            var gamePath = _gameStateService.CurrentGamePath;

            if (string.IsNullOrEmpty(gamePath))
            {
                ShowInfoBar("未选择游戏", "请先在仪表盘页面选择游戏目录", InfoBarSeverity.Warning);
                return;
            }

            try
            {
                // Install font to game
                await _fontManagementService.InstallFontToGameAsync(gamePath, fontInfo);

                // Show success message with config path
                ShowInfoBar(
                    "安装成功",
                    $"字体已安装到游戏。请在配置编辑页面设置字体路径: {fontInfo.ConfigPath}",
                    InfoBarSeverity.Success);

                // Refresh list to update button states
                await LoadFontsAsync();
            }
            catch (Exception ex)
            {
                LogService.Instance.Log($"字体安装失败: {ex.Message}", LogLevel.Error, "[FontDownload]");
                ShowInfoBar("安装失败", ex.Message, InfoBarSeverity.Error);
            }
        }

        /// <summary>
        /// 显示信息栏
        /// </summary>
        private void ShowInfoBar(string title, string message, InfoBarSeverity severity)
        {
            DispatcherQueue.TryEnqueue(async () =>
            {
                if (this.XamlRoot != null)
                {
                    var dialog = new ContentDialog
                    {
                        Title = title,
                        Content = message,
                        CloseButtonText = "确定",
                        XamlRoot = this.XamlRoot
                    };

                    await dialog.ShowAsync();
                }
            });
        }
    }
}
