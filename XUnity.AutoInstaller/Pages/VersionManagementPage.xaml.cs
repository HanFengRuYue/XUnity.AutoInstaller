using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XUnity.AutoInstaller.Models;
using XUnity.AutoInstaller.Services;
using XUnity.AutoInstaller.Utils;

namespace XUnity.AutoInstaller.Pages
{
    public sealed partial class VersionManagementPage : Page
    {
        private readonly GameStateService _gameStateService;
        private readonly VersionCacheService _versionCacheService;
        private readonly VersionService _versionService;
        private List<VersionInfo> _allBepInExVersions = new();
        private List<VersionInfo> _allXUnityVersions = new();
        private List<InstalledVersionInfo> _installedVersions = new();
        private List<SnapshotInfo> _snapshots = new();

        public VersionManagementPage()
        {
            this.InitializeComponent();
            _gameStateService = GameStateService.Instance;
            _versionCacheService = VersionCacheService.Instance;
            _versionService = new VersionService();

            // Subscribe to game path changes
            _gameStateService.GamePathChanged += OnGamePathChanged;

            // Subscribe to version updates
            _versionCacheService.VersionsUpdated += OnVersionsUpdated;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Load from GameStateService and cache
            if (_gameStateService.HasValidGamePath())
            {
                _ = LoadDataAsync();
            }

            // 从缓存加载版本列表（不触发 API 调用）
            LoadVersionsFromCache();
        }

        /// <summary>
        /// 从缓存加载版本列表（不触发 API 调用）
        /// </summary>
        private void LoadVersionsFromCache()
        {
            _allBepInExVersions = _versionCacheService.GetBepInExVersions();
            _allXUnityVersions = _versionCacheService.GetXUnityVersions();

            if (_allBepInExVersions.Count > 0 || _allXUnityVersions.Count > 0)
            {
                ApplyFilters();
                BepInExLoadingPanel.Visibility = Visibility.Collapsed;
                BepInExVersionsListView.Visibility = Visibility.Visible;
                XUnityLoadingPanel.Visibility = Visibility.Collapsed;
                XUnityVersionsListView.Visibility = Visibility.Visible;

                LogService.Instance.Log($"从缓存加载版本列表: BepInEx {_allBepInExVersions.Count} 个, XUnity {_allXUnityVersions.Count} 个", LogLevel.Info, "[版本管理]");
            }
            else
            {
                LogService.Instance.Log("缓存中没有版本数据，请点击刷新按钮获取", LogLevel.Info, "[版本管理]");
            }
        }

