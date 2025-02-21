# XUnity.AutoInstaller

一个用于自动安装和配置Unity游戏翻译插件的图形化工具。

[English](README_EN.md) | 简体中文

## 功能特点

- 🎮 自动检测Unity游戏信息
  - Unity引擎版本
  - 程序架构（32位/64位）
  - 脚本后端（Mono/IL2CPP）

- 🛠 自动化安装流程
  - 自动安装适配版本的BepInEx框架
  - 自动安装XUnity.AutoTranslator插件
  - 自动配置必要的运行环境

- ⚙️ 图形化配置界面
  - 支持多种翻译服务配置（谷歌、必应、百度等）
  - 支持自定义字体设置
  - 支持多种文本框架的配置

- 🌈 智能主题切换
  - 自动适应Windows深色/浅色模式
  - 提供蓝白和暗黑两种主题风格

## 内置组件版本

- BepInEx Mono: v5.4.23.2
- BepInEx IL2CPP: v6.0.0-be.733
- XUnity.AutoTranslator: v5.4.4

## 系统要求

- Windows 7/8/10/11
- [Python 3.8+](https://www.python.org/downloads/)
- 管理员权限（用于安装系统组件）

## 安装说明

### 使用发布版本

1. 从[Releases](../../releases)页面下载最新版本
2. 直接运行可执行文件

### 从源码运行

1. 克隆仓库
```bash
git clone https://github.com/yourusername/XUnity.AutoInstaller.git
cd XUnity.AutoInstaller
```

2. 安装依赖
```bash
pip install -r requirements.txt
```

3. 运行程序
```bash
python main.py
```

## 使用教程

1. 启动程序
2. 点击"选择游戏目录"按钮选择需要安装翻译插件的游戏目录
3. 程序会自动检测游戏信息
4. 点击"安装翻译插件"开始安装
5. 安装完成后点击"初始化插件"进行初始配置
6. 使用"编辑插件配置"可以自定义翻译服务和其他设置

## 常见问题

1. **Q: 为什么某些游戏无法检测？**  
   A: 确保选择的是游戏主程序所在目录，不是快捷方式或启动器目录。

2. **Q: 安装后游戏无法启动？**  
   A: 检查是否以管理员权限运行游戏，某些游戏需要管理员权限才能正常加载插件。

3. **Q: 如何卸载插件？**  
   A: 在程序界面中点击"卸载插件"按钮，会自动清理所有相关文件。

## 开发相关

### 打包程序

使用以下命令打包成可执行文件：
```bash
python build.py
```

### 项目结构

```
XUnity.AutoInstaller/
├── main.py              # 程序入口
├── build.py            # 打包脚本
├── requirements.txt    # 项目依赖
├── core/              # 核心功能模块
├── gui/               # 图形界面模块
└── File/              # 资源文件
```

## 贡献指南

欢迎提交Issue和Pull Request！

## 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。

## 致谢

- [BepInEx](https://github.com/BepInEx/BepInEx) - Unity游戏模组框架
- [XUnity.AutoTranslator](https://github.com/bbepis/XUnity.AutoTranslator) - 游戏翻译插件
- [ttkbootstrap](https://github.com/israel-dryer/ttkbootstrap) - 现代化的tkinter主题 