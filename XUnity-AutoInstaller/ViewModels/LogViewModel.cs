using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XUnity_AutoInstaller.Services;

namespace XUnity_AutoInstaller.ViewModels;

public partial class LogViewModel : ObservableObject
{
    private readonly LogService _logService;
    private LogLevel? _currentFilter;

    public event EventHandler<LogEntry>? LogEntryAdded;

    [ObservableProperty]
    private string logText = string.Empty;

    [ObservableProperty]
    private string logCount = "日志数: 0";

    [ObservableProperty]
    private bool autoScroll = true;

    [ObservableProperty]
    private int selectedFilterIndex;

    public LogViewModel(LogService logService)
    {
        _logService = logService;
        _logService.LogEntryAdded += OnLogServiceLogEntryAdded;
    }

    partial void OnSelectedFilterIndexChanged(int value)
    {
        _currentFilter = value switch
        {
            0 => null,
            1 => LogLevel.Debug,
            2 => LogLevel.Info,
            3 => LogLevel.Warning,
            4 => LogLevel.Error,
            _ => null
        };

        RefreshLogDisplay();
    }

    public void Initialize()
    {
        RefreshLogDisplay();
    }

    private void OnLogServiceLogEntryAdded(object? sender, LogEntry entry)
    {
        UpdateLogCount();

        if (_currentFilter == null || entry.Level == _currentFilter)
        {
            LogEntryAdded?.Invoke(this, entry);
        }
    }

    public void RefreshLogDisplay()
    {
        var logs = _logService.GetAllLogs();

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

        LogText = sb.ToString();
        UpdateLogCount();
    }

    private void UpdateLogCount()
    {
        var totalCount = _logService.GetAllLogs().Count();
        LogCount = $"日志数: {totalCount}";
    }

    [RelayCommand]
    private void Clear()
    {
        _logService.Clear();
        LogText = string.Empty;
        UpdateLogCount();
    }

    public IEnumerable<LogEntry> GetFilteredLogs()
    {
        var logs = _logService.GetAllLogs();

        if (_currentFilter != null)
        {
            logs = logs.Where(log => log.Level == _currentFilter);
        }

        return logs;
    }

    public string GetFilterName()
    {
        return _currentFilter == null ? "全部" : _currentFilter.ToString()!;
    }

    public void Cleanup()
    {
        _logService.LogEntryAdded -= OnLogServiceLogEntryAdded;
    }
}
