/*
    Enable-ICashTestCard.sql  --  WRITES TO THE DATABASE, TEST DATABASE ONLY

    Makes the ICASH e-ticket card(s) in ESVC_CARD usable for the ICASH authorization test. Without it every
    ICASH purchase is declined: the only ICASH card has control code 'C' and an expired e-ticket expiry.

    For each ICASH card whose linked credit card is usable (no control code, opened, not expired, not VD) and
    which has no lost-card report sent (ESVC_SEND_RRN is not 12 characters long), it
      - clears ESVC_CTL_CODE
      - sets EXPIR_DTE to the credit card's EXPIR_DTE: dbo.ESVC_0100AuthProcess checks OPEN_FLAG only when the
        e-ticket's expiry month equals the credit card's, and declines an expired e-ticket
    Nothing else changes. Running it again does nothing once the card is ready. No card number is stored here.

    How to run (DataGrip or any client):
      1. Run Preview-ICashTestCard.sql; check the server, database and the "to change" row.
      2. Run the statements below (USE first). DataGrip in auto-commit mode commits right away.
      3. Run Preview-ICashTestCard.sql again: the row should be "ready" and the count 0.
      4. Point the ICASH templates at the card: pwsh tools\Set-ETicketTestCards.ps1, then -Apply.
*/

-- 0. The tables live in NCARD; a console opened on the login's default database fails with error 208.
USE NCARD;

-- 1. Clear the control code and align the e-ticket expiry with the credit card.
UPDATE e
SET    ESVC_CTL_CODE = '',
       EXPIR_DTE     = c.EXPIR_DTE
FROM   dbo.ESVC_CARD e
JOIN   dbo.CARD_INF c WITH (NOLOCK) ON c.CARD_NBR = e.CARD_NBR
JOIN   dbo.COLA_PACARDF p WITH (NOLOCK) ON p.BU = c.BU AND p.PRODUCT = c.CARD_PRODUCT
WHERE  e.ESVC_TYPE = 'ICASH'
  AND  ISNULL(c.CTL_CODE, '') = '' AND c.OPEN_FLAG <> '0' AND c.EXPIR_DTE >= GETDATE()
  AND  ISNULL(p.DEBIT_SW, 'N') <> 'Y' AND LEN(e.ESVC_SEND_RRN) <> 12
  AND  (e.ESVC_CTL_CODE <> '' OR CONVERT(varchar(6), e.EXPIR_DTE, 112) <> CONVERT(varchar(6), c.EXPIR_DTE, 112));
