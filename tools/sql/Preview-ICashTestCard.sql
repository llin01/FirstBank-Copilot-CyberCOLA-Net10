/*
    Preview-ICashTestCard.sql  --  read-only (SELECT only)

    Shows whether each ICASH e-ticket card in ESVC_CARD passes the checks of dbo.ESVC_0100AuthProcess that
    Enable-ICashTestCard.sql fixes, and what that script would change. Run it before and after the change.
    Card numbers are shown as their last 4 digits only.

    ESVC_0100AuthProcess approves an ICASH card only when
      - ESVC_CTL_CODE is empty (FnVfyESVCCtlCode)
      - neither the e-ticket (ESVC_CARD.EXPIR_DTE) nor its credit card is expired
      - the e-ticket's expiry month equals the credit card's EXPIR_DTE (then OPEN_FLAG is checked) or
        EXPIR_DTE_LAST (then PREV_OPEN is checked), and that flag is not '0'
      - ESVC_SEND_RRN is not 12 characters long (a lost-card report already sent)
      - the linked credit card passes its own control-code, delinquency and credit checks
    Each statement runs on its own (DataGrip runs them one by one).
*/

-- 0. The tables live in NCARD; a console opened on the login's default database fails with error 208.
USE NCARD;

-- 1. Which server and database this console is connected to.
SELECT @@SERVERNAME AS server_name, DB_NAME() AS database_name;

-- 2. Each ICASH card, its linked credit card and the values Enable-ICashTestCard.sql would set.
SELECT '****' + RIGHT(e.ESVC_NBR, 4) AS esvc_nbr,
       e.ESVC_CTL_CODE AS esvc_ctl_code,
       CONVERT(varchar(7), e.EXPIR_DTE, 120) AS esvc_expiry,
       LEN(e.ESVC_SEND_RRN) AS lost_rrn_len,
       '****' + RIGHT(c.CARD_NBR, 4) AS credit_card,
       ISNULL(c.CTL_CODE, '') AS card_ctl_code, c.OPEN_FLAG, ISNULL(p.DEBIT_SW, 'N') AS vd,
       CONVERT(varchar(7), c.EXPIR_DTE, 120) AS card_expiry,
       l.CREDIT_AVAIL AS card_credit_avail,
       IIF(e.ESVC_CTL_CODE = ''
           AND CONVERT(varchar(6), e.EXPIR_DTE, 112) = CONVERT(varchar(6), c.EXPIR_DTE, 112)
           AND e.EXPIR_DTE >= DATEADD(MONTH, DATEDIFF(MONTH, 0, GETDATE()), 0), 'ready', 'to change') AS state,
       '' AS esvc_ctl_code_after,
       CONVERT(varchar(7), c.EXPIR_DTE, 120) AS esvc_expiry_after
FROM   dbo.ESVC_CARD e WITH (NOLOCK)
LEFT JOIN dbo.CARD_INF c WITH (NOLOCK) ON c.CARD_NBR = e.CARD_NBR
LEFT JOIN dbo.COLA_PACARDF p WITH (NOLOCK) ON p.BU = c.BU AND p.PRODUCT = c.CARD_PRODUCT
LEFT JOIN dbo.CARD_LIMIT l WITH (NOLOCK) ON l.CARD_NBR = c.CARD_NBR AND l.BU = c.BU
WHERE  e.ESVC_TYPE = 'ICASH';

-- 3. Rows Enable-ICashTestCard.sql would change (0 = every usable ICASH card is ready).
SELECT COUNT(*) AS esvc_card_rows_to_update
FROM   dbo.ESVC_CARD e WITH (NOLOCK)
JOIN   dbo.CARD_INF c WITH (NOLOCK) ON c.CARD_NBR = e.CARD_NBR
JOIN   dbo.COLA_PACARDF p WITH (NOLOCK) ON p.BU = c.BU AND p.PRODUCT = c.CARD_PRODUCT
WHERE  e.ESVC_TYPE = 'ICASH'
  AND  ISNULL(c.CTL_CODE, '') = '' AND c.OPEN_FLAG <> '0' AND c.EXPIR_DTE >= GETDATE()
  AND  ISNULL(p.DEBIT_SW, 'N') <> 'Y' AND LEN(e.ESVC_SEND_RRN) <> 12
  AND  (e.ESVC_CTL_CODE <> '' OR CONVERT(varchar(6), e.EXPIR_DTE, 112) <> CONVERT(varchar(6), c.EXPIR_DTE, 112));
