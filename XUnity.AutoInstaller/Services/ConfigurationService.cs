using System;
using System.IO;
using XUnity.AutoInstaller.Models;
using XUnity.AutoInstaller.Utils;

namespace XUnity.AutoInstaller.Services;

/// <summary>
/// 配置管理服务
/// 负责读取和写入 BepInEx 和 XUnity 配置文件
/// </summary>
public class ConfigurationService
{
    /// <summary>
    /// 加载 BepInEx 配置
    /// </summary>
    public static BepInExConfig LoadBepInExConfig(string gamePath)
    {
        var configPath = PathHelper.GetBepInExConfigFile(gamePath);

        LogService.Instance.Log($"加载 BepInEx 配置: {configPath}", LogLevel.Debug, "[Config]");

        if (!File.Exists(configPath))
        {
            LogService.Instance.Log($"配置文件不存在，使用默认值", LogLevel.Debug, "[Config]");
            return BepInExConfig.CreateDefault();
        }

        try
        {
            var data = IniParser.Parse(configPath);
            LogService.Instance.Log($"成功解析配置文件，节数: {data.Count}", LogLevel.Debug, "[Config]");

            var config = new BepInExConfig();

            // [Caching]
            config.CachingEnableAssemblyCache = IniParser.GetBool(data, "Caching", "EnableAssemblyCache", true);

            // [Chainloader]
            config.ChainloaderHideManagerGameObject = IniParser.GetBool(data, "Chainloader", "HideManagerGameObject", false);
            config.ChainloaderLogLevels = IniParser.GetValue(data, "Chainloader", "LogLevels", "Info,Message,Warning,Error,Fatal");
            config.ChainloaderLogUnityMessages = IniParser.GetBool(data, "Chainloader", "LogUnityMessages", true);

            // [Harmony.Logger]
            config.HarmonyLoggerLogChannels = IniParser.GetValue(data, "Harmony.Logger", "LogChannels", "Warn, Error");

            // [Logging]
            config.LoggingUnityLogListening = IniParser.GetBool(data, "Logging", "UnityLogListening", true);
            config.LoggingLogConsoleToUnityLog = IniParser.GetBool(data, "Logging", "LogConsoleToUnityLog", false);

            // [Logging.Console]
            config.LoggingConsoleEnabled = IniParser.GetBool(data, "Logging.Console", "Enabled", false);
            config.LoggingConsolePreventClose = IniParser.GetBool(data, "Logging.Console", "PreventClose", false);
            config.LoggingConsoleShiftJisEncoding = IniParser.GetBool(data, "Logging.Console", "ShiftJisEncoding", false);
            config.LoggingConsoleStandardOutType = IniParser.GetValue(data, "Logging.Console", "StandardOutType", "Auto");
            config.LoggingConsoleLogLevels = IniParser.GetValue(data, "Logging.Console", "LogLevels", "Fatal, Error, Warning, Message, Info");
            LogService.Instance.Log($"LoggingConsoleEnabled = {config.LoggingConsoleEnabled}", LogLevel.Debug, "[Config]");

            // [Logging.Disk]
            config.LoggingDiskWriteUnityLog = IniParser.GetBool(data, "Logging.Disk", "WriteUnityLog", false);
            config.LoggingDiskAppendLog = IniParser.GetBool(data, "Logging.Disk", "AppendLog", false);
            config.LoggingDiskEnabled = IniParser.GetBool(data, "Logging.Disk", "Enabled", true);
            config.LoggingDiskLogLevels = IniParser.GetValue(data, "Logging.Disk", "LogLevels", "Fatal, Error, Warning, Message, Info");

            // [Preloader]
            config.PreloaderApplyRuntimePatches = IniParser.GetBool(data, "Preloader", "ApplyRuntimePatches", true);
            config.PreloaderHarmonyBackend = IniParser.GetValue(data, "Preloader", "HarmonyBackend", "auto");
            config.PreloaderDumpAssemblies = IniParser.GetBool(data, "Preloader", "DumpAssemblies", false);
            config.PreloaderLoadDumpedAssemblies = IniParser.GetBool(data, "Preloader", "LoadDumpedAssemblies", false);
            config.PreloaderBreakBeforeLoadAssemblies = IniParser.GetBool(data, "Preloader", "BreakBeforeLoadAssemblies", false);
            config.PreloaderLogConsoleToUnityLog = IniParser.GetBool(data, "Preloader", "LogConsoleToUnityLog", false);

            // [Preloader.Entrypoint]
            config.PreloaderEntrypointAssembly = IniParser.GetValue(data, "Preloader.Entrypoint", "Assembly", "UnityEngine.CoreModule.dll");
            config.PreloaderEntrypointType = IniParser.GetValue(data, "Preloader.Entrypoint", "Type", "MonoBehaviour");
            config.PreloaderEntrypointMethod = IniParser.GetValue(data, "Preloader.Entrypoint", "Method", ".cctor");

            LogService.Instance.Log($"BepInEx 配置加载成功", LogLevel.Debug, "[Config]");
            return config;
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"加载失败: {ex.GetType().Name} - {ex.Message}", LogLevel.Error, "[Config]");
            LogService.Instance.Log($"堆栈: {ex.StackTrace}", LogLevel.Error, "[Config]");
            return BepInExConfig.CreateDefault();
        }
    }

    /// <summary>
    /// 保存 BepInEx 配置
    /// </summary>
    public static void SaveBepInExConfig(string gamePath, BepInExConfig config)
    {
        var configPath = PathHelper.GetBepInExConfigFile(gamePath);
        var data = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>();

        // [Caching]
        IniParser.SetValue(data, "Caching", "EnableAssemblyCache", config.CachingEnableAssemblyCache.ToString().ToLower());

        // [Chainloader]
        IniParser.SetValue(data, "Chainloader", "HideManagerGameObject", config.ChainloaderHideManagerGameObject.ToString().ToLower());
        IniParser.SetValue(data, "Chainloader", "LogLevels", config.ChainloaderLogLevels);
        IniParser.SetValue(data, "Chainloader", "LogUnityMessages", config.ChainloaderLogUnityMessages.ToString().ToLower());

        // [Harmony.Logger]
        IniParser.SetValue(data, "Harmony.Logger", "LogChannels", config.HarmonyLoggerLogChannels);

        // [Logging]
        IniParser.SetValue(data, "Logging", "UnityLogListening", config.LoggingUnityLogListening.ToString().ToLower());
        IniParser.SetValue(data, "Logging", "LogConsoleToUnityLog", config.LoggingLogConsoleToUnityLog.ToString().ToLower());

        // [Logging.Console]
        IniParser.SetValue(data, "Logging.Console", "Enabled", config.LoggingConsoleEnabled.ToString().ToLower());
        IniParser.SetValue(data, "Logging.Console", "PreventClose", config.LoggingConsolePreventClose.ToString().ToLower());
        IniParser.SetValue(data, "Logging.Console", "ShiftJisEncoding", config.LoggingConsoleShiftJisEncoding.ToString().ToLower());
        IniParser.SetValue(data, "Logging.Console", "StandardOutType", config.LoggingConsoleStandardOutType);
        IniParser.SetValue(data, "Logging.Console", "LogLevels", config.LoggingConsoleLogLevels);

        // [Logging.Disk]
        IniParser.SetValue(data, "Logging.Disk", "WriteUnityLog", config.LoggingDiskWriteUnityLog.ToString().ToLower());
        IniParser.SetValue(data, "Logging.Disk", "AppendLog", config.LoggingDiskAppendLog.ToString().ToLower());
        IniParser.SetValue(data, "Logging.Disk", "Enabled", config.LoggingDiskEnabled.ToString().ToLower());
        IniParser.SetValue(data, "Logging.Disk", "LogLevels", config.LoggingDiskLogLevels);

        // [Preloader]
        IniParser.SetValue(data, "Preloader", "ApplyRuntimePatches", config.PreloaderApplyRuntimePatches.ToString().ToLower());
        IniParser.SetValue(data, "Preloader", "HarmonyBackend", config.PreloaderHarmonyBackend);
        IniParser.SetValue(data, "Preloader", "DumpAssemblies", config.PreloaderDumpAssemblies.ToString().ToLower());
        IniParser.SetValue(data, "Preloader", "LoadDumpedAssemblies", config.PreloaderLoadDumpedAssemblies.ToString().ToLower());
        IniParser.SetValue(data, "Preloader", "BreakBeforeLoadAssemblies", config.PreloaderBreakBeforeLoadAssemblies.ToString().ToLower());
        IniParser.SetValue(data, "Preloader", "LogConsoleToUnityLog", config.PreloaderLogConsoleToUnityLog.ToString().ToLower());

        // [Preloader.Entrypoint]
        IniParser.SetValue(data, "Preloader.Entrypoint", "Assembly", config.PreloaderEntrypointAssembly);
        IniParser.SetValue(data, "Preloader.Entrypoint", "Type", config.PreloaderEntrypointType);
        IniParser.SetValue(data, "Preloader.Entrypoint", "Method", config.PreloaderEntrypointMethod);

        IniParser.Write(configPath, data);
    }

    /// <summary>
    /// 加载 XUnity 配置
    /// </summary>
    public static XUnityConfig LoadXUnityConfig(string gamePath)
    {
        var configPath = PathHelper.GetXUnityConfigFile(gamePath);

        LogService.Instance.Log($"加载 XUnity 配置: {configPath}", LogLevel.Debug, "[Config]");

        if (!File.Exists(configPath))
        {
            LogService.Instance.Log($"配置文件不存在，使用推荐值", LogLevel.Debug, "[Config]");
            return XUnityConfig.CreateRecommended();
        }

        try
        {
            var data = IniParser.Parse(configPath);
            LogService.Instance.Log($"成功解析配置文件，节数: {data.Count}", LogLevel.Debug, "[Config]");

            var config = new XUnityConfig();

            // [Service]
            config.ServiceEndpoint = IniParser.GetValue(data, "Service", "Endpoint", "GoogleTranslate");
            config.ServiceFallbackEndpoint = IniParser.GetValue(data, "Service", "FallbackEndpoint", "");

            // [General] - 只有语言设置
            config.GeneralLanguage = IniParser.GetValue(data, "General", "Language", "zh-CN");
            config.GeneralFromLanguage = IniParser.GetValue(data, "General", "FromLanguage", "ja");

            // [Behaviour] - 行为设置（实际配置文件中的节名）
            config.GeneralMaxCharactersPerTranslation = IniParser.GetInt(data, "Behaviour", "MaxCharactersPerTranslation", 200);
            LogService.Instance.Log($"MaxCharactersPerTranslation = {config.GeneralMaxCharactersPerTranslation}", LogLevel.Debug, "[Config]");
            config.GeneralMinDialogueChars = IniParser.GetInt(data, "Behaviour", "MinDialogueChars", 20);
            config.GeneralIgnoreWhitespaceInDialogue = IniParser.GetBool(data, "Behaviour", "IgnoreWhitespaceInDialogue", true);
            config.GeneralEnableUIResizing = IniParser.GetBool(data, "Behaviour", "EnableUIResizing", true);
            config.GeneralOverrideFont = IniParser.GetValue(data, "Behaviour", "OverrideFont", "");
            config.GeneralCopyToClipboard = IniParser.GetBool(data, "Behaviour", "CopyToClipboard", false);

            // [TextFrameworks] - 键名是 EnableXXX
            config.TextFrameworksUGUI = IniParser.GetBool(data, "TextFrameworks", "EnableUGUI", true);
            config.TextFrameworksNGUI = IniParser.GetBool(data, "TextFrameworks", "EnableNGUI", true);
            config.TextFrameworksTextMeshPro = IniParser.GetBool(data, "TextFrameworks", "EnableTextMeshPro", true);
            config.TextFrameworksTextMesh = IniParser.GetBool(data, "TextFrameworks", "EnableTextMesh", true);
            config.TextFrameworksIMGUI = IniParser.GetBool(data, "TextFrameworks", "EnableIMGUI", false);

            // [Files]
            config.FilesDirectory = IniParser.GetValue(data, "Files", "Directory", "Translation\\{Lang}\\Text");
            config.FilesOutputFile = IniParser.GetValue(data, "Files", "OutputFile", "Translation\\{Lang}\\Text\\_AutoGeneratedTranslations.txt");
            config.FilesSubstitutionFile = IniParser.GetValue(data, "Files", "SubstitutionFile", "Translation\\{Lang}\\Text\\_Substitutions.txt");
            config.FilesPreprocessorsFile = IniParser.GetValue(data, "Files", "PreprocessorsFile", "Translation\\{Lang}\\Text\\_Preprocessors.txt");
            config.FilesPostprocessorsFile = IniParser.GetValue(data, "Files", "PostprocessorsFile", "Translation\\{Lang}\\Text\\_Postprocessors.txt");

            // [Texture]
            config.TextureDirectory = IniParser.GetValue(data, "Texture", "TextureDirectory", "Translation\\{Lang}\\Texture");
            config.TextureEnableTranslation = IniParser.GetBool(data, "Texture", "EnableTextureTranslation", false);
            config.TextureEnableDumping = IniParser.GetBool(data, "Texture", "EnableTextureDumping", false);
            config.TextureHashGenerationStrategy = IniParser.GetValue(data, "Texture", "TextureHashGenerationStrategy", "FromImageName");

            // [Behaviour] - 高级选项也在 Behaviour 节
            config.AdvancedEnableTranslationScoping = IniParser.GetBool(data, "Behaviour", "EnableTranslationScoping", true);
            config.AdvancedHandleRichText = IniParser.GetBool(data, "Behaviour", "HandleRichText", true);
            config.AdvancedMaxTextParserRecursion = IniParser.GetInt(data, "Behaviour", "MaxTextParserRecursion", 10);
            config.AdvancedHtmlEntityPreprocessing = IniParser.GetBool(data, "Behaviour", "HtmlEntityPreprocessing", true);

            // [Behaviour] - Additional options
            config.BehaviourEnableBatching = IniParser.GetBool(data, "Behaviour", "EnableBatching", true);
            config.BehaviourUseStaticTranslations = IniParser.GetBool(data, "Behaviour", "UseStaticTranslations", true);
            config.BehaviourIgnoreTextStartingWith = IniParser.GetValue(data, "Behaviour", "IgnoreTextStartingWith", "");
            config.BehaviourOutputUntranslatableText = IniParser.GetBool(data, "Behaviour", "OutputUntranslatableText", false);
            config.BehaviourDelay = IniParser.GetInt(data, "Behaviour", "Delay", 0);

            // [Http]
            config.HttpUserAgent = IniParser.GetValue(data, "Http", "UserAgent", "");
            config.HttpDisableCertificateChecks = IniParser.GetBool(data, "Http", "DisableCertificateChecks", false);

            // [Debug]
            config.DebugEnableConsole = IniParser.GetBool(data, "Debug", "EnableConsole", false);
            config.DebugEnableLog = IniParser.GetBool(data, "Debug", "EnableLog", false);

            // [Optimization]
            config.OptimizationEnableCache = IniParser.GetBool(data, "Optimization", "EnableCache", true);
            config.OptimizationMaxCacheEntries = IniParser.GetInt(data, "Optimization", "MaxCacheEntries", 5000);

            // [Integration]
            config.IntegrationTextGetterCompatibilityMode = IniParser.GetBool(data, "Integration", "TextGetterCompatibilityMode", false);

            // [ResourceRedirector]
            config.ResourceRedirectorEnableRedirector = IniParser.GetBool(data, "ResourceRedirector", "EnableRedirector", true);
            config.ResourceRedirectorDetectDuplicateResources = IniParser.GetBool(data, "ResourceRedirector", "DetectDuplicateResources", false);

            // [Authentication] - 各个翻译服务的认证信息在各自的节里
            config.AuthenticationGoogleAPIKey = IniParser.GetValue(data, "GoogleLegitimate", "GoogleAPIKey", "");
            config.AuthenticationBingSubscriptionKey = IniParser.GetValue(data, "BingLegitimate", "OcpApimSubscriptionKey", "");
            config.AuthenticationDeepLAPIKey = IniParser.GetValue(data, "DeepLLegitimate", "ApiKey", "");
            config.AuthenticationBaiduAppId = IniParser.GetValue(data, "Baidu", "BaiduAppId", "");
            config.AuthenticationBaiduAppSecret = IniParser.GetValue(data, "Baidu", "BaiduAppSecret", "");
            config.AuthenticationYandexAPIKey = IniParser.GetValue(data, "Yandex", "YandexAPIKey", "");
            config.AuthenticationWatsonAPIKey = IniParser.GetValue(data, "Watson", "ApiKey", "");
            config.AuthenticationLingoCloudToken = IniParser.GetValue(data, "LingoCloud", "Token", "");

            LogService.Instance.Log($"XUnity 配置加载成功", LogLevel.Debug, "[Config]");
            return config;
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"加载失败: {ex.GetType().Name} - {ex.Message}", LogLevel.Error, "[Config]");
            LogService.Instance.Log($"堆栈: {ex.StackTrace}", LogLevel.Error, "[Config]");
            return XUnityConfig.CreateRecommended();
        }
    }

    /// <summary>
    /// 保存 XUnity 配置
    /// </summary>
    public static void SaveXUnityConfig(string gamePath, XUnityConfig config)
    {
        var configPath = PathHelper.GetXUnityConfigFile(gamePath);
        var data = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>>();

        // [Service]
        IniParser.SetValue(data, "Service", "Endpoint", config.ServiceEndpoint);
        IniParser.SetValue(data, "Service", "FallbackEndpoint", config.ServiceFallbackEndpoint);

        // [General] - 只有语言
        IniParser.SetValue(data, "General", "Language", config.GeneralLanguage);
        IniParser.SetValue(data, "General", "FromLanguage", config.GeneralFromLanguage);

        // [Behaviour] - 行为设置
        IniParser.SetValue(data, "Behaviour", "MaxCharactersPerTranslation", config.GeneralMaxCharactersPerTranslation.ToString());
        IniParser.SetValue(data, "Behaviour", "MinDialogueChars", config.GeneralMinDialogueChars.ToString());
        IniParser.SetValue(data, "Behaviour", "IgnoreWhitespaceInDialogue", config.GeneralIgnoreWhitespaceInDialogue.ToString());
        IniParser.SetValue(data, "Behaviour", "EnableUIResizing", config.GeneralEnableUIResizing.ToString());
        IniParser.SetValue(data, "Behaviour", "OverrideFont", config.GeneralOverrideFont);
        IniParser.SetValue(data, "Behaviour", "CopyToClipboard", config.GeneralCopyToClipboard.ToString());

        // [TextFrameworks] - 键名是 EnableXXX
        IniParser.SetValue(data, "TextFrameworks", "EnableUGUI", config.TextFrameworksUGUI.ToString());
        IniParser.SetValue(data, "TextFrameworks", "EnableNGUI", config.TextFrameworksNGUI.ToString());
        IniParser.SetValue(data, "TextFrameworks", "EnableTextMeshPro", config.TextFrameworksTextMeshPro.ToString());
        IniParser.SetValue(data, "TextFrameworks", "EnableTextMesh", config.TextFrameworksTextMesh.ToString());
        IniParser.SetValue(data, "TextFrameworks", "EnableIMGUI", config.TextFrameworksIMGUI.ToString());

        // [Files]
        IniParser.SetValue(data, "Files", "Directory", config.FilesDirectory);
        IniParser.SetValue(data, "Files", "OutputFile", config.FilesOutputFile);
        IniParser.SetValue(data, "Files", "SubstitutionFile", config.FilesSubstitutionFile);
        IniParser.SetValue(data, "Files", "PreprocessorsFile", config.FilesPreprocessorsFile);
        IniParser.SetValue(data, "Files", "PostprocessorsFile", config.FilesPostprocessorsFile);

        // [Texture]
        IniParser.SetValue(data, "Texture", "TextureDirectory", config.TextureDirectory);
        IniParser.SetValue(data, "Texture", "EnableTextureTranslation", config.TextureEnableTranslation.ToString());
        IniParser.SetValue(data, "Texture", "EnableTextureDumping", config.TextureEnableDumping.ToString());
        IniParser.SetValue(data, "Texture", "TextureHashGenerationStrategy", config.TextureHashGenerationStrategy);

        // [Behaviour] - 高级选项也在 Behaviour 节
        IniParser.SetValue(data, "Behaviour", "EnableTranslationScoping", config.AdvancedEnableTranslationScoping.ToString());
        IniParser.SetValue(data, "Behaviour", "HandleRichText", config.AdvancedHandleRichText.ToString());
        IniParser.SetValue(data, "Behaviour", "MaxTextParserRecursion", config.AdvancedMaxTextParserRecursion.ToString());
        IniParser.SetValue(data, "Behaviour", "HtmlEntityPreprocessing", config.AdvancedHtmlEntityPreprocessing.ToString());

        // [Behaviour] - Additional options
        IniParser.SetValue(data, "Behaviour", "EnableBatching", config.BehaviourEnableBatching.ToString());
        IniParser.SetValue(data, "Behaviour", "UseStaticTranslations", config.BehaviourUseStaticTranslations.ToString());
        IniParser.SetValue(data, "Behaviour", "IgnoreTextStartingWith", config.BehaviourIgnoreTextStartingWith);
        IniParser.SetValue(data, "Behaviour", "OutputUntranslatableText", config.BehaviourOutputUntranslatableText.ToString());
        IniParser.SetValue(data, "Behaviour", "Delay", config.BehaviourDelay.ToString());

        // [Http]
        IniParser.SetValue(data, "Http", "UserAgent", config.HttpUserAgent);
        IniParser.SetValue(data, "Http", "DisableCertificateChecks", config.HttpDisableCertificateChecks.ToString());

        // [Debug]
        IniParser.SetValue(data, "Debug", "EnableConsole", config.DebugEnableConsole.ToString());
        IniParser.SetValue(data, "Debug", "EnableLog", config.DebugEnableLog.ToString());

        // [Optimization]
        IniParser.SetValue(data, "Optimization", "EnableCache", config.OptimizationEnableCache.ToString());
        IniParser.SetValue(data, "Optimization", "MaxCacheEntries", config.OptimizationMaxCacheEntries.ToString());

        // [Integration]
        IniParser.SetValue(data, "Integration", "TextGetterCompatibilityMode", config.IntegrationTextGetterCompatibilityMode.ToString());

        // [ResourceRedirector]
        IniParser.SetValue(data, "ResourceRedirector", "EnableRedirector", config.ResourceRedirectorEnableRedirector.ToString());
        IniParser.SetValue(data, "ResourceRedirector", "DetectDuplicateResources", config.ResourceRedirectorDetectDuplicateResources.ToString());

        // [Authentication] - 各个翻译服务的认证信息在各自的节里
        IniParser.SetValue(data, "GoogleLegitimate", "GoogleAPIKey", config.AuthenticationGoogleAPIKey);
        IniParser.SetValue(data, "BingLegitimate", "OcpApimSubscriptionKey", config.AuthenticationBingSubscriptionKey);
        IniParser.SetValue(data, "DeepLLegitimate", "ApiKey", config.AuthenticationDeepLAPIKey);
        IniParser.SetValue(data, "Baidu", "BaiduAppId", config.AuthenticationBaiduAppId);
        IniParser.SetValue(data, "Baidu", "BaiduAppSecret", config.AuthenticationBaiduAppSecret);
        IniParser.SetValue(data, "Yandex", "YandexAPIKey", config.AuthenticationYandexAPIKey);
        IniParser.SetValue(data, "Watson", "ApiKey", config.AuthenticationWatsonAPIKey);
        IniParser.SetValue(data, "LingoCloud", "Token", config.AuthenticationLingoCloudToken);

        IniParser.Write(configPath, data);
    }

    /// <summary>
    /// 验证游戏路径是否有效
    /// </summary>
    public static bool ValidateGamePath(string gamePath)
    {
        if (string.IsNullOrEmpty(gamePath))
        {
            return false;
        }

        // 检查 BepInEx 是否已安装
        var bepinexPath = PathHelper.GetBepInExPath(gamePath);
        return Directory.Exists(bepinexPath);
    }
}
