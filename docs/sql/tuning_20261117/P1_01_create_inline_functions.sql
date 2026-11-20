USE [NCARD]   -- test database; change for other environments
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
/**********************************************************************************
 * P1 step 1 of 3: create inline (single-statement) versions of two multi-statement
 * table-valued functions, side by side with the originals (new names, *_IF).
 * Running services keep using the originals; nothing changes until P1_03_cutover.sql.
 *
 * Why: in NCARD (2026-11-17) dbo.NCCC_0200AuthProcess averaged 44.6 ms, 28 ms of it
 * CPU, with only 606 logical reads. Each call of a multi-statement TVF costs about
 * 0.5 ms (table variable, own execution context, insert + read back) while the query
 * inside costs about 0.05-0.17 ms. FnGetAuthCheckSw alone ran 263,561 times, about
 * 25-30 times per approved NCCC request.
 *
 * The inline bodies reproduce the originals exactly, including:
 *   - FnGetAuthCheckSw returns ONE row even when COLA_AUTH_CHECK has no match, with
 *     authcheck = 'Y' and resp = ';' (the original's @resp_#39 / @resp_innser start as
 *     '' not NULL, so ISNULL does not apply its defaults in that case);
 *   - the date test uses the same CONVERT(VARCHAR(20), GETDATE(), 112) value compared
 *     with the datetime columns EFF_START_DTE / EFF_END_DTE;
 *   - the same varchar lengths the original variables truncate to (20 / 50, 2 / 3).
 **********************************************************************************/

CREATE OR ALTER FUNCTION [dbo].[FnGetAuthCheckSw_IF]
(
    @bu             VARCHAR(5),
    @authCheck      VARCHAR(20),
    @qtrlyAffiliate VARCHAR(01)
)
RETURNS TABLE
AS
RETURN
SELECT authcheck = CAST(ISNULL(x.sw, 'Y') AS VARCHAR(1)),
       resp      = CAST(CASE WHEN x.found IS NULL
                             THEN ';'                                         -- no row: '' + ';' + ''
                             ELSE ISNULL(x.resp39, '00050701') + ';' + ISNULL(x.respInner, '011012013014')
                        END AS VARCHAR(50))
FROM (SELECT d = CONVERT(VARCHAR(20), GETDATE(), 112)) AS t
OUTER APPLY (
    SELECT TOP (1)
           found     = 1,
           sw        = CASE WHEN a.AUTH_CHECK_SW = 'T' AND a.EFF_START_DTE <= t.d AND a.EFF_END_DTE >= t.d THEN 'Y'
                            WHEN a.AUTH_CHECK_SW = 'S' AND a.EFF_START_DTE <= t.d AND a.EFF_END_DTE >= t.d THEN 'N'
                            WHEN a.AUTH_CHECK_SW = 'S' AND (a.EFF_START_DTE > t.d OR a.EFF_END_DTE < t.d) THEN 'Y'
                            WHEN a.AUTH_CHECK_SW = 'Y' THEN 'Y'
                            WHEN a.AUTH_CHECK_SW = 'N' THEN 'N'
                            ELSE 'N' END,
           resp39    = CAST(CASE @qtrlyAffiliate WHEN 'V' THEN a.RESP_VISA
                                                 WHEN 'M' THEN a.RESP_MASTERCARD
                                                 WHEN 'J' THEN a.RESP_JCB
                                                 ELSE a.RESP_NCCC END AS VARCHAR(20)),
           respInner = CAST(a.RESP_INNER AS VARCHAR(20))
    FROM dbo.COLA_AUTH_CHECK AS a WITH (NOLOCK)
    WHERE a.BU = @bu AND a.AUTH_CHECK = @authCheck
) AS x;
GO

CREATE OR ALTER FUNCTION [dbo].[FnGetRspCode_IF]
(
    @inAuthRsp VARCHAR(01),
    @resp      VARCHAR(50)
)
RETURNS TABLE
AS
RETURN
-- Same arithmetic as the original: external codes A(2)D(2)P(2)R(2) before ';',
-- internal codes A(3)D(3)P(3)R(3) after it. A @resp without ';' raises the same
-- "invalid length" error as the original.
SELECT outRsp   = CAST(SUBSTRING(r.resp39,    (k.pos - 1) * 2 + 1, 2) AS VARCHAR(02)),
       InnerRsp = CAST(SUBSTRING(r.respInner, (k.pos - 1) * 3 + 1, 3) AS VARCHAR(03))
FROM (SELECT p = CHARINDEX(';', @resp, 1)) AS s
CROSS APPLY (SELECT resp39    = CAST(SUBSTRING(@resp, 1, s.p - 1) AS VARCHAR(20)),
                    respInner = CAST(SUBSTRING(@resp, s.p + 1, LEN(@resp) - s.p) AS VARCHAR(20))) AS r
CROSS APPLY (SELECT pos = CHARINDEX(@inAuthRsp, 'ADPR', 1)) AS k;
GO
