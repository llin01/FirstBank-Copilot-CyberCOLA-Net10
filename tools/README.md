# CyberCOLA tools/ — PowerShell 工具導覽

`tools/` 內有 41 支 PowerShell script。本文件依「使用情境」分類，協助第一次使用的人挑選合適的工具。
script 之間的呼叫關係圖與各 script 詳細說明請見 [`docs/tools_scripts_guide.md`](../docs/tools_scripts_guide.md)。

**圖示說明**

| 圖示 | 意義 |
|---|---|
| 🔒 | 需以系統管理員身分執行 PowerShell |
| 🌐 | 需連得到資料庫（通常要先連 GlobalProtect VPN） |
| 📦 | 需先發行（publish）二進位檔到 `publish\` |
| ⚠️ | 會停止程序、刪除或覆寫檔案 |

---

## 1. 第一次使用？從這裡開始

```powershell
pwsh tools/COLAMainMenu.ps1
```

- 需要 **PowerShell 7（`pwsh`）**。多數 script 在 Windows PowerShell 5.x 啟動時會自動改用 `pwsh` 重新執行；沒有安裝 `pwsh` 的話請先安裝：<https://aka.ms/powershell>。
- 主選單（Unified Control Center）已涵蓋大部分日常操作，不確定要用哪支 script 時，從主選單進入即可：

| 主選單區塊 | 按鍵 | 功能 |
|---|---|---|
| Core Management & Sub-Panels | `1` | 系統服務管理（Auth / GW）→ `COLAMainMenu-Console.ps1` |
| | `2` | 模擬器管理（WPF / Web）→ `COLASimulatorMainMenu.ps1` |
| | `3` | 整合測試流程（Sim → GW → Auth）→ `COLATestFlowMenu.ps1` |
| | `4` | MaintGateway 管理與測試 → `Start-MaintGatewayTest.ps1` |
| | `5` ⚠️ | 停止所有程序，並刪除所有 `bin`、`obj`、`publish\win-x64`、`publish\linux-x64` 與 `*.log` |
| | `W` | Web 模擬器控制面板 → `StartStop-WebApps.ps1` |
| | `X` | 切換模擬器類型（WPF / Web） |
| One-Click Automation Flows | `6` / `7` | 一鍵啟動 / 停止 NCCC → `Start-NCCC.ps1` / `Stop-NCCC.ps1` |
| | `8` / `9` | 一鍵啟動 / 停止 FISC → `Start-FISC.ps1` / `Stop-FISC.ps1` |
| | `0` ⚠️ | 停止所有服務與模擬器 |
| Diagnostics & Monitoring | `H` | 系統健康診斷 → `Check-SystemHealth.ps1` |
| | `M` | 即時程序與 TCP 監控 → `Monitor-COLAServices.ps1` |
| | `T` / `S` / `D` / `F` | TCP 連線明細 / 模擬器摘要 / `COLA_TRAN_LOG` 收送配對 / log 中的 WARN、ERROR、FATAL（掃描中按 `Q` 中止並回主選單） |
| Development & Maintenance | `P` | 發行所有服務 → `Publish-CyberCOLAServices.ps1` |
| | `K` / `E` / `I` | `SocketConfig.json` 同步至 appsettings / 匯出 CSV / 從 CSV 匯入 |
| | `Y` | 專案相依性視覺化 → `Get-ProjectDependency.ps1` |
| | `O` | 程式碼排版（`dotnet format`）→ `Format-CodeStyle.ps1` |
| | `L` ⚠️ | 清除所有 log 檔 |

> 主選單支援兩碼捷徑直接進入子選單項目，例如 `11` = 子選單 1 的選項 1、`23` = 子選單 2 的選項 3。

**建議的第一次流程**：`H`（健康診斷）→ `P`（發行）→ `6`（啟動 NCCC）→ `M`（監控）→ `7`（停止 NCCC）。

### 網頁版：WebToolsDashboard

不想記指令時，也可以用瀏覽器操作（`src/CyberCOLA/WebToolsDashboard`，方案資料夾 `Test/Web`）：

```powershell
dotnet run --project src/CyberCOLA/WebToolsDashboard/WebToolsDashboard.csproj
# then open http://127.0.0.1:5300
```

- **工具頁**：以表單填參數執行可以自己跑完的工具（授權測試、DB／健康／VPN 檢查、測試資料預覽、診斷），即時顯示輸出。授權測試另外畫出流程（DB → ServiceRouter → GW → HSM → Auth → 模擬器 → 跑流量 → 取統計 → 停止）與統計結果，並和基準比較；同一時間只能跑一個授權測試。
- **互動選單**（主選單、Start-NCCC／FISC 等）只能「在新視窗開啟」，dashboard 拿不到它們的結果。
- **測試歷史頁**：列出 `LogFiles\AuthTest\` 的每次 transcript，點選後看流程、統計與原文。
- 第一版**只提供唯讀操作**：不執行任何 `-Apply`、寫入 DB 的 SQL 或發行；這些仍要自己在 PowerShell 執行（指令見下方各節）。
- 只綁 `127.0.0.1`；輸出中的 13–19 位數字遮成末 4 碼，`password=`／`pwd=` 的值遮成 `***`。
- 工具用 `Write-Host -ForegroundColor` 標的顏色會照樣顯示，例如額度預覽中額度不足的紅色列。
- 右上角「⏻ 關閉」結束 dashboard 程式（確認後才關；有工具在執行時會一起中止）。

---

## 2. 依情境找工具

| 我想要… | 用這支 | 從主選單 |
|---|---|---|
| 不知道該用什麼，先看全部功能 | `COLAMainMenu.ps1` | — |
| 確認環境是否正常（DB、port 衝突、log 權限） | `Check-SystemHealth.ps1` | `H` |
| 測試資料庫連不連得到 | `Test-DatabaseConnection.ps1` | `1` → `V` |
| VPN 沒連上，想自動連 GlobalProtect | `Connect-GlobalProtect.ps1` | —（`Test-DatabaseConnection.ps1` 會自動呼叫） |
| 把服務發行到 `publish\` | `Publish-CyberCOLAServices.ps1` | `P` |
| 看哪些已發行的服務過期、該重新發行哪些 | `Test-CyberCOLAPublishState.ps1` | — |
| 只發行 Web 專案（跨平台） | `Publish-WebProjectsCrossPlatform.ps1` | — |
| 一鍵啟動 / 停止 NCCC 全套 | `Start-NCCC.ps1` / `Stop-NCCC.ps1` | `6` / `7` |
| 一鍵啟動 / 停止 FISC 全套 | `Start-FISC.ps1` / `Stop-FISC.ps1` | `8` / `9` |
| 個別啟停 FISC / NCCC / SVCS / ICASH / IPASS 服務 | `COLAMainMenu-Console.ps1` | `1` |
| 啟動 / 停止模擬器 | `COLASimulatorMainMenu.ps1` | `2` |
| 跑 Sim → GW → Auth 的整合測試 | `COLATestFlowMenu.ps1` | `3` |
| 自動啟動任一網路的授權鏈並接上模擬器（NCCC / FISC / ICASH / IPASS / SVCS） | `Test-AuthorizationSystem.ps1` | — |
| 無人值守跑完一個網路的授權測試並存下統計結果 | `Invoke-AuthTestUnattended.ps1` | — |
| 把歷次授權測試的統計整理成 JSON，給 Claude「Data」的授權測試趨勢 dashboard | `Export-AuthTestDashboardData.ps1` | — |
| 授權測試的卡被拒（卡不存在、未開卡、額度用完）時，從 DB 重選 NCCC / FISC 測試卡 | `New-AuthTestCardPool.ps1` | — |
| 電子票證測試回 RC 14（卡號無效）時，把 ICASH / IPASS / SVCS 範本的 F2 / F14 換成 DB 裡可用的票卡 | `Set-ETicketTestCards.ps1` | — |
| 測試卡額度被壓力測試用完（RC 05、CHECK_CODE 062）時，把測試卡的可用額度補回上限（先用 `sql/Preview-AuthTestCardCredit.sql` 唯讀預覽；⚠️ `Reset` 會寫入 DB，只限測試庫，要用有寫入權限的帳號執行，可在 DataGrip 逐句執行，或用 `Invoke-CyberCOLASql.ps1` 直接執行；可重複執行） | `sql/Preview-AuthTestCardCredit.sql`、`sql/Reset-AuthTestCardCredit.sql` | — |
| ICASH 測試全部被拒（唯一的 ICASH 票卡有管制碼 `C`、票卡已過期）時，讓它可以授權（先用 `sql/Preview-ICashTestCard.sql` 唯讀預覽；⚠️ `Enable` 會寫入 DB，只限測試庫；之後用 `Set-ETicketTestCards.ps1 -Apply` 把 ICASH 範本換成這張卡） | `sql/Preview-ICashTestCard.sql`、`sql/Enable-ICashTestCard.sql` | — |
| 不開 DataGrip，直接從 PowerShell 執行 `tools/sql` 的 SQL（唯讀的直接跑；⚠️ 會寫入 DB 的要加 `-Apply` 並輸入 `YES` 確認，只限測試庫） | `Invoke-CyberCOLASql.ps1` | — |
| 自動啟動 NCCC 授權鏈並跑壓力模擬器（舊版，僅 NCCC） | `Test-NcccAuthorizationSystem.ps1` | — |
| 用 Web 模擬器測 NCCC，並看即時 Dashboard | `Start-NcccWebSimulatorTest.ps1` | — |
| 測 MaintGateway（HTTP / HTTPS / JWT） | `Start-MaintGatewayTest.ps1` | `4` |
| 一次自動測完 MaintGateway 六種模式（不需互動） | `Invoke-MaintGatewayTestUnattended.ps1` | — |
| 看各網路即時交易量（TPS 圖） | `Show-CyberCOLATrafficChart.ps1` | — |
| 即時看程序與 TCP 連線 | `Monitor-COLAServices.ps1` | `M` |
| 從 build / runtime log 找錯誤並連回原始碼 | `Find-LogErrors.ps1` | — |
| 解開一筆 ISO 8583 報文（範本檔或 log 裡的 hex），看 MTI 與各欄位 | `ConvertFrom-Iso8583.ps1` | — |
| 以 Windows Service 方式安裝服務 | `Install-CyberCOLAServices.ps1` 🔒 | — |
| 把 Linux 版發行檔同步到 WSL2 | `Sync-ToWSL.ps1` | — |
| repo clone／搬到別的資料夾後，把設定檔裡寫死的路徑改成新位置 | `Set-CyberCOLARepoRoot.ps1` | — |
| 換電腦：把專案連同 Claude Code 設定、對話紀錄打包搬到另一台 Windows 電腦 | `Move-ClaudeProject.ps1` | — |
| 換電腦：把 Claude Code 的 user scope MCP server 設定搬到另一台電腦 | `Move-ClaudeMcpServers.ps1` | — |

---

## 3. 分類總表

### 3.1 入口選單

| Script | 用途 | 從主選單 | 前置條件 / 注意 |
|---|---|---|---|
| `COLAMainMenu.ps1` | **統一入口**，整合所有子選單、一鍵流程、診斷與開發工具 | — | 選項 `5`、`0`、`L` ⚠️ |
| `COLAMainMenu-Console.ps1` | 以 Console 模式啟停各網路服務（含 Multi-Node N1+N2）、發行、TCP 狀態、DB 測試 | `1` | 📦；啟動服務前會先跑 DB 檢查 🌐 |
| `COLASimulatorMainMenu.ps1` | 啟停各網路模擬器、HSM、SMS、EAI，複製模擬器設定 | `2` | 📦；模擬器類型由 `$env:COLA_SIMULATOR_TYPE` 決定（主選單 `X` 切換） |
| `COLATestFlowMenu.ps1` | 依序啟動 Sim → GW → Auth 的整合測試流程（FISC / NCCC / ICASH / IPASS / SVCS） | `3` | 📦 🌐 |
| `COLAMainMenu-Service.ps1` | 以 **Windows Service** 模式啟停服務；`I` 進入 `Install-CyberCOLAServices.ps1` | — | 🔒；服務需先安裝 |

### 3.2 一鍵啟停

| Script | 用途 | 從主選單 | 前置條件 / 注意 |
|---|---|---|---|
| `Start-NCCC.ps1` | 啟動 NCCC GW / Auth、ServiceRouter、HSM、NCCC 與 QRCode 模擬器、SMS 模擬器、SMS Web API Server；每步會詢問是否執行 | `6` | 📦 🌐 |
| `Stop-NCCC.ps1` | 停止上述 NCCC 相關程序 | `7` | ⚠️ |
| `Start-FISC.ps1` | 啟動 FISC 服務、ServiceRouter、HSM、FISC 與 ID Verify 模擬器、SMS Web API Server；每步會詢問是否執行 | `8` | 📦 🌐 |
| `Stop-FISC.ps1` | 停止上述 FISC 相關程序 | `9` | ⚠️ |
| `StartStop-WebApps.ps1` | Web（Blazor）模擬器控制面板：啟動、停止、建置、監控，並偵測 Hyper-V 保留 port 衝突 | `W` | — |

### 3.3 整合測試

| Script | 用途 | 從主選單 | 前置條件 / 注意 |
|---|---|---|---|
| `Test-AuthorizationSystem.ps1` | 依網路分兩組自動啟動授權鏈：**Group 1（NCCC、FISC，GW 主動連線）** DB → ServiceRouter → GW → HSM → Auth → ConsoleStressSimulator（`--type <NET> --count <SimCount> --interval <SimInterval> --auto-start`，自動送交易，預設每條連線 100 筆）；**Group 2（ICASH、IPASS、SVCS，GW 被動監聽）** DB → ServiceRouter → GW → Auth → WpfETicketClientSimulator（自動連上 GW，從視窗送交易；加 `-AutoSend` 則自己送出全部範本，不需操作視窗）。之後進入狀態選單（`[5]` 統計本次交易成功／失敗筆數與原因，支援 NCCC、FISC、ICASH、IPASS、SVCS；FISC 6P 標示為身分驗證不符的測試資料） | — | 📦 🌐；參數 `-Network`、`-TimeoutSec`、`-SimCount`（Group 1，預設 100）、`-SimInterval`（Group 1，每筆間隔 ms，預設 0）、`-SimReplaceCard`（Group 1，是否以卡片清單替換卡號，預設 $true）、`-SimIncludeChip`（Group 1，換卡時仍送晶片範本，預期有 102／232 拒絕；要走到 HSM 的 ARQC 驗證 Func07，另需 `SystemParameterInfo.xml` 的 `HSMCheckFlags` 將該網路的 `IsARQCCheck` 設為 `Y`）、`-SessionCount`（Group 2）、`-AutoSend`（Group 2，連線後自動送出全部範本，0302 除外）、`-RepeatCount`（Group 2，預設 10）、`-IntervalMs`（Group 2，每筆間隔 ms，預設 100）；已在執行的程序會沿用、不會被停止；離開時 ⚠️ 停止本次啟動的程序；⚠️ Group 2 會以對應網路設定覆寫模擬器資料夾的 `appsettings.json` |
| `Invoke-AuthTestUnattended.ps1` | 不需人操作地執行 `Test-AuthorizationSystem.ps1`：Group 1 先唯讀檢查卡片清單的可用額度，不足時警告（不中止測試；檢查失敗時印出遮罩過的原因）→ 等授權鏈就緒（Group 2 以 `-AutoSend` 啟動，WpfETicketClientSimulator 自己送出全部範本）→ 讓流量跑一段時間 → 選 `[5]` 取統計 → `[9]` 停止全部，整段輸出存成文字檔 | — | 📦 🌐；Group 2 的模擬器是 WPF 視窗，需要已登入的桌面工作階段；參數 `-Network`（必要）、`-TrafficWaitSec`（預設 90）、`-SimCount`（Group 1，每條連線送幾筆，預設 100；範本依檔名順序送出，FISC 有 308 個範本，要全部送到需 ≥ 308）、`-SimInterval`（預設 50）、`-SimReplaceCard`、`-SimIncludeChip`、`-RepeatCount`（預設 10）、`-IntervalMs`（預設 100）（Group 2，傳給 `-AutoSend`）、`-MinCreditAvail`（Group 1，額度警告門檻，預設 10000）、`-OutFile`（預設 `LogFiles\AuthTest\<NET>_<時間>.txt`）、`-StreamOutput`（測試輸出也即時印到主控台，WebToolsDashboard 用它即時顯示流程）；exit code `0` 成功、`1` 授權鏈未就緒、`2` 沒有統計、`3` 測試腳本被強制結束 |
| `Export-AuthTestDashboardData.ps1` | 讀 `LogFiles\AuthTest` 中有統計的 transcript，寫出 `runs.json`（每輪請求／成功／失敗／未回應、成功率、流量參數、是否為冒煙測試）與 `rc.json`（每輪各回應碼筆數），格式為 `{rows, meta}`，`meta` 附本腳本全文；上傳到 Claude「Data」dashboard 當附加檔 | — | 不依賴 `CyberCOLA-Common.ps1`；支援 Windows PowerShell 5.1；參數 `-TranscriptDir`（預設 `LogFiles\AuthTest`）、`-OutDir`（預設 `LogFiles\AuthTest\dashboard`）；只輸出彙總數字，含 13–19 位數字或 IPv4 時拒絕寫出；dashboard 讀者看得到附加檔全文；exit code `0` 已寫出、`1` 沒有可匯出的資料、`2` 拒絕寫出 |
| `New-AuthTestCardPool.ps1` | 唯讀查詢 DB，挑出授權 stored procedure 在卡片層級會接受的卡（有卡/帳戶/客戶資料、無管制碼、已開卡、未過期、非 VD、BU 001/007、可用額度足夠、卡片與帳戶在 `COLA_AUPALIM` 沒有國內一般消費的限額／限次控管），寫成 ConsoleStressSimulator 的卡號池（`Config\Nccc` 與 `Config\Fisc` 的 `CardXmlFilePath.xml`，NCCC 與 FISC 共用）；卡號只顯示末 4 碼 | — | 🌐；預設只列出、不寫檔；`-Apply` ⚠️ 覆寫 `src` 與 `publish\win-x64` 的卡號池；`-MinCreditAvail`（預設 10000，比對 `CARD_LIMIT`／`CUST_LIMIT` 目前的可用額度）；exit code `0` 完成、`1` DB 連不到、`2` 沒有合格的卡 |
| `Invoke-CyberCOLASql.ps1` | 從 PowerShell 執行 `tools/sql` 下的 SQL（不帶參數時列出各檔並標示唯讀／會寫入）。唯讀的 SQL 以 `ApplicationIntent=ReadOnly` 執行；會寫入 DB 的（含 INSERT / UPDATE / DELETE / MERGE）要加 `-Apply`，顯示伺服器與資料庫後輸入 `YES` 才執行，所有語句在同一個交易中，任一句失敗就 rollback；結果中 13–19 碼的數字只顯示末 4 碼；結果集的 `highlight` 欄為 1 的列以紅色顯示（例如 `Preview-AuthTestCardCredit` 標出可用額度低於 10000 的卡） | — | 🌐；⚠️ `-Apply` 會寫入 DB，只限測試庫；參數 `-Name`（第一個參數，檔名不含 `.sql`）、`-Apply`、`-TimeoutSec`（預設 120）；exit code `0` 完成、`1` DB 連不到、`2` 找不到 SQL 檔、`3` 寫入用的 SQL 沒加 `-Apply` 或未確認（沒有執行）、`4` SQL 錯誤（已 rollback） |
| `Set-ETicketTestCards.ps1` | 唯讀查詢 DB 的 `ESVC_CARD`，挑出授權會接受的電子票卡（票卡與關聯信用卡都無管制碼、未過期、信用卡已開卡且非 VD；SVCS 對應 `ESVC_TYPE` TSCC），把 WpfETicketClientSimulator 範本的 F2 換成票卡號、F14 換成票卡效期；範本現用的卡仍合格時保留不換；卡號只顯示末 4 碼 | — | 🌐；需先發布 WpfETicketClientSimulator（用它的 `TM.dll` 解碼範本）；預設只列出、不寫檔；`-Apply` ⚠️ 覆寫 `src` 與 `publish\Config` 兩份範本；無法原樣重新編碼的範本不會被寫入；exit code `0` 完成、`1` DB 或模擬器執行檔不可用、`2` 有網路沒有合格的票卡（ICASH 目前即是） |
| `Test-NcccAuthorizationSystem.ps1` | 依序檢查 DB → 啟動 ServiceRouter → CyberAuthNCCC → CyberGWNCCC → ConsoleStressSimulator（`--auto-start`），之後進入監控選單 | — | 📦 🌐；離開時（選項 `9`）⚠️ 停止所有已啟動服務 |
| `Start-NcccWebSimulatorTest.ps1` | NCCC + WebStressSimulator 測試：啟動 / 停止 / Socket 列表 / Dashboard / 交易量圖 | — | 📦 🌐；參數 `-Action Start\|Stop\|Sockets\|Dashboard\|Chart`、`-TimeoutSec`、`-DurationSec`、`-RefreshSec` |
| `Start-MaintGatewayTest.ps1` | MaintGateway HTTP / HTTPS / JWT 模式的完整流程（Sim → GW → Tester） | `4` | 📦 |
| `Invoke-MaintGatewayTestUnattended.ps1` | 不需互動依序測 MaintGateway 六種模式（HTTP / HTTPS / JWT 與 Unified 三種），每種啟動 NCCC 模擬器 → GW → Tester，打 `/api/test/send-xml`，並從模擬器 log 確認收到 0420、送出 0430 | — | 📦；參數 `-Mode`（`All` 或逗號分隔）、`-StartTimeoutSec`；會停掉所有 MaintGateway／Tester／`WpfStressSimulator`；exit code `0` 全部通過、`1` 沒有執行（缺發行檔）、`2` 有模式失敗 |

### 3.4 監控與診斷

| Script | 用途 | 從主選單 | 前置條件 / 注意 |
|---|---|---|---|
| `Check-SystemHealth.ps1` | 健康診斷：DB 連線、Kestrel port 衝突、log 目錄寫入權限 | `H` | — |
| `Test-DatabaseConnection.ps1` | 解密 `SystemParameterInfo.xml` 的連線字串並測試 DB TCP 連線；連不到時會呼叫 `Connect-GlobalProtect.ps1` | `1` → `V` | 🌐；exit code 非 0 表示失敗 |
| `Connect-GlobalProtect.ps1` | 自動點擊 GlobalProtect「Connect」並重試，再檢查 DB 連線；按鈕不是「Connect／連線」時只等待不點擊；VPN 已連線但 DB 不通時先中斷再重連 | — | 需已儲存 VPN 帳密且不需 MFA；參數 `-Hosts`、`-Port`、`-ConnectTimeoutMs`（DB 連線埠逾時，預設 3000 ms）、`-TimeoutSeconds`、`-MaxAttempts`、`-RetryDelaySeconds`、`-SkipDbCheck`；exit code `0` 成功、`1` VPN 未連、`2` DB 不通 |
| `Monitor-COLAServices.ps1` | 即時顯示服務程序狀態與 TCP 連線 | `M` | 參數 `-IntervalSeconds`（預設 2） |
| `Show-CyberCOLATrafficChart.ps1` | 從 GW log 統計各網路、各 socket port 的即時交易量（TPS 長條圖 + sparkline） | — | 參數 `-Network NCCC,FISC,...`、`-RefreshSec`、`-DurationSec`、`-History`；只計算開啟後寫入的 log |
| `Find-LogErrors.ps1` | 從 build / runtime log 找出錯誤與 stack trace，輸出可點擊的原始碼連結 | — | 參數 `-Path <log>`；未指定時自動找最新的 log |
| `ConvertFrom-Iso8583.ps1` | 用發行版的 `TM.dll` 解出 ISO 8583 報文的表頭、MTI 與 F1～F127 各欄位的值（FISC／NCCC／NCCCQRCode／TWIN／ICASH／IPASS／SVCS；不拆子欄位），約 2 秒；偵測並核對 2 位元組長度前綴，多筆時逐筆解碼；卡號、Track、PIN 一律遮罩，內含 CVV2 的欄位（FISC F48；NCCC／TWIN F62、F63）整欄隱藏 | — | 📦（需要 ConsoleStressSimulator 或 WpfETicketClientSimulator 的發行版）；參數 `-Network`（必要）、`-Path` 或 `-Hex`、`-LengthPrefix Auto\|Yes\|No`、`-TmDir`；exit code `0` 成功、`1` 輸入錯誤或找不到 `TM.dll`、`2` 解碼失敗 |

### 3.5 發行與部署

| Script | 用途 | 從主選單 | 前置條件 / 注意 |
|---|---|---|---|
| `Publish-CyberCOLAServices.ps1` | 發行核心服務到 `publish\win-x64` / `publish\linux-x64`，複製 Config 並重寫 NLog / XML 路徑 | `P`、`1` → `C` / `P` | ⚠️ 覆寫輸出目錄；`src/` 或 `libs/` 有未 commit 的修改時會列出並詢問是否繼續（預設取消），因為發行檔只記錄 commit、追不回未 commit 的內容；參數 `-SingleProjectName`（多個用逗號分隔：`A,B`，以空白分隔會報錯）、`-TargetRuntime`、`-LinuxDeployBasePath`（須為 `/` 開頭的絕對路徑）、`-SqlColumnEncryption Enabled\|Disabled`（DB 服務的 `sql-column-encryption-setting`；省略時保留上次發行的值） |
| `Test-CyberCOLAPublishState.ps1` | 唯讀比對 `publish\<runtime>` 每個服務主 DLL 蓋的發行 commit 與 HEAD（含遞迴的 `ProjectReference`、build props），分 `stale-code`／`stale-shared`／`stale-config`／`never published`，依授權測試相關與其他分組，並產生發行指令；約 10 秒 | — | 參數 `-Runtime win-x64|linux-x64`、`-Exclude`（刻意保留的服務，例如 `WpfHSMSimulator`）、`-Detail`；exit code `0` 全部最新、`1` 沒有執行、`2` 有過期或從沒發行的服務 |
| `Publish-WebProjectsCrossPlatform.ps1` | 批次發行所有 `Web*` 專案與 ConsoleStressSimulator 到 `publish\cross-platform` | — | 參數 `-CustomOutputDir` |
| `Enforce-PublishProfilesCRLF.ps1` | 重寫所有 Web 專案的發行設定檔（`FolderProfile.pubxml`），統一 CRLF 與 x64 設定 | — | ⚠️ 覆寫 `.pubxml` 與 `.pubxml.user` |
| `Sync-ToWSL.ps1` | 將 `publish\linux-x64` 與 `publish\Config` 同步到 WSL2（`Ubuntu-24.04:/opt/CyberCOLA`） | — | 📦（linux-x64）；⚠️ `robocopy /MIR` 會刪除目的地多餘檔案 |
| `Set-CyberCOLARepoRoot.ps1` | repo clone／搬家後，把 `src/`、`publish/Config/` 設定檔（json／config／xml）裡寫死的 repo 路徑改成新位置；`E:\`、JSON 的 `E:\\`、NLog 的 `E:/` 三種寫法都處理，保留 BOM 與換行 | — | 預設只列出（dry run）；`-Apply` ⚠️ 寫入；`-IncludePublished` 一併改 `publish\win-x64`；`-OldRoot`（預設從設定檔讀出）、`-NewRoot`（預設為這份 clone）；改動只適用這份 clone，不要 commit；只支援 Windows；exit code `0` 完成／不需修改、`1` 沒有執行。發行腳本即使沒先執行它也會辨認舊路徑 |
| `Move-ClaudeProject.ps1` | 舊電腦匯出、新電腦匯入：專案（含 `.git`）＋使用者層級 `.claude` 設定＋本專案對話紀錄與 memory 打成一個 zip，並把本腳本複製到 zip 旁；排除 `bin`／`obj`、`publish\win-x64`／`linux-x64`／`cross-platform` 和 `LogFiles`，不含登入憑證；匯入到不同路徑時改寫對話紀錄內的路徑 | — | 不依賴 `CyberCOLA-Common.ps1`，可單獨複製；支援 Windows PowerShell 5.1；參數 `-Mode Export\|Import`（省略＝互動選單）、`-OutputDir`（預設 `E:\Export`）、`-ZipPath`、`-TargetPath`、`-Force`（⚠️ 覆蓋前先備份）；zip 含對話紀錄，請當敏感資料保管；exit code `0` 完成、`1` 失敗 |
| `Move-ClaudeMcpServers.ps1` | 舊電腦匯出、新電腦匯入 Claude Code 的 user scope MCP server（`.claude.json` 的 `mcpServers`，`Move-ClaudeProject.ps1` 不搬這部分）；匯出時警告 `headers`／`env` 中可能的明文 secret（只列名稱），匯入後檢查引用的 `${變數}` 與 Node.js | — | 不依賴 `CyberCOLA-Common.ps1`；支援 Windows PowerShell 5.1；參數 `-Mode Export\|Import`（必要）、`-Path`（預設 `E:\Export\claude-mcp-servers_<時間>.json`，Import 必要）、`-Force`（⚠️ 取代同名 server）；環境變數要在新電腦另外設定；exit code `0` 完成、`1` 沒有執行、`2` 有 server 加入失敗 |
| `Install-CyberCOLAServices.ps1` | 安裝 / 解除安裝 / 查詢 CyberCOLA Windows Service | — | 🔒 📦（win-x64） |

### 3.6 開發輔助

| Script | 用途 | 從主選單 | 前置條件 / 注意 |
|---|---|---|---|
| `Format-CodeStyle.ps1` | 對 `CyberCOLA.sln` 執行 `dotnet format` | `O` | ⚠️ 會直接修改原始碼 |
| `Get-ProjectDependency.ps1` | 掃描所有 `.csproj` 的專案參考，輸出樹狀圖與 Mermaid 圖 | `Y` | — |
| `Verify-Scripts.ps1` | 檢查 `tools/` 內所有 `.ps1` 的語法 | — | 修改任何 script 後建議執行 |
| `CyberCOLA-Common.Tests.ps1` | `CyberCOLA-Common.ps1` 的 Pester 單元測試 | — | 執行：`Invoke-Pester tools/CyberCOLA-Common.Tests.ps1` |

### 3.7 共用庫（勿直接執行）

| Script | 用途 |
|---|---|
| `CyberCOLA-Common.ps1` | 所有工具共用的函式（路徑、程序管理、快速 TCP 表、連線字串解密、設定檔讀寫等）。由其他 script 以 `. "$PSScriptRoot\CyberCOLA-Common.ps1"` 載入，單獨執行不會有任何作用。 |

---

## 4. 不在任何選單中、需直接執行的工具

以下 script（含 `Test-AuthorizationSystem.ps1`）沒有被主選單或其他 script 呼叫，要在 `pwsh` 中直接執行（範例皆從 repo 根目錄執行）：

```powershell
# Windows Service mode menu (run PowerShell as Administrator)
pwsh tools/COLAMainMenu-Service.ps1

