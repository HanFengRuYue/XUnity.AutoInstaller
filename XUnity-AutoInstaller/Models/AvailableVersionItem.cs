namespace XUnity_AutoInstaller.Models
{
    /// <summary>
    /// 用于在版本管理页面ListView中显示可用版本信息的辅助类
    /// </summary>
    public class AvailableVersionItem
    {
        public string DisplayText { get; set; } = string.Empty;
        public VersionInfo Version { get; set; } = null!;
    }
}
