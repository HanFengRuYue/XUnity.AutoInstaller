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

# ARM64 build (Note: Plugin support discontinued, but build system still supports it)
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
- **MSIX packaging** enabled for deployment
- **Nullable reference types** enabled

### Backend Architecture

The backend follows a service-oriented architecture organized into three main layers:

**Models/** - Data structures and enums:
- `GameEngine`, `Platform`, `PackageType` enums (Platform includes x86, x64, IL2CPP variants, ARM64)
- `VersionInfo.TargetPlatform` is nullable `Platform?`
- `BepInExConfig`, `XUnityConfig` contain 40+ configuration properties
- `AppSettings` stores user preferences including GitHub Token

**Services/** - Business logic layer:
- `LogService`: **Singleton** for unified application logging with event-driven updates to LogPage. Replaces all `Debug.WriteLine()` calls
- `GameStateService`: **Singleton** for global game path management. Use `Instance.CurrentGamePath` and subscribe to `GamePathChanged` event
- `GitHubApiClient`: Supports optional GitHub Token authentication (60→5000 requests/hour) and rate limit error handling
- `VersionService`: Lazy-loads `GitHubApiClient` with token from `SettingsService`
- `BepInExBuildsApiClient`: Fetches IL2CPP versions (optimized to latest 1 build only)
- `ConfigurationService`: Parses INI files with correct section mappings for XUnity config
- `InstallationService`: Orchestrates installation pipeline using `LogWriter` which internally uses `LogService`
- `SettingsService`: Manages settings persistence including GitHub Token

**Utils/** - Shared utilities:
- `IniParser`: INI file parser with type conversion helpers
- `PathHelper`: Centralized path logic for BepInEx directories
- `LogWriter`: Adapter that uses `LogService` internally for unified logging

**Key Architectural Patterns:**

1. **Singleton Pattern**: `GameStateService` and `LogService` use thread-safe singletons
2. **Event-Driven Communication**: `GameStateService.GamePathChanged` and `LogService.LogEntryAdded` events for cross-component updates
3. **Async/Await Throughout**: All I/O operations with proper cancellation token support
4. **Progress Reporting**: Services accept `IProgress<T>` for real-time UI updates
5. **UI Thread Synchronization**: Use `DispatcherQueue.TryEnqueue()` for UI updates
6. **No MVVM**: Direct code-behind approach with manual UI updates
7. **Lazy Initialization**: VersionService lazy-loads GitHubApiClient with settings

### Code Patterns

**XAML Structure**:
- Use `<Button.Content>` child elements instead of `Content="..."` attribute
- All child controls in Expanders should have `HorizontalAlignment="Stretch"`
- Card-style UI using `CardBackgroundFillColorDefaultBrush` and `CornerRadius="8"`

**C# Code-Behind**:
- Page constructors: `InitializeComponent()` → subscribe to events → use `Loaded` event for XAML control access
- **CRITICAL**: Never access XAML controls in constructor before `Loaded` event fires (causes NullReferenceException)
- Use `this.Loaded += OnPageLoaded` pattern to access controls safely after XAML initialization
- Add null checks in all methods that access XAML controls for defensive programming
- **Always use `GameStateService.Instance.CurrentGamePath`** for game path access
- Subscribe to `GameStateService.GamePathChanged` event for path change notifications
- WinUI3 FolderPicker requires HWND: `WinRT.Interop.WindowNative.GetWindowHandle()` and `WinRT.Interop.InitializeWithWindow.Initialize()`
- Access MainWindow via `App.MainWindow` static property

**Logging Pattern**:
- Use `LogService.Instance.Log(message, LogLevel, prefix)` for all logging
- Log levels: `Debug`, `Info`, `Warning`, `Error`
- Use consistent prefixes: `[Config]`, `[IL2CPP]`, `[GitHub]`, `[VersionService]`, `[安装]`
- Never use `System.Diagnostics.Debug.WriteLine()` directly

**Installation Flow**:
1. Validate game path and detect engine type
2. Optionally backup existing BepInEx directory
3. Optionally clean old installation
4. Download BepInEx for target platform from GitHub
5. Extract BepInEx to game root
6. Download XUnity.AutoTranslator from GitHub
7. Extract XUnity to `BepInEx/plugins/`
8. Apply recommended configuration (optional)

## Important Notes

### Configuration File Mapping

**BepInEx.cfg** - INI format with sections:
- `[Logging.Console]`, `[Logging.Disk]` - Logging configuration
- `[Preloader]`, `[Preloader.Entrypoint]` - Preloader settings
- `[Chainloader]` - Plugin loading settings

**AutoTranslatorConfig.ini** - CRITICAL section mappings:
- `[Service]` - Translation endpoint (17 supported: Passthrough, GoogleTranslate, GoogleTranslateV2, GoogleTranslateCompat, GoogleTranslateLegitimate, BingTranslate, BingTranslateLegitimate, DeepLTranslate, DeepLTranslateLegitimate, PapagoTranslate, BaiduTranslate, YandexTranslate, WatsonTranslate, LecPowerTranslator15, ezTransXP, LingoCloudTranslate, CustomTranslate)
- `[General]` - **Only contains Language and FromLanguage**
- `[Behaviour]` - **Most settings are here:** MaxCharactersPerTranslation, EnableUIResizing, OverrideFont, EnableTranslationScoping, HandleRichText, etc.
- `[TextFrameworks]` - Keys use `Enable` prefix: `EnableUGUI`, `EnableNGUI`, etc.
- `[Texture]` - Keys use `Texture` prefix or `Enable` prefix: `TextureDirectory`, `EnableTextureTranslation`
- **Authentication sections** - Each service has its own section: `[GoogleLegitimate]`, `[BingLegitimate]`, `[DeepLLegitimate]`, `[Baidu]`, `[Yandex]`

**IMPORTANT**: Use correct section names when reading/writing XUnity config:
- ❌ WRONG: `IniParser.GetInt(data, "General", "MaxCharactersPerTranslation", 200)`
- ✅ CORRECT: `IniParser.GetInt(data, "Behaviour", "MaxCharactersPerTranslation", 200)`

### GitHub API Integration
- `GitHubApiClient` supports optional token authentication via constructor parameter
- Unauthenticated: 60 requests/hour, Authenticated: 5000 requests/hour
- Rate limit errors throw custom exception with Chinese error message and guidance
- `VersionService` lazy-loads `GitHubApiClient` with token from `SettingsService.LoadSettings()`
- All GitHub API calls wrapped with `RateLimitExceededException` handling

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

**GameStateService Initialization**:
- `GameStateService.Instance.Initialize()` must be called in `App.OnLaunched()` before window activation
- Use property assignment (not field) in `Initialize()` to trigger `GamePathChanged` event
- Subscribe to `GamePathChanged` event in page constructor for reactive updates

**VersionInfo.Platform vs TargetPlatform**:
- Use `VersionInfo.TargetPlatform` (nullable `Platform?`), not `Platform`
- Use null-coalescing for display: `version.TargetPlatform?.ToString() ?? "未知"`

**WinUI3 ContentDialog**:
- Must set `XamlRoot` property to page's `XamlRoot` before calling `ShowAsync()`

**Settings Persistence**:
- `SettingsService` uses `ApplicationData.Current.LocalSettings`
- GitHub Token stored securely in local settings
- Theme changes applied immediately via `SettingsService.ApplyTheme()`

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
1. **Unified Logging System**: All output routed through LogService to dedicated LogPage
2. **GitHub Token Support**: Optional token configuration increases API rate limit 60→5000/hour
3. **Translation Services**: Expanded from 6 to 17 endpoints with Tag-based mapping
4. **IL2CPP Optimization**: Reduced fetch from 5 builds to 1 for faster loading
5. **Page Initialization**: Fixed NullReferenceException with Loaded event pattern
6. **Layout Redesign**: VersionManagementPage changed to vertical layout with larger fonts/controls
7. **ARM64 Removal**: Discontinued from UI (platform filter) due to plugin incompatibility
