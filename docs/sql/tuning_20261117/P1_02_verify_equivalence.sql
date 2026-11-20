USE [NCARD]   -- test database; change for other environments
GO
/**********************************************************************************
 * P1 step 2 of 3: read-only check that the inline versions return exactly what the
 * originals return. Every query below must return 0 rows before running P1_03.
 **********************************************************************************/

-- FnGetAuthCheckSw: every (BU, AUTH_CHECK) row, a key that does not exist, and the
-- four card-scheme values the callers pass (V, M, J, anything else = NCCC).
SELECT k.BU, k.AUTH_CHECK, q.v AS qtrlyAffiliate,
       o.authcheck AS old_authcheck, n.authcheck AS new_authcheck,
       o.resp      AS old_resp,      n.resp      AS new_resp
FROM (SELECT BU, AUTH_CHECK FROM dbo.COLA_AUTH_CHECK WITH (NOLOCK)
      UNION ALL SELECT '000', '__NO_SUCH_SWITCH__') AS k
CROSS JOIN (VALUES ('V'), ('M'), ('J'), ('N'), ('')) AS q(v)
CROSS APPLY dbo.FnGetAuthCheckSw   (k.BU, k.AUTH_CHECK, q.v) AS o
CROSS APPLY dbo.FnGetAuthCheckSw_IF(k.BU, k.AUTH_CHECK, q.v) AS n
WHERE ISNULL(o.authcheck, '<null>') <> ISNULL(n.authcheck, '<null>')
   OR ISNULL(o.resp, '<null>')      <> ISNULL(n.resp, '<null>');

-- Row counts must match too (both always return exactly one row per call).
SELECT 'row count mismatch' AS problem, k.BU, k.AUTH_CHECK
FROM (SELECT BU, AUTH_CHECK FROM dbo.COLA_AUTH_CHECK WITH (NOLOCK)) AS k
WHERE (SELECT COUNT(*) FROM dbo.FnGetAuthCheckSw   (k.BU, k.AUTH_CHECK, 'V'))
   <> (SELECT COUNT(*) FROM dbo.FnGetAuthCheckSw_IF(k.BU, k.AUTH_CHECK, 'V'));

-- FnGetRspCode: every resp string the switches can produce (from COLA_AUTH_CHECK)
-- combined with every auth result letter, plus an unknown letter and NULL.
SELECT s.resp, a.r AS inAuthRsp,
       o.outRsp AS old_outRsp, n.outRsp AS new_outRsp,
       o.InnerRsp AS old_innerRsp, n.InnerRsp AS new_innerRsp
FROM (SELECT DISTINCT f.resp
      FROM dbo.COLA_AUTH_CHECK AS c WITH (NOLOCK)
      CROSS JOIN (VALUES ('V'), ('M'), ('J'), ('N')) AS q(v)
      CROSS APPLY dbo.FnGetAuthCheckSw(c.BU, c.AUTH_CHECK, q.v) AS f
      WHERE CHARINDEX(';', f.resp) > 0
      UNION SELECT '00050701;011012013014') AS s
CROSS JOIN (VALUES ('A'), ('D'), ('P'), ('R'), ('X'), (NULL)) AS a(r)
CROSS APPLY dbo.FnGetRspCode   (a.r, s.resp) AS o
CROSS APPLY dbo.FnGetRspCode_IF(a.r, s.resp) AS n
WHERE ISNULL(o.outRsp, '<null>')   <> ISNULL(n.outRsp, '<null>')
   OR ISNULL(o.InnerRsp, '<null>') <> ISNULL(n.InnerRsp, '<null>');
GO

-- Optional cost comparison on the test database (read-only; compare "CPU time").
SET STATISTICS TIME ON;
SELECT COUNT(*) FROM dbo.COLA_AUTH_CHECK AS c WITH (NOLOCK)
CROSS APPLY dbo.FnGetAuthCheckSw(c.BU, c.AUTH_CHECK, 'V') AS f;
SELECT COUNT(*) FROM dbo.COLA_AUTH_CHECK AS c WITH (NOLOCK)
CROSS APPLY dbo.FnGetAuthCheckSw_IF(c.BU, c.AUTH_CHECK, 'V') AS f;
SET STATISTICS TIME OFF;
GO
