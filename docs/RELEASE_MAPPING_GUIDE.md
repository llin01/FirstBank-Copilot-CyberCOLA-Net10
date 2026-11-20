# CyberCOLA .NET 10 系統 - Release 版本編譯與發行路徑映射指南

本文件旨在詳細記錄與引導開發及運維人員，在編譯 (Build) 與發行 (Publish) **Release** 版本時，CyberCOLA 系統內各模組（公共程式庫、WPF 模擬器、Web 模擬器、Daemon 服務）所對應與映射的實體檔案目錄。

---

## 1. 方案一般編譯輸出路徑 (Visual Studio Standard Build Output)

當使用 Visual Studio 2026 或透過命令列 `dotnet build -c Release` 進行常規編譯時，產生的 `.dll`、`.exe` 與 `.pdb` 除錯檔會直接輸出到各個專案資料夾下的 **`bin`** 目錄中：

### A. 公共核心程式庫 (Shared Libraries)
- **CommonLib 核心邏輯庫**：
  `src\CyberCOLA\CommonLib\bin\x64\Release\net10.0\`
- **CyberUtils 一般工具庫**：
  `libs\CyberUtils\src\CyberUtils\bin\x64\Release\net10.0\`
- **Yetty 高階通訊框架**：
  `libs\Yetty\src\Yetty\bin\Release\net10.0\`

### B. 授權與閘道核心主機服務 (Auth & Gateway Services)
- **FISC 授權主機控制台** (`CyberAuthFISCConsole`)：
  `src\CyberCOLA\CyberAuthFISCConsole\bin\Release\net10.0\`
- **NCCC 閘道控制台** (`CyberGWNCCCConsole`)：
  `src\CyberCOLA\CyberGWNCCCConsole\bin\Release\net10.0\`

### C. 壓力測試與功能測試模擬器 (Simulators)
- **Web 壓力模擬器** (`WebStressSimulator`)：
  `src\CyberCOLA\WebStressSimulator\bin\x64\Release\net10.0\`
- **WPF 壓力模擬器** (`WpfStressSimulator`)：
  `src\CyberCOLA\WpfStressSimulator\bin\Release\net10.0-windows\` (若設定為 x64 則在 `bin\x64\Release\net10.0-windows\`)

---

## 2. 自動化工具發行輸出路徑 (Automation Publish Output)

透過 `tools/` 目錄下的自動化部署發行腳本進行打包時，二進位檔會經由最佳化編譯（Strip PDB/Optimize IL）並集中映射輸出至根目錄的 **`publish\`** 資料夾下：

### A. 22 個 Web 模擬器部署包
使用指令 `tools/Publish-WebProjectsCrossPlatform.ps1` 進行批次發行時：
- **發行目的根目錄**：`publish\cross-platform\`
- **每個專案對應子資料夾**：
  - `publish\cross-platform\WebStressSimulator\`
  - `publish\cross-platform\WebClientSimulator\`
  - `publish\cross-platform\WebEAISimulator\`
  - ... (涵蓋所有 22 個 Web 專案)
- **網頁資源自動映射**：腳本會自動將原始碼專案下的 `wwwroot`（HTML/CSS/JS 靜態資源）完整複製到發行包下，確保自帶 Kestrel 網頁伺服器在無原始碼環境中直接執行 `.exe` 時能正確顯示前端介面。

### B. 核心背景服務與 Daemon 部署包
使用指令 `tools/Publish-CyberCOLAServices.ps1` 進行一鍵部署發行時：
- **發行目的根目錄**：`publish\<Runtime>\`
  - 若發行 Windows x64 執行檔：**`publish\win-x64\`**
  - 若發行 Linux 跨平台套件：**`publish\linux-x64\`**
- **對應子資料夾**：
  - `publish\win-x64\CyberAuthFISC\` (FISC 授權節點二進位包)
  - `publish\win-x64\CyberGWNCCC\` (NCCC 閘道接收節點二進位包)
  - ... (其他指定發行之核心節點)

### C. 執行版 XML 全域設定檔集散路徑
- **映射輸出目錄**：**`publish\Config\`**
  - 自動化工具會自動將原始碼中 `src\CyberCOLA\Config\` 裡的所有 XML 設定檔（例如 `SystemParameterInfo.xml`、`SMSWebAPIInfo.xml`、`TCPClientServiceInfo.xml` 等）同步複製到此發行配置專屬目錄中。
- **路徑自動重寫（關鍵功能）**：
  發行腳本會自動掃描並更新發行部署包內的 `appsettings.json` 檔案，將其中原本指向開發環境 `E:\FirstBank\...` 的硬編碼絕對路徑，自動替換為相對於發行結構下的相對映射路徑（例如：`..\..\Config\SystemParameterInfo.xml`），確保發行套件能做到「即壓即用、不限路徑」的免安裝便攜特性。
