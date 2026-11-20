---
name: cybercola-auth-test
description: Run the CyberCOLA authorization test flow for NCCC, FISC, SVCS, ICASH and IPASS end to end, then report success/failure counts and the reason for each response code. Use when asked to test, verify or re-test an authorization flow ("測 NCCC", "test fisc auth flow", "統計成功與失敗") or after changing GW/Auth/simulator code.
---

# CyberCOLA 授權流程測試

用 `tools/Invoke-AuthTestUnattended.ps1` 無人值守地跑 `tools/Test-AuthorizationSystem.ps1`，再補充分析 log，產出一致格式的報告。回覆一律繁體中文。

**參數**：要測的網路（`NCCC`、`FISC`、`SVCS`、`ICASH`、`IPASS`，可多個）。沒指定就依序全測：NCCC → FISC → SVCS → ICASH → IPASS。使用者指定的 `-SimInterval`、`-SimReplaceCard`、`-TrafficWaitSec`、`-RepeatCount` 照傳。

| 網路 | Group | GW 角色 | 送交易的模擬器 |
|---|---|---|---|
| NCCC、FISC | 1 | 主動連線 | ConsoleStressSimulator，自動送 |
| ICASH、IPASS、SVCS | 2 | 被動監聽 | WpfETicketClientSimulator，以 `-AutoSend` 啟動，連線後自己送出全部範本 |

## 1. 前置檢查（每次開跑前）

```powershell
Get-Process CyberGW*,CyberAuth*,ServiceRouter,WpfETicketClientSimulator,ConsoleStressSimulator,WpfHSMSimulator -EA SilentlyContinue | Select Name,Id
Get-NetAdapter | ? InterfaceDescription -match PANGP | Select Name,Status
'DBServer', 'RD-DB' | % { "$_ 1433: " + (Test-NetConnection $_ -Port 1433 -WarningAction SilentlyContinue -InformationLevel Quiet) }   # both hosts, as Connect-GlobalProtect.ps1 checks
```

- **有殘留程序**：列出來問使用者，不要自己殺（可能是使用者手動開的）。
- **VPN 不是 Up 或 DB 連不上**：測試腳本會自己呼叫 `Connect-GlobalProtect.ps1` 重試；但如果輸出出現「MFA or sign-in may be required」或連續失敗，**停止並請使用者手動連 GlobalProtect**，不要反覆重跑。
- **發行版是否過期**：測試優先使用 `publish/win-x64/<服務>/`（`publish/cross-platform` 只是後備）。

  **不要用檔案修改時間判斷**：git 切換分支或合併時會改寫檔案時間，造成誤報。也**不能比對 DLL 雜湊值**：SDK 會把 commit hash 寫進版本資訊，每次 commit 後所有 DLL 都不同。正確做法是讀出 DLL 版本資訊裡的發行 commit（`1.0.0+<sha>`），再用 git 比對從那個 commit 到現在，相關原始碼有沒有變更：

  ```powershell
  Set-Location 'E:\FirstBank\Copilot\CyberCOLA_Net10'
  $head = git rev-parse HEAD
  foreach ($svc in 'CyberGW<NET>','CyberAuth<NET>','<模擬器>','ServiceRouter') {
    $dll = "publish\win-x64\$svc\$svc.dll"
    if (-not (Test-Path $dll)) { "$svc : MISSING"; continue }
    $sha = ([Diagnostics.FileVersionInfo]::GetVersionInfo((Resolve-Path $dll)).ProductVersion -split '\+')[1]
    $paths = "src/CyberCOLA/$svc", 'src/CyberCOLA/ServiceProvider', 'src/CyberCOLA/CommonLib', 'src/CyberCOLA/Interchange', 'libs', 'Directory.Packages.props', 'Directory.Build.props'
    $changed = @(git diff --name-only $sha $head -- $paths) + @(git status --porcelain -- $paths | % { $_.Substring(3) }) | ? { $_ -and $_ -notmatch '\.(md|txt)$' -and $_ -notmatch '\.Test/' } | Select -Unique
    "{0,-26} published {1:MM-dd HH:mm} at {2}  changed since: {3}" -f $svc, (Get-Item $dll).LastWriteTime, $sha.Substring(0,7), $(if ($changed) { "$($changed.Count) file(s)" } else { 'none (up to date)' })
    $changed | Select -First 10 | % { "    $_" }
  }
  ```
  模擬器：Group 1 是 `ConsoleStressSimulator`，Group 2 是 `WpfETicketClientSimulator`；Group 1 另有 `WpfHSMSimulator`。

  - `changed since: none` → 最新，直接測。
  - 有變更檔案 → **列出服務、發行時間、變更檔案和發行指令，先問使用者，不要自動發行**。要附註：「如果發行時工作目錄已經包含這些修改（尚未 commit 就發行），就不算過期」，因為 commit hash 看不出未 commit 的修改。
  ```
  dotnet publish src/CyberCOLA/<Service>/<Service>.csproj -c Release -r win-x64 --self-contained false -o publish/win-x64/<Service>
  ```

