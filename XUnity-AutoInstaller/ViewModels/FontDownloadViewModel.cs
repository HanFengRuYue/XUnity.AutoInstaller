using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using XUnity_AutoInstaller.Models;
using XUnity_AutoInstaller.Services;

namespace XUnity_AutoInstaller.ViewModels;

public partial class FontDownloadViewModel : ObservableObject
{
    private readonly GameStateService _gameStateService;
    private readonly FontManagementService _fontManagementService;

    private List<FontResourceInfo> _allFonts = new();
    private string? _detectedUnityVersion;
    private string _currentSortColumn = "";
    private bool _currentSortAscending = true;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool hasError;

    [ObservableProperty]
    private string errorMessage = string.Empty;

    [ObservableProperty]
    private string unityVersion = "未选择游戏";

    [ObservableProperty]
    private string fontNameFilter = string.Empty;

    [ObservableProperty]
    private int selectedUnityVersionFilterIndex;

    [ObservableProperty]
    private ObservableCollection<FontResourceInfo> fonts = new();

    [ObservableProperty]
    private bool isDownloading;

    [ObservableProperty]
    private int downloadProgress;

    [ObservableProperty]
    private string downloadStatusMessage = string.Empty;

    // Visibility properties
    public Visibility LoadingVisibility => IsLoading ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ContentVisibility => !IsLoading && !HasError && Fonts.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility EmptyVisibility => !IsLoading && !HasError && Fonts.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ErrorVisibility => HasError ? Visibility.Visible : Visibility.Collapsed;
    public Visibility DownloadDialogVisibility => IsDownloading ? Visibility.Visible : Visibility.Collapsed;

    // Sort icon visibility
    public Visibility FontNameSortIconVisibility => _currentSortColumn == "fontname" ? Visibility.Visible : Visibility.Collapsed;
    public Visibility UnityVersionSortIconVisibility => _currentSortColumn == "unityversion" ? Visibility.Visible : Visibility.Collapsed;
    public Visibility FileSizeSortIconVisibility => _currentSortColumn == "filesize" ? Visibility.Visible : Visibility.Collapsed;

    // Sort icon glyphs
    public string FontNameSortGlyph => _currentSortAscending ? "\uE74A" : "\uE74B";
    public string UnityVersionSortGlyph => _currentSortAscending ? "\uE74A" : "\uE74B";
    public string FileSizeSortGlyph => _currentSortAscending ? "\uE74A" : "\uE74B";

    public FontDownloadViewModel(
        GameStateService gameStateService,
        FontManagementService fontManagementService)
    {
        _gameStateService = gameStateService;
        _fontManagementService = fontManagementService;

        _gameStateService.GamePathChanged += OnGamePathChanged;
    }

    partial void OnIsLoadingChanged(bool value)
    {
        OnPropertyChanged(nameof(LoadingVisibility));
        OnPropertyChanged(nameof(ContentVisibility));
        OnPropertyChanged(nameof(EmptyVisibility));
    }

    partial void OnHasErrorChanged(bool value)
    {
        OnPropertyChanged(nameof(ErrorVisibility));
        OnPropertyChanged(nameof(ContentVisibility));
        OnPropertyChanged(nameof(EmptyVisibility));
    }

    partial void OnFontsChanged(ObservableCollection<FontResourceInfo> value)
    {
        OnPropertyChanged(nameof(ContentVisibility));
        OnPropertyChanged(nameof(EmptyVisibility));
    }

    partial void OnIsDownloadingChanged(bool value)
    {
        OnPropertyChanged(nameof(DownloadDialogVisibility));
    }

    partial void OnFontNameFilterChanged(string value)
    {
        ApplyFilters();
    }

    partial void OnSelectedUnityVersionFilterIndexChanged(int value)
    {
        ApplyFilters();
    }

    public async Task InitializeAsync()
    {
        UpdateGameInfo();
        await LoadFontsAsync();
    }

