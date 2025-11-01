using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using XUnity_AutoInstaller.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace XUnity_AutoInstaller
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;

        /// <summary>
        /// 获取主窗口实例（用于 FolderPicker 等需要 HWND 的场景）
        /// </summary>
        public static Window? MainWindow { get; private set; }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            // For unpackaged apps, Bootstrap.Initialize is called in Program.cs Main() before this
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
            _window = new MainWindow();
            MainWindow = _window;

            // Initialize GameStateService and auto-load last game path if enabled
            GameStateService.Instance.Initialize();

            // Initialize VersionCacheService in background (don't block UI startup)
            _ = InitializeVersionCacheAsync();

            _window.Activate();
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
    }
}
