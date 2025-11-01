using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using XUnity_AutoInstaller.Utils;

namespace XUnity_AutoInstaller.Services;

/// <summary>
/// 游戏启动服务
/// 负责启动游戏并等待配置文件生成
/// </summary>
public class GameLauncherService
{
    private readonly LogWriter? _logger;

    public GameLauncherService(LogWriter? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// 启动游戏并等待BepInEx配置文件生成
    /// </summary>
    /// <param name="gamePath">游戏根目录路径</param>
    /// <param name="timeoutSeconds">超时时间（秒），默认60秒</param>
    /// <param name="progress">进度报告</param>
    /// <returns>是否成功生成配置文件</returns>
    public async Task<bool> LaunchGameAndWaitForConfigGenerationAsync(
        string gamePath,
        int timeoutSeconds = 60,
        IProgress<(int percentage, string message)>? progress = null)
    {
        try
        {
            _logger?.Info("开始启动游戏生成配置文件");
            progress?.Report((0, "查找游戏可执行文件..."));

            // 1. 获取游戏信息
            var gameInfo = GameDetectionService.GetGameInfo(gamePath);
            if (string.IsNullOrEmpty(gameInfo.ExecutablePath) || !File.Exists(gameInfo.ExecutablePath))
            {
                _logger?.Error("未找到游戏可执行文件");
                return false;
            }

            _logger?.Info($"找到游戏可执行文件: {gameInfo.ExecutablePath}");

            // 2. 准备监控的配置文件路径
            var configPath = PathHelper.GetBepInExConfigPath(gamePath);
            var bepinexConfigFile = PathHelper.GetBepInExConfigFile(gamePath);
            var xunityConfigFile = PathHelper.GetXUnityConfigFile(gamePath);

            _logger?.Info($"等待配置文件生成: {bepinexConfigFile}");
            _logger?.Info($"等待配置文件生成: {xunityConfigFile}");

            // 3. 确保配置目录存在
            if (!Directory.Exists(configPath))
            {
                Directory.CreateDirectory(configPath);
            }

            // 4. 记录配置文件是否已存在（用于判断是否新生成）
            bool bepinexConfigExistedBefore = File.Exists(bepinexConfigFile);
            bool xunityConfigExistedBefore = File.Exists(xunityConfigFile);

            // 5. 启动游戏进程
            progress?.Report((10, "启动游戏进程..."));
            _logger?.Info($"启动游戏: {gameInfo.ExecutablePath}");

            Process? gameProcess = null;
            try
            {
                gameProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = gameInfo.ExecutablePath,
                    WorkingDirectory = gamePath,
                    UseShellExecute = true
                });

                if (gameProcess == null)
                {
                    _logger?.Error("游戏进程启动失败");
                    return false;
                }

                _logger?.Info($"游戏进程已启动 (PID: {gameProcess.Id})");
            }
            catch (Exception ex)
            {
                _logger?.Error($"启动游戏失败: {ex.Message}");
                return false;
            }

            // 6. 等待配置文件生成
            progress?.Report((20, "等待配置文件生成..."));

            var cts = new CancellationTokenSource();
            var configGeneratedTask = WaitForConfigFilesAsync(
                bepinexConfigFile,
                xunityConfigFile,
                bepinexConfigExistedBefore,
                xunityConfigExistedBefore,
                progress,
                cts.Token);

            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds), cts.Token);

            var completedTask = await Task.WhenAny(configGeneratedTask, timeoutTask);

            // 7. 处理结果
            bool success = false;
            if (completedTask == configGeneratedTask)
            {
                // 配置文件已生成
                success = await configGeneratedTask;
                _logger?.Info(success ? "配置文件生成成功" : "配置文件生成失败");
            }
            else
            {
                // 超时
                _logger?.Warning($"等待配置文件生成超时（{timeoutSeconds}秒）");
                progress?.Report((95, "等待超时，正在检查..."));

                // 检查配置文件是否已生成
                bool bepinexExists = File.Exists(bepinexConfigFile);
                bool xunityExists = File.Exists(xunityConfigFile);

                if (bepinexExists && xunityExists)
                {
                    _logger?.Info("配置文件已生成，但超过预期时间");
                    success = true;
                }
                else
                {
                    // 诊断问题
                    await DiagnoseConfigGenerationFailureAsync(gamePath);
                    success = false;
                }
            }

            // 8. 关闭游戏进程
            progress?.Report((90, "关闭游戏进程..."));
            await TerminateGameProcessAsync(gameProcess, gamePath);

            // 取消所有等待任务
            cts.Cancel();

            progress?.Report((100, success ? "配置文件生成完成" : "配置文件生成失败"));
            return success;
        }
        catch (Exception ex)
        {
            _logger?.Error($"启动游戏生成配置失败: {ex.Message}");
            progress?.Report((0, $"错误: {ex.Message}"));
            return false;
        }
    }

    /// <summary>
    /// 等待配置文件生成
    /// </summary>
    private async Task<bool> WaitForConfigFilesAsync(
        string bepinexConfigFile,
        string xunityConfigFile,
        bool bepinexExistedBefore,
        bool xunityExistedBefore,
        IProgress<(int percentage, string message)>? progress,
        CancellationToken cancellationToken)
    {
        int checkIntervalMs = 500; // 每500ms检查一次
        int maxChecks = 120; // 最多检查60秒
        int checkCount = 0;

        bool bepinexGenerated = bepinexExistedBefore;
        bool xunityGenerated = xunityExistedBefore;

        while (checkCount < maxChecks && !cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(checkIntervalMs, cancellationToken);
            checkCount++;

            // 检查BepInEx配置文件
            if (!bepinexGenerated && File.Exists(bepinexConfigFile))
            {
                // 确认文件可读且有内容
                try
                {
                    var fileInfo = new FileInfo(bepinexConfigFile);
                    if (fileInfo.Length > 0)
                    {
                        bepinexGenerated = true;
                        _logger?.Success($"BepInEx.cfg 已生成");
                        progress?.Report((50, "BepInEx.cfg 已生成"));
                    }
                }
                catch
                {
                    // 文件可能还在写入中
                }
            }

            // 检查XUnity配置文件
            if (!xunityGenerated && File.Exists(xunityConfigFile))
            {
                try
                {
                    var fileInfo = new FileInfo(xunityConfigFile);
                    if (fileInfo.Length > 0)
                    {
                        xunityGenerated = true;
                        _logger?.Success($"AutoTranslatorConfig.ini 已生成");
                        progress?.Report((80, "AutoTranslatorConfig.ini 已生成"));
                    }
                }
                catch
                {
                    // 文件可能还在写入中
                }
            }

            // 如果两个配置文件都已生成，返回成功
            if (bepinexGenerated && xunityGenerated)
            {
                // 额外等待1秒确保文件完全写入
                await Task.Delay(1000, cancellationToken);
                return true;
            }

            // 更新进度（20%-85%之间）
            int progressPercentage = 20 + (int)((checkCount / (double)maxChecks) * 65);
            progress?.Report((progressPercentage, $"等待配置生成... ({checkCount * checkIntervalMs / 1000}秒)"));
        }

        return false;
    }

    /// <summary>
    /// 终止游戏进程
    /// </summary>
    private async Task TerminateGameProcessAsync(Process? gameProcess, string gamePath)
    {
        try
        {
            if (gameProcess != null && !gameProcess.HasExited)
            {
                _logger?.Info($"正在终止游戏进程 (PID: {gameProcess.Id})");

                // 尝试优雅关闭
                gameProcess.CloseMainWindow();

                // 等待最多3秒
                bool exited = gameProcess.WaitForExit(3000);

                if (!exited)
                {
                    // 强制终止
                    _logger?.Warning("游戏未响应关闭请求，强制终止");
                    gameProcess.Kill();
                    await Task.Delay(500); // 等待进程完全终止
                }

                _logger?.Info("游戏进程已终止");
            }

            // 尝试关闭所有相关进程
            var gameName = Path.GetFileNameWithoutExtension(
                GameDetectionService.GetGameInfo(gamePath).ExecutablePath ?? "");

            if (!string.IsNullOrEmpty(gameName))
            {
                var processes = Process.GetProcessesByName(gameName);
                foreach (var process in processes)
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            _logger?.Info($"终止关联进程: {process.ProcessName} (PID: {process.Id})");
                            process.Kill();
                        }
                    }
                    catch
                    {
                        // 忽略错误
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.Warning($"终止游戏进程时出错: {ex.Message}");
        }
    }

    /// <summary>
    /// 诊断配置文件生成失败的原因
    /// </summary>
    private async Task DiagnoseConfigGenerationFailureAsync(string gamePath)
    {
        _logger?.Warning("开始诊断配置生成失败原因...");

        // 检查BepInEx是否正确安装
        var bepinexPath = PathHelper.GetBepInExPath(gamePath);
        var winhttpDll = PathHelper.GetWinhttpDllPath(gamePath);
        var doorstopConfig = PathHelper.GetDoorstopConfigPath(gamePath);

        if (!Directory.Exists(bepinexPath))
        {
            _logger?.Error("诊断: BepInEx目录不存在");
        }

        if (!File.Exists(winhttpDll))
        {
            _logger?.Error("诊断: winhttp.dll不存在（BepInEx入口点缺失）");
        }

        if (File.Exists(doorstopConfig))
        {
            _logger?.Info("诊断: 检测到doorstop_config.ini");
        }

        // 检查BepInEx日志
        var logPath = Path.Combine(bepinexPath, "LogOutput.log");
        if (File.Exists(logPath))
        {
            try
            {
                var logLines = await File.ReadAllLinesAsync(logPath);
                bool foundError = false;

                for (int i = Math.Max(0, logLines.Length - 50); i < logLines.Length; i++)
                {
                    if (logLines[i].Contains("ERROR", StringComparison.OrdinalIgnoreCase) ||
                        logLines[i].Contains("FATAL", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger?.Error($"BepInEx日志错误: {logLines[i]}");
                        foundError = true;
                    }
                }

                if (!foundError)
                {
                    _logger?.Info("诊断: BepInEx日志中未发现明显错误");
                }
            }
            catch
            {
                _logger?.Warning("诊断: 无法读取BepInEx日志");
            }
        }
        else
        {
            _logger?.Error("诊断: BepInEx日志文件不存在（BepInEx可能未运行）");
        }

        // 检查游戏引擎类型
        var gameEngine = GameDetectionService.DetectGameEngine(gamePath);
        _logger?.Info($"诊断: 游戏引擎类型 = {gameEngine}");

        // 建议
        _logger?.Info("建议: 请检查");
        _logger?.Info("  1. 游戏是否支持BepInEx");
        _logger?.Info("  2. BepInEx版本是否与游戏引擎匹配（Mono/IL2CPP）");
        _logger?.Info("  3. 杀毒软件是否阻止了BepInEx加载");
        _logger?.Info("  4. 游戏是否需要管理员权限运行");
    }
}
