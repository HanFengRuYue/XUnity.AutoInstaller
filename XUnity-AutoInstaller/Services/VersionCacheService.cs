using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XUnity_AutoInstaller.Models;

namespace XUnity_AutoInstaller.Services;

/// <summary>
/// 版本缓存服务 - 单例模式
/// 负责管理全局版本列表缓存，避免重复 API 调用
/// </summary>
public class VersionCacheService
{
    private static VersionCacheService? _instance;
    private static readonly object _lock = new object();

    private readonly VersionService _versionService;
    private List<VersionInfo> _bepInExVersions = new();
    private List<VersionInfo> _xUnityVersions = new();
    private DateTime _lastRefreshTime = DateTime.MinValue;
    private bool _isInitialized = false;

    /// <summary>
    /// 版本列表更新事件
    /// </summary>
    public event EventHandler<VersionsUpdatedEventArgs>? VersionsUpdated;

    /// <summary>
    /// 获取单例实例
    /// </summary>
    public static VersionCacheService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new VersionCacheService();
                    }
                }
            }
            return _instance;
        }
    }

    private VersionCacheService()
    {
        _versionService = new VersionService();
    }

    /// <summary>
    /// 初始化版本缓存（应用启动时调用）
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            LogService.Instance.Log("版本缓存已初始化，跳过", LogLevel.Debug, "[VersionCache]");
            return;
        }

        try
        {
            LogService.Instance.Log("开始初始化版本缓存...", LogLevel.Info, "[VersionCache]");
            await RefreshAsync();
            _isInitialized = true;
            LogService.Instance.Log("版本缓存初始化完成", LogLevel.Info, "[VersionCache]");
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"版本缓存初始化失败: {ex.Message}", LogLevel.Error, "[VersionCache]");
            // 初始化失败不应阻止应用启动
        }
    }

    /// <summary>
    /// 刷新版本列表（从 GitHub API 重新获取）
    /// </summary>
    public async Task RefreshAsync()
    {
        try
        {
            LogService.Instance.Log("正在刷新版本列表...", LogLevel.Info, "[VersionCache]");

            // 获取所有可用版本（includePrerelease: true 以包含 IL2CPP 版本）
            var allVersions = await _versionService.GetAllAvailableVersionsAsync(includePrerelease: true);

            // 分离 BepInEx 和 XUnity 版本
            _bepInExVersions = allVersions
                .Where(v => v.PackageType == PackageType.BepInEx)
                .OrderByDescending(v => v.ReleaseDate)
                .ToList();

            _xUnityVersions = allVersions
                .Where(v => v.PackageType == PackageType.XUnity)
                .OrderByDescending(v => v.ReleaseDate)
                .ToList();

            _lastRefreshTime = DateTime.Now;

            LogService.Instance.Log($"版本列表刷新完成: BepInEx {_bepInExVersions.Count} 个, XUnity {_xUnityVersions.Count} 个", LogLevel.Info, "[VersionCache]");

            // 触发更新事件
            VersionsUpdated?.Invoke(this, new VersionsUpdatedEventArgs
            {
                BepInExVersions = _bepInExVersions,
                XUnityVersions = _xUnityVersions,
                RefreshTime = _lastRefreshTime
            });
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"刷新版本列表失败: {ex.Message}", LogLevel.Error, "[VersionCache]");
            throw;
        }
    }

    /// <summary>
    /// 获取缓存的 BepInEx 版本列表
    /// </summary>
    public List<VersionInfo> GetBepInExVersions()
    {
        return new List<VersionInfo>(_bepInExVersions);
    }

    /// <summary>
    /// 获取缓存的 XUnity 版本列表
    /// </summary>
    public List<VersionInfo> GetXUnityVersions()
    {
        return new List<VersionInfo>(_xUnityVersions);
    }

    /// <summary>
    /// 获取所有缓存的版本
    /// </summary>
    public List<VersionInfo> GetAllVersions()
    {
        var allVersions = new List<VersionInfo>();
        allVersions.AddRange(_bepInExVersions);
        allVersions.AddRange(_xUnityVersions);
        return allVersions;
    }

    /// <summary>
    /// 获取指定平台的 BepInEx 版本（已过滤）
    /// </summary>
    public List<VersionInfo> GetBepInExVersionsByPlatform(Platform platform, bool includePrerelease = false)
    {
        return _bepInExVersions
            .Where(v => v.TargetPlatform == platform && (includePrerelease || !v.IsPrerelease))
            .OrderByDescending(v => v.ReleaseDate)
            .ToList();
    }

    /// <summary>
    /// 获取最新的 BepInEx 版本（指定平台）
    /// </summary>
    public VersionInfo? GetLatestBepInExVersion(Platform platform, bool includePrerelease = false)
    {
        return _bepInExVersions
            .Where(v => v.TargetPlatform == platform && (includePrerelease || !v.IsPrerelease))
            .OrderByDescending(v => v.ReleaseDate)
            .FirstOrDefault();
    }

    /// <summary>
    /// 获取最新的 XUnity 版本
    /// </summary>
    public VersionInfo? GetLatestXUnityVersion(bool includePrerelease = false)
    {
        return _xUnityVersions
            .Where(v => includePrerelease || !v.IsPrerelease)
            .OrderByDescending(v => v.ReleaseDate)
            .FirstOrDefault();
    }

    /// <summary>
    /// 检查缓存是否已初始化
    /// </summary>
    public bool IsInitialized => _isInitialized;

    /// <summary>
    /// 获取上次刷新时间
    /// </summary>
    public DateTime LastRefreshTime => _lastRefreshTime;

    /// <summary>
    /// 获取缓存的版本数量
    /// </summary>
    public (int BepInExCount, int XUnityCount) GetVersionCounts()
    {
        return (_bepInExVersions.Count, _xUnityVersions.Count);
    }
}

/// <summary>
/// 版本更新事件参数
/// </summary>
public class VersionsUpdatedEventArgs : EventArgs
{
    public List<VersionInfo> BepInExVersions { get; set; } = new();
    public List<VersionInfo> XUnityVersions { get; set; } = new();
    public DateTime RefreshTime { get; set; }
}
