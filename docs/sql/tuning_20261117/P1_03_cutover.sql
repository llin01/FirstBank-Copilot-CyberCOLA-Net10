USE [NCARD]   -- test database; change for other environments
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
/**********************************************************************************
 * P1 step 3 of 3: replace dbo.FnGetAuthCheckSw and dbo.FnGetRspCode with their inline
 * versions under the SAME names, so every caller (SELECT ... FROM dbo.FnGetAuthCheckSw(...))
 * keeps working without change.
 *
 * Run only after P1_02_verify_equivalence.sql returned 0 rows on this database.
 * A multi-statement TVF cannot be ALTERed into an inline TVF, so this DROPs and
 * CREATEs inside one transaction: callers wait on the schema lock for the moment it
 * takes instead of failing. Keep P1_99_rollback.sql at hand.
 **********************************************************************************/
SET XACT_ABORT ON;
GO

-- Stop if anything is schema-bound to the functions (DROP would fail half way).
IF EXISTS (SELECT 1 FROM sys.sql_expression_dependencies
           WHERE referenced_id IN (OBJECT_ID(N'dbo.FnGetAuthCheckSw'), OBJECT_ID(N'dbo.FnGetRspCode'))
             AND is_schema_bound_reference = 1)
BEGIN
    RAISERROR(N'Schema-bound objects reference FnGetAuthCheckSw / FnGetRspCode; resolve them first.', 16, 1);
    SET NOEXEC ON;   -- skip the rest of the script
END
GO

BEGIN TRANSACTION;
GO

DROP FUNCTION [dbo].[FnGetAuthCheckSw];
GO

CREATE FUNCTION [dbo].[FnGetAuthCheckSw]
(
    @bu             VARCHAR(5),
    @authCheck      VARCHAR(20),
    @qtrlyAffiliate VARCHAR(01)
)
RETURNS TABLE
AS
RETURN
-- Inline version (2026-11-17 tuning, P1). Same result as the former multi-statement
-- version, including one row with authcheck 'Y' and resp ';' when no switch row exists.
SELECT authcheck = CAST(ISNULL(x.sw, 'Y') AS VARCHAR(1)),
       resp      = CAST(CASE WHEN x.found IS NULL
                             THEN ';'
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

DROP FUNCTION [dbo].[FnGetRspCode];
GO

CREATE FUNCTION [dbo].[FnGetRspCode]
(
    @inAuthRsp VARCHAR(01),
    @resp      VARCHAR(50)
)
RETURNS TABLE
AS
RETURN
-- Inline version (2026-11-17 tuning, P1). External codes A(2)D(2)P(2)R(2) before ';',
-- internal codes A(3)D(3)P(3)R(3) after it.
SELECT outRsp   = CAST(SUBSTRING(r.resp39,    (k.pos - 1) * 2 + 1, 2) AS VARCHAR(02)),
       InnerRsp = CAST(SUBSTRING(r.respInner, (k.pos - 1) * 3 + 1, 3) AS VARCHAR(03))
FROM (SELECT p = CHARINDEX(';', @resp, 1)) AS s
CROSS APPLY (SELECT resp39    = CAST(SUBSTRING(@resp, 1, s.p - 1) AS VARCHAR(20)),
                    respInner = CAST(SUBSTRING(@resp, s.p + 1, LEN(@resp) - s.p) AS VARCHAR(20))) AS r
CROSS APPLY (SELECT pos = CHARINDEX(@inAuthRsp, 'ADPR', 1)) AS k;
GO

-- Commit only if both functions now exist as inline TVFs; otherwise roll everything back.
IF @@TRANCOUNT > 0
BEGIN
    IF OBJECTPROPERTYEX(OBJECT_ID(N'dbo.FnGetAuthCheckSw'), 'IsInlineFunction') = 1
       AND OBJECTPROPERTYEX(OBJECT_ID(N'dbo.FnGetRspCode'), 'IsInlineFunction') = 1
        COMMIT TRANSACTION;
    ELSE
    BEGIN
        ROLLBACK TRANSACTION;
        RAISERROR(N'Cut-over incomplete; rolled back. The original functions are unchanged.', 16, 1);
    END
END
GO
SET NOEXEC OFF;
GO

-- The side-by-side copies from P1_01 are no longer needed.
DROP FUNCTION IF EXISTS [dbo].[FnGetAuthCheckSw_IF];
DROP FUNCTION IF EXISTS [dbo].[FnGetRspCode_IF];
GO
