<#
    Runs tools\Test-AuthorizationSystem.ps1 for one network without a person at the keyboard:
      1. starts the authorization chain and waits for its Control Menu; Group 2 (ICASH, IPASS, SVCS) is
         started with -AutoSend, so WpfETicketClientSimulator sends every template by itself
      2. lets traffic run for -TrafficWaitSec, then selects [5] (transaction summary) and [9] (stop everything)
    The whole console output is saved to -OutFile. Group 1 (NCCC, FISC) first checks, read-only, whether the card
    pool still has credit and warns if not; the run goes ahead either way.

    Usage:
      pwsh tools\Invoke-AuthTestUnattended.ps1 -Network NCCC
      pwsh tools\Invoke-AuthTestUnattended.ps1 -Network FISC -SimInterval 50 -SimReplaceCard:$false
      pwsh tools\Invoke-AuthTestUnattended.ps1 -Network FISC -SimCount 330 -TrafficWaitSec 150
      pwsh tools\Invoke-AuthTestUnattended.ps1 -Network FISC -SimIncludeChip
      pwsh tools\Invoke-AuthTestUnattended.ps1 -Network IPASS -RepeatCount 10 -IntervalMs 100
      pwsh tools\Invoke-AuthTestUnattended.ps1 -Network NCCC -StreamOutput   (also print the test output live)

    Exit codes: 0 = summary collected, 1 = the chain never reached the Control Menu,
                2 = the summary was not printed, 3 = the test script had to be killed.
#>
param(
    [Parameter(Mandatory)]
    [ValidateSet('NCCC', 'FISC', 'ICASH', 'IPASS', 'SVCS')]
    [string]$Network,
    # Seconds of traffic between reaching the Control Menu and asking for the summary.
    [int]$TrafficWaitSec = 90,
    # Group 1 only: passed to Test-AuthorizationSystem.ps1. Templates are sent in file order, so a
    # -SimCount below the template count (FISC sends 308) never reaches the later ones.
    [int]$SimCount = 100,
    [int]$SimInterval = 50,
    [bool]$SimReplaceCard = $true,
    # Group 1 only: also send the chip templates (102/232 declines expected). They reach the HSM's ARQC check
    # only when HSMCheckFlags in SystemParameterInfo.xml set IsARQCCheck=Y for the network.
    [switch]$SimIncludeChip,
    # Group 2 only: passed to Test-AuthorizationSystem.ps1 -AutoSend.
    [int]$RepeatCount = 10,
    [int]$IntervalMs = 100,
    # Group 1 only: warn before the run when a pool card has less credit available than this.
    [decimal]$MinCreditAvail = 10000,
    [string]$OutFile,
    # Also print the test script's output as it arrives (it is otherwise only in -OutFile at the end), so a
    # caller such as WebToolsDashboard can follow the [Step n/N] progress live.
    [switch]$StreamOutput
)

if ($PSVersionTable.PSEdition -eq 'Desktop') {
    Write-Warning "PowerShell 7 (pwsh.exe) is required."
    exit 1
}

$RootDir = Split-Path -Parent $PSScriptRoot
$TestScript = Join-Path $PSScriptRoot 'Test-AuthorizationSystem.ps1'
if (-not $OutFile) {
    $OutFile = Join-Path $RootDir "LogFiles\AuthTest\${Network}_$(Get-Date -Format yyyyMMdd_HHmmss).txt"
}
New-Item -ItemType Directory -Force -Path (Split-Path -Parent $OutFile) | Out-Null
$isGroup2 = $Network -in 'ICASH', 'IPASS', 'SVCS'

$out = [Text.StringBuilder]::new()
function Write-Note([string]$Text) {
    $line = "[Unattended $(Get-Date -Format HH:mm:ss)] $Text"
    [void]$out.AppendLine($line)
    Write-Host $line -ForegroundColor Cyan
}

