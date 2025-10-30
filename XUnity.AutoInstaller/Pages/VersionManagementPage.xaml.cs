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
        private string? _gamePath;
        private readonly VersionService _versionService;
        private List<VersionInfo> _allAvailableVersions = new();
        private List<InstalledVersionInfo> _installedVersions = new();
        private List<SnapshotInfo> _snapshots = new();

        public VersionManagementPage()
        {
            this.InitializeComponent();
            _versionService = new VersionService();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is string gamePath)
            {
                _gamePath = gamePath;
                _ = LoadDataAsync();
            }
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
            if (string.IsNullOrEmpty(_gamePath))
            {
                return;
            }

            try
            {
                _installedVersions = _versionService.GetInstalledVersions(_gamePath);
                _snapshots = _versionService.GetSnapshots(_gamePath);
                UpdateInstalledVersionsUI();
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"刷新已安装版本失败: {ex.Message}");
            }
        }

        private async Task RefreshAvailableVersions()
        {
            try
            {
                LoadingPanel.Visibility = Visibility.Visible;
                AvailableVersionsListView.Visibility = Visibility.Collapsed;

                _allAvailableVersions = await _versionService.GetAllAvailableVersionsAsync();
                ApplyFilters();

                LoadingPanel.Visibility = Visibility.Collapsed;
                AvailableVersionsListView.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                LoadingPanel.Visibility = Visibility.Collapsed;
                await ShowErrorAsync($"刷新可用版本失败: {ex.Message}");
            }
        }

        private void UpdateInstalledVersionsUI()
        {
            InstalledVersionsListView.Items.Clear();

            // 显示当前安装
            if (_installedVersions.Count > 0)
            {
                InstalledVersionsListView.Items.Add("=== 当前安装 ===");
                foreach (var version in _installedVersions)
                {
                    InstalledVersionsListView.Items.Add($"[活动] {version.PackageType} {version.Version} - {version.Platform}");
                }
            }

            // 显示快照
            if (_snapshots.Count > 0)
            {
                InstalledVersionsListView.Items.Add("");
                InstalledVersionsListView.Items.Add("=== 版本快照 ===");
                foreach (var snapshot in _snapshots)
                {
                    var versionInfo = snapshot.BepInExVersion ?? "未知";
                    if (!string.IsNullOrEmpty(snapshot.XUnityVersion))
                    {
                        versionInfo += $" + XUnity {snapshot.XUnityVersion}";
                    }
                    InstalledVersionsListView.Items.Add($"{snapshot.Name} - {snapshot.CreatedAt:yyyy-MM-dd HH:mm} ({versionInfo})");
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
            var filtered = _allAvailableVersions.AsEnumerable();

            // 包类型筛选
            if (PackageFilterComboBox.SelectedIndex == 1)
            {
                filtered = filtered.Where(v => v.PackageType == PackageType.BepInEx);
            }
            else if (PackageFilterComboBox.SelectedIndex == 2)
            {
                filtered = filtered.Where(v => v.PackageType == PackageType.XUnity);
            }

            // 版本类型筛选
            if (VersionTypeComboBox.SelectedIndex == 1)
            {
                filtered = filtered.Where(v => !v.IsPrerelease);
            }
            else if (VersionTypeComboBox.SelectedIndex == 2)
            {
                filtered = filtered.Where(v => v.IsPrerelease);
            }

            UpdateAvailableVersionsUI(filtered.ToList());
        }

        private void UpdateAvailableVersionsUI(List<VersionInfo> versions)
        {
            AvailableVersionsListView.Items.Clear();

            foreach (var version in versions.Take(50)) // 限制显示数量
            {
                var sizeText = PathHelper.FormatFileSize(version.FileSize);
                var dateText = version.ReleaseDate.ToString("yyyy-MM-dd");
                var displayText = $"{version.Name} - {dateText} ({sizeText})";

                AvailableVersionsListView.Items.Add(displayText);
            }
        }

        private async void UninstallButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is InstalledVersionInfo version)
            {
                var dialog = new ContentDialog
                {
                    Title = "确认卸载",
                    Content = $"确定要卸载 {version.PackageType} {version.Version} 吗？",
                    PrimaryButtonText = "卸载",
                    CloseButtonText = "取消",
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary && !string.IsNullOrEmpty(_gamePath))
                {
                    try
                    {
                        _versionService.UninstallPackage(_gamePath, version.PackageType);
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
            if (string.IsNullOrEmpty(_gamePath))
            {
                return;
            }

            // 检查是否有安装
            if (_installedVersions.Count == 0)
            {
                await ShowErrorAsync("当前没有已安装的版本");
                return;
            }

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
                    var snapshotPath = await _versionService.CreateSnapshotAsync(_gamePath, snapshotName);
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
            if (string.IsNullOrEmpty(_gamePath))
            {
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
                    await _versionService.RestoreSnapshotAsync(_gamePath, snapshot.Path);
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
            if (string.IsNullOrEmpty(_gamePath))
            {
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

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag is VersionInfo version)
            {
                var dialog = new ContentDialog
                {
                    Title = "下载版本",
                    Content = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock { Text = $"正在下载: {version.Name}" },
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
                    await ShowSuccessAsync($"下载完成: {downloadPath}");
                }
                catch (Exception ex)
                {
                    dialog.Hide();
                    await ShowErrorAsync($"下载失败: {ex.Message}");
                }
            }
        }

        private void PackageFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_allAvailableVersions.Count > 0)
            {
                ApplyFilters();
            }
        }

        private void VersionTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_allAvailableVersions.Count > 0)
            {
                ApplyFilters();
            }
        }

        private async Task ShowErrorAsync(string message)
        {
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
            var dialog = new ContentDialog
            {
                Title = "成功",
                Content = message,
                CloseButtonText = "确定",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
