using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using XUnity.AutoInstaller.Models;

namespace XUnity.AutoInstaller.Services
{
    /// <summary>
    /// 用于从 builds.bepinex.dev 获取 IL2CPP 版本的客户端
    /// </summary>
    public class BepInExBuildsApiClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private const string BuildsBaseUrl = "https://builds.bepinex.dev";
        private const string BepInExBeProjectUrl = "https://builds.bepinex.dev/projects/bepinex_be";

        public BepInExBuildsApiClient()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "XUnity-AutoInstaller");
        }

        /// <summary>
        /// 获取 BepInEx Bleeding Edge IL2CPP 版本列表
        /// </summary>
        public async Task<List<VersionInfo>> GetIL2CPPVersionsAsync()
        {
            var versions = new List<VersionInfo>();

            try
            {
                LogService.Instance.Log($"开始获取 IL2CPP 版本，URL: {BepInExBeProjectUrl}", LogLevel.Debug, "[IL2CPP]");

                // 获取项目页面 HTML
                var html = await _httpClient.GetStringAsync(BepInExBeProjectUrl);
                LogService.Instance.Log($"HTML 长度: {html.Length} 字符", LogLevel.Debug, "[IL2CPP]");

                // 解析构建列表
                var builds = ParseBuilds(html);
                LogService.Instance.Log($"解析到 {builds.Count} 个构建", LogLevel.Debug, "[IL2CPP]");

                if (builds.Count == 0)
                {
                    LogService.Instance.Log("警告: 未找到任何构建，HTML 片段:", LogLevel.Warning, "[IL2CPP]");
                    LogService.Instance.Log(html.Substring(0, Math.Min(500, html.Length)), LogLevel.Warning, "[IL2CPP]");
                }

                // 为每个构建获取 IL2CPP 版本（只获取最新的1个以提升性能）
                foreach (var build in builds.Take(1))
                {
                    LogService.Instance.Log($"处理构建 #{build.BuildNumber} - {build.Version}", LogLevel.Debug, "[IL2CPP]");
                    var il2cppVersions = await GetBuildArtifactsAsync(build.BuildNumber, build.Version);
                    LogService.Instance.Log($"构建 #{build.BuildNumber} 找到 {il2cppVersions.Count} 个制品", LogLevel.Debug, "[IL2CPP]");
                    versions.AddRange(il2cppVersions);
                }

                LogService.Instance.Log($"完成！总共获取 {versions.Count} 个 IL2CPP 版本", LogLevel.Debug, "[IL2CPP]");
                return versions;
            }
            catch (Exception ex)
            {
                LogService.Instance.Log($"错误: {ex.GetType().Name} - {ex.Message}", LogLevel.Error, "[IL2CPP]");
                LogService.Instance.Log($"堆栈: {ex.StackTrace}", LogLevel.Error, "[IL2CPP]");
                throw new Exception($"从 builds.bepinex.dev 获取 IL2CPP 版本失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 解析构建列表
        /// </summary>
        private List<BuildInfo> ParseBuilds(string html)
        {
            var builds = new List<BuildInfo>();

            // 尝试多个正则模式以提高兼容性
            var patterns = new[]
            {
                // 模式1: #738 - 6.0.0-be.738+af0cba7 (原始模式)
                @"#(\d+)[^<]*?6\.0\.0-be\.(\d+)\+([a-f0-9]+)",
                // 模式2: 更宽松的版本匹配
                @"build[^\d]*(\d+)[^<]*?6\.0\.0-be\.(\d+)\+([a-f0-9]+)",
                // 模式3: 直接匹配版本字符串
                @"6\.0\.0-be\.(\d+)\+([a-f0-9]+)"
            };

            foreach (var pattern in patterns)
            {
                var matches = Regex.Matches(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);

                if (matches.Count > 0)
                {
                    LogService.Instance.Log($"使用正则匹配到 {matches.Count} 个构建", LogLevel.Debug, "[IL2CPP]");
                    foreach (Match match in matches)
                    {
                        try
                        {
                            string buildNumber, beNumber, commitHash;

                            if (match.Groups.Count >= 4)
                            {
                                // 模式1和2: 有构建号
                                buildNumber = match.Groups[1].Value;
                                beNumber = match.Groups[2].Value;
                                commitHash = match.Groups[3].Value;
                            }
                            else if (match.Groups.Count >= 3)
                            {
                                // 模式3: 没有构建号，使用 beNumber 作为 buildNumber
                                beNumber = match.Groups[1].Value;
                                commitHash = match.Groups[2].Value;
                                buildNumber = beNumber; // 使用 BE 号作为构建号
                            }
                            else
                            {
                                continue;
                            }

                            var version = $"6.0.0-be.{beNumber}+{commitHash}";

                            builds.Add(new BuildInfo
                            {
                                BuildNumber = buildNumber,
                                Version = version
                            });
                        }
                        catch (Exception ex)
                        {
                            LogService.Instance.Log($"解析匹配失败: {ex.Message}", LogLevel.Error, "[IL2CPP]");
                        }
                    }

                    if (builds.Count > 0)
                    {
                        break; // 找到结果就停止尝试其他模式
                    }
                }
            }

            return builds.DistinctBy(b => b.BuildNumber).ToList();
        }

        /// <summary>
        /// 获取特定构建的 IL2CPP 制品
        /// </summary>
        private async Task<List<VersionInfo>> GetBuildArtifactsAsync(string buildNumber, string version)
        {
            var artifacts = new List<VersionInfo>();

            try
            {
                // 根据已知的文件命名模式构造下载 URL
                // BepInEx-Unity.IL2CPP-win-x86-{version}.zip
                // BepInEx-Unity.IL2CPP-win-x64-{version}.zip

                var architectures = new[]
                {
                    ("win-x86", Platform.IL2CPP_x86),
                    ("win-x64", Platform.IL2CPP_x64)
                };

                foreach (var (arch, platform) in architectures)
                {
                    var fileName = $"BepInEx-Unity.IL2CPP-{arch}-{version}.zip";
                    // 不使用 Uri.EscapeDataString，因为 builds.bepinex.dev 期望原始文件名
                    // 只需要对空格进行编码（如果有的话）
                    var encodedFileName = fileName.Replace(" ", "%20");
                    var downloadUrl = $"{BuildsBaseUrl}/projects/bepinex_be/{buildNumber}/{encodedFileName}";

                    // 尝试获取文件大小（通过 HEAD 请求）
                    long fileSize = 0;
                    try
                    {
                        var headRequest = new HttpRequestMessage(HttpMethod.Head, downloadUrl);
                        var headResponse = await _httpClient.SendAsync(headRequest);

                        if (headResponse.IsSuccessStatusCode)
                        {
                            if (headResponse.Content.Headers.ContentLength.HasValue)
                            {
                                fileSize = headResponse.Content.Headers.ContentLength.Value;
                                LogService.Instance.Log($"找到 {arch} 制品 ({fileSize / 1024.0 / 1024.0:F1} MB)", LogLevel.Debug, "[IL2CPP]");
                            }
                            else
                            {
                                // 文件存在但没有 Content-Length，设置默认值
                                fileSize = 0;
                                LogService.Instance.Log("警告: 文件存在但无 Content-Length，使用默认值", LogLevel.Warning, "[IL2CPP]");
                            }

                            artifacts.Add(new VersionInfo
                            {
                                Version = version,
                                Name = fileName,
                                DownloadUrl = downloadUrl,
                                FileSize = fileSize,
                                ReleaseDate = DateTime.Now, // builds.bepinex.dev 没有提供确切日期
                                IsPrerelease = true, // Bleeding Edge 都是预览版
                                PackageType = PackageType.BepInEx,
                                TargetPlatform = platform
                            });
                        }
                        else
                        {
                            LogService.Instance.Log($"文件不存在，跳过: {fileName}", LogLevel.Debug, "[IL2CPP]");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogService.Instance.Log($"HEAD 请求失败: {ex.Message}", LogLevel.Warning, "[IL2CPP]");
                        // HEAD 请求失败，仍然添加版本（设置文件大小为0）
                        artifacts.Add(new VersionInfo
                        {
                            Version = version,
                            Name = fileName,
                            DownloadUrl = downloadUrl,
                            FileSize = 0,
                            ReleaseDate = DateTime.Now,
                            IsPrerelease = true,
                            PackageType = PackageType.BepInEx,
                            TargetPlatform = platform
                        });
                    }
                }

                return artifacts;
            }
            catch
            {
                // 获取单个构建失败不应影响整体流程
                return new List<VersionInfo>();
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        /// <summary>
        /// 构建信息
        /// </summary>
        private class BuildInfo
        {
            public string BuildNumber { get; set; } = string.Empty;
            public string Version { get; set; } = string.Empty;
        }
    }
}
