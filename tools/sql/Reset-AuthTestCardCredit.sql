/*
    Reset-AuthTestCardCredit.sql  --  WRITES TO THE DATABASE, TEST DATABASE ONLY

    Restores the credit used up by authorization stress tests on the test cards (the cards that pass
    tools\New-AuthTestCardPool.ps1's card-level conditions, credit left out).

    Why credit runs out: every approved authorization lowers CREDIT_AVAIL / CASH_AVAIL in CARD_LIMIT,
    CUST_LIMIT and (when a row exists) PRODUCT_LIMIT through dbo.Cola_UpdateCoreFile, and the reversals the
    simulator sends do not match their original transaction, so nothing gives the credit back. Once
    CREDIT_AVAIL is too low, the Auth declines with RC 05, CHECK_CODE 062 (ID_AVAIL).

    What it does: raises CREDIT_AVAIL to CREDIT_LIMIT and CASH_AVAIL to CASH_LIMIT where they are lower, in
      1. dbo.CARD_LIMIT    (the card)
      2. dbo.CUST_LIMIT    (the customer behind the card's ACCT_NBR; shared by that customer's other cards)
      3. dbo.PRODUCT_LIMIT (when a row exists for the card's account and product)
    Values at or above the limit (e.g. overpayment) are left as they are; limits, authorization history and
    card data are not touched. No card number is stored in this file.

    How to run (DataGrip or any client):
      1. Run Preview-AuthTestCardCredit.sql first; check the server, database and rows.
      2. Run the three UPDATE statements below. Each one stands alone and can be run again safely: a value
         already restored is not changed, so running one twice, or only some of them, does no harm.
         DataGrip in auto-commit mode commits each statement; in manual-commit mode, commit afterwards.
      3. Run Preview-AuthTestCardCredit.sql again: every "rows to update" count should be 0.
*/

-- 0. The tables live in NCARD; a console opened on the login's default database (often master) fails with
--    error 208 "invalid object name 'dbo.CARD_INF'".
USE NCARD;

-- 1. Card level
WITH cards AS (
    SELECT c.CARD_NBR, c.BU, c.ACCT_NBR, c.PRODUCT, c.CURRENCY
    FROM   dbo.CARD_INF c WITH (NOLOCK)
    JOIN   dbo.COLA_PACARDF p WITH (NOLOCK) ON p.BU = c.BU AND p.PRODUCT = c.CARD_PRODUCT
    JOIN   dbo.ACCT_INF a WITH (NOLOCK) ON a.ACCT_NBR = c.ACCT_NBR AND a.BU = c.BU AND a.PRODUCT = c.PRODUCT AND a.CURRENCY = c.CURRENCY
    JOIN   dbo.ACCT_BOOK k WITH (NOLOCK) ON k.ACCT_NBR = a.ACCT_NBR AND k.BU = a.BU AND k.PRODUCT = a.PRODUCT AND k.CURRENCY = a.CURRENCY
    JOIN   dbo.CUST_INF u WITH (NOLOCK) ON u.CUST_NBR = c.CARDHOLDER_NBR AND u.BU = c.BU
    WHERE  ISNULL(c.CTL_CODE, '') = '' AND ISNULL(a.CTL_CODE, '') = '' AND c.EXPIR_DTE >= GETDATE()
      AND  LEN(c.CARD_NBR) = 16 AND c.OPEN_FLAG <> '0' AND p.AUTH_SW <> 'N'
      AND  c.BU IN ('001', '007')
      AND  ISNULL(p.DEBIT_SW, 'N') <> 'Y'
)
UPDATE l
SET    CREDIT_AVAIL = IIF(l.CREDIT_AVAIL < l.CREDIT_LIMIT, l.CREDIT_LIMIT, l.CREDIT_AVAIL),
       CASH_AVAIL   = IIF(l.CASH_AVAIL   < l.CASH_LIMIT,   l.CASH_LIMIT,   l.CASH_AVAIL)
FROM   dbo.CARD_LIMIT l
JOIN   cards c ON l.CARD_NBR = c.CARD_NBR AND l.BU = c.BU AND l.ACCT_NBR = c.ACCT_NBR AND l.PRODUCT = c.PRODUCT AND l.CURRENCY = c.CURRENCY
WHERE  l.CREDIT_AVAIL < l.CREDIT_LIMIT OR l.CASH_AVAIL < l.CASH_LIMIT;

