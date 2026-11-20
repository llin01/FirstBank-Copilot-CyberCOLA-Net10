import os
import re

css_files = [
    r"E:\FirstBank\Claude\CyberCOLA_Net10\src\CyberCOLA\WebClientSimulator\wwwroot\css\style.css",
    r"E:\FirstBank\Claude\CyberCOLA_Net10\src\CyberCOLA\WebEAISimulator\wwwroot\css\style.css",
    r"E:\FirstBank\Claude\CyberCOLA_Net10\src\CyberCOLA\WebETicketClientSimulator\wwwroot\css\style.css",
    r"E:\FirstBank\Claude\CyberCOLA_Net10\src\CyberCOLA\WebFiscMaintSimulator\wwwroot\css\style.css",
    r"E:\FirstBank\Claude\CyberCOLA_Net10\src\CyberCOLA\WebHSMSimulator\wwwroot\css\style.css",
    r"E:\FirstBank\Claude\CyberCOLA_Net10\src\CyberCOLA\WebHighStressSimulator\wwwroot\css\style.css",
    r"E:\FirstBank\Claude\CyberCOLA_Net10\src\CyberCOLA\WebSMSSimulator\wwwroot\css\style.css",
    r"E:\FirstBank\Claude\CyberCOLA_Net10\src\CyberCOLA\WebStressSimulator\wwwroot\css\style.css",
]

for file_path in css_files:
    if not os.path.exists(file_path):
        print(f"Skipping: {file_path} (not exist)")
        continue
        
    print(f"Processing: {file_path}")
    with open(file_path, "r", encoding="utf-8") as f:
        content = f.read()
        
    # 進行一些常見的寫死白色字體替換，這些文字在高亮底色上會不可見
    # 1. 終端機或 input 等地方如果是 color: #ffffff 或 color: #fff
    # 為了避免誤傷像按鈕 .btn-primary 裡的白色，我們針對特定的選擇器來做替換
    
    # 替換 input, select, textarea, option 等表單欄位裡的白色字體
    # 我們先比對 .form-group input, input, select 等地方
    lines = content.split('\n')
    in_input_block = False
    in_terminal_block = False
    in_text_link_block = False
    
    for i in range(len(lines)):
        line = lines[i]
        # 偵測輸入框相關的選擇器區塊
        if any(sel in line for sel in ["input", "select", "textarea", ".form-control", ".form-group"]) and "{" in line:
            in_input_block = True
        if ".terminal" in line and "{" in line:
            in_terminal_block = True
        if ".text-link" in line and "{" in line:
            in_text_link_block = True
            
        if "}" in line:
            in_input_block = False
            in_terminal_block = False
            in_text_link_block = False
            
        # 替換邏輯
        if in_input_block and ("color: #ffffff" in line or "color: #fff" in line):
            print(f"  Fixing input text color at line {i+1}: {line}")
            lines[i] = line.replace("#ffffff", "var(--text-primary, var(--text-main, #0f172a))").replace("#fff", "var(--text-primary, var(--text-main, #0f172a))")
            
        if in_terminal_block and ("color: #ffffff" in line or "color: #fff" in line or "color: #e5e7eb" in line):
            print(f"  Fixing terminal text color at line {i+1}: {line}")
            lines[i] = line.replace("#ffffff", "var(--text-primary, var(--text-main, #0f172a))").replace("#fff", "var(--text-primary, var(--text-main, #0f172a))").replace("#e5e7eb", "var(--text-primary, var(--text-main, #0f172a))")
            
        if in_text_link_block and ("color: #ffffff" in line or "color: #fff" in line):
            print(f"  Fixing text-link hover color at line {i+1}: {line}")
            lines[i] = line.replace("#ffffff", "var(--primary-color, #5d5bf4)").replace("#fff", "var(--primary-color, #5d5bf4)")
            
    content = '\n'.join(lines)
    
    # 針對 WebStressSimulator 特別修正：
    # .template-files-list 的背景太暗了，改為淺灰白背景
    content = content.replace("background: rgba(0, 0, 0, 0.25);", "background: rgba(15, 23, 42, 0.04);")
    content = content.replace("border: 1px solid rgba(255, 255, 255, 0.05);", "border: 1px solid var(--border-color);")
    
    # checkbox hover 等的白色
    content = content.replace("color: #fff;\n    background: rgba(255, 122, 0, 0.05);", "color: var(--accent-orange, #ff7a00);\n    background: rgba(255, 122, 0, 0.1);")
    content = content.replace("color: #fff;\n    background: rgba(255, 94, 0, 0.05);", "color: var(--neon-orange, #ff5e00);\n    background: rgba(255, 94, 0, 0.1);")
    
    # 7. 某些表格 th 內文字可能是白色
    content = content.replace("color: #ffffff;\n    font-weight: 600;", "color: var(--text-primary, var(--text-main, #0f172a));\n    font-weight: 600;")
    content = content.replace("color: #fff;\n    font-weight: 600;", "color: var(--text-primary, var(--text-main, #0f172a));\n    font-weight: 600;")

    # 確保 th 的 color 不是白色
    content = re.sub(
        r"(th\s*\{[^}]*color:\s*)#fff(fff)?",
        r"\g<1>var(--text-primary, var(--text-main, #0f172a))",
        content
    )
    
    with open(file_path, "w", encoding="utf-8") as f:
        f.write(content)
        
print("CSS color scan and adjustment complete.")