## 2. 執行（一次一個網路，前景執行，timeout 600000）

```powershell
pwsh -NoProfile -File tools\Invoke-AuthTestUnattended.ps1 -Network <NET>
```

- 傳 `[bool]` 參數（例如 `-SimReplaceCard:$false`）要改用 `-Command`，用 `-File` 會因為型別轉換而失敗：`pwsh -NoProfile -Command "& .\tools\Invoke-AuthTestUnattended.ps1 -Network FISC -SimReplaceCard:`$false"`。transcript 的 `[Step 6/6]` 那行會顯示 `replace card False`，可以確認參數有傳進去。
- 輸出最後一行是 transcript 路徑（預設 `LogFiles\AuthTest\<NET>_<yyyyMMdd_HHmmss>.txt`）。transcript 第一行 `[Unattended HH:mm:ss] Run start yyyy-MM-dd HH:mm:ss` 是本次的起始時間，log 分析以它為下限。
- Exit code：`0` 成功、`1` 授權鏈沒到 Control Menu（環境問題，看 transcript 的 `[Step n/…]` 與 `[ERROR]`）、`2` 沒有印出統計、`3` 測試腳本被強制結束。非 0 就照實回報原因，**不能當成測試結果**。
- Group 2 的 transcript 有 `Launching WpfETicketClientSimulator ... SEND <Repeat> <IntervalMs>`；實際送出筆數看模擬器 log `LogFiles\...\Process.log` 的 `AutoSend: <n> message(s) ... on <s> session(s)`（請求數 = n × s）。沒連上 GW 時會記 `AutoSend: no channel connected ... nothing sent`。
- 擷取統計（FISC 的格式不同：沒有 `Requests from simulator`，改用 `Messages seen by GW`、`Responses produced`、`No answer from Auth` 等，也沒有依 MTI 的表）：

  ```powershell
  Select-String -Path <transcript> -Pattern '^\[Unattended|Requests from|Responses to|Unanswered|Messages seen by GW|Responses produced|Delivered to simulator|Dropped|No answer from Auth|Not decodable|Duplicate|^\s+Success\s|^\s+Failed\s|^\s+[0-9A-Z]{2}\s+\d+\s+[\d\.]+%|^\s+0\d{3}\s+\d+\s+\d+\s+\d+' | % { $_.Line.Trim() }
  ```
  FISC 的「未回應」看 `No answer from Auth` 加上 `Dropped (no return channel)`。

## 3. 補充 log 分析（必須在跑下一個網路之前做）

服務每次啟動都會**重建** log 檔，下一次測試會蓋掉這次的紀錄。