# Approved stress traffic uses up the test cards' credit, and the declines (RC 05, check code 06x) then read like a
# system fault. Report it up front. Read-only and never fatal: a failed check must not stop the test.
function Test-CardPoolCredit {
    try {
        . (Join-Path $PSScriptRoot 'CyberCOLA-Common.ps1')
        $folder = if ($Network -eq 'NCCC') { 'Nccc' } else { 'Fisc' }
        [xml]$pool = Get-Content (Join-Path $RootDir "publish\win-x64\ConsoleStressSimulator\Config\$folder\CardXmlFilePath.xml") -Raw -ErrorAction Stop
        $pans = @($pool.SelectNodes('//Track2Data') | ForEach-Object { $_.InnerText.Trim().Split('=', 'D', '~', '^')[0] })
        Add-Type -Path (Join-Path $RootDir 'publish\win-x64\CyberAuthNCCC\Microsoft.Data.SqlClient.dll')
        $builder = [Microsoft.Data.SqlClient.SqlConnectionStringBuilder]::new((Get-CyberCOLADatabaseConnectionString))
        $builder['ApplicationIntent'] = 'ReadOnly'; $builder['Connect Timeout'] = 5
        $conn = [Microsoft.Data.SqlClient.SqlConnection]::new($builder.ConnectionString)
        try {
            $conn.Open()
            $cmd = $conn.CreateCommand()
            $cmd.CommandText = @'
select case when exists (select 1 from dbo.CARD_LIMIT l with(nolock) where l.CARD_NBR=@pan and l.CREDIT_AVAIL < @min)
              or exists (select 1 from dbo.CARD_INF c with(nolock) join dbo.CUST_LIMIT s with(nolock)
                           on s.CUST_NBR=c.ACCT_NBR and s.BU=c.BU where c.CARD_NBR=@pan and s.CREDIT_AVAIL < @min)
            then 1 else 0 end
'@
            $panParam = $cmd.Parameters.Add('@pan', [Data.SqlDbType]::VarChar, 19)
            [void]$cmd.Parameters.AddWithValue('@min', $MinCreditAvail)
            $low = @($pans | Where-Object { $panParam.Value = $_; $cmd.ExecuteScalar() -eq 1 })
        }
        finally { $conn.Dispose() }

        # Brands, not card numbers: FISC draws only on the VISA cards, so they run out first.
        $byBrand = (@(@('VISA', '4'), @('MC', '5'), @('JCB', '3')) | ForEach-Object {
                $prefix = $_[1]
                "$($_[0]) $(@($low | Where-Object { $_.StartsWith($prefix) }).Count)/$(@($pans | Where-Object { $_.StartsWith($prefix) }).Count)"
            }) -join ', '
        if ($low.Count -eq 0) {
            Write-Note "Credit check: all $($pans.Count) pool cards have at least $MinCreditAvail available."
        }
        else {
            Write-Note "WARNING: $($low.Count)/$($pans.Count) pool cards are below $MinCreditAvail available ($byBrand). Expect extra RC 05 (check code 06x); restore with: pwsh tools\Invoke-CyberCOLASql.ps1 Reset-AuthTestCardCredit -Apply"
        }
    }
    catch {
        # .NET calls surface wrapped in MethodInvocationException; unwrap only that (the SqlException under it
        # says why, e.g. the DB is unreachable). Redact anything that could be a password or a card number.
        $inner = $_.Exception
        while ($inner -is [System.Management.Automation.MethodInvocationException] -and $inner.InnerException) { $inner = $inner.InnerException }
        $reason = ($inner.Message -replace '(?i)\b(password|pwd)\s*=\s*[^;]*', '$1=***' -replace '\d{13,19}', '<card>' -replace '\s+', ' ').Trim()
        if ($reason.Length -gt 200) { $reason = $reason.Substring(0, 200) + '...' }
        Write-Note "Credit check skipped: $($inner.GetType().Name): $reason"
    }
}
# Only the card pool is checked: without it, or on Group 2, the templates' own cards are used.
if (-not $isGroup2 -and $SimReplaceCard) { Test-CardPoolCredit }

