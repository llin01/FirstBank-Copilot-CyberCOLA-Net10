<#
    Points the WpfETicketClientSimulator templates (ICASH, IPASS, SVCS) at an e-ticket card the authorization
    stored procedures accept: F2 becomes the card's ESVC_CARD.ESVC_NBR and, when the template carries F14,
    F14 becomes its ESVC_CARD.EXPIR_DTE (YYMM; the SVCS Auth checks it). Templates with another e-ticket number
    are answered with RC 14.
    Read-only against the database (ApplicationIntent=ReadOnly, SELECT only). Card numbers are never printed.

    An ESVC_CARD row qualifies when:
      - neither the e-ticket nor its linked credit card has a control code, and the credit card is opened
      - neither the e-ticket nor the credit card is expired
      - the credit card is not a VD card (VD funds are blocked over IBM MQ, which the test network cannot reach)
    ESVC_TYPE TSCC is the SVCS network. A template whose card still qualifies keeps it; otherwise it gets the
    first qualifying card (by ESVC_NBR).

    Templates are read from (and with -Apply written to) both copies:
      src\CyberCOLA\WpfETicketClientSimulator\Data          (source)
      publish\Config\WpfETicketClientSimulator\Data         (the folder the published simulator reads)
    A template that does not re-encode to its original bytes is reported and never written.

    Usage:
      pwsh tools\Set-ETicketTestCards.ps1            (show each template's card and what would change; nothing is written)
      pwsh tools\Set-ETicketTestCards.ps1 -Apply     (rewrite the templates that use another card)

    Exit codes: 0 = done, 1 = database or WpfETicketClientSimulator binaries not available,
                2 = some network has no qualifying card (its templates are left unchanged).
#>
param(
    [switch]$Apply
)

if ($PSVersionTable.PSEdition -eq 'Desktop') {
    Write-Warning "PowerShell 7 (pwsh.exe) is required."
    exit 1
}

. "$PSScriptRoot\CyberCOLA-Common.ps1"
$BaseDir = Get-CyberCOLABaseDir
$Networks = [ordered]@{ ICASH = 'ICASH'; IPASS = 'IPASS'; SVCS = 'TSCC' }
$TemplateRoots = 'src\CyberCOLA\WpfETicketClientSimulator\Data', 'publish\Config\WpfETicketClientSimulator\Data'

function Hide-Number([string]$Value) {
    if ($Value.Length -ge 4) { '****' + $Value.Substring($Value.Length - 4) } else { $Value }
}

# --- 1. Qualifying e-ticket cards -------------------------------------------------------------
$connString = Get-CyberCOLADatabaseConnectionString
if (-not $connString) { exit 1 }

# Same driver choice as New-AuthTestCardPool.ps1: the Auth's Microsoft.Data.SqlClient, else System.Data.SqlClient.
$ns = 'Microsoft.Data.SqlClient'
try { Add-Type -Path (Join-Path $BaseDir 'publish\win-x64\CyberAuthNCCC\Microsoft.Data.SqlClient.dll') -ErrorAction Stop }
catch { $ns = 'System.Data.SqlClient'; $connString = $connString -replace '(?i)Column\s+Encryption\s+Setting\s*=\s*[^;]+;?', '' }
$builder = New-Object "$ns.SqlConnectionStringBuilder" $connString
$builder['ApplicationIntent'] = 'ReadOnly'; $builder['Connect Timeout'] = 10
$conn = New-Object "$ns.SqlConnection" $builder.ConnectionString

$sql = @'
select e.ESVC_TYPE, e.ESVC_NBR, substring(convert(varchar(8),e.EXPIR_DTE,112),3,4) yymm
from dbo.ESVC_CARD e with(nolock)
join dbo.CARD_INF c with(nolock) on c.CARD_NBR=e.CARD_NBR
left join dbo.COLA_PACARDF p with(nolock) on p.BU=c.BU and p.PRODUCT=c.CARD_PRODUCT
where isnull(e.ESVC_CTL_CODE,'')='' and isnull(c.CTL_CODE,'')='' and c.OPEN_FLAG <> '0'
  and e.EXPIR_DTE >= getdate() and c.EXPIR_DTE >= getdate()
  and isnull(p.DEBIT_SW,'N') <> 'Y'
order by e.ESVC_TYPE, e.ESVC_NBR
'@

$table = [Data.DataTable]::new()
try {
    $conn.Open()
    $cmd = $conn.CreateCommand(); $cmd.CommandText = $sql; $cmd.CommandTimeout = 60
    $table.Load($cmd.ExecuteReader())
} catch {
    Write-Host "Database query failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Check the VPN (tools\Connect-GlobalProtect.ps1) and try again." -ForegroundColor Yellow
    exit 1
} finally {
    $conn.Dispose()
}

$cards = @{}
foreach ($net in $Networks.Keys) {
    $rows = @($table.Rows | Where-Object { $_.ESVC_TYPE -eq $Networks[$net] } | ForEach-Object { @{ Pid = "$($_.ESVC_NBR)".Trim(); Exp = "$($_.yymm)" } })
    if ($rows.Count -gt 0) {
        $cards[$net] = $rows
        $list = ($rows | ForEach-Object { "$(Hide-Number $_.Pid) (expiry $($_.Exp))" }) -join ', '
        Write-Host ("{0,-6} qualifying card(s): {1}" -f $net, $list) -ForegroundColor Cyan
    } else {
        Write-Host ("{0,-6} no qualifying card in ESVC_CARD (ESVC_TYPE {1}); its templates are left unchanged." -f $net, $Networks[$net]) -ForegroundColor Yellow
    }
}

# --- 2. Templates -----------------------------------------------------------------------------
# The simulator's own message classes decode and re-encode the templates, so the bytes stay in its format.
$simDir = Join-Path $BaseDir 'publish\win-x64\WpfETicketClientSimulator'
if (-not (Test-Path (Join-Path $simDir 'TM.dll'))) {
    Write-Host "WpfETicketClientSimulator is not published ($simDir). Publish it first: pwsh tools\Publish-CyberCOLAServices.ps1 -SingleProjectName WpfETicketClientSimulator -TargetRuntime win-x64" -ForegroundColor Red
    exit 1
}
Get-ChildItem $simDir -Filter *.dll | ForEach-Object { try { [void][Reflection.Assembly]::LoadFrom($_.FullName) } catch { } }
$tm = [Reflection.Assembly]::LoadFrom((Join-Path $simDir 'TM.dll'))
$types = try { $tm.GetTypes() } catch [Reflection.ReflectionTypeLoadException] { $_.Exception.Types | Where-Object { $_ } }
$fieldEnum = $types | Where-Object { $_.Name -eq 'FIELD' -and $_.IsEnum } | Select-Object -First 1
$f2 = [Enum]::Parse($fieldEnum, 'F_2'); $f14 = [Enum]::Parse($fieldEnum, 'F_14')

$changed = 0; $current = 0; $failed = 0
foreach ($root in $TemplateRoots) {
    Write-Host "`n$root" -ForegroundColor White
    foreach ($net in $Networks.Keys) {
        $type = $types | Where-Object { $_.Name -eq $net -and $_.IsClass -and -not $_.IsAbstract } | Select-Object -First 1
        $dir = Join-Path $BaseDir "$root\$net"
        if (-not (Test-Path $dir)) { continue }
        foreach ($file in Get-ChildItem $dir -Recurse -File) {
            # Some template file names contain card numbers.
            $label = "{0,-6} {1}" -f $net, ($file.Name -replace '\d{9,}(\d{4})', '<..$1>')
            $bytes = [IO.File]::ReadAllBytes($file.FullName)
            try {
                $msg = $type.GetConstructor([type[]]@([byte[]])).Invoke(@(, $bytes)); $core = $msg.Core()
            } catch {
                Write-Host "  $label not decodable: $($_.Exception.InnerException.Message)" -ForegroundColor Red
                $failed++; continue
            }
            $label += " MTI $($msg.Mti())"
            $pidNow = "$($core.Field($f2))"
            if (-not $pidNow) { Write-Host "  $label no F2" -ForegroundColor DarkGray; continue }
            $expNow = "$($core.Field($f14))"
            $state = "F2 $(Hide-Number $pidNow)" + $(if ($expNow) { " F14 $expNow" } else { '' })
            if (-not $cards.ContainsKey($net)) { Write-Host "  $label $state (no card to switch to)" -ForegroundColor Yellow; continue }

            # A template whose card still qualifies keeps it (only its F14 is corrected); otherwise it gets the first one.
            $target = $cards[$net] | Where-Object { $_.Pid -eq $pidNow } | Select-Object -First 1
            if (-not $target) { $target = $cards[$net][0] }
            if ($pidNow -eq $target.Pid -and (-not $expNow -or $expNow -eq $target.Exp)) {
                Write-Host "  $label $state (current)" -ForegroundColor Green
                $current++; continue
            }
            if (-not [Linq.Enumerable]::SequenceEqual([byte[]]$msg.Encode().Val(), $bytes)) {
                Write-Host "  $label $state does not re-encode to the same bytes; left unchanged" -ForegroundColor Red
                $failed++; continue
            }
            $core.Field($f2, $target.Pid)
            if ($expNow) { $core.Field($f14, $target.Exp) }
            $newState = "F2 $(Hide-Number $target.Pid)" + $(if ($expNow) { " F14 $($target.Exp)" } else { '' })
            if ($Apply) {
                [IO.File]::WriteAllBytes($file.FullName, [byte[]]$msg.Encode().Val())
                Write-Host "  $label $state -> $newState (written)" -ForegroundColor Cyan
            } else {
                Write-Host "  $label $state -> $newState" -ForegroundColor Cyan
            }
            $changed++
        }
    }
}

$verb = if ($Apply) { 'rewritten' } else { 'to rewrite' }
Write-Host "`n$current template(s) already current, $changed $verb, $failed not handled." -ForegroundColor Cyan
if (-not $Apply -and $changed -gt 0) { Write-Host "Nothing written. Run again with -Apply to update the templates." -ForegroundColor DarkGray }
if ($cards.Count -lt $Networks.Count) { exit 2 }
exit 0