```powershell
$root = 'E:\FirstBank\Copilot\CyberCOLA_Net10'; $net = '<NET>'; $since = '[yyyy-MM-dd HH:mm:ss'   # transcript 的 Run start
$day = $since.Substring(1,10)
$read = { param($dir) Get-ChildItem "$root\LogFiles\$net\$dir\$day" -Filter *.log -EA SilentlyContinue | % { [IO.File]::ReadLines($_.FullName) } | ? { $_ -ge $since } }
$gw = @(& $read "CyberGW$net"); $auth = @(& $read "CyberAuth$net")
"GW no free COLA channel (RC 96): " + @($gw | ? { $_.Contains("Can't find free Cola channel") }).Count
"GW timed out after waiting     : " + @($gw | ? { $_.Contains('No free Cola channel after waiting') }).Count
"GW no online COLA channel      : " + @($gw | ? { $_.Contains('No online Cola channel') }).Count
$auth | ? { $_ -match 'Message Type No Match : (\d{4})' } | % { $Matches[1] } | Group | % { "Auth No Match MTI $($_.Name): $($_.Count)" }
$auth | Select -Unique | % { if ($_ -match 'Message Type (\d{4}), Return Data Is Null') { $Matches[1] } } | Group | % { "Auth flow null response MTI $($_.Name): $($_.Count)" }
$auth | Select -Unique | % { if ($_ -match 'Call (\w+) HSM Return Data Is Null') { $Matches[1] } } | Group | % { "HSM null reply ($($_.Name)): $($_.Count)" }
"Auth 0620 not classified       : " + @($auth | ? { $_.Contains('0620 not classified') } | Select -Unique).Count
$auth | ? { $_ -match '\]\[ERROR\]' } | % { ($_ -replace '^\[[^\]]*\]\[ERROR\]\[[^\]]*\]','' -replace '[0-9]{6,}','<n>') } | % { $_.Substring(0,[Math]::Min(120,$_.Length)) } | Group | Sort Count -Desc | Select -First 5 | % { "Auth ERROR x$($_.Count): $($_.Name)" }
```

同一行可能同時寫進主 log 和 `_Error`/`_Fatal` log，計數用 `Select -Unique` 或只看單一檔案時要注意重複。

## 4. 判讀