# Authorization chain for any network (menu, or -Network NCCC|FISC|ICASH|IPASS|SVCS)
pwsh tools/Test-AuthorizationSystem.ps1
pwsh tools/Test-AuthorizationSystem.ps1 -Network FISC

# Same test without a person at the keyboard; prints the transcript path
pwsh tools/Invoke-AuthTestUnattended.ps1 -Network NCCC
pwsh tools/Invoke-AuthTestUnattended.ps1 -Network IPASS -RepeatCount 10

# Turn the transcripts into runs.json / rc.json for the auth test trend dashboard
pwsh tools/Export-AuthTestDashboardData.ps1

# Pick NCCC/FISC test cards from the DB (read-only); -Apply writes the card pool
pwsh tools/New-AuthTestCardPool.ps1
pwsh tools/New-AuthTestCardPool.ps1 -Apply

# Point the e-ticket templates (F2/F14) at a usable ESVC_CARD (read-only DB); -Apply writes them
pwsh tools/Set-ETicketTestCards.ps1
pwsh tools/Set-ETicketTestCards.ps1 -Apply

# Restore the test cards' credit (SQL, TEST DB ONLY, in DataGrip or with Invoke-CyberCOLASql.ps1):
# preview first, then reset, then preview again (all counts 0)
pwsh tools/Invoke-CyberCOLASql.ps1 Preview-AuthTestCardCredit
pwsh tools/Invoke-CyberCOLASql.ps1 Reset-AuthTestCardCredit -Apply