# Clear-Host throws "invalid handle" when stdout is redirected, so it is stubbed out for this run.
$sendArgs = if ($isGroup2) { " -AutoSend -RepeatCount $RepeatCount -IntervalMs $IntervalMs" } elseif ($SimIncludeChip) { ' -SimIncludeChip' } else { '' }
$cmd = "function global:Clear-Host {}; & '$TestScript' -Network $Network -SimCount $SimCount -SimInterval $SimInterval -SimReplaceCard:`$$SimReplaceCard$sendArgs; exit `$LASTEXITCODE"
$psi = [Diagnostics.ProcessStartInfo]::new('pwsh', "-NoProfile -Command `"$cmd`"")
$psi.RedirectStandardInput = $true; $psi.RedirectStandardOutput = $true; $psi.RedirectStandardError = $true
$psi.UseShellExecute = $false; $psi.WorkingDirectory = $PSScriptRoot
Write-Note "Run start $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), network $Network"
$p = [Diagnostics.Process]::Start($psi)

$readers = @(
    @{ R = $p.StandardOutput; T = $p.StandardOutput.ReadLineAsync(); P = '' },
    @{ R = $p.StandardError; T = $p.StandardError.ReadLineAsync(); P = 'ERR: ' })
function Pump {
    foreach ($x in $readers) {
        while ($x.T -and $x.T.IsCompleted) {
            if ($null -eq $x.T.Result) { $x.T = $null; break }
            [void]$out.AppendLine($x.P + $x.T.Result)
            if ($StreamOutput) { Write-Host ($x.P + $x.T.Result) }
            $x.T = $x.R.ReadLineAsync()
        }
    }
}
function Wait-Until([scriptblock]$Cond, [int]$Sec) {
    $deadline = (Get-Date).AddSeconds($Sec)
    while ((Get-Date) -lt $deadline -and -not (& $Cond)) { Pump; Start-Sleep -Milliseconds 500 }
    Pump
}

$exitCode = 0
Wait-Until { $p.HasExited -or $out.ToString().Contains('Control Menu') } 300
if (-not $out.ToString().Contains('Control Menu')) {
    Write-Note "The chain did not reach the Control Menu (test script exited: $($p.HasExited)). See the output above."
    $exitCode = 1
} else {
    Write-Note 'Control Menu reached.'
    Write-Note "Letting traffic run for $TrafficWaitSec s."
    Wait-Until { $p.HasExited } $TrafficWaitSec
    $mark = $out.Length
    $p.StandardInput.WriteLine('5'); $p.StandardInput.Flush()
    Wait-Until { $out.ToString().Substring($mark).Contains('Press any key to return') } 180
    if (-not $out.ToString().Substring($mark).Contains('Transaction Summary')) {
        Write-Note 'The transaction summary was not printed.'
        $exitCode = 2
    }
    Wait-Until { $false } 2
}

if (-not $p.HasExited) { $p.StandardInput.WriteLine('9'); $p.StandardInput.Flush() }
Wait-Until { $p.HasExited } 90
if (-not $p.HasExited) {
    $p.Kill($true)
    Write-Note 'Test script did not stop within 90 s and was killed.'
    $exitCode = 3
}
Wait-Until { -not ($readers | Where-Object { $_.T }) } 10
Write-Note "Test script exit code: $($p.ExitCode)"

# Test-AuthorizationSystem.ps1 stops only what it started, so anything left was running before or leaked.
$left = Get-Process CyberGW*, CyberAuth*, ServiceRouter, WpfETicketClientSimulator, ConsoleStressSimulator, WpfHSMSimulator -ErrorAction SilentlyContinue
if ($left) { Write-Note "Still running after the test: $(($left | ForEach-Object { "$($_.Name)($($_.Id))" }) -join ', ')" }

[IO.File]::WriteAllText($OutFile, $out.ToString())
Write-Host "Transcript: $OutFile"
exit $exitCode
