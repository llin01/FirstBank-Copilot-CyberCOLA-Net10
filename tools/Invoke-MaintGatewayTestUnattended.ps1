<#
    Unattended smoke test of the MaintGateway family: for each mode, start the NCCC simulator (WpfStressSimulator on
    14801), the gateway and its tester from publish\win-x64, call the tester's GET /api/test/send-xml, check the reply
    and that the simulator really received the 0420 and answered 0430, then stop everything.

      Mode          Gateway (port)              Tester (5005)
      HTTP          MaintGateway (5000)         MaintGatewayTester
      HTTPS         MaintGatewayHttps (5001)    MaintGatewayTesterHttps
      JWT           MaintGatewayJwt (5001)      MaintGatewayTesterJwt (also acquires a token from /api/token)
      UnifiedHTTP   MaintGatewayUnified         MaintGatewayUnifiedTester, ASPNETCORE_ENVIRONMENT=Http
      UnifiedHTTPS  MaintGatewayUnified         MaintGatewayUnifiedTester, ASPNETCORE_ENVIRONMENT=Https
      UnifiedJWT    MaintGatewayUnified         MaintGatewayUnifiedTester, ASPNETCORE_ENVIRONMENT=Jwt

    A mode passes when the tester answers 200 with Status=Success and the simulator log shows a new 0420 received and
    a 0430 sent. The tester's request already carries P39=00, so the reply's P39 alone proves nothing.
    Every MaintGateway/tester/WpfStressSimulator process is stopped before each mode and at the end. The WPF tester
    (WpfMaintGatewayTester) is not started. Output is masked: card as ****-****-****-1234, token by length only.

    Usage:
      pwsh tools\Invoke-MaintGatewayTestUnattended.ps1
      pwsh tools\Invoke-MaintGatewayTestUnattended.ps1 -Mode HTTP,JWT

    Exit codes: 0 = every mode passed, 1 = not run (missing published exe, Windows PowerShell),
                2 = at least one mode failed.
#>
param(
    # All, or a comma-separated list of HTTP, HTTPS, JWT, UnifiedHTTP, UnifiedHTTPS, UnifiedJWT. Validated below
    # because pwsh -File passes "HTTP,JWT" as a single string.
    [string[]]$Mode = @('All'),
    # Seconds to wait for each process to open its port.
    [ValidateRange(5, 300)]
    [int]$StartTimeoutSec = 30
)

if ($PSVersionTable.PSEdition -eq 'Desktop') {
    Write-Warning "PowerShell 7 (pwsh.exe) is required."
    exit 1
}

. "$PSScriptRoot\CyberCOLA-Common.ps1"
$BaseDir = Get-CyberCOLABaseDir
$PublishDir = Get-CyberCOLAPublishDir

