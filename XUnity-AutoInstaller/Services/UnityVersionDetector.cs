using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace XUnity_AutoInstaller.Services;

/// <summary>
/// Unity 版本检测服务
/// </summary>
public class UnityVersionDetector
{
    private const int MaxSearchBytes = 1024 * 1024; // 搜索前 1MB

    /// <summary>
    /// 检测游戏的 Unity 版本
    /// </summary>
    /// <param name="gamePath">游戏路径</param>
    /// <returns>Unity 版本字符串（例如: "2019.4.40"），如果检测失败返回 null</returns>
    public async Task<string?> DetectUnityVersionAsync(string gamePath)
    {
        if (string.IsNullOrEmpty(gamePath) || !Directory.Exists(gamePath))
        {
            LogService.Instance.Log($"游戏路径无效: {gamePath}", LogLevel.Warning, "[UnityVersion]");
            return null;
        }

        try
        {
            // 方法 1: 尝试从 globalgamemanagers 文件读取
            var version = await TryReadFromGlobalGameManagersAsync(gamePath);
            if (!string.IsNullOrEmpty(version))
            {
                LogService.Instance.Log($"检测到 Unity 版本: {version} (来源: globalgamemanagers)", LogLevel.Info, "[UnityVersion]");
                return version;
            }

            // 方法 2: 尝试从 data.unity3d 文件读取（某些旧版本游戏）
            version = await TryReadFromDataUnity3dAsync(gamePath);
            if (!string.IsNullOrEmpty(version))
            {
                LogService.Instance.Log($"检测到 Unity 版本: {version} (来源: data.unity3d)", LogLevel.Info, "[UnityVersion]");
                return version;
            }

            LogService.Instance.Log("未能检测到 Unity 版本", LogLevel.Warning, "[UnityVersion]");
            return null;
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"检测 Unity 版本时出错: {ex.Message}", LogLevel.Error, "[UnityVersion]");
            return null;
        }
    }

    /// <summary>
    /// 从 globalgamemanagers 文件读取 Unity 版本
    /// </summary>
    private async Task<string?> TryReadFromGlobalGameManagersAsync(string gamePath)
    {
        try
        {
            // 查找 Data 目录
            var dataDirectories = Directory.GetDirectories(gamePath, "*_Data", SearchOption.TopDirectoryOnly);
            if (dataDirectories.Length == 0)
            {
                return null;
            }

            var dataDirectory = dataDirectories[0];
            var globalGameManagersPath = Path.Combine(dataDirectory, "globalgamemanagers");

            if (!File.Exists(globalGameManagersPath))
            {
                // 某些游戏可能有 .assets 扩展名
                globalGameManagersPath = Path.Combine(dataDirectory, "globalgamemanagers.assets");
                if (!File.Exists(globalGameManagersPath))
                {
                    return null;
                }
            }

            // 读取文件的前部分内容
            using var fileStream = new FileStream(globalGameManagersPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var buffer = new byte[Math.Min(MaxSearchBytes, fileStream.Length)];
            await fileStream.ReadAsync(buffer, 0, buffer.Length);

            // 在二进制数据中搜索 Unity 版本字符串
            // Unity 版本格式: 2019.4.40f1, 2021.3.16f1, 等等
            var version = SearchForUnityVersion(buffer);
            return version;
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"读取 globalgamemanagers 失败: {ex.Message}", LogLevel.Debug, "[UnityVersion]");
            return null;
        }
    }

    /// <summary>
    /// 从 data.unity3d 文件读取 Unity 版本（某些旧版本游戏）
    /// </summary>
    private async Task<string?> TryReadFromDataUnity3dAsync(string gamePath)
    {
        try
        {
            var dataDirectories = Directory.GetDirectories(gamePath, "*_Data", SearchOption.TopDirectoryOnly);
            if (dataDirectories.Length == 0)
            {
                return null;
            }

            var dataDirectory = dataDirectories[0];
            var dataUnity3dPath = Path.Combine(dataDirectory, "data.unity3d");

            if (!File.Exists(dataUnity3dPath))
            {
                return null;
            }

            using var fileStream = new FileStream(dataUnity3dPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var buffer = new byte[Math.Min(MaxSearchBytes, fileStream.Length)];
            await fileStream.ReadAsync(buffer, 0, buffer.Length);

            var version = SearchForUnityVersion(buffer);
            return version;
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"读取 data.unity3d 失败: {ex.Message}", LogLevel.Debug, "[UnityVersion]");
            return null;
        }
    }

    /// <summary>
    /// 在二进制数据中搜索 Unity 版本字符串
    /// </summary>
    private string? SearchForUnityVersion(byte[] buffer)
    {
        try
        {
            // 将二进制数据转换为字符串（使用 ASCII 和 UTF-8）
            var asciiText = Encoding.ASCII.GetString(buffer);
            var utf8Text = Encoding.UTF8.GetString(buffer);

            // Unity 版本的正则表达式模式
            // 匹配格式: 2019.4.40f1, 2021.3.16f1, 2018.4.36f1, 等等
            var pattern = @"\b(\d{4}\.\d+\.\d+[a-z]\d+)\b";

            // 先尝试 ASCII
            var match = Regex.Match(asciiText, pattern);
            if (match.Success)
            {
                var fullVersion = match.Groups[1].Value;
                return NormalizeVersionString(fullVersion);
            }

            // 再尝试 UTF-8
            match = Regex.Match(utf8Text, pattern);
            if (match.Success)
            {
                var fullVersion = match.Groups[1].Value;
                return NormalizeVersionString(fullVersion);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 规范化版本字符串，转换为 YYYY-M-P 格式
    /// </summary>
    /// <param name="fullVersion">完整版本号（例如: 2019.4.40f1）</param>
    /// <returns>规范化的版本号（例如: 2019-4-40）</returns>
    private string NormalizeVersionString(string fullVersion)
    {
        try
        {
            // 移除后缀（f1, b1, p1 等）
            var versionWithoutSuffix = Regex.Replace(fullVersion, @"[a-z]\d+$", "");

            // 转换为使用连字符的格式: 2019.4.40 -> 2019-4-40
            var normalized = versionWithoutSuffix.Replace('.', '-');

            return normalized;
        }
        catch
        {
            return fullVersion;
        }
    }

    /// <summary>
    /// 计算版本相似度（用于推荐字体）
    /// </summary>
    /// <param name="gameVersion">游戏版本（例如: "2019-4-40"）</param>
    /// <param name="fontVersion">字体版本（例如: "2018-4-36"）</param>
    /// <returns>相似度得分（越高越相似）</returns>
    public int CalculateVersionSimilarity(string? gameVersion, string fontVersion)
    {
        if (string.IsNullOrEmpty(gameVersion))
            return 0;

        try
        {
            var gameParts = gameVersion.Split('-');
            var fontParts = fontVersion.Split('-');

            if (gameParts.Length < 2 || fontParts.Length < 2)
                return 0;

            int score = 0;

            // 主版本号匹配（权重最高）
            if (gameParts[0] == fontParts[0])
            {
                score += 1000;

                // 次版本号匹配
                if (gameParts.Length > 1 && fontParts.Length > 1 && gameParts[1] == fontParts[1])
                {
                    score += 100;

                    // 补丁版本号接近度
                    if (gameParts.Length > 2 && fontParts.Length > 2)
                    {
                        if (int.TryParse(gameParts[2], out int gamePatch) && int.TryParse(fontParts[2], out int fontPatch))
                        {
                            score += Math.Max(0, 50 - Math.Abs(gamePatch - fontPatch));
                        }
                    }
                }
                else if (gameParts.Length > 1 && fontParts.Length > 1)
                {
                    // 次版本号不完全匹配，但接近
                    if (int.TryParse(gameParts[1], out int gameMinor) && int.TryParse(fontParts[1], out int fontMinor))
                    {
                        score += Math.Max(0, 50 - Math.Abs(gameMinor - fontMinor) * 10);
                    }
                }
            }
            else
            {
                // 主版本号不匹配，但接近也有一定得分
                if (int.TryParse(gameParts[0], out int gameMajor) && int.TryParse(fontParts[0], out int fontMajor))
                {
                    score += Math.Max(0, 100 - Math.Abs(gameMajor - fontMajor) * 20);
                }
            }

            return score;
        }
        catch
        {
            return 0;
        }
    }
}
