<#
    CyberCOLA NCCC Web Simulator Test Flow

    Start: brings up the NCCC chain in order and stops at the first failing step:
      1. Database connection (incl. GlobalProtect pre-check)
      2. ServiceRouter
      3. CyberGWNCCC
      4. WpfHSMSimulator (HSM on port 8500; CyberAuthNCCC calls it and exits when it is missing)
      5. CyberAuthNCCC
      6. WebStressSimulator (NCCC config)
    then prints how to use the WebStressSimulator web UI.

    Stop: closes the simulator browser window opened by Start, then stops the same processes in
    reverse order.

    Sockets: lists every TCP socket of those processes with the peer process on the other end.

    Dashboard: live view (refresh every 2 s) of process status, link connection counts and
    transaction counts read from the GW and simulator logs, counted from when it is opened.

    Chart: per-port transaction chart (Show-CyberCOLATrafficChart.ps1 -Network NCCC), redrawn every
    -RefreshSec seconds.

    Usage:
      .\Start-NcccWebSimulatorTest.ps1                  (menu loop: 1 Start, 2 Stop, 3 Sockets, 4 Dashboard, 5 Chart, Q Quit)
      .\Start-NcccWebSimulatorTest.ps1 -Action Start
      .\Start-NcccWebSimulatorTest.ps1 -Action Start -TimeoutSec 60
      .\Start-NcccWebSimulatorTest.ps1 -Action Stop
      .\Start-NcccWebSimulatorTest.ps1 -Action Sockets
      .\Start-NcccWebSimulatorTest.ps1 -Action Dashboard                  (until Q / Esc)
      .\Start-NcccWebSimulatorTest.ps1 -Action Dashboard -DurationSec 60  (stops by itself)
      .\Start-NcccWebSimulatorTest.ps1 -Action Chart -RefreshSec 5        (until Q / Esc)

    Exit codes (with -Action; the menu loop exits 0 on Q):
                Start: 0 = all steps OK, 1..6 = number of the step that failed.
                Stop:  0 = all stopped, 1 = some process is still running.
                Sockets / Dashboard / Chart: 0.
#>
param(
    [ValidateSet('Start', 'Stop', 'Sockets', 'Dashboard', 'Chart')]
    [string]$Action,
    [int]$TimeoutSec = 30,
    [int]$DurationSec = 0,
    [int]$RefreshSec = 2
)

# Common requires PowerShell 7 (Core edition); restart under pwsh when launched from Windows PowerShell 5.x.
if ($PSVersionTable.PSEdition -eq 'Desktop') {
    $pwshCmd = Get-Command pwsh -ErrorAction SilentlyContinue
    if ($pwshCmd) {
        $forward = @()
        if ($Action) { $forward += '-Action', $Action }
        & $pwshCmd.Source -NoProfile -File $MyInvocation.MyCommand.Path @forward -TimeoutSec $TimeoutSec -DurationSec $DurationSec -RefreshSec $RefreshSec
        exit $LASTEXITCODE
    } else {
        Write-Warning "PowerShell 7 (pwsh.exe) is required but was not found."
        exit 1
    }
}

$ScriptDir = $PSScriptRoot
. "$ScriptDir\CyberCOLA-Common.ps1"

$StepCount = 6
$NcccSimPort = 14801
$HsmPort = 8500
$NcccSimUrl = 'http://localhost:5200'
# Dedicated browser profile for the simulator window: its browser processes are the only ones whose
# command line contains this folder, so Stop can close them without touching the user's own windows.
$SimBrowserProfile = Join-Path $env:TEMP 'CyberCOLA-NcccSim-Browser'
$Results = [System.Collections.Generic.List[object]]::new()

