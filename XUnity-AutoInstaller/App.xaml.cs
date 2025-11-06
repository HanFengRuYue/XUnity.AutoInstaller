using System;
using System.Net.Http;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using XUnity_AutoInstaller.Services;
using XUnity_AutoInstaller.ViewModels;

namespace XUnity_AutoInstaller
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// 获取主窗口实例（用于 FolderPicker 等需要 HWND 的场景）
        /// </summary>
        public static Window? MainWindow { get; private set; }

        /// <summary>
        /// 获取应用程序的服务提供者（用于依赖注入）
        /// </summary>
        public static IServiceProvider Services { get; private set; } = null!;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            // Required for PublishSingleFile with framework-dependent deployment
            // This ensures Windows App Runtime can locate its files when bundled in single-file exe
            Environment.SetEnvironmentVariable("MICROSOFT_WINDOWSAPPRUNTIME_BASE_DIRECTORY", AppContext.BaseDirectory);

            // Configure dependency injection container
            Services = ConfigureServices();

            InitializeComponent();

            // 添加全局异常处理器
            this.UnhandledException += App_UnhandledException;
        }

        /// <summary>
        /// 全局未处理异常处理器
        /// </summary>
        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            // 记录异常
            LogService.Instance.Log($"未处理的异常: {e.Exception.GetType().Name}", LogLevel.Error, "[App]");
            LogService.Instance.Log($"异常消息: {e.Exception.Message}", LogLevel.Error, "[App]");
            LogService.Instance.Log($"堆栈跟踪: {e.Exception.StackTrace}", LogLevel.Error, "[App]");

            // 标记为已处理，防止应用崩溃
            e.Handled = true;

            // 显示友好的错误消息（在 UI 线程上）
            if (MainWindow != null)
            {
                try
                {
                    MainWindow.DispatcherQueue.TryEnqueue(async () =>
                    {
                        var dialog = new ContentDialog
                        {
                            Title = "应用程序错误",
                            Content = $"XUnity-AutoInstaller 遇到意外错误，但已被安全处理。\n\n错误详情：\n{e.Exception.Message}\n\n您可以继续使用应用程序，或查看日志页面了解详细信息。",
                            CloseButtonText = "确定",
                            XamlRoot = MainWindow.Content.XamlRoot
                        };

                        if (dialog.XamlRoot != null)
                        {
                            await dialog.ShowAsync();
                        }
                    });
                }
                catch
                {
                    // 如果无法显示对话框，至少不要崩溃
                }
            }
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            MainWindow = new MainWindow();

            // Initialize GameStateService and auto-load last game path if enabled
            GameStateService.Instance.Initialize();

            // Initialize VersionCacheService in background (don't block UI startup)
            _ = InitializeVersionCacheAsync();

            MainWindow.Activate();
        }

        /// <summary>
        /// 在后台初始化版本缓存
        /// </summary>
        private async System.Threading.Tasks.Task InitializeVersionCacheAsync()
        {
            try
            {
                LogService.Instance.Log("开始后台初始化版本缓存...", LogLevel.Info, "[App]");

                // 设置 30 秒超时，防止网络问题导致长时间挂起
                using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(30));

                var initTask = VersionCacheService.Instance.InitializeAsync();
                var completedTask = await System.Threading.Tasks.Task.WhenAny(
                    initTask,
                    System.Threading.Tasks.Task.Delay(Timeout.Infinite, cts.Token)
                );

                if (completedTask == initTask)
                {
                    // 初始化完成，获取结果（如果有异常会在这里抛出）
                    await initTask;
                    LogService.Instance.Log("版本缓存后台初始化成功", LogLevel.Info, "[App]");
                }
                else
                {
                    // 超时
                    LogService.Instance.Log("版本缓存初始化超时（30秒），将在后续使用时重试", LogLevel.Warning, "[App]");
                }
            }
            catch (System.Threading.Tasks.TaskCanceledException)
            {
                LogService.Instance.Log("版本缓存初始化被取消（超时）", LogLevel.Warning, "[App]");
            }
            catch (HttpRequestException httpEx)
            {
                // 网络异常
                LogService.Instance.Log($"版本缓存初始化失败 - 网络错误: {httpEx.Message}", LogLevel.Warning, "[App]");
                LogService.Instance.Log("应用程序将正常启动，版本列表功能可能受限。您可以稍后在版本管理页面手动刷新。", LogLevel.Info, "[App]");
            }
            catch (Exception ex)
            {
                // 其他异常
                LogService.Instance.Log($"版本缓存后台初始化失败: {ex.GetType().Name} - {ex.Message}", LogLevel.Warning, "[App]");
                LogService.Instance.Log($"详细信息: {ex.StackTrace}", LogLevel.Debug, "[App]");
                LogService.Instance.Log("应用程序将正常启动，版本列表功能可能受限。您可以稍后在版本管理页面手动刷新。", LogLevel.Info, "[App]");
            }
        }

        /// <summary>
        /// 配置依赖注入服务
        /// </summary>
        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            // 注册单例服务（已存在的全局单例）
            services.AddSingleton<GameStateService>(GameStateService.Instance);
            services.AddSingleton<LogService>(LogService.Instance);
            services.AddSingleton<VersionCacheService>(VersionCacheService.Instance);
            services.AddSingleton<InstallationStateService>(InstallationStateService.Instance);
            services.AddSingleton<SettingsService>(); // SettingsService 没有 Instance，创建新实例

            // 注册 Transient 服务（每次请求创建新实例）
            services.AddTransient<VersionService>();
            services.AddTransient<InstallationService>();
            services.AddTransient<ConfigurationService>();
            services.AddTransient<FontManagementService>();

            // 注册 ViewModels（每次请求创建新实例）
            services.AddTransient<InstallViewModel>();
            services.AddTransient<VersionManagementViewModel>();
            services.AddTransient<FontDownloadViewModel>();
            services.AddTransient<DashboardViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<LogViewModel>();
            services.AddTransient<ConfigViewModel>();

            return services.BuildServiceProvider();
        }
    }
}
