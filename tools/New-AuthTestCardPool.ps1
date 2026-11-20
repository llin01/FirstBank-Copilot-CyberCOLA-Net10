<#
    Picks test cards the authorization stored procedures accept at the card level and builds the
    ConsoleStressSimulator card pool (Config\Nccc and Config\Fisc\CardXmlFilePath.xml, shared by NCCC and FISC).
    Read-only against the database (ApplicationIntent=ReadOnly, SELECT only). Card numbers are never printed.

    A card qualifies when:
      - CARD_INF / ACCT_INF / ACCT_BOOK / CUST_INF rows exist and neither card nor account has a control code
      - it is opened (OPEN_FLAG <> '0'), not expired, and its product allows authorization (AUTH_SW <> 'N')
      - it is not a VD card: VD funds are blocked over IBM MQ, which the test network cannot reach, and each
        attempt holds a COLA channel for 14-21 s (GW then answers RC 96)
      - its BU skips the CVV check (001, 007): the HSM simulator does not return CVV results
      - its current credit availability (CARD_LIMIT, and CUST_LIMIT when the customer has a row) is at least
        -MinCreditAvail: stress runs use up credit, and the Auth declines with check code 062 when it runs out.
        tools\sql\Reset-AuthTestCardCredit.sql restores it
      - neither the card nor its account has a domestic retail count or amount limit in COLA_AUPALIM
        (DELETE_FLAG 'Y' marks an active row): a few test transactions exceed it and the Auth then answers
        with that row's action, e.g. RC 01 with check code 124 (CARD_REQ_SW)

    Usage:
      pwsh tools\New-AuthTestCardPool.ps1                 (list the qualifying cards, masked; nothing is written)
      pwsh tools\New-AuthTestCardPool.ps1 -Apply          (also write the pool to src and publish\win-x64)
      pwsh tools\New-AuthTestCardPool.ps1 -MinCreditAvail 20000

    Exit codes: 0 = done, 1 = database not reachable, 2 = no qualifying card (nothing written).
#>
param(
    [switch]$Apply,
    [decimal]$MinCreditAvail = 10000
)

if ($PSVersionTable.PSEdition -eq 'Desktop') {
    Write-Warning "PowerShell 7 (pwsh.exe) is required."
    exit 1
}

. "$PSScriptRoot\CyberCOLA-Common.ps1"
$BaseDir = Get-CyberCOLABaseDir

$connString = Get-CyberCOLADatabaseConnectionString
if (-not $connString) { exit 1 }

# Same driver choice as COLAMainMenu.ps1: the Auth's Microsoft.Data.SqlClient, else System.Data.SqlClient.
$ns = 'Microsoft.Data.SqlClient'
try { Add-Type -Path (Join-Path $BaseDir 'publish\win-x64\CyberAuthNCCC\Microsoft.Data.SqlClient.dll') -ErrorAction Stop }
catch { $ns = 'System.Data.SqlClient'; $connString = $connString -replace '(?i)Column\s+Encryption\s+Setting\s*=\s*[^;]+;?', '' }
$builder = New-Object "$ns.SqlConnectionStringBuilder" $connString
$builder['ApplicationIntent'] = 'ReadOnly'; $builder['Connect Timeout'] = 10
$conn = New-Object "$ns.SqlConnection" $builder.ConnectionString

$sql = @'
select c.CARD_NBR, c.BU, left(p.QTRLY_AFFILIATE,1) brand, c.OPEN_FLAG,
       substring(convert(varchar(8),c.EXPIR_DTE,112),3,4) yymm,
       case when len(isnull(c.SERVICE_CODE,''))=3 then c.SERVICE_CODE else '201' end svc