        private void OnGamePathChanged(object? sender, string? gamePath)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (!string.IsNullOrEmpty(gamePath))
                {
                    _ = LoadDataAsync();
                }
            });
        }

        private async Task LoadDataAsync()
        {
            await RefreshInstalledVersions();
            await RefreshAvailableVersions();
        }

        private async void RefreshInstalledButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshInstalledVersions();
        }

        private async void RefreshAvailableButton_Click(object sender, RoutedEventArgs e)
        {
            await RefreshAvailableVersions();
        }

        private async Task RefreshInstalledVersions()
        {
            var gamePath = _gameStateService.CurrentGamePath;
            if (string.IsNullOrEmpty(gamePath))
            {
                return;
            }

            try
            {
                _installedVersions = _versionService.GetInstalledVersions(gamePath);
                _snapshots = _versionService.GetSnapshots(gamePath);
                UpdateInstalledVersionsUI();
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"刷新已安装版本失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 版本更新事件处理
        /// </summary>
        private void OnVersionsUpdated(object? sender, VersionsUpdatedEventArgs e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                _allBepInExVersions = e.BepInExVersions;
                _allXUnityVersions = e.XUnityVersions;

                ApplyFilters();

                LogService.Instance.Log($"版本列表已更新: BepInEx {_allBepInExVersions.Count} 个, XUnity {_allXUnityVersions.Count} 个", LogLevel.Info, "[版本管理]");
            });
        }

        private async Task RefreshAvailableVersions()
        {
            try
            {
                BepInExLoadingPanel.Visibility = Visibility.Visible;
                BepInExVersionsListView.Visibility = Visibility.Collapsed;
                XUnityLoadingPanel.Visibility = Visibility.Visible;
                XUnityVersionsListView.Visibility = Visibility.Collapsed;

                LogService.Instance.Log("正在刷新版本列表（从 GitHub API）...", LogLevel.Info, "[版本管理]");

                // 调用 VersionCacheService 刷新（会触发 API 调用）
                await _versionCacheService.RefreshAsync();

                // 从缓存获取最新版本
                _allBepInExVersions = _versionCacheService.GetBepInExVersions();
                _allXUnityVersions = _versionCacheService.GetXUnityVersions();

                ApplyFilters();

                BepInExLoadingPanel.Visibility = Visibility.Collapsed;
                BepInExVersionsListView.Visibility = Visibility.Visible;
                XUnityLoadingPanel.Visibility = Visibility.Collapsed;
                XUnityVersionsListView.Visibility = Visibility.Visible;

                LogService.Instance.Log($"版本列表刷新完成: BepInEx {_allBepInExVersions.Count} 个, XUnity {_allXUnityVersions.Count} 个", LogLevel.Info, "[版本管理]");
            }
            catch (Exception ex)
            {
                BepInExLoadingPanel.Visibility = Visibility.Collapsed;
                XUnityLoadingPanel.Visibility = Visibility.Collapsed;
                LogService.Instance.Log($"刷新版本列表失败: {ex.Message}", LogLevel.Error, "[版本管理]");
                await ShowErrorAsync($"刷新可用版本失败: {ex.Message}");
            }
        }

        private void UpdateInstalledVersionsUI()
        {
            InstalledVersionsListView.Items.Clear();

            // 显示当前安装
            if (_installedVersions.Count > 0)
            {
                // 添加标题
                InstalledVersionsListView.Items.Add(new InstalledVersionDisplayItem
                {
                    ItemType = InstalledItemType.Header,
                    DisplayText = "当前安装",
                    ShowButtons = false
                });

                // 添加已安装的版本
                foreach (var version in _installedVersions)
                {
                    var installDate = version.InstallDate.ToString("yyyy-MM-dd");
                    InstalledVersionsListView.Items.Add(new InstalledVersionDisplayItem
                    {
                        ItemType = InstalledItemType.CurrentInstallation,
                        DisplayText = $"[{version.PackageType}] {version.Version}",
                        SubText1 = $"平台: {version.Platform}",
                        SubText2 = $"安装于: {installDate}",
                        ItemData = version,
                        ShowButtons = false
                    });
                }
            }

            // 显示快照
            if (_snapshots.Count > 0)
            {
                // 添加标题
                InstalledVersionsListView.Items.Add(new InstalledVersionDisplayItem
                {
                    ItemType = InstalledItemType.Header,
                    DisplayText = "版本快照",
                    ShowButtons = false
                });

                // 添加快照
                foreach (var snapshot in _snapshots)
                {
                    var bepinexInfo = snapshot.BepInExVersion ?? "未知";
                    var xunityInfo = snapshot.XUnityVersion ?? "未知";
                    InstalledVersionsListView.Items.Add(new InstalledVersionDisplayItem
                    {
                        ItemType = InstalledItemType.Snapshot,
                        DisplayText = $"[快照] {snapshot.Name}",
                        SubText1 = $"时间: {snapshot.CreatedAt:yyyy-MM-dd HH:mm}",
                        SubText2 = $"BepInEx: {bepinexInfo} | XUnity: {xunityInfo}",
                        ItemData = snapshot,
                        ShowButtons = true
                    });
                }
            }

            // 更新空状态
            if (_installedVersions.Count == 0 && _snapshots.Count == 0)
            {
                EmptyInstalledPanel.Visibility = Visibility.Visible;
                InstalledVersionsListView.Visibility = Visibility.Collapsed;
            }
            else
            {
                EmptyInstalledPanel.Visibility = Visibility.Collapsed;
                InstalledVersionsListView.Visibility = Visibility.Visible;
            }
        }

        private void ApplyFilters()
        {
            // Filter BepInEx versions
            var filteredBepInEx = _allBepInExVersions.AsEnumerable();

            // Platform filter (x86/x64) - ARM64 removed
            if (PlatformFilterComboBox.SelectedIndex == 1) // x86
            {
                filteredBepInEx = filteredBepInEx.Where(v => v.TargetPlatform == Platform.x86 || v.TargetPlatform == Platform.IL2CPP_x86);
            }
            else if (PlatformFilterComboBox.SelectedIndex == 2) // x64
            {
                filteredBepInEx = filteredBepInEx.Where(v => v.TargetPlatform == Platform.x64 || v.TargetPlatform == Platform.IL2CPP_x64);
            }

            // Architecture filter (Mono/IL2CPP)
            if (ArchitectureFilterComboBox.SelectedIndex == 1) // Mono
            {
                filteredBepInEx = filteredBepInEx.Where(v => v.TargetPlatform == Platform.x86 || v.TargetPlatform == Platform.x64);
            }
            else if (ArchitectureFilterComboBox.SelectedIndex == 2) // IL2CPP
            {
                filteredBepInEx = filteredBepInEx.Where(v => v.TargetPlatform == Platform.IL2CPP_x86 || v.TargetPlatform == Platform.IL2CPP_x64);
            }

            // Version type filter (Stable/Prerelease)
            if (VersionTypeComboBox.SelectedIndex == 1) // Stable
            {
                filteredBepInEx = filteredBepInEx.Where(v => !v.IsPrerelease);
            }
            else if (VersionTypeComboBox.SelectedIndex == 2) // Prerelease
            {
                filteredBepInEx = filteredBepInEx.Where(v => v.IsPrerelease);
            }

            // Filter XUnity versions (no platform filter needed)
            var filteredXUnity = _allXUnityVersions.AsEnumerable();

            if (VersionTypeComboBox.SelectedIndex == 1) // Stable
            {
                filteredXUnity = filteredXUnity.Where(v => !v.IsPrerelease);
            }
            else if (VersionTypeComboBox.SelectedIndex == 2) // Prerelease
            {
                filteredXUnity = filteredXUnity.Where(v => v.IsPrerelease);
            }

            UpdateBepInExVersionsUI(filteredBepInEx.ToList());
            UpdateXUnityVersionsUI(filteredXUnity.ToList());
        }

        private void UpdateBepInExVersionsUI(List<VersionInfo> versions)
        {
            BepInExVersionsListView.Items.Clear();

            if (versions.Count == 0)
            {
                BepInExVersionsListView.Items.Add(new { DisplayText = "未找到符合条件的版本", DownloadUrl = "", FileSize = 0L, Name = "", PackageType = PackageType.BepInEx, ReleaseDate = DateTime.Now, TargetPlatform = (Platform?)null, Version = "", IsPrerelease = false });
                return;
            }

            foreach (var version in versions.Take(50)) // Limit display count
            {
                var dateText = version.ReleaseDate.ToString("yyyy-MM-dd");
                var platformText = GetPlatformDisplayName(version.TargetPlatform);
                var displayText = $"{version.Version} - {platformText} - {dateText}";

                // 创建包含DisplayText和VersionInfo的对象
                var item = new VersionDisplayItem
                {
                    DisplayText = displayText,
                    Version = version
                };

                BepInExVersionsListView.Items.Add(item);
            }
        }

        /// <summary>
        /// 将 Platform 枚举转换为用户友好的显示名称
        /// </summary>
        private string GetPlatformDisplayName(Platform? platform)
        {
            return platform switch
            {
                Platform.x86 => "Mono x86",
                Platform.x64 => "Mono x64",
                Platform.IL2CPP_x86 => "IL2CPP x86",
                Platform.IL2CPP_x64 => "IL2CPP x64",
                Platform.ARM64 => "ARM64",
                null => "未知",
                _ => platform.ToString() ?? "未知"
            };
        }

        private void UpdateXUnityVersionsUI(List<VersionInfo> versions)
        {
            XUnityVersionsListView.Items.Clear();

            if (versions.Count == 0)
            {
                XUnityVersionsListView.Items.Add(new { DisplayText = "未找到符合条件的版本", DownloadUrl = "", FileSize = 0L, Name = "", PackageType = PackageType.XUnity, ReleaseDate = DateTime.Now, TargetPlatform = (Platform?)null, Version = "", IsPrerelease = false });
                return;
            }

            foreach (var version in versions.Take(50)) // Limit display count
            {
                var dateText = version.ReleaseDate.ToString("yyyy-MM-dd");
                var displayText = $"{version.Version} - {dateText}";

                // 创建包含DisplayText和VersionInfo的对象
                var item = new VersionDisplayItem
                {
                    DisplayText = displayText,
                    Version = version
                };

                XUnityVersionsListView.Items.Add(item);
            }
        }

        private async void UninstallButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.XamlRoot == null) return;

            var button = sender as Button;
            if (button?.Tag is InstalledVersionInfo version)
            {
                var gamePath = _gameStateService.CurrentGamePath;
                var dialog = new ContentDialog
                {
                    Title = "确认卸载",
                    Content = $"确定要卸载 {version.PackageType} {version.Version} 吗？",
                    PrimaryButtonText = "卸载",
                    CloseButtonText = "取消",
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary && !string.IsNullOrEmpty(gamePath))
                {
                    try
                    {
                        _versionService.UninstallPackage(gamePath, version.PackageType);
                        await RefreshInstalledVersions();
                        await ShowSuccessAsync("卸载成功");
                    }
                    catch (Exception ex)
                    {
                        await ShowErrorAsync($"卸载失败: {ex.Message}");
                    }
                }
            }
        }

        private async void SetActiveButton_Click(object sender, RoutedEventArgs e)
        {
            // 创建快照功能
            var gamePath = _gameStateService.CurrentGamePath;
            if (string.IsNullOrEmpty(gamePath))
            {
                await ShowErrorAsync("未设置游戏路径");
                return;
            }

            // 检查是否有安装
            if (_installedVersions.Count == 0)
            {
                await ShowErrorAsync("当前没有已安装的版本");
                return;
            }

            if (this.XamlRoot == null) return;

            // 询问快照名称
            var inputDialog = new ContentDialog
            {
                Title = "创建版本快照",
                Content = CreateSnapshotInputUI(out var nameTextBox),
                PrimaryButtonText = "创建",
                CloseButtonText = "取消",
                XamlRoot = this.XamlRoot
            };

            var result = await inputDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var snapshotName = nameTextBox.Text.Trim();
                if (string.IsNullOrEmpty(snapshotName))
                {
                    snapshotName = "Snapshot";
                }

                try
                {
                    var snapshotPath = await _versionService.CreateSnapshotAsync(gamePath, snapshotName);
                    await RefreshInstalledVersions();
                    await ShowSuccessAsync($"快照创建成功: {snapshotPath}");
                }
                catch (Exception ex)
                {
                    await ShowErrorAsync($"创建快照失败: {ex.Message}");
                }
            }
        }

        private StackPanel CreateSnapshotInputUI(out TextBox nameTextBox)
        {
            var textBox = new TextBox
            {
                PlaceholderText = "输入快照名称（可选）",
                Text = $"Backup_{DateTime.Now:yyyyMMdd}"
            };
            nameTextBox = textBox;

            return new StackPanel
            {
                Spacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        Text = "为当前安装创建一个版本快照，方便日后恢复。",
                        TextWrapping = TextWrapping.Wrap
                    },
                    new TextBlock
                    {
                        Text = "快照名称：",
                        FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
                    },
                    textBox
                }
            };
        }

        private async void RestoreSnapshotButton_Click(object sender, RoutedEventArgs e)
        {
            var gamePath = _gameStateService.CurrentGamePath;
            if (string.IsNullOrEmpty(gamePath))
            {
                await ShowErrorAsync("未设置游戏路径");
                return;
            }

            // 检查是否有快照
            if (_snapshots.Count == 0)
            {
                await ShowErrorAsync("没有可用的快照");
                return;
            }

            // 检查是否选择了快照
            if (InstalledVersionsListView.SelectedIndex < 0)
            {
                await ShowErrorAsync("请先选择要恢复的快照");
                return;
            }

            // 计算选择的是哪个快照（跳过标题行和当前安装）
            var selectedIndex = InstalledVersionsListView.SelectedIndex;
            var headerOffset = _installedVersions.Count > 0 ? _installedVersions.Count + 1 : 0; // "=== 当前安装 ===" + versions
            var snapshotHeaderOffset = _snapshots.Count > 0 ? 2 : 0; // empty line + "=== 版本快照 ==="

            var snapshotIndex = selectedIndex - headerOffset - snapshotHeaderOffset;
            if (snapshotIndex < 0 || snapshotIndex >= _snapshots.Count)
            {
                await ShowErrorAsync("请选择一个快照条目");
                return;
            }

            var snapshot = _snapshots[snapshotIndex];

            if (this.XamlRoot == null) return;

            // 确认对话框
            var confirmDialog = new ContentDialog
            {
                Title = "确认恢复快照",
                Content = $"确定要恢复快照 \"{snapshot.Name}\" 吗？\n\n这将替换当前安装的版本。",
                PrimaryButtonText = "恢复",
                CloseButtonText = "取消",
                XamlRoot = this.XamlRoot,
                DefaultButton = ContentDialogButton.Close
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    await _versionService.RestoreSnapshotAsync(gamePath, snapshot.Path);
                    await RefreshInstalledVersions();
                    await ShowSuccessAsync("快照恢复成功");
                }
                catch (Exception ex)
                {
                    await ShowErrorAsync($"恢复快照失败: {ex.Message}");
                }
            }
        }

        private async void DeleteSnapshotButton_Click(object sender, RoutedEventArgs e)
        {
            var gamePath = _gameStateService.CurrentGamePath;
            if (string.IsNullOrEmpty(gamePath))
            {
                await ShowErrorAsync("未设置游戏路径");
                return;
            }

            if (_snapshots.Count == 0)
            {
                await ShowErrorAsync("没有可用的快照");
                return;
            }

            if (InstalledVersionsListView.SelectedIndex < 0)
            {
                await ShowErrorAsync("请先选择要删除的快照");
                return;
            }

            // 计算选择的快照索引
            var selectedIndex = InstalledVersionsListView.SelectedIndex;
            var headerOffset = _installedVersions.Count > 0 ? _installedVersions.Count + 1 : 0;
            var snapshotHeaderOffset = _snapshots.Count > 0 ? 2 : 0;

            var snapshotIndex = selectedIndex - headerOffset - snapshotHeaderOffset;
            if (snapshotIndex < 0 || snapshotIndex >= _snapshots.Count)
            {
                await ShowErrorAsync("请选择一个快照条目");
                return;
            }

            var snapshot = _snapshots[snapshotIndex];

            if (this.XamlRoot == null) return;

            // 确认对话框
            var confirmDialog = new ContentDialog
            {
                Title = "确认删除快照",
                Content = $"确定要删除快照 \"{snapshot.Name}\" 吗？\n\n此操作无法撤销。",
                PrimaryButtonText = "删除",
                CloseButtonText = "取消",
                XamlRoot = this.XamlRoot,
                DefaultButton = ContentDialogButton.Close
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    _versionService.DeleteSnapshot(snapshot.Path);
                    await RefreshInstalledVersions();
                    await ShowSuccessAsync("快照已删除");
                }
                catch (Exception ex)
                {
                    await ShowErrorAsync($"删除快照失败: {ex.Message}");
                }
            }
        }

        private async void RestoreSnapshotItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.XamlRoot == null) return;

            var button = sender as Button;
            var displayItem = button?.Tag as InstalledVersionDisplayItem;
            if (displayItem?.ItemData is SnapshotInfo snapshot)
            {
                var gamePath = _gameStateService.CurrentGamePath;
                if (string.IsNullOrEmpty(gamePath))
                {
                    await ShowErrorAsync("未设置游戏路径");
                    return;
                }

                // 确认对话框
                var confirmDialog = new ContentDialog
                {
                    Title = "确认恢复快照",
                    Content = $"确定要恢复快照 \"{snapshot.Name}\" 吗？\n\n这将替换当前安装的版本。",
                    PrimaryButtonText = "恢复",
                    CloseButtonText = "取消",
                    XamlRoot = this.XamlRoot,
                    DefaultButton = ContentDialogButton.Close
                };

                var result = await confirmDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    try
                    {
                        await _versionService.RestoreSnapshotAsync(gamePath, snapshot.Path);
                        await RefreshInstalledVersions();
                        await ShowSuccessAsync("快照恢复成功");
                    }
                    catch (Exception ex)
                    {
                        await ShowErrorAsync($"恢复快照失败: {ex.Message}");
                    }
                }
            }
        }

        private async void DeleteSnapshotItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.XamlRoot == null) return;

            var button = sender as Button;
            var displayItem = button?.Tag as InstalledVersionDisplayItem;
            if (displayItem?.ItemData is SnapshotInfo snapshot)
            {
                // 确认对话框
                var confirmDialog = new ContentDialog
                {
                    Title = "确认删除快照",
                    Content = $"确定要删除快照 \"{snapshot.Name}\" 吗？\n\n此操作无法撤销。",
                    PrimaryButtonText = "删除",
                    CloseButtonText = "取消",
                    XamlRoot = this.XamlRoot,
                    DefaultButton = ContentDialogButton.Close
                };

                var result = await confirmDialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    try
                    {
                        _versionService.DeleteSnapshot(snapshot.Path);
                        await RefreshInstalledVersions();
                        await ShowSuccessAsync("快照已删除");
                    }
                    catch (Exception ex)
                    {
                        await ShowErrorAsync($"删除快照失败: {ex.Message}");
                    }
                }
            }
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.XamlRoot == null) return;

            var button = sender as Button;
            // 从VersionDisplayItem中提取VersionInfo
            var displayItem = button?.Tag as VersionDisplayItem;
            if (displayItem?.Version is VersionInfo version)
            {
                var dialog = new ContentDialog
                {
                    Title = "下载到缓存",
                    Content = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock { Text = $"正在下载: {version.Name}" },
                            new TextBlock { Text = "文件将被下载到本地缓存，不会自动安装", Margin = new Thickness(0, 5, 0, 0), Opacity = 0.8 },
                            new ProgressBar { IsIndeterminate = true, Margin = new Thickness(0, 10, 0, 0) }
                        }
                    },
                    XamlRoot = this.XamlRoot
                };

                _ = dialog.ShowAsync();

                try
                {
                    var progress = new Progress<int>();
                    var downloadPath = await _versionService.DownloadVersionAsync(version, progress);

                    dialog.Hide();
                    await ShowSuccessAsync($"下载完成！\n\n文件已保存到缓存:\n{downloadPath}\n\n您可以在安装页面中使用已下载的版本进行安装。");
                }
                catch (Exception ex)
                {
                    dialog.Hide();
                    await ShowErrorAsync($"下载失败: {ex.Message}");
                }
            }
        }

        private void PlatformFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_allBepInExVersions.Count > 0)
            {
                ApplyFilters();
            }
        }

        private void ArchitectureFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_allBepInExVersions.Count > 0)
            {
                ApplyFilters();
            }
        }

        private void VersionTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_allBepInExVersions.Count > 0 || _allXUnityVersions.Count > 0)
            {
                ApplyFilters();
            }
        }

        private async Task ShowErrorAsync(string message)
        {
            // 如果 XamlRoot 为 null,延迟到下一个 UI 周期
            if (this.XamlRoot == null)
            {
                DispatcherQueue.TryEnqueue(async () => await ShowErrorAsync(message));
                return;
            }

            var dialog = new ContentDialog
            {
                Title = "错误",
                Content = message,
                CloseButtonText = "确定",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private async Task ShowSuccessAsync(string message)
        {
            // 如果 XamlRoot 为 null,延迟到下一个 UI 周期
            if (this.XamlRoot == null)
            {
                DispatcherQueue.TryEnqueue(async () => await ShowSuccessAsync(message));
                return;
            }

            var dialog = new ContentDialog
            {
                Title = "成功",
                Content = message,
                CloseButtonText = "确定",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }

        /// <summary>
        /// 用于在ListView中显示版本信息的辅助类
        /// </summary>
        private class VersionDisplayItem
        {
            public string DisplayText { get; set; } = string.Empty;
            public VersionInfo Version { get; set; } = null!;
        }

        /// <summary>
        /// 已安装版本/快照显示项类型
        /// </summary>
        private enum InstalledItemType
        {
            Header,              // 标题行
            CurrentInstallation, // 当前安装的版本
            Snapshot             // 快照
        }

        /// <summary>
        /// 用于在已安装版本ListView中显示的辅助类
        /// </summary>
        private class InstalledVersionDisplayItem
        {
            public InstalledItemType ItemType { get; set; }
            public string DisplayText { get; set; } = string.Empty;
            public string SubText1 { get; set; } = string.Empty;
            public string SubText2 { get; set; } = string.Empty;
            public object? ItemData { get; set; }
            public bool ShowButtons { get; set; }

            // 用于XAML绑定的可见性属性
            public Visibility HeaderVisibility => ItemType == InstalledItemType.Header ? Visibility.Visible : Visibility.Collapsed;
            public Visibility ContentVisibility => ItemType != InstalledItemType.Header ? Visibility.Visible : Visibility.Collapsed;
            public Visibility ButtonsVisibility => ShowButtons ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
