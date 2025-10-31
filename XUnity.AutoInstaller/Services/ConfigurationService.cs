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

            // [Logging.Console]
            config.LoggingConsoleEnabled = IniParser.GetBool(data, "Logging.Console", "Enabled", false);
            config.LoggingConsoleShiftJISCompatible = IniParser.GetBool(data, "Logging.Console", "ShiftJISCompatible", false);
            LogService.Instance.Log($"LoggingConsoleEnabled = {config.LoggingConsoleEnabled}", LogLevel.Debug, "[Config]");

            // [Logging.Disk]
            config.LoggingDiskEnabled = IniParser.GetBool(data, "Logging.Disk", "Enabled", true);

            // [Preloader.Entrypoint]
            config.PreloaderEntrypointAssembly = IniParser.GetValue(data, "Preloader.Entrypoint", "Assembly", "UnityEngine.CoreModule.dll");
            config.PreloaderEntrypointType = IniParser.GetValue(data, "Preloader.Entrypoint", "Type", "MonoBehaviour");
            config.PreloaderEntrypointMethod = IniParser.GetValue(data, "Preloader.Entrypoint", "Method", ".cctor");

            // [Preloader]
            config.PreloaderLogConsoleToUnityLog = IniParser.GetBool(data, "Preloader", "LogConsoleToUnityLog", false);
            config.PreloaderDumpAssemblies = IniParser.GetBool(data, "Preloader", "DumpAssemblies", false);

            // [Chainloader]
            config.ChainloaderLoggerDisplayedLevels = IniParser.GetValue(data, "Chainloader", "LogLevels", "Info,Message,Warning,Error,Fatal");
            config.ChainloaderLogUnityMessages = IniParser.GetBool(data, "Chainloader", "LogUnityMessages", true);

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

        // [Logging.Console]
        IniParser.SetValue(data, "Logging.Console", "Enabled", config.LoggingConsoleEnabled.ToString().ToLower());
        IniParser.SetValue(data, "Logging.Console", "ShiftJISCompatible", config.LoggingConsoleShiftJISCompatible.ToString().ToLower());

        // [Logging.Disk]
        IniParser.SetValue(data, "Logging.Disk", "Enabled", config.LoggingDiskEnabled.ToString().ToLower());

        // [Preloader.Entrypoint]
        IniParser.SetValue(data, "Preloader.Entrypoint", "Assembly", config.PreloaderEntrypointAssembly);
        IniParser.SetValue(data, "Preloader.Entrypoint", "Type", config.PreloaderEntrypointType);
        IniParser.SetValue(data, "Preloader.Entrypoint", "Method", config.PreloaderEntrypointMethod);

        // [Preloader]
        IniParser.SetValue(data, "Preloader", "LogConsoleToUnityLog", config.PreloaderLogConsoleToUnityLog.ToString().ToLower());
        IniParser.SetValue(data, "Preloader", "DumpAssemblies", config.PreloaderDumpAssemblies.ToString().ToLower());

        // [Chainloader]
        IniParser.SetValue(data, "Chainloader", "LogLevels", config.ChainloaderLoggerDisplayedLevels);
        IniParser.SetValue(data, "Chainloader", "LogUnityMessages", config.ChainloaderLogUnityMessages.ToString().ToLower());

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
            config.AdvancedMaxTextParserRecursion = IniParser.GetInt(data, "Behaviour", "MaxTextParserRecursion", 1);
            config.AdvancedHtmlEntityPreprocessing = IniParser.GetBool(data, "Behaviour", "HtmlEntityPreprocessing", true);

            // [Authentication] - 各个翻译服务的认证信息在各自的节里
            config.AuthenticationGoogleAPIKey = IniParser.GetValue(data, "GoogleLegitimate", "GoogleAPIKey", "");
            config.AuthenticationBingSubscriptionKey = IniParser.GetValue(data, "BingLegitimate", "OcpApimSubscriptionKey", "");
            config.AuthenticationDeepLAPIKey = IniParser.GetValue(data, "DeepLLegitimate", "ApiKey", "");
            config.AuthenticationBaiduAppId = IniParser.GetValue(data, "Baidu", "BaiduAppId", "");
            config.AuthenticationBaiduAppSecret = IniParser.GetValue(data, "Baidu", "BaiduAppSecret", "");
            config.AuthenticationYandexAPIKey = IniParser.GetValue(data, "Yandex", "YandexAPIKey", "");

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

        // [Authentication] - 各个翻译服务的认证信息在各自的节里
        IniParser.SetValue(data, "GoogleLegitimate", "GoogleAPIKey", config.AuthenticationGoogleAPIKey);
        IniParser.SetValue(data, "BingLegitimate", "OcpApimSubscriptionKey", config.AuthenticationBingSubscriptionKey);
        IniParser.SetValue(data, "DeepLLegitimate", "ApiKey", config.AuthenticationDeepLAPIKey);
        IniParser.SetValue(data, "Baidu", "BaiduAppId", config.AuthenticationBaiduAppId);
        IniParser.SetValue(data, "Baidu", "BaiduAppSecret", config.AuthenticationBaiduAppSecret);
        IniParser.SetValue(data, "Yandex", "YandexAPIKey", config.AuthenticationYandexAPIKey);

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