| 現象 | 判讀 |
|---|---|
| RC **05** / **01** / FISC **5C**、**6P** | 授權 stored procedure（`NCCC_0200AuthProcess`、`FISC_0100AuthProcess`）依規則拒絕，不是系統故障；原因看 `CHECK_CODE`（下表），2026-10-28 換卡後主要剩 062 額度、072 PVV、102 ARQC、252/262 紅利/分期——都是範本或測試資料限制 |
| FISC **6P**，`CHECK_CODE` 012、RSN **63** | 身分驗證不符（`FnNcccActVfy`）：範本 P58 Tag 68 = 01／03 時，Auth 比對範本帶的 ID、生日、手機與持卡人，換卡後必定不符。**自 `6fc2ab1` 起，ConsoleStressSimulator 換卡時會跳過這類範本**（資料來源列會顯示 `skipped N identity-verification ...`），正常不應再出現。若又出現：確認 `publish\win-x64\ConsoleStressSimulator` 是 `6fc2ab1` 之後的版本、`-SimReplaceCard` 為 true；不換卡時出現則是範本原卡的資料問題。統計畫面把 6P 標成「identity verification mismatch (test data)」 |
| RC **14**（e-ticket） | `ESVC_CARD.ESVC_NBR` 查不到範本 F2 的票卡號（`ESVC_0100AuthProcess` reason 19）。先跑 `pwsh tools/Set-ETicketTestCards.ps1` 檢查範本的票卡。ICASH 唯一的票卡（****0102）若又被設回管制碼或過期，請使用者執行 `tools/sql/Enable-ICashTestCard.sql`（會寫入 DB，**不可自行執行**）；ICASH 交易也會檢查關聯信用卡 ****0422 的額度，不足時回 RC 51，用 `Reset-AuthTestCardCredit.sql` 補回 |
| RC **96** + GW「no free COLA channel」 | Auth 連線全忙且等滿 `cola-service-idle-channel-wait-ms`；先查 Auth log 有沒有 `MQRC_HOST_NOT_AVAILABLE`（下一列），沒有才調 `cola-service-sessions`（Auth）或等待上限（GW），見 `docs/AUTH_TIMEOUT_REFERENCE.md` |
| Auth FATAL `MQMessageSendReceive ... MQRC_HOST_NOT_AVAILABLE`，伴隨 RC 96 與 No answer from Auth | 卡號池混入 VD（Visa Debit，`COLA_PACARDF.DEBIT_SW='Y'`）卡：圈存走 IBM MQ，測試網路連不到，每筆卡住 14–21 s 佔住 channel。把 VD 卡移出卡號池 |
| RC **96** + Auth「No Match : MTI」 | 該 MTI 不該由網路端送入（例：0302 是銀行端主動發出的檔案維護），檢查範本 |
| RC **96** + Auth「Message Type nnnn, Return Data Is Null」 | Auth 處理該交易失敗，追同一個 STAN／RelationId 的 GW 與 Auth log |
| 「Call CVV2 HSM Return Data Is Null」等 HSM 訊息 | HSM（本機是 WpfHSMSimulator）沒有回該項檢查的結果；INFO 等級，交易仍會回覆。回報次數，但不要和交易失敗混在一起。這個環境 `HSMCheckFlags` 全部是 N（`src/CyberCOLA/Config/SystemParameterInfo.xml`，不可修改），正常只有 NCCC token 開卡會呼叫 HSM；其他交易出現 HSM 呼叫，代表開關被改過 |
| **Unanswered > 0** | 真的漏回，最嚴重；追 RelationId 在 GW → COLA → Auth 的流向 |
| transcript 的 RC 05 比 `COLA_TX_AUTH_TODAY` 多（FISC 最常見） | SP 執行到一半出錯：`CATCH` rollback、改寫 `COLA_TXN_ERROR`、回 RC 05，所以 DB 沒有授權紀錄。查 `COLA_TXN_ERROR`（下方對帳），`ERROR_TYPE` 會寫出失敗的 SP 行號和 SQL 錯誤。2026-11-09 的 `COUNTRY_CODE` NULL（F43 結尾空白）就是這樣找到的，已修在測試庫，見 `docs/FISC_SP_COUNTRY_CODE_NULL_FIX_20261109.md` |
| 0 筆請求、或沒到 Control Menu | 環境問題（VPN、DB、發行版），重跑前先修環境 |
| Auth ERROR `SendSMSMessageToWebAPINoCert` 逾時 | 本機沒有 SMS 服務，可忽略 |

### 拒絕原因（需使用者同意唯讀查詢 DB）

SP 把每筆結果寫進 `dbo.COLA_TX_AUTH_TODAY`：`RESP`（A 核准 / D 拒絕 / R 轉介，舊報告的「reason D」就是這個）、`RESP_CODE`（#39）、`CHECK_CODE`（哪一道檢核）、`RSN_CODE`。`MESS_FORMAT`：N=NCCC、F=FISC、S=e-ticket。DB 時鐘比本機慢約 1 分鐘，時間下限要往前抓。只用 `ApplicationIntent=ReadOnly` + SELECT，卡號只輸出末 4 碼。

```sql
select MESS_TYPE, RESP, RESP_CODE, CHECK_CODE, RSN_CODE, ENTRY_MODE, PROCESS_CODE, count(*) n, count(distinct CARD_NBR) cards
from dbo.COLA_TX_AUTH_TODAY with(nolock)
where RECEIVE_DT >= '<run start - 2 min>' and MESS_FORMAT = 'F'
group by MESS_TYPE, RESP, RESP_CODE, CHECK_CODE, RSN_CODE, ENTRY_MODE, PROCESS_CODE order by n desc
```

**對帳（FISC 必做，其他網路在數字對不上時做）**：DB 的筆數要等於 transcript 的請求數（FISC 看 `Messages seen by GW`），RC 05 筆數要等於 transcript 的 RC 05。FISC 的 transcript 沒有依 MTI 的表，可以改用 GW log 的 `FiscColaSession][CHANNELREAD ... MTI[0110] ... ResponseCode[05]` 計數。少掉的筆數查 `COLA_TXN_ERROR`。`ERROR_TYPE` 開頭是 `0100FISC Fail Row:` 或 `0100FiscHce Fail Row:`，行號是對應 SP 裡出錯的那一行，可以用 `sp_helptext` 唯讀讀出 SP 來對照：

