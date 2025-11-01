# XUnity-AutoInstaller

<div align="center">

**XUnity 自动安装器**

一个用于自动安装和配置 BepInEx 与 XUnity.AutoTranslator 的 WinUI3 桌面应用程序

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![WinUI3](https://img.shields.io/badge/WinUI-3.0-0078D4?logo=windows)](https://microsoft.github.io/microsoft-ui-xaml/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%2B-blue.svg)](https://www.microsoft.com/windows)

</div>

---

## 📖 项目简介

XUnity-AutoInstaller 是一款专为 Unity 游戏设计的自动化安装工具，可以帮助用户快速便捷地安装和配置：

- **BepInEx** - Unity 游戏通用插件框架
- **XUnity.AutoTranslator** - 游戏自动翻译插件

该工具提供了图形化界面，自动处理版本选择、下载、安装、配置等繁琐步骤，让游戏本地化变得简单高效。

## ✨ 主要功能

### 🎮 游戏管理
- **自动游戏检测** - 智能识别 Unity 游戏目录和引擎类型（Mono/IL2CPP）
- **安装状态监控** - 实时显示 BepInEx 和 XUnity.AutoTranslator 安装状态
- **快速操作面板** - 一键安装、卸载、打开游戏目录

### 📦 版本管理
- **智能版本获取** - 采用双源策略（Atom Feed + GitHub API）避免速率限制
- **平台自动适配** - 支持 x86、x64、IL2CPP 等多种平台自动匹配
- **版本缓存系统** - 全局缓存机制，减少 90% 以上的网络请求
- **手动版本选择** - 支持安装特定历史版本

### ⚙️ 配置编辑器
- **BepInEx 配置** - 30+ 配置项，覆盖缓存、日志、预加载等 8 个配置段
- **XUnity 配置** - 50+ 配置项，支持 17 种翻译服务端点
- **可视化编辑** - 图形化界面，无需手动编辑 INI 文件
- **实时验证** - 配置项类型检查和范围验证

### 🚀 自动化安装
- **一键安装流程** - 自动下载、解压、配置，全程进度可视化
- **智能配置生成** - 可选自动启动游戏生成默认配置文件
- **备份保护** - 安装前可选备份现有 BepInEx 目录
- **旧版本清理** - 可选清理旧版本文件避免冲突

### 🔧 高级特性
- **GitHub Token 支持** - 配置 Token 可提升 API 请求限额（60→5000/小时）
- **统一日志系统** - 所有操作日志集中显示，支持级别过滤
- **并发安装保护** - 线程安全机制防止多个安装任务冲突
- **安装进度跨页面** - 全局进度追踪，任意页面查看安装状态

## 💻 系统要求

- **操作系统**: Windows 10 Build 17763 (2018 年 10 月更新) 或更高版本
- **架构**: x64 (推荐) / x86
- **.NET Runtime**: 无需安装（应用程序自包含）
- **磁盘空间**: 约 150 MB

## 📥 下载与安装

### 方式一：从 Release 下载（推荐）

1. 前往 [Releases](../../releases) 页面
2. 下载最新版本的 `XUnity-AutoInstaller.exe`
3. 双击运行即可，无需安装

### 方式二：从源码构建

```bash
# 克隆仓库
git clone https://github.com/yourusername/XUnity-AutoInstaller.git
cd XUnity-AutoInstaller

# 构建 Release 版本（生成单文件 exe）
powershell.exe -ExecutionPolicy Bypass -File Build-Release.ps1

# 输出文件位于 Release/XUnity-AutoInstaller.exe
```

## 🎯 使用指南

### 1️⃣ 选择游戏目录
在主页面点击"选择游戏文件夹"，选择 Unity 游戏的根目录（包含游戏 .exe 文件的文件夹）

### 2️⃣ 安装插件
1. 进入"安装"页面
2. 选择版本模式：
   - **自动推荐**（默认）：自动选择最新稳定版本和适配平台
   - **手动选择**：自行选择 BepInEx 和 XUnity 版本及平台
3. 配置安装选项：
   - ✅ **自动启动游戏生成配置**（推荐）- 首次安装自动生成配置文件
   - ⚙️ **配置生成超时** - 等待配置文件生成的时间（默认 60 秒）
   - 📦 **备份现有文件** - 覆盖前备份旧版本
   - 🧹 **清理旧版本** - 删除旧文件避免冲突
4. 点击"开始安装"

### 3️⃣ 配置翻译设置
1. 安装完成后进入"配置编辑"页面
2. 选择翻译服务（如 GoogleTranslate、BaiduTranslate 等）
3. 配置源语言和目标语言（如 ja→zh-CN）
4. 根据需要调整其他高级选项
5. 保存配置

### 4️⃣ 启动游戏
直接启动游戏，BepInEx 会自动加载，XUnity.AutoTranslator 将开始翻译游戏文本

## 🛠️ 技术栈

### 核心框架
- **.NET 9.0** - 最新的 .NET 框架
- **WinUI3** - Windows App SDK 1.8.251003001
- **C# 13** - 启用可空引用类型

### 关键库
- **Octokit.NET 14.0.0** - GitHub API 集成
- **SharpCompress 0.41.0** - ZIP 压缩解压
- **System.Xml.Linq** - Atom Feed XML 解析

### 架构特点
- **单例服务模式** - 全局状态管理（GameStateService、LogService、InstallationStateService、VersionCacheService）
- **事件驱动通信** - 跨页面响应式更新
- **异步优先** - 所有 I/O 操作使用 async/await
- **无 MVVM** - 直接代码后置模式，简化架构

## 🔨 开发指南

### 环境配置

#### 必需工具
- **Visual Studio 2022** (17.8+) 或 **Visual Studio Code**
- **.NET 9.0 SDK**
- **Windows 10 SDK (10.0.26100.0)**

#### Visual Studio 调试配置
1. 使用 **"XUnity-AutoInstaller (Unpackaged)"** 启动配置文件
2. 不要使用 Package 模式（项目使用 Unpackaged 部署）
3. 如遇调试问题：
   - 关闭 Visual Studio
   - 删除 `.vs`、`bin`、`obj` 文件夹
   - 重新打开解决方案并重建

### 构建命令

```bash
# 标准构建（x64）
dotnet build -p:Platform=x64

# x86 构建
dotnet build -p:Platform=x86

# 运行调试版本
dotnet run --project XUnity-AutoInstaller/XUnity-AutoInstaller.csproj

# 运行 Release 版本
dotnet run --project XUnity-AutoInstaller/XUnity-AutoInstaller.csproj -c Release

# 发布单文件 exe
dotnet publish -p:Platform=x64 -c Release

# 或使用自动化脚本
powershell.exe -ExecutionPolicy Bypass -File Build-Release.ps1
```

### 项目结构

```
XUnity-AutoInstaller/
├── Pages/                  # 6 个功能页面
│   ├── DashboardPage.xaml         # 主页 - 游戏路径选择和状态卡片
│   ├── InstallPage.xaml           # 安装页 - 版本选择和安装进度
│   ├── ConfigPage.xaml            # 配置页 - BepInEx 和 XUnity 配置编辑
│   ├── VersionManagementPage.xaml # 版本管理 - 已安装和可用版本
│   ├── LogPage.xaml               # 日志页 - 统一日志显示和过滤
│   └── SettingsPage.xaml          # 设置页 - 主题、Token、路径记忆
├── Services/               # 业务逻辑层
│   ├── GameStateService.cs        # 全局游戏路径管理（单例）
│   ├── LogService.cs              # 统一日志服务（单例）
│   ├── InstallationStateService.cs # 全局安装进度追踪（单例）
│   ├── VersionCacheService.cs     # 全局版本缓存（单例）
│   ├── GitHubAtomFeedClient.cs    # Atom Feed 版本获取（无速率限制）
│   ├── GitHubApiClient.cs         # GitHub API 版本获取（支持 Token）
│   ├── VersionService.cs          # 智能版本服务（双源切换）
│   ├── InstallationService.cs     # 安装编排服务
│   ├── GameLauncherService.cs     # 游戏启动和配置生成监控
│   ├── ConfigurationService.cs    # INI 配置文件读写
│   └── SettingsService.cs         # 设置持久化（JSON 文件）
├── Models/                 # 数据模型
│   ├── BepInExConfig.cs           # 30 个 BepInEx 配置属性
│   ├── XUnityConfig.cs            # 50+ XUnity 配置属性
│   ├── InstallOptions.cs          # 安装选项模型
│   └── AppSettings.cs             # 应用设置模型
├── Utils/                  # 工具类
│   ├── IniParser.cs               # INI 文件解析器
│   ├── PathHelper.cs              # 路径处理工具
│   └── LogWriter.cs               # LogService 适配器
├── App.xaml                # 应用入口
├── MainWindow.xaml         # 主窗口（导航框架）
└── Package.appxmanifest    # 应用清单
```

### 关键设计模式

#### 单例服务
```csharp
// 所有页面共享同一个游戏路径
var gamePath = GameStateService.Instance.CurrentGamePath;

// 订阅路径变化事件
GameStateService.Instance.GamePathChanged += OnGamePathChanged;
```

#### 版本缓存
```csharp
// 应用启动时初始化一次（App.OnLaunched）
await VersionCacheService.Instance.RefreshAsync();

// 页面只读取缓存，不触发刷新
var versions = _versionCacheService.GetBepInExVersions();
_versionCacheService.VersionsUpdated += OnVersionsUpdated;
```

#### 安装进度追踪
```csharp
// 跨页面进度显示
InstallationStateService.Instance.InstallationStarted += OnInstallationStarted;
InstallationStateService.Instance.ProgressChanged += OnProgressChanged;
InstallationStateService.Instance.InstallationCompleted += OnInstallationCompleted;

// 服务内部创建进度报告器
var progress = InstallationStateService.Instance.CreateProgressReporter();
```

#### 统一日志
```csharp
// 所有日志通过 LogService
LogService.Instance.Log("配置已保存", LogLevel.Info, "[Config]");

// LogPage 自动接收和显示
_logService.LogEntryAdded += OnLogEntryAdded;
```

## 📝 配置文件说明

### BepInEx.cfg
包含 30 个配置项，分为 8 个配置段：
- `[Caching]` - 程序集缓存
- `[Chainloader]` - 插件加载器
- `[Harmony.Logger]` - Harmony 日志
- `[Logging]` / `[Logging.Console]` / `[Logging.Disk]` - 日志系统
- `[Preloader]` / `[Preloader.Entrypoint]` - 预加载器

### AutoTranslatorConfig.ini
包含 50+ 配置项，支持 17 种翻译服务：
- Google Translate (多种模式)
- Bing Translate
- DeepL Translate
- Baidu Translate
- Yandex Translate
- Papago Translate
- Watson Translate
- LecPowerTranslator15
- ezTransXP
- LingoCloud Translate
- Custom Translate

**重要提示**: XUnity 配置项的正确配置段映射：
- `Language`, `FromLanguage` → `[General]`
- `MaxCharactersPerTranslation`, `EnableUIResizing`, `Delay` 等 → `[Behaviour]`
- `EnableUGUI`, `EnableTextMeshPro` 等 → `[TextFrameworks]`

## 🤝 贡献指南

欢迎贡献代码！请遵循以下步骤：

1. Fork 本仓库
2. 创建功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 开启 Pull Request

### 代码规范
- 遵循 C# 编码规范
- 使用 async/await 处理异步操作
- 通过 LogService 记录日志，不使用 Debug.WriteLine
- 添加必要的注释和文档
- 页面初始化使用 `Loaded` 事件模式访问 XAML 控件

## 📄 许可证

本项目采用 [MIT 许可证](LICENSE)。

## 🙏 致谢

- [BepInEx](https://github.com/BepInEx/BepInEx) - Unity 游戏插件框架
- [XUnity.AutoTranslator](https://github.com/bbepis/XUnity.AutoTranslator) - 游戏自动翻译插件
- [WinUI3](https://github.com/microsoft/microsoft-ui-xaml) - 现代 Windows 应用开发框架

## 📮 联系方式

如有问题或建议，请通过以下方式联系：

- 提交 [Issue](../../issues)
- 发起 [Discussion](../../discussions)

---

<div align="center">
Made with ❤️ for Unity game localization
</div>
