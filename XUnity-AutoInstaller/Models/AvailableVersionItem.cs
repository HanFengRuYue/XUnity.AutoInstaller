using Microsoft.UI.Xaml;

namespace XUnity_AutoInstaller.Models
{
    /// <summary>
    /// 用于在版本管理页面ListView中显示可用版本信息的辅助类
    /// </summary>
    public class AvailableVersionItem
    {
        public string DisplayText { get; set; } = string.Empty;
        public VersionInfo Version { get; set; } = null!;

        /// <summary>
        /// 是否已缓存到本地
        /// </summary>
        public bool IsCached { get; set; }

        /// <summary>
        /// 下载按钮的可见性
        /// </summary>
        public Visibility IsDownloadButtonVisible => IsCached ? Visibility.Collapsed : Visibility.Visible;

        /// <summary>
        /// "已下载"文本的可见性
        /// </summary>
        public Visibility IsDownloadedTextVisible => IsCached ? Visibility.Visible : Visibility.Collapsed;
    }
}
