import os
import zipfile
import time
import subprocess
import configparser
from pathlib import Path

class PluginInstaller:
    def __init__(self, game_path):
        self.game_path = game_path
        self.file_dir = os.path.join(os.path.dirname(os.path.dirname(__file__)), 'File')
        self.main_window = None  # 添加对主窗口的引用
        
    def set_main_window(self, main_window):
        """设置主窗口引用"""
        self.main_window = main_window
        
    def log_output(self, message):
        """输出信息到主窗口"""
        if self.main_window and hasattr(self.main_window, 'log_output'):
            self.main_window.log_output(message)
            
    def install_bepinex(self, is_64bit, backend_type):
        """安装BepInEx插件框架"""
        # 选择对应的BepInEx版本
        if backend_type == 'Mono':
            zip_name = f"BepInEx_win_{'x64' if is_64bit else 'x86'}_5.4.23.2.zip"
        else:  # IL2CPP
            zip_name = f"BepInEx-Unity.IL2CPP-win-{'x64' if is_64bit else 'x86'}-6.0.0-be.733+995f049.zip"
            
        zip_path = os.path.join(self.file_dir, zip_name)
        
        if not os.path.exists(zip_path):
            return False, f"未找到BepInEx安装包：{zip_name}"
            
        try:
            with zipfile.ZipFile(zip_path, 'r') as zip_ref:
                zip_ref.extractall(self.game_path)
            return True, "BepInEx安装成功"
        except Exception as e:
            return False, f"BepInEx安装失败：{str(e)}"
            
    def install_xunity(self, backend_type):
        """安装XUnity插件"""
        # 选择对应的XUnity版本
        zip_name = "XUnity.AutoTranslator-BepInEx-IL2CPP-5.4.4.zip" if backend_type == 'IL2CPP' else "XUnity.AutoTranslator-BepInEx-5.4.4.zip"
        zip_path = os.path.join(self.file_dir, zip_name)
        
        if not os.path.exists(zip_path):
            return False, f"未找到XUnity安装包：{zip_name}"
            
        try:
            with zipfile.ZipFile(zip_path, 'r') as zip_ref:
                zip_ref.extractall(self.game_path)
            return True, "XUnity插件安装成功"
        except Exception as e:
            return False, f"XUnity插件安装失败：{str(e)}"
            
    def initialize_plugin(self, executable_path):
        """初始化插件"""
        try:
            process = subprocess.Popen([executable_path])
            config_path = os.path.join(self.game_path, 'BepInEx', 'config', 'AutoTranslatorConfig.ini')
            
            # 等待配置文件生成，最多等待60秒
            start_time = time.time()
            while not os.path.exists(config_path):
                if time.time() - start_time > 60:
                    process.kill()
                    return False, "未找到XUnity插件生成的配置文件，请检查插件是否正常运行！"
                time.sleep(1)
                
            # 配置文件生成后关闭游戏
            process.kill()
            return True, "插件初始化成功"
        except Exception as e:
            return False, f"插件初始化失败：{str(e)}"
            
    def update_config(self, config_data):
        """更新插件配置"""
        config_path = os.path.join(self.game_path, 'BepInEx', 'config', 'AutoTranslatorConfig.ini')
        if not os.path.exists(config_path):
            return False, "配置文件不存在"
            
        try:
            # 读取现有配置，保持原始格式
            config = configparser.ConfigParser(delimiters='=')
            config.optionxform = str  # 保持键的大小写
            with open(config_path, 'r', encoding='utf-8-sig') as f:
                config.read_string(f.read())
            
            # 更新General组配置
            if 'General' in config_data:
                if 'General' not in config:
                    config.add_section('General')
                for key, value in config_data['General'].items():
                    config['General'][key] = str(value)
                    
            # 更新Service组配置
            if 'Service' in config_data:
                if 'Service' not in config:
                    config.add_section('Service')
                for key, value in config_data['Service'].items():
                    config['Service'][key] = str(value)
                    
            # 更新TextFrameworks组配置
            if 'TextFrameworks' in config_data:
                if 'TextFrameworks' not in config:
                    config.add_section('TextFrameworks')
                for key, value in config_data['TextFrameworks'].items():
                    config['TextFrameworks'][key] = str(value)
                    
            # 更新Behaviour组配置
            if 'Behaviour' in config_data:
                if 'Behaviour' not in config:
                    config.add_section('Behaviour')
                for key, value in config_data['Behaviour'].items():
                    config['Behaviour'][key] = str(value)
                    
            # 更新Custom组配置
            if 'Custom' in config_data:
                if 'Custom' not in config:
                    config.add_section('Custom')
                for key, value in config_data['Custom'].items():
                    config['Custom'][key] = str(value)
                    
            # 更新Baidu组配置
            if 'Baidu' in config_data:
                if 'Baidu' not in config:
                    config.add_section('Baidu')
                for key, value in config_data['Baidu'].items():
                    config['Baidu'][key] = str(value)
                    
            # 更新LingoCloud组配置
            if 'LingoCloud' in config_data:
                if 'LingoCloud' not in config:
                    config.add_section('LingoCloud')
                for key, value in config_data['LingoCloud'].items():
                    config['LingoCloud'][key] = str(value)
                    
            # 设置固定配置
            if 'Baidu' not in config:
                config.add_section('Baidu')
            config['Baidu']['DelaySeconds'] = '0.1'
            
            if 'Custom' not in config:
                config.add_section('Custom')
            config['Custom']['EnableShortDelay'] = 'True'
            config['Custom']['DisableSpamChecks'] = 'True'
            
            if 'Behaviour' not in config:
                config.add_section('Behaviour')
            config['Behaviour']['ReloadTranslationsOnFileChange'] = 'True'
            
            # 自定义写入格式，不添加空格
            with open(config_path, 'w', encoding='utf-8') as f:
                for section in config.sections():
                    f.write(f'[{section}]\n')
                    for key, value in config[section].items():
                        f.write(f'{key}={value}\n')
                    f.write('\n')
                
            return True, "配置更新成功"
        except Exception as e:
            return False, f"配置更新失败：{str(e)}" 