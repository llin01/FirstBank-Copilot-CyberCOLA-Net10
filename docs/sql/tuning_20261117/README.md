# NCCC_0200AuthProcess 調校腳本（2026-11-17）

依據 2026-11-17 對測試庫 NCARD 的唯讀分析：`dbo.NCCC_0200AuthProcess` 每次平均 44.6 ms，其中 CPU 28 ms、讀取只有 606 頁，主要耗在核准一筆約 50 次「多陳述式資料表函式」的固定開銷。完整分析與給 DBA 的說明：[CyberCOLA 授權 DB 效能檢查請求](https://claude.ai/artifact/G2HnGdLS1JaZKvfqmXh4j2)。

**這些腳本都還沒有在任何 DB 執行過。** 請 DBA 先在測試庫套用並驗證；正式庫要先用當時的線上版本重新比對（函式與 SP 可能和測試庫不同）。

## 檔案與執行順序

| 順序 | 檔案 | 內容 | 會改 DB？ |
|---|---|---|---|
| 1 | `P1_01_create_inline_functions.sql` | 建立 `FnGetAuthCheckSw_IF`、`FnGetRspCode_IF`（新名稱，和原函式並存） | 是（新增物件，不影響執行中的服務） |
| 2 | `P1_02_verify_equivalence.sql` | 用 `COLA_AUTH_CHECK` 的每一筆、查無資料的情況與所有卡別，比對新舊函式的回傳 | 否（唯讀）；**每個查詢都必須是 0 筆** |
| 3 | `P1_03_cutover.sql` | 在同一個交易裡 DROP + CREATE，用原名稱換成 inline 版本；兩支都成功才 COMMIT，否則全部回復 | 是 |
| — | `P1_99_rollback.sql` | 還原成原本的多陳述式版本（用 `sp_helptext` 讀出的原文組成） | 是（只在要撤回 P1 時用） |
| 4 | `P2_COLA_AUCTOTF_unique_index.sql` | `COLA_AUCTOTF (BU, ACCT_NBR)` 唯一索引；有重複資料時自動停止 | 是 |
| 5 | `P3_tx_auth_card_indexes.sql` | 交易紀錄表 TODAY / HIST 以卡號開頭的索引 | 是 |

P1 不需要修改任何呼叫端：呼叫端都是 `SELECT ... FROM dbo.FnGetAuthCheckSw(...)`，欄位名稱與回傳一列的行為都不變。

## 一定要保留的行為

- `FnGetAuthCheckSw` 查不到開關資料時，回傳 `authcheck = 'Y'`、`resp = ';'`（原函式的變數初始值是空字串，不是 NULL，所以不會套用預設回應碼）。inline 版本照原樣保留，`P1_02` 會驗證這一點。
- **不要拿掉 `Cola_InsertCheckFIl` 的 `UPDLOCK, HOLDLOCK`，也不要把它移出交易。** `NCCC_0200AuthProcess` 第 841 行讀額度時沒有加鎖，防止同一帳戶兩筆交易同時通過額度檢核（超額核准）的就是這把帳戶鎖。P2 只是讓它鎖得更精準。

## 量測方式

套用每一步之後，在 repo 裡依序執行（需要 VPN 與測試卡額度充足）：

```powershell
# Stress: compare AuthTiming (Auth log) and RC 96 with the runs before the change
pwsh tools\Invoke-AuthTestUnattended.ps1 -Network NCCC -SimInterval 0 -SimCount 200 -TrafficWaitSec 120
# Functional regression: NCCC 99.0% (092 x4), FISC 96.5%, SVCS/ICASH/IPASS 100%
pwsh tools\Invoke-AuthTestUnattended.ps1 -Network NCCC
```

套用前的參考值（2026-11-17，測試環境，`Column Encryption Setting` 預設開啟）：NCCC 壓力測試成功率 94.5–96.1%、Auth 每筆平均 87–123 ms；`NCCC_0200AuthProcess` 平均 44.6 ms（CPU 28 ms）。

## 不在這批腳本內

- P4（`Cola_UpdateCheckFIl` 不要每次更新主鍵欄位 `CUST_NBR`）需要修改 SP 本體，要先由 SP 負責人確認 `CUST_NBR` 是否會變動。
- 其他單列多陳述式函式（`FnGetCashMcc`、`FnGetEMGroup`、`FnGetAcqBank`、`FnGetEcFlag`、`FnGetMerInfo` 等）可用同樣方式改寫；建議先看 P1 的實測效果再決定。
