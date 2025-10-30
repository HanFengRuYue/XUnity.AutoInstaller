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
- **MainWindow.xaml**: Root window with NavigationView sidebar
  - Uses Frame-based navigation to switch between pages
  - NavigationView.SelectionChanged event handles page routing via Tag properties
  - Default window size: 1200x800px

### Page Organization
The application uses a 5-page structure in the `Pages/` folder:

1. **DashboardPage**: Game path selection, installation status cards (BepInEx/XUnity), quick actions
2. **InstallPage**: Version selection (auto/manual), platform choice, installation progress, logs
3. **ConfigPage**: Configuration editor for BepInEx and XUnity settings (see Configuration Structure below)
4. **VersionManagementPage**: Two-column layout for installed versions/snapshots (left) and available versions with filters (right)
5. **SettingsPage**: Application settings including theme, defaults, and about information (accessed via NavigationView settings icon)

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
- `GameEngine`, `Platform`, `PackageType` enums define supported game types and platforms
- `GameInfo`, `InstallationStatus`, `VersionInfo` represent game and version metadata
- `BepInExConfig`, `XUnityConfig` contain 40+ configuration properties mapping to INI files
- `InstallOptions`, `InstalledVersionInfo`, `SnapshotInfo` control installation behavior and version management
- `AppSettings` stores user preferences (theme, auto-detect, defaults)

**Services/** - Business logic layer:
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

1. **Async/Await Throughout**: All I/O operations are async with proper cancellation token support
2. **Progress Reporting**: Services accept `IProgress<T>` for real-time UI updates during long operations
3. **UI Thread Synchronization**: All page code-behind uses `DispatcherQueue.TryEnqueue()` to update UI from background threads
4. **No MVVM**: Direct code-behind approach with manual UI updates (no data binding framework)
5. **Static Service Methods**: FileSystemService uses static methods; others use instance methods for state management

### Code Patterns

**XAML Structure**:
- Use `<Button.Content>` child elements instead of `Content="..."` attribute to avoid duplication errors
- All child controls in Expanders should have `HorizontalAlignment="Stretch"`
- Card-style UI using `CardBackgroundFillColorDefaultBrush` and `CornerRadius="8"`

**C# Code-Behind**:
- Page constructors only call `InitializeComponent()` and set default UI states
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

Auto-detection scans Steam library paths (from registry) and common game directories (`C:/Program Files/`, `D:/Games/`, etc.)

### Configuration File Mapping
- **BepInEx.cfg**: INI format with sections `[Logging.Console]`, `[Preloader]`, `[Chainloader]`
- **AutoTranslatorConfig.ini**: INI format with 8 sections (Service, General, TextFrameworks, Files, Texture, Advanced, Authentication, HTTP)
- `IniParser.Parse()` returns `Dictionary<string, Dictionary<string, string>>` for section → key → value structure
- Type conversion helpers: `GetBool()`, `GetInt()`, `GetValue()` with defaults

### GitHub API Integration
- `GitHubApiClient` uses Octokit with product header "XUnity-AutoInstaller"
- Rate limit checking via `GetRateLimitAsync()` (default: 60 requests/hour for unauthenticated)
- Asset filtering: BepInEx uses filename patterns (x64, x86, il2cpp_x64, il2cpp_x86); XUnity excludes ReiPatcher variants
- Downloads use HttpClient with `ResponseHeadersRead` for streaming large files with progress reporting

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
- Lists current installation and all snapshots in left panel
- "创建快照" button creates snapshot with custom name
- "恢复快照" button restores selected snapshot (with confirmation dialog)
- "删除快照" button removes snapshot directory (with confirmation dialog)
- Snapshot selection uses list index calculation to determine which snapshot was selected

### Common Pitfalls and Solutions

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

### UI Design Language
The application follows Windows 11 Fluent Design principles with Mica backdrop material. Maintain consistency with:
- `FontIcon` glyphs for icons (Segoe Fluent Icons)
- `AccentButtonStyle` for primary actions
- `InfoBar` for status messages
- `ProgressRing` and `ProgressBar` for loading states
