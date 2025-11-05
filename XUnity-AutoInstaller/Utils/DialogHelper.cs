using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;

namespace XUnity_AutoInstaller.Utils;

/// <summary>
/// UI对话框和选择器的辅助类
/// 提供统一的对话框和文件夹选择器方法
/// </summary>
public static class DialogHelper
{
    /// <summary>
    /// 显示文件夹选择器
    /// </summary>
    /// <returns>选中的文件夹路径，如果取消则返回null</returns>
    public static async Task<string?> PickFolderAsync()
    {
        var picker = new FolderPicker();
        picker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
        picker.FileTypeFilter.Add("*");

        // 获取主窗口 HWND（WinUI3 必需）
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var folder = await picker.PickSingleFolderAsync();
        return folder?.Path;
    }

    /// <summary>
    /// 显示错误对话框
    /// </summary>
    /// <param name="xamlRoot">XAML根元素</param>
    /// <param name="title">标题</param>
    /// <param name="message">消息内容</param>
    public static async Task ShowErrorAsync(XamlRoot? xamlRoot, string title, string message)
    {
        if (xamlRoot == null) return;

        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "确定",
            XamlRoot = xamlRoot
        };

        await dialog.ShowAsync();
    }

    /// <summary>
    /// 显示成功对话框
    /// </summary>
    /// <param name="xamlRoot">XAML根元素</param>
    /// <param name="title">标题</param>
    /// <param name="message">消息内容</param>
    public static async Task ShowSuccessAsync(XamlRoot? xamlRoot, string title, string message)
    {
        if (xamlRoot == null) return;

        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "确定",
            XamlRoot = xamlRoot
        };

        await dialog.ShowAsync();
    }

    /// <summary>
    /// 显示确认对话框
    /// </summary>
    /// <param name="xamlRoot">XAML根元素</param>
    /// <param name="title">标题</param>
    /// <param name="message">消息内容</param>
    /// <param name="primaryButtonText">主按钮文本（默认：确定）</param>
    /// <param name="closeButtonText">取消按钮文本（默认：取消）</param>
    /// <returns>用户是否点击了主按钮</returns>
    public static async Task<bool> ShowConfirmAsync(
        XamlRoot? xamlRoot,
        string title,
        string message,
        string primaryButtonText = "确定",
        string closeButtonText = "取消")
    {
        if (xamlRoot == null) return false;

        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = primaryButtonText,
            CloseButtonText = closeButtonText,
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = xamlRoot
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    /// <summary>
    /// 显示带详细信息的确认对话框
    /// </summary>
    /// <param name="xamlRoot">XAML根元素</param>
    /// <param name="title">标题</param>
    /// <param name="message">消息内容</param>
    /// <param name="primaryButtonText">主按钮文本</param>
    /// <param name="secondaryButtonText">次要按钮文本（可选）</param>
    /// <param name="closeButtonText">取消按钮文本</param>
    /// <returns>对话框结果</returns>
    public static async Task<ContentDialogResult> ShowDialogAsync(
        XamlRoot? xamlRoot,
        string title,
        string message,
        string primaryButtonText,
        string? secondaryButtonText = null,
        string closeButtonText = "取消")
    {
        if (xamlRoot == null) return ContentDialogResult.None;

        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = primaryButtonText,
            CloseButtonText = closeButtonText,
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = xamlRoot
        };

        if (!string.IsNullOrEmpty(secondaryButtonText))
        {
            dialog.SecondaryButtonText = secondaryButtonText;
        }

        return await dialog.ShowAsync();
    }
}
