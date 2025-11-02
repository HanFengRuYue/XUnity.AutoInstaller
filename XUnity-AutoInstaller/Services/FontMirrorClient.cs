using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebDav;
using XUnity_AutoInstaller.Models;

namespace XUnity_AutoInstaller.Services;

/// <summary>
/// TextMeshPro 字体资源 WebDAV 镜像客户端
/// </summary>
public class FontMirrorClient : IDisposable
{
    private readonly WebDavClient _webDavClient;
    private readonly HttpClient _httpClient;
    private const string FontMirrorUrl = "https://fraxelia.com:60761/TextMeshProFonts/";

    public FontMirrorClient()
    {
        var clientParams = new WebDavClientParams
        {
            BaseAddress = new Uri("https://fraxelia.com:60761/"),
            Timeout = TimeSpan.FromSeconds(30)
        };

        _webDavClient = new WebDavClient(clientParams);
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "XUnity-AutoInstaller");

        LogService.Instance.Log("初始化 TextMeshPro 字体镜像客户端", LogLevel.Info, "[FontMirror]");
    }

    /// <summary>
    /// 获取所有可用的 TextMeshPro 字体资源
    /// </summary>
    public async Task<List<FontResourceInfo>> GetAvailableFontsAsync()
    {
        try
        {
            LogService.Instance.Log("从 WebDAV 镜像获取 TextMeshPro 字体列表", LogLevel.Debug, "[FontMirror]");

            // 使用 PROPFIND 列出所有文件
            var result = await _webDavClient.Propfind(FontMirrorUrl);

            if (!result.IsSuccessful)
            {
                throw new Exception($"WebDAV PROPFIND 失败: {result.StatusCode}");
            }

            var fonts = new List<FontResourceInfo>();

            // 解析文件列表（过滤掉目录，只保留文件）
            var files = result.Resources
                .Where(r => !r.IsCollection && !string.IsNullOrEmpty(r.DisplayName))
                .OrderByDescending(r => r.LastModifiedDate ?? DateTime.MinValue);

            foreach (var file in files)
            {
                var fileName = file.DisplayName;

                // 跳过特殊文件（如 .DS_Store, thumbs.db 等）
                if (fileName.StartsWith(".") || fileName.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var fontInfo = ParseFontFileName(fileName, file);

                if (fontInfo != null)
                {
                    fonts.Add(fontInfo);
                }
            }

            LogService.Instance.Log($"从 WebDAV 镜像获取 {fonts.Count} 个 TextMeshPro 字体", LogLevel.Info, "[FontMirror]");
            return fonts;
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"从 WebDAV 镜像获取字体列表失败: {ex.Message}", LogLevel.Error, "[FontMirror]");
            throw new Exception($"获取字体列表失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 解析字体文件名
    /// 格式: {FontName}_U{UnityVersion}
    /// 例如: SourceHanSans_U2018-4-36
    /// </summary>
    private FontResourceInfo? ParseFontFileName(string fileName, WebDavResource resource)
    {
        try
        {
            // 匹配格式: {FontName}_U{UnityVersion}
            // Unity 版本格式: YYYY-M-P (例如: 2018-4-36)
            var match = Regex.Match(fileName, @"^(.+)_U(\d{4}-\d+-\d+)$");

            if (!match.Success)
            {
                LogService.Instance.Log($"无法解析字体文件名（格式不匹配）: {fileName}", LogLevel.Debug, "[FontMirror]");
                return null;
            }

            var fontName = match.Groups[1].Value;
            var unityVersion = match.Groups[2].Value;

            // 构造完整下载 URL
            var downloadUrl = $"{FontMirrorUrl}{fileName}";

            var fontInfo = new FontResourceInfo
            {
                FontName = fontName,
                UnityVersion = unityVersion,
                FileName = fileName,
                DownloadUrl = downloadUrl,
                FileSize = resource.ContentLength ?? 0,
                LastModified = resource.LastModifiedDate ?? DateTime.Now,
                IsCached = false,  // 将在 FontManagementService 中更新
                IsInstalled = false // 将在 FontManagementService 中更新
            };

            LogService.Instance.Log($"解析字体: {fontInfo.DisplayName} (大小: {fontInfo.FileSizeFormatted})", LogLevel.Debug, "[FontMirror]");

            return fontInfo;
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"解析字体文件名失败 ({fileName}): {ex.Message}", LogLevel.Warning, "[FontMirror]");
            return null;
        }
    }

    /// <summary>
    /// 下载字体文件到指定路径
    /// </summary>
    public async Task DownloadFontAsync(FontResourceInfo fontInfo, string destinationPath, IProgress<int>? progress = null)
    {
        const int maxRetries = 3;
        const int retryDelayMs = 1000;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                LogService.Instance.Log($"开始下载字体: {fontInfo.DisplayName} (尝试 {attempt}/{maxRetries})", LogLevel.Info, "[FontMirror]");

                // 确保目标目录存在
                var directory = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // 直接使用 HTTP GET 下载
                using var response = await _httpClient.GetAsync(fontInfo.DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                var totalBytes = response.Content.Headers.ContentLength ?? -1;
                var canReportProgress = totalBytes != -1 && progress != null;

                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                long totalRead = 0;
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    totalRead += bytesRead;

                    if (canReportProgress)
                    {
                        var percentage = (int)((double)totalRead / totalBytes * 100);
                        progress!.Report(percentage);
                    }
                }

                // 验证文件完整性
                if (totalBytes > 0 && totalRead != totalBytes)
                {
                    throw new Exception($"下载不完整: 期望 {totalBytes} 字节，实际 {totalRead} 字节");
                }

                LogService.Instance.Log($"字体下载完成: {fontInfo.DisplayName}", LogLevel.Info, "[FontMirror]");
                return; // 下载成功，退出重试循环
            }
            catch (Exception ex)
            {
                // 如果是最后一次尝试，抛出异常
                if (attempt == maxRetries)
                {
                    LogService.Instance.Log($"字体下载失败（已重试 {maxRetries} 次）: {ex.Message}", LogLevel.Error, "[FontMirror]");
                    throw new Exception($"下载字体失败（已重试 {maxRetries} 次）: {ex.Message}", ex);
                }

                // 否则记录日志并重试
                LogService.Instance.Log($"字体下载失败（尝试 {attempt}/{maxRetries}）: {ex.Message}，{retryDelayMs}ms后重试...", LogLevel.Warning, "[FontMirror]");
                await Task.Delay(retryDelayMs * attempt); // 递增延迟时间
            }
        }
    }

    /// <summary>
    /// 验证 WebDAV 连接是否可用
    /// </summary>
    public async Task<bool> ValidateConnectionAsync()
    {
        try
        {
            LogService.Instance.Log("测试 TextMeshPro 字体镜像连接", LogLevel.Debug, "[FontMirror]");
            var result = await _webDavClient.Propfind(FontMirrorUrl);
            var isSuccessful = result.IsSuccessful;

            if (isSuccessful)
            {
                LogService.Instance.Log("TextMeshPro 字体镜像连接成功", LogLevel.Info, "[FontMirror]");
            }
            else
            {
                LogService.Instance.Log($"TextMeshPro 字体镜像连接失败: {result.StatusCode}", LogLevel.Warning, "[FontMirror]");
            }

            return isSuccessful;
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"测试 TextMeshPro 字体镜像连接时出错: {ex.Message}", LogLevel.Error, "[FontMirror]");
            return false;
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
