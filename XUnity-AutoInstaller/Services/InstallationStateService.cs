using System;

namespace XUnity_AutoInstaller.Services;

/// <summary>
/// 全局安装状态管理服务
/// 用于跨页面共享安装进度和状态
/// </summary>
public class InstallationStateService
{
    private static readonly InstallationStateService _instance = new();
    public static InstallationStateService Instance => _instance;

    private InstallationStateService() { }

    /// <summary>
    /// 是否正在安装
    /// </summary>
    public bool IsInstalling { get; private set; }

    /// <summary>
    /// 当前进度（0-100）
    /// </summary>
    public int Progress { get; private set; }

    /// <summary>
    /// 当前状态消息
    /// </summary>
    public string Message { get; private set; } = "";

    /// <summary>
    /// 安装开始事件
    /// </summary>
    public event EventHandler? InstallationStarted;

    /// <summary>
    /// 进度更新事件
    /// </summary>
    public event EventHandler<(int progress, string message)>? ProgressChanged;

    /// <summary>
    /// 安装完成事件（参数：是否成功）
    /// </summary>
    public event EventHandler<bool>? InstallationCompleted;

    /// <summary>
    /// 开始安装
    /// </summary>
    public void StartInstallation()
    {
        IsInstalling = true;
        Progress = 0;
        Message = "准备安装...";
        InstallationStarted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 更新进度
    /// </summary>
    public void UpdateProgress(int progress, string message)
    {
        Progress = progress;
        Message = message;
        ProgressChanged?.Invoke(this, (progress, message));
    }

    /// <summary>
    /// 完成安装
    /// </summary>
    public void CompleteInstallation(bool success)
    {
        IsInstalling = false;
        Progress = success ? 100 : 0;
        InstallationCompleted?.Invoke(this, success);
    }

    /// <summary>
    /// 创建进度报告器
    /// </summary>
    public IProgress<(int, string)> CreateProgressReporter()
    {
        return new Progress<(int percentage, string message)>(p =>
        {
            UpdateProgress(p.percentage, p.message);
        });
    }
}