# Make the ICASH e-ticket card usable (SQL, TEST DB ONLY), then point the ICASH templates at it
pwsh tools/Invoke-CyberCOLASql.ps1 Preview-ICashTestCard
pwsh tools/Invoke-CyberCOLASql.ps1 Enable-ICashTestCard -Apply

# List the SQL scripts in tools/sql and whether each one writes
pwsh tools/Invoke-CyberCOLASql.ps1

# NCCC-only predecessor
pwsh tools/Test-NcccAuthorizationSystem.ps1

# NCCC + WebStressSimulator test (interactive menu, or use -Action directly)
pwsh tools/Start-NcccWebSimulatorTest.ps1
pwsh tools/Start-NcccWebSimulatorTest.ps1 -Action Start -TimeoutSec 60

# Live transaction chart
pwsh tools/Show-CyberCOLATrafficChart.ps1 -Network NCCC

# Locate errors in the most recent log (or pass -Path)
pwsh tools/Find-LogErrors.ps1

# Decode an ISO 8583 message from a template file or from hex copied out of a log
pwsh tools/ConvertFrom-Iso8583.ps1 -Network FISC -Path src/CyberCOLA/ConsoleStressSimulator/Data/Fisc/0100#48.dat
pwsh tools/ConvertFrom-Iso8583.ps1 -Network NCCC -Hex '49534F0260...'

