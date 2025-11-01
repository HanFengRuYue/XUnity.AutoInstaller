using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace XUnity_AutoInstaller.Utils;

/// <summary>
/// INI 文件解析器
/// 支持读取和写入 INI 格式配置文件
/// </summary>
public class IniParser
{
    /// <summary>
    /// 解析 INI 文件为字典结构
    /// </summary>
    /// <param name="filePath">INI 文件路径</param>
    /// <returns>字典：[Section][Key] = Value</returns>
    public static Dictionary<string, Dictionary<string, string>> Parse(string filePath)
    {
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(filePath))
        {
            return result;
        }

        string currentSection = string.Empty;

        foreach (var line in File.ReadAllLines(filePath))
        {
            var trimmed = line.Trim();

            // 跳过空行和注释
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith(";") || trimmed.StartsWith("#"))
            {
                continue;
            }

            // 解析 Section
            if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
            {
                currentSection = trimmed[1..^1].Trim();
                if (!result.ContainsKey(currentSection))
                {
                    result[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                continue;
            }

            // 解析 Key=Value
            var equalsIndex = trimmed.IndexOf('=');
            if (equalsIndex > 0)
            {
                var key = trimmed[..equalsIndex].Trim();
                var value = trimmed[(equalsIndex + 1)..].Trim();

                // 移除值两端的引号
                if (value.StartsWith("\"") && value.EndsWith("\""))
                {
                    value = value[1..^1];
                }

                if (string.IsNullOrEmpty(currentSection))
                {
                    currentSection = "General";
                    if (!result.ContainsKey(currentSection))
                    {
                        result[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    }
                }

                result[currentSection][key] = value;
            }
        }

        return result;
    }

    /// <summary>
    /// 将字典结构写入 INI 文件
    /// </summary>
    /// <param name="filePath">INI 文件路径</param>
    /// <param name="data">字典：[Section][Key] = Value</param>
    public static void Write(string filePath, Dictionary<string, Dictionary<string, string>> data)
    {
        var sb = new StringBuilder();

        foreach (var section in data)
        {
            sb.AppendLine($"[{section.Key}]");

            foreach (var kv in section.Value)
            {
                sb.AppendLine($"{kv.Key} = {kv.Value}");
            }

            sb.AppendLine(); // 空行分隔
        }

        // 确保目录存在
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    /// <summary>
    /// 获取指定值，如果不存在则返回默认值
    /// </summary>
    public static string GetValue(Dictionary<string, Dictionary<string, string>> data, string section, string key, string defaultValue = "")
    {
        if (data.TryGetValue(section, out var sectionDict))
        {
            if (sectionDict.TryGetValue(key, out var value))
            {
                return value;
            }
        }
        return defaultValue;
    }

    /// <summary>
    /// 设置指定值
    /// </summary>
    public static void SetValue(Dictionary<string, Dictionary<string, string>> data, string section, string key, string value)
    {
        if (!data.ContainsKey(section))
        {
            data[section] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        data[section][key] = value;
    }

    /// <summary>
    /// 获取布尔值
    /// </summary>
    public static bool GetBool(Dictionary<string, Dictionary<string, string>> data, string section, string key, bool defaultValue = false)
    {
        var value = GetValue(data, section, key);
        if (bool.TryParse(value, out var result))
        {
            return result;
        }
        // 兼容 true/false, yes/no, 1/0
        return value.ToLowerInvariant() switch
        {
            "true" or "yes" or "1" => true,
            "false" or "no" or "0" => false,
            _ => defaultValue
        };
    }

    /// <summary>
    /// 获取整数值
    /// </summary>
    public static int GetInt(Dictionary<string, Dictionary<string, string>> data, string section, string key, int defaultValue = 0)
    {
        var value = GetValue(data, section, key);
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    public static float GetFloat(Dictionary<string, Dictionary<string, string>> data, string section, string key, float defaultValue = 0f)
    {
        var value = GetValue(data, section, key);
        return float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : defaultValue;
    }
}
