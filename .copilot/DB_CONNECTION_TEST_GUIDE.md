# 資料庫連線測試指南（Copilot 版）

在 GitHub Copilot 中測試資料庫連線的三種方式。

## 方法 1️⃣：在 Copilot Chat 中直接執行

**最簡單，推薦新手使用**

```powershell
@cybercola-unit-test-runner
Run the database connection test: .\tools\Test-DatabaseConnection.ps1
```

Copilot 會：
- ✅ 自動在 PowerShell 中執行腳本
- ✅ 解密 SystemParameterInfo.xml 中的連接字符串
- ✅ 驗證 SQL Server TCP 連線（Port 1433）
- ✅ 檢查 GlobalProtect VPN 連線狀態
- ✅ 顯示測試結果

---

## 方法 2️⃣：建立自動化測試工作階段

**適合重複測試或排程執行**

在 Copilot 的「Automations」標籤中建立：

```
名稱：每日資料庫連線檢查
排程：每天上午 09:00
代理：cybercola-unit-test-runner

提示內容：
"執行 .\tools\Test-DatabaseConnection.ps1
然後輸出測試結果到 logs/db-connection-test.log"
```

---

## 方法 3️⃣：建立子工作階段進行專項測試

**適合深度除錯或與其他測試組合**

```
1. 在 Copilot 中按「+」建立新工作階段
2. 指定項目為：CyberCOLA_Net10
3. 輸入提示：

"測試資料庫連線：
1. 執行 .\tools\Test-DatabaseConnection.ps1
2. 如果失敗，讀取 SystemParameterInfo.xml 診斷問題
3. 檢查 connection string 加密狀態
4. 驗證 DbStoreProc 類別邏輯"
```

---

## 🔄 從 Claude Code CLI 遷移對比

| 操作 | Claude Code CLI | GitHub Copilot |
|------|-----------------|-----------------|
| **執行連線測試** | `.\tools\Test-DatabaseConnection.ps1` | `@cybercola-unit-test-runner` + 提示 |
| **查看加密狀態** | 手動編輯 XML | 代理自動解析 |
| **排程執行** | Windows Task Scheduler | Automations 標籤 |
| **結果追蹤** | 本地日誌 | Copilot 聊天記錄 + 檔案庫 |
| **團隊分享** | 無 | 在專案中共享自動化設定 |

---

## ⚙️ 腳本功能清單

Test-DatabaseConnection.ps1 包含：

✅ **AES-GCM 解密** — 讀取加密的連接字符串  
✅ **SQL Server 探測** — TCP 1433 連線檢查  
✅ **VPN 驗證** — GlobalProtect 連線確認  
✅ **錯誤診斷** — 詳細的失敗原因說明  
✅ **互動式確認** — 允許略過失敗繼續執行  

---

## 🎯 快速開始

**現在就試試：**

在 Copilot Chat 中輸入：
```
@cybercola-unit-test-runner
Run the database connection test from tools directory
```

或建立自動化：
1. 開啟 Automations 標籤
2. 按「New Automation」
3. 選擇 cybercola-unit-test-runner 代理
4. 輸入 `.\tools\Test-DatabaseConnection.ps1`
5. 設定排程（例：Daily at 9:00 AM）

---

## ⚠️ 注意事項

- ✅ **推薦**：在開發機上首先執行一次，確認連線成功
- ⚠️ **VPN**：連線遠端 SQL Server 需要 GlobalProtect VPN
- 🔒 **安全**：密碼遮蔽在日誌中，不會外洩
- 📋 **需求**：PowerShell 7+（自動偵測，不支援 PowerShell 5）

---

## 📚 相關檔案

- `tools/Test-DatabaseConnection.ps1` — 連線測試主程式
- `tools/CyberCOLA-Common.ps1` — 通用函式庫（AES-GCM 解密）
- `tools/Connect-GlobalProtect.ps1` — VPN 連線驗證
- `src/CyberCOLA/Config/SystemParameterInfo.xml` — 連接字符串定義

---

✨ **準備好測試資料庫連線了嗎？**
