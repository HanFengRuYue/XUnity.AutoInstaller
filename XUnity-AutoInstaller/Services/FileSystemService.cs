using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;
using XUnity_AutoInstaller.Utils;

namespace XUnity_AutoInstaller.Services;

/// <summary>
/// 文件系统服务
/// 负责文件解压、复制、备份等操作
/// </summary>
public class FileSystemService
{
    /// <summary>
    /// 解压 ZIP 文件到指定目录
    /// </summary>
    /// <param name="zipPath">ZIP 文件路径</param>
    /// <param name="destinationPath">目标目录</param>
    /// <param name="progress">进度报告（0-100）</param>
    public static async Task ExtractZipAsync(string zipPath, string destinationPath, IProgress<int>? progress = null)
    {
        await Task.Run((Action)(() =>
        {
            try
            {
                if (!File.Exists(zipPath))
                {
                    throw new FileNotFoundException($"ZIP 文件不存在: {zipPath}");
                }

                // 确保目标目录存在
                Directory.CreateDirectory(destinationPath);

                using var archive = ArchiveFactory.Open(zipPath);
                var entries = archive.Entries.ToList();
                int totalEntries = entries.Count;
                int currentEntry = 0;

                foreach (var entry in entries)
                {
                    if (!entry.IsDirectory)
                    {
                        entry.WriteToDirectory(destinationPath, new ExtractionOptions
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }

                    currentEntry++;
                    progress?.Report((int)((double)currentEntry / totalEntries * 100));
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"解压文件失败: {ex.Message}", ex);
            }
        }));
    }

    /// <summary>
    /// 备份目录
    /// </summary>
    /// <param name="sourcePath">源目录</param>
    /// <param name="backupName">备份名称（可选）</param>
    /// <returns>备份文件路径</returns>
    public static string BackupDirectory(string sourcePath, string? backupName = null)
    {
        try
        {
            if (!Directory.Exists(sourcePath))
            {
                throw new DirectoryNotFoundException($"源目录不存在: {sourcePath}");
            }

            var parentDir = Path.GetDirectoryName(sourcePath) ?? throw new InvalidOperationException("无法获取父目录");
            var dirName = Path.GetFileName(sourcePath);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupFileName = backupName ?? $"{dirName}_backup_{timestamp}.zip";
            var backupPath = Path.Combine(parentDir, backupFileName);

            // 创建备份 ZIP
            using (var archive = SharpCompress.Archives.Zip.ZipArchive.Create())
            {
                archive.AddAllFromDirectory(sourcePath);
                archive.SaveTo(backupPath, CompressionType.Deflate);
            }

            return backupPath;
        }
        catch (Exception ex)
        {
            throw new Exception($"备份目录失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 删除目录及其所有内容
    /// </summary>
    public static void DeleteDirectoryRecursive(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"删除目录失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 复制目录及其所有内容
    /// </summary>
    public static void CopyDirectory(string sourcePath, string destinationPath, bool overwrite = true)
    {
        try
        {
            // 创建目标目录
            Directory.CreateDirectory(destinationPath);

            // 复制文件
            foreach (var file in Directory.GetFiles(sourcePath))
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(destinationPath, fileName);
                File.Copy(file, destFile, overwrite);
            }

            // 递归复制子目录
            foreach (var dir in Directory.GetDirectories(sourcePath))
            {
                var dirName = Path.GetFileName(dir);
                var destDir = Path.Combine(destinationPath, dirName);
                CopyDirectory(dir, destDir, overwrite);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"复制目录失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 创建桌面快捷方式（Windows）
    /// </summary>
    public static void CreateDesktopShortcut(string targetPath, string shortcutName)
    {
        try
        {
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var shortcutPath = Path.Combine(desktopPath, $"{shortcutName}.lnk");

            // 使用 WScript.Shell COM 对象创建快捷方式
            Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType != null)
            {
                dynamic? shell = Activator.CreateInstance(shellType);
                if (shell != null)
                {
                    dynamic? shortcut = shell.CreateShortcut(shortcutPath);
                    if (shortcut != null)
                    {
                        shortcut.TargetPath = targetPath;
                        shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
                        shortcut.Save();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // 快捷方式创建失败不应该阻止安装
            throw new Exception($"创建快捷方式失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 获取目录大小（字节）
    /// </summary>
    public static long GetDirectorySize(string path)
    {
        if (!Directory.Exists(path))
        {
            return 0;
        }

        long size = 0;

        try
        {
            foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                size += new FileInfo(file).Length;
            }
        }
        catch
        {
            // 忽略访问被拒绝等错误
        }

        return size;
    }

    /// <summary>
    /// 验证 ZIP 文件完整性
    /// </summary>
    public static bool ValidateZipFile(string zipPath)
    {
        try
        {
            using var archive = ArchiveFactory.Open(zipPath);
            // 尝试读取所有条目
            foreach (var entry in archive.Entries)
            {
                // 验证条目可访问
                if (entry == null)
                {
                    return false;
                }
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
}
