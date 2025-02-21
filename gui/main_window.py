import tkinter as tk
from tkinter import filedialog, messagebox
import ttkbootstrap as ttk
from ttkbootstrap.constants import *
from core.game_detector import GameDetector
from core.plugin_installer import PluginInstaller
from gui.config_window import ConfigWindow
import os

class MainWindow:
    def __init__(self):
        # 添加版本号变量
        self.BEPINEX_MONO_VERSION = "5.4.23.2"
        self.BEPINEX_IL2CPP_VERSION = "6.0.0-be.733"
        self.XUNITY_VERSION = "5.4.4"
        
        # 检测系统主题
        self.theme = self.detect_system_theme()
        
        self.root = ttk.Window(
            title="XUnity自动安装工具ver.20250221",
            themename=self.theme,
            size=(600, 850),  # 增加窗口高度以适应新区域
            resizable=(False, False)
        )
        
        # 保存style引用
        self.style = self.root.style
        
        self.game_path = None
        self.game_info = None
        self.detector = None
        self.installer = None
        
        # 保存子窗口引用
        self.child_windows = []
        
        self.setup_ui()
        
    def detect_system_theme(self):
        """检测系统主题颜色模式"""
        try:
            import winreg
            registry = winreg.ConnectRegistry(None, winreg.HKEY_CURRENT_USER)
            key = winreg.OpenKey(registry, r"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize")
            value, _ = winreg.QueryValueEx(key, "AppsUseLightTheme")
            winreg.CloseKey(key)
            return "cosmo" if value == 1 else "darkly"
        except Exception:
            # 如果无法检测,默认使用亮色主题
            return "cosmo"
        
    def setup_ui(self):
        """设置UI界面"""
        # 创建主框架
        main_frame = ttk.Frame(self.root, padding=20)
        main_frame.pack(fill=BOTH, expand=YES)
        
        # 游戏路径选择
        path_frame = ttk.LabelFrame(main_frame, text="游戏路径", padding=10)
        path_frame.pack(fill=X, pady=10)
        
        self.path_var = tk.StringVar()
        path_entry = ttk.Entry(path_frame, textvariable=self.path_var, state="readonly")
        path_entry.pack(side=LEFT, fill=X, expand=YES, padx=(0, 10))
        
        select_btn = ttk.Button(
            path_frame,
            text="选择游戏目录",
            command=self.select_game_path,
            style="primary.TButton"
        )
        select_btn.pack(side=RIGHT)
        
        # 插件信息显示
        plugin_frame = ttk.LabelFrame(main_frame, text="安装程序内置插件信息", padding=10)
        plugin_frame.pack(fill=X, pady=10)
        
        # BepInEx Mono版本信息
        bepinex_mono_frame = ttk.Frame(plugin_frame)
        bepinex_mono_frame.pack(fill=X, pady=2)
        ttk.Label(bepinex_mono_frame, text="BepInEx Mono版本：").pack(side=LEFT)
        ttk.Label(bepinex_mono_frame, text=self.BEPINEX_MONO_VERSION).pack(side=LEFT)
        
        # BepInEx IL2CPP版本信息
        bepinex_il2cpp_frame = ttk.Frame(plugin_frame)
        bepinex_il2cpp_frame.pack(fill=X, pady=2)
        ttk.Label(bepinex_il2cpp_frame, text="BepInEx IL2CPP版本：").pack(side=LEFT)
        ttk.Label(bepinex_il2cpp_frame, text=self.BEPINEX_IL2CPP_VERSION).pack(side=LEFT)
        
        # XUnity插件版本信息
        xunity_frame = ttk.Frame(plugin_frame)
        xunity_frame.pack(fill=X, pady=2)
        ttk.Label(xunity_frame, text="XUnity插件版本：").pack(side=LEFT)
        ttk.Label(xunity_frame, text=self.XUNITY_VERSION).pack(side=LEFT)
        
        # 游戏信息显示
        info_frame = ttk.LabelFrame(main_frame, text="游戏信息", padding=10)
        info_frame.pack(fill=X, pady=10)
        
        # Unity版本信息
        version_frame = ttk.Frame(info_frame)
        version_frame.pack(fill=X, pady=2)
        ttk.Label(version_frame, text="Unity引擎版本：").pack(side=LEFT)
        self.version_label = ttk.Label(version_frame, text="未检测")
        self.version_label.pack(side=LEFT)
        
        # 程序架构信息
        arch_frame = ttk.Frame(info_frame)
        arch_frame.pack(fill=X, pady=2)
        ttk.Label(arch_frame, text="程序架构：").pack(side=LEFT)
        self.arch_label = ttk.Label(arch_frame, text="未检测")
        self.arch_label.pack(side=LEFT)
        
        # 脚本后端信息
        backend_frame = ttk.Frame(info_frame)
        backend_frame.pack(fill=X, pady=2)
        ttk.Label(backend_frame, text="脚本后端：").pack(side=LEFT)
        self.backend_label = ttk.Label(backend_frame, text="未检测")
        self.backend_label.pack(side=LEFT)
        
        # 操作按钮
        btn_frame = ttk.LabelFrame(main_frame, text="操作", padding=10)
        btn_frame.pack(fill=X, pady=10)
        
        # 启动游戏按钮
        self.launch_btn = ttk.Button(
            btn_frame,
            text="启动游戏",
            command=self.launch_game,
            state=DISABLED,
            style="success.TButton"  # 使用成功操作样式
        )
        self.launch_btn.pack(fill=X, pady=5)
        
        # 整合安装按钮
        self.install_btn = ttk.Button(
            btn_frame,
            text="安装翻译插件",
            command=self.install_plugins,
            state=DISABLED,
            style="primary.TButton"
        )
        self.install_btn.pack(fill=X, pady=5)
        
        self.init_btn = ttk.Button(
            btn_frame,
            text="初始化插件",
            command=self.initialize_plugin,
            state=DISABLED,
            style="primary.TButton"
        )
        self.init_btn.pack(fill=X, pady=5)
        
        self.config_btn = ttk.Button(
            btn_frame,
            text="编辑插件配置",
            command=self.edit_config,
            state=DISABLED,
            style="primary.TButton"
        )
        self.config_btn.pack(fill=X, pady=5)
        
        # 添加卸载插件按钮
        self.uninstall_btn = ttk.Button(
            btn_frame,
            text="卸载插件",
            command=self.uninstall_plugin,
            state=DISABLED,
            style="danger.TButton"  # 使用危险操作样式
        )
        self.uninstall_btn.pack(fill=X, pady=5)
        
        # 附加操作区域
        extra_frame = ttk.LabelFrame(main_frame, text="附加操作", padding=10)
        extra_frame.pack(fill=X, pady=10)
        
        # 清除已翻译文本按钮
        self.clear_trans_btn = ttk.Button(
            extra_frame,
            text="清除已翻译文本",
            command=self.clear_translations,
            state=DISABLED,
            style="primary.TButton"  # 改为普通按钮样式
        )
        self.clear_trans_btn.pack(fill=X, pady=5)
        
        # 编辑已翻译文本按钮
        self.edit_trans_btn = ttk.Button(
            extra_frame,
            text="编辑已翻译文本",
            command=self.edit_translations,
            state=DISABLED,
            style="primary.TButton"
        )
        self.edit_trans_btn.pack(fill=X, pady=5)
        
        # 导出插件日志按钮
        self.export_log_btn = ttk.Button(
            extra_frame,
            text="导出插件日志",
            command=self.export_log,
            state=DISABLED,
            style="primary.TButton"
        )
        self.export_log_btn.pack(fill=X, pady=5)
        
    def select_game_path(self):
        """选择游戏路径"""
        path = filedialog.askdirectory(title="选择游戏目录")
        if not path:
            return
            
        self.game_path = path
        self.path_var.set(path)
        
        # 分析游戏信息
        self.detector = GameDetector(path)
        success, result = self.detector.analyze_game()
        
        if not success:
            messagebox.showerror("错误", result)
            self.reset_ui()
            return
            
        self.game_info = result
        self.installer = PluginInstaller(path)
        
        # 更新界面显示
        self.version_label.config(text=result['version'])
        self.arch_label.config(text=result['architecture'])
        self.backend_label.config(text=result['backend'])
        
        # 启用启动游戏按钮
        self.launch_btn.config(state=NORMAL)
        
        # 检查已安装的组件
        self.check_installed_components()
        
    def check_installed_components(self):
        """检查已安装的组件"""
        # 检查BepInEx
        bepinex_path = os.path.join(self.game_path, 'BepInEx')
        has_bepinex = os.path.exists(bepinex_path)
        
        # 检查XUnity插件
        has_xunity = False
        if has_bepinex:
            for root, _, files in os.walk(self.game_path):
                if 'XUnity.AutoTranslator.Plugin.Core.dll' in files:
                    has_xunity = True
                    break
        
        # 检查配置文件
        config_path = os.path.join(self.game_path, 'BepInEx', 'config', 'AutoTranslatorConfig.ini')
        has_config = os.path.exists(config_path)
        
        # 更新按钮状态
        if has_bepinex and has_xunity:
            self.install_btn.config(state=DISABLED)
            self.install_btn.config(text="翻译插件 [已安装]")
            self.init_btn.config(state=NORMAL)
        else:
            self.install_btn.config(state=NORMAL)
            self.install_btn.config(text="安装翻译插件")
            self.init_btn.config(state=DISABLED)
            
        if has_config:
            self.init_btn.config(state=DISABLED)
            self.init_btn.config(text="初始化插件 [已完成]")
            self.config_btn.config(state=NORMAL)
            # 启用附加操作按钮（仅当插件完全配置后）
            if has_bepinex and has_xunity:
                self.clear_trans_btn.config(state=NORMAL)
                self.edit_trans_btn.config(state=NORMAL)
                self.export_log_btn.config(state=NORMAL)
        else:
            self.config_btn.config(state=DISABLED)
            # 禁用附加操作按钮
            self.clear_trans_btn.config(state=DISABLED)
            self.edit_trans_btn.config(state=DISABLED)
            self.export_log_btn.config(state=DISABLED)
            
        # 更新卸载按钮状态
        if has_bepinex or has_xunity or has_config:
            self.uninstall_btn.config(state=NORMAL)
        else:
            self.uninstall_btn.config(state=DISABLED)
        
    def reset_ui(self):
        """重置UI状态"""
        self.path_var.set("")
        self.version_label.config(text="未检测")
        self.arch_label.config(text="未检测")
        self.backend_label.config(text="未检测")
        self.launch_btn.config(state=DISABLED)
        self.install_btn.config(state=DISABLED)
        self.install_btn.config(text="安装翻译插件")
        self.init_btn.config(state=DISABLED)
        self.init_btn.config(text="初始化插件")
        self.config_btn.config(state=DISABLED)
        self.uninstall_btn.config(state=DISABLED)
        self.clear_trans_btn.config(state=DISABLED)
        self.edit_trans_btn.config(state=DISABLED)
        self.export_log_btn.config(state=DISABLED)  # 添加导出日志按钮状态重置
        
    def install_plugins(self):
        """安装插件框架和翻译插件"""
        # 安装BepInEx
        is_64bit = self.game_info['architecture'] == '64位'
        success, message = self.installer.install_bepinex(is_64bit, self.game_info['backend'])
        
        if not success:
            messagebox.showerror("错误", message)
            return
            
        # 安装XUnity
        success, message = self.installer.install_xunity(self.game_info['backend'])
        
        if success:
            messagebox.showinfo("成功", "翻译插件安装成功")
            self.init_btn.config(state=NORMAL)
            self.check_installed_components()
        else:
            messagebox.showerror("错误", message)
            
    def initialize_plugin(self):
        """初始化插件"""
        success, message = self.installer.initialize_plugin(self.detector.main_executable)
        
        if success:
            messagebox.showinfo("成功", "插件初始化成功")
            self.config_btn.config(state=NORMAL)
            self.check_installed_components()  # 检查安装状态
        else:
            messagebox.showerror("错误", message)
            
    def edit_config(self):
        """编辑插件配置"""
        config_window = ConfigWindow(self.root, self.game_path)
        # 将子窗口添加到列表中
        self.child_windows.append(config_window)
        # 绑定窗口关闭事件
        config_window.top.protocol("WM_DELETE_WINDOW", lambda: self.on_child_window_close(config_window))
        
    def on_child_window_close(self, window):
        """处理子窗口关闭事件"""
        if window in self.child_windows:
            self.child_windows.remove(window)
        window.top.destroy()
        
    def update_theme(self, new_theme):
        """更新主题"""
        self.theme = new_theme
        self.style.theme_use(new_theme)
        # 更新所有子窗口的主题
        for window in self.child_windows:
            if hasattr(window, 'top') and window.top.winfo_exists():
                window.style = self.style
        
    def uninstall_plugin(self):
        """卸载插件"""
        # 确认对话框
        if not messagebox.askyesno("确认", "确定要卸载插件吗？这将删除所有相关文件和配置。"):
            return
            
        try:
            # 要删除的文件和文件夹列表
            items_to_delete = [
                os.path.join(self.game_path, 'BepInEx'),
                os.path.join(self.game_path, 'dotnet'),
                os.path.join(self.game_path, '.doorstop_version'),
                os.path.join(self.game_path, 'changelog.txt'),
                os.path.join(self.game_path, 'doorstop_config.ini'),
                os.path.join(self.game_path, 'sourcehansanscn_u2022'),
                os.path.join(self.game_path, 'tangyuanti_u2018'),
                os.path.join(self.game_path, 'winhttp.dll')
            ]
            
            # 删除文件和文件夹
            for item in items_to_delete:
                if os.path.exists(item):
                    if os.path.isfile(item):
                        try:
                            os.chmod(item, 0o777)  # 修改文件权限
                            os.remove(item)
                        except Exception as e:
                            messagebox.showerror("错误", f"删除文件 {os.path.basename(item)} 失败: {str(e)}")
                            return
                    else:
                        import shutil
                        try:
                            shutil.rmtree(item)
                        except Exception as e:
                            messagebox.showerror("错误", f"删除文件夹 {os.path.basename(item)} 失败: {str(e)}")
                            return
            
            # 重置按钮状态
            self.install_btn.config(state=NORMAL)  # 改为NORMAL，允许重新安装
            self.install_btn.config(text="安装翻译插件")
            self.init_btn.config(state=DISABLED)
            self.init_btn.config(text="初始化插件")
            self.config_btn.config(state=DISABLED)
            self.uninstall_btn.config(state=DISABLED)
            # 禁用附加操作按钮
            self.clear_trans_btn.config(state=DISABLED)
            self.edit_trans_btn.config(state=DISABLED)
            self.export_log_btn.config(state=DISABLED)
            
            messagebox.showinfo("成功", "插件卸载成功")
            
        except Exception as e:
            messagebox.showerror("错误", f"卸载插件时发生错误: {str(e)}")
        
    def launch_game(self):
        """启动游戏"""
        try:
            import subprocess
            import sys
            
            # 获取游戏主程序路径
            if not hasattr(self.detector, 'main_executable') or not self.detector.main_executable:
                messagebox.showerror("错误", "未找到游戏主程序")
                return
                
            # 启动游戏
            if sys.platform == 'win32':
                os.startfile(self.detector.main_executable)
            else:
                subprocess.Popen([self.detector.main_executable])
                
        except Exception as e:
            messagebox.showerror("错误", f"启动游戏失败: {str(e)}")
        
    def clear_translations(self):
        """清除已翻译文本"""
        # 确认对话框
        if not messagebox.askyesno("确认", "确定要清除所有已翻译的文本吗？这将删除所有已翻译的内容。"):
            return
            
        try:
            trans_path = os.path.join(self.game_path, 'BepInEx', 'Translation')
            if os.path.exists(trans_path):
                import shutil
                shutil.rmtree(trans_path)
                messagebox.showinfo("成功", "已清除所有翻译文本")
            else:
                messagebox.showinfo("提示", "没有找到已翻译的文本")
        except Exception as e:
            messagebox.showerror("错误", f"清除翻译文本失败: {str(e)}")

    def edit_translations(self):
        """编辑已翻译文本"""
        try:
            # 读取配置文件获取目标语言
            config_path = os.path.join(self.game_path, 'BepInEx', 'config', 'AutoTranslatorConfig.ini')
            if not os.path.exists(config_path):
                messagebox.showerror("错误", "未找到配置文件")
                return
                
            import configparser
            config = configparser.ConfigParser()
            with open(config_path, 'r', encoding='utf-8-sig') as f:
                config.read_string(f.read())
                
            target_lang = config.get('General', 'Language', fallback='')
            if not target_lang:
                messagebox.showerror("错误", "未找到目标语言设置")
                return
                
            # 构建翻译文件路径
            trans_file = os.path.join(self.game_path, 'BepInEx', 'Translation', target_lang, 'Text', '_AutoGeneratedTranslations.txt')
            if not os.path.exists(trans_file):
                messagebox.showinfo("提示", "未找到已翻译的文本文件")
                return
                
            # 使用系统默认程序打开文件
            import sys
            if sys.platform == 'win32':
                os.startfile(trans_file)
            else:
                import subprocess
                subprocess.Popen(['xdg-open', trans_file])
                
        except Exception as e:
            messagebox.showerror("错误", f"打开翻译文件失败: {str(e)}")
        
    def export_log(self):
        """导出插件日志"""
        try:
            # 检查日志文件是否存在
            log_path = os.path.join(self.game_path, 'BepInEx', 'LogOutput.log')
            if not os.path.exists(log_path):
                messagebox.showinfo("提示", "未找到插件日志文件")
                return
                
            # 生成默认文件名（使用当前时间）
            from datetime import datetime
            current_time = datetime.now().strftime("%Y%m%d_%H%M%S")
            default_filename = f"XUnity_{current_time}.log"
            
            # 弹出文件保存对话框
            save_path = filedialog.asksaveasfilename(
                title="选择保存位置",
                initialfile=default_filename,
                defaultextension=".log",
                filetypes=[("日志文件", "*.log")]
            )
            
            if not save_path:  # 用户取消了选择
                return
                
            # 复制日志文件到选择的位置
            import shutil
            shutil.copy2(log_path, save_path)
            messagebox.showinfo("成功", "插件日志导出成功")
        except Exception as e:
            messagebox.showerror("错误", f"导出插件日志失败: {str(e)}")
        
    def run(self):
        """运行程序"""
        self.root.mainloop() 