$AllModes = [ordered]@{
    HTTP         = @{ Gw = 'MaintGateway';        Tst = 'MaintGatewayTester';        GwPort = 5000; Env = $null }
    HTTPS        = @{ Gw = 'MaintGatewayHttps';   Tst = 'MaintGatewayTesterHttps';   GwPort = 5001; Env = $null }
    JWT          = @{ Gw = 'MaintGatewayJwt';     Tst = 'MaintGatewayTesterJwt';     GwPort = 5001; Env = $null }
    UnifiedHTTP  = @{ Gw = 'MaintGatewayUnified'; Tst = 'MaintGatewayUnifiedTester'; GwPort = 5000; Env = 'Http' }
    UnifiedHTTPS = @{ Gw = 'MaintGatewayUnified'; Tst = 'MaintGatewayUnifiedTester'; GwPort = 5001; Env = 'Https' }
    UnifiedJWT   = @{ Gw = 'MaintGatewayUnified'; Tst = 'MaintGatewayUnifiedTester'; GwPort = 5001; Env = 'Jwt' }
}
$Mode = @($Mode | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
$unknown = @($Mode | Where-Object { $_ -ne 'All' -and $_ -notin $AllModes.Keys })
if ($unknown.Count) {
    Write-Host "Unknown -Mode: $($unknown -join ', '). Use All or $($AllModes.Keys -join ', ')." -ForegroundColor Red
    exit 1
}
$selected = if ($Mode -contains 'All') { @($AllModes.Keys) } else { @($AllModes.Keys | Where-Object { $_ -in $Mode }) }

$SimName = 'WpfStressSimulator'
$SimPort = 14801
$TesterPort = 5005
$ProcessNames = 'MaintGateway', 'MaintGatewayHttps', 'MaintGatewayJwt', 'MaintGatewayUnified', 'MaintGatewayTester',
                'MaintGatewayTesterHttps', 'MaintGatewayTesterJwt', 'MaintGatewayUnifiedTester', 'WpfMaintGatewayTester', $SimName

# Every exe this run needs must be published; checked up front so a half-run never happens.
$needed = @($SimName) + @($selected | ForEach-Object { $AllModes[$_].Gw, $AllModes[$_].Tst }) | Select-Object -Unique
$missing = @($needed | Where-Object { -not (Test-Path (Join-Path $PublishDir "$_\$_.exe")) })
if ($missing.Count) {
    Write-Host "Not published: $($missing -join ', ')" -ForegroundColor Red
    Write-Host "  pwsh -NoProfile -File tools\Publish-CyberCOLAServices.ps1 -SingleProjectName $($missing -join ',') -TargetRuntime win-x64"
    exit 1
}

# Port check against the listener table: the simulator binds the machine's host name address, not 127.0.0.1.
function Wait-Listening([int]$port, [int]$timeoutSec) {
    $end = (Get-Date).AddSeconds($timeoutSec)
    while ((Get-Date) -lt $end) {
        $listeners = [Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners()
        if ($listeners | Where-Object Port -eq $port) { return $true }
        Start-Sleep -Milliseconds 500
    }
    return $false
}

function Stop-MaintProcesses {
    Stop-CyberCOLAProcesses -Names $ProcessNames *> $null
    # Wait until the ports are free so the next mode does not see the previous listeners.
    $end = (Get-Date).AddSeconds(15)
    while ((Get-Date) -lt $end -and (Get-Process -Name $ProcessNames -ErrorAction SilentlyContinue)) { Start-Sleep -Milliseconds 500 }
}

function Start-Published([string]$name, [string]$environment, [string]$arguments) {
    $exe = Join-Path $PublishDir "$name\$name.exe"
    $psi = [Diagnostics.ProcessStartInfo]::new($exe, $arguments)
    $psi.WorkingDirectory = Split-Path $exe
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    if ($environment) { $psi.Environment['ASPNETCORE_ENVIRONMENT'] = $environment }
    [void][Diagnostics.Process]::Start($psi)
}

# The simulator logs to LogFiles\<date>\Process.log; count the NCCC MTIs it handled after a point in time.
function Get-SimulatorMtiCount([datetime]$since, [string]$pattern) {
    $log = Join-Path $BaseDir "LogFiles\$((Get-Date).ToString('yyyy-MM-dd'))\Process.log"
    if (-not (Test-Path $log)) { return $null }
    $stamp = $since.ToString('yyyy-MM-dd HH:mm:ss')
    $count = 0
    try {
        # The simulator keeps the file open for writing, so it must be opened with shared read/write.
        $reader = [IO.StreamReader]::new([IO.FileStream]::new($log, 'Open', 'Read', 'ReadWrite'))
        try {
            while ($null -ne ($line = $reader.ReadLine())) {
                if ($line.Length -ge 19 -and $line[0] -eq '2' -and [string]::CompareOrdinal($line.Substring(0, 19), $stamp) -ge 0 -and $line.Contains($pattern)) { $count++ }
            }
        } finally { $reader.Dispose() }
    } catch { return $null }
    return $count
}

function Hide-Card([string]$s) {
    if ($s -match '(\d{4})\s*$' -and ($s -replace '\D', '').Length -ge 12) { return "****-****-****-$($Matches[1])" }
    if ($s) { return '***' }
    return ''
}

Write-Host "MaintGateway unattended test: $($selected -join ', ')" -ForegroundColor Cyan
$results = [Collections.Generic.List[object]]::new()
try {
    foreach ($m in $selected) {
        $c = $AllModes[$m]
        Write-Host "`n=== $m : $($c.Gw) + $($c.Tst)$(if ($c.Env) { " (ASPNETCORE_ENVIRONMENT=$($c.Env))" })" -ForegroundColor Cyan
        $r = [ordered]@{ Mode = $m; Http = ''; Status = ''; P39 = ''; Card = ''; Token = ''; Sim0420 = ''; Sim0430 = ''; Ms = ''; Result = 'FAIL'; Error = '' }
        $results.Add([pscustomobject]$r)
        $row = $results[$results.Count - 1]
        Stop-MaintProcesses

        # Same NCCC simulator config as Start-MaintGatewayTest.ps1
        $simCfg = Join-Path $BaseDir "publish\Config\$SimName\Nccc\appsettings.json"
        if (Test-Path $simCfg) { Copy-Item $simCfg (Join-Path $PublishDir "$SimName\appsettings.json") -Force }
        Start-Published $SimName $null "$SimPort AUTO"
        if (-not (Wait-Listening $SimPort $StartTimeoutSec)) { $row.Error = "$SimName did not listen on $SimPort"; Write-Host "  $($row.Error)" -ForegroundColor Red; continue }
        Write-Host "  $SimName listening on $SimPort"

        Start-Published $c.Gw $c.Env ''
        if (-not (Wait-Listening $c.GwPort $StartTimeoutSec)) { $row.Error = "$($c.Gw) did not listen on $($c.GwPort)"; Write-Host "  $($row.Error)" -ForegroundColor Red; continue }
        Write-Host "  $($c.Gw) listening on $($c.GwPort)"

        Start-Published $c.Tst $c.Env ''
        if (-not (Wait-Listening $TesterPort $StartTimeoutSec)) { $row.Error = "$($c.Tst) did not listen on $TesterPort"; Write-Host "  $($row.Error)" -ForegroundColor Red; continue }
        Write-Host "  $($c.Tst) listening on $TesterPort"

        # The gateway signs on to the simulator (0800/0810) after it starts; give it a moment.
        Start-Sleep -Seconds 3
        $sent = Get-Date
        $sw = [Diagnostics.Stopwatch]::StartNew()
        try {
            $resp = Invoke-WebRequest "http://127.0.0.1:$TesterPort/api/test/send-xml" -TimeoutSec 120 -SkipHttpErrorCheck
            $row.Ms = $sw.ElapsedMilliseconds
            $row.Http = [int]$resp.StatusCode
            $body = $resp.Content | ConvertFrom-Json -ErrorAction SilentlyContinue
            if ($resp.StatusCode -eq 200 -and $body) {
                $row.Status = $body.status ?? $body.Status
                $x = $body.response ?? $body.Response
                $row.P39 = $x.webIsoP39Rsp ?? $x.WebIsoP39Rsp
                $row.Card = Hide-Card ($x.webIsoP2CardNbr ?? $x.WebIsoP2CardNbr)
                $tok = $body.token ?? $body.Token
                $row.Token = if ($tok) { "$($tok.Length) chars" } else { '-' }
            } else {
                $detail = if ($body) { "$($body.title) $($body.detail)" } else { "$($resp.Content)" }
                $detail = $detail -replace '\d{12,19}', '<num>'
                $row.Error = $detail.Substring(0, [Math]::Min(200, $detail.Length))
            }
        } catch {
            $row.Ms = $sw.ElapsedMilliseconds
            $row.Error = $_.Exception.Message -replace '\d{12,19}', '<num>'
        }

        # The simulator logs the MTI it parsed and the one it encoded; allow a second for the log to flush.
        Start-Sleep -Seconds 1
        $since = $sent.AddSeconds(-1)
        $row.Sim0420 = Get-SimulatorMtiCount $since 'LoggerHandler[ServerService] | MTI [0420]'
        $row.Sim0430 = Get-SimulatorMtiCount $since 'NcccIso8583 | MTI         :0430'
        $chainOk = ($null -eq $row.Sim0420) -or ($row.Sim0420 -ge 1 -and $row.Sim0430 -ge 1)
        # The TM library logs the MTI both when encoding and when decoding, so 0430 is reported as present or not.
        if ($null -eq $row.Sim0420) { $row.Sim0420 = 'n/a'; $row.Sim0430 = 'n/a' }
        else { $row.Sim0430 = if ($row.Sim0430 -ge 1) { 'yes' } else { 'no' } }
        if ($row.Http -eq 200 -and $row.Status -eq 'Success' -and $chainOk) { $row.Result = 'PASS' }
        elseif (-not $row.Error -and -not $chainOk) { $row.Error = 'tester answered but the simulator log shows no 0420/0430' }
        $color = if ($row.Result -eq 'PASS') { 'Green' } else { 'Red' }
        Write-Host "  $($row.Result): HTTP $($row.Http) $($row.Status), simulator 0420 x$($row.Sim0420) / 0430 x$($row.Sim0430), $($row.Ms) ms" -ForegroundColor $color
    }
} finally {
    Stop-MaintProcesses
}

Write-Host ""
$results | Format-Table Mode, Result, Http, Status, P39, Card, Token, Sim0420, Sim0430, Ms, Error -AutoSize -Wrap | Out-String -Width 220 | ForEach-Object { $_.TrimEnd() } | Write-Host
$passed = @($results | Where-Object Result -eq 'PASS').Count
Write-Host "Summary: $passed/$($results.Count) passed" -ForegroundColor $(if ($passed -eq $results.Count) { 'Green' } else { 'Red' })
exit $(if ($passed -eq $results.Count) { 0 } else { 2 })
