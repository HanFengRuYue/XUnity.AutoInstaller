# MVVM 重构实施指南

## 📚 已创建的示范

已创建 `ViewModels/VersionManagementViewModel.cs` 作为完整的MVVM模式参考，展示了：

- ✅ `[ObservableProperty]` - 自动属性通知
- ✅ `[RelayCommand]` - 自动命令生成
- ✅ 异步命令支持
- ✅ CanExecute 条件执行
- ✅ 依赖注入模式
- ✅ 事件订阅和清理

## 🔧 完整实施步骤

### 第1步：配置依赖注入（App.xaml.cs）

```csharp
using Microsoft.Extensions.DependencyInjection;
using System;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        Services = ConfigureServices();
        InitializeComponent();
        this.UnhandledException += App_UnhandledException;
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // 注册单例服务（已存在的）
        services.AddSingleton<GameStateService>(GameStateService.Instance);
        services.AddSingleton<LogService>(LogService.Instance);
        services.AddSingleton<VersionCacheService>(VersionCacheService.Instance);
        services.AddSingleton<InstallationStateService>(InstallationStateService.Instance);
        services.AddSingleton<SettingsService>(SettingsService.Instance);

        // 注册其他服务
        services.AddTransient<VersionService>();
        services.AddTransient<InstallationService>();
        services.AddTransient<ConfigurationService>();
        services.AddTransient<FontManagementService>();

        // 注册 ViewModels
        services.AddTransient<VersionManagementViewModel>();
        services.AddTransient<ConfigPageViewModel>();

        return services.BuildServiceProvider();
    }
}
```

### 第2步：更新 Page 构造函数

**之前（代码后置）：**
```csharp
public VersionManagementPage()
{
    this.InitializeComponent();
    _versionCacheService = VersionCacheService.Instance;
    // 大量业务逻辑...
}
```

**之后（MVVM）：**
```csharp
public sealed partial class VersionManagementPage : Page
{
    public VersionManagementViewModel ViewModel { get; }

    public VersionManagementPage()
    {
        // 从DI容器获取ViewModel
        ViewModel = App.Services.GetRequiredService<VersionManagementViewModel>();

        this.InitializeComponent();

        this.Loaded += async (s, e) => await ViewModel.LoadVersionsAsync();
        this.Unloaded += (s, e) => ViewModel.Cleanup();
    }
}
```

### 第3步：更新 XAML 绑定

**XAML 头部添加：**
```xml
<Page
    x:Class="XUnity_AutoInstaller.Pages.VersionManagementPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:viewmodels="using:XUnity_AutoInstaller.ViewModels">

    <Page.DataContext>
        <viewmodels:VersionManagementViewModel x:Name="ViewModel"/>
    </Page.DataContext>
```

**控件绑定示例：**
```xml
<!-- 文本框双向绑定 -->
<TextBox
    Text="{x:Bind ViewModel.SearchText, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
    PlaceholderText="搜索版本..." />

<!-- 按钮命令绑定 -->
<Button
    Content="刷新"
    Command="{x:Bind ViewModel.RefreshCommand}" />

<!-- 带参数的命令绑定 -->
<Button
    Content="下载"
    Command="{x:Bind ViewModel.DownloadVersionCommand}"
    CommandParameter="{x:Bind SelectedVersion, Mode=OneWay}" />

<!-- 进度指示器 -->
<ProgressRing
    IsActive="{x:Bind ViewModel.IsLoading, Mode=OneWay}" />

<!-- 列表绑定 -->
<ListView
    ItemsSource="{x:Bind ViewModel.BepInExVersions, Mode=OneWay}"
    SelectedItem="{x:Bind ViewModel.SelectedVersion, Mode=TwoWay}" />
```

## 📋 重构检查清单

### VersionManagementPage
- [ ] 创建完整的 VersionManagementViewModel
- [ ] 将所有状态属性迁移到 ViewModel（约15个）
- [ ] 将所有方法转换为命令（约10个）
- [ ] 更新 XAML 绑定（约30处）
- [ ] 测试所有功能

### ConfigPage
- [ ] 创建 ConfigPageViewModel
- [ ] 处理约50个配置属性
- [ ] 迁移加载/保存逻辑
- [ ] 更新 XAML 绑定
- [ ] 测试配置读写

## 🎯 优先级建议

**高优先级**（推荐先做）：
1. VersionManagementPage - 逻辑清晰，状态管理明确
2. InstallPage - 相对简单，命令较少

**中优先级**：
3. DashboardPage - 需要处理多个状态卡片
4. FontDownloadPage - 包含复杂的筛选逻辑

**低优先级**（可选）：
5. ConfigPage - 最复杂，170+配置项
6. SettingsPage - 相对独立，当前实现已够用
7. LogPage - 主要是只读显示

## 💡 实用技巧

### 1. 渐进式迁移
不需要一次性完全重构，可以：
- 先迁移命令（RelayCommand）
- 再迁移属性（ObservableProperty）
- 最后更新 XAML 绑定

### 2. 保留后备代码
在完全迁移前，Page 可以同时保留：
- ViewModel（新代码）
- 代码后置（旧代码，作为参考）

### 3. 使用部分类
可以将 ViewModel 拆分为多个文件：
```
VersionManagementViewModel.cs        // 核心
VersionManagementViewModel.Commands.cs  // 命令
VersionManagementViewModel.Filters.cs   // 筛选逻辑
```

## 🔍 常见问题

**Q: 所有 Page 都必须使用 MVVM 吗？**
A: 不必须。简单的页面（如 LogPage）可以继续使用代码后置。

**Q: ViewModel 中可以直接操作 UI 吗？**
A: 不应该。ViewModel 应该只处理数据和业务逻辑，UI 操作通过绑定完成。

**Q: 如何在 ViewModel 中显示对话框？**
A: 使用消息传递或回调接口，避免直接引用 XamlRoot。

**Q: 现有的单例服务如何处理？**
A: 通过 `services.AddSingleton<T>(T.Instance)` 将已有实例注册到 DI 容器。

## 📊 预期收益

**代码质量**：
- ✅ 业务逻辑与 UI 分离
- ✅ 更容易编写单元测试
- ✅ 更好的代码组织

**开发效率**：
- ✅ 自动属性通知（减少样板代码）
- ✅ 自动命令生成
- ✅ 依赖注入提高可维护性

**文件大小**：
- ConfigPage.xaml.cs: 1082行 → 预计 400行（-63%）
- VersionManagementPage.xaml.cs: 877行 → 预计 300行（-66%）

## 🚀 下一步行动

1. **立即可做**：参考 `VersionManagementViewModel.cs` 了解模式
2. **近期计划**：为 VersionManagementPage 或 InstallPage 实施完整 MVVM
3. **长期目标**：逐步迁移所有大型页面

---

**注意**：当前项目已经完成了第一和第二阶段的优化，代码质量已经很好。MVVM 重构是可选的进一步提升，可以根据实际需要逐步实施。
