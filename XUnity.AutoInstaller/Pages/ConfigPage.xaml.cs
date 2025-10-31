using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage.Pickers;
using System;
using System.Diagnostics;
using System.IO;
using XUnity.AutoInstaller.Models;
using XUnity.AutoInstaller.Services;
using XUnity.AutoInstaller.Utils;

namespace XUnity.AutoInstaller.Pages
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
                    ShowError("未设置游戏路径，请先在首页选择游戏目录，或点击下方按钮手动选择");
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
        /// 异步加载配置并填充 UI
        /// </summary>
        private async System.Threading.Tasks.Task LoadConfigurationAsync()
        {
            var gamePath = _gameStateService.CurrentGamePath;
            if (string.IsNullOrEmpty(gamePath) || !ConfigurationService.ValidateGamePath(gamePath))
            {
                ShowError("无效的游戏路径或 BepInEx 未安装");
                return;
            }

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
            ConsoleToggle.IsOn = config.LoggingConsoleEnabled;
            ConsoleShiftJISToggle.IsOn = config.LoggingConsoleShiftJISCompatible;
            PreloaderLogConsoleToggle.IsOn = config.PreloaderLogConsoleToUnityLog;

            // 日志级别
            var logLevels = config.ChainloaderLoggerDisplayedLevels;
            LoggerDisplayedLevelsComboBox.SelectedIndex = logLevels switch
            {
                var l when l.Contains("All") => 7,
                var l when l.Contains("Debug") => 6,
                var l when l.Contains("Info") => 5,
                var l when l.Contains("Message") => 4,
                var l when l.Contains("Warning") => 3,
                var l when l.Contains("Error") => 2,
                var l when l.Contains("Fatal") => 1,
                _ => 0
            };

            ChainloaderLogUnityMessagesToggle.IsOn = config.ChainloaderLogUnityMessages;

            // Preloader 高级设置
            EntrypointAssemblyTextBox.Text = config.PreloaderEntrypointAssembly;
            EntrypointTypeTextBox.Text = config.PreloaderEntrypointType;
            EntrypointMethodTextBox.Text = config.PreloaderEntrypointMethod;
            DumpAssembliesToggle.IsOn = config.PreloaderDumpAssemblies;
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
                _ => 0
            };

            FromLanguageComboBox.SelectedIndex = config.GeneralFromLanguage switch
            {
                "auto" => 0,
                "ja" => 1,
                "en" => 2,
                "ko" => 3,
                _ => 1
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
        }

        /// <summary>
        /// 从 UI 读取 BepInEx 配置
        /// </summary>
        private BepInExConfig ReadBepInExConfigFromUI()
        {
            var config = new BepInExConfig
            {
                LoggingConsoleEnabled = ConsoleToggle.IsOn,
                LoggingConsoleShiftJISCompatible = ConsoleShiftJISToggle.IsOn,
                PreloaderLogConsoleToUnityLog = PreloaderLogConsoleToggle.IsOn,
                ChainloaderLoggerDisplayedLevels = LoggerDisplayedLevelsComboBox.SelectedIndex switch
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
                PreloaderEntrypointAssembly = EntrypointAssemblyTextBox.Text,
                PreloaderEntrypointType = EntrypointTypeTextBox.Text,
                PreloaderEntrypointMethod = EntrypointMethodTextBox.Text,
                PreloaderDumpAssemblies = DumpAssembliesToggle.IsOn
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
                    _ => "zh-CN"
                },
                GeneralFromLanguage = FromLanguageComboBox.SelectedIndex switch
                {
                    2 => "en",
                    3 => "ko",
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
                AuthenticationYandexAPIKey = YandexAPIKeyTextBox.Text
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
