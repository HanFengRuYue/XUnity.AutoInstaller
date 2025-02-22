import tkinter as tk
from tkinter import messagebox
import ttkbootstrap as ttk
from ttkbootstrap.constants import *
import configparser
import os
from core.plugin_installer import PluginInstaller

class ConfigWindow:
    def __init__(self, parent, game_path):
        # 保存对主窗口的引用
        self.parent = parent
        
        # 创建配置窗口并设置基本属性
        self.top = ttk.Toplevel(parent)
        self.top.title("编辑插件配置")
        self.top.geometry("800x580")
        self.top.resizable(False, False)
        
        # 使用主窗口的style对象
        self.style = parent.style
        
        self.game_path = game_path
        self.installer = PluginInstaller(game_path)
        self.installer.set_main_window(parent)  # 设置installer的主窗口引用
        
        self.setup_ui()
        self.load_config()
        
    def setup_ui(self):
        """设置UI界面"""
        main_frame = ttk.Frame(self.top, padding=20)
        main_frame.pack(fill=BOTH, expand=YES)
        
        # 基本设置和翻译服务设置
        settings_frame = ttk.Frame(main_frame)
        settings_frame.pack(fill=X, pady=5)
        
        # 创建左右两列的框架
        settings_left_frame = ttk.LabelFrame(settings_frame, text="翻译语言", padding=10)
        settings_left_frame.pack(side=LEFT, fill=BOTH, expand=YES, padx=(0, 5))
        
        settings_right_frame = ttk.LabelFrame(settings_frame, text="翻译服务", padding=10)
        settings_right_frame.pack(side=LEFT, fill=BOTH, expand=YES, padx=(5, 0))
        
        # 创建内部容器以实现居中效果
        left_container = ttk.Frame(settings_left_frame)
        left_container.pack(expand=YES, fill=BOTH, padx=5, pady=5)
        
        right_container = ttk.Frame(settings_right_frame)
        right_container.pack(expand=YES, fill=BOTH, padx=5, pady=5)
        
        # 语言代码映射
        self.lang_map = {
            '中文': 'zh',
            '简体中文': 'zh-CN',
            '繁体中文': 'zh-TW',
            '英文': 'en',
            '日文': 'ja',
            '俄文': 'ru',
            '韩文': 'ko'
        }
        lang_options = list(self.lang_map.keys())
        
        # 原语言
        ttk.Label(left_container, text="原语言:").grid(row=0, column=0, sticky=E, padx=(0, 5))
        self.from_lang_var = tk.StringVar()
        self.from_lang_combo = ttk.Combobox(
            left_container,
            textvariable=self.from_lang_var,
            values=lang_options,
            width=20
        )
        self.from_lang_combo.grid(row=0, column=1, sticky=EW)
        self.from_lang_combo.bind('<KeyRelease>', lambda e: self.validate_lang_code(self.from_lang_var))
        
        # 翻译语言
        ttk.Label(left_container, text="翻译语言:").grid(row=1, column=0, sticky=E, padx=(0, 5))
        self.to_lang_var = tk.StringVar()
        self.to_lang_combo = ttk.Combobox(
            left_container,
            textvariable=self.to_lang_var,
            values=lang_options,
            width=20
        )
        self.to_lang_combo.grid(row=1, column=1, sticky=EW)
        self.to_lang_combo.bind('<KeyRelease>', lambda e: self.validate_lang_code(self.to_lang_var))
        
        # 添加语言代码提示
        hint_style = ttk.Style()
        hint_style.configure("small.info.TLabel", font=("", 10))  # 使用更小的字号
        ttk.Label(
            left_container,
            text="提示：可直接输入语言代码，如：en(英文)、ja(日文)、zh(中文)",
            wraplength=300,
            style="small.info.TLabel"  # 使用新的样式
        ).grid(row=2, column=0, columnspan=2, sticky=W, pady=(5, 0))
        
        # 确保左侧容器列宽合适
        left_container.grid_columnconfigure(1, weight=1)
        
        # 首选翻译平台
        ttk.Label(right_container, text="首选翻译平台:").grid(row=0, column=0, sticky=E, padx=(0, 5))
        self.primary_service_var = tk.StringVar()
        services = ['Papago', '百度', '彩云小译', '自定义', '谷歌', '必应']
        primary_combo = ttk.Combobox(right_container, textvariable=self.primary_service_var, values=services, width=20)
        primary_combo.grid(row=0, column=1, sticky=EW)
        
        # 备选翻译平台
        ttk.Label(right_container, text="备选翻译平台:").grid(row=1, column=0, sticky=E, padx=(0, 5))
        self.fallback_service_var = tk.StringVar()
        fallback_combo = ttk.Combobox(right_container, textvariable=self.fallback_service_var, values=services, width=20)
        fallback_combo.grid(row=1, column=1, sticky=EW)
        
        # 确保右侧容器列宽合适
        right_container.grid_columnconfigure(1, weight=1)
        
        # 为容器添加行权重以实现垂直居中
        for container in [left_container, right_container]:
            for i in range(3):
                container.grid_rowconfigure(i, weight=1)
        
        # TextMeshPro和框架设置
        tmp_framework_frame = ttk.Frame(main_frame)
        tmp_framework_frame.pack(fill=X, pady=5)
        
        # 创建左右两列的框架
        tmp_left_frame = ttk.LabelFrame(tmp_framework_frame, text="TextMeshPro字体设置", padding=10)
        tmp_left_frame.pack(side=LEFT, fill=BOTH, expand=YES, padx=(0, 5))
        
        tmp_right_frame = ttk.LabelFrame(tmp_framework_frame, text="捕获文本框架设置", padding=10)
        tmp_right_frame.pack(side=LEFT, fill=BOTH, expand=YES, padx=(5, 0))
        
        # TextMeshPro设置
        tmp_container = ttk.Frame(tmp_left_frame)
        tmp_container.pack(expand=YES, fill=BOTH, padx=5, pady=5)
        
        # 字体选项
        self.font_options = ['', 'sourcehansanscn_u6', 'sourcehansanscn_u2018', 'sourcehansanscn_u2019', 'sourcehansanscn_u2020', 'sourcehansanscn_u2021', 'sourcehansanscn_u2022']
        
        ttk.Label(tmp_container, text="覆盖TextMeshPro字体:").grid(row=0, column=0, sticky=E, padx=(0, 5))
        self.tmp_font_var = tk.StringVar()
        self.tmp_font_combo = ttk.Combobox(
            tmp_container,
            textvariable=self.tmp_font_var,
            values=self.font_options,
            width=25
        )
        self.tmp_font_combo.grid(row=0, column=1, sticky=EW)
        
        ttk.Label(tmp_container, text="备选TextMeshPro字体:").grid(row=1, column=0, sticky=E, padx=(0, 5))
        self.tmp_fallback_font_var = tk.StringVar()
        self.tmp_fallback_font_combo = ttk.Combobox(
            tmp_container,
            textvariable=self.tmp_fallback_font_var,
            values=self.font_options,
            width=25
        )
        self.tmp_fallback_font_combo.grid(row=1, column=1, sticky=EW)
        
        # 确保容器列宽合适
        tmp_container.grid_columnconfigure(1, weight=1)
        
        # 为容器添加行权重以实现垂直居中
        for i in range(2):
            tmp_container.grid_rowconfigure(i, weight=1)
            
        # 添加提示文本
        hint_style = ttk.Style()
        hint_style.configure("small.info.TLabel", font=("", 10))
        ttk.Label(
            tmp_container,
            text="提示：可直接输入字体文件名，或从列表中选择",
            wraplength=300,
            style="small.info.TLabel"
        ).grid(row=2, column=0, columnspan=2, sticky=W, pady=(5, 0))
        
        # 创建复选框变量
        self.framework_vars = {
            'UGUI': tk.BooleanVar(),
            'NGUI': tk.BooleanVar(),
            'TextMeshPro': tk.BooleanVar(),
            'TextMesh': tk.BooleanVar(),
            'IMGUI': tk.BooleanVar(),
            'FairyGUI': tk.BooleanVar()
        }
        
        # 创建一个框架来容纳复选框，并使其居中
        checkbox_container = ttk.Frame(tmp_right_frame)
        checkbox_container.pack(expand=YES, fill=BOTH, padx=10, pady=5)
        
        # 计算每列的宽度
        max_text_length = max(len(name) for name in self.framework_vars.keys())
        column_width = max_text_length + 10  # 添加一些额外的空间
        
        # 添加复选框（2列布局）
        row = 0
        col = 0
        for name, var in self.framework_vars.items():
            cb = ttk.Checkbutton(
                checkbox_container,
                text=f"捕获{name}文本",
                variable=var,
                width=column_width
            )
            cb.grid(row=row, column=col, sticky=W, padx=5, pady=2)
            col += 1
            if col > 1:  # 改为2列布局
                col = 0
                row += 1
                
        # 确保容器框架能够正确扩展
        for i in range(2):  # 2列
            checkbox_container.grid_columnconfigure(i, weight=1)
        for i in range(3):  # 3行
            checkbox_container.grid_rowconfigure(i, weight=1)
        
        # API和文本设置
        api_text_frame = ttk.Frame(main_frame)
        api_text_frame.pack(fill=X, pady=5)
        
        # 创建左右两列的框架
        api_left_frame = ttk.LabelFrame(api_text_frame, text="翻译服务API设置", padding=10)
        api_left_frame.pack(side=LEFT, fill=BOTH, expand=YES, padx=(0, 5))
        
        api_right_frame = ttk.LabelFrame(api_text_frame, text="翻译文本设置", padding=10)
        api_right_frame.pack(side=LEFT, fill=BOTH, expand=YES, padx=(5, 0))
        
        # 创建内部容器以实现居中效果
        api_container = ttk.Frame(api_left_frame)
        api_container.pack(expand=YES, fill=BOTH, padx=5, pady=5)
        
        text_container = ttk.Frame(api_right_frame)
        text_container.pack(expand=YES, fill=BOTH, padx=5, pady=5)
        
        # API设置（左列）
        ttk.Label(api_container, text="自定义翻译平台:").grid(row=0, column=0, sticky=E, padx=(0, 5))
        self.custom_url_var = tk.StringVar()
        ttk.Entry(api_container, textvariable=self.custom_url_var, width=35).grid(row=0, column=1, sticky=EW)
        
        ttk.Label(api_container, text="百度AppId:").grid(row=1, column=0, sticky=E, padx=(0, 5))
        self.baidu_id_var = tk.StringVar()
        ttk.Entry(api_container, textvariable=self.baidu_id_var, width=35).grid(row=1, column=1, sticky=EW)
        
        ttk.Label(api_container, text="百度App密钥:").grid(row=2, column=0, sticky=E, padx=(0, 5))
        self.baidu_key_var = tk.StringVar()
        ttk.Entry(api_container, textvariable=self.baidu_key_var, width=35).grid(row=2, column=1, sticky=EW)
        
        ttk.Label(api_container, text="彩云小译Token:").grid(row=3, column=0, sticky=E, padx=(0, 5))
        self.caiyun_token_var = tk.StringVar()
        ttk.Entry(api_container, textvariable=self.caiyun_token_var, width=35).grid(row=3, column=1, sticky=EW)
        
        # 确保API容器列宽合适
        api_container.grid_columnconfigure(1, weight=1)
        
        # 为API容器添加行权重以实现垂直居中
        for i in range(4):
            api_container.grid_rowconfigure(i, weight=1)
        
        # 文本设置（右列）
        ttk.Label(text_container, text="最大翻译字数:").grid(row=0, column=0, sticky=E, padx=(0, 5))
        self.max_chars_var = tk.StringVar()
        ttk.Entry(text_container, textvariable=self.max_chars_var, width=15).grid(row=0, column=1, sticky=EW)
        
        ttk.Label(text_container, text="自动换行字数:").grid(row=1, column=0, sticky=E, padx=(0, 5))
        self.split_chars_var = tk.StringVar()
        ttk.Entry(text_container, textvariable=self.split_chars_var, width=15).grid(row=1, column=1, sticky=EW)
        
        # 确保文本容器列宽合适
        text_container.grid_columnconfigure(1, weight=1)
        
        # 为文本容器添加行权重以实现垂直居中
        for i in range(2):
            text_container.grid_rowconfigure(i, weight=1)
        
        # 保存按钮
        save_btn = ttk.Button(
            main_frame,
            text="保存配置",
            command=self.save_config,
            style="primary.TButton",
            width=20
        )
        save_btn.pack(pady=10)
        
    def validate_lang_code(self, var):
        """验证并转换语言代码"""
        value = var.get()
        # 如果选择了预设选项
        if value in self.lang_map:
            var.set(self.lang_map[value])
        # 如果直接输入了语言代码，保持不变
        
    def load_config(self):
        """加载配置"""
        config_path = os.path.join(self.game_path, 'BepInEx', 'config', 'AutoTranslatorConfig.ini')
        if not os.path.exists(config_path):
            self.log_output("错误：配置文件不存在")
            self.top.destroy()
            return
            
        try:
            # 使用utf-8-sig编码来处理带BOM的UTF-8文件
            with open(config_path, 'r', encoding='utf-8-sig') as f:
                content = f.read()
                
            config = configparser.ConfigParser()
            config.read_string(content)
            
            # 加载基本设置
            from_lang = config.get('General', 'FromLanguage', fallback='')
            to_lang = config.get('General', 'Language', fallback='')
            
            # 设置语言选项
            self.set_lang_value(self.from_lang_var, from_lang)
            self.set_lang_value(self.to_lang_var, to_lang)
            
            # 加载服务设置
            endpoint = config.get('Service', 'Endpoint', fallback='')
            fallback = config.get('Service', 'FallbackEndpoint', fallback='')
            
            # 转换服务名称
            service_map = {
                'GoogleTranslateV2': '谷歌',
                'BingTranslate': '必应',
                'PapagoTranslate': 'Papago',
                'BaiduTranslate': '百度',
                'LingoCloudTranslate': '彩云小译',
                'CustomTranslate': '自定义'
            }
            
            self.primary_service_var.set(service_map.get(endpoint, ''))
            self.fallback_service_var.set(service_map.get(fallback, ''))
            
            # 加载API设置
            self.custom_url_var.set(config.get('Custom', 'Url', fallback=''))
            self.baidu_id_var.set(config.get('Baidu', 'BaiduAppId', fallback=''))
            self.baidu_key_var.set(config.get('Baidu', 'BaiduAppSecret', fallback=''))
            self.caiyun_token_var.set(config.get('LingoCloud', 'LingoCloudToken', fallback=''))
            
            # 加载文本设置
            self.max_chars_var.set(config.get('Behaviour', 'MaxCharactersPerTranslation', fallback=''))
            self.split_chars_var.set(config.get('Behaviour', 'ForceSplitTextAfterCharacters', fallback=''))
            
            # 加载TextMeshPro设置
            self.tmp_font_var.set(config.get('Behaviour', 'OverrideFontTextMeshPro', fallback=''))
            self.tmp_fallback_font_var.set(config.get('Behaviour', 'FallbackFontTextMeshPro', fallback=''))
            
            # 加载框架设置
            self.framework_vars['UGUI'].set(config.getboolean('TextFrameworks', 'EnableUGUI', fallback=True))
            self.framework_vars['NGUI'].set(config.getboolean('TextFrameworks', 'EnableNGUI', fallback=True))
            self.framework_vars['TextMeshPro'].set(config.getboolean('TextFrameworks', 'EnableTextMeshPro', fallback=True))
            self.framework_vars['TextMesh'].set(config.getboolean('TextFrameworks', 'EnableTextMesh', fallback=True))
            self.framework_vars['IMGUI'].set(config.getboolean('TextFrameworks', 'EnableIMGUI', fallback=True))
            self.framework_vars['FairyGUI'].set(config.getboolean('TextFrameworks', 'EnableFairyGUI', fallback=True))
            
            self.log_output("配置文件加载成功")
            
        except Exception as e:
            self.log_output(f"读取配置文件时发生错误: {str(e)}")
            self.top.destroy()
        
    def set_lang_value(self, var, code):
        """设置语言值，自动处理代码和显示名称的转换"""
        # 反向查找语言名称
        for name, lang_code in self.lang_map.items():
            if lang_code == code:
                var.set(name)
                return
        # 如果找不到对应的名称，直接设置代码
        var.set(code)
        
    def log_output(self, message):
        """输出信息到主窗口"""
        if hasattr(self.parent, 'log_output'):
            self.parent.log_output(message)

    def handle_font_files(self):
        """处理字体文件的复制和删除"""
        def process_font(font_var, is_fallback=False):
            font_name = font_var.get()
            font_type = "fallback" if is_fallback else "override"
            
            try:
                # 如果选择了预设字体选项
                if font_name in ['sourcehansanscn_u6', 'sourcehansanscn_u2018', 'sourcehansanscn_u2019', 'sourcehansanscn_u2020', 'sourcehansanscn_u2021', 'sourcehansanscn_u2022']:
                    # 源文件路径
                    source_path = os.path.join(os.path.dirname(os.path.dirname(__file__)), 'File', font_name)
                    # 目标路径
                    target_path = os.path.join(self.game_path, font_name)
                    
                    # 复制文件到游戏目录
                    if os.path.exists(source_path):
                        import shutil
                        try:
                            # 如果目标文件已存在，先尝试删除
                            if os.path.exists(target_path):
                                os.chmod(target_path, 0o777)  # 修改文件权限
                                os.remove(target_path)
                            # 复制新文件
                            shutil.copy2(source_path, target_path)
                            self.log_output(f"已复制{font_type}字体文件: {font_name}")
                        except Exception as e:
                            self.log_output(f"复制{font_type}字体文件失败: {str(e)}")
                
                # 如果清空了选项
                elif not font_name:
                    # 查找并删除可能存在的字体文件
                    for font in ['sourcehansanscn_u6', 'sourcehansanscn_u2018', 'sourcehansanscn_u2019', 'sourcehansanscn_u2020', 'sourcehansanscn_u2021', 'sourcehansanscn_u2022']:
                        target_path = os.path.join(self.game_path, font)
                        if os.path.exists(target_path):
                            try:
                                os.chmod(target_path, 0o777)  # 修改文件权限
                                os.remove(target_path)
                                self.log_output(f"已删除{font_type}字体文件: {font}")
                            except Exception as e:
                                self.log_output(f"删除{font_type}字体文件失败: {str(e)}")
                                
            except Exception as e:
                self.log_output(f"处理{font_type}字体文件时发生错误: {str(e)}")
        
        # 处理主字体和备选字体
        process_font(self.tmp_font_var)
        process_font(self.tmp_fallback_font_var, True)
        
    def save_config(self):
        """保存配置"""
        self.log_output("正在保存配置...")
        
        # 处理字体文件
        self.handle_font_files()
        
        # 准备配置数据
        service_map = {
            '谷歌': 'GoogleTranslateV2',
            '必应': 'BingTranslate',
            'Papago': 'PapagoTranslate',
            '百度': 'BaiduTranslate',
            '彩云小译': 'LingoCloudTranslate',
            '自定义': 'CustomTranslate'
        }
        
        # 获取语言代码
        from_lang = self.from_lang_var.get()
        to_lang = self.to_lang_var.get()
        
        # 如果选择的是显示名称，转换为语言代码
        if from_lang in self.lang_map:
            from_lang = self.lang_map[from_lang]
        if to_lang in self.lang_map:
            to_lang = self.lang_map[to_lang]
        
        config_data = {
            'General': {
                'FromLanguage': from_lang,
                'Language': to_lang
            },
            'Service': {
                'Endpoint': service_map.get(self.primary_service_var.get(), ''),
                'FallbackEndpoint': service_map.get(self.fallback_service_var.get(), '')
            },
            'Custom': {
                'Url': self.custom_url_var.get()
            },
            'Baidu': {
                'BaiduAppId': self.baidu_id_var.get(),
                'BaiduAppSecret': self.baidu_key_var.get()
            },
            'LingoCloud': {
                'LingoCloudToken': self.caiyun_token_var.get()
            },
            'Behaviour': {
                'MaxCharactersPerTranslation': self.max_chars_var.get(),
                'ForceSplitTextAfterCharacters': self.split_chars_var.get(),
                'OverrideFontTextMeshPro': self.tmp_font_var.get(),
                'FallbackFontTextMeshPro': self.tmp_fallback_font_var.get()
            },
            'TextFrameworks': {
                'EnableUGUI': str(self.framework_vars['UGUI'].get()),
                'EnableNGUI': str(self.framework_vars['NGUI'].get()),
                'EnableTextMeshPro': str(self.framework_vars['TextMeshPro'].get()),
                'EnableTextMesh': str(self.framework_vars['TextMesh'].get()),
                'EnableIMGUI': str(self.framework_vars['IMGUI'].get()),
                'EnableFairyGUI': str(self.framework_vars['FairyGUI'].get())
            }
        }
        
        # 保存配置
        success, message = self.installer.update_config(config_data)
        
        if success:
            messagebox.showinfo("成功", "配置更新成功")
            self.top.destroy()
        else:
            messagebox.showerror("错误", f"配置更新失败: {message}") 