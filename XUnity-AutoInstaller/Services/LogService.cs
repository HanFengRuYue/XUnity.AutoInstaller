using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace XUnity_AutoInstaller.Services;

/// <summary>
/// Singleton service for managing application-wide logging
/// </summary>
public class LogService
{
    private static LogService? _instance;
    private static readonly object _lock = new object();

    private readonly ConcurrentQueue<LogEntry> _logEntries;
    private const int MaxLogEntries = 10000; // Limit log size to prevent memory issues

    /// <summary>
    /// Event fired when a new log entry is added
    /// </summary>
    public event EventHandler<LogEntry>? LogEntryAdded;

    /// <summary>
    /// Gets the singleton instance of LogService
    /// </summary>
    public static LogService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new LogService();
                }
            }
            return _instance;
        }
    }

    private LogService()
    {
        _logEntries = new ConcurrentQueue<LogEntry>();
    }

    /// <summary>
    /// Logs a message with specified level
    /// </summary>
    /// <param name="message">The log message</param>
    /// <param name="level">The log level</param>
    /// <param name="prefix">Optional prefix for categorization (e.g., [Config], [IL2CPP])</param>
    public void Log(string message, LogLevel level = LogLevel.Info, string? prefix = null)
    {
        var entry = new LogEntry
        {
            Timestamp = DateTime.Now,
            Level = level,
            Prefix = prefix,
            Message = message
        };

        _logEntries.Enqueue(entry);

        // Trim old entries if exceeding max capacity
        while (_logEntries.Count > MaxLogEntries)
        {
            _logEntries.TryDequeue(out _);
        }

        // Notify subscribers
        LogEntryAdded?.Invoke(this, entry);
    }

    /// <summary>
    /// Gets all log entries
    /// </summary>
    public IEnumerable<LogEntry> GetAllLogs()
    {
        return _logEntries.ToArray();
    }

    /// <summary>
    /// Clears all log entries
    /// </summary>
    public void Clear()
    {
        _logEntries.Clear();
    }
}

/// <summary>
/// Represents a single log entry
/// </summary>
public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public string? Prefix { get; set; }
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets the formatted log entry as a string
    /// </summary>
    public string FormattedMessage
    {
        get
        {
            var levelStr = Level switch
            {
                LogLevel.Debug => "DEBUG",
                LogLevel.Info => "INFO",
                LogLevel.Warning => "WARN",
                LogLevel.Error => "ERROR",
                _ => "INFO"
            };

            var prefixStr = !string.IsNullOrEmpty(Prefix) ? $"{Prefix} " : "";
            return $"[{Timestamp:HH:mm:ss}] [{levelStr}] {prefixStr}{Message}";
        }
    }
}

/// <summary>
/// Log severity levels
/// </summary>
public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}
