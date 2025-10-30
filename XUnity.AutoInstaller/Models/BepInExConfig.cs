namespace XUnity.AutoInstaller.Models;

/// <summary>
/// BepInEx 配置 (BepInEx.cfg)
/// </summary>
public class BepInExConfig
{
    // [Logging.Console]
    public bool LoggingConsoleEnabled { get; set; } = false;
    public bool LoggingConsoleShiftJISCompatible { get; set; } = false;

    // [Logging.Disk]
    public bool LoggingDiskEnabled { get; set; } = true;

    // [Preloader.Entrypoint]
    public string PreloaderEntrypointAssembly { get; set; } = "UnityEngine.CoreModule.dll";
    public string PreloaderEntrypointType { get; set; } = "MonoBehaviour";
    public string PreloaderEntrypointMethod { get; set; } = ".cctor";

    // [Preloader]
    public bool PreloaderLogConsoleToUnityLog { get; set; } = false;
    public bool PreloaderDumpAssemblies { get; set; } = false;

    // [Chainloader]
    public string ChainloaderLoggerDisplayedLevels { get; set; } = "Info,Message,Warning,Error,Fatal";
    public bool ChainloaderLogUnityMessages { get; set; } = true;

    /// <summary>
    /// 创建默认配置
    /// </summary>
    public static BepInExConfig CreateDefault()
    {
        return new BepInExConfig
        {
            LoggingConsoleEnabled = false,
            LoggingConsoleShiftJISCompatible = false,
            LoggingDiskEnabled = true,
            PreloaderEntrypointAssembly = "UnityEngine.CoreModule.dll",
            PreloaderEntrypointType = "MonoBehaviour",
            PreloaderEntrypointMethod = ".cctor",
            ChainloaderLoggerDisplayedLevels = "Info,Message,Warning,Error,Fatal",
            ChainloaderLogUnityMessages = true
        };
    }
}