```sql
select ProcessCde, ERROR_TYPE, count(*) n, count(distinct CARD_NBR) cards, min(RECEIVE_DTE) first_dt, max(RECEIVE_DTE) last_dt
from dbo.COLA_TXN_ERROR with(nolock)
where RECEIVE_DTE >= '<run start - 2 min>'
group by ProcessCde, ERROR_TYPE order by n desc
```

`ERROR_TYPE` 可能夾帶卡號，顯示前要把 13–19 位數字遮成末 4 碼。要追到範本時，用 `TRACE_NO`／`REF_NO` 對 GW log 的 RelationId（`<MMddHHmmss>-<STAN>-<RRN>-...`）。模擬器的 log 不記範本名稱，所以要自己解析範本：用 `publish\win-x64\ConsoleStressSimulator` 的 `Com.Cybersoft4u.TM.FISC.FISC` 類別解出範本欄位後比對。範本檔名含卡號，回報時要遮罩。

`CHECK_CODE` 前兩碼是檢核項目（對應 `dbo.COLA_AUTH_CHECK.RESP_INNER`），末碼是結果：1 核准、2 拒絕、3 Pick-up、4 轉介（`FnGetRspCode` 的 A/D/P/R 順序）：

| CHECK_CODE | 檢核 | 典型原因 | 處理 |
|---|---|---|---|
| 012 | 資料驗證 | RSN 02 卡不在 `CARD_INF`、03 帳戶不存在、77 等；RSN **63** = NCCC 帳戶驗證的手機／生日不符（換卡造成，預期結果，見上表 6P 列） | 02/03 換卡（卡號池 / 範本）；63 不處理 |
| 022 | 卡片狀態 | 卡有管制碼 `CTL_CODE` | 換卡 |
| 032 | 效期 | RSN 03 交易效期 ≠ 卡片效期（F14/F35） | 模擬器換卡時要同步 F14（`c9c587a` 已修） |
| 042 | 開卡 | `OPEN_FLAG=0` 未開卡 | 換已開卡的卡 |
| 062 | 可用額度 | 壓測消耗額度（核准未全數沖正）：`Cola_UpdateCoreFile` 扣 `CARD_LIMIT`／`CUST_LIMIT`／`PRODUCT_LIMIT` 的可用額度，沖正沒對到原交易就不會加回 | 先跑唯讀的 `tools/sql/Preview-AuthTestCardCredit.sql`，請使用者用 `tools/sql/Reset-AuthTestCardCredit.sql` 補回（會寫入 DB，**不可自行執行**） |
| 072 | PVV | 範本 PIN 對不到新卡 | 範本限制 |
| 082 | CVV | RSN 03 沒帶 CVV 驗證結果：`HSMCheckFlags` 的 `IsCVVCheck=N`，Auth 不呼叫 HSM 驗 CVV，SP 對 BU 003 仍要求有結果 | 卡號池不要放 BU 003 |
| 102 / 232 | ARQC / ATC | 晶片範本密文對不到新卡；ATC（F55 9F36）固定，`Cola_CheckICStatus` 要求大於卡片已存的 ATC，同卡只有第一筆會過 | 範本限制。換卡時模擬器跳過晶片範本：FISC 自 `328e085` 起（F55 帶 9F36）、NCCC 自 `7040e4d` 起（帶 token B2），兩者的 102／232 都不應再出現；代價是晶片交易（entry mode 05/07）沒有測到，NCCC 的 0100 範本全是晶片範本，所以 NCCC 也不會有 0100 |
| 092 | CVV2 | CVV2 驗證失敗。這個環境 `IsCVV2Check=N`，一般交易不驗 CVV2，所以 FISC 的 CVV2 錯誤範本也會被核准；只有 NCCC token 開卡（PROCESS_CODE 920000）不看開關、寫死呼叫 HSM 驗 CVV2（`NcccAuthFlowUtility.cs:416`，`"NNYNNN"`），所以 092 只出現在 NCCC 920000。拒絕原因推測是範本帶的 CVV2 對不到換上的卡，尚未逐筆驗證 | 範本限制 |
| 122 / 124 | 卡片需求（`CARD_REQ_SW`） | 個別卡的自訂需求規則；122 拒絕（RC 05）、124 轉介（RC 01），RSN 22/24 | 只見於少數卡，通常忽略；要細究看 SP 的 `CARD_REQ_SW` 段 |
| 282 | 圈存處理（`BANCS_SW`） | RSN NRSP：VD 圈存沒有回應（MQ 連不到） | 卡號池混入 VD 卡，移除 |
| 252 / 262 | 紅利 / 分期 | 範本交易類型與卡片產品不符 | 範本限制 |

