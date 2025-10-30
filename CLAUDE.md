# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

XUnity.AutoInstaller (XUnity自动安装器) is a WinUI3 desktop application for automatically installing and configuring BepInEx (plugin framework) and XUnity.AutoTranslator (auto-translation plugin) for Unity games. The application handles version management, automatic game detection, configuration editing, and installation orchestration.

## Build Commands

### Standard Build
```bash
dotnet build -p:Platform=x64
```

### Platform-Specific Builds
```bash
# x86 build
dotnet build -p:Platform=x86

# ARM64 build
dotnet build -p:Platform=ARM64
```

### Running the Application
```bash
# Debug mode
dotnet run --project XUnity.AutoInstaller/XUnity.AutoInstaller.csproj

# Release mode
dotnet run --project XUnity.AutoInstaller/XUnity.AutoInstaller.csproj -c Release
```

### Publishing
```bash
# Self-contained deployment with trimming and AOT
dotnet publish -p:Platform=x64 -c Release
```

## Architecture

### Navigation Structure
- **MainWindow.xaml**: Root window with NavigationView sidebar and custom title bar
  - Uses Frame-based navigation to switch between pages
  - NavigationView.SelectionChanged event handles page routing via Tag properties
  - Custom title bar with `ExtendsContentIntoTitleBar = true` and Mica backdrop
  - Settings navigation uses FooterMenuItems with Chinese "设置" text (Tag="Settings")
  - Default window size: 1200x800px

### Page Organization
The application uses a 5-page structure in the `Pages/` folder:

1. **DashboardPage**: Game path selection via browse button, installation status cards (BepInEx/XUnity), quick actions. Uses `GameStateService` for path persistence.
2. **InstallPage**: Version selection (auto/manual), platform choice, installation progress, logs. Uses `GameStateService` instead of navigation parameters.
3. **ConfigPage**: Configuration editor for BepInEx and XUnity settings with browse button to select game path if not detected. Uses `GameStateService` for path management.
4. **VersionManagementPage**: Two-column layout - left shows installed versions/snapshots, right shows separate BepInEx and XUnity version lists with platform (x86/x64/ARM64) and architecture (Mono/IL2CPP) filters. Uses `GameStateService` and subscribes to `GamePathChanged` event.
5. **SettingsPage**: Application settings including theme, path memory, detailed progress, and default installation options. Accessed via NavigationView FooterMenuItems with Chinese "设置" text.

### Configuration Structure (ConfigPage)

The configuration page mirrors the actual configuration files used by BepInEx and XUnity.AutoTranslator:

**BepInEx Configuration** (maps to `BepInEx/config/BepInEx.cfg`):
- **Logging Section**: Console, ShiftJIS, LogLevels, Unity message logging
- **Preloader Section**: Entrypoint assembly/type/method, dump assemblies (advanced)

**XUnity.AutoTranslator Configuration** (maps to `BepInEx/config/AutoTranslatorConfig.ini`):
- **Service**: Translation endpoint selection (GoogleTranslate, BingTranslate, DeepL, etc.)
- **General**: Language, max characters, UI resizing, font override
- **Text Frameworks**: UGUI, NGUI, TextMeshPro, TextMesh, IMGUI toggles
- **Files**: Directory paths for translations, substitutions, preprocessors
- **Texture**: Texture translation and dumping settings
- **Advanced**: Translation scoping, rich text handling, HTML preprocessing
- **Authentication**: API keys for various translation services

All Expander controls use `HorizontalAlignment="Stretch"` and `HorizontalContentAlignment="Stretch"` to ensure proper layout filling.

### Technology Stack
- **.NET 9.0** (net9.0-windows10.0.26100.0)
- **WinUI3** via Microsoft.WindowsAppSDK 1.8.251003001
- **Octokit.NET 14.0.0** for GitHub API integration
- **SharpCompress 0.41.0** for ZIP extraction
- **Minimum OS**: Windows 10 17763 (October 2018 Update)
- **MSIX packaging** enabled for deployment
- **Nullable reference types** enabled

### Backend Architecture

