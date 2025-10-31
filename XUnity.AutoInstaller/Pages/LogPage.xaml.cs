using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using System.Text;
using XUnity.AutoInstaller.Services;

namespace XUnity.AutoInstaller.Pages
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
    }
}
