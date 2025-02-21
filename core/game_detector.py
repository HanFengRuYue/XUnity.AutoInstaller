import os
import struct
import platform
import subprocess

class GameDetector:
    UNITY_FILES = ['UnityPlayer.dll', 'UnityCrashHandler64.exe', 'GameAssembly.dll']
    UNITY_FOLDERS = ['Managed', 'il2cpp_data']
    
    def __init__(self, game_path):
        self.game_path = game_path
        self.unity_version = None
        self.is_64bit = None
        self.backend_type = None
        self.main_executable = None
        
    def find_main_executable(self):
        """查找游戏主程序"""
        # 需要排除的文件名
        exclude_files = [
            'UnityCrashHandler64.exe',
            'UnityCrashHandler32.exe',
            'UnityPlayer.exe',
            'BepInEx.exe',
            'winhttp.dll',
            'doorstop_config.ini',
            'changelog.txt',
            'install_script.bat',
            'install_script_x64.bat',
            'uninstall_script.bat',
            'uninstall_script_x64.bat'
        ]
        
        # 获取目录下所有exe文件
        exe_files = []
        for file in os.listdir(self.game_path):
            if file.endswith('.exe') and file not in exclude_files:
                full_path = os.path.join(self.game_path, file)
                if os.path.isfile(full_path):
                    # 获取文件大小（通常主程序文件最大）
                    file_size = os.path.getsize(full_path)
                    exe_files.append((full_path, file_size))
        
        if not exe_files:
            return False
            
        # 按文件大小降序排序，选择最大的文件作为主程序
        exe_files.sort(key=lambda x: x[1], reverse=True)
        self.main_executable = exe_files[0][0]
        
        self.log_output(f"找到游戏主程序：{os.path.basename(self.main_executable)}")
        return True
        
    def log_output(self, message):
        """输出日志信息"""
        print(f"[GameDetector] {message}")
        
    def is_unity_game(self):
        """检查是否为Unity游戏"""
        # 检查直接目录下的Unity文件
        for file in self.UNITY_FILES:
            if os.path.exists(os.path.join(self.game_path, file)):
                return True
                
        # 检查子目录中的特征文件夹
        for root, dirs, _ in os.walk(self.game_path):
            for dir_name in dirs:
                if dir_name in self.UNITY_FOLDERS:
                    return True
        return False
        
    def detect_unity_version(self):
        """检测Unity版本"""
        if not self.main_executable or not os.path.exists(self.main_executable):
            return None
            
        try:
            # 使用powershell获取文件版本信息
            cmd = f'powershell "(Get-Item \'{self.main_executable}\').VersionInfo.FileVersion"'
            version = subprocess.check_output(cmd, shell=True).decode().strip()
            
            if not version or version == "0.0.0.0":
                # 尝试获取产品版本
                cmd = f'powershell "(Get-Item \'{self.main_executable}\').VersionInfo.ProductVersion"'
                version = subprocess.check_output(cmd, shell=True).decode().strip()
            
            self.unity_version = version
            return version
        except:
            return None
            
    def detect_architecture(self):
        """检测程序是否为64位"""
        if not self.main_executable:
            return None
            
        try:
            with open(self.main_executable, 'rb') as f:
                dos_header = f.read(2)
                if dos_header != b'MZ':
                    return None
                    
                f.seek(60)
                pe_offset = struct.unpack('<L', f.read(4))[0]
                f.seek(pe_offset)
                pe_header = f.read(6)
                
                if pe_header[0:4] != b'PE\x00\x00':
                    return None
                    
                machine_type = struct.unpack('<H', pe_header[4:6])[0]
                self.is_64bit = machine_type == 0x8664  # IMAGE_FILE_MACHINE_AMD64
                return self.is_64bit
        except:
            return None
            
    def detect_backend(self):
        """检测Unity脚本后端类型"""
        # 检测Mono后端
        for root, dirs, files in os.walk(self.game_path):
            if 'Mono' in dirs or 'Managed' in dirs:
                self.backend_type = 'Mono'
                return 'Mono'
                
        # 检测IL2CPP后端
        for root, dirs, files in os.walk(self.game_path):
            if 'il2cpp_data' in dirs or 'GameAssembly.dll' in files:
                self.backend_type = 'IL2CPP'
                return 'IL2CPP'
                
        return None
        
    def analyze_game(self):
        """分析游戏信息"""
        if not self.find_main_executable():
            return False, "未找到游戏主程序"
            
        if not self.is_unity_game():
            return False, "该游戏不是Unity引擎开发的游戏"
            
        self.detect_unity_version()
        self.detect_architecture()
        self.detect_backend()
        
        if not all([self.unity_version, self.is_64bit is not None, self.backend_type]):
            return False, "无法获取完整的游戏信息"
            
        return True, {
            'version': self.unity_version,
            'architecture': '64位' if self.is_64bit else '32位',
            'backend': self.backend_type
        } 