# The NCCC test flow processes in start order. Only the NCCC simulator instance (run from its
# Nccc config folder) is matched, so other WebStressSimulator instances (e.g. FISC) are ignored.
$NcccProcesses = @(
    @{ Name = 'ServiceRouter';             Get = { Get-Process ServiceRouter -ErrorAction SilentlyContinue } }
    @{ Name = 'CyberGWNCCC';               Get = { Get-Process CyberGWNCCC -ErrorAction SilentlyContinue } }
    @{ Name = 'WpfHSMSimulator';           Get = { Get-Process WpfHSMSimulator -ErrorAction SilentlyContinue } }
    @{ Name = 'CyberAuthNCCC';             Get = { Get-Process CyberAuthNCCC -ErrorAction SilentlyContinue } }
    @{ Name = 'WebStressSimulator (NCCC)'; Get = { Get-Process WebStressSimulator -ErrorAction SilentlyContinue | Where-Object { $_.Path -like '*\WebStressSimulator\Nccc\*' } } }
)

# Out-String pads the table with blank lines; trim them so the spacing stays one blank line.
function Show-Results {
    Write-Host ""
    Write-Host (($Results | Format-Table -AutoSize | Out-String).Trim())
    Write-Host ""
}

# Returns $Ok; the caller aborts the flow on $false (no exit here, so the menu loop survives).
function Complete-Step([int]$Number, [string]$Title, [bool]$Ok, [TimeSpan]$Elapsed) {
    $Results.Add([pscustomobject]@{
        Step    = $Number
        Name    = $Title
        Status  = if ($Ok) { 'OK' } else { 'FAILED' }
        Seconds = [math]::Round($Elapsed.TotalSeconds, 1)
    })
    if (-not $Ok) {
        Show-Results
        Write-Host "[FAILED] Step $Number/$StepCount ($Title). Aborting NCCC test flow." -ForegroundColor Red
    }
    return $Ok
}

