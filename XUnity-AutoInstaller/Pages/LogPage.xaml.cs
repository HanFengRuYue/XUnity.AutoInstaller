using Microsoft.Extensions.DependencyInjection;
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
using XUnity_AutoInstaller.Utils;
using XUnity_AutoInstaller.ViewModels;

namespace XUnity_AutoInstaller.Pages
{
    public sealed partial class LogPage : Page
    {
        public LogViewModel ViewModel { get; }

        public LogPage()
        {
            ViewModel = App.Services.GetRequiredService<LogViewModel>();

            this.InitializeComponent();

            ViewModel.LogEntryAdded += OnLogEntryAdded;

            this.Loaded += OnPageLoaded;
            this.Unloaded += OnPageUnloaded;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            ViewModel.Initialize();
            this.Loaded -= OnPageLoaded;
        }

        private void OnPageUnloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.Cleanup();
            ViewModel.LogEntryAdded -= OnLogEntryAdded;
            this.Unloaded -= OnPageUnloaded;
        }

        private void OnLogEntryAdded(object? sender, LogEntry entry)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (LogTextBlock == null || LogScrollViewer == null)
                {
                    return;
                }

                if (!string.IsNullOrEmpty(LogTextBlock.Text))
                {
                    LogTextBlock.Text += Environment.NewLine;
                }
                LogTextBlock.Text += entry.FormattedMessage;

                if (ViewModel.AutoScroll)
                {
                    LogScrollViewer.ChangeView(null, LogScrollViewer.ScrollableHeight, null);
                }
            });
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ClearCommand.Execute(null);
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var savePicker = new FileSavePicker();

                var window = App.MainWindow;
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hWnd);

                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("文本文件", new System.Collections.Generic.List<string>() { ".txt" });
                savePicker.FileTypeChoices.Add("日志文件", new System.Collections.Generic.List<string>() { ".log" });
                savePicker.SuggestedFileName = $"XUnityInstaller_Log_{DateTime.Now:yyyyMMdd_HHmmss}";

                StorageFile file = await savePicker.PickSaveFileAsync();
                if (file != null)
                {
                    CachedFileManager.DeferUpdates(file);

                    var logs = ViewModel.GetFilteredLogs();

                    var sb = new StringBuilder();
                    sb.AppendLine("=".PadRight(80, '='));
                    sb.AppendLine($"XUnity.AutoInstaller 运行日志");
                    sb.AppendLine($"导出时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine($"日志级别: {ViewModel.GetFilterName()}");
                    sb.AppendLine($"日志条数: {logs.Count()}");
                    sb.AppendLine("=".PadRight(80, '='));
                    sb.AppendLine();

                    foreach (var log in logs)
                    {
                        sb.AppendLine(log.FormattedMessage);
                    }

                    using (var stream = await file.OpenStreamForWriteAsync())
                    {
                        using (var writer = new StreamWriter(stream, Encoding.UTF8))
                        {
                            await writer.WriteAsync(sb.ToString());
                        }
                    }

                    FileUpdateStatus status = await CachedFileManager.CompleteUpdatesAsync(file);
                    if (status == FileUpdateStatus.Complete)
                    {
                        LogService.Instance.Log($"日志已成功导出到: {file.Path}", LogLevel.Info, "[日志]");
                        await DialogHelper.ShowSuccessAsync(this.XamlRoot, "导出成功", $"日志已成功导出到:\n{file.Path}");
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
                await DialogHelper.ShowErrorAsync(this.XamlRoot, "导出失败", $"导出日志时发生错误:\n{ex.Message}");
            }
        }
    }
}
