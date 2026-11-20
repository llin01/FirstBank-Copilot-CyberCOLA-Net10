<#
    Runs one of the SQL scripts in tools\sql against the CyberCOLA database (connection string from
    SystemParameterInfo.xml, decrypted the same way as the services), so they can be run without DataGrip.
    The .sql files stay the only copy of each statement.

    Read-only scripts (no INSERT / UPDATE / DELETE / MERGE) connect with ApplicationIntent=ReadOnly and run as is.
    Scripts that write - TEST DATABASE ONLY - need -Apply, show the server and database, and run only after you
    type YES; all their statements run in one transaction, rolled back if any of them fails.
    Result sets are printed with every 13-19 digit number masked to its last 4 digits (PCI DSS).

    Usage:
      pwsh tools\Invoke-CyberCOLASql.ps1                                         (list the scripts)
      pwsh tools\Invoke-CyberCOLASql.ps1 Preview-AuthTestCardCredit
      pwsh tools\Invoke-CyberCOLASql.ps1 Reset-AuthTestCardCredit -Apply

    Exit codes: 0 = done (or listed), 1 = database not reachable, 2 = no such script,
                3 = write script without -Apply or not confirmed (nothing run), 4 = SQL error (rolled back).
#>
param(
    [Parameter(Position = 0)]
    [string]$Name,
    [switch]$Apply,
    [int]$TimeoutSec = 120
)

if ($PSVersionTable.PSEdition -eq 'Desktop') {
    Write-Warning "PowerShell 7 (pwsh.exe) is required."
    exit 1
}

. "$PSScriptRoot\CyberCOLA-Common.ps1"
$BaseDir = Get-CyberCOLABaseDir
$sqlDir = Join-Path $PSScriptRoot 'sql'

# Comments are removed first, so a statement named in a header ("run the UPDATE below") does not count.
function Test-WritesData([string]$Text) {
    $code = $Text -replace '/\*[\s\S]*?\*/', '' -replace '--[^\r\n]*', ''
    return $code -match '(?i)\b(insert|update|delete|merge)\b'
}

if (-not $Name) {
    Write-Host "SQL scripts in $sqlDir :" -ForegroundColor Cyan
    foreach ($f in Get-ChildItem $sqlDir -Filter *.sql | Sort-Object Name) {
        $kind = if (Test-WritesData (Get-Content $f.FullName -Raw)) { 'WRITES (needs -Apply)' } else { 'read-only' }
        Write-Host ("  {0,-34} {1}" -f $f.BaseName, $kind)
    }
    exit 0
}

$path = Join-Path $sqlDir "$($Name -replace '\.sql$', '').sql"
# Only the scripts in tools\sql: a name with a path in it is rejected rather than resolved.
if ($Name -match '[\\/:]' -or -not (Test-Path $path)) {
    Write-Host "No script '$Name' in $sqlDir. Run without arguments to list them." -ForegroundColor Red
    exit 2
}
$sqlText = Get-Content $path -Raw
$writes = Test-WritesData $sqlText

$connString = Get-CyberCOLADatabaseConnectionString
if (-not $connString) { exit 1 }

# Same driver choice as COLAMainMenu.ps1: the Auth's Microsoft.Data.SqlClient, else System.Data.SqlClient.
$ns = 'Microsoft.Data.SqlClient'
try { Add-Type -Path (Join-Path $BaseDir 'publish\win-x64\CyberAuthNCCC\Microsoft.Data.SqlClient.dll') -ErrorAction Stop }
catch { $ns = 'System.Data.SqlClient'; $connString = $connString -replace '(?i)Column\s+Encryption\s+Setting\s*=\s*[^;]+;?', '' }
$builder = New-Object "$ns.SqlConnectionStringBuilder" $connString
$builder['Connect Timeout'] = 10
if (-not $writes) { $builder['ApplicationIntent'] = 'ReadOnly' }

