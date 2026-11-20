# 授權連線逾時參考（Authorization Timeout Reference）

本文件整理各外部網路對**發卡端（CyberCOLA）回覆時限**的規定，以及 CyberCOLA 內部與逾時相關的設定，供調整參數或排查逾時問題時查閱。

> 規格內容以下列版本為準，查閱日期 2026-10-27。外部規格若有改版，請重新核對並更新本文件。
> 規格書原文位於 `D:\Document\Credit Card\`，受各發行單位著作權保護，本文件只摘錄數值與章節出處。

---

## 1. 總覽

| 網路 | 發卡端必須回覆的時限 | 逾時後外部網路的處理 | 規格文件 |
|---|---|---|---|
| **FISC** | **4 秒** | 財金進入代行授權 | 財金 6.11 銷售點服務系統 05APP2 **V10.6** |
| **NCCC** | **6 秒** | NCCC 進入代行處理；逾時才到的回覆不採用 | NCCC 銀行授權連線標準手冊（2026-01-01 版） |
| **SVCS** | **5 秒**（參數） | 以 Time Out 處理，回收單端 RC 91 | SVCS 會員銀行連線交易規格 **V2.7** |
| **ICASH** | **5 秒** | 以 Time Out 處理 | icash 聯名卡發卡行連線規格（2015-04-13） |
| **IPASS** | **5 秒**（參數） | 判定逾時，回 RC 91 | 一卡通銀行聯名卡 BMS 連線交易規格書（1040616） |

**最嚴格的是 FISC 的 4 秒。** CyberCOLA 從 GW 收到請求到回覆的總時間（GW 排隊 + 等待空閒 COLA 連線 + Auth 的 DB/HSM 處理）都必須落在這個時限內。

---

## 2. 各網路規定

### 2.1 FISC（財金）

出處：`FISC Manual\43-金融資訊系統概要設計規格書6.11+銷售點服務系統05APP2-V10.6-10.6.pdf`，附件二第 6 章。

| 交易 | 規定 | 逾時處理 |
|---|---|---|
| 0100 → 0110 | 會員銀行收到後 **4 秒**內回覆 | 依「發卡一般交易逾時流程」（6.3.1）：財金進入代行授權 |
| 0120 → 0130 | 發卡行 **4 秒**內回覆 | 依「他行代行授權通知發卡會員流程」；財金視為逾時（6.3.2） |
| 0420 → 0430 | 發卡行 **4 秒**內回覆 | 依「發卡沖正交易逾時流程」（6.3.4） |
| 0302 → 0312（銀行主動） | 財金 4 秒內回覆 0312 | 財金未於 4 秒內回覆時，**發卡行應重送 0302**（6.3.3） |

- 專線邏輯通道：財金會定時監控使用狀態，以 **300 秒**內有無使用為判斷基準（05APP2 專線監控章節）。

### 2.2 NCCC（聯合信用卡處理中心）

出處：`NCCC相關手冊\NCCC銀行授權連線標準手冊-2026年1月1日出版(對外發佈).pdf`。

| 項目 | 規定 | 出處 |
|---|---|---|
| 授權回覆（0110、0210 等） | 發卡連線機構需在 **6 秒**內回覆 | 第四章交易逾時處理流程（4-85 起） |
| 逾時處理 | NCCC 進入代行處理；**超過 6 秒才到的回覆不採用**；若該回覆是授權成功，NCCC 會再送 **0420 沖正**給發卡端 | 同上 |
| 通知類重送 | 0120、0220、0420、0500、0502、0520、0522、0820 在 6 秒內未得回覆，改送 0121、0221、0421、0521、0523、0821 | 第四章 MTI 說明 |
| 0620 → 0630 | 代碼化通知；**規格沒有 0621**（不會重送） | 第六章 6-1、6-27 |
| 0800 → 0810 | NCCC 每 **2 分鐘**送 0800；**120 秒**內未收到 0810，該 channel 視為 link down | 4-79 |
| 0800/0820 cutover | 6 秒內未以 0810 回覆換日，改送 0820；0820 6 秒內未得 0830，改送 0821 | 第四章 |
| TCP 斷線偵測 | 簽出狀態下 NCCC 每 **45 秒**送 TCP KEEP-ALIVE（`0x0000` 兩位元組），未收到 ACK 即關閉連線 | 第三章「斷線偵測」 |

> 實務影響：CyberCOLA 若在 6 秒後才回 0210 核准，NCCC 會丟棄這筆回覆並送 0420，所以 Auth 端必須能正確處理隨後的沖正。

### 2.3 SVCS（悠遊卡）

出處：`SVCS(TSCC)\SVCS會員銀行連線交易規格(V2.7).pdf`。

| 項目 | 規定 |
|---|---|
| SVCS → Issuer 各項交易 | Timer = **5 秒**（參數），未回應即以 Time Out 處理 |
| Acquirer → SVCS | 建議 Timer **≥ 8 秒** |
| Issuer 逾時 | SVCS 回覆 **RC 91**；授權交易無法完成時需進行沖正（2.5～2.8 節逾時流程） |
| 斷線重連 | SVCS 主動連線；Socket 中斷時每 **6 秒**（參數）重連 |
| Echo test | 每 **180 秒**（參數）互送 |

### 2.4 ICASH（愛金卡）

出處：`icash聯名卡的電文規格檔案\icash聯名卡發卡行連線規格_20150413.pdf`。

| 項目 | 規定 |
|---|---|
| 交易逾時 | **5 秒**，接受端沒有 queue |
| 沖正（Reversal） | 逾時或連線異常時**持續 repeat**，直到發卡機構接受 |
| 代行授權通知 | 送 0120；逾時或連線異常時改以 **0121** repeat |
| 斷線重連 | 愛金卡中心主動連線；中斷時每 **6 秒**（參數）重連 |
| Echo test | 每 **180 秒**（參數）互送 |

### 2.5 IPASS（一卡通）

出處：`一卡通\銀行聯名卡「BMS」連線交易規格書_1040616_12.pdf`。

| 項目 | 規定 |
|---|---|
| 交易逾時 | BMS 各通道各項交易 **5 秒**（參數），銀行端未回應即判定逾時（2.3 節） |
| 發卡端逾時 | BMS 回應 **RC 91** |
| 斷線重連 | BMS 主動連線；中斷時每 **10 秒**（可變參數）重連 |
| Echo | 登入後每 **60 秒**（可變參數）互送 |

---

## 3. CyberCOLA 內部的逾時設定

設定的主檔是 `src/CyberCOLA/Config/SocketConfig.json`，用主選單 **K** 同步到各服務的 `appsettings.json`（K 只更新已存在的鍵）。

| 設定 | 預設值 | 位置 | 作用 |
|---|---|---|---|
| `cola-service-idle-channel-wait-ms` | 1000 | 各 GW，`ColaServiceConfig` | GW 到 Auth 的連線全部忙碌時，請求最多等待多久才回 RC 96；0 表示不等待（`IdleChannelRetry`） |
| `cola-service-sessions` | NCCC、FISC、SVCS、ICASH、IPASS 64，TWIN 1 | 各 Auth，`ColaServiceConfig` | Auth 對 GW 開的連線數；每條一次只處理一筆 |
| `fisc-timeout-milliseconds` | 5000 | `CyberAuthFISC`，`IDVerifyClientConfig`（由 `CyberAuthFISCIDVerify` 讀取） | 等待 FISC IDVerify 回覆的上限；回覆一到即喚醒（`PendingResponses`），不再輪詢。舊的 `fisc-sleep-waiting-milliseconds` 已於 2026-11-12 從設定檔移除 |
| `nccc-timeout-milliseconds` | 5000 | `CyberAuthNCCC`，`QRCodeClientConfig`（由 `CyberAuthNCCCQRCode` 讀取） | 等待 NCCC QRCode 回覆的上限；回覆一到即喚醒（`PendingResponses`），不再輪詢。舊的 `nccc-sleep-waiting-milliseconds` 已於 2026-11-12 從設定檔移除 |
| `cola-key-data-retain-minutes` | 20 | 各 Auth | 保留交易 key 資料的時間 |
| `fisc-key-data-retain-minutes` / `nccc-key-data-retain-minutes` | 20（沒有這個鍵時為 10） | `CyberAuthFISC` 的 `IDVerifyClientConfig`／`CyberAuthNCCC` 的 `QRCodeClientConfig` | IDVerify／QRCode 保留交易 key 資料的時間 |
| `server-service-max-timeout-milliseconds` | 10000 | 模擬器（WpfEAISimulator、FiscMaint、SMS）的 `GeneralConfig` | 僅模擬器使用，與正式授權無關。Auth 的 `IDVerifyServerServiceConfig`／`QRCodeServerServiceConfig` 裡原本也有一份，但沒有程式讀取，已於 2026-11-12 移除 |
| Router 用戶端的 `ConnectionTimeOut`／`SendTimeOut`／`ReceiveTimeOut` | 3 秒 | `Config/SystemParameterInfo.xml` 的 `ClientAuthRouterServices` | `CommonLib/Network/Client/ClientService.cs` 乘以 1000 當毫秒用，只讀 NCCC 與 ICASH 兩組；同區段的 `RetryCount`、`MaxSessionNumber` 沒有程式讀取 |
| `fisc-service-reconnect-interval-ms` / `nccc-service-reconnect-interval-ms` / `twin-service-reconnect-interval-ms` | 10000 | 主動端 GW（FISC、FISC IDVerify、NCCC、NCCC QRCode、TWIN），`FiscServiceConfig`／`NcccServiceConfig`／`TwinServiceConfig` | GW 的外部連線斷線後，等多久才重連；沒有這個鍵時為 10 秒。被動端 GW（ICASH、IPASS、SVCS）由外部主動連入，不重連，沒有這個設定 |
| Auth 到 GW 的重連間隔 | 5 秒（程式預設） | `ClientAuth/CyberAuth*.cs` 內的 `ChannelReviveHandler(int interval = 5000)` | Auth 的 COLA 連線斷線後的重連間隔，目前不可設定 |
| HSM 用戶端的重試與間隔 | `retry_time=5`、`interval=60000` | `D:\NCARD_EXE\ProHSM\config.ini`（`[server]` 的 `ip_1`／`port_1`、`ip_2`／`port_2`、`retry_time`、`interval`、`databuffer`），容錯切換在同目錄的 `suipenv.ini` | 由廠商 `SocketClient.dll` 讀取（路徑寫死在 DLL 內；測試環境連 192.168.199.1:8500）。**單位與重試方式無法從原始碼確認**。`Config/HSMServerInfo.xml` 的 `ConnectionTimeOut`／`SendTimeOut`／`ReceiveTimeOut`／`RetryCount` 與 12119–12122 埠**沒有程式讀取**，不影響 HSM 連線 |
| DB `Connect Timeout` | 10 秒 | `Config/SystemParameterInfo.xml` 連線字串 | 建立 DB 連線的上限 |

### 3.1 時間預算（以 FISC 4 秒為例）

| 階段 | 最壞情況 |
|---|---|
| GW 等待空閒 COLA 連線 | 1 秒（`cola-service-idle-channel-wait-ms`；只有連線全忙時才會等） |
| Auth 處理（DB stored procedure + HSM） | 需 **< 約 3 秒** |
| 合計 | 必須 < 4 秒 |

各網路的餘裕：FISC 約 3 秒、SVCS／ICASH／IPASS 約 4 秒、NCCC 約 5 秒（皆已扣除 GW 最多 1 秒的等待）。

---

## 4. 待確認事項

1. **HSM 逾時的單位與重試方式**：若 `ReceiveTimeOut=3` 是秒，而且逾時後會重試 `RetryCount=3` 次，單筆 HSM 呼叫最壞可能接近 9 秒，會超過所有網路的時限。需向 HSM 廠商確認。
2. **規格版本**：本文件依據的 FISC V10.6、SVCS V2.7、icash 2015-04-13、BMS 1040616 是 `D:\Document\Credit Card` 內最新的版本，但不保證是現行版本，請向各單位確認。
3. **FISC 的 Auth 處理時間**：如果正式環境的 Auth 處理常超過 2 秒，建議把 FISC GW 的 `cola-service-idle-channel-wait-ms` 調低（例如 500）。
4. **IDVerify 的等待上限比 FISC 時限長**：`fisc-timeout-milliseconds` 是 5 秒，而 FISC 發卡端時限是 4 秒。如果 IDVerify 在 0100 授權的同步路徑上，等到逾時時財金早已進入代行授權。需確認 IDVerify 是否在該路徑上，再決定是否調低。