# Publish Web* projects cross-platform
pwsh tools/Publish-WebProjectsCrossPlatform.ps1

# Rewrite Web publish profiles (CRLF / x64)
pwsh tools/Enforce-PublishProfilesCRLF.ps1

# After cloning or moving the repository: point the config files at the new folder (dry run, then apply)
pwsh tools/Set-CyberCOLARepoRoot.ps1
pwsh tools/Set-CyberCOLARepoRoot.ps1 -Apply -IncludePublished

# Syntax check for every script in tools/
pwsh tools/Verify-Scripts.ps1

# Unit tests for the shared library
Invoke-Pester tools/CyberCOLA-Common.Tests.ps1
```

---

## 5. 延伸閱讀

- [`docs/tools_scripts_guide.md`](../docs/tools_scripts_guide.md)：script 之間的呼叫關係與各 script 詳細說明
- [`docs/TOOLS_ARCHITECTURE.md`](../docs/TOOLS_ARCHITECTURE.md)：發行（Publish）資料流程圖
- [`docs/RELEASE_MAPPING_GUIDE.md`](../docs/RELEASE_MAPPING_GUIDE.md)：各類專案的 Release build / publish 輸出路徑
- [`docs/MAINT_GATEWAY_TESTER_FLOW.md`](../docs/MAINT_GATEWAY_TESTER_FLOW.md)：MaintGateway 測試流程
