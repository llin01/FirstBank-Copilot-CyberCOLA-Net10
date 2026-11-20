# CyberCOLA_Net10

FirstBank CyberCOLA Payment Gateway System (.NET 10)

## 專案結構

- \src/\ - 主要應用程式
- \libs/\ - 共用程式庫
- \publish/\ - 發佈輸出
- \	ools/\ - 工具和測試腳本
- \.copilot/\ - Copilot 代理和技能設定

## 系統需求

- .NET 10.0.404 或更新版本
- Visual Studio 2022 Enterprise Edition
- SQL Server 2019 或更新版本
- PowerShell 7+（用於工具腳本）

## 快速開始

### 編譯專案
\\\powershell
dotnet build -c Release
\\\

### 執行測試
\\\powershell
dotnet test --no-build -c Release
\\\

### 測試資料庫連線
\\\powershell
.\tools\Test-DatabaseConnection.ps1
\\\

## Copilot 代理

- \@cybercola-pci-reviewer\ - PCI 合規審查
- \@cybercola-auth-test-runner\ - 授權系統測試
- \@cybercola-unit-test-runner\ - 單元測試執行
- \@cybercola-stress-runner\ - 壓力測試
- \@cybercola-log-analyzer\ - 日誌分析

更多資訊見 \.copilot/QUICKSTART.md\

## 重要提醒

⚠️ **不要提交的文件：**
- 完整 PAN、CVV、PIN、金鑰等敏感資料
- 資料庫密碼或連接字符串（除非加密）
- 本機環境設定檔

## 安全政策

參見 \.copilot/SECURITY.md\ 和各個代理定義。

## 許可證

FirstBank Proprietary
