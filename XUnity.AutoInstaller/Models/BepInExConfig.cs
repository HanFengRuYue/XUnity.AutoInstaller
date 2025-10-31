namespace XUnity.AutoInstaller.Models;

/// <summary>
/// BepInEx 配置 (BepInEx.cfg)
/// </summary>
public class BepInExConfig
{
    // [Caching]
    /// <summary>
    /// 启用程序集元数据缓存以加速插件发现
    /// </summary>
    public bool CachingEnableAssemblyCache { get; set; } = true;

    // [Chainloader]
    /// <summary>
    /// 隐藏BepInEx管理器GameObject（可修复某些游戏的加载问题）
    /// </summary>
    public bool ChainloaderHideManagerGameObject { get; set; } = false;

    /// <summary>
    /// Chainloader日志显示级别
    /// </summary>
    public string ChainloaderLogLevels { get; set; } = "Info,Message,Warning,Error,Fatal";

    /// <summary>
    /// 在BepInEx日志中显示Unity日志消息
    /// </summary>
    public bool ChainloaderLogUnityMessages { get; set; } = true;

    // [Harmony.Logger]
    /// <summary>
    /// Harmony日志通道（None, Info, IL, Warn, Error, Debug, All）
    /// </summary>
    public string HarmonyLoggerLogChannels { get; set; } = "Warn, Error";

    // [Logging]
    /// <summary>
    /// 在BepInEx日志系统中显示Unity日志消息
    /// </summary>
    public bool LoggingUnityLogListening { get; set; } = true;

    /// <summary>
    /// 将标准输出消息写入Unity日志
    /// </summary>
    public bool LoggingLogConsoleToUnityLog { get; set; } = false;

    // [Logging.Console]
    /// <summary>
    /// 启用控制台日志输出
    /// </summary>
    public bool LoggingConsoleEnabled { get; set; } = false;

    /// <summary>
    /// 防止关闭控制台窗口
    /// </summary>
    public bool LoggingConsolePreventClose { get; set; } = false;

    /// <summary>
    /// 使用Shift-JIS编码（否则使用UTF-8）
    /// </summary>
    public bool LoggingConsoleShiftJisEncoding { get; set; } = false;

    /// <summary>
    /// 标准输出重定向类型（Auto, ConsoleOut, StandardOut）
    /// </summary>
    public string LoggingConsoleStandardOutType { get; set; } = "Auto";

    /// <summary>
    /// 控制台显示的日志级别
    /// </summary>
    public string LoggingConsoleLogLevels { get; set; } = "Fatal, Error, Warning, Message, Info";

    // [Logging.Disk]
    /// <summary>
    /// 在磁盘日志中包含Unity日志消息
    /// </summary>
    public bool LoggingDiskWriteUnityLog { get; set; } = false;

    /// <summary>
    /// 游戏启动时追加到日志文件而不是覆盖
    /// </summary>
    public bool LoggingDiskAppendLog { get; set; } = false;

    /// <summary>
    /// 启用磁盘日志输出
    /// </summary>
    public bool LoggingDiskEnabled { get; set; } = true;

    /// <summary>
    /// 保存到磁盘的日志级别
    /// </summary>
    public string LoggingDiskLogLevels { get; set; } = "Fatal, Error, Warning, Message, Info";

    // [Preloader]
    /// <summary>
    /// 启用或禁用运行时补丁（除非遇到Harmony问题，否则应始终为true）
    /// </summary>
    public bool PreloaderApplyRuntimePatches { get; set; } = true;

    /// <summary>
    /// MonoMod后端类型（auto, dynamicmethod, methodbuilder, cecil）
    /// </summary>
    public string PreloaderHarmonyBackend { get; set; } = "auto";

    /// <summary>
    /// 将修补后的程序集保存到BepInEx/DumpedAssemblies
    /// </summary>
    public bool PreloaderDumpAssemblies { get; set; } = false;

    /// <summary>
    /// 从BepInEx/DumpedAssemblies加载修补后的程序集
    /// </summary>
    public bool PreloaderLoadDumpedAssemblies { get; set; } = false;

    /// <summary>
    /// 在加载修补程序集前调用Debugger.Break()
    /// </summary>
    public bool PreloaderBreakBeforeLoadAssemblies { get; set; } = false;

    /// <summary>
    /// 将控制台日志输出到Unity日志（Preloader节点）
    /// </summary>
    public bool PreloaderLogConsoleToUnityLog { get; set; } = false;

    // [Preloader.Entrypoint]
    /// <summary>
    /// 入口点程序集文件名
    /// </summary>
    public string PreloaderEntrypointAssembly { get; set; } = "UnityEngine.CoreModule.dll";

    /// <summary>
    /// 入口点程序集中的类型名称
    /// </summary>
    public string PreloaderEntrypointType { get; set; } = "MonoBehaviour";

    /// <summary>
    /// 入口点方法名称
    /// </summary>
    public string PreloaderEntrypointMethod { get; set; } = ".cctor";

    /// <summary>
    /// 创建默认配置
    /// </summary>
    public static BepInExConfig CreateDefault()
    {
        return new BepInExConfig
        {
            CachingEnableAssemblyCache = true,
            ChainloaderHideManagerGameObject = false,
            ChainloaderLogLevels = "Info,Message,Warning,Error,Fatal",
            ChainloaderLogUnityMessages = true,
            HarmonyLoggerLogChannels = "Warn, Error",
            LoggingUnityLogListening = true,
            LoggingLogConsoleToUnityLog = false,
            LoggingConsoleEnabled = false,
            LoggingConsolePreventClose = false,
            LoggingConsoleShiftJisEncoding = false,
            LoggingConsoleStandardOutType = "Auto",
            LoggingConsoleLogLevels = "Fatal, Error, Warning, Message, Info",
            LoggingDiskWriteUnityLog = false,
            LoggingDiskAppendLog = false,
            LoggingDiskEnabled = true,
            LoggingDiskLogLevels = "Fatal, Error, Warning, Message, Info",
            PreloaderApplyRuntimePatches = true,
            PreloaderHarmonyBackend = "auto",
            PreloaderDumpAssemblies = false,
            PreloaderLoadDumpedAssemblies = false,
            PreloaderBreakBeforeLoadAssemblies = false,
            PreloaderLogConsoleToUnityLog = false,
            PreloaderEntrypointAssembly = "UnityEngine.CoreModule.dll",
            PreloaderEntrypointType = "MonoBehaviour",
            PreloaderEntrypointMethod = ".cctor"
        };
    }
}
