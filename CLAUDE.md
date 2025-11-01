# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

XUnity-AutoInstaller (XUnity自动安装器) is a WinUI3 desktop application for automatically installing and configuring BepInEx (plugin framework) and XUnity.AutoTranslator (auto-translation plugin) for Unity games. The application handles version management, automatic game detection, configuration editing, and installation/uninstallation orchestration.

## Build Commands

### Standard Build
```bash
dotnet build -p:Platform=x64
```

### Platform-Specific Builds
```bash
# x86 build
dotnet build -p:Platform=x86

# ARM64 build (Note: Plugin support discontinued, but build system still supports it)
dotnet build -p:Platform=ARM64
```

### Running the Application
```bash
# Debug mode
dotnet run --project XUnity-AutoInstaller/XUnity-AutoInstaller.csproj

# Release mode
dotnet run --project XUnity-AutoInstaller/XUnity-AutoInstaller.csproj -c Release
```

### Publishing
```bash
# Self-contained deployment with trimming and single-file exe
dotnet publish -p:Platform=x64 -c Release

# Automated Release Build (generates single exe in Release/)
powershell.exe -ExecutionPolicy Bypass -File Build-Release.ps1
```

### Visual Studio Debugging
When debugging in Visual Studio, ensure you:
1. Use the **"XUnity-AutoInstaller (Unpackaged)"** launch profile (not Package mode)
2. If debugging fails after renaming or configuration changes:
   - Close Visual Studio completely
   - Delete cache: `.vs`, `bin`, and `obj` folders
   - Reopen solution and rebuild
3. Launch profiles are defined in `Properties/launchSettings.json`
4. Package manifest is in `Package.appxmanifest` (display names only, not used for unpackaged deployment)

## Architecture

### Navigation Structure
- **MainWindow.xaml**: Root window with NavigationView sidebar and custom title bar
  - Uses Frame-based navigation to switch between pages via Tag properties
  - Custom title bar with `ExtendsContentIntoTitleBar = true` and Mica backdrop
  - Settings navigation uses FooterMenuItems (Tag="Settings")
  - Default window size: 1200x800px

### Page Organization
The application uses a 6-page structure in the `Pages/` folder:

1. **DashboardPage**: Game path selection, installation status cards (BepInEx/XUnity), quick actions
2. **InstallPage**: Version selection (auto/manual), platform choice, installation progress
3. **ConfigPage**: Configuration editor for BepInEx and XUnity settings with 17 translation service endpoints
4. **VersionManagementPage**: Vertical layout with installed versions at top, available versions below. Platform filters exclude ARM64 (discontinued plugin support)
5. **LogPage**: Unified logging page showing all application output with filtering and auto-scroll
6. **SettingsPage**: Application settings including theme, path memory, GitHub Token configuration

All pages use `GameStateService.Instance.CurrentGamePath` for global path access.

### Technology Stack
- **.NET 9.0** (net9.0-windows10.0.26100.0)
- **WinUI3** via Microsoft.WindowsAppSDK 1.8.251003001
- **Octokit.NET 14.0.0** for GitHub API integration
- **SharpCompress 0.41.0** for ZIP extraction
- **Minimum OS**: Windows 10 17763 (October 2018 Update)
- **Deployment Mode**: Unpackaged (WindowsPackageType=None, WindowsAppSDKSelfContained=true)
- **Nullable reference types** enabled

### Unpackaged App Configuration
This application uses **unpackaged deployment** mode with self-contained Windows App SDK:

**Project Settings (XUnity-AutoInstaller.csproj)**:
- `<WindowsPackageType>None</WindowsPackageType>` - Disables MSIX packaging
- `<WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>` - Enables self-contained deployment with **automatic Bootstrap initialization**
- `<EnableMsixTooling>true</EnableMsixTooling>` - Kept enabled for resources.pri generation
- `<PublishSingleFile>true</PublishSingleFile>` - Generates single-file executable in Release builds

**Critical Implementation Details**:
1. **Bootstrap Initialization**: When `WindowsAppSDKSelfContained=true` and `WindowsPackageType=None` are set, the Windows App SDK **automatically** performs Bootstrap initialization. **DO NOT** manually call `Bootstrap.Initialize()` or `DeploymentManager.Initialize()` - this causes fast-fail crashes (0xc0000602).

