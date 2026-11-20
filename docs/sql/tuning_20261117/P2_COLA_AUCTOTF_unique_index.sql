USE [NCARD]   -- test database; change for other environments
GO
/**********************************************************************************
 * P2: unique index on COLA_AUCTOTF (BU, ACCT_NBR).
 *
 * Why: Cola_InsertCheckFIl (called at NCCC_0200AuthProcess line 565, inside the one
 * transaction that runs to COMMIT at line 1175) reads
 *     COLA_AUCTOTF WITH (UPDLOCK, HOLDLOCK) WHERE BU = @bu AND ACCT_NBR = @acctNbr
 * The clustered PK is (BU, ACCT_NBR, PRODUCT, CURRENCY, CUST_NBR), so that predicate
 * only matches a prefix and HOLDLOCK takes key-range + next-key locks: requests for
 * OTHER accounts that sort next to it also wait. In NCARD that statement averaged
 * 1.21 ms elapsed for 0.067 ms CPU (max 293 ms), i.e. mostly lock waiting.
 * With a unique index on exactly (BU, ACCT_NBR) the same hint locks one key.
 *
 * DO NOT remove the UPDLOCK/HOLDLOCK or move Cola_InsertCheckFIl out of the
 * transaction: NCCC_0200AuthProcess line 841 reads CARD_LIMIT / CUST_LIMIT without a
 * lock, and this account lock is what stops two requests of one account from both
 * passing the credit check (over-limit approval). This index keeps that protection.
 *
 * Risk: any process that inserts a second COLA_AUCTOTF row for the same (BU, ACCT_NBR)
 * with another PRODUCT/CURRENCY/CUST_NBR would start failing. The guard below refuses
 * to create the index if such rows exist today; confirm with the SP owner that one row
 * per account is the design before applying it in production.
 **********************************************************************************/

IF EXISTS (SELECT 1 FROM dbo.COLA_AUCTOTF WITH (NOLOCK)
           GROUP BY BU, ACCT_NBR HAVING COUNT(*) > 1)
BEGIN
    RAISERROR(N'COLA_AUCTOTF has more than one row for some (BU, ACCT_NBR); index not created.', 16, 1);
    RETURN;
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID(N'dbo.COLA_AUCTOTF') AND name = N'UX_COLA_AUCTOTF_BU_ACCT')
    CREATE UNIQUE NONCLUSTERED INDEX [UX_COLA_AUCTOTF_BU_ACCT]
        ON [dbo].[COLA_AUCTOTF] ([BU], [ACCT_NBR]);
        -- On Enterprise edition add: WITH (ONLINE = ON)
GO

-- Rollback:
-- DROP INDEX IF EXISTS [UX_COLA_AUCTOTF_BU_ACCT] ON [dbo].[COLA_AUCTOTF];