from dbo.CARD_INF c with(nolock)
join dbo.COLA_PACARDF p with(nolock) on p.BU=c.BU and p.PRODUCT=c.CARD_PRODUCT
join dbo.ACCT_INF a with(nolock) on a.ACCT_NBR=c.ACCT_NBR and a.BU=c.BU and a.PRODUCT=c.PRODUCT and a.CURRENCY=c.CURRENCY
join dbo.ACCT_BOOK k with(nolock) on k.ACCT_NBR=a.ACCT_NBR and k.BU=a.BU and k.PRODUCT=a.PRODUCT and k.CURRENCY=a.CURRENCY
join dbo.CUST_INF u with(nolock) on u.CUST_NBR=c.CARDHOLDER_NBR and u.BU=c.BU
where isnull(c.CTL_CODE,'')='' and isnull(a.CTL_CODE,'')='' and c.EXPIR_DTE >= getdate()
  and len(c.CARD_NBR)=16 and c.OPEN_FLAG <> '0' and p.AUTH_SW <> 'N'
  and c.BU in ('001','007')
  and isnull(p.DEBIT_SW,'N') <> 'Y'
  and exists (select 1 from dbo.CARD_LIMIT l with(nolock)
              where l.CARD_NBR=c.CARD_NBR and l.BU=c.BU and l.ACCT_NBR=c.ACCT_NBR and l.PRODUCT=c.PRODUCT
                and l.CURRENCY=c.CURRENCY and l.CREDIT_AVAIL >= @minAvail)
  and not exists (select 1 from dbo.CUST_LIMIT s with(nolock)
                  where s.CUST_NBR=c.ACCT_NBR and s.BU=c.BU and s.CREDIT_AVAIL < @minAvail)
  and not exists (select 1 from dbo.COLA_AUPALIM m with(nolock)
                  where m.BU=c.BU and m.CONTROL_NBR in (c.CARD_NBR, c.ACCT_NBR) and m.DELETE_FLAG='Y'
                    and (isnull(m.OS_RT_DT_SW,'N') <> 'N' or isnull(m.OS_RT_MO_TM_SW,'N') <> 'N'
                         or isnull(m.OS_RT_TI_UL_SW,'N') <> 'N' or isnull(m.OS_RT_DT_UL_SW,'N') <> 'N'
                         or isnull(m.OS_RT_MO_UL_SW,'N') <> 'N'))
order by brand, c.BU, c.CARD_NBR
'@

$table = [Data.DataTable]::new()
try {
    $conn.Open()
    $cmd = $conn.CreateCommand(); $cmd.CommandText = $sql; $cmd.CommandTimeout = 60
    [void]$cmd.Parameters.AddWithValue('@minAvail', $MinCreditAvail)
    $table.Load($cmd.ExecuteReader())
} catch {
    Write-Host "Database query failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Check the VPN (tools\Connect-GlobalProtect.ps1) and try again." -ForegroundColor Yellow
    exit 1
} finally {
    $conn.Dispose()
}

$table.Rows | ForEach-Object {
    [pscustomobject]@{ Card = '****' + $_.CARD_NBR.Substring(12); BU = $_.BU; Brand = $_.brand; Open = $_.OPEN_FLAG; Exp = $_.yymm; Svc = $_.svc }
} | Format-Table -AutoSize | Out-String -Width 120 | Write-Host
$byBrand = ($table.Rows | Group-Object { $_.brand } | ForEach-Object { "$($_.Name) $($_.Count)" }) -join ', '
Write-Host "$($table.Rows.Count) qualifying card(s) ($byBrand)." -ForegroundColor Cyan

if ($table.Rows.Count -eq 0) {
    Write-Host "No card qualifies; the pool files were left unchanged." -ForegroundColor Yellow
    exit 2
}
if (-not $Apply) {
    Write-Host "Nothing written. Run again with -Apply to update the card pool files." -ForegroundColor DarkGray
    exit 0
}

$doc = [xml]'<?xml version="1.0" encoding="utf-8"?><CardArray/>'
foreach ($row in $table.Rows) {
    $info = $doc.CreateElement('CardInformation'); $track2 = $doc.CreateElement('Track2Data')
    # PVKI/PVV/CVV are unknown for these cards; only PIN templates verify them.
    $track2.InnerText = "$($row.CARD_NBR)=$($row.yymm)$($row.svc)00000000"
    [void]$info.AppendChild($track2); [void]$doc.DocumentElement.AppendChild($info)
}
foreach ($root in 'src\CyberCOLA\ConsoleStressSimulator\Config', 'publish\win-x64\ConsoleStressSimulator\Config') {
    foreach ($network in 'Nccc', 'Fisc') {
        $dir = Join-Path $BaseDir "$root\$network"
        if (-not (Test-Path $dir)) { Write-Host "  - skipped (not found): $dir" -ForegroundColor Yellow; continue }
        $doc.Save((Join-Path $dir 'CardXmlFilePath.xml'))
        Write-Host "  - written: $root\$network\CardXmlFilePath.xml" -ForegroundColor Green
    }
}
exit 0