2. **Settings Storage**: Unpackaged apps cannot use `ApplicationData.Current` (requires package identity). This app uses **JSON file** at `%AppData%\Roaming\XUnity-AutoInstaller\settings.json` via `SettingsService.cs` (migrated from legacy Registry storage).

3. **Version Retrieval**: Cannot use `Package.Current.Id.Version`. Use `Assembly.GetExecutingAssembly().GetName().Version` instead (see `SettingsService.GetAppVersion()`).

4. **App.xaml.cs Entry Point**: No custom `Main()` entry point needed. The app relies on default WinUI3 initialization with auto-Bootstrap:
```csharp
public App()
{
    // Bootstrap.Initialize is called automatically by WindowsAppSDKSelfContained=true
    InitializeComponent();
    this.UnhandledException += App_UnhandledException;
}
```

**Build Output**:
- Debug builds: Standard multi-file output in `bin/`
- Release builds: Single-file exe (~143 MB) with trimming and ReadyToRun compilation
- `Build-Release.ps1`: PowerShell script that automates Release build and copies exe to `Release/` folder

### Backend Architecture

The backend follows a service-oriented architecture organized into three main layers:

**Models/** - Data structures and enums:
- `GameEngine`, `Platform`, `PackageType` enums (Platform includes x86, x64, IL2CPP variants, ARM64)
- `VersionInfo.TargetPlatform` is nullable `Platform?`
- `BepInExConfig`: 30 configuration properties covering Caching, Chainloader, Harmony.Logger, Logging (Console/Disk), and Preloader sections
- `XUnityConfig`: 50+ configuration properties covering Service, General, Behaviour, TextFrameworks, Files, Texture, Advanced, Authentication (8 services), Http, Debug, Optimization, Integration, and ResourceRedirector sections
- `InstallOptions`: Includes `LaunchGameToGenerateConfig` (bool) and `ConfigGenerationTimeout` (int) for automatic config generation
- `AppSettings` stores user preferences including GitHub Token

**Services/** - Business logic layer:
- `LogService`: **Singleton** for unified application logging with event-driven updates to LogPage. Replaces all `Debug.WriteLine()` calls
- `GameStateService`: **Singleton** for global game path management. Use `Instance.CurrentGamePath` and subscribe to `GamePathChanged` event
- `InstallationStateService`: **Singleton** for global installation progress tracking. Provides cross-page installation state management with events (`InstallationStarted`, `ProgressChanged`, `InstallationCompleted`) and `CreateProgressReporter()` method for IProgress integration
- `VersionCacheService`: **Singleton** for global version list caching. Initialized once at app startup, provides cached versions to all pages, only refreshes on manual request in VersionManagementPage
- `GitHubAtomFeedClient`: **Primary version source** - Fetches releases via Atom feed (no rate limits, no authentication required). Parses XML and constructs download URLs
- `GitHubApiClient`: **Fallback/Token mode** - Supports optional GitHub Token authentication (60→5000 requests/hour), rate limit error handling, and API pagination limiting (maxCount parameter defaults to 3 versions)
- `VersionService`: **Smart mode switching** - Uses `GitHubAtomFeedClient` by default (no rate limits), automatically switches to `GitHubApiClient` when Token configured or on Atom feed failure
- `BepInExBuildsApiClient`: Fetches IL2CPP versions (optimized to latest 1 build only)
- `ConfigurationService`: Parses INI files with correct section mappings for XUnity config
- `InstallationService`: Orchestrates installation pipeline using `LogWriter` which internally uses `LogService`. Thread-safe with concurrent installation prevention
- `GameLauncherService`: Launches game, monitors config file generation (BepInEx.cfg, AutoTranslatorConfig.ini), auto-closes game when configs are detected. Includes timeout handling and diagnostic capabilities
- `SettingsService`: Manages settings persistence via **JSON file** at `%AppData%\Roaming\XUnity-AutoInstaller\settings.json` (migrated from Registry) for unpackaged app compatibility. Stores GitHub Token, theme, path memory, and other user preferences

**Utils/** - Shared utilities:
- `IniParser`: INI file parser with type conversion helpers
- `PathHelper`: Centralized path logic for BepInEx directories
- `LogWriter`: Adapter that uses `LogService` internally for unified logging

**Key Architectural Patterns:**

1. **Singleton Pattern**: `GameStateService`, `LogService`, `InstallationStateService`, and `VersionCacheService` use thread-safe singletons
2. **Event-Driven Communication**:
   - `GameStateService.GamePathChanged` - Game path changes
   - `LogService.LogEntryAdded` - New log entries
   - `VersionCacheService.VersionsUpdated` - Version list updates
   - `InstallationStateService.InstallationStarted/ProgressChanged/InstallationCompleted` - Installation progress tracking
3. **Global State Management**: Cross-page state sharing via singleton services (`InstallationStateService` for install progress, `GameStateService` for game path)
4. **Global Caching**: `VersionCacheService` provides application-wide version caching, initialized once at startup in `App.OnLaunched()`, preventing redundant API calls
5. **Async/Await Throughout**: All I/O operations with proper cancellation token support
6. **Progress Reporting**: Services use `IProgress<(int, string)>` for real-time UI updates. `InstallationStateService.CreateProgressReporter()` provides standardized progress reporting
7. **UI Thread Synchronization**: Use `DispatcherQueue.TryEnqueue()` for UI updates
8. **No MVVM**: Direct code-behind approach with manual UI updates
9. **Lazy Initialization**: VersionService lazy-loads GitHubApiClient with settings, but prioritizes VersionCacheService
10. **Thread Safety**: `InstallationService` prevents concurrent installations with lock-based synchronization

### Code Patterns

**XAML Structure**:
- Use `<Button.Content>` child elements instead of `Content="..."` attribute
- All child controls in Expanders should have `HorizontalAlignment="Stretch"`
- Card-style UI using `CardBackgroundFillColorDefaultBrush` and `CornerRadius="8"`

**C# Code-Behind**:
- Page constructors: `InitializeComponent()` → subscribe to events → explicitly set control properties → use `Loaded` event for deferred XAML control access
- **CRITICAL**: Never access XAML controls in constructor before `Loaded` event fires (causes NullReferenceException)
- **CRITICAL**: WinUI3 RadioButtons - Setting `IsChecked="True"` on a RadioButton does NOT set `RadioButtons.SelectedIndex`. Always explicitly set `SelectedIndex` in constructor:
  ```csharp
  public InstallPage()
  {
      this.InitializeComponent();
      // WinUI3 requires explicit SelectedIndex initialization
      VersionModeRadio.SelectedIndex = 0;  // Even if XAML has IsChecked="True"
  }
  ```
- Use `this.Loaded += OnPageLoaded` pattern to access controls safely after XAML initialization
- Add null checks in all methods that access XAML controls for defensive programming
- **Always use `GameStateService.Instance.CurrentGamePath`** for game path access
- Subscribe to `GameStateService.GamePathChanged` event for path change notifications
- WinUI3 FolderPicker requires HWND: `WinRT.Interop.WindowNative.GetWindowHandle()` and `WinRT.Interop.InitializeWithWindow.Initialize()`
- Access MainWindow via `App.MainWindow` static property

**Logging Pattern**:
- Use `LogService.Instance.Log(message, LogLevel, prefix)` for all logging
- Log levels: `Debug`, `Info`, `Warning`, `Error`
- Use consistent prefixes: `[Config]`, `[IL2CPP]`, `[GitHub]`, `[VersionService]`, `[VersionCache]`, `[版本管理]`, `[安装]`, `[Settings]`
- Never use `System.Diagnostics.Debug.WriteLine()` directly

**Installation Progress Pattern**:
- Use `InstallationStateService.Instance` for cross-page installation progress tracking
- Subscribe to events in page constructors:
  ```csharp
  public DashboardPage()
  {
      this.InitializeComponent();
      InstallationStateService.Instance.InstallationStarted += OnInstallationStarted;
      InstallationStateService.Instance.ProgressChanged += OnProgressChanged;
      InstallationStateService.Instance.InstallationCompleted += OnInstallationCompleted;
  }
  ```
- Create progress reporter in InstallationService:
  ```csharp
  var progress = InstallationStateService.Instance.CreateProgressReporter();
  await SomeOperationAsync(progress);  // Pass IProgress<(int, string)>
  ```
- Always call `StartInstallation()` before starting and `CompleteInstallation(success)` when done
- Check `IsInstalling` property to prevent concurrent installations or show installation status

**Version Management Pattern**:
- **Never call GitHub API directly in pages** - always use `VersionCacheService.Instance`
- **CRITICAL**: Pages must NEVER call `VersionCacheService.RefreshAsync()` - only read from cache
  - ✅ CORRECT: `_versionCacheService.GetBepInExVersions()` (read from cache)
  - ❌ WRONG: `await _versionCacheService.RefreshAsync()` (triggers network call)
- Pages read from cache via `GetBepInExVersions()`, `GetXUnityVersions()`, or helper methods
- Subscribe to `VersionCacheService.VersionsUpdated` event in page constructors
- **Only `VersionManagementPage` can trigger refresh** via `VersionCacheService.RefreshAsync()` (manual "刷新" button)
- Cache initialized once in `App.OnLaunched()` via background `InitializeVersionCacheAsync()`
- If cache not initialized on page load, wait for initialization (max 10s), don't trigger refresh:
  ```csharp
  if (!_versionCacheService.IsInitialized)
  {
      var startTime = DateTime.Now;
      while (!_versionCacheService.IsInitialized && (DateTime.Now - startTime).TotalSeconds < 10)
      {
          await Task.Delay(500);
      }
  }
  ```
- **Atom Feed vs API**:
  - Default: `GitHubAtomFeedClient` (no rate limits, no auth required)
  - Token mode: `GitHubApiClient` when user configures GitHub Token (more detailed info)
  - Auto-fallback: Atom feed failure automatically switches to API
  - Download URLs constructed from version info: `https://github.com/{owner}/{repo}/releases/download/{tag}/{filename}`

**Installation Flow** (InstallationService.InstallAsync):
1. Validate game path and detect engine type (`PathHelper.IsValidGameDirectory()`)
2. Check for concurrent installation (thread-safe lock prevents multiple simultaneous installs)
3. Notify `InstallationStateService.StartInstallation()` to broadcast installation start event
4. Optionally backup existing BepInEx directory (`InstallOptions.BackupExisting`)
5. Optionally clean old installation (`InstallOptions.CleanOldVersion`)
6. Download BepInEx for target platform from GitHub (`VersionService.DownloadVersionAsync()`)
7. Extract BepInEx to game root (uses SharpCompress)
8. Download XUnity.AutoTranslator from GitHub
9. Extract XUnity to `BepInEx/plugins/`
10. **Auto-launch game to generate configs** (`InstallOptions.LaunchGameToGenerateConfig`, optional, default enabled):
   - Launches game executable via `GameLauncherService`
   - Monitors for BepInEx.cfg and AutoTranslatorConfig.ini generation (polls every 500ms)
   - Verifies file size > 0 to ensure complete writes
   - Auto-closes game gracefully (3s timeout before force kill)
   - Includes diagnostic logging if generation fails
   - Configurable timeout (default 60s, range 10-300s via `InstallOptions.ConfigGenerationTimeout`)
11. Report progress via `InstallationStateService.CreateProgressReporter()` throughout entire flow
12. Notify `InstallationStateService.CompleteInstallation(success)` to broadcast completion event

**Uninstallation Flow** (UninstallationService.UninstallAsync):
1. Validate game path and check installation status
2. Delete BepInEx directory
3. Delete winhttp.dll (BepInEx entry point)
4. Delete doorstop_config.ini
5. Delete other related files (.doorstop_version, doorstop.dll, changelog.txt)
6. Verify uninstallation completed (`GameDetectionService.DetectInstallationStatus()`)

## Important Notes

### Configuration File Mapping

**BepInEx.cfg** - INI format with 30 configuration options across sections:
- `[Caching]` - EnableAssemblyCache
- `[Chainloader]` - HideManagerGameObject, LogLevels, LogUnityMessages
- `[Harmony.Logger]` - LogChannels (None, Warn/Error, All)
- `[Logging]` - UnityLogListening, LogConsoleToUnityLog
- `[Logging.Console]` - Enabled, PreventClose, ShiftJisEncoding, StandardOutType (Auto/ConsoleOut/StandardOut), LogLevels (8 levels from None to All)
- `[Logging.Disk]` - WriteUnityLog, AppendLog, Enabled, LogLevels
- `[Preloader]` - ApplyRuntimePatches, HarmonyBackend (auto/dynamicmethod/methodbuilder/cecil), DumpAssemblies, LoadDumpedAssemblies, BreakBeforeLoadAssemblies, LogConsoleToUnityLog
- `[Preloader.Entrypoint]` - Assembly, Type, Method

**AutoTranslatorConfig.ini** - CRITICAL section mappings with 50+ configuration options:
- `[Service]` - Translation endpoint (17 supported: Passthrough, GoogleTranslate, GoogleTranslateV2, GoogleTranslateCompat, GoogleTranslateLegitimate, BingTranslate, BingTranslateLegitimate, DeepLTranslate, DeepLTranslateLegitimate, PapagoTranslate, BaiduTranslate, YandexTranslate, WatsonTranslate, LecPowerTranslator15, ezTransXP, LingoCloudTranslate, CustomTranslate), FallbackEndpoint
- `[General]` - **Only contains Language and FromLanguage** (use zh-CN, zh-TW, en, ja, ko)
- `[Behaviour]` - **Most settings are here:** MaxCharactersPerTranslation, MinDialogueChars, IgnoreWhitespaceInDialogue, EnableUIResizing, OverrideFont, CopyToClipboard, EnableTranslationScoping, HandleRichText, MaxTextParserRecursion, HtmlEntityPreprocessing, EnableBatching, UseStaticTranslations, IgnoreTextStartingWith, OutputUntranslatableText, Delay
- `[TextFrameworks]` - Keys use `Enable` prefix: `EnableUGUI`, `EnableNGUI`, `EnableTextMeshPro`, `EnableTextMesh`, `EnableIMGUI`
- `[Files]` - Directory, OutputFile, SubstitutionFile, PreprocessorsFile, PostprocessorsFile
- `[Texture]` - Directory, EnableTranslation, EnableDumping, HashGenerationStrategy (FromImageName/FromImageData)
- `[Http]` - UserAgent, DisableCertificateChecks
- `[Debug]` - EnableConsole, EnableLog
- `[Optimization]` - EnableCache, MaxCacheEntries (100-50000)
- `[Integration]` - TextGetterCompatibilityMode
- `[ResourceRedirector]` - EnableRedirector, DetectDuplicateResources
- **Authentication sections** - Each service has its own section with API keys/tokens: Google (APIKey), Bing (SubscriptionKey), DeepL (APIKey), Baidu (AppId + AppSecret), Yandex (APIKey), Watson (APIKey), LingoCloud (Token)

**IMPORTANT**: Use correct section names when reading/writing XUnity config:
- ❌ WRONG: `IniParser.GetInt(data, "General", "MaxCharactersPerTranslation", 200)`
- ✅ CORRECT: `IniParser.GetInt(data, "Behaviour", "MaxCharactersPerTranslation", 200)`

### Version Fetching Strategy (Rate Limit Solution)
The application uses a **dual-client strategy** with global caching to eliminate rate limit issues:

**Primary: GitHub Atom Feed (GitHubAtomFeedClient)**
- Fetches releases from `https://github.com/{owner}/{repo}/releases.atom`
- **No rate limits, no authentication required**
- Parses XML using `System.Xml.Linq` to extract version info
- Constructs download URLs from version tags:
  - BepInEx: `https://github.com/BepInEx/BepInEx/releases/download/v{version}/BepInEx_win_{arch}_{version}.zip`
  - XUnity: `https://github.com/bbepis/XUnity.AutoTranslator/releases/download/v{version}/XUnity.AutoTranslator-BepInEx-{version}.zip`
- Provides `ValidateDownloadUrlAsync()` and `GetFileSizeAsync()` for URL verification

**Fallback: GitHub REST API (GitHubApiClient)**
- Used when: (1) User configures GitHub Token, or (2) Atom feed fails
- Unauthenticated: 60 requests/hour, Authenticated: 5000 requests/hour (set in SettingsPage)
- Rate limit errors throw custom exception with Chinese error message
- Uses Octokit with `ApiOptions` to limit results (maxCount parameter defaults to 3)

**Smart Mode Switching (VersionService)**
- Constructor checks for GitHub Token in settings (`SettingsService.LoadSettings()`)
- If Token present: uses API mode (more detailed metadata)
- If no Token: uses Atom feed mode (unlimited, faster)
- All methods have automatic fallback: Atom → API on failure
- Logs mode selection: `[VersionService]` prefix shows active mode

**Global Caching Layer (VersionCacheService)**
- **Singleton** initialized once in `App.OnLaunched()` via background `InitializeVersionCacheAsync()`
- 30-second timeout with graceful failure handling to prevent app startup blocking
- Pages never fetch directly - always read from cache via `GetBepInExVersions()`, `GetXUnityVersions()`
- Subscribe to `VersionsUpdated` event in page constructors for reactive updates
- Manual refresh **only** in `VersionManagementPage` via "刷新" button calling `RefreshAsync()`
- Pages wait for initialization (max 10s) without triggering refresh if cache not ready
- Typical usage: 1 fetch at startup, then cache-only until manual refresh

### IL2CPP Version Detection
- `BepInExBuildsApiClient` scrapes HTML from builds.bepinex.dev
- **Performance optimization**: Only fetches latest 1 build (optimized from 5, originally 50)
- Uses multiple regex patterns for compatibility
- All IL2CPP versions marked as `IsPrerelease = true`

### Translation Service Configuration
- ConfigPage supports all 17 official translation endpoints
- Uses Tag-based mapping in XAML for endpoint selection
- Handles unknown endpoint values gracefully by defaulting to GoogleTranslate
- Tag attributes ensure maintainability if endpoints are reordered

### Version Management Layout
- **Vertical layout**: Installed versions at top, available versions below
- **Platform filter excludes ARM64**: Plugin support discontinued
- **Font sizes increased**: Title 32px, headers 22px, list items 16px
- **Control sizes increased**: ListViews 250-400px, buttons 40px height

### Unified Logging System
- **LogService**: Singleton managing all application logs
- **LogPage**: Dedicated page showing all logs with filtering (Debug/Info/Warn/Error)
- **LogWriter**: Adapter pattern - internally uses LogService for unified output
- **Event-driven updates**: LogPage subscribes to `LogService.LogEntryAdded` event
- **No console output**: All Debug.WriteLine replaced with LogService

### Common Pitfalls and Solutions

**Page Initialization Timing**:
- **Problem**: Accessing XAML controls in constructor causes NullReferenceException
- **Solution**: Use `this.Loaded += OnPageLoaded` event pattern
- **Example**:
```csharp
public LogPage()
{
    this.InitializeComponent();
    _logService = LogService.Instance;

    // Subscribe to events but don't access XAML controls yet
    _logService.LogEntryAdded += OnLogEntryAdded;

    // Wait for XAML to load
    this.Loaded += OnPageLoaded;
}

private void OnPageLoaded(object sender, RoutedEventArgs e)
{
    // Safe to access XAML controls now
    RefreshLogDisplay();
    this.Loaded -= OnPageLoaded;
}
```
- Add null checks in all methods accessing XAML controls

**WinUI3 RadioButtons SelectedIndex**:
- **Problem**: Setting `IsChecked="True"` in XAML doesn't set `RadioButtons.SelectedIndex`, causing conditional logic to fail
- **Root Cause**: In WinUI3, individual RadioButton's `IsChecked` property is independent of the parent `RadioButtons` container's `SelectedIndex`
- **Solution**: Always explicitly set `SelectedIndex` in constructor after `InitializeComponent()`
- **Example**:
```csharp
// XAML has: <RadioButton Content="Option 1" IsChecked="True"/>
// But this is NOT enough!

public MyPage()
{
    this.InitializeComponent();

    // REQUIRED: Explicitly set SelectedIndex
    MyRadioButtons.SelectedIndex = 0;  // Now SelectedIndex == 0
}
```
- **Symptom**: Logs show `SelectedIndex=-1` even with `IsChecked="True"` in XAML
- **Impact**: Conditional checks like `if (MyRadioButtons.SelectedIndex == 0)` will fail

**Version Cache Refresh in Pages**:
- **Problem**: Page triggers network refresh on every navigation by calling `VersionCacheService.RefreshAsync()`
- **Root Cause**: Attempting to ensure data availability, but violates single-refresh architectural principle
- **Solution**: Pages should only read from cache and wait for initialization, never trigger refresh
- **Wrong Pattern**:
```csharp
// ❌ DO NOT DO THIS in any page except VersionManagementPage
if (!_versionCacheService.IsInitialized || versionCounts.BepInExCount == 0)
{
    await _versionCacheService.RefreshAsync();  // WRONG! Triggers network call
}
```
- **Correct Pattern**:
```csharp
// ✅ CORRECT: Wait for initialization, don't trigger refresh
if (!_versionCacheService.IsInitialized)
{
    var startTime = DateTime.Now;
    while (!_versionCacheService.IsInitialized && (DateTime.Now - startTime).TotalSeconds < 10)
    {
        await Task.Delay(500);
    }
}

// If still empty after waiting, show message
if (versionCounts.BepInExCount == 0)
{
    ComboBox.PlaceholderText = "缓存为空，请在版本管理页面刷新";
    return;
}
```

**GameStateService Initialization**:
- `GameStateService.Instance.Initialize()` must be called in `App.OnLaunched()` before window activation
- Use property assignment (not field) in `Initialize()` to trigger `GamePathChanged` event
- Subscribe to `GamePathChanged` event in page constructor for reactive updates

**VersionInfo.Platform vs TargetPlatform**:
- Use `VersionInfo.TargetPlatform` (nullable `Platform?`), not `Platform`
- Use null-coalescing for display: `version.TargetPlatform?.ToString() ?? "未知"`

**WinUI3 ContentDialog**:
- Must set `XamlRoot` property to page's `XamlRoot` before calling `ShowAsync()`

**Settings Persistence (Unpackaged App)**:
- `SettingsService` uses **JSON file** at `%AppData%\Roaming\XUnity-AutoInstaller\settings.json` (migrated from Registry)
- One-time automatic migration from legacy Registry storage (`HKCU\SOFTWARE\XUnity-AutoInstaller`) with flag file (`.migrated`)
- Settings: Theme, RememberLastGamePath, LastGamePath, ShowDetailedProgress, DefaultBackupExisting, GitHubToken
- Atomic file writes using temp file + rename pattern for safety
- Theme changes applied immediately via `SettingsService.ApplyTheme()`
- **DO NOT** use `ApplicationData.Current` anywhere - this will throw `System.InvalidOperationException` (HResult=0x80073D54) in unpackaged apps

**Configuration Accuracy**:
- ConfigPage options must match official documentation
- Use WebSearch or Context7 to verify options against:
  - BepInEx: https://github.com/BepInEx/BepInEx/wiki/Configuration
  - XUnity.AutoTranslator: https://github.com/bbepis/XUnity.AutoTranslator

### UI Design Language
- Windows 11 Fluent Design with Mica backdrop
- `FontIcon` glyphs for icons (Segoe Fluent Icons)
- `AccentButtonStyle` for primary actions
- `InfoBar` for status messages (preferred over ContentDialog)
- `ProgressRing` and `ProgressBar` for loading states

### Recent Architectural Changes
1. **Project Renaming** (Latest - Nov 2025):
   - Renamed entire project from `XUnity.AutoInstaller` to `XUnity-AutoInstaller` (dot to hyphen)
   - Updated namespace from `XUnity.AutoInstaller` to `XUnity_AutoInstaller` (C# doesn't allow hyphens)
   - Updated all configuration files: .csproj, .slnx, launchSettings.json, Package.appxmanifest
   - Changed AppData path from `%AppData%\XUnity.AutoInstaller` to `%AppData%\XUnity-AutoInstaller`
   - Updated Registry path from `SOFTWARE\XUnity.AutoInstaller` to `SOFTWARE\XUnity-AutoInstaller`
   - Modified Build-Release.ps1 to use new project paths and output filename
   - Updated all XAML x:Class attributes and documentation files
2. **Installation Progress & Settings Refactoring** (Nov 2025):
   - Introduced `InstallationStateService` singleton for global installation progress tracking across pages
   - Migrated settings storage from Windows Registry to **JSON file** at `%AppData%\Roaming\XUnity-AutoInstaller\settings.json`
   - Implemented automatic one-time migration from legacy Registry storage with `.migrated` flag file
   - Removed "UseRecommendedConfig" option from `InstallOptions` (simplified installation flow)
   - Enhanced `InstallationService` with thread-safe concurrent installation prevention
   - Updated `PathHelper` to use AppData directory for cache storage
   - Added cache management features to SettingsPage (clear version cache, clear all data)
   - Atomic file writes using temp file + rename pattern for settings persistence
2. **Unpackaged App Deployment**: Converted from MSIX packaged app to unpackaged deployment
   - Added `WindowsPackageType=None` and `WindowsAppSDKSelfContained=true` to .csproj
   - Changed version retrieval from Package.Current to Assembly.GetExecutingAssembly()
   - Removed manual Bootstrap initialization (auto-initialized by WindowsAppSDKSelfContained)
   - Created Build-Release.ps1 automation script for single-file exe generation
   - **Critical**: Manual Bootstrap.Initialize() or DeploymentManager.Initialize() calls cause fast-fail crashes (0xc0000602)
3. **Uninstallation Service Refactoring**: Removed automatic backup from uninstallation flow
   - Deleted `UninstallOptions.BackupBeforeUninstall` property
   - Removed `UninstallationService.BackupBeforeUninstall()` method and backup check logic
   - Updated DashboardPage confirmation dialog to remove backup messaging
   - Uninstallation now directly deletes files without creating backups
4. **WinUI3 RadioButtons Fix**: Fixed auto-recommended version not displaying in InstallPage
   - Root cause: `IsChecked="True"` in XAML doesn't set `RadioButtons.SelectedIndex`
   - Solution: Explicitly set `VersionModeRadio.SelectedIndex = 0` in constructor
   - Added comprehensive documentation in "Common Pitfalls" section
5. **Version Cache Refresh Prevention**: Fixed unwanted network calls on page navigation
   - Removed `VersionCacheService.RefreshAsync()` call from InstallPage
   - Pages now only wait for initialization (max 10s), never trigger refresh
   - Only VersionManagementPage can manually refresh
   - Enforces single-refresh architectural principle
6. **Atom Feed Integration**: Eliminated GitHub API rate limit issues
   - New `GitHubAtomFeedClient` fetches from `releases.atom` (no authentication, no limits)
   - Smart mode switching in `VersionService`: Atom feed by default, API when Token configured
   - Automatic fallback: Atom → API on failure
   - Download URL construction from version tags
   - **Result**: Users without Token can refresh versions unlimited times
7. **Auto-Launch Game for Config Generation**: New `GameLauncherService` automates first-run config generation
   - Launches game after installation, monitors for BepInEx.cfg and AutoTranslatorConfig.ini
   - Polls every 500ms with file size verification
   - Auto-closes game gracefully or force-kills after 3s
   - Configurable timeout (10-300s) with diagnostic logging on failure
   - Integrated into installation flow
8. **Comprehensive Configuration Editor**: Expanded from basic to complete config coverage
   - BepInEx: 11→30 properties across 8 sections (Caching, Chainloader, Harmony, Logging, Preloader)
   - XUnity: 34→50+ properties across 12 sections (added Http, Debug, Optimization, Integration, ResourceRedirector, Watson/LingoCloud auth)
   - All UI controls properly wired with correct property names and type mappings
9. **Version Caching System**: Introduced `VersionCacheService` singleton for global version list caching
   - Initialized once at app startup in background (non-blocking)
   - All pages read from cache, preventing redundant fetches
   - Manual refresh only in VersionManagementPage
   - Reduces fetch frequency by 90%+, improves page navigation speed 10x
10. **Unified Logging System**: All output routed through LogService to dedicated LogPage
11. **GitHub Token Support**: Optional token configuration switches to API mode (more metadata)
12. **Translation Services**: Expanded from 6 to 17 endpoints with Tag-based mapping
13. **IL2CPP Optimization**: Reduced fetch from 5 builds to 1 for faster loading
14. **Page Initialization**: Fixed NullReferenceException with Loaded event pattern
15. **Layout Redesign**: VersionManagementPage changed to vertical layout with larger fonts/controls
16. **ARM64 Removal**: Discontinued from UI (platform filter) due to plugin incompatibility