    private void UpdateGameInfo()
    {
        var gamePath = _gameStateService.CurrentGamePath;

        if (string.IsNullOrEmpty(gamePath))
        {
            UnityVersion = "未选择游戏";
        }
        else
        {
            UnityVersion = "检测中...";
        }
    }

    [RelayCommand]
    private async Task LoadFontsAsync()
    {
        try
        {
            IsLoading = true;
            HasError = false;

            var gamePath = _gameStateService.CurrentGamePath;

            List<FontResourceInfo> fonts;

            if (string.IsNullOrEmpty(gamePath))
            {
                fonts = await _fontManagementService.GetAvailableFontsAsync();
                _detectedUnityVersion = null;
                UnityVersion = "未选择游戏";
            }
            else
            {
                fonts = await _fontManagementService.GetRecommendedFontsAsync(gamePath);

                var detector = new UnityVersionDetector();
                _detectedUnityVersion = await detector.DetectUnityVersionAsync(gamePath);
                UnityVersion = UnityVersionDetector.FormatVersionForDisplay(_detectedUnityVersion) ?? "未知";

                // Mark exact matches as recommended
                if (!string.IsNullOrEmpty(_detectedUnityVersion))
                {
                    foreach (var font in fonts)
                    {
                        font.IsRecommended = font.UnityVersion == _detectedUnityVersion;
                    }
                }

                // Auto-filter by Unity major version
                if (!string.IsNullOrEmpty(_detectedUnityVersion))
                {
                    var majorVersion = _detectedUnityVersion.Split('-').FirstOrDefault();
                    if (!string.IsNullOrEmpty(majorVersion))
                    {
                        // Map major version to filter index
                        if (int.TryParse(majorVersion, out int ver) && ver >= 6000)
                        {
                            SelectedUnityVersionFilterIndex = 1; // Unity 6
                        }
                        else
                        {
                            SelectedUnityVersionFilterIndex = majorVersion switch
                            {
                                "2017" => 2,
                                "2018" => 3,
                                "2019" => 4,
                                "2020" => 5,
                                "2021" => 6,
                                "2022" => 7,
                                _ => 0 // All versions
                            };
                        }
                    }
                }
            }

            _allFonts = fonts;
            ApplyFilters();

            LogService.Instance.Log($"加载 {_allFonts.Count} 个字体", LogLevel.Info, "[FontDownload]");
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"加载字体列表失败: {ex.Message}", LogLevel.Error, "[FontDownload]");
            HasError = true;
            ErrorMessage = $"加载失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ClearFilter()
    {
        FontNameFilter = string.Empty;
        SelectedUnityVersionFilterIndex = 0;
    }

    [RelayCommand]
    private void SortByFontName()
    {
        if (_currentSortColumn == "fontname")
        {
            _currentSortAscending = !_currentSortAscending;
        }
        else
        {
            _currentSortColumn = "fontname";
            _currentSortAscending = true;
        }

        UpdateSortIcons();
        ApplyFilters();
    }

    [RelayCommand]
    private void SortByUnityVersion()
    {
        if (_currentSortColumn == "unityversion")
        {
            _currentSortAscending = !_currentSortAscending;
        }
        else
        {
            _currentSortColumn = "unityversion";
            _currentSortAscending = true;
        }

        UpdateSortIcons();
        ApplyFilters();
    }

    [RelayCommand]
    private void SortByFileSize()
    {
        if (_currentSortColumn == "filesize")
        {
            _currentSortAscending = !_currentSortAscending;
        }
        else
        {
            _currentSortColumn = "filesize";
            _currentSortAscending = true;
        }

        UpdateSortIcons();
        ApplyFilters();
    }

    [RelayCommand]
    private async Task DownloadFontAsync(FontResourceInfo fontInfo)
    {
        if (fontInfo == null) return;

        try
        {
            IsDownloading = true;
            DownloadProgress = 0;
            DownloadStatusMessage = $"正在下载: {fontInfo.DisplayFontName}";

            var progress = new Progress<int>(percentage =>
            {
                DownloadProgress = percentage;
                DownloadStatusMessage = $"正在下载: {fontInfo.DisplayFontName} ({percentage}%)";
            });

            await _fontManagementService.DownloadFontAsync(fontInfo, progress);

            LogService.Instance.Log($"字体下载成功: {fontInfo.FileName}", LogLevel.Info, "[FontDownload]");

            // Refresh list
            await LoadFontsAsync();
        }
        catch (Exception ex)
        {
            LogService.Instance.Log($"字体下载失败: {ex.Message}", LogLevel.Error, "[FontDownload]");
            throw; // Re-throw to let view handle error dialog
        }
        finally
        {
            IsDownloading = false;
        }
    }

    [RelayCommand]
    private async Task InstallFontAsync(FontResourceInfo fontInfo)
    {
        if (fontInfo == null) return;

        var gamePath = _gameStateService.CurrentGamePath;

        if (string.IsNullOrEmpty(gamePath))
        {
            throw new InvalidOperationException("请先在仪表盘页面选择游戏目录");
        }

        await _fontManagementService.InstallFontToGameAsync(gamePath, fontInfo);

        LogService.Instance.Log($"字体安装成功: {fontInfo.FileName}", LogLevel.Info, "[FontDownload]");

        // Refresh list
        await LoadFontsAsync();
    }

    private void ApplyFilters()
    {
        var filteredFonts = new List<FontResourceInfo>(_allFonts);

        // Apply font name filter
        var fontNameFilterValue = FontNameFilter?.Trim().ToLower();
        if (!string.IsNullOrEmpty(fontNameFilterValue))
        {
            filteredFonts = filteredFonts.Where(f =>
                f.FontName.ToLower().Contains(fontNameFilterValue) ||
                (!string.IsNullOrEmpty(f.ChineseName) && f.ChineseName.ToLower().Contains(fontNameFilterValue))).ToList();
        }

        // Apply Unity version filter
        var versionFilter = SelectedUnityVersionFilterIndex switch
        {
            1 => "6000", // Unity 6 (6000+)
            2 => "2017",
            3 => "2018",
            4 => "2019",
            5 => "2020",
            6 => "2021",
            7 => "2022",
            _ => null // All versions
        };

        if (!string.IsNullOrEmpty(versionFilter))
        {
            filteredFonts = filteredFonts.Where(f =>
                f.UnityMajorVersion.ToString() == versionFilter ||
                (versionFilter == "6000" && f.UnityMajorVersion >= 6000)).ToList();
        }

        // Apply sorting
        filteredFonts = _currentSortColumn switch
        {
            "fontname" => _currentSortAscending
                ? filteredFonts.OrderBy(f => f.SortFontName).ToList()
                : filteredFonts.OrderByDescending(f => f.SortFontName).ToList(),
            "unityversion" => _currentSortAscending
                ? filteredFonts.OrderBy(f => f.UnityVersion).ToList()
                : filteredFonts.OrderByDescending(f => f.UnityVersion).ToList(),
            "filesize" => _currentSortAscending
                ? filteredFonts.OrderBy(f => f.FileSize).ToList()
                : filteredFonts.OrderByDescending(f => f.FileSize).ToList(),
            _ => filteredFonts
        };

        Fonts.Clear();
        foreach (var font in filteredFonts)
        {
            Fonts.Add(font);
        }
    }

    private void UpdateSortIcons()
    {
        OnPropertyChanged(nameof(FontNameSortIconVisibility));
        OnPropertyChanged(nameof(UnityVersionSortIconVisibility));
        OnPropertyChanged(nameof(FileSizeSortIconVisibility));
        OnPropertyChanged(nameof(FontNameSortGlyph));
        OnPropertyChanged(nameof(UnityVersionSortGlyph));
        OnPropertyChanged(nameof(FileSizeSortGlyph));
    }

    private void OnGamePathChanged(object? sender, string? newPath)
    {
        UpdateGameInfo();
        _ = LoadFontsAsync();
    }

    public void Cleanup()
    {
        _gameStateService.GamePathChanged -= OnGamePathChanged;
    }
}
