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
        private List<FontResourceInfo> _allFonts = new();
        private List<FontResourceInfo> _filteredFonts = new();
        private string? _detectedUnityVersion;
        private string _currentSortColumn = ""; // "", "fontname", "unityversion", "filesize"
        private bool _currentSortAscending = true;

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
            if (UnityVersionRun == null)
                return;

            var gamePath = _gameStateService.CurrentGamePath;

            if (string.IsNullOrEmpty(gamePath))
            {
                UnityVersionRun.Text = "未选择游戏";
                return;
            }

            UnityVersionRun.Text = "检测中...";
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

                    if (UnityVersionRun != null)
                    {
                        UnityVersionRun.Text = "未选择游戏";
                    }
                }
                else
                {
                    // Get recommended fonts based on Unity version
                    fonts = await _fontManagementService.GetRecommendedFontsAsync(gamePath);

                    // Detect Unity version
                    var detector = new UnityVersionDetector();
                    _detectedUnityVersion = await detector.DetectUnityVersionAsync(gamePath);

                    if (UnityVersionRun != null)
                    {
                        UnityVersionRun.Text = UnityVersionDetector.FormatVersionForDisplay(_detectedUnityVersion) ?? "未知";
                    }

                    // Mark exact matches as recommended
                    if (!string.IsNullOrEmpty(_detectedUnityVersion))
                    {
                        foreach (var font in fonts)
                        {
                            font.IsRecommended = font.UnityVersion == _detectedUnityVersion;
                        }
                    }

                    // Auto-filter by Unity major version if game selected
                    if (!string.IsNullOrEmpty(_detectedUnityVersion) && UnityVersionFilterBox != null)
                    {
                        var majorVersion = _detectedUnityVersion.Split('-').FirstOrDefault();
                        if (!string.IsNullOrEmpty(majorVersion))
                        {
                            // Find matching ComboBox item
                            foreach (ComboBoxItem item in UnityVersionFilterBox.Items)
                            {
                                if (item.Tag?.ToString() == majorVersion)
                                {
                                    UnityVersionFilterBox.SelectedItem = item;
                                    break;
                                }
                            }
                        }
                    }
                }

                _allFonts = fonts;

                // Apply filters
                ApplyFilters();

                LogService.Instance.Log($"加载 {_allFonts.Count} 个字体", LogLevel.Info, "[FontDownload]");
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
        /// 应用筛选和排序
        /// </summary>
        private void ApplyFilters()
        {
            if (FontNameFilterBox == null || UnityVersionFilterBox == null)
                return;

            // Start with all fonts
            _filteredFonts = new List<FontResourceInfo>(_allFonts);

            // Apply font name filter
            var fontNameFilter = FontNameFilterBox.Text?.Trim().ToLower();
            if (!string.IsNullOrEmpty(fontNameFilter))
            {
                _filteredFonts = _filteredFonts.Where(f =>
                    f.FontName.ToLower().Contains(fontNameFilter)).ToList();
            }

            // Apply Unity version filter
            var selectedItem = UnityVersionFilterBox.SelectedItem as ComboBoxItem;
            var versionFilter = selectedItem?.Tag?.ToString();
            if (!string.IsNullOrEmpty(versionFilter))
            {
                _filteredFonts = _filteredFonts.Where(f =>
                    f.UnityMajorVersion.ToString() == versionFilter ||
                    (versionFilter == "6000" && f.UnityMajorVersion >= 6000)).ToList();
            }

            // Apply current sort
            ApplySort();

            // Update UI
            UpdateListView();
        }

        /// <summary>
        /// 应用当前排序
        /// </summary>
        private void ApplySort()
        {
            if (string.IsNullOrEmpty(_currentSortColumn))
                return;

            _filteredFonts = _currentSortColumn switch
            {
                "fontname" => _currentSortAscending
                    ? _filteredFonts.OrderBy(f => f.FontName).ToList()
                    : _filteredFonts.OrderByDescending(f => f.FontName).ToList(),
                "unityversion" => _currentSortAscending
                    ? _filteredFonts.OrderBy(f => f.UnityVersion).ToList()
                    : _filteredFonts.OrderByDescending(f => f.UnityVersion).ToList(),
                "filesize" => _currentSortAscending
                    ? _filteredFonts.OrderBy(f => f.FileSize).ToList()
                    : _filteredFonts.OrderByDescending(f => f.FileSize).ToList(),
                _ => _filteredFonts
            };
        }

        /// <summary>
        /// 更新列表视图
        /// </summary>
        private void UpdateListView()
        {
            if (FontListView == null || LoadingPanel == null || EmptyFontsPanel == null)
                return;

            if (_filteredFonts.Count > 0)
            {
                FontListView.ItemsSource = _filteredFonts;
                FontListView.Visibility = Visibility.Visible;
                LoadingPanel.Visibility = Visibility.Collapsed;
                EmptyFontsPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                EmptyFontsPanel.Visibility = Visibility.Visible;
                LoadingPanel.Visibility = Visibility.Collapsed;
                FontListView.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 更新排序图标
        /// </summary>
        private void UpdateSortIcons(string column)
        {
            if (FontNameSortIcon == null || UnityVersionSortIcon == null || FileSizeSortIcon == null)
                return;

            // Hide all icons
            FontNameSortIcon.Visibility = Visibility.Collapsed;
            UnityVersionSortIcon.Visibility = Visibility.Collapsed;
            FileSizeSortIcon.Visibility = Visibility.Collapsed;

            // Show active column icon
            FontIcon? activeIcon = column switch
            {
                "fontname" => FontNameSortIcon,
                "unityversion" => UnityVersionSortIcon,
                "filesize" => FileSizeSortIcon,
                _ => null
            };

            if (activeIcon != null)
            {
                activeIcon.Visibility = Visibility.Visible;
                activeIcon.Glyph = _currentSortAscending ? "\uE74A" : "\uE74B"; // Up/Down arrows
            }
        }

        /// <summary>
        /// 字体名称筛选框文本变化
        /// </summary>
        private void FilterBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        /// <summary>
        /// Unity 版本筛选框选择变化
        /// </summary>
        private void FilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        /// <summary>
        /// 清除筛选按钮
        /// </summary>
        private void ClearFilter_Click(object sender, RoutedEventArgs e)
        {
            if (FontNameFilterBox != null)
                FontNameFilterBox.Text = "";

            if (UnityVersionFilterBox != null)
                UnityVersionFilterBox.SelectedIndex = 0;

            ApplyFilters();
        }

        /// <summary>
        /// 按字体名称排序
        /// </summary>
        private void SortByFontName_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSortColumn == "fontname")
            {
                _currentSortAscending = !_currentSortAscending;
            }
            else
            {
                _currentSortColumn = "fontname";
                _currentSortAscending = true;
            }

            UpdateSortIcons("fontname");
            ApplyFilters();
        }

        /// <summary>
        /// 按 Unity 版本排序
        /// </summary>
        private void SortByUnityVersion_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSortColumn == "unityversion")
            {
                _currentSortAscending = !_currentSortAscending;
            }
            else
            {
                _currentSortColumn = "unityversion";
                _currentSortAscending = true;
            }

            UpdateSortIcons("unityversion");
            ApplyFilters();
        }

        /// <summary>
        /// 按文件大小排序
        /// </summary>
        private void SortByFileSize_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSortColumn == "filesize")
            {
                _currentSortAscending = !_currentSortAscending;
            }
            else
            {
                _currentSortColumn = "filesize";
                _currentSortAscending = true;
            }

            UpdateSortIcons("filesize");
            ApplyFilters();
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
                    Text = $"正在下载: {fontInfo.FontName}",
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
                            progressText.Text = $"正在下载: {fontInfo.FontName} ({percentage}%)";
                        }
                    });
                });

                // Download font
                await _fontManagementService.DownloadFontAsync(fontInfo, progress);

                // Close dialog
                progressDialog?.Hide();

                // Show success message
                ShowInfoBar("下载成功", $"字体 {fontInfo.FontName} (Unity {fontInfo.UnityVersionForDisplay}) 已下载到缓存", InfoBarSeverity.Success);

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

                LogService.Instance.Log($"字体安装成功: {fontInfo.FileName}", LogLevel.Info, "[FontDownload]");

                // Show config selection dialog
                var configDialog = new ContentDialog
                {
                    Title = "字体安装成功",
                    Content = "是否立即配置字体设置？",
                    PrimaryButtonText = "设置为 Override Font",
                    SecondaryButtonText = "设置为 Fallback Font",
                    CloseButtonText = "稍后配置",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };

                var result = await configDialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    // Set as Override Font
                    bool success = ConfigurationService.UpdateFontConfig(gamePath, fontInfo.ConfigPath, isOverride: true);

                    if (success)
                    {
                        ShowInfoBar("配置成功", $"已将 {fontInfo.FontName} 设置为 Override Font TextMeshPro", InfoBarSeverity.Success);
                    }
                    else
                    {
                        ShowInfoBar("配置失败", "更新配置文件时出错，请查看日志", InfoBarSeverity.Error);
                    }
                }
                else if (result == ContentDialogResult.Secondary)
                {
                    // Set as Fallback Font
                    bool success = ConfigurationService.UpdateFontConfig(gamePath, fontInfo.ConfigPath, isOverride: false);

                    if (success)
                    {
                        ShowInfoBar("配置成功", $"已将 {fontInfo.FontName} 设置为 Fallback Font TextMeshPro", InfoBarSeverity.Success);
                    }
                    else
                    {
                        ShowInfoBar("配置失败", "更新配置文件时出错，请查看日志", InfoBarSeverity.Error);
                    }
                }
                else
                {
                    // Configure Later
                    ShowInfoBar("稍后配置", $"字体路径: {fontInfo.ConfigPath}\n请在配置编辑页面手动设置", InfoBarSeverity.Informational);
                }

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
