using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Linq;
using System.Text;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Provider;
using XUnity_AutoInstaller.Services;

namespace XUnity_AutoInstaller.Pages
{
    public sealed partial class LogPage : Page
    {
        private readonly LogService _logService;
        private LogLevel? _currentFilter;

        public LogPage()
        {
            this.InitializeComponent();
            _logService = LogService.Instance;

            // Subscribe to log events
            _logService.LogEntryAdded += OnLogEntryAdded;

            // Wait for page to fully load before accessing XAML controls
            this.Loaded += OnPageLoaded;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            // Load existing logs after XAML is fully initialized
            RefreshLogDisplay();

            // Unsubscribe after first load
            this.Loaded -= OnPageLoaded;
        }

        private void OnLogEntryAdded(object? sender, LogEntry entry)
        {
            // Update UI on dispatcher queue
            DispatcherQueue.TryEnqueue(() =>
            {
                // Guard against XAML controls not being initialized yet
                if (LogTextBlock == null || AutoScrollCheckBox == null || LogScrollViewer == null)
                {
                    return;
                }

                // Check if entry matches current filter
                if (_currentFilter == null || entry.Level == _currentFilter)
                {
                    AppendLogEntry(entry);
                }

                UpdateLogCount();

                // Auto-scroll to bottom if enabled
                if (AutoScrollCheckBox.IsChecked == true)
                {
                    LogScrollViewer.ChangeView(null, LogScrollViewer.ScrollableHeight, null);
                }
            });
        }

        private void AppendLogEntry(LogEntry entry)
        {
            if (!string.IsNullOrEmpty(LogTextBlock.Text))
            {
                LogTextBlock.Text += Environment.NewLine;
            }
            LogTextBlock.Text += entry.FormattedMessage;
        }

        private void RefreshLogDisplay()
        {
            // Guard against XAML controls not being initialized yet
            if (LogTextBlock == null || AutoScrollCheckBox == null || LogScrollViewer == null)
            {
                return;
            }

            var logs = _logService.GetAllLogs();

            // Apply filter if set
            if (_currentFilter != null)
            {
                logs = logs.Where(log => log.Level == _currentFilter);
            }

            var sb = new StringBuilder();
            foreach (var log in logs)
            {
                if (sb.Length > 0)
                {
                    sb.AppendLine();
                }
                sb.Append(log.FormattedMessage);
            }

            LogTextBlock.Text = sb.ToString();
            UpdateLogCount();

            // Scroll to bottom
            if (AutoScrollCheckBox.IsChecked == true)
            {
                LogScrollViewer.ChangeView(null, LogScrollViewer.ScrollableHeight, null);
            }
        }

        private void UpdateLogCount()
        {
            // Guard against XAML controls not being initialized yet
            if (LogCountText == null)
            {
                return;
            }

            var totalCount = _logService.GetAllLogs().Count();
            LogCountText.Text = $"日志数: {totalCount}";
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            _logService.Clear();
            LogTextBlock.Text = string.Empty;
            UpdateLogCount();
        }

        private void LogLevelFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Guard against early event firing before page is loaded
            if (LogLevelFilterComboBox == null)
            {
                return;
            }

            var selectedIndex = LogLevelFilterComboBox.SelectedIndex;
            _currentFilter = selectedIndex switch
            {
                0 => null, // All
                1 => LogLevel.Debug,
                2 => LogLevel.Info,
                3 => LogLevel.Warning,
                4 => LogLevel.Error,
                _ => null
            };

            RefreshLogDisplay();
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Create a file picker
                var savePicker = new FileSavePicker();

                // Retrieve the window handle (HWND) of the current WinUI 3 window
                var window = App.MainWindow;
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

                // Initialize the file picker with the window handle (HWND)
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hWnd);

                // Set options for your file picker
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("文本文件", new System.Collections.Generic.List<string>() { ".txt" });
                savePicker.FileTypeChoices.Add("日志文件", new System.Collections.Generic.List<string>() { ".log" });
                savePicker.SuggestedFileName = $"XUnityInstaller_Log_{DateTime.Now:yyyyMMdd_HHmmss}";

                // Open the picker for the user to pick a file
                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    // Prevent updates to the remote version of the file until we finish making changes
                    CachedFileManager.DeferUpdates(file);

                    // Get logs with current filter applied
                    var logs = _logService.GetAllLogs();
                    if (_currentFilter != null)
                    {
                        logs = logs.Where(log => log.Level == _currentFilter);
                    }

                    // Build export content
                    var sb = new StringBuilder();
                    sb.AppendLine("=".PadRight(80, '='));
                    sb.AppendLine($"XUnity.AutoInstaller 运行日志");
                    sb.AppendLine($"导出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine($"日志级别: {(_currentFilter == null ? "全部" : _currentFilter.ToString())}");
                    sb.AppendLine($"日志条数: {logs.Count()}");
                    sb.AppendLine("=".PadRight(80, '='));
                    sb.AppendLine();

                    foreach (var log in logs)
                    {
                        sb.AppendLine(log.FormattedMessage);
                    }

                    // Write to file
                    using (var stream = await file.OpenStreamForWriteAsync())
                    {
                        using (var writer = new StreamWriter(stream, System.Text.Encoding.UTF8))
                        {
                            await writer.WriteAsync(sb.ToString());
                        }
                    }

                    // Let Windows know that we're finished changing the file
                    FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                    if (status == FileUpdateStatus.Complete)
                    {
                        LogService.Instance.Log($"日志已成功导出到: {file.Path}", LogLevel.Info, "[日志]");

                        // Show success message
                        if (this.XamlRoot != null)
                        {
                            var dialog = new ContentDialog
                            {
                                Title = "导出成功",
                                Content = $"日志已成功导出到:\n{file.Path}",
                                CloseButtonText = "确定",
                                XamlRoot = this.XamlRoot
                            };
                            await dialog.ShowAsync();
                        }
                    }
                    else
                    {
                        LogService.Instance.Log($"日志导出失败，文件状态: {status}", LogLevel.Warning, "[日志]");
                    }
                }
                else
                {
                    LogService.Instance.Log("日志导出已取消", LogLevel.Info, "[日志]");
                }
            }
            catch (Exception ex)
            {
                LogService.Instance.Log($"导出日志时发生错误: {ex.Message}", LogLevel.Error, "[日志]");

                // Show error message
                if (this.XamlRoot != null)
                {
                    var dialog = new ContentDialog
                    {
                        Title = "导出失败",
                        Content = $"导出日志时发生错误:\n{ex.Message}",
                        CloseButtonText = "确定",
                        XamlRoot = this.XamlRoot
                    };
                    await dialog.ShowAsync();
                }
            }
        }
    }
}
