USE [NCARD]   -- test database; change for other environments
GO
/**********************************************************************************
 * P3: card-number indexes on COLA_TX_AUTH_TODAY / COLA_TX_AUTH_HIST.
 *
 * Why: FnGetDomestic02PType (called at NCCC_0200AuthProcess line 524) matches a new
 * refund / adjustment / pre-auth completion / resend against earlier transactions by
 * CARD_NBR, AUTH_CODE, ORIG_TX_AMT, TX_FLAG, ...; Cola_MatchUpdTxAuth (line 1168)
 * by CARD_NBR, SEQ. In NCARD:
 *   - TODAY has only IX_KRTC_KEY (CARD_NBR, ...) usable, then a key lookup per row
 *     (687 reads per match; the refund match at line 214 averaged 12,610 reads, 139 ms);
 *   - HIST has no index starting with CARD_NBR at all (15,476 scans).
 * The missing-index DMV suggested the same leading columns (impact ~99%).
 *
 * ORIG_TX_AMT is numeric(15,2) while the SP compares it with decimal(15,3), which
 * converts the column side, so it is an INCLUDE column, not a key.
 *
 * Cost: every authorization INSERTs one TODAY row, so two more indexes to maintain
 * there (estimated +0.05-0.1 ms per insert) plus storage; HIST is written by the
 * day-end move. Check first whether production already has equivalent indexes, and
 * schedule HIST (large in production) outside business hours.
 **********************************************************************************/

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID(N'dbo.COLA_TX_AUTH_TODAY') AND name = N'IX_TX_AUTH_TODAY_CARD_AUTHCODE')
    CREATE NONCLUSTERED INDEX [IX_TX_AUTH_TODAY_CARD_AUTHCODE]
        ON [dbo].[COLA_TX_AUTH_TODAY] ([CARD_NBR], [AUTH_CODE])
        INCLUDE ([TX_FLAG], [ORIG_TX_AMT], [PROCESS_CODE], [MESS_TYPE], [RESP], [AUTH_END_DT],
                 [MONETARY_FLAG], [MARK_ADJ_FLAG], [MARK_REV_FLAG], [ADJ_FLAG], [SETTLE_FLAG], [SEQ]);
        -- On Enterprise edition add: WITH (ONLINE = ON)
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID(N'dbo.COLA_TX_AUTH_TODAY') AND name = N'IX_TX_AUTH_TODAY_CARD_SEQ')
    CREATE NONCLUSTERED INDEX [IX_TX_AUTH_TODAY_CARD_SEQ]
        ON [dbo].[COLA_TX_AUTH_TODAY] ([CARD_NBR], [SEQ]);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID(N'dbo.COLA_TX_AUTH_HIST') AND name = N'IX_TX_AUTH_HIST_CARD_AUTHCODE')
    CREATE NONCLUSTERED INDEX [IX_TX_AUTH_HIST_CARD_AUTHCODE]
        ON [dbo].[COLA_TX_AUTH_HIST] ([CARD_NBR], [AUTH_CODE])
        INCLUDE ([TX_FLAG], [ORIG_TX_AMT], [PROCESS_CODE], [MESS_TYPE], [RESP], [AUTH_END_DT],
                 [MONETARY_FLAG], [MARK_ADJ_FLAG], [MARK_REV_FLAG], [ADJ_FLAG], [SETTLE_FLAG], [SEQ]);
GO

-- Rollback:
-- DROP INDEX IF EXISTS [IX_TX_AUTH_TODAY_CARD_AUTHCODE] ON [dbo].[COLA_TX_AUTH_TODAY];
-- DROP INDEX IF EXISTS [IX_TX_AUTH_TODAY_CARD_SEQ]      ON [dbo].[COLA_TX_AUTH_TODAY];
-- DROP INDEX IF EXISTS [IX_TX_AUTH_HIST_CARD_AUTHCODE]  ON [dbo].[COLA_TX_AUTH_HIST];
