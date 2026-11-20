# GitHub Copilot 快速參考

**CyberCOLA_Net10 專案 - Agents 和 Skills 指南**

## 🎯 核心 Agents 命令

### PCI 合規性檢查
```
@cybercola-pci-reviewer
審查我的變更以確保卡號/敏感資料安全
```

### 授權測試執行
```
@cybercola-auth-test-runner
跑授權測試 (NCCC, FISC, SVCS, ICASH, IPASS)
```

### 單元測試
```
@cybercola-unit-test-runner
執行 Yetty.Test 和 CyberCOLA.Test
```

### 壓力測試
```
@cybercola-stress-runner
執行 TPS 和延遲測試
```

### 日誌分析
```
@cybercola-log-analyzer
分析和診斷系統日誌
```

### ISO 8583 解碼
```
@cybercola-iso8583-decoder
解碼和分析 ISO 8583 訊息
```

### 發佈驗證
```
@cybercola-publish-checker
驗證發佈輸出和建立工件
```

### 配置稽核
```
@cybercola-config-auditor
檢查設定檔和環境配置
```

### SQL 預存程序檢查
```
@cybercola-sp-reader
檢查和分析 SQL Server 預存程序
```

### 工具同步檢查
```
@cybercola-tools-sync-checker
驗證 PowerShell 工具和指令碼同步
```

## 📚 相關 Skills

### CyberCOLA 專用
- **cybercola-auth-test** - 授權測試技能和最佳實踐

### 核心開發
- **csharp-coding-standards** - C# 編碼標準
- **csharp-concurrency-patterns** - 並行模式
- **efcore-patterns** - Entity Framework Core 模式
- **microsoft-extensions-dependency-injection** - DI 容器使用

### 測試
- **testcontainers** - 容器化測試
- **snapshot-testing** - 快照測試
- **database-performance** - 資料庫效能

### 部署和工具
- **aspire-configuration** - .NET Aspire 配置
- **dotnet-devcert-trust** - 開發憑證管理
- **package-management** - 套件管理

## 🔍 常見工作流

### 1️⃣ 程式碼變更審查
```
1. 進行變更
2. 使用 @cybercola-pci-reviewer 檢查卡片資料安全
3. 檢查日誌和異常處理
4. 提交 PR
```

### 2️⃣ 測試變更
```
1. 修改 GW/COLA/Auth 程式碼
2. 執行 @cybercola-unit-test-runner
3. 執行 @cybercola-auth-test-runner （完整網路測試）
4. 使用 @cybercola-stress-runner 進行效能檢查
```

### 3️⃣ 發佈驗證
```
1. 建置發佈 (dotnet publish -r win-x64)
2. 使用 @cybercola-publish-checker 驗證輸出
3. 檢查版本戳記和依賴項
```

### 4️⃣ 除錯 ISO 8583 問題
```
1. 收集交易日誌中的訊息
2. 使用 @cybercola-iso8583-decoder 分析結構
3. 檢查回應碼和欄位對應
4. 使用 @cybercola-log-analyzer 追蹤流程
```

## 📋 檔案位置參考

| 項目 | 位置 |
|------|------|
| Agent 定義 | `.copilot/agents/*.md` |
| Skills 定義 | `.copilot/skills/*/SKILL.md` |
| 設定檔 | `.copilot/settings.json` |
| 規則 | `.copilot/rules/*.md` |
| 此指南 | `.copilot/QUICKSTART.md` |
| 轉換報告 | `.copilot/CONVERSION_REPORT.md` |

## 💡 提示

- 所有 Agents 都是 **唯讀的**，除了 `cybercola-auth-test-runner` 和 `cybercola-stress-runner`（但它們不會編輯程式碼）
- 在提交 PR 前，務必執行 `@cybercola-pci-reviewer`
- 授權測試使用真實的網路連接 - 在執行前檢查 VPN 和資料庫連接
- ISO 8583 訊息很複雜 - 使用 `@cybercola-iso8583-decoder` 以避免錯誤

## ❓ 需要幫助？

參考以下文件以了解更多：
- **CLAUDE.md** - 專案架構和慣例
- **docs/transaction_flow.md** - 交易流程詳情
- **docs/AUTH_TIMEOUT_REFERENCE.md** - 超時設定
- **tools/README.md** - PowerShell 工具目錄