The backend follows a service-oriented architecture organized into three main layers:

**Models/** - Data structures and enums:
- `GameEngine`, `Platform`, `PackageType` enums define supported game types and platforms. `Platform` includes x86, x64, IL2CPP_x86, IL2CPP_x64, and ARM64.
- `GameInfo`, `InstallationStatus`, `VersionInfo` represent game and version metadata. `VersionInfo.TargetPlatform` is nullable Platform?.
- `BepInExConfig`, `XUnityConfig` contain 40+ configuration properties mapping to INI files
- `InstallOptions`, `InstalledVersionInfo`, `SnapshotInfo` control installation behavior and version management
- `AppSettings` stores user preferences (theme, path memory, detailed progress, default backup/config options)

**Services/** - Business logic layer:
- `GameStateService`: **Singleton** for global game path management across all pages. Provides event-driven path changes (`GamePathChanged` event), auto-loads last path from settings on startup, and validates paths. All pages should use `GameStateService.Instance.CurrentGamePath` instead of maintaining local path state.
- `GameDetectionService`: Detects Unity engine type (Mono vs IL2CPP) by analyzing game executables and DLL files
- `GitHubApiClient`: Wraps Octokit to fetch BepInEx and XUnity releases, handles downloads with progress reporting
- `VersionService`: Coordinates version downloads, caching, local version detection, and snapshot management
- `ConfigurationService`: Parses and serializes BepInEx.cfg and AutoTranslatorConfig.ini files
- `FileSystemService`: Handles ZIP extraction, directory backup, and file operations
- `InstallationService`: Orchestrates the full installation pipeline (backup → clean → download → extract → configure)
- `SettingsService`: Manages application settings persistence using Windows ApplicationData

**Utils/** - Shared utilities:
- `IniParser`: Complete INI file parser supporting sections, key-value pairs, and type conversions
- `PathHelper`: Centralized path logic for BepInEx directories, Steam registry detection, common game locations
- `LogWriter`: Thread-safe logging with DispatcherQueue for UI updates

**Key Architectural Patterns:**

1. **Singleton Pattern**: `GameStateService` uses thread-safe singleton with auto-initialization for global state management
2. **Event-Driven Communication**: `GameStateService.GamePathChanged` event allows pages to react to path changes without tight coupling
3. **Async/Await Throughout**: All I/O operations are async with proper cancellation token support
4. **Progress Reporting**: Services accept `IProgress<T>` for real-time UI updates during long operations
5. **UI Thread Synchronization**: All page code-behind uses `DispatcherQueue.TryEnqueue()` to update UI from background threads
6. **No MVVM**: Direct code-behind approach with manual UI updates (no data binding framework)
7. **Static Service Methods**: FileSystemService uses static methods; others use instance methods for state management

### Code Patterns

**XAML Structure**:
- Use `<Button.Content>` child elements instead of `Content="..."` attribute to avoid duplication errors
- All child controls in Expanders should have `HorizontalAlignment="Stretch"`
- Card-style UI using `CardBackgroundFillColorDefaultBrush` and `CornerRadius="8"`

**C# Code-Behind**:
- Page constructors only call `InitializeComponent()` and set default UI states
- **Always use `GameStateService.Instance.CurrentGamePath`** for game path access across all pages
- Subscribe to `GameStateService.GamePathChanged` event in pages that need to react to path changes
- Event handlers are async when performing I/O operations
- WinUI3 FolderPicker requires HWND initialization: `WinRT.Interop.WindowNative.GetWindowHandle()` and `WinRT.Interop.InitializeWithWindow.Initialize()`
- Access MainWindow via `App.MainWindow` static property for HWND retrieval
- No MVVM pattern - direct code-behind approach with manual UI updates

**Installation Flow** (InstallPage → InstallationService):
1. Validate game path and detect engine type
2. Optionally backup existing BepInEx directory
3. Optionally clean old installation (delete BepInEx folder and winhttp.dll)
4. Download BepInEx for target platform from GitHub releases (auto-recommended or user-selected version)
5. Extract BepInEx to game root directory
6. Download XUnity.AutoTranslator from GitHub releases (auto-recommended or user-selected version)
7. Extract XUnity to `BepInEx/plugins/` directory
8. Apply recommended configuration (optional)
9. Create desktop shortcut (optional)

**Version Management Flow**:
- **Manual Version Selection**: When user switches to manual mode in InstallPage, `LoadVersionsAsync()` fetches all available versions from GitHub and populates ComboBoxes filtered by selected platform
- **Version Snapshots**: Users can create timestamped backups of their current BepInEx installation (stored in `BepInEx_Snapshots/` directory) and restore them later
- **Snapshot Structure**: Each snapshot contains BepInEx directory, winhttp.dll, doorstop_config.ini, and a snapshot.json metadata file with version info and creation timestamp

## Important Notes

### Configuration Accuracy
When modifying ConfigPage.xaml, configuration options must match the official documentation:
- **BepInEx**: https://github.com/BepInEx/BepInEx/wiki/Configuration
- **XUnity.AutoTranslator**: https://github.com/bbepis/XUnity.AutoTranslator

Use WebSearch or Context7 to verify configuration options against official sources.

### Platform Configuration
The project supports three platforms (x86, x64, ARM64). Always specify `-p:Platform=x64` (or desired platform) when building to avoid ambiguity.

### Game Detection Logic
`GameDetectionService.DetectGameEngine()` identifies Unity engine type by checking for:
- **UnityMono**: Presence of `UnityPlayer.dll` + `<GameName>_Data/Managed/Assembly-CSharp.dll`
- **UnityIL2CPP**: Presence of `UnityPlayer.dll` + `GameAssembly.dll` + `<GameName>_Data/il2cpp_data/`

**Note**: Auto-detection features have been removed from the UI. Users must manually browse and select game directories.

### Configuration File Mapping

**BepInEx.cfg** - INI format with sections:
- `[Logging.Console]`, `[Logging.Disk]` - Logging configuration
- `[Preloader]`, `[Preloader.Entrypoint]` - Preloader settings
- `[Chainloader]` - Plugin loading settings

**AutoTranslatorConfig.ini** - CRITICAL: The actual file structure differs from early assumptions:
- `[Service]` - Translation endpoint (e.g., Passthrough, GoogleTranslate)
- `[General]` - **Only contains Language and FromLanguage**
- `[Behaviour]` - **Most "General" settings are actually here:**
  - `MaxCharactersPerTranslation`, `MinDialogueChars`, `IgnoreWhitespaceInDialogue`
  - `EnableUIResizing`, `CopyToClipboard`, `OverrideFont`
  - **Advanced settings also in [Behaviour]:** `EnableTranslationScoping`, `HandleRichText`, `MaxTextParserRecursion`, `HtmlEntityPreprocessing`
- `[TextFrameworks]` - Keys use `Enable` prefix: `EnableUGUI`, `EnableNGUI`, `EnableTextMeshPro`, `EnableTextMesh`, `EnableIMGUI`
- `[Files]` - Translation file paths (Directory, OutputFile, SubstitutionFile, etc.)
- `[Texture]` - Keys use `Texture` prefix or `Enable` prefix:
  - `TextureDirectory` (not `Directory`)
  - `EnableTextureTranslation` (not `EnableTranslation`)
  - `EnableTextureDumping`, `TextureHashGenerationStrategy`
- **Authentication sections** - Each service has its own section:
  - `[GoogleLegitimate]` → `GoogleAPIKey`
  - `[BingLegitimate]` → `OcpApimSubscriptionKey`
  - `[DeepLLegitimate]` → `ApiKey`
  - `[Baidu]` → `BaiduAppId`, `BaiduAppSecret`
  - `[Yandex]` → `YandexAPIKey`

**IMPORTANT**: When reading/writing XUnity config, use the correct section names:
- ❌ WRONG: `IniParser.GetInt(data, "General", "MaxCharactersPerTranslation", 200)`
- ✅ CORRECT: `IniParser.GetInt(data, "Behaviour", "MaxCharactersPerTranslation", 200)`

`IniParser.Parse()` returns `Dictionary<string, Dictionary<string, string>>` for section → key → value structure. Type conversion helpers: `GetBool()`, `GetInt()`, `GetValue()` with defaults.

### GitHub API Integration
- `GitHubApiClient` uses Octokit with product header "XUnity-AutoInstaller"
- Rate limit checking via `GetRateLimitAsync()` (default: 60 requests/hour for unauthenticated)
- Asset filtering: BepInEx Mono versions from GitHub (x64, x86), IL2CPP versions from builds.bepinex.dev
- Downloads use HttpClient with `ResponseHeadersRead` for streaming large files with progress reporting

### IL2CPP Version Detection
**BepInExBuildsApiClient** fetches IL2CPP versions from builds.bepinex.dev:
- Scrapes HTML from `https://builds.bepinex.dev/projects/bepinex_be`
- Uses multiple regex patterns for compatibility: `#(\d+).*6\.0\.0-be\.(\d+)\+([a-f0-9]+)`
- **Performance optimization**: Only fetches latest 5 builds (changed from 50 to reduce load time from 30-60s to 3-6s)
- Performs HEAD requests to verify file existence before adding to version list
- All IL2CPP versions are marked as `IsPrerelease = true`
- Extensive `System.Diagnostics.Debug.WriteLine()` logging with `[IL2CPP]` prefix for troubleshooting
- Fallback: If HEAD request fails, still adds version with FileSize=0

### Version Snapshot System
Version snapshots allow users to save and restore different BepInEx configurations:

**Snapshot Creation** (`VersionService.CreateSnapshotAsync`):
- Creates `BepInEx_Snapshots/` directory in game root
- Copies entire BepInEx folder, winhttp.dll, and doorstop_config.ini
- Generates unique snapshot name: `{UserName}_{yyyyMMdd_HHmmss}`
- Saves metadata to snapshot.json (name, creation time, BepInEx version, XUnity version)

**Snapshot Restoration** (`VersionService.RestoreSnapshotAsync`):
- Uninstalls current BepInEx installation
- Copies snapshot contents back to game directory
- Restores all configuration and plugin files

**UI Integration** (VersionManagementPage):
- **Left panel**: Displays current installation and snapshots using `InstalledVersionDisplayItem` with `ItemTemplate`
  - Headers: "当前安装" and "版本快照"
  - Icons: ✓ for current installation, 📷 for snapshots
  - Each snapshot has individual "恢复" and "删除" buttons in its row
  - Uses `InstalledItemType` enum (Header, CurrentInstallation, Snapshot) to control visibility
  - `InstalledVersionDisplayItem` has `HeaderVisibility`, `ContentVisibility`, `ButtonsVisibility` properties
- **Right panel**: Separate lists for BepInEx and XUnity versions with `VersionDisplayItem` wrapper
  - Platform filter ComboBox: 全部平台 / x86 / x64 / ARM64 (filters BepInEx versions only)
  - Architecture filter ComboBox: 全部架构 / Mono / IL2CPP (filters BepInEx versions only)
  - Version type filter: 全部版本 / 稳定版 / 预览版 (filters both BepInEx and XUnity)
  - Each version row has a "下载" button that downloads to cache without installing
  - Display format uses `GetPlatformDisplayName()`: "Mono x64", "IL2CPP x64", etc.
- Global "创建快照" button creates snapshot with custom name dialog
- Removed global "恢复快照" and "删除快照" buttons (now per-item)

### Common Pitfalls and Solutions

**VersionInfo.Platform vs TargetPlatform:**
- Use `VersionInfo.TargetPlatform` (nullable `Platform?`), not `Platform`
- When filtering versions, check `v.TargetPlatform == Platform.x64` with nullable comparison
- Use null-coalescing for display: `version.TargetPlatform?.ToString() ?? "未知"`

**GameStateService Initialization:**
- `GameStateService.Instance.Initialize()` must be called in `App.OnLaunched()` before window activation
- All pages should use `GameStateService.Instance.CurrentGamePath` instead of maintaining local `_gamePath` fields
- Subscribe to `GamePathChanged` event in page constructor or OnNavigatedTo, unsubscribe in destructor if needed

**SharpCompress Archive Entries:**
- `archive.Entries` returns `IEnumerable<IArchiveEntry>`, not `ICollection`
- Use `.ToList()` to get a countable collection for progress calculation

**Octokit Namespace Conflicts:**
- `Octokit.PackageType` conflicts with `XUnity.AutoInstaller.Models.PackageType`
- Use alias: `using PackageType = XUnity.AutoInstaller.Models.PackageType;` in files with Octokit imports

**Task.Run Lambda Type Inference:**
- Explicit cast may be needed: `Task.Run((Action)(() => { ... }))` when capturing variables in lambda

**WinUI3 ContentDialog:**
- Must set `XamlRoot` property to page's `XamlRoot` before calling `ShowAsync()`
- Use `await dialog.ShowAsync()` to wait for user response

**WinUI3 FontWeight:**
- Use `Microsoft.UI.Text.FontWeights.SemiBold` instead of creating new FontWeight structs with Value property
- FontWeight in WinUI3 is a struct without a public Value setter

**Manual Version Selection Implementation:**
- Use `VersionDisplayItem` wrapper class with `Display` property for ComboBox binding
- Set ComboBox's `DisplayMemberPath="Display"` to show formatted version strings
- Filter BepInEx versions by selected platform; XUnity versions are platform-independent
- Pass `VersionInfo.Version` string to `InstallOptions.BepInExVersion`/`XUnityVersion` properties

**Settings Persistence:**
- `SettingsService` uses `ApplicationData.Current.LocalSettings` for storing key-value pairs
- Theme changes are applied immediately via `SettingsService.ApplyTheme()` which updates the root FrameworkElement's `RequestedTheme`
- Settings are saved explicitly via Save button; no auto-save on navigation
- `GameStateService` is initialized in `App.xaml.cs.OnLaunched()` to auto-load last game path on startup

**Game Path State Management:**
- Use `GameStateService.Instance.SetGamePath(path, saveToSettings: true)` to update path globally
- All pages receive `GamePathChanged` events when path changes
- Path is automatically saved to settings when `saveToSettings: true` is passed
- `GameStateService.Instance.Initialize()` must be called once in `App.OnLaunched()` to restore saved path

### UI Design Language
The application follows Windows 11 Fluent Design principles with Mica backdrop material. Maintain consistency with:
- `FontIcon` glyphs for icons (Segoe Fluent Icons)
- `AccentButtonStyle` for primary actions
- `InfoBar` for status messages (preferred over ContentDialog for non-blocking errors)
- `ProgressRing` and `ProgressBar` for loading states

### Debugging and Logging
All services use `System.Diagnostics.Debug.WriteLine()` for diagnostic logging:
- `[Config]` prefix - ConfigurationService (file paths, parsed values, section counts)
- `[IL2CPP]` prefix - BepInExBuildsApiClient (HTML parsing, regex matches, HTTP responses)
- `[VersionService]` prefix - VersionService (version counts, filter operations)

To view logs during development:
- Visual Studio: Debug → Windows → Output
- Or use DebugView tool from Sysinternals

Common log patterns:
```csharp
System.Diagnostics.Debug.WriteLine($"[IL2CPP] 开始获取 IL2CPP 版本，URL: {url}");
System.Diagnostics.Debug.WriteLine($"[Config] MaxCharactersPerTranslation = {value}");
```

### Recent Bug Fixes (Reference)
1. **ConfigPage freezing**: Changed from recursive ContentDialog to InfoBar (`ConfigPage.xaml.cs:36-50`)
2. **IL2CPP versions missing**: Created BepInExBuildsApiClient with web scraping (`BepInExBuildsApiClient.cs`)
3. **Config values not loading**: Fixed section/key names to match actual INI structure (`ConfigurationService.cs:125-170`)
4. **Version list performance**: Reduced IL2CPP fetch from 50 to 5 builds (`BepInExBuildsApiClient.cs:55`)
5. **Snapshot buttons missing**: Restored ItemTemplate with data binding (`VersionManagementPage.xaml:65-141`)