Write-Host "Script   : $(Split-Path $path -Leaf) ($(if ($writes) { 'WRITES TO THE DATABASE' } else { 'read-only' }))" -ForegroundColor Cyan
Write-Host "Server   : $($builder.DataSource)"
Write-Host "Database : $($builder.InitialCatalog) (the script may switch it with USE)"

if ($writes) {
    if (-not $Apply) {
        Write-Host "This script writes to the database. Run its Preview script first, then add -Apply." -ForegroundColor Yellow
        exit 3
    }
    Write-Host "TEST DATABASE ONLY. All statements run in one transaction." -ForegroundColor Yellow
    if ((Read-Host "Type YES to run it on $($builder.DataSource)") -cne 'YES') {
        Write-Host "Not confirmed; nothing was run." -ForegroundColor Yellow
        exit 3
    }
}

function Write-Masked([string]$Text, [string]$Color) {
    # PANs never reach the console: keep the last 4 digits of any 13-19 digit number.
    $masked = [regex]::Replace($Text, '\b\d{9,15}(\d{4})\b', '****$1')
    if ($Color) { Write-Host $masked -ForegroundColor $Color } else { Write-Host $masked }
}

# A script flags the rows to print in red with a 'highlight' column (1 = red), which is not printed itself.
# The script owns the rule (e.g. Preview-AuthTestCardCredit: credit under the card pool's floor).
function Write-ResultTable([Data.DataTable]$Table) {
    $flags = $null
    if ($Table.Columns.Contains('highlight')) {
        $flags = @($Table.Rows | ForEach-Object { $_['highlight'] -eq 1 })
        $Table.Columns.Remove('highlight')
    }
    $text = ($Table | Format-Table -AutoSize | Out-String -Width 250).TrimEnd()
    if ($null -eq $flags -or $Table.Rows.Count -eq 0) { Write-Masked $text; return }

    # Format-Table prints the rows in order right after the dashed separator line, one line per row.
    $lines = $text -split '\r?\n'
    $sep = [Array]::FindIndex($lines, [Predicate[string]] { param($l) $l -match '^-[-\s]*$' })
    $lines[0..$sep] | ForEach-Object { Write-Masked $_ }
    for ($r = 0; $r -lt $flags.Count; $r++) { Write-Masked $lines[$sep + 1 + $r] ($flags[$r] ? 'Red' : '') }
    $red = @($flags | Where-Object { $_ }).Count
    if ($red) { Write-Host "$red row(s) in red (flagged by the script's highlight column)" -ForegroundColor Red }
}

$conn = New-Object "$ns.SqlConnection" $builder.ConnectionString
try {
    $conn.Open()
}
catch {
    Write-Host "Cannot connect to the database: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

$tx = $null
try {
    if ($writes) { $tx = $conn.BeginTransaction() }
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $sqlText
    $cmd.CommandTimeout = $TimeoutSec
    if ($tx) { $cmd.Transaction = $tx }

    # One line per statement that changed rows, so each UPDATE's count is visible.
    $affected = [System.Collections.Generic.List[int]]::new()
    $cmd.add_StatementCompleted({ param($s, $e) $affected.Add($e.RecordCount) })

    $reader = $cmd.ExecuteReader()
    $set = 0
    do {
        if ($reader.FieldCount -gt 0) {
            $set++
            $table = [Data.DataTable]::new()
            $table.Load($reader)
            Write-Host "-- Result $set ($($table.Rows.Count) row(s))" -ForegroundColor Cyan
            Write-ResultTable $table
        }
        elseif (-not $reader.NextResult()) {
            break
        }
    } while (-not $reader.IsClosed)
    $reader.Dispose()

    if ($writes) {
        $i = 0
        foreach ($n in $affected) { $i++; Write-Host "Statement $i : $n row(s) affected" }
        $tx.Commit()
        Write-Host "Committed." -ForegroundColor Green
    }
}
catch {
    if ($tx) { try { $tx.Rollback() } catch { } }
    Write-Host "SQL error$(if ($tx) { ', rolled back' }): $($_.Exception.Message)" -ForegroundColor Red
    exit 4
}
finally {
    $conn.Dispose()
}
exit 0