卡號池（`src/CyberCOLA/ConsoleStressSimulator/Config/{Nccc,Fisc}/CardXmlFilePath.xml`，NCCC 與 FISC 共用同一份）的選卡條件：`CARD_INF`/`ACCT_INF`/`ACCT_BOOK`/`CUST_INF` 都有、卡與帳戶無管制碼、`OPEN_FLAG<>'0'`、未過期、`AUTH_SW<>'N'`、非 VD、BU 001/007、`CARD_LIMIT`（及 `CUST_LIMIT`，有資料時）目前可用額度 ≥ 10,000，且卡片與帳戶在 `COLA_AUPALIM` 沒有國內一般消費的限額／限次控管（`DELETE_FLAG='Y'` 為生效；有控管的卡會回 RC 01 `CHECK_CODE 124` 或 RC 05）。目前 41 張（V 26、M 10、J 5）。Track2 = `PAN=YYMM+SERVICE_CODE+00000000`。用 `pwsh tools/New-AuthTestCardPool.ps1` 列出合格卡（唯讀，卡號遮罩），使用者同意後加 `-Apply` 寫入 `src` 與 `publish\win-x64` 的卡號池。e-ticket 範本（`Data/{SVCS,IPASS}`，`src` 與 `publish/Config` 各一份）的 F2 要是 `ESVC_CARD` 內有效的 PID；SVCS（TSCC）還要求 F14 = `ESVC_CARD.EXPIR_DTE`。用 `pwsh tools/Set-ETicketTestCards.ps1` 檢查範本的票卡（唯讀，卡號遮罩），使用者同意後加 `-Apply` 改寫兩份範本；exit 2 代表有網路沒有合格票卡（目前 ICASH）。改卡號池或範本屬於修改測試資料，先問使用者。

2026-10-28 換卡後的基準（成功率）：NCCC 81.5%、SVCS 100%、IPASS 100%、ICASH 100%（16:35 啟用 ICASH 票卡並補回額度後；之前是 50%）；FISC（308 個範本含子資料夾、`-SimCount 330`、額度補回後）46.1%，其中 6P／RSN 63 約 552 筆是預期的帳戶驗證失敗，扣掉後約 79%。明顯低於基準時，先看 062 是否增加（額度又被用完）。
2026-10-29 卡號池改用 `CARD_LIMIT`/`CUST_LIMIT` 篩選（27 → 49 張）後：NCCC 83.0%（400/400 有回覆）；拒絕全是範本限制：102×20、252×24、232×9、092×4、122×8、124×2、262×1，無 062／042／012。同日 FISC（`-SimCount 330`）45.2%，6P 552，扣掉後 77.6%；拒絕以 232×74、122/124×52 為主，132（`CR_AV_SW` 額度控制）RSN 62×17、294（`FREQUENCY_SW` 交易頻率）×5 是壓測重複用卡造成。DB 的 `RESPONSE_DT` 比本機慢約 1 分鐘，依時間查 DB 時起點要提早。