# Same launch style as Manage-SingleProcess in COLAMainMenu-Console.ps1; an already running
# process is left alone, but its ports are still verified.
function Start-NcccService([int]$Number, [string]$Name) {
    Write-Host "`n>>> [$Number/$StepCount] Starting $Name..." -ForegroundColor Yellow
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    if (Get-Process $Name -ErrorAction SilentlyContinue) {
        Write-Host "  $Name is already running; skipping start." -ForegroundColor DarkGray
    } else {
        $exe = Resolve-CyberCOLAExecutable -Name $Name
        if (-not $exe) {
            Write-Host "  Binary not found for $Name. Publish it first." -ForegroundColor Red
            return (Complete-Step $Number $Name $false $sw.Elapsed)
        }
        Start-Process "cmd.exe" -ArgumentList "/c title $Name & `"$exe`"" -WorkingDirectory (Split-Path $exe) -WindowStyle Minimized
        # cmd.exe spawns the service asynchronously; without this wait the port check below
        # sees no process yet and reports it as terminated.
        for ($i = 0; $i -lt 20 -and -not (Get-Process $Name -ErrorAction SilentlyContinue); $i++) {
            Start-Sleep -Milliseconds 250
        }
    }
    $ports = @(Get-CyberCOLAExpectedPorts -Name $Name)
    $ok = Wait-CyberCOLAPorts -Description "$Name ports $($ports -join ',')" -Ports $ports -ProcessName $Name -TimeoutSec $TimeoutSec
    return (Complete-Step $Number $Name $ok $sw.Elapsed)
}

function Show-WebSimulatorGuide {
    Write-Host "`n================ How to use WebStressSimulator (NCCC) ================" -ForegroundColor Cyan
    Write-Host " 1. $NcccSimUrl is opened in its own browser window (open it manually if it does not appear)."
    Write-Host "    The listener is already started (TRANS TYPE = NCCC, LISTEN PORT = $NcccSimPort):"
    Write-Host "    the header shows 'Running' and ACTIVE CONNS shows the CyberGWNCCC connections."
    Write-Host " 2. Send transactions (card 'Payload & Queue Management'):"
    Write-Host "    a. Tick templates under TEMPLATES (or click 'All')."
    Write-Host "    b. Click 'Load Selected Templates'."
    Write-Host "    c. Set the number next to 'Ingest payloads' (repeat count) and click 'Ingest payloads'."
    Write-Host " 3. Watch the results:"
    Write-Host "    - TPS / SUCCESS / ERRORS in 'Live Telemetry & Performance Metrics'."
    Write-Host "    - Request/response lines in 'Transaction Logs' (System Logs / UDP Logs)."
    Write-Host "    - ACTIVE MESSAGE QUEUES shows pending messages; 'Reset Queues' purges them."
    Write-Host " 4. TRANS TYPE / LISTEN PORT are locked while running. Other settings (ECHO INT., REPEAT COUNT,"
    Write-Host "    TPS CALC INT., Sign-on Check, Auto Echo) apply on the next 'Stop Service' + 'Start Service'."
    Write-Host "    Stopping the listener drops the CyberGWNCCC connections until it is started again."
    Write-Host " 5. 'Close Simulator' ends the simulator process only."
    Write-Host "    To stop everything: .\Start-NcccWebSimulatorTest.ps1 -Action Stop"
    Write-Host "=======================================================================" -ForegroundColor Cyan
}

function Get-SimBrowserProcesses {
    Get-CimInstance Win32_Process -Filter "Name = 'msedge.exe' OR Name = 'chrome.exe'" -ErrorAction SilentlyContinue |
        Where-Object { $_.CommandLine -like "*$SimBrowserProfile*" }
}

# Opens the simulator UI as an app window (Edge, else Chrome) with the dedicated profile; falls back
# to the default browser, whose tab Stop cannot close.
function Open-SimulatorBrowser {
    if (@(Get-SimBrowserProcesses).Count -gt 0) {
        Write-Host "Simulator browser window is already open." -ForegroundColor DarkGray
        return
    }
    $browser = @(
        "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe"
        "$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe"
        "$env:ProgramFiles\Google\Chrome\Application\chrome.exe"
        "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe"
        "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe"
    ) | Where-Object { Test-Path $_ } | Select-Object -First 1
    if ($browser) {
        Write-Host "Opening $NcccSimUrl in its own browser window..." -ForegroundColor Cyan
        Start-Process $browser -ArgumentList "--user-data-dir=`"$SimBrowserProfile`"", '--no-first-run', '--no-default-browser-check', "--app=$NcccSimUrl"
    } else {
        Write-Host "Edge/Chrome not found; opening $NcccSimUrl in the default browser (Stop will not close it)." -ForegroundColor Yellow
        Start-Process $NcccSimUrl
    }
}

function Close-SimulatorBrowser {
    $procs = @(Get-SimBrowserProcesses)
    if ($procs.Count -eq 0) {
        Write-Host ("  {0,-28} not running" -f 'Simulator browser window') -ForegroundColor DarkGray
        return $true
    }
    # Helper processes (e.g. crashpad) can outlive the first kill by a few seconds, so repeat until none remain.
    $deadline = (Get-Date).AddSeconds(10)
    do {
        $left = @(Get-SimBrowserProcesses)
        $left | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
        if ($left.Count -gt 0) { Start-Sleep -Milliseconds 500 }
    } while ($left.Count -gt 0 -and (Get-Date) -lt $deadline)
    if (@(Get-SimBrowserProcesses).Count -eq 0) {
        Write-Host ("  {0,-28} closed ({1} browser processes)" -f 'Simulator browser window', $procs.Count) -ForegroundColor Green
        return $true
    }
    Write-Host ("  {0,-28} STILL RUNNING" -f 'Simulator browser window') -ForegroundColor Red
    return $false
}

function Invoke-StartAll {
    Write-Host "==================================================" -ForegroundColor Cyan
    Write-Host "      CyberCOLA NCCC Web Simulator Test Flow" -ForegroundColor Cyan
    Write-Host "==================================================" -ForegroundColor Cyan

    # 1. Database (Out-Host keeps child-script output on screen and out of this function's return value)
    Write-Host "`n>>> [1/$StepCount] Testing database connection..." -ForegroundColor Yellow
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    & "$ScriptDir\Test-DatabaseConnection.ps1" | Out-Host
    if (-not (Complete-Step 1 'Database' ($LASTEXITCODE -eq 0) $sw.Elapsed)) { return 1 }

    # 2-3. ServiceRouter, Gateway
    if (-not (Start-NcccService 2 'ServiceRouter')) { return 2 }
    if (-not (Start-NcccService 3 'CyberGWNCCC')) { return 3 }

    # 4. HSM simulator, before Auth: CyberAuthNCCC calls the HSM (port 8500) and exits when it is missing.
    Write-Host "`n>>> [4/$StepCount] Starting HSM Simulator..." -ForegroundColor Yellow
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    if (Test-CyberCOLAPortListening -Port $HsmPort) {
        Write-Host "  Port $HsmPort is already listening; skipping start." -ForegroundColor DarkGray
    } else {
        & "$ScriptDir\COLASimulatorMainMenu.ps1" -AutoSelect "7" | Out-Host
    }
    $ok = Wait-CyberCOLAPorts -Description "HSM Simulator port $HsmPort" -Ports @($HsmPort) -TimeoutSec $TimeoutSec
    if (-not (Complete-Step 4 'WpfHSMSimulator' $ok $sw.Elapsed)) { return 4 }

    if (-not (Start-NcccService 5 'CyberAuthNCCC')) { return 5 }

    # 6. NCCC Web simulator (listens on 14801; CyberGWNCCC is the TCP client and connects in)
    Write-Host "`n>>> [6/$StepCount] Starting NCCC Web Simulator..." -ForegroundColor Yellow
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    if (Test-CyberCOLAPortListening -Port $NcccSimPort) {
        Write-Host "  Port $NcccSimPort is already listening; skipping start." -ForegroundColor DarkGray
    } else {
        $oldSimType = $env:COLA_SIMULATOR_TYPE
        $env:COLA_SIMULATOR_TYPE = "WebStressSimulator"
        # No try/finally here: inside a try block, the menu's harmless Clear-Host error (no console
        # handle, e.g. redirected output) aborts the whole menu script before the simulator starts.
        & "$ScriptDir\COLASimulatorMainMenu.ps1" -AutoSelect "0" | Out-Host
        $env:COLA_SIMULATOR_TYPE = $oldSimType
    }
    $ok = Wait-CyberCOLAPorts -Description "NCCC Web Simulator port $NcccSimPort" -Ports @($NcccSimPort) -TimeoutSec $TimeoutSec
    if (-not (Complete-Step 6 'WebStressSimulator (NCCC)' $ok $sw.Elapsed)) { return 6 }

    Show-Results
    Write-Host "==================================================" -ForegroundColor Green
    Write-Host "   [SUCCESS] NCCC Web Simulator test flow is up." -ForegroundColor Green
    Write-Host "==================================================" -ForegroundColor Green
    Show-WebSimulatorGuide
    Open-SimulatorBrowser
    return 0
}

function Invoke-StopAll {
    Write-Host "==================================================" -ForegroundColor Yellow
    Write-Host "      Stopping CyberCOLA NCCC Web Simulator Flow" -ForegroundColor Yellow
    Write-Host "==================================================" -ForegroundColor Yellow

    # Close the browser first so the page does not sit on a "reconnecting" screen.
    $allStopped = Close-SimulatorBrowser
    # Reverse of the start order.
    foreach ($t in $NcccProcesses[($NcccProcesses.Count - 1)..0]) {
        $procs = @(& $t.Get)
        if ($procs.Count -eq 0) {
            Write-Host ("  {0,-28} not running" -f $t.Name) -ForegroundColor DarkGray
            continue
        }
        $procs | Stop-Process -Force -ErrorAction SilentlyContinue
        $procs | ForEach-Object { $_.WaitForExit(5000) | Out-Null }
        if (@(& $t.Get).Count -eq 0) {
            Write-Host ("  {0,-28} stopped (PID {1})" -f $t.Name, ($procs.Id -join ',')) -ForegroundColor Green
        } else {
            Write-Host ("  {0,-28} STILL RUNNING" -f $t.Name) -ForegroundColor Red
            $allStopped = $false
        }
    }

    if ($allStopped) {
        Write-Host "`n[SUCCESS] All NCCC test flow processes are stopped." -ForegroundColor Green
        return 0
    }
    Write-Host "`n[FAILED] Some processes are still running." -ForegroundColor Red
    return 1
}

# Maps PID -> component name for the running NCCC flow processes.
function Get-NcccProcessMap {
    $map = @{}
    foreach ($t in $NcccProcesses) {
        foreach ($proc in @(& $t.Get)) { $map[[int]$proc.Id] = $t.Name }
    }
    return $map
}

function Show-NcccSockets {
    $pidMap = Get-NcccProcessMap
    if ($pidMap.Count -eq 0) {
        Write-Host "No NCCC test flow process is running." -ForegroundColor Yellow
        return 0
    }

    $table = Get-CyberCOLAFastTCPTable
    # Owner of each local port, used to name the process on the other end of a local connection.
    $portOwner = @{}
    foreach ($row in $table) { if (-not $portOwner.ContainsKey([int]$row.LocalPort)) { $portOwner[[int]$row.LocalPort] = [int]$row.OwningProcess } }
    # Only a remote address of this machine can be matched to a local port owner.
    $localIps = @([System.Net.NetworkInformation.NetworkInterface]::GetAllNetworkInterfaces() |
        ForEach-Object { $_.GetIPProperties().UnicastAddresses.Address.IPAddressToString })

    # netstat-style state names.
    $stateNames = @{ Listen = 'LISTENING'; Established = 'ESTABLISHED'; TimeWait = 'TIME_WAIT'; CloseWait = 'CLOSE_WAIT'
                     FinWait1 = 'FIN_WAIT_1'; FinWait2 = 'FIN_WAIT_2'; SynSent = 'SYN_SENT'; SynReceived = 'SYN_RECEIVED'
                     LastAck = 'LAST_ACK'; Closing = 'CLOSING'; Closed = 'CLOSED' }
    $endpoint = { param($ip, $port) if ("$ip".Contains(':')) { "[$ip]:$port" } else { "${ip}:$port" } }

    # Reuse the fast Win32 table (well under a second); Get-NetTCPConnection took ~26 s here.
    $rows = foreach ($row in $table) {
        $ownerPid = [int]$row.OwningProcess
        if (-not $pidMap.ContainsKey($ownerPid)) { continue }
        $state = "$($row.State)"
        $peer = ''
        if ($state -ne 'Listen' -and $localIps -contains "$($row.RemoteAddress)" -and $portOwner.ContainsKey([int]$row.RemotePort)) {
            $peerPid = $portOwner[[int]$row.RemotePort]
            $peer = if ($pidMap.ContainsKey($peerPid)) { $pidMap[$peerPid] } else { (Get-Process -Id $peerPid -ErrorAction SilentlyContinue).ProcessName }
        }
        [pscustomobject]@{
            Process = $pidMap[$ownerPid]
            PID     = $ownerPid
            Local   = & $endpoint $row.LocalAddress $row.LocalPort
            Foreign = & $endpoint $row.RemoteAddress $row.RemotePort
            State   = if ($stateNames.ContainsKey($state)) { $stateNames[$state] } else { $state.ToUpper() }
            Port    = [int]$row.LocalPort
            Peer    = $peer
        }
    }

    $order = @{}
    for ($i = 0; $i -lt $NcccProcesses.Count; $i++) { $order[$NcccProcesses[$i].Name] = $i }
    $rows = @($rows | Sort-Object { $order[$_.Process] }, { $_.State -ne 'LISTENING' }, Port, Foreign)

    $wLocal = [math]::Max(15, ($rows.Local | Measure-Object -Property Length -Maximum).Maximum)
    $wForeign = [math]::Max(15, ($rows.Foreign | Measure-Object -Property Length -Maximum).Maximum)
    $wProc = [math]::Max(7, ($rows.Process | Measure-Object -Property Length -Maximum).Maximum)
    $fmt = "  {0,-5} {1,-$wLocal} {2,-$wForeign} {3,-12} {4,-7} {5,-$wProc} {6}"

    # Center the title in '=' so the banner is as wide as the column header below it.
    $header = $fmt -f 'Proto', 'Local Address', 'Foreign Address', 'State', 'PID', 'Process', 'Peer'
    $title = " NCCC Socket Connections ($(Get-Date -Format 'HH:mm:ss')) "
    $left = [math]::Max(3, [int][math]::Floor(($header.Length - $title.Length) / 2))
    $right = [math]::Max(3, $header.Length - $title.Length - $left)
    Write-Host ""
    Write-Host (('=' * $left) + $title + ('=' * $right)) -ForegroundColor Cyan
    Write-Host $header -ForegroundColor Cyan
    foreach ($r in $rows) {
        $color = if ($r.State -eq 'ESTABLISHED') { 'Green' } elseif ($r.State -eq 'LISTENING') { 'Gray' } else { 'Yellow' }
        Write-Host ($fmt -f 'TCP', $r.Local, $r.Foreign, $r.State, $r.PID, $r.Process, $r.Peer) -ForegroundColor $color
    }
    Write-Host ""
    Write-Host "Summary:" -ForegroundColor Cyan
    $summary = $rows | Group-Object Process, State | ForEach-Object {
        $state = $_.Group[0].State
        [pscustomobject]@{
            Process = $_.Group[0].Process
            State   = $state
            Count   = $_.Count
            # For listeners, show which ports the process listens on.
            Ports   = if ($state -eq 'LISTENING') { ($_.Group.Port | Sort-Object -Unique) -join ', ' } else { '' }
        }
    } | Sort-Object { $order[$_.Process] }, State
    Write-Host (($summary | Format-Table -AutoSize | Out-String).Trim())
    return 0
}

function Show-NcccDashboard([int]$Seconds) {
    $refreshSec = 2
    $logRoot = Get-CyberCOLALogRoot
    # GW: NcccColaSession WRITE = request forwarded to Auth, NcccSession WRITE = message sent to
    # the simulator (responses, network management). Simulator: requests sent / messages received.
    $gw = New-LogCounter { Join-Path $logRoot "NCCC\CyberGWNCCC\$(Get-Date -Format 'yyyy-MM-dd')\CyberGWNCCC.log" } `
        ([regex]'(?m)^\[[^\]]+\]\[[^\]]+\]\[[^\]]+\]\[(NcccColaSession|NcccSession)\]\[WRITE \[[^\]]*\].*?MTI\[(\d{4})\]') `
        { param($m) "$($m.Groups[1].Value)|$($m.Groups[2].Value)" }
    $sim = New-LogCounter { Join-Path $logRoot "WebStressSimulator\Nccc\$(Get-Date -Format 'yyyy-MM-dd')\Process.log" } `
        ([regex]'(?m)\| (Send Message|Server Receive Data)') `
        { param($m) $m.Groups[1].Value }

    $openedAt = Get-Date
    $deadline = if ($Seconds -gt 0) { $openedAt.AddSeconds($Seconds) } else { [datetime]::MaxValue }
    while ((Get-Date) -lt $deadline) {
        Update-LogCounter $gw
        Update-LogCounter $sim
        $pidMap = Get-NcccProcessMap
        $table = Get-CyberCOLAFastTCPTable
        $simLinks = @($table | Where-Object { $_.State -eq 'Established' -and $_.LocalPort -eq $NcccSimPort }).Count
        $colaPorts = @(Get-CyberCOLAExpectedPorts -Name 'CyberGWNCCC')
        $colaLinks = @($table | Where-Object { $_.State -eq 'Established' -and $colaPorts -contains $_.LocalPort }).Count

        Clear-CyberCOLAScreen
        Write-Host "================ NCCC Transaction Dashboard ================" -ForegroundColor Cyan
        Write-Host (" Now {0:HH:mm:ss}   Counting since {1:HH:mm:ss}   Refresh {2} s   Press Q / Esc to return" -f (Get-Date), $openedAt, $refreshSec)
        Write-Host ""
        Write-Host " Process                      PID      Status" -ForegroundColor Cyan
        foreach ($t in $NcccProcesses) {
            $procId = ($pidMap.GetEnumerator() | Where-Object Value -eq $t.Name | Select-Object -First 1).Key
            $status = if ($procId) { 'Running' } else { 'Stopped' }
            Write-Host (" {0,-28} {1,-8} " -f $t.Name, $procId) -NoNewline
            Write-Host $status -ForegroundColor $(if ($procId) { 'Green' } else { 'Red' })
        }
        Write-Host ""
        Write-Host " Links (established)" -ForegroundColor Cyan
        Write-Host (" GW -> Simulator   port {0,-6}   {1}" -f $NcccSimPort, $simLinks)
        Write-Host (" Auth -> GW COLA   port {0,-6}   {1}" -f ($colaPorts -join ','), $colaLinks)
        Write-Host ""
        Write-Host (" {0,-40} {1,8} {2,8} {3,7}" -f 'Transactions', 'Total', '+Last', 'TPS') -ForegroundColor Cyan
        $lines = @(
            @{ Label = 'Simulator  sent requests';           Counter = $sim; Key = 'Send Message' }
            @{ Label = 'Simulator  received messages';       Counter = $sim; Key = 'Server Receive Data' }
        )
        foreach ($key in ($gw.Counts.Keys | Sort-Object)) {
            $session, $mti = $key -split '\|'
            $label = if ($session -eq 'NcccColaSession') { "GW  request  -> Auth       MTI $mti" } else { "GW  message  -> Simulator  MTI $mti" }
            $lines += @{ Label = $label; Counter = $gw; Key = $key }
        }
        foreach ($l in $lines) {
            $total = [int]$l.Counter.Counts[$l.Key]
            $last = [int]$l.Counter.Last[$l.Key]
            Write-Host (" {0,-40} {1,8} {2,8} {3,7:N1}" -f $l.Label, $total, $last, ($last / $refreshSec))
        }
        if ($gw.Counts.Count -eq 0) { Write-Host " (no GW traffic since the dashboard opened)" -ForegroundColor DarkGray }

        # Wait for the next refresh while watching for Q / Esc.
        $waitUntil = (Get-Date).AddSeconds($refreshSec)
        while ((Get-Date) -lt $waitUntil) {
            if (Test-CyberCOLAKeyAvailable) {
                $key = Read-CyberCOLAConsoleKey -Intercept
                if ($key -and ($key.Key -eq 'Escape' -or $key.KeyChar -in 'q', 'Q')) { return 0 }
            }
            Start-Sleep -Milliseconds 200
        }
    }
    return 0
}

function Invoke-Action([string]$Name) {
    $Results.Clear()
    switch ($Name) {
        'Start'     { Invoke-StartAll }
        'Stop'      { Invoke-StopAll }
        'Sockets'   { Show-NcccSockets }
        'Dashboard' { Show-NcccDashboard $DurationSec }
        'Chart'     { & "$ScriptDir\Show-CyberCOLATrafficChart.ps1" -Network NCCC -RefreshSec $RefreshSec -DurationSec $DurationSec; 0 }
    }
}

# Non-interactive: run the requested action once and return its exit code.
if ($Action) {
    exit (Invoke-Action $Action)
}

# Interactive: keep showing the menu until the user quits.
while ($true) {
    Write-Host "`n================ NCCC Web Simulator Test Flow ================" -ForegroundColor Cyan
    Write-Host " 1. Start all services + NCCC Web Simulator"
    Write-Host " 2. Stop all services + NCCC Web Simulator"
    Write-Host " 3. Display all socket connections"
    Write-Host " 4. Transaction dashboard (GW + Simulator)"
    Write-Host " 5. Transaction chart per socket port"
    Write-Host " Q. Quit"
    Write-Host "===============================================================" -ForegroundColor Cyan
    $choice = Read-Host "Enter choice [1/2/3/4/5/Q]"
    # $null means stdin is closed (e.g. piped input ran out); treat it as Quit to avoid looping forever.
    if ($null -eq $choice) { $choice = 'Q' }

    switch ($choice.Trim().ToUpper()) {
        '1' { $code = Invoke-Action 'Start'; Write-Host "`n[Start] finished with code $code." -ForegroundColor DarkGray }
        '2' { $code = Invoke-Action 'Stop';  Write-Host "`n[Stop] finished with code $code." -ForegroundColor DarkGray }
        '3' { $null = Invoke-Action 'Sockets' }
        '4' { $null = Invoke-Action 'Dashboard' }
        '5' { $null = Invoke-Action 'Chart' }
        'Q' { Write-Host "Bye." -ForegroundColor Cyan; exit 0 }
        default { Write-Host "Invalid choice: '$choice'." -ForegroundColor Red }
    }
}