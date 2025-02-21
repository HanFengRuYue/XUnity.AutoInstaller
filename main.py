import os
import sys
from gui.main_window import MainWindow

def main():
    """主程序入口"""
    # 确保File目录存在
    file_dir = os.path.join(os.path.dirname(__file__), 'File')
    if not os.path.exists(file_dir):
        os.makedirs(file_dir)
        
    # 启动主窗口
    app = MainWindow()
    app.run()
    
if __name__ == '__main__':
    main() 