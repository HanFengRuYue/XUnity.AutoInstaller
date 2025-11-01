using System;
using System.IO;
using Microsoft.UI.Dispatching;
using XUnity_AutoInstaller.Services;

namespace XUnity_AutoInstaller.Utils;

/// <summary>
/// 日志记录工具
/// 支持写入到 UI 和文件
/// </summary>
public class LogWriter
{
    private readonly Action<string>? _uiLogAction;
    private readonly DispatcherQueue? _dispatcherQueue;
    private readonly string? _logFilePath;

    /// <summary>
    /// 创建日志记录器
    /// </summary>
    /// <param name="uiLogAction">UI 日志回调（例如：更新 TextBlock）</param>
    /// <param name="dispatcherQueue">UI 线程调度器</param>
    /// <param name="logFilePath">日志文件路径（可选）</param>
    public LogWriter(Action<string>? uiLogAction = null, DispatcherQueue? dispatcherQueue = null, string? logFilePath = null)
    {
        _uiLogAction = uiLogAction;
        _dispatcherQueue = dispatcherQueue;
        _logFilePath = logFilePath;

        // 如果指定了日志文件，确保目录存在
        if (!string.IsNullOrEmpty(_logFilePath))
        {
            var directory = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }

    /// <summary>
    /// 写入信息日志
    /// </summary>
    public void Info(string message)
    {
        WriteLog("INFO", message);
    }

    /// <summary>
    /// 写入警告日志
    /// </summary>
    public void Warning(string message)
    {
        WriteLog("WARN", message);
    }

    /// <summary>
    /// 写入错误日志
    /// </summary>
    public void Error(string message)
    {
        WriteLog("ERROR", message);
    }

    /// <summary>
    /// 写入成功日志
    /// </summary>
    public void Success(string message)
    {
        WriteLog("SUCCESS", message);
    }

    /// <summary>
    /// 写入日志
    /// </summary>
    private void WriteLog(string level, string message)
    {
        // Map level to LogLevel enum
        var logLevel = level switch
        {
            "WARN" => LogLevel.Warning,
            "ERROR" => LogLevel.Error,
            _ => LogLevel.Info
        };

        // Use LogService for unified logging
        LogService.Instance.Log(message, logLevel, "[安装]");

        // Write to file if specified
        if (!string.IsNullOrEmpty(_logFilePath))
        {
            try
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                var logMessage = $"[{timestamp}] [{level}] {message}";
                File.AppendAllText(_logFilePath, logMessage + Environment.NewLine);
            }
            catch
            {
                // 忽略文件写入错误
            }
        }

        // Update UI callback if provided (for backward compatibility)
        if (_uiLogAction != null)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var logMessage = $"[{timestamp}] [{level}] {message}";

            if (_dispatcherQueue != null)
            {
                _dispatcherQueue.TryEnqueue(() => _uiLogAction(logMessage));
            }
            else
            {
                _uiLogAction(logMessage);
            }
        }
    }

    /// <summary>
    /// 清空日志文件
    /// </summary>
    public void ClearLogFile()
    {
        if (!string.IsNullOrEmpty(_logFilePath) && File.Exists(_logFilePath))
        {
            try
            {
                File.WriteAllText(_logFilePath, string.Empty);
            }
            catch
            {
                // 忽略错误
            }
        }
    }
}