-- 2. Customer level
WITH cards AS (
    SELECT c.BU, c.ACCT_NBR
    FROM   dbo.CARD_INF c WITH (NOLOCK)
    JOIN   dbo.COLA_PACARDF p WITH (NOLOCK) ON p.BU = c.BU AND p.PRODUCT = c.CARD_PRODUCT
    JOIN   dbo.ACCT_INF a WITH (NOLOCK) ON a.ACCT_NBR = c.ACCT_NBR AND a.BU = c.BU AND a.PRODUCT = c.PRODUCT AND a.CURRENCY = c.CURRENCY
    JOIN   dbo.ACCT_BOOK k WITH (NOLOCK) ON k.ACCT_NBR = a.ACCT_NBR AND k.BU = a.BU AND k.PRODUCT = a.PRODUCT AND k.CURRENCY = a.CURRENCY
    JOIN   dbo.CUST_INF u WITH (NOLOCK) ON u.CUST_NBR = c.CARDHOLDER_NBR AND u.BU = c.BU
    WHERE  ISNULL(c.CTL_CODE, '') = '' AND ISNULL(a.CTL_CODE, '') = '' AND c.EXPIR_DTE >= GETDATE()
      AND  LEN(c.CARD_NBR) = 16 AND c.OPEN_FLAG <> '0' AND p.AUTH_SW <> 'N'
      AND  c.BU IN ('001', '007')
      AND  ISNULL(p.DEBIT_SW, 'N') <> 'Y'
)
UPDATE u
SET    CREDIT_AVAIL = IIF(u.CREDIT_AVAIL < u.CREDIT_LIMIT, u.CREDIT_LIMIT, u.CREDIT_AVAIL),
       CASH_AVAIL   = IIF(u.CASH_AVAIL   < u.CASH_LIMIT,   u.CASH_LIMIT,   u.CASH_AVAIL)
FROM   dbo.CUST_LIMIT u
WHERE  EXISTS (SELECT 1 FROM cards c WHERE c.ACCT_NBR = u.CUST_NBR AND c.BU = u.BU)
  AND  (u.CREDIT_AVAIL < u.CREDIT_LIMIT OR u.CASH_AVAIL < u.CASH_LIMIT);

-- 3. Product level
WITH cards AS (
    SELECT c.BU, c.ACCT_NBR, c.PRODUCT, c.CURRENCY, c.CARD_PRODUCT
    FROM   dbo.CARD_INF c WITH (NOLOCK)
    JOIN   dbo.COLA_PACARDF p WITH (NOLOCK) ON p.BU = c.BU AND p.PRODUCT = c.CARD_PRODUCT
    JOIN   dbo.ACCT_INF a WITH (NOLOCK) ON a.ACCT_NBR = c.ACCT_NBR AND a.BU = c.BU AND a.PRODUCT = c.PRODUCT AND a.CURRENCY = c.CURRENCY
    JOIN   dbo.ACCT_BOOK k WITH (NOLOCK) ON k.ACCT_NBR = a.ACCT_NBR AND k.BU = a.BU AND k.PRODUCT = a.PRODUCT AND k.CURRENCY = a.CURRENCY
    JOIN   dbo.CUST_INF u WITH (NOLOCK) ON u.CUST_NBR = c.CARDHOLDER_NBR AND u.BU = c.BU
    WHERE  ISNULL(c.CTL_CODE, '') = '' AND ISNULL(a.CTL_CODE, '') = '' AND c.EXPIR_DTE >= GETDATE()
      AND  LEN(c.CARD_NBR) = 16 AND c.OPEN_FLAG <> '0' AND p.AUTH_SW <> 'N'
      AND  c.BU IN ('001', '007')
      AND  ISNULL(p.DEBIT_SW, 'N') <> 'Y'
)
UPDATE pl
SET    CREDIT_AVAIL = IIF(pl.CREDIT_AVAIL < pl.CREDIT_LIMIT, pl.CREDIT_LIMIT, pl.CREDIT_AVAIL),
       CASH_AVAIL   = IIF(pl.CASH_AVAIL   < pl.CASH_LIMIT,   pl.CASH_LIMIT,   pl.CASH_AVAIL)
FROM   dbo.PRODUCT_LIMIT pl
WHERE  EXISTS (SELECT 1 FROM cards c WHERE c.BU = pl.BU AND c.ACCT_NBR = pl.ACCT_NBR AND c.PRODUCT = pl.PRODUCT
                                       AND c.CURRENCY = pl.CURRENCY AND c.CARD_PRODUCT = pl.CARD_PRODUCT)
  AND  (pl.CREDIT_AVAIL < pl.CREDIT_LIMIT OR pl.CASH_AVAIL < pl.CASH_LIMIT);
