using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage.Pickers;
using System;
using System.Diagnostics;
using System.IO;
using XUnity_AutoInstaller.Models;
using XUnity_AutoInstaller.Services;
using XUnity_AutoInstaller.Utils;

namespace XUnity_AutoInstaller.Pages
{
    public sealed partial class ConfigPage : Page
    {
        private readonly GameStateService _gameStateService;
        private BepInExConfig? _bepinexConfig;
        private XUnityConfig? _xunityConfig;

        public ConfigPage()
        {
            this.InitializeComponent();
            _gameStateService = GameStateService.Instance;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // 异步加载配置
            DispatcherQueue.TryEnqueue(async () =>
            {
                // 从 GameStateService 获取游戏路径
                var gamePath = _gameStateService.CurrentGamePath;
                if (!string.IsNullOrEmpty(gamePath))
                {
                    await LoadConfigurationAsync();
                }
                else
                {
                    SetLockedState(true);
                }
            });
        }

        private async void BrowseGamePathButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FolderPicker();
                picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
                picker.FileTypeFilter.Add("*");

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                var folder = await picker.PickSingleFolderAsync();
                if (folder != null)
                {
                    var gamePath = folder.Path;
                    if (PathHelper.IsValidGameDirectory(gamePath))
                    {
                        _gameStateService.SetGamePath(gamePath, saveToSettings: true);
                        await LoadConfigurationAsync();
                    }
                    else
                    {
                        SetLockedState(true);
                        ShowError("所选目录不是有效的游戏目录");
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError($"选择文件夹失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置锁定状态
        /// </summary>
        private void SetLockedState(bool isLocked)
        {
            if (LockedPanel == null || ConfigContentPanel == null || 
                SaveButton == null || ResetButton == null || CancelButton == null ||
                OpenConfigFolderButton == null)
                return;

            if (isLocked)
            {
                LockedPanel.Visibility = Visibility.Visible;
                ConfigContentPanel.Visibility = Visibility.Collapsed;
                SaveButton.IsEnabled = false;
                ResetButton.IsEnabled = false;
                CancelButton.IsEnabled = false;
                OpenConfigFolderButton.IsEnabled = false;
            }
            else
            {
                LockedPanel.Visibility = Visibility.Collapsed;
                ConfigContentPanel.Visibility = Visibility.Visible;
                SaveButton.IsEnabled = true;
                ResetButton.IsEnabled = true;
                CancelButton.IsEnabled = true;
                OpenConfigFolderButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// 异步加载配置并填充 UI
        /// </summary>
        private async System.Threading.Tasks.Task LoadConfigurationAsync()
        {
            var gamePath = _gameStateService.CurrentGamePath;
            if (string.IsNullOrEmpty(gamePath) || !ConfigurationService.ValidateGamePath(gamePath))
            {
                SetLockedState(true);
                ShowError("无效的游戏路径或 BepInEx 未安装");
                return;
            }

            // 解锁界面
            SetLockedState(false);

            try
            {
                // 显示加载指示器，隐藏配置内容
                LoadingPanel.Visibility = Visibility.Visible;
                ConfigContentPanel.Visibility = Visibility.Collapsed;

                // 在后台线程加载配置文件
                var (bepinexConfig, xunityConfig) = await System.Threading.Tasks.Task.Run(() =>
                {
                    var bepinex = ConfigurationService.LoadBepInExConfig(gamePath);
                    var xunity = ConfigurationService.LoadXUnityConfig(gamePath);
                    return (bepinex, xunity);
                });

                // 在 UI 线程更新界面
                _bepinexConfig = bepinexConfig;
                _xunityConfig = xunityConfig;

                LoadBepInExConfigToUI(_bepinexConfig);
                LoadXUnityConfigToUI(_xunityConfig);

                // 隐藏加载指示器，显示配置内容
                LoadingPanel.Visibility = Visibility.Collapsed;
                ConfigContentPanel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                // 隐藏加载指示器
                LoadingPanel.Visibility = Visibility.Collapsed;
                ConfigContentPanel.Visibility = Visibility.Visible;

                ShowError($"加载配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 加载 BepInEx 配置到 UI
        /// </summary>
        private void LoadBepInExConfigToUI(BepInExConfig config)
        {
            // Caching
            EnableAssemblyCacheToggle.IsOn = config.CachingEnableAssemblyCache;

            // Chainloader
            HideManagerGameObjectToggle.IsOn = config.ChainloaderHideManagerGameObject;

            var chainloaderLogLevels = config.ChainloaderLogLevels;
            ChainloaderLogLevelsComboBox.SelectedIndex = chainloaderLogLevels switch
            {
                "All" => 7,
                var l when l.Contains("Debug") && l.Contains("Info") => 6,
                var l when l.Contains("Info") && l.Contains("Message") => 5,
                var l when l.Contains("Message") && l.Contains("Warning") => 4,
                var l when l.Contains("Warning") && l.Contains("Error") => 3,
                var l when l.Contains("Error") && l.Contains("Fatal") => 2,
                var l when l.Contains("Fatal") => 1,
                "None" => 0,
                _ => 5
            };

            ChainloaderLogUnityMessagesToggle.IsOn = config.ChainloaderLogUnityMessages;

            // Harmony.Logger
            var harmonyChannels = config.HarmonyLoggerLogChannels;
            HarmonyLogChannelsComboBox.SelectedIndex = harmonyChannels switch
            {
                "All" => 2,
                var l when l.Contains("Warn") || l.Contains("Error") => 1,
                "None" => 0,
                _ => 1
            };

            // Logging
            UnityLogListeningToggle.IsOn = config.LoggingUnityLogListening;
            LogConsoleToUnityLogToggle.IsOn = config.LoggingLogConsoleToUnityLog;

            // Logging.Console
            ConsoleToggle.IsOn = config.LoggingConsoleEnabled;
            ConsolePreventCloseToggle.IsOn = config.LoggingConsolePreventClose;
            ConsoleShiftJISToggle.IsOn = config.LoggingConsoleShiftJisEncoding;

            ConsoleStandardOutTypeComboBox.SelectedIndex = config.LoggingConsoleStandardOutType switch
            {
                "Auto" => 0,
                "ConsoleOut" => 1,
                "StandardOut" => 2,
                _ => 0
            };

            var consoleLogLevels = config.LoggingConsoleLogLevels;
            ConsoleLogLevelsComboBox.SelectedIndex = consoleLogLevels switch
            {
                "All" => 7,
                var l when l.Contains("Debug") && l.Contains("Info") => 6,
                var l when l.Contains("Info") && l.Contains("Message") => 5,
                var l when l.Contains("Message") && l.Contains("Warning") => 4,
                var l when l.Contains("Warning") && l.Contains("Error") => 3,
                var l when l.Contains("Error") && l.Contains("Fatal") => 2,
                var l when l.Contains("Fatal") => 1,
                "None" => 0,
                _ => 5
            };

            // Logging.Disk
            DiskWriteUnityLogToggle.IsOn = config.LoggingDiskWriteUnityLog;
            DiskAppendLogToggle.IsOn = config.LoggingDiskAppendLog;
            DiskEnabledToggle.IsOn = config.LoggingDiskEnabled;

            var diskLogLevels = config.LoggingDiskLogLevels;
            DiskLogLevelsComboBox.SelectedIndex = diskLogLevels switch
            {
                "All" => 7,
                var l when l.Contains("Debug") && l.Contains("Info") => 6,
                var l when l.Contains("Info") && l.Contains("Message") => 5,
                var l when l.Contains("Message") && l.Contains("Warning") => 4,
                var l when l.Contains("Warning") && l.Contains("Error") => 3,
                var l when l.Contains("Error") && l.Contains("Fatal") => 2,
                var l when l.Contains("Fatal") => 1,
                "None" => 0,
                _ => 5
            };

            // Preloader
            ApplyRuntimePatchesToggle.IsOn = config.PreloaderApplyRuntimePatches;

            HarmonyBackendComboBox.SelectedIndex = config.PreloaderHarmonyBackend switch
            {
                "auto" => 0,
                "dynamicmethod" => 1,
                "methodbuilder" => 2,
                "cecil" => 3,
                _ => 0
            };

            DumpAssembliesToggle.IsOn = config.PreloaderDumpAssemblies;
            LoadDumpedAssembliesToggle.IsOn = config.PreloaderLoadDumpedAssemblies;
            BreakBeforeLoadAssembliesToggle.IsOn = config.PreloaderBreakBeforeLoadAssemblies;
            PreloaderLogConsoleToUnityLogToggle.IsOn = config.PreloaderLogConsoleToUnityLog;

            // Preloader.Entrypoint
            EntrypointAssemblyTextBox.Text = config.PreloaderEntrypointAssembly;
            EntrypointTypeTextBox.Text = config.PreloaderEntrypointType;
            EntrypointMethodTextBox.Text = config.PreloaderEntrypointMethod;
        }

        /// <summary>
        /// 加载 XUnity 配置到 UI
        /// </summary>
        private void LoadXUnityConfigToUI(XUnityConfig config)
        {
            // Service - Use Tag-based mapping for more reliability
            var endpoint = config.ServiceEndpoint;
            var foundIndex = -1;
            for (int i = 0; i < EndpointComboBox.Items.Count; i++)
            {
                if (EndpointComboBox.Items[i] is ComboBoxItem item &&
                    item.Tag?.ToString() == endpoint)
                {
                    foundIndex = i;
                    break;
                }
            }
            EndpointComboBox.SelectedIndex = foundIndex >= 0 ? foundIndex : 1; // Default to GoogleTranslate if not found

            FallbackEndpointComboBox.SelectedIndex = string.IsNullOrEmpty(config.ServiceFallbackEndpoint) ? 0 : 1;

            // General
            LanguageComboBox.SelectedIndex = config.GeneralLanguage switch
            {
                "zh-CN" => 0,
                "zh-TW" => 1,
                "en" => 2,
                "ja" => 3,
                "ko" => 4,
                "fr" => 5,
                "de" => 6,
                "es" => 7,
                "ru" => 8,
                "pt" => 9,
                "it" => 10,
                "ar" => 11,
                "th" => 12,
                "vi" => 13,
                "id" => 14,
                "tr" => 15,
                "pl" => 16,
                "nl" => 17,
                "sv" => 18,
                "da" => 19,
                "fi" => 20,
                "no" => 21,
                "cs" => 22,
                "hu" => 23,
                "ro" => 24,
                "uk" => 25,
                "el" => 26,
                "he" => 27,
                "hi" => 28,
                _ => 0
            };

            FromLanguageComboBox.SelectedIndex = config.GeneralFromLanguage switch
            {
                "ja" => 0,
                "en" => 1,
                "ko" => 2,
                "zh-CN" => 3,
                "zh-TW" => 4,
                "fr" => 5,
                "de" => 6,
                "es" => 7,
                "ru" => 8,
                "pt" => 9,
                "it" => 10,
                "ar" => 11,
                "th" => 12,
                "vi" => 13,
                "id" => 14,
                "tr" => 15,
                "pl" => 16,
                "nl" => 17,
                "sv" => 18,
                "da" => 19,
                "fi" => 20,
                "no" => 21,
                "cs" => 22,
                "hu" => 23,
                "ro" => 24,
                "uk" => 25,
                "el" => 26,
                "he" => 27,
                "hi" => 28,
                _ => 0
            };

            MaxCharactersPerTranslationNumber.Value = config.GeneralMaxCharactersPerTranslation;
            MinDialogueCharsNumber.Value = config.GeneralMinDialogueChars;
            IgnoreWhitespaceInDialogueToggle.IsOn = config.GeneralIgnoreWhitespaceInDialogue;
            EnableUIResizingToggle.IsOn = config.GeneralEnableUIResizing;
            OverrideFontTextBox.Text = config.GeneralOverrideFont;
            CopyToClipboardToggle.IsOn = config.GeneralCopyToClipboard;

            // Text Frameworks
            EnableUGUIToggle.IsOn = config.TextFrameworksUGUI;
            EnableNGUIToggle.IsOn = config.TextFrameworksNGUI;
            EnableTextMeshProToggle.IsOn = config.TextFrameworksTextMeshPro;
            EnableTextMeshToggle.IsOn = config.TextFrameworksTextMesh;
            EnableIMGUIToggle.IsOn = config.TextFrameworksIMGUI;
            EnableFairyGUIToggle.IsOn = config.TextFrameworksFairyGUI;

            // Files
            DirectoryTextBox.Text = config.FilesDirectory;
            OutputFileTextBox.Text = config.FilesOutputFile;
            SubstitutionFileTextBox.Text = config.FilesSubstitutionFile;
            PreprocessorsFileTextBox.Text = config.FilesPreprocessorsFile;
            PostprocessorsFileTextBox.Text = config.FilesPostprocessorsFile;

            // Texture
            TextureDirectoryTextBox.Text = config.TextureDirectory;
            EnableTextureTranslationToggle.IsOn = config.TextureEnableTranslation;
            EnableTextureDumpingToggle.IsOn = config.TextureEnableDumping;
            TextureHashGenerationStrategyComboBox.SelectedIndex = config.TextureHashGenerationStrategy == "FromImageData" ? 1 : 0;

            // Advanced
            EnableTranslationScopingToggle.IsOn = config.AdvancedEnableTranslationScoping;
            HandleRichTextToggle.IsOn = config.AdvancedHandleRichText;
            MaxTextParserRecursionNumber.Value = config.AdvancedMaxTextParserRecursion;
            HtmlEntityPreprocessingToggle.IsOn = config.AdvancedHtmlEntityPreprocessing;

            // Authentication
            GoogleAPIKeyTextBox.Text = config.AuthenticationGoogleAPIKey;
            BingSubscriptionKeyTextBox.Text = config.AuthenticationBingSubscriptionKey;
            DeepLAPIKeyTextBox.Text = config.AuthenticationDeepLAPIKey;
            BaiduAppIdTextBox.Text = config.AuthenticationBaiduAppId;
            BaiduAppSecretTextBox.Text = config.AuthenticationBaiduAppSecret;
            YandexAPIKeyTextBox.Text = config.AuthenticationYandexAPIKey;
            WatsonAPIKeyTextBox.Text = config.AuthenticationWatsonAPIKey;
            LingoCloudTokenTextBox.Text = config.AuthenticationLingoCloudToken;

            // Behaviour (扩展)
            EnableBatchingToggle.IsOn = config.BehaviourEnableBatching;
            UseStaticTranslationsToggle.IsOn = config.BehaviourUseStaticTranslations;
            IgnoreTextStartingWithTextBox.Text = config.BehaviourIgnoreTextStartingWith;
            OutputUntranslatableTextToggle.IsOn = config.BehaviourOutputUntranslatableText;
            TranslationDelayNumber.Value = config.BehaviourDelay;

            // Behaviour扩展 - 新增项
            OverrideFontTextMeshProTextBox.Text = config.BehaviourOverrideFontTextMeshPro;
            FallbackFontTextMeshProTextBox.Text = config.BehaviourFallbackFontTextMeshPro;
            MaxClipboardCopyCharactersNumber.Value = config.BehaviourMaxClipboardCopyCharacters;
            ClipboardDebounceTimeNumber.Value = config.BehaviourClipboardDebounceTime;
            ForceUIResizingToggle.IsOn = config.BehaviourForceUIResizing;
            ResizeUILineSpacingScaleTextBox.Text = config.BehaviourResizeUILineSpacingScale;
            ForceSplitTextAfterCharactersNumber.Value = config.BehaviourForceSplitTextAfterCharacters;
            IgnoreWhitespaceInNGUIToggle.IsOn = config.BehaviourIgnoreWhitespaceInNGUI;

            // Behaviour高级后处理
            RomajiPostProcessingTextBox.Text = config.BehaviourRomajiPostProcessing;
            TranslationPostProcessingTextBox.Text = config.BehaviourTranslationPostProcessing;
            RegexPostProcessingTextBox.Text = config.BehaviourRegexPostProcessing;
            GameLogTextPathsTextBox.Text = config.BehaviourGameLogTextPaths;
            CacheRegexLookupsToggle.IsOn = config.BehaviourCacheRegexLookups;
            CacheWhitespaceDifferencesToggle.IsOn = config.BehaviourCacheWhitespaceDifferences;
            CacheRegexPatternResultsToggle.IsOn = config.BehaviourCacheRegexPatternResults;
            GenerateStaticSubstitutionTranslationsToggle.IsOn = config.BehaviourGenerateStaticSubstitutionTranslations;
            GeneratePartialTranslationsToggle.IsOn = config.BehaviourGeneratePartialTranslations;
            EnableSilentModeToggle.IsOn = config.BehaviourEnableSilentMode;
            BlacklistedIMGUIPluginsTextBox.Text = config.BehaviourBlacklistedIMGUIPlugins;
            IgnoreVirtualTextSetterCallingRulesToggle.IsOn = config.BehaviourIgnoreVirtualTextSetterCallingRules;
            OutputTooLongTextToggle.IsOn = config.BehaviourOutputTooLongText;
            OverrideFontSizeTextBox.Text = config.BehaviourOverrideFontSize;
            BehaviourTextGetterCompatibilityModeToggle.IsOn = config.BehaviourTextGetterCompatibilityMode;
            EnableTextPathLoggingToggle.IsOn = config.BehaviourEnableTextPathLogging;
            TemplateAllNumberAwayToggle.IsOn = config.BehaviourTemplateAllNumberAway;
            ReloadTranslationsOnFileChangeToggle.IsOn = config.BehaviourReloadTranslationsOnFileChange;
            DisableTextMeshProScrollInEffectsToggle.IsOn = config.BehaviourDisableTextMeshProScrollInEffects;
            CacheParsedTranslationsToggle.IsOn = config.BehaviourCacheParsedTranslations;

            // PersistRichTextMode ComboBox
            PersistRichTextModeComboBox.SelectedIndex = config.BehaviourPersistRichTextMode switch
            {
                "None" => 0,
                "Restored" => 1,
                "Final" => 2,
                _ => 2
            };

            // Behaviour开发者选项
            EnableTranslationHelperToggle.IsOn = config.BehaviourEnableTranslationHelper;
            ForceMonoModHooksToggle.IsOn = config.BehaviourForceMonoModHooks;
            InitializeHarmonyDetourBridgeToggle.IsOn = config.BehaviourInitializeHarmonyDetourBridge;
            RedirectedResourceDetectionStrategyTextBox.Text = config.BehaviourRedirectedResourceDetectionStrategy;

            // Texture扩展选项
            EnableTextureTogglingToggle.IsOn = config.TextureEnableTextureToggling;
            EnableTextureScanOnSceneLoadToggle.IsOn = config.TextureEnableTextureScanOnSceneLoad;
            EnableSpriteRendererHookingToggle.IsOn = config.TextureEnableSpriteRendererHooking;
            LoadUnmodifiedTexturesToggle.IsOn = config.TextureLoadUnmodifiedTextures;
            DetectDuplicateTextureNamesToggle.IsOn = config.TextureDetectDuplicateTextureNames;
            DuplicateTextureNamesTextBox.Text = config.TextureDuplicateTextureNames;
            EnableLegacyTextureLoadingToggle.IsOn = config.TextureEnableLegacyTextureLoading;
            CacheTexturesInMemoryToggle.IsOn = config.TextureCacheTexturesInMemory;
            EnableSpriteHookingToggle.IsOn = config.TextureEnableSpriteHooking;

            // ResourceRedirector扩展选项
            PreferredStoragePathTextBox.Text = config.ResourceRedirectorPreferredStoragePath;
            EnableTextAssetRedirectorToggle.IsOn = config.ResourceRedirectorEnableTextAssetRedirector;
            LogAllLoadedResourcesToggle.IsOn = config.ResourceRedirectorLogAllLoadedResources;
            ResourceRedirectorEnableDumpingToggle.IsOn = config.ResourceRedirectorEnableDumping;
            CacheMetadataForAllFilesToggle.IsOn = config.ResourceRedirectorCacheMetadataForAllFiles;

            // Authentication扩展
            WatsonUrlTextBox.Text = config.WatsonUrl;
            WatsonKeyTextBox.Text = config.WatsonKey;
            DeepLFreeToggle.IsOn = config.DeepLLegitimateFree;
            DeepLLegitimateExecutableLocationTextBox.Text = config.DeepLLegitimateExecutableLocation;
            BaiduDelaySecondsNumber.Value = config.BaiduDelaySeconds;
            LingoCloudLingoCloudTokenTextBox.Text = config.LingoCloudLingoCloudToken;

            // 翻译服务高级配置
            GoogleServiceUrlTextBox.Text = config.GoogleServiceUrl;
            DeepLMinDelayNumber.Value = config.DeepLMinDelay;
            DeepLMaxDelayNumber.Value = config.DeepLMaxDelay;
            DeepLExecutableLocationTextBox.Text = config.DeepLExecutableLocation;
            DeepLMinDelaySecondsNumber.Value = config.DeepLMinDelaySeconds;
            DeepLMaxDelaySecondsNumber.Value = config.DeepLMaxDelaySeconds;
            CustomUrlTextBox.Text = config.CustomUrl;
            CustomEnableShortDelayToggle.IsOn = config.CustomEnableShortDelay;
            CustomDisableSpamChecksToggle.IsOn = config.CustomDisableSpamChecks;
            LecPowerTranslator15InstallationPathTextBox.Text = config.LecPowerTranslator15InstallationPath;
            EzTransInstallationPathTextBox.Text = config.EzTransInstallationPath;
            GoogleV2ServiceUrlTextBox.Text = config.GoogleV2ServiceUrl;
            GoogleV2RPCIDTextBox.Text = config.GoogleV2RPCID;
            GoogleV2VersionTextBox.Text = config.GoogleV2Version;
            GoogleV2UseSimplestToggle.IsOn = config.GoogleV2UseSimplest;

            // Translation Aggregator
            TranslationAggregatorWidthNumber.Value = config.TranslationAggregatorWidth;
            TranslationAggregatorHeightNumber.Value = config.TranslationAggregatorHeight;
            EnabledTranslatorsTextBox.Text = config.TranslationAggregatorEnabledTranslators;

            // Migrations
            MigrationsEnableToggle.IsOn = config.MigrationsEnable;
            MigrationsTagTextBox.Text = config.MigrationsTag;

            // Http
            HttpUserAgentTextBox.Text = config.HttpUserAgent;
            DisableCertificateChecksToggle.IsOn = config.HttpDisableCertificateChecks;
            DisableCertificateValidationToggle.IsOn = config.HttpDisableCertificateValidation;

            // Debug
            DebugEnableConsoleToggle.IsOn = config.DebugEnableConsole;
            DebugEnableLogToggle.IsOn = config.DebugEnableLog;

            // Optimization
            EnableCacheToggle.IsOn = config.OptimizationEnableCache;
            MaxCacheEntriesNumber.Value = config.OptimizationMaxCacheEntries;

            // Integration
            TextGetterCompatibilityModeToggle.IsOn = config.IntegrationTextGetterCompatibilityMode;

            // ResourceRedirector
            EnableRedirectorToggle.IsOn = config.ResourceRedirectorEnableRedirector;
            DetectDuplicateResourcesToggle.IsOn = config.ResourceRedirectorDetectDuplicateResources;
        }

        /// <summary>
        /// 从 UI 读取 BepInEx 配置
        /// </summary>
        private BepInExConfig ReadBepInExConfigFromUI()
        {
            var config = new BepInExConfig
            {
                // Caching
                CachingEnableAssemblyCache = EnableAssemblyCacheToggle.IsOn,

                // Chainloader
                ChainloaderHideManagerGameObject = HideManagerGameObjectToggle.IsOn,
                ChainloaderLogLevels = ChainloaderLogLevelsComboBox.SelectedIndex switch
                {
                    0 => "None",
                    1 => "Fatal",
                    2 => "Error,Fatal",
                    3 => "Warning,Error,Fatal",
                    4 => "Message,Warning,Error,Fatal",
                    5 => "Info,Message,Warning,Error,Fatal",
                    6 => "Debug,Info,Message,Warning,Error,Fatal",
                    7 => "All",
                    _ => "Info,Message,Warning,Error,Fatal"
                },
                ChainloaderLogUnityMessages = ChainloaderLogUnityMessagesToggle.IsOn,

                // Harmony.Logger
                HarmonyLoggerLogChannels = HarmonyLogChannelsComboBox.SelectedIndex switch
                {
                    0 => "None",
                    1 => "Warn, Error",
                    2 => "All",
                    _ => "Warn, Error"
                },

                // Logging
                LoggingUnityLogListening = UnityLogListeningToggle.IsOn,
                LoggingLogConsoleToUnityLog = LogConsoleToUnityLogToggle.IsOn,

                // Logging.Console
                LoggingConsoleEnabled = ConsoleToggle.IsOn,
                LoggingConsolePreventClose = ConsolePreventCloseToggle.IsOn,
                LoggingConsoleShiftJisEncoding = ConsoleShiftJISToggle.IsOn,
                LoggingConsoleStandardOutType = ConsoleStandardOutTypeComboBox.SelectedIndex switch
                {
                    0 => "Auto",
                    1 => "ConsoleOut",
                    2 => "StandardOut",
                    _ => "Auto"
                },
                LoggingConsoleLogLevels = ConsoleLogLevelsComboBox.SelectedIndex switch
                {
                    0 => "None",
                    1 => "Fatal",
                    2 => "Error,Fatal",
                    3 => "Warning,Error,Fatal",
                    4 => "Message,Warning,Error,Fatal",
                    5 => "Info,Message,Warning,Error,Fatal",
                    6 => "Debug,Info,Message,Warning,Error,Fatal",
                    7 => "All",
                    _ => "Info,Message,Warning,Error,Fatal"
                },

                // Logging.Disk
                LoggingDiskWriteUnityLog = DiskWriteUnityLogToggle.IsOn,
                LoggingDiskAppendLog = DiskAppendLogToggle.IsOn,
                LoggingDiskEnabled = DiskEnabledToggle.IsOn,
                LoggingDiskLogLevels = DiskLogLevelsComboBox.SelectedIndex switch
                {
                    0 => "None",
                    1 => "Fatal",
                    2 => "Error,Fatal",
                    3 => "Warning,Error,Fatal",
                    4 => "Message,Warning,Error,Fatal",
                    5 => "Info,Message,Warning,Error,Fatal",
                    6 => "Debug,Info,Message,Warning,Error,Fatal",
                    7 => "All",
                    _ => "Info,Message,Warning,Error,Fatal"
                },

                // Preloader
                PreloaderApplyRuntimePatches = ApplyRuntimePatchesToggle.IsOn,
                PreloaderHarmonyBackend = HarmonyBackendComboBox.SelectedIndex switch
                {
                    0 => "auto",
                    1 => "dynamicmethod",
                    2 => "methodbuilder",
                    3 => "cecil",
                    _ => "auto"
                },
                PreloaderDumpAssemblies = DumpAssembliesToggle.IsOn,
                PreloaderLoadDumpedAssemblies = LoadDumpedAssembliesToggle.IsOn,
                PreloaderBreakBeforeLoadAssemblies = BreakBeforeLoadAssembliesToggle.IsOn,
                PreloaderLogConsoleToUnityLog = PreloaderLogConsoleToUnityLogToggle.IsOn,

                // Preloader.Entrypoint
                PreloaderEntrypointAssembly = EntrypointAssemblyTextBox.Text,
                PreloaderEntrypointType = EntrypointTypeTextBox.Text,
                PreloaderEntrypointMethod = EntrypointMethodTextBox.Text
            };

            return config;
        }

        /// <summary>
        /// 从 UI 读取 XUnity 配置
        /// </summary>
        private XUnityConfig ReadXUnityConfigFromUI()
        {
            var config = new XUnityConfig
            {
                // Service - Use Tag-based mapping
                ServiceEndpoint = EndpointComboBox.SelectedItem is ComboBoxItem selectedEndpoint &&
                                  selectedEndpoint.Tag != null
                    ? selectedEndpoint.Tag.ToString() ?? "GoogleTranslate"
                    : "GoogleTranslate",
                ServiceFallbackEndpoint = FallbackEndpointComboBox.SelectedIndex > 0 ? "BingTranslate" : "",
                GeneralLanguage = LanguageComboBox.SelectedIndex switch
                {
                    1 => "zh-TW",
                    2 => "en",
                    3 => "ja",
                    4 => "ko",
                    5 => "fr",
                    6 => "de",
                    7 => "es",
                    8 => "ru",
                    9 => "pt",
                    10 => "it",
                    11 => "ar",
                    12 => "th",
                    13 => "vi",
                    14 => "id",
                    15 => "tr",
                    16 => "pl",
                    17 => "nl",
                    18 => "sv",
                    19 => "da",
                    20 => "fi",
                    21 => "no",
                    22 => "cs",
                    23 => "hu",
                    24 => "ro",
                    25 => "uk",
                    26 => "el",
                    27 => "he",
                    28 => "hi",
                    _ => "zh-CN"
                },
                GeneralFromLanguage = FromLanguageComboBox.SelectedIndex switch
                {
                    1 => "en",
                    2 => "ko",
                    3 => "zh-CN",
                    4 => "zh-TW",
                    5 => "fr",
                    6 => "de",
                    7 => "es",
                    8 => "ru",
                    9 => "pt",
                    10 => "it",
                    11 => "ar",
                    12 => "th",
                    13 => "vi",
                    14 => "id",
                    15 => "tr",
                    16 => "pl",
                    17 => "nl",
                    18 => "sv",
                    19 => "da",
                    20 => "fi",
                    21 => "no",
                    22 => "cs",
                    23 => "hu",
                    24 => "ro",
                    25 => "uk",
                    26 => "el",
                    27 => "he",
                    28 => "hi",
                    _ => "ja"
                },
                GeneralMaxCharactersPerTranslation = (int)MaxCharactersPerTranslationNumber.Value,
                GeneralMinDialogueChars = (int)MinDialogueCharsNumber.Value,
                GeneralIgnoreWhitespaceInDialogue = IgnoreWhitespaceInDialogueToggle.IsOn,
                GeneralEnableUIResizing = EnableUIResizingToggle.IsOn,
                GeneralOverrideFont = OverrideFontTextBox.Text,
                GeneralCopyToClipboard = CopyToClipboardToggle.IsOn,
                TextFrameworksUGUI = EnableUGUIToggle.IsOn,
                TextFrameworksNGUI = EnableNGUIToggle.IsOn,
                TextFrameworksTextMeshPro = EnableTextMeshProToggle.IsOn,
                TextFrameworksTextMesh = EnableTextMeshToggle.IsOn,
                TextFrameworksIMGUI = EnableIMGUIToggle.IsOn,
                TextFrameworksFairyGUI = EnableFairyGUIToggle.IsOn,
                FilesDirectory = DirectoryTextBox.Text,
                FilesOutputFile = OutputFileTextBox.Text,
                FilesSubstitutionFile = SubstitutionFileTextBox.Text,
                FilesPreprocessorsFile = PreprocessorsFileTextBox.Text,
                FilesPostprocessorsFile = PostprocessorsFileTextBox.Text,
                TextureDirectory = TextureDirectoryTextBox.Text,
                TextureEnableTranslation = EnableTextureTranslationToggle.IsOn,
                TextureEnableDumping = EnableTextureDumpingToggle.IsOn,
                TextureHashGenerationStrategy = TextureHashGenerationStrategyComboBox.SelectedIndex == 1 ? "FromImageData" : "FromImageName",
                AdvancedEnableTranslationScoping = EnableTranslationScopingToggle.IsOn,
                AdvancedHandleRichText = HandleRichTextToggle.IsOn,
                AdvancedMaxTextParserRecursion = (int)MaxTextParserRecursionNumber.Value,
                AdvancedHtmlEntityPreprocessing = HtmlEntityPreprocessingToggle.IsOn,
                AuthenticationGoogleAPIKey = GoogleAPIKeyTextBox.Text,
                AuthenticationBingSubscriptionKey = BingSubscriptionKeyTextBox.Text,
                AuthenticationDeepLAPIKey = DeepLAPIKeyTextBox.Text,
                AuthenticationBaiduAppId = BaiduAppIdTextBox.Text,
                AuthenticationBaiduAppSecret = BaiduAppSecretTextBox.Text,
                AuthenticationYandexAPIKey = YandexAPIKeyTextBox.Text,
                AuthenticationWatsonAPIKey = WatsonAPIKeyTextBox.Text,
                AuthenticationLingoCloudToken = LingoCloudTokenTextBox.Text,

                // Behaviour (扩展)
                BehaviourEnableBatching = EnableBatchingToggle.IsOn,
                BehaviourUseStaticTranslations = UseStaticTranslationsToggle.IsOn,
                BehaviourIgnoreTextStartingWith = IgnoreTextStartingWithTextBox.Text,
                BehaviourOutputUntranslatableText = OutputUntranslatableTextToggle.IsOn,
                BehaviourDelay = (int)TranslationDelayNumber.Value,

                // Behaviour扩展 - 新增项
                BehaviourOverrideFontTextMeshPro = OverrideFontTextMeshProTextBox.Text,
                BehaviourFallbackFontTextMeshPro = FallbackFontTextMeshProTextBox.Text,
                BehaviourMaxClipboardCopyCharacters = (int)MaxClipboardCopyCharactersNumber.Value,
                BehaviourClipboardDebounceTime = (float)ClipboardDebounceTimeNumber.Value,
                BehaviourForceUIResizing = ForceUIResizingToggle.IsOn,
                BehaviourResizeUILineSpacingScale = ResizeUILineSpacingScaleTextBox.Text,
                BehaviourForceSplitTextAfterCharacters = (int)ForceSplitTextAfterCharactersNumber.Value,
                BehaviourIgnoreWhitespaceInNGUI = IgnoreWhitespaceInNGUIToggle.IsOn,

                // Behaviour高级后处理
                BehaviourRomajiPostProcessing = RomajiPostProcessingTextBox.Text,
                BehaviourTranslationPostProcessing = TranslationPostProcessingTextBox.Text,
                BehaviourRegexPostProcessing = RegexPostProcessingTextBox.Text,
                BehaviourGameLogTextPaths = GameLogTextPathsTextBox.Text,
                BehaviourCacheRegexLookups = CacheRegexLookupsToggle.IsOn,
                BehaviourCacheWhitespaceDifferences = CacheWhitespaceDifferencesToggle.IsOn,
                BehaviourCacheRegexPatternResults = CacheRegexPatternResultsToggle.IsOn,
                BehaviourGenerateStaticSubstitutionTranslations = GenerateStaticSubstitutionTranslationsToggle.IsOn,
                BehaviourGeneratePartialTranslations = GeneratePartialTranslationsToggle.IsOn,
                BehaviourEnableSilentMode = EnableSilentModeToggle.IsOn,
                BehaviourBlacklistedIMGUIPlugins = BlacklistedIMGUIPluginsTextBox.Text,
                BehaviourIgnoreVirtualTextSetterCallingRules = IgnoreVirtualTextSetterCallingRulesToggle.IsOn,
                BehaviourOutputTooLongText = OutputTooLongTextToggle.IsOn,
                BehaviourOverrideFontSize = OverrideFontSizeTextBox.Text,
                BehaviourTextGetterCompatibilityMode = BehaviourTextGetterCompatibilityModeToggle.IsOn,
                BehaviourEnableTextPathLogging = EnableTextPathLoggingToggle.IsOn,
                BehaviourTemplateAllNumberAway = TemplateAllNumberAwayToggle.IsOn,
                BehaviourReloadTranslationsOnFileChange = ReloadTranslationsOnFileChangeToggle.IsOn,
                BehaviourDisableTextMeshProScrollInEffects = DisableTextMeshProScrollInEffectsToggle.IsOn,
                BehaviourCacheParsedTranslations = CacheParsedTranslationsToggle.IsOn,

                // PersistRichTextMode
                BehaviourPersistRichTextMode = PersistRichTextModeComboBox.SelectedIndex switch
                {
                    0 => "None",
                    1 => "Restored",
                    2 => "Final",
                    _ => "Final"
                },

                // Behaviour开发者选项
                BehaviourEnableTranslationHelper = EnableTranslationHelperToggle.IsOn,
                BehaviourForceMonoModHooks = ForceMonoModHooksToggle.IsOn,
                BehaviourInitializeHarmonyDetourBridge = InitializeHarmonyDetourBridgeToggle.IsOn,
                BehaviourRedirectedResourceDetectionStrategy = RedirectedResourceDetectionStrategyTextBox.Text,

                // Texture扩展选项
                TextureEnableTextureToggling = EnableTextureTogglingToggle.IsOn,
                TextureEnableTextureScanOnSceneLoad = EnableTextureScanOnSceneLoadToggle.IsOn,
                TextureEnableSpriteRendererHooking = EnableSpriteRendererHookingToggle.IsOn,
                TextureLoadUnmodifiedTextures = LoadUnmodifiedTexturesToggle.IsOn,
                TextureDetectDuplicateTextureNames = DetectDuplicateTextureNamesToggle.IsOn,
                TextureDuplicateTextureNames = DuplicateTextureNamesTextBox.Text,
                TextureEnableLegacyTextureLoading = EnableLegacyTextureLoadingToggle.IsOn,
                TextureCacheTexturesInMemory = CacheTexturesInMemoryToggle.IsOn,
                TextureEnableSpriteHooking = EnableSpriteHookingToggle.IsOn,

                // ResourceRedirector扩展选项
                ResourceRedirectorPreferredStoragePath = PreferredStoragePathTextBox.Text,
                ResourceRedirectorEnableTextAssetRedirector = EnableTextAssetRedirectorToggle.IsOn,
                ResourceRedirectorLogAllLoadedResources = LogAllLoadedResourcesToggle.IsOn,
                ResourceRedirectorEnableDumping = ResourceRedirectorEnableDumpingToggle.IsOn,
                ResourceRedirectorCacheMetadataForAllFiles = CacheMetadataForAllFilesToggle.IsOn,

                // Authentication扩展
                WatsonUrl = WatsonUrlTextBox.Text,
                WatsonKey = WatsonKeyTextBox.Text,
                DeepLLegitimateFree = DeepLFreeToggle.IsOn,
                DeepLLegitimateExecutableLocation = DeepLLegitimateExecutableLocationTextBox.Text,
                BaiduDelaySeconds = (int)BaiduDelaySecondsNumber.Value,
                LingoCloudLingoCloudToken = LingoCloudLingoCloudTokenTextBox.Text,

                // 翻译服务高级配置
                GoogleServiceUrl = GoogleServiceUrlTextBox.Text,
                DeepLMinDelay = (int)DeepLMinDelayNumber.Value,
                DeepLMaxDelay = (int)DeepLMaxDelayNumber.Value,
                DeepLExecutableLocation = DeepLExecutableLocationTextBox.Text,
                DeepLMinDelaySeconds = (int)DeepLMinDelaySecondsNumber.Value,
                DeepLMaxDelaySeconds = (int)DeepLMaxDelaySecondsNumber.Value,
                CustomUrl = CustomUrlTextBox.Text,
                CustomEnableShortDelay = CustomEnableShortDelayToggle.IsOn,
                CustomDisableSpamChecks = CustomDisableSpamChecksToggle.IsOn,
                LecPowerTranslator15InstallationPath = LecPowerTranslator15InstallationPathTextBox.Text,
                EzTransInstallationPath = EzTransInstallationPathTextBox.Text,
                GoogleV2ServiceUrl = GoogleV2ServiceUrlTextBox.Text,
                GoogleV2RPCID = GoogleV2RPCIDTextBox.Text,
                GoogleV2Version = GoogleV2VersionTextBox.Text,
                GoogleV2UseSimplest = GoogleV2UseSimplestToggle.IsOn,

                // Translation Aggregator
                TranslationAggregatorWidth = (int)TranslationAggregatorWidthNumber.Value,
                TranslationAggregatorHeight = (int)TranslationAggregatorHeightNumber.Value,
                TranslationAggregatorEnabledTranslators = EnabledTranslatorsTextBox.Text,

                // Migrations
                MigrationsEnable = MigrationsEnableToggle.IsOn,
                MigrationsTag = MigrationsTagTextBox.Text,

                // Http
                HttpUserAgent = HttpUserAgentTextBox.Text,
                HttpDisableCertificateChecks = DisableCertificateChecksToggle.IsOn,
                HttpDisableCertificateValidation = DisableCertificateValidationToggle.IsOn,

                // Debug
                DebugEnableConsole = DebugEnableConsoleToggle.IsOn,
                DebugEnableLog = DebugEnableLogToggle.IsOn,

                // Optimization
                OptimizationEnableCache = EnableCacheToggle.IsOn,
                OptimizationMaxCacheEntries = (int)MaxCacheEntriesNumber.Value,

                // Integration
                IntegrationTextGetterCompatibilityMode = TextGetterCompatibilityModeToggle.IsOn,

                // ResourceRedirector
                ResourceRedirectorEnableRedirector = EnableRedirectorToggle.IsOn,
                ResourceRedirectorDetectDuplicateResources = DetectDuplicateResourcesToggle.IsOn
            };

            return config;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var gamePath = _gameStateService.CurrentGamePath;
            if (string.IsNullOrEmpty(gamePath))
            {
                ShowError("游戏路径未设置");
                return;
            }

            try
            {
                // 读取 UI 配置
                var bepinexConfig = ReadBepInExConfigFromUI();
                var xunityConfig = ReadXUnityConfigFromUI();

                // 保存配置
                ConfigurationService.SaveBepInExConfig(gamePath, bepinexConfig);
                ConfigurationService.SaveXUnityConfig(gamePath, xunityConfig);

                // 更新内部引用
                _bepinexConfig = bepinexConfig;
                _xunityConfig = xunityConfig;

                // 显示成功对话框
                if (this.XamlRoot != null)
                {
                    var dialog = new ContentDialog
                    {
                        Title = "保存成功",
                        Content = "配置已成功保存",
                        CloseButtonText = "确定",
                        XamlRoot = this.XamlRoot
                    };
                    await dialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                ShowError($"保存配置失败: {ex.Message}");
            }
        }

        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // 重新加载配置
            await LoadConfigurationAsync();
        }

        private async void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.XamlRoot == null) return;

            var dialog = new ContentDialog
            {
                Title = "重置配置",
                Content = "确定要重置为默认配置吗？此操作不可撤销。",
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                // 加载默认配置
                _bepinexConfig = BepInExConfig.CreateDefault();
                _xunityConfig = XUnityConfig.CreateRecommended();

                // 更新 UI
                LoadBepInExConfigToUI(_bepinexConfig);
                LoadXUnityConfigToUI(_xunityConfig);
            }
        }

        private void OpenConfigFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var gamePath = _gameStateService.CurrentGamePath;
            if (string.IsNullOrEmpty(gamePath))
            {
                ShowError("游戏路径未设置");
                return;
            }

            try
            {
                var configPath = PathHelper.GetBepInExConfigPath(gamePath);

                if (!Directory.Exists(configPath))
                {
                    ShowError("配置文件夹不存在");
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = configPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                ShowError($"打开文件夹失败: {ex.Message}");
            }
        }

        private async void BrowseDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FolderPicker();
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                picker.FileTypeFilter.Add("*");

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                var folder = await picker.PickSingleFolderAsync();
                if (folder != null)
                {
                    DirectoryTextBox.Text = folder.Path;
                }
            }
            catch (Exception ex)
            {
                ShowError($"选择文件夹失败: {ex.Message}");
            }
        }

        private async void BrowseTextureDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FolderPicker();
                picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                picker.FileTypeFilter.Add("*");

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                var folder = await picker.PickSingleFolderAsync();
                if (folder != null)
                {
                    TextureDirectoryTextBox.Text = folder.Path;
                }
            }
            catch (Exception ex)
            {
                ShowError($"选择文件夹失败: {ex.Message}");
            }
        }

        private async void BrowseEzTransPathButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FolderPicker();
                picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
                picker.FileTypeFilter.Add("*");

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                var folder = await picker.PickSingleFolderAsync();
                if (folder != null)
                {
                    EzTransInstallationPathTextBox.Text = folder.Path;
                }
            }
            catch (Exception ex)
            {
                ShowError($"选择文件夹失败: {ex.Message}");
            }
        }

        private async void BrowseLecPathButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FolderPicker();
                picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
                picker.FileTypeFilter.Add("*");

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                var folder = await picker.PickSingleFolderAsync();
                if (folder != null)
                {
                    LecPowerTranslator15InstallationPathTextBox.Text = folder.Path;
                }
            }
            catch (Exception ex)
            {
                ShowError($"选择文件夹失败: {ex.Message}");
            }
        }

        private void ShowError(string message)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                ErrorInfoBar.Message = message;
                ErrorInfoBar.IsOpen = true;
            });
        }
    }
}