2026-11-04 換卡池重建為 41 張（排除 `COLA_AUPALIM` 控管卡）、模擬器跳過 FISC 身分驗證與晶片範本後：FISC（`-SimCount 100`，400 筆）**95.0%**，剩 RC 05×20（PVV 072×8、資料驗證 012/RSN 24×4 等），無 6P、無 232、無 RC 96；NCCC（`9d2e2df` 再跳過紅利退貨範本後，2026-11-05）**99.0%**，剩 RC 05×4（CVV2 不符 920000 等範本限制），無 RC 96（SP 平均 146 ms、Auth p90 817 ms）；同日稍早 SP 一度慢到平均 326 ms，NCCC 也出現 RC 96×73，和 FISC 的 RC 96 同一機制。0220 資料驗證 012／RSN 75 仍會被 GW 回 0230 時統計算成功；SVCS／ICASH／IPASS 100%。FISC 的 RC 96 取決於 `FISC_0100AuthProcess` 的速度：SP 平均約 150 ms 時沒有，約 360 ms 時 64 條 COLA 連線會被占滿；再出現時記下當時的 SP 時間。

**2026-11-09 目前的基準**（預設參數，每條連線 100 筆，HEAD `5e1b9c1`）：NCCC **99.0%**（400 筆，RC 05×4 全是 092 CVV2，PROCESS_CODE 920000）；FISC **96.5%**（400 筆，RC 05×14：072 PVV×8、HCE 查不到 TPAN 012／RSN 24×4、132 額度控制×2）；SVCS 60 筆、ICASH 40 筆、IPASS 60 筆皆 **100%**。都沒有 RC 96，也沒有未回應。
- FISC 從 95.0% 升到 96.5%，是因為測試庫的 `FISC_0100AuthProcess`／`FISC_0100HceAuthProcess` 修好了 `COUNTRY_CODE` NULL：F43 結尾空白的 MasterCard MTAR 範本每輪 8 筆，以前會走 `CATCH` 回 RC 05。11-04 那 20 筆 RC 05 很可能也包含這 8 筆，但當時沒有對帳，這是推測。
- 修正只套用在測試庫 NCARD。如果 SP 被換回舊版，這 8 筆會再出現，`COLA_TXN_ERROR` 裡會有 `COUNTRY_CODE` 的 Err 515。
- 同日 NCCC 第一次跑只有 91.2%，原因是 062 額度用完（062×31）。使用者執行 `Reset-AuthTestCardCredit` 補回後回到 99.0%。開跑前的額度檢查若在 VPN 連上之前執行，會因為連不到 DB 而被跳過，這時要自己確認額度。
- 這個環境的 `HSMCheckFlags` 全部是 N，所以 FISC 的 CVV2 錯誤範本也會被核准，不會出現 092。NCCC 的 092 來自 token 開卡流程寫死的 CVV2 檢核。

不確定的判讀要標明是推測，並說明怎麼驗證；不要把推測寫成結論。

## 5. 報告格式

每個網路一節，最後一張總表：

```
### NCCC（Group 1）— 2026-10-27 16:09，transcript: LogFiles\AuthTest\NCCC_20261027_160949.txt
| 項目 | 結果 |
| 請求（依 MTI） | 400（0100 ×12、0200 ×124、…） |
| 回應 / 未回應 | 400 / 0 |
| 成功 / 失敗 | 306（76.5%）/ 94（23.5%） |
回應碼分布：RC、筆數、比例、成功/失敗、原因（第 4 節的判讀）
log 異常：GW 連線全忙、Auth No Match（依 MTI）、交易流程 null 回應（依 MTI）、HSM 沒回結果、主要 ERROR
與上一次比較（同 session 有跑過才列）
```

總表欄位：網路、請求數、成功率、未回應、主要失敗原因、是否需要處理。

## 6. 規則

- 報告和指令輸出都**不能出現完整卡號**；需要提到卡號時用 `****-****-****-1234`。log 分析時把 6 位以上的數字替換成 `<n>` 再顯示。
- 不 commit、不 push，除非使用者要求。
- 不改設定檔。若為了測試暫時修改 `publish/` 底下的設定（例如連線數），必須在 `try/finally` 中還原，並在結束後驗證已還原。
- 測試失敗或沒跑起來就照實回報，附上 transcript 路徑與相關輸出。
