# FISC 授權 SP 寫入失敗（COUNTRY_CODE NULL）修正說明

2026-11-09

`FISC_0100AuthProcess` 與 `FISC_0100HceAuthProcess` 遇到 F43 結尾為空白的交易時，會把 NULL 寫進 NOT NULL 的 `COUNTRY_CODE`，交易被 rollback 並回 RC 05，且 `COLA_TX_AUTH_TODAY` 沒有授權紀錄。建議修改兩處：長度判斷改為包含結尾空白，並在 INSERT 加上 `ISNULL` 防呆。兩處修正已於 2026-11-09 套用到測試庫（NCARD）並驗證通過，見「套用紀錄」。

## 問題與影響

2026-11-09 的 FISC 授權測試中，400 筆裡有 8 筆 0100 被回 RC 05，但 `COLA_TX_AUTH_TODAY` 查不到這 8 筆。錯誤記錄在 `COLA_TXN_ERROR`，訊息一致：

```
0100FISC Fail Row:1007 Err-No:515 Msg:無法插入 NULL 值到資料行 'COUNTRY_CODE'，
資料表 'NCARD.dbo.COLA_TX_AUTH_TODAY'; 資料行不得有 Null。INSERT 失敗。
```

- **觸發條件**：F43（Merchant Information，40 碼）的最後一碼是空白。測試中是 MasterCard 範本把州別代碼 `NV ` 放在第 38–40 碼，這是 MasterCard 允許的格式，正式交易也可能出現。
- **對持卡人的影響**：交易應該依規則判斷核准或拒絕，結果一律被拒（RC 05）。
- **對帳務與查詢的影響**：交易被 rollback，`COLA_TX_AUTH_TODAY` 沒有紀錄，客服或對帳只能從 `COLA_TXN_ERROR` 找到。

## 根因

T-SQL 的 `LEN()` 不計算結尾空白，F43 結尾是空白時 `LEN(@isoP43MerInfo)` 只有 39，解析國別的區塊整個被跳過。

1. `FISC_0100AuthProcess` 第 364 行：`IF LEN(@isoP43MerInfo) = 40` 不成立，`@isoP43LocCountry` 維持 NULL。
2. 第 1007 行：`INSERT dbo.COLA_TX_AUTH_TODAY` 把 `@isoP43LocCountry` 寫進 NOT NULL 的 `COUNTRY_CODE`，寫入失敗（Err 515）。
3. 進入 `CATCH`（第 1087–1089 行）：rollback、呼叫 `cola_InsertTxnError` 寫入 `COLA_TXN_ERROR`，回 RC 05。

| F43 第 38–40 碼 | `LEN()` | 國別結果 |
| --- | --- | --- |
| `TWN` | 40 | `TW` |
| ` TW` | 40 | `TW` |
| `NV `（結尾空白） | 39 | NULL，INSERT 失敗 |

Auth 端（`FiscAuthInfoStruct`）把 F43 原樣傳入，長度是 40，沒有裁掉空白；問題只在 SP 的長度判斷。此外，F43 為 `AVR`／`CVR` 開頭、或交易沒有帶 F43 時，國別同樣不會被設定，也有相同的寫入失敗風險。

## 修正內容

建議兩處都改：第 1 處修正長度判斷，第 2 處在寫入時防呆，涵蓋 `AVR`／`CVR` 和沒有 F43 的情況。

### 1. 長度判斷改成包含結尾空白（主要修正）

只改第一行的條件，區塊內容不變：

```sql
-- LEN() ignores trailing spaces, so a 40-char F43 ending in a blank (e.g. MasterCard state 'NV ')
-- was skipped and left @isoP43LocCountry NULL. Appending a sentinel char counts every character.
IF LEN(@isoP43MerInfo + 'X') - 1 = 40
        BEGIN 
          IF SUBSTRING(@isoP43MerInfo,1,3) ='AVR'  SET @isoS127Id = SUBSTRING(@isoP43MerInfo,4,10)
          ELSE
          IF SUBSTRING(@isoP43MerInfo,1,3) ='CVR' SET @isoS127Pws = SUBSTRING(@isoP43MerInfo,10,6)
          ELSE
              BEGIN 
                 SET @isoP43MerName = SUBSTRING(@isoP43MerInfo,1,22)
                 SET @isoP43MerCity = SUBSTRING(@isoP43MerInfo,24,12)
                 SET @isoP43LocCountry = LEFT(RTRIM(LTRIM(SUBSTRING(@isoP43MerInfo,38,3))),2)
              END        
        END 
```

不用 `DATALENGTH()` 的原因：它算的是 byte 數。特店名稱含中文時（varchar、CP950 一個字 2 bytes），40 個字元會超過 40，反而讓原本正常的交易進不了解析。`LEN(x + 'X') - 1` 算的是含結尾空白的字元數。

### 2. INSERT 時防呆

在 `INSERT dbo.COLA_TX_AUTH_TODAY` 的 SELECT 清單中，對應 `COUNTRY_CODE` 的值改為：

```sql
ISNULL(@isoP43LocCountry, '')   -- COUNTRY_CODE is NOT NULL; AVR/CVR or a missing F43 leave it unset
```

如果 `CITY`（`@isoP43MerCity`）等欄位也是 NOT NULL，請一併加上 `ISNULL`。可先用唯讀查詢確認：

