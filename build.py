import os
import sys
import subprocess
import warnings

# 过滤掉特定的警告
warnings.filterwarnings("ignore", category=SyntaxWarning, module="ttkbootstrap")

def check_and_install_dependencies():
    """检查并安装必要的依赖"""
    try:
        import PyInstaller
    except ImportError:
        print("正在安装 PyInstaller...")
        subprocess.check_call([sys.executable, "-m", "pip", "install", "--upgrade", "pyinstaller==6.3.0"])
        print("PyInstaller 安装完成")

def build():
    """打包程序"""
    try:
        # 检查并安装依赖
        check_and_install_dependencies()
        
        # 获取当前目录
        current_dir = os.path.dirname(os.path.abspath(__file__))
        
        # 配置打包参数
        params = [
            'main.py',  # 主程序入口
            '--name=XUnity自动安装工具',  # 程序名称
            '--windowed',  # 使用窗口模式
            '--noconsole',  # 不显示控制台
            f'--add-data={os.path.join(current_dir, "File")};File',  # 添加File目录
            '--clean',  # 清理临时文件
            '--noconfirm',  # 不确认覆盖
            '--uac-admin',  # 请求管理员权限
            '--onefile',  # 打包成单个文件
            '--log-level=WARN',  # 只显示警告和错误
        ]
        
        try:
            # 执行打包
            import PyInstaller.__main__
            PyInstaller.__main__.run(params)
            print("打包完成！")
        except Exception as e:
            print(f"打包过程中出现错误: {str(e)}")
            sys.exit(1)
            
    except Exception as e:
        print(f"构建过程中出现错误: {str(e)}")
        sys.exit(1)

if __name__ == '__main__':
    build() 