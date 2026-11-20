/*
    Preview-AuthTestCardCredit.sql  --  read-only (SELECT only)

    Shows what Reset-AuthTestCardCredit.sql would change: the test cards (last 4 digits only) with their
    current and restored credit, and how many rows each table would update. Run it before the reset to check
    the server, database and rows, and again afterwards: every "rows to update" count should then be 0.

    Test cards = cards that pass tools\New-AuthTestCardPool.ps1's card-level conditions (credit left out).
    Each statement runs on its own (DataGrip runs them one by one); no variables or temp tables.
*/

-- 0. The tables live in NCARD; a console opened on the login's default database (often master) fails with
--    error 208 "invalid object name 'dbo.CARD_INF'".
USE NCARD;

-- 1. Which server and database this console is connected to.
SELECT @@SERVERNAME AS server_name, DB_NAME() AS database_name;

-- 2. Each test card: current credit and the value after the reset (customer level is shared by a customer's cards).
--    highlight = 1 when card_avail or cust_avail is under 10000, the card pool's floor (New-AuthTestCardPool.ps1
--    -MinCreditAvail): such a card gets declined with CHECK_CODE 062. Invoke-CyberCOLASql.ps1 prints these rows in red.
WITH cards AS (
    SELECT c.CARD_NBR, c.BU, c.ACCT_NBR, c.PRODUCT, c.CURRENCY, c.CARD_PRODUCT, LEFT(p.QTRLY_AFFILIATE, 1) AS brand
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
SELECT c.brand, '****' + RIGHT(c.CARD_NBR, 4) AS card, c.BU,
       l.CREDIT_LIMIT AS card_limit, l.CREDIT_AVAIL AS card_avail,
       IIF(l.CREDIT_AVAIL < l.CREDIT_LIMIT, l.CREDIT_LIMIT, l.CREDIT_AVAIL) AS card_avail_after,
       l.CASH_LIMIT AS card_cash_limit, l.CASH_AVAIL AS card_cash_avail,
       u.CREDIT_LIMIT AS cust_limit, u.CREDIT_AVAIL AS cust_avail,
       IIF(u.CREDIT_AVAIL < u.CREDIT_LIMIT, u.CREDIT_LIMIT, u.CREDIT_AVAIL) AS cust_avail_after,
       IIF(l.CREDIT_AVAIL < 10000 OR u.CREDIT_AVAIL < 10000, 1, 0) AS highlight
FROM   cards c
JOIN   dbo.CARD_LIMIT l WITH (NOLOCK) ON l.CARD_NBR = c.CARD_NBR AND l.BU = c.BU AND l.ACCT_NBR = c.ACCT_NBR AND l.PRODUCT = c.PRODUCT AND l.CURRENCY = c.CURRENCY
LEFT JOIN dbo.CUST_LIMIT u WITH (NOLOCK) ON u.CUST_NBR = c.ACCT_NBR AND u.BU = c.BU
ORDER  BY c.brand, card;

-- 3. Rows each UPDATE in Reset-AuthTestCardCredit.sql would change (0 everywhere = nothing left to reset).
WITH cards AS (
    SELECT c.CARD_NBR, c.BU, c.ACCT_NBR, c.PRODUCT, c.CURRENCY, c.CARD_PRODUCT
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
SELECT (SELECT COUNT(*) FROM cards) AS test_cards,
       (SELECT COUNT(*) FROM dbo.CARD_LIMIT l WITH (NOLOCK)
         WHERE EXISTS (SELECT 1 FROM cards c WHERE c.CARD_NBR = l.CARD_NBR AND c.BU = l.BU AND c.ACCT_NBR = l.ACCT_NBR
                                               AND c.PRODUCT = l.PRODUCT AND c.CURRENCY = l.CURRENCY)
           AND (l.CREDIT_AVAIL < l.CREDIT_LIMIT OR l.CASH_AVAIL < l.CASH_LIMIT)) AS card_limit_rows_to_update,
       (SELECT COUNT(*) FROM dbo.CUST_LIMIT u WITH (NOLOCK)
         WHERE EXISTS (SELECT 1 FROM cards c WHERE c.ACCT_NBR = u.CUST_NBR AND c.BU = u.BU)
           AND (u.CREDIT_AVAIL < u.CREDIT_LIMIT OR u.CASH_AVAIL < u.CASH_LIMIT)) AS cust_limit_rows_to_update,
       (SELECT COUNT(*) FROM dbo.PRODUCT_LIMIT pl WITH (NOLOCK)
         WHERE EXISTS (SELECT 1 FROM cards c WHERE c.BU = pl.BU AND c.ACCT_NBR = pl.ACCT_NBR AND c.PRODUCT = pl.PRODUCT
                                               AND c.CURRENCY = pl.CURRENCY AND c.CARD_PRODUCT = pl.CARD_PRODUCT)
           AND (pl.CREDIT_AVAIL < pl.CREDIT_LIMIT OR pl.CASH_AVAIL < pl.CASH_LIMIT)) AS product_limit_rows_to_update;
