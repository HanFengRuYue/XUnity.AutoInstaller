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

        if (!File.Exists(configPath))
        {
            return BepInExConfig.CreateDefault();
        }

        try
        {
            var data = IniParser.Parse(configPath);
            var config = new BepInExConfig();

            // [Logging.Console]
            config.LoggingConsoleEnabled = IniParser.GetBool(data, "Logging.Console", "Enabled", false);
            config.LoggingConsoleShiftJISCompatible = IniParser.GetBool(data, "Logging.Console", "ShiftJISCompatible", false);

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

            return config;
        }
        catch
        {
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

        if (!File.Exists(configPath))
        {
            return XUnityConfig.CreateRecommended();
        }

        try
        {
            var data = IniParser.Parse(configPath);
            var config = new XUnityConfig();

            // [Service]
            config.ServiceEndpoint = IniParser.GetValue(data, "Service", "Endpoint", "GoogleTranslate");
            config.ServiceFallbackEndpoint = IniParser.GetValue(data, "Service", "FallbackEndpoint", "");

            // [General]
            config.GeneralLanguage = IniParser.GetValue(data, "General", "Language", "zh-CN");
            config.GeneralFromLanguage = IniParser.GetValue(data, "General", "FromLanguage", "ja");
            config.GeneralMaxCharactersPerTranslation = IniParser.GetInt(data, "General", "MaxCharactersPerTranslation", 200);
            config.GeneralMinDialogueChars = IniParser.GetInt(data, "General", "MinDialogueChars", 20);
            config.GeneralIgnoreWhitespaceInDialogue = IniParser.GetBool(data, "General", "IgnoreWhitespaceInDialogue", true);
            config.GeneralEnableUIResizing = IniParser.GetBool(data, "General", "EnableUIResizing", true);
            config.GeneralOverrideFont = IniParser.GetValue(data, "General", "OverrideFont", "");
            config.GeneralCopyToClipboard = IniParser.GetBool(data, "General", "CopyToClipboard", false);

            // [TextFrameworks]
            config.TextFrameworksUGUI = IniParser.GetBool(data, "TextFrameworks", "UGUI", true);
            config.TextFrameworksNGUI = IniParser.GetBool(data, "TextFrameworks", "NGUI", true);
            config.TextFrameworksTextMeshPro = IniParser.GetBool(data, "TextFrameworks", "TextMeshPro", true);
            config.TextFrameworksTextMesh = IniParser.GetBool(data, "TextFrameworks", "TextMesh", true);
            config.TextFrameworksIMGUI = IniParser.GetBool(data, "TextFrameworks", "IMGUI", false);

            // [Files]
            config.FilesDirectory = IniParser.GetValue(data, "Files", "Directory", "Translation");
            config.FilesOutputFile = IniParser.GetValue(data, "Files", "OutputFile", "_AutoGeneratedTranslations.txt");
            config.FilesSubstitutionFile = IniParser.GetValue(data, "Files", "SubstitutionFile", "_Substitutions.txt");
            config.FilesPreprocessorsFile = IniParser.GetValue(data, "Files", "PreprocessorsFile", "_Preprocessors.txt");
            config.FilesPostprocessorsFile = IniParser.GetValue(data, "Files", "PostprocessorsFile", "_Postprocessors.txt");

            // [Texture]
            config.TextureDirectory = IniParser.GetValue(data, "Texture", "Directory", "Translation");
            config.TextureEnableTranslation = IniParser.GetBool(data, "Texture", "EnableTranslation", true);
            config.TextureEnableDumping = IniParser.GetBool(data, "Texture", "EnableDumping", false);
            config.TextureHashGenerationStrategy = IniParser.GetValue(data, "Texture", "HashGenerationStrategy", "FromImageName");

            // [Advanced]
            config.AdvancedEnableTranslationScoping = IniParser.GetBool(data, "Advanced", "EnableTranslationScoping", true);
            config.AdvancedHandleRichText = IniParser.GetBool(data, "Advanced", "HandleRichText", true);
            config.AdvancedMaxTextParserRecursion = IniParser.GetInt(data, "Advanced", "MaxTextParserRecursion", 10);
            config.AdvancedHtmlEntityPreprocessing = IniParser.GetBool(data, "Advanced", "HtmlEntityPreprocessing", true);

            // [Authentication]
            config.AuthenticationGoogleAPIKey = IniParser.GetValue(data, "Authentication", "GoogleAPIKey", "");
            config.AuthenticationBingSubscriptionKey = IniParser.GetValue(data, "Authentication", "BingSubscriptionKey", "");
            config.AuthenticationDeepLAPIKey = IniParser.GetValue(data, "Authentication", "DeepLAPIKey", "");
            config.AuthenticationBaiduAppId = IniParser.GetValue(data, "Authentication", "BaiduAppId", "");
            config.AuthenticationBaiduAppSecret = IniParser.GetValue(data, "Authentication", "BaiduAppSecret", "");
            config.AuthenticationYandexAPIKey = IniParser.GetValue(data, "Authentication", "YandexAPIKey", "");

            return config;
        }
        catch
        {
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

        // [General]
        IniParser.SetValue(data, "General", "Language", config.GeneralLanguage);
        IniParser.SetValue(data, "General", "FromLanguage", config.GeneralFromLanguage);
        IniParser.SetValue(data, "General", "MaxCharactersPerTranslation", config.GeneralMaxCharactersPerTranslation.ToString());
        IniParser.SetValue(data, "General", "MinDialogueChars", config.GeneralMinDialogueChars.ToString());
        IniParser.SetValue(data, "General", "IgnoreWhitespaceInDialogue", config.GeneralIgnoreWhitespaceInDialogue.ToString().ToLower());
        IniParser.SetValue(data, "General", "EnableUIResizing", config.GeneralEnableUIResizing.ToString().ToLower());
        IniParser.SetValue(data, "General", "OverrideFont", config.GeneralOverrideFont);
        IniParser.SetValue(data, "General", "CopyToClipboard", config.GeneralCopyToClipboard.ToString().ToLower());

        // [TextFrameworks]
        IniParser.SetValue(data, "TextFrameworks", "UGUI", config.TextFrameworksUGUI.ToString().ToLower());
        IniParser.SetValue(data, "TextFrameworks", "NGUI", config.TextFrameworksNGUI.ToString().ToLower());
        IniParser.SetValue(data, "TextFrameworks", "TextMeshPro", config.TextFrameworksTextMeshPro.ToString().ToLower());
        IniParser.SetValue(data, "TextFrameworks", "TextMesh", config.TextFrameworksTextMesh.ToString().ToLower());
        IniParser.SetValue(data, "TextFrameworks", "IMGUI", config.TextFrameworksIMGUI.ToString().ToLower());

        // [Files]
        IniParser.SetValue(data, "Files", "Directory", config.FilesDirectory);
        IniParser.SetValue(data, "Files", "OutputFile", config.FilesOutputFile);
        IniParser.SetValue(data, "Files", "SubstitutionFile", config.FilesSubstitutionFile);
        IniParser.SetValue(data, "Files", "PreprocessorsFile", config.FilesPreprocessorsFile);
        IniParser.SetValue(data, "Files", "PostprocessorsFile", config.FilesPostprocessorsFile);

        // [Texture]
        IniParser.SetValue(data, "Texture", "Directory", config.TextureDirectory);
        IniParser.SetValue(data, "Texture", "EnableTranslation", config.TextureEnableTranslation.ToString().ToLower());
        IniParser.SetValue(data, "Texture", "EnableDumping", config.TextureEnableDumping.ToString().ToLower());
        IniParser.SetValue(data, "Texture", "HashGenerationStrategy", config.TextureHashGenerationStrategy);

        // [Advanced]
        IniParser.SetValue(data, "Advanced", "EnableTranslationScoping", config.AdvancedEnableTranslationScoping.ToString().ToLower());
        IniParser.SetValue(data, "Advanced", "HandleRichText", config.AdvancedHandleRichText.ToString().ToLower());
        IniParser.SetValue(data, "Advanced", "MaxTextParserRecursion", config.AdvancedMaxTextParserRecursion.ToString());
        IniParser.SetValue(data, "Advanced", "HtmlEntityPreprocessing", config.AdvancedHtmlEntityPreprocessing.ToString().ToLower());

        // [Authentication]
        IniParser.SetValue(data, "Authentication", "GoogleAPIKey", config.AuthenticationGoogleAPIKey);
        IniParser.SetValue(data, "Authentication", "BingSubscriptionKey", config.AuthenticationBingSubscriptionKey);
        IniParser.SetValue(data, "Authentication", "DeepLAPIKey", config.AuthenticationDeepLAPIKey);
        IniParser.SetValue(data, "Authentication", "BaiduAppId", config.AuthenticationBaiduAppId);
        IniParser.SetValue(data, "Authentication", "BaiduAppSecret", config.AuthenticationBaiduAppSecret);
        IniParser.SetValue(data, "Authentication", "YandexAPIKey", config.AuthenticationYandexAPIKey);

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