```sql
select name, is_nullable from sys.columns
where object_id = object_id('dbo.COLA_TX_AUTH_TODAY') and name in ('COUNTRY_CODE','CITY')
```

預設值用 `''` 而不是 `'TW'`，是為了不捏造國別；若業務規則要求必填，見「待確認事項」。

## 影響範圍

兩支 SP 的寫法相同，都要修改。行號依 2026-11-09 測試庫 `sp_helptext` 的內容：

| SP | 長度判斷 | INSERT `COLA_TX_AUTH_TODAY` | CATCH 回 RC 05 |
| --- | --- | --- | --- |
| `FISC_0100AuthProcess` | 第 364 行 | 第 1007 行 | 第 1087–1089 行 |
| `FISC_0100HceAuthProcess` | 第 318 行 | 第 842 行 | 第 920–922 行 |

其他 SP 可能也有同樣寫法，請用唯讀查詢掃描：

```sql
select o.name from sys.sql_modules m join sys.objects o on o.object_id = m.object_id
where m.definition like '%LEN(@isoP43MerInfo)%'
```

NCCC／TWIN 的國別是在 C# 端（`NcccAuthInfoStruct`）以 `Length == 40` 解析，`Length` 會計入結尾空白，不受此問題影響。

## 驗證方式

1. **單元驗證**（不寫入 DB），預期結果為 `39 / 40 / NV`：

    ```sql
    declare @v varchar(40) = left('MERCHANT NAME' + replicate(' ',40), 37) + 'NV '
    select LEN(@v) old_len, LEN(@v + 'X') - 1 new_len,
           LEFT(RTRIM(LTRIM(SUBSTRING(@v,38,3))),2) country
    ```

2. **整合測試**：SP 套用到測試庫後，重跑 FISC 授權測試（`pwsh -NoProfile -File tools\Invoke-AuthTestUnattended.ps1 -Network FISC`），預期：
    - `COLA_TXN_ERROR` 不再出現 `COUNTRY_CODE` 的 Err 515。
    - 兩個 MasterCard MTAR 範本（F43 結尾 `NV `）正常寫入 `COLA_TX_AUTH_TODAY`。
    - DB 的 RC 05 筆數與測試統計一致（2026-11-09 修正前為 DB 15 筆、統計 23 筆）。

## 套用紀錄（2026-11-09，測試庫 NCARD）

1. **第一次套用（11:35–11:36）**：兩支 SP 都套用了修正 1，但沒有套用修正 2。而且 `FISC_0100AuthProcess` 是用舊版腳本 ALTER 的，因此遺失了 4 次修改：
    - 20251119 2025-020：`@skipExMCC` 與略過客製 MCC 的判斷、`@cardPermLimit`
    - 20251202：公司卡（BU 003）不能環匯分期的檢核（`FnCheckGlPayWithout003`）
    - 20260828：VD ATC 規則（`FnGetDecimalAtc(@isoP55ATC)`）
    - 20260909：`@vdBlockSeq` 與 `VD_BLOCK_SEQ` 的寫入
2. **重新套用（11:47 `FISC_0100HceAuthProcess`、11:50 `FISC_0100AuthProcess`）**：
    - `FISC_0100AuthProcess` 改用當天錯誤 ALTER 之前讀出的版本為底稿，加上修正 1 與修正 2。
    - `FISC_0100HceAuthProcess` 以當時的線上版本為底稿，補上修正 2。
    - 套用後重新讀取 SP，和腳本逐行比對完全一致，上述 4 次修改都已恢復；`ANSI_NULLS`、`QUOTED_IDENTIFIER` 皆為 ON。
    - 套用的完整腳本：`docs/sql/ALTER_FISC_0100AuthProcess_20261109.sql`、`docs/sql/ALTER_FISC_0100HceAuthProcess_20261109.sql`（只限測試庫；正式庫請以當時的線上版本重新比對後再套用）。
3. **驗證（11:55 FISC 授權測試，400 筆）**：

    | 項目 | 修正前（10:12） | 修正後（11:55） |
    | --- | --- | --- |
    | 成功率 | 94.2% | 96.5% |
    | RC 05（測試統計／DB） | 23 ／ 15 | 14 ／ 14 |
    | `COLA_TX_AUTH_TODAY` 紀錄數 | 392 ／ 400 | 400 ／ 400 |
    | `COLA_TXN_ERROR` 新增 | 8 筆（`COUNTRY_CODE` Err 515） | 0 |

    剩下的 14 筆 RC 05 都是範本或測試資料限制：072 PVV ×8、012／RSN 24（HCE 查不到 TPAN）×4、132 額度控制 ×2。

**請 SP 負責人注意**：第一次套用時用到的 `FISC_0100AuthProcess` 舊版腳本，可能也存放在版本控制或正式庫的部署來源。請確認原始碼來源是不是最新版，避免日後再次被退回舊版。

## 待確認事項

- [ ] 修正後 `NV ` 會被當成國別 `NV`，`@txLocation` 判定為國外。美國州別放在國別欄位是否可接受，還是要另外對應成 `US`？
- [ ] `COUNTRY_CODE` 取不到時，預設值用 `''` 是否可以？下游報表或規則是否要求必填國別？
- [ ] 掃描其他 SP 後，是否有同樣使用 `LEN(@isoP43MerInfo)` 的 SP 也需要一併修正？
