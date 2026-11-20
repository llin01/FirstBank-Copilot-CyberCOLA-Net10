<#
    CyberCOLA Authorization System Automated Test & Lifecycle Manager (all networks)

    Brings up one network's authorization chain from published binaries, stops at the first
    failing step, then shows a control menu. Every process this script started is stopped on exit
    (menu option 9, Ctrl+C or error); processes that were already running are reused and left alone.

    Group 1 - Active gateway (GW = TCP client): NCCC, FISC
      1. Database   2. ServiceRouter   3. CyberGW<NET>   4. HSM simulator (port 8500)
      5. CyberAuth<NET>   6. ConsoleStressSimulator --type <NET> --count <SimCount> --auto-start
         (listens on the GW's <net>-service-target-ports; the GW connects to it and traffic starts)

    Group 2 - Passive gateway (GW = TCP server): ICASH, IPASS, SVCS
      1. Database   2. ServiceRouter   3. CyberGW<NET> (waits for its server-service-bind-port)
      4. CyberAuth<NET>   5. WpfETicketClientSimulator "<SessionCount> AUTO <NET>"
         (connects to the GW automatically; transactions are sent from its window, or with -AutoSend
         "<SessionCount> AUTO <NET> SEND <RepeatCount> <IntervalMs>" sends every template by itself)

    Usage:
      .\Test-AuthorizationSystem.ps1                      (pick the network from a menu)
      .\Test-AuthorizationSystem.ps1 -Network FISC
      .\Test-AuthorizationSystem.ps1 -Network NCCC -SimCount 10000      (test items per GW connection, default 100)
      .\Test-AuthorizationSystem.ps1 -Network NCCC -SimInterval 50      (ms between messages per GW connection, default 0)
      .\Test-AuthorizationSystem.ps1 -Network FISC -SimReplaceCard:$false  (send the templates' own cards, default $true)
      .\Test-AuthorizationSystem.ps1 -Network FISC -SimIncludeChip      (also send the chip templates; 102/232 declines expected)
      .\Test-AuthorizationSystem.ps1 -Network ICASH -SessionCount 4 -TimeoutSec 30
      .\Test-AuthorizationSystem.ps1 -Network IPASS -AutoSend -RepeatCount 10 -IntervalMs 100  (Group 2: send all templates without the window)

    Binaries are resolved from publish\win-x64 (services, WPF simulators) and publish\cross-platform
    (ConsoleStressSimulator). Missing binaries are reported with the publish command to run.
    Exit codes: 0 = finished from the menu, 1..6 = number of the step that failed.
#>
param(
    [ValidateSet('NCCC', 'FISC', 'ICASH', 'IPASS', 'SVCS')]
    [string]$Network,
    [int]$TimeoutSec = 20,
    [int]$SessionCount = 2,
    # Group 1 only: test items ConsoleStressSimulator generates (each GW connection gets its own copy).
    [int]$SimCount = 100,
    # Group 1 only: delay in ms between messages on each GW connection (0 = as fast as possible).
    # The GW holds at most one in-flight request per COLA channel, so bursts beyond that are answered with RC 96.
    [ValidateRange(0, [int]::MaxValue)]
    [int]$SimInterval = 0,
    # Group 1 only: replace the template card numbers with random cards from CardXmlFilePath.xml
    # ($false = send each template's own card, useful to tell card-pool declines from real ones).
    [bool]$SimReplaceCard = $true,
    # Group 1 only: with card replacement, keep the chip templates ConsoleStressSimulator otherwise skips; they are
    # expected to decline (102/232). The HSM's ARQC check (Func07) runs only when HSMCheckFlags in
    # SystemParameterInfo.xml set IsARQCCheck=Y for the network; with N, chip templates never reach the HSM.
    [switch]$SimIncludeChip,
    # Group 2 only: WpfETicketClientSimulator sends every template (0302 excluded) -RepeatCount times,
    # -IntervalMs apart, once its sessions connect; without it, transactions are sent from its window.
    [switch]$AutoSend,
    [ValidateRange(1, [int]::MaxValue)]
    [int]$RepeatCount = 10,
    [ValidateRange(0, [int]::MaxValue)]
    [int]$IntervalMs = 100
)

# Common requires PowerShell 7 (Core edition); restart under pwsh when launched from Windows PowerShell 5.x.
# This guard must stay inline (before dot-sourcing CyberCOLA-Common.ps1).
if ($PSVersionTable.PSEdition -eq 'Desktop') {
    $pwshCmd = Get-Command pwsh -ErrorAction SilentlyContinue
    if ($pwshCmd) {
        $forward = @()
        if ($Network) { $forward += '-Network', $Network }
        & $pwshCmd.Source -NoProfile -File $MyInvocation.MyCommand.Path @forward -TimeoutSec $TimeoutSec -SessionCount $SessionCount -SimCount $SimCount -SimInterval $SimInterval -SimReplaceCard:$SimReplaceCard -SimIncludeChip:$SimIncludeChip -AutoSend:$AutoSend -RepeatCount $RepeatCount -IntervalMs $IntervalMs
        exit $LASTEXITCODE
    } else {
        Write-Warning "PowerShell 7 (pwsh.exe) is required but was not found."
        exit 1
    }
}

$ErrorActionPreference = "Stop"
$ScriptDir = $PSScriptRoot
. "$ScriptDir\CyberCOLA-Common.ps1"
$RootDir = Get-CyberCOLABaseDir

$HsmPort = 8500

# Per-network settings. TargetPortKey / DefaultSimPort: where the active GW connects to (group 1).
$NetworkCatalog = [ordered]@{
    NCCC  = @{ Group = 1; Gateway = 'CyberGWNCCC';  Auth = 'CyberAuthNCCC';  TargetPortKey = 'nccc-service-target-ports'; DefaultSimPort = 14801 }
    FISC  = @{ Group = 1; Gateway = 'CyberGWFISC';  Auth = 'CyberAuthFISC';  TargetPortKey = 'fisc-service-target-ports'; DefaultSimPort = 8100 }
    ICASH = @{ Group = 2; Gateway = 'CyberGWICASH'; Auth = 'CyberAuthICASH' }
    IPASS = @{ Group = 2; Gateway = 'CyberGWIPASS'; Auth = 'CyberAuthIPASS' }
    SVCS  = @{ Group = 2; Gateway = 'CyberGWSVCS';  Auth = 'CyberAuthSVCS' }
}

# Processes started by this script, in start order (stopped in reverse order on exit).
$Script:RunningProcesses = [System.Collections.Generic.List[object]]::new()
# Processes that were already running and are reused: shown in the status, never stopped.
$Script:ReusedProcesses = [System.Collections.Generic.List[object]]::new()
# Start of this run; the transaction summary only counts GW log lines from here on.
$Script:RunStartTime = Get-Date

# ISO 8583 response codes (85 = "no reason to decline", i.e. a verification-only approval).
$ResponseCodeText = @{
    '00' = 'Approved'; '01' = 'Refer to card issuer'; '05' = 'Do not honor'; '12' = 'Invalid transaction'
    '13' = 'Invalid amount'; '14' = 'Invalid card number'; '30' = 'Format error'; '41' = 'Lost card'
    '43' = 'Stolen card'; '51' = 'Insufficient funds'; '54' = 'Expired card'; '55' = 'Incorrect PIN'
    '57' = 'Not permitted to cardholder'; '58' = 'Not permitted to terminal'; '61' = 'Exceeds amount limit'
    '62' = 'Restricted card'; '85' = 'No reason to decline'; '91' = 'Issuer unavailable'; '96' = 'System malfunction'
}
$SuccessResponseCodes = @('00', '08', '10', '11', '85')
# FISC-specific codes. 6P is the data verification decline (CHECK_CODE 012). In smoke runs it is mostly the
# identity verification (FnNcccActVfy, reason 63): -SimReplaceCard swaps the card number but the template keeps
# its ID, birth date and phone, which then do not match the new card's holder.
$FiscResponseCodeText = @{
    '6P' = 'Data verification failed - in smoke runs mostly identity verification mismatch (reason 63, test data: template ID/DOB/phone vs replaced card)'
}

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    switch ($Level) {
        "SUCCESS" { Write-Host "[$timestamp] [SUCCESS] $Message" -ForegroundColor Green }
        "WARN"    { Write-Host "[$timestamp] [WARN]    $Message" -ForegroundColor Yellow }
        "ERROR"   { Write-Host "[$timestamp] [ERROR]   $Message" -ForegroundColor Red }
        default   { Write-Host "[$timestamp] [INFO]    $Message" -ForegroundColor Cyan }
    }
}

function Write-StepHeader([int]$Step, [int]$Total, [string]$Title) {
    Write-Host "`n==========================================================================" -ForegroundColor Cyan
    Write-Host " [Step $Step/$Total] $Title" -ForegroundColor Cyan
    Write-Host "==========================================================================" -ForegroundColor Cyan
}

function Select-Network {
    while ($true) {
        Write-Host "==========================================================================" -ForegroundColor Green
        Write-Host "   CyberCOLA Authorization System Automated Test - Select Network" -ForegroundColor Green
        Write-Host "==========================================================================" -ForegroundColor Green
        Write-Host " --- Group 1: Active Gateway (GW connects to ConsoleStressSimulator) ---" -ForegroundColor Cyan
        Write-Host "  [1] NCCC" -ForegroundColor White
        Write-Host "  [2] FISC" -ForegroundColor White
        Write-Host " --- Group 2: Passive Gateway (WpfETicketClientSimulator connects to GW) ---" -ForegroundColor Cyan
        Write-Host "  [3] ICASH" -ForegroundColor White
        Write-Host "  [4] IPASS" -ForegroundColor White
        Write-Host "  [5] SVCS" -ForegroundColor White
        Write-Host "  [Q] Quit" -ForegroundColor Yellow
        switch ((Read-Host "請選擇網路 (Select network) [1-5, Q]").Trim().ToUpper()) {
            '1' { return 'NCCC' }
            '2' { return 'FISC' }
            '3' { return 'ICASH' }
            '4' { return 'IPASS' }
            '5' { return 'SVCS' }
            'Q' { return $null }
        }
    }
}

# Resolves a published binary (.exe preferred, .dll run via dotnet as fallback). publish\ is checked
# before src\...\bin: dotnet publish leaves an intermediate build in bin\ that can be older than publish\.
function Resolve-ServiceBinary([string]$Name) {
    foreach ($p in @(
        Join-Path $RootDir "publish\win-x64\$Name\$Name.exe"
        Join-Path $RootDir "publish\cross-platform\$Name\$Name.exe"
    )) { if (Test-Path $p) { return $p } }

    $exe = Resolve-CyberCOLAExecutable -Name $Name
    if ($exe) { return $exe }

    $candidates = @(
        Join-Path $RootDir "src\CyberCOLA\$Name\bin\x64\Release\net10.0-windows\$Name.exe"
        Join-Path $RootDir "publish\cross-platform\$Name\$Name.dll"
        Join-Path $RootDir "publish\win-x64\$Name\$Name.dll"
        Join-Path $RootDir "publish\$Name\$Name.dll"
    )
    foreach ($p in $candidates) { if (Test-Path $p) { return $p } }
    return $null
}

function Get-PublishHint([string]$Name) {
    # ConsoleStressSimulator too: Resolve-ServiceBinary prefers publish\win-x64, so a cross-platform
    # publish would leave an older win-x64 copy in use.
    return "pwsh tools\Publish-CyberCOLAServices.ps1 -SingleProjectName $Name -TargetRuntime win-x64"
}

# Starts a published binary unless a process with that name is already running (then it is reused
# and not tracked, so it survives this script's teardown). Returns the process, or $null on failure.
function Start-TrackedProcess {
    param([string]$Name, [string]$Arguments, [switch]$Hidden)

    $existing = Get-Process -Name $Name -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($existing) {
        Write-Log "$Name is already running (PID: $($existing.Id)); reusing it (it will not be stopped on exit)." "WARN"
        $existing | Add-Member -NotePropertyName "ServiceName" -NotePropertyValue $Name -Force
        if (-not ($Script:ReusedProcesses | Where-Object Id -eq $existing.Id)) { $Script:ReusedProcesses.Add($existing) }
        return $existing
    }

    $path = Resolve-ServiceBinary $Name
    if (-not $path) {
        Write-Log "Published binary for $Name not found. Publish it first: $(Get-PublishHint $Name)" "ERROR"
        return $null
    }

    $startArgs = @{ PassThru = $true; WorkingDirectory = (Split-Path $path) }
    if ($path.EndsWith('.exe', [StringComparison]::OrdinalIgnoreCase)) {
        $startArgs.FilePath = $path
        if ($Arguments) { $startArgs.ArgumentList = $Arguments }
    } else {
        $startArgs.FilePath = "dotnet"
        $startArgs.ArgumentList = "`"$path`" $Arguments".TrimEnd()
    }
    if ($Hidden) { $startArgs.WindowStyle = "Hidden" }

    Write-Log "Launching $Name from: $path $Arguments" "INFO"
    $proc = Start-Process @startArgs
    $proc | Add-Member -NotePropertyName "ServiceName" -NotePropertyValue $Name -Force
    $Script:RunningProcesses.Add($proc)
    return $proc
}

# Starts a service and waits for the ports it binds (from its appsettings.json); fails if it exits.
function Start-ServiceStep {
    param([int]$Step, [int]$Total, [string]$Name)

    Write-StepHeader $Step $Total "Starting $Name..."
    $proc = Start-TrackedProcess -Name $Name -Hidden
    if (-not $proc) { return $false }

    $ports = @(Get-CyberCOLAExpectedPorts -Name $Name | Select-Object -Unique)
    if ($ports.Count -gt 0) {
        if (-not (Wait-CyberCOLAPorts -Description "$Name ports $($ports -join ', ')" -Ports $ports -ProcessName $Name -TimeoutSec $TimeoutSec)) {
            if ($proc.HasExited) {
                Write-Log "$Name exited before opening its ports. Check its log under LogFiles." "ERROR"
                return $false
            }
            # Some listeners open only after an upstream link is up; a live process is not a failure.
            Write-Log "$Name is running but not all of ports $($ports -join ', ') are listening yet; continuing." "WARN"
        }
    } else {
        Start-Sleep -Seconds 3
        if ($proc.HasExited) {
            Write-Log "$Name exited immediately (exit code $($proc.ExitCode)). Check its log under LogFiles." "ERROR"
            return $false
        }
    }
    Write-Log "$Name started (PID: $($proc.Id))." "SUCCESS"
    return $true
}

# First port of the GW's "<net>-service-target-ports" (any section), else the catalog default.
function Get-GatewayTargetPort([hashtable]$Net) {
    $json = Read-CyberCOLAJsonWithComments (Join-Path $RootDir "src\CyberCOLA\$($Net.Gateway)\appsettings.json")
    if ($json) {
        foreach ($section in $json.PSObject.Properties) {
            $value = $section.Value.($Net.TargetPortKey)
            if ($value) {
                $ports = @(ConvertTo-CyberCOLAPortList -Value $value)
                if ($ports.Count -gt 0) { return [int]$ports[0] }
            }
        }
    }
    return $Net.DefaultSimPort
}

function Start-HsmStep([int]$Step, [int]$Total) {
    Write-StepHeader $Step $Total "Starting HSM Simulator (port $HsmPort)..."
    if (Test-CyberCOLAPortListening -Port $HsmPort) {
        Write-Log "Port $HsmPort is already listening; skipping HSM start." "INFO"
        return $true
    }
    # Same launch path as Start-NcccWebSimulatorTest.ps1: the simulator menu copies the HSM config.
    & "$ScriptDir\COLASimulatorMainMenu.ps1" -AutoSelect "7" | Out-Host
    if (-not (Wait-CyberCOLAPorts -Description "HSM Simulator port $HsmPort" -Ports @($HsmPort) -TimeoutSec $TimeoutSec)) {
        Write-Log "HSM Simulator did not listen on port $HsmPort." "ERROR"
        return $false
    }
    $hsm = Get-Process -Name 'WpfHSMSimulator' -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($hsm) {
        $hsm | Add-Member -NotePropertyName "ServiceName" -NotePropertyValue 'WpfHSMSimulator' -Force
        $Script:RunningProcesses.Add($hsm)
    }
    Write-Log "HSM Simulator is listening on port $HsmPort." "SUCCESS"
    return $true
}

function Start-SimulatorStep([string]$NetName, [hashtable]$Net, [int]$Step, [int]$Total) {
    if ($Net.Group -eq 1) {
        $simPort = Get-GatewayTargetPort $Net
        Write-StepHeader $Step $Total "Starting ConsoleStressSimulator ($NetName, listen port $simPort, $SimCount items per connection, interval ${SimInterval}ms, replace card $SimReplaceCard, include chip $SimIncludeChip)..."
        $chipArg = if ($SimIncludeChip) { ' --include-chip-templates' } else { '' }
        $proc = Start-TrackedProcess -Name 'ConsoleStressSimulator' -Arguments "--type $NetName --port $simPort --count $SimCount --interval $SimInterval --replace-card $($SimReplaceCard.ToString().ToLower())$chipArg --auto-start"
        if (-not $proc) { return $false }
        if (-not (Wait-CyberCOLAPorts -Description "ConsoleStressSimulator port $simPort" -Ports @($simPort) -ProcessName 'ConsoleStressSimulator' -TimeoutSec $TimeoutSec)) {
            Write-Log "ConsoleStressSimulator did not listen on port $simPort (or exited)." "ERROR"
            return $false
        }
        Write-Log "ConsoleStressSimulator started (PID: $($proc.Id)); $($Net.Gateway) connects to port $simPort." "SUCCESS"
        return $true
    }

    $simName = 'WpfETicketClientSimulator'
    Write-StepHeader $Step $Total "Starting $simName ($NetName, $SessionCount sessions)..."
    $exe = Resolve-ServiceBinary $simName
    if (-not $exe) {
        Write-Log "Published binary for $simName not found. Publish it first: $(Get-PublishHint $simName)" "ERROR"
        return $false
    }
    # The simulator reads appsettings.json from its own folder; use the per-network copy (as COLATestFlowMenu.ps1 does).
    $cfg = Join-Path $RootDir "publish\Config\$simName\$NetName\appsettings.json"
    if (Test-Path $cfg) {
        Copy-Item $cfg (Join-Path (Split-Path $exe) "appsettings.json") -Force
        Write-Log "Applied simulator config: $cfg" "INFO"
    } else {
        Write-Log "Simulator config not found ($cfg); using the existing appsettings.json." "WARN"
    }
    $simArgs = "$SessionCount AUTO $NetName"
    if ($AutoSend) { $simArgs += " SEND $RepeatCount $IntervalMs" }
    $proc = Start-TrackedProcess -Name $simName -Arguments $simArgs
    if (-not $proc) { return $false }
    Start-Sleep -Seconds 3
    if ($proc.HasExited) {
        Write-Log "$simName exited immediately." "ERROR"
        return $false
    }
    if ($AutoSend) {
        Write-Log "$simName started (PID: $($proc.Id)); it sends every template $RepeatCount time(s), $IntervalMs ms apart, once connected." "SUCCESS"
    } else {
        Write-Log "$simName started (PID: $($proc.Id)); send transactions from its window." "SUCCESS"
    }
    return $true
}

function Show-ProcessStatus {
    $rows = Get-CyberCOLAFastTCPTable
    foreach ($p in @($Script:ReusedProcesses) + @($Script:RunningProcesses)) {
        $label = if ($Script:ReusedProcesses.Contains($p)) { "$($p.ServiceName) (reused)" } else { $p.ServiceName }
        if ($p.HasExited) {
            Write-Host ("   - {0,-35} PID {1,-6} STOPPED" -f $label, $p.Id) -ForegroundColor Red
            continue
        }
        $mine = @($rows | Where-Object OwningProcess -eq $p.Id)
        $listen = ($mine | Where-Object State -eq 'Listen' | ForEach-Object { "$($_.LocalAddress):$($_.LocalPort)" }) -join ', '
        if (-not $listen) { $listen = '-' }
        $established = @($mine | Where-Object State -eq 'Established').Count
        Write-Host ("   - {0,-35} PID {1,-6} Listen [{2}]  Established: {3}" -f $label, $p.Id, $listen, $established) -ForegroundColor Green
    }
}

# Snapshot of every TCP socket of the running chain processes. Returns $null when none is running.
# Rows carry the peer process for local connections and whether the socket is the server side.
function Get-ChainSockets {
    $chain = @(@($Script:ReusedProcesses) + @($Script:RunningProcesses) | Where-Object { -not $_.HasExited })
    if ($chain.Count -eq 0) { return $null }
    $pidMap = @{}
    $order = @{}
    for ($i = 0; $i -lt $chain.Count; $i++) {
        $pidMap[$chain[$i].Id] = $chain[$i].ServiceName
        $order[$chain[$i].ServiceName] = $i
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

    # Listening ports per owning process: a connection on one of these is the server side.
    $listenByPid = @{}
    foreach ($row in $table) {
        if ("$($row.State)" -ne 'Listen') { continue }
        $ownerPid = [int]$row.OwningProcess
        if (-not $listenByPid.ContainsKey($ownerPid)) { $listenByPid[$ownerPid] = @{} }
        $listenByPid[$ownerPid][[int]$row.LocalPort] = $true
    }

    $rows = foreach ($row in $table) {
        $ownerPid = [int]$row.OwningProcess
        if (-not $pidMap.ContainsKey($ownerPid)) { continue }
        $state = "$($row.State)"
        $peer = ''
        $peerIsChain = $false
        if ($state -ne 'Listen' -and $localIps -contains "$($row.RemoteAddress)" -and $portOwner.ContainsKey([int]$row.RemotePort)) {
            $peerPid = $portOwner[[int]$row.RemotePort]
            $peerIsChain = $pidMap.ContainsKey($peerPid)
            $peer = if ($peerIsChain) { $pidMap[$peerPid] } else { (Get-Process -Id $peerPid -ErrorAction SilentlyContinue).ProcessName }
        }
        [pscustomobject]@{
            Process       = $pidMap[$ownerPid]
            PID           = $ownerPid
            Local         = "$($row.LocalAddress):$($row.LocalPort)"
            Foreign       = "$($row.RemoteAddress):$($row.RemotePort)"
            State         = if ($stateNames.ContainsKey($state)) { $stateNames[$state] } else { $state.ToUpper() }
            Port          = [int]$row.LocalPort
            RemoteAddress = "$($row.RemoteAddress)"
            RemotePort    = [int]$row.RemotePort
            Peer          = $peer
            PeerIsChain   = $peerIsChain
            IsServerSide  = $listenByPid.ContainsKey($ownerPid) -and $listenByPid[$ownerPid].ContainsKey([int]$row.LocalPort)
        }
    }
    return @{
        Rows  = @($rows | Sort-Object { $order[$_.Process] }, { $_.State -ne 'LISTENING' }, Port, Foreign)
        Order = $order
    }
}

# Every TCP socket of the chain processes, with the process on the other end of local connections.
# Same layout as Start-NcccWebSimulatorTest.ps1's Sockets view.
function Show-SocketConnections([string]$NetName) {
    $info = Get-ChainSockets
    if (-not $info) {
        Write-Host "No authorization chain process is running." -ForegroundColor Yellow
        return
    }
    $rows = $info.Rows
    $order = $info.Order
    if ($rows.Count -eq 0) {
        Write-Host "The chain processes have no TCP sockets." -ForegroundColor Yellow
        return
    }

    $wLocal = [math]::Max(15, ($rows.Local | Measure-Object -Property Length -Maximum).Maximum)
    $wForeign = [math]::Max(15, ($rows.Foreign | Measure-Object -Property Length -Maximum).Maximum)
    $wProc = [math]::Max(7, ($rows.Process | Measure-Object -Property Length -Maximum).Maximum)
    $fmt = "  {0,-5} {1,-$wLocal} {2,-$wForeign} {3,-12} {4,-7} {5,-$wProc} {6}"

    # Center the title in '=' so the banner is as wide as the column header below it.
    $header = $fmt -f 'Proto', 'Local Address', 'Foreign Address', 'State', 'PID', 'Process', 'Peer'
    $title = " $NetName Socket Connections ($(Get-Date -Format 'HH:mm:ss')) "
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
}

# Connection diagram: one "client ---[ n ]---> server :port" line per link, built from ESTABLISHED
# sockets. Links between two chain processes are counted once, from the client side.
function Show-SocketDiagram([string]$NetName) {
    $info = Get-ChainSockets
    if (-not $info) {
        Write-Host "No authorization chain process is running." -ForegroundColor Yellow
        return
    }
    $order = $info.Order
    $wellKnown = @{ 1433 = 'SQL Server'; 8500 = 'HSM'; 9901 = 'ServiceRouter'; 9902 = 'ServiceRouter' }

    $edges = @{}
    foreach ($r in ($info.Rows | Where-Object State -eq 'ESTABLISHED')) {
        if ($r.IsServerSide) {
            # The chain client of this link is counted from its own row.
            if ($r.PeerIsChain) { continue }
            $from = if ($r.Peer) { $r.Peer } else { "[remote] $($r.RemoteAddress)" }
            $to = $r.Process; $port = $r.Port; $kind = if ($r.Peer) { 'local' } else { 'remote' }
        } else {
            $from = $r.Process; $port = $r.RemotePort
            if ($r.PeerIsChain) { $to = $r.Peer; $kind = 'chain' }
            elseif ($r.Peer) { $to = $r.Peer; $kind = 'local' }
            else { $to = "[remote] $($r.RemoteAddress)"; $kind = 'remote' }
        }
        $key = "$from|$to|$port"
        if (-not $edges.ContainsKey($key)) {
            $edges[$key] = [pscustomobject]@{ From = $from; To = $to; Port = $port; Kind = $kind; Count = 0 }
        }
        $edges[$key].Count++
    }

    # Sort by the server's position in the chain (external targets last), then by client.
    $rank = { param($name) if ($order.ContainsKey($name)) { $order[$name] } else { 999 } }
    $lines = @($edges.Values | Sort-Object { & $rank $_.To }, { & $rank $_.From }, Port)

    $wFrom = [math]::Max(6, ($lines.From | Measure-Object -Property Length -Maximum).Maximum)
    $title = " $NetName Socket Connection Diagram ($(Get-Date -Format 'HH:mm:ss')) "
    Write-Host ""
    Write-Host (('=' * 10) + $title + ('=' * 10)) -ForegroundColor Cyan
    Write-Host ("  {0,-$wFrom}   {1,-16}   {2}" -f 'Client', 'ESTABLISHED', 'Server :Port') -ForegroundColor Cyan
    if ($lines.Count -eq 0) {
        Write-Host "  (no established connections)" -ForegroundColor Yellow
    }
    foreach ($e in $lines) {
        $hint = if ($e.Kind -eq 'remote' -and $wellKnown.ContainsKey($e.Port)) { "  ($($wellKnown[$e.Port]))" } else { '' }
        $color = switch ($e.Kind) { 'chain' { 'Green' } 'remote' { 'Cyan' } default { 'Gray' } }
        $arrow = "---[{0,4} ]--->" -f $e.Count
        Write-Host ("  {0,-$wFrom}   {1,-16}   {2} :{3}{4}" -f $e.From, $arrow, $e.To, $e.Port, $hint) -ForegroundColor $color
    }

    # Chain listeners nobody is connected to (e.g. a GW whose peer has not connected yet).
    $connected = @{}
    foreach ($e in $lines) { $connected["$($e.To)|$($e.Port)"] = $true }
    $idle = @($info.Rows | Where-Object { $_.State -eq 'LISTENING' -and -not $connected.ContainsKey("$($_.Process)|$($_.Port)") } |
        ForEach-Object { "$($_.Process) :$($_.Port)" } | Select-Object -Unique)
    if ($idle.Count -gt 0) {
        Write-Host ""
        Write-Host "  Listening with no connections:" -ForegroundColor Yellow
        foreach ($i in $idle) { Write-Host "    - $i" -ForegroundColor Yellow }
    }
    Write-Host ""
    Write-Host "  Green = between chain processes, Cyan = remote host, Gray = other local process." -ForegroundColor DarkGray
}

# Summarises this run from the GW log: requests from the simulator, responses sent back (by response
# code and MTI, with the GW-side cause of RC 96) and requests left unanswered. Only MTIs and response
# codes are printed - the log lines also carry card data, which is never echoed.
function Show-TransactionSummary([string]$NetName, [hashtable]$Net) {
    # GW logger name of the external (simulator-facing) session; FISC has none and is summarised separately.
    $sessionTag = @{ NCCC = 'NcccSession'; ICASH = 'CyberGWICASHSession'; IPASS = 'CyberGWIPASSSession'; SVCS = 'CyberGWSVCSSession' }[$NetName]
    if (-not $sessionTag -and $NetName -ne 'FISC') {
        Write-Log "Transaction summary does not support $NetName yet." "WARN"
        return
    }

    # Log directory comes from the GW's own NLog.config (LogDirectory variable, ${shortdate} per day).
    $dirTemplate = $null
    $gwBin = Resolve-ServiceBinary $Net.Gateway
    $nlog = if ($gwBin) { Join-Path (Split-Path $gwBin) 'NLog.config' }
    if ($nlog -and (Test-Path $nlog)) {
        $m = Select-String -Path $nlog -Pattern '^\s*<variable\s+name="LogDirectory"\s+value="([^"]+)"' | Select-Object -First 1
        if ($m) { $dirTemplate = $m.Matches[0].Groups[1].Value }
    }
    if (-not $dirTemplate) { $dirTemplate = Join-Path $RootDir "LogFiles\$NetName\$($Net.Gateway)\`${shortdate}" }
    $logFile = Join-Path $dirTemplate.Replace('${shortdate}', $Script:RunStartTime.ToString('yyyy-MM-dd')) "$($Net.Gateway).log"
    if (-not (Test-Path $logFile)) { Write-Log "GW log not found: $logFile" "ERROR"; return }

    $since = '[' + $Script:RunStartTime.ToString('yyyy-MM-dd HH:mm:ss')
    if ($NetName -eq 'FISC') { Show-FiscTransactionSummary $logFile $since; return }

    # Requests are counted per log line by MTI: RelationIds are not unique for every network (e.g. IPASS
    # zero-fills field 7), so de-duplicating on them under-counted requests.
    $requestsByMti = @{}
    $responses = [System.Collections.Generic.List[object]]::new()
    $busy = @{}                                       # RelationIds answered 96 because every COLA channel was busy
    $noOnline = 0; $dropped = 0; $duplicateRelations = 0
    $reader = [System.IO.StreamReader]::new([System.IO.FileStream]::new($logFile, 'Open', 'Read', 'ReadWrite'))
    try {
        while ($null -ne ($line = $reader.ReadLine())) {
            if ($line.Length -lt $since.Length -or [string]::CompareOrdinal($line, 0, $since, 0, $since.Length) -lt 0) { continue }
            if ($line.Contains("[$sessionTag][CHANNELREAD") -and $line -match 'MTI\[(\d{4})\], RelationId\[([^\]]*)\]') {
                # Even third MTI digit = request from the simulator (echo/sign-on replies are odd).
                if ([int][string]$Matches[1][2] % 2 -eq 0) { $requestsByMti[$Matches[1]] = 1 + [int]$requestsByMti[$Matches[1]] }
            } elseif ($line.Contains("[$sessionTag][WRITE") -and $line -match 'MTI\[(\d{4})\], RelationId\[([^\]]*)\].*ResponseCode\[([^\]]*)\]') {
                if ([int][string]$Matches[1][2] % 2 -eq 1) {
                    $responses.Add([pscustomobject]@{ Mti = $Matches[1]; RelationId = $Matches[2].Trim(); Rc = $Matches[3].Trim() })
                }
            } elseif ($line.Contains("Can't find free Cola channel") -and $line -match 'RelationId\[([^\]]*)\]') {
                $busy[$Matches[1].Trim()] = $true
            } elseif ($line.Contains('No online Cola channel')) {
                $noOnline++
            } elseif ($line.Contains('Retry limit count exceeded')) {
                $dropped++
            } elseif ($line.Contains('RelationChannels[') -and $line.Contains('] have Channel')) {
                $duplicateRelations++
            }
        }
    } finally {
        $reader.Dispose()
    }

    # Pair each request MTI with its response MTI (third digit + 1, e.g. 0120 -> 0130) to find unanswered requests.
    $responsesByMti = @{}
    foreach ($r in $responses) { $responsesByMti[$r.Mti] = 1 + [int]$responsesByMti[$r.Mti] }
    $requestCount = ($requestsByMti.Values | Measure-Object -Sum).Sum
    $unanswered = foreach ($k in ($requestsByMti.Keys | Sort-Object)) {
        $respMti = $k.Substring(0, 2) + ([int][string]$k[2] + 1) + $k.Substring(3)
        $missing = $requestsByMti[$k] - [int]$responsesByMti[$respMti]
        if ($missing -gt 0) { [pscustomobject]@{ Mti = $k; Count = $missing } }
    }
    $unansweredCount = ($unanswered | Measure-Object -Property Count -Sum).Sum
    $success = @($responses | Where-Object { $SuccessResponseCodes -contains $_.Rc }).Count
    $failed = $responses.Count - $success
    $pct = { param($n) if ($responses.Count) { '{0,5:N1}%' -f (100.0 * $n / $responses.Count) } else { '    -' } }

    Write-Host "`n========================= Transaction Summary ($NetName) =========================" -ForegroundColor Cyan
    Write-Host "  Source : $logFile" -ForegroundColor Gray
    Write-Host "  Since  : $($Script:RunStartTime.ToString('yyyy-MM-dd HH:mm:ss')) (start of this run)" -ForegroundColor Gray
    Write-Host ("  Requests from simulator : {0,6}  ({1})" -f $requestCount, (($requestsByMti.Keys | Sort-Object | ForEach-Object { "$_ x$($requestsByMti[$_])" }) -join ', '))
    Write-Host ("  Responses to simulator  : {0,6}" -f $responses.Count)
    Write-Host ("  Success                 : {0,6}  {1}" -f $success, (& $pct $success)) -ForegroundColor Green
    Write-Host ("  Failed                  : {0,6}  {1}" -f $failed, (& $pct $failed)) -ForegroundColor $(if ($failed) { 'Red' } else { 'Green' })
    Write-Host ("  Unanswered requests     : {0,6}" -f [int]$unansweredCount) -ForegroundColor $(if ($unansweredCount) { 'Red' } else { 'Green' })
    if ($dropped) {
        Write-Host ("  Dropped by GW           : {0,6}  (response had no return channel after retries)" -f $dropped) -ForegroundColor Red
    }

    Write-Host "`n  By response code:" -ForegroundColor Cyan
    Write-Host ("  {0,-4} {1,6} {2,7}  {3,-8} {4}" -f 'RC', 'Count', 'Share', 'Result', 'Reason')
    foreach ($g in ($responses | Group-Object Rc | Sort-Object Count -Descending)) {
        $ok = $SuccessResponseCodes -contains $g.Name
        $reason = if ($ResponseCodeText.ContainsKey($g.Name)) { $ResponseCodeText[$g.Name] } else { "Network-specific code (see $NetName spec)" }
        if ($g.Name -eq '96') {
            $byBusy = @($g.Group | Where-Object { $busy.ContainsKey($_.RelationId) }).Count
            $reason += " - GW: no free COLA channel $byBusy, no online COLA channel $noOnline, other/Auth $($g.Count - $byBusy - $noOnline)"
        }
        $color = if ($ok) { 'Green' } elseif ($g.Name -eq '96') { 'Red' } else { 'Yellow' }
        Write-Host ("  {0,-4} {1,6} {2,7}  {3,-8} {4}" -f $(if ($g.Name) { $g.Name } else { '(none)' }), $g.Count, (& $pct $g.Count), $(if ($ok) { 'Success' } else { 'Failed' }), $reason) -ForegroundColor $color
    }

    Write-Host "`n  By response MTI:" -ForegroundColor Cyan
    Write-Host ("  {0,-5} {1,6} {2,8} {3,7}" -f 'MTI', 'Total', 'Success', 'Failed')
    foreach ($g in ($responses | Group-Object Mti | Sort-Object Name)) {
        $s = @($g.Group | Where-Object { $SuccessResponseCodes -contains $_.Rc }).Count
        Write-Host ("  {0,-5} {1,6} {2,8} {3,7}" -f $g.Name, $g.Count, $s, ($g.Count - $s))
    }

    if ($unansweredCount) {
        Write-Host "`n  Unanswered requests by MTI:" -ForegroundColor Red
        foreach ($u in $unanswered) { Write-Host ("  {0,-5} {1,6}" -f $u.Mti, $u.Count) -ForegroundColor Red }
    }
    if ($dropped) {
        Write-Host "`n  Hint: $duplicateRelations duplicate RelationId registrations - two in-flight requests had the same RelationId" -ForegroundColor DarkGray
        Write-Host "        (fields 7/11/37 or 36/41), so the GW lost the return channel of one and dropped its response." -ForegroundColor DarkGray
    }
    if ($busy.Count) {
        Write-Host "`n  Hint: RC 96 from 'no free COLA channel' means the simulator sent faster than the Auth service answered;" -ForegroundColor DarkGray
        $slower = if ($Net.Group -eq 1) { "this run used -SimInterval $SimInterval ms; rerun with a larger value" } else { 'send with a longer interval from the simulator window' }
        Write-Host "        $slower to measure authorization results rather than GW back-pressure." -ForegroundColor DarkGray
    }
}

# The FISC GW does not log its simulator-side session, so this summary is built from what it does log:
# requests forwarded to / answered by the Auth service (FiscColaSession), RC 96 generated by the GW,
# responses delivered back to the simulator or dropped, and messages the GW could not decode.
function Show-FiscTransactionSummary([string]$LogFile, [string]$Since) {
    $toAuth = 0; $busy = 0; $noOnline = 0; $delivered = 0; $dropped = 0; $decodeFailed = 0; $duplicateRelations = 0
    $toAuthNoReply = @{}                              # response-type MTIs (e.g. 0310) forwarded to Auth: no reply expected
    $authResponses = [System.Collections.Generic.List[object]]::new()
    $reader = [System.IO.StreamReader]::new([System.IO.FileStream]::new($LogFile, 'Open', 'Read', 'ReadWrite'))
    try {
        while ($null -ne ($line = $reader.ReadLine())) {
            if ($line.Length -lt $Since.Length -or [string]::CompareOrdinal($line, 0, $Since, 0, $Since.Length) -lt 0) { continue }
            if ($line.Contains('[FiscColaSession][WRITE') -and $line -match 'MTI\[(\d{4})\]') {
                if ([int][string]$Matches[1][2] % 2 -eq 0) { $toAuth++ } else { $toAuthNoReply[$Matches[1]] = 1 + [int]$toAuthNoReply[$Matches[1]] }
            } elseif ($line.Contains('[FiscColaSession][CHANNELREAD') -and $line -match 'MTI\[(\d{4})\].*ResponseCode\[([^\]]*)\]') {
                if ([int][string]$Matches[1][2] % 2 -eq 1) { $authResponses.Add([pscustomobject]@{ Mti = $Matches[1]; Rc = $Matches[2].Trim() }) }
            } elseif ($line.Contains("Can't find free Cola channel")) {
                $busy++
            } elseif ($line.Contains('No online Cola channel')) {
                $noOnline++
            } elseif ($line.Contains('1. CyberGWFISC DispatchTask Get channel: ') -and $line -match 'Get channel: \d.*MTI\[(\d{4})\]') {
                # Logged on every dispatch attempt; only the attempt that found the simulator channel has an address.
                if ([int][string]$Matches[1][2] % 2 -eq 1) { $delivered++ }
            } elseif ($line.Contains('Retry limit count exceeded')) {
                $dropped++
            } elseif ($line.Contains('Content decode failed')) {
                $decodeFailed++
            } elseif ($line.Contains('CyberGWFISC RelationChannels[')) {
                $duplicateRelations++
            }
        }
    } finally {
        $reader.Dispose()
    }

    $gw96 = $busy + $noOnline
    $produced = $authResponses.Count + $gw96
    $success = @($authResponses | Where-Object { $SuccessResponseCodes -contains $_.Rc }).Count
    $failed = $produced - $success
    $pct = { param($n) if ($produced) { '{0,5:N1}%' -f (100.0 * $n / $produced) } else { '    -' } }

    Write-Host "`n========================= Transaction Summary (FISC) =========================" -ForegroundColor Cyan
    Write-Host "  Source : $LogFile" -ForegroundColor Gray
    Write-Host "  Since  : $($Since.TrimStart('[')) (start of this run)" -ForegroundColor Gray
    Write-Host "  Note   : the FISC GW does not log the simulator-side session; counts come from the GW/Auth side." -ForegroundColor DarkGray
    $noReplyTotal = ($toAuthNoReply.Values | Measure-Object -Sum).Sum
    Write-Host ("  Messages seen by GW         : {0,6}  (requests to Auth {1}, answered 96 by GW {2}, not decodable {3}, no reply expected {4})" -f ($toAuth + $gw96 + $decodeFailed + $noReplyTotal), $toAuth, $gw96, $decodeFailed, $noReplyTotal)
    Write-Host ("  Responses produced          : {0,6}  (from Auth {1}, from GW {2})" -f $produced, $authResponses.Count, $gw96)
    Write-Host ("  Success                     : {0,6}  {1}" -f $success, (& $pct $success)) -ForegroundColor Green
    Write-Host ("  Failed                      : {0,6}  {1}" -f $failed, (& $pct $failed)) -ForegroundColor $(if ($failed) { 'Red' } else { 'Green' })
    Write-Host ("  Delivered to simulator      : {0,6}" -f $delivered) -ForegroundColor Green
    Write-Host ("  Dropped (no return channel) : {0,6}" -f $dropped) -ForegroundColor $(if ($dropped) { 'Red' } else { 'Green' })
    Write-Host ("  No answer from Auth         : {0,6}" -f [Math]::Max(0, $toAuth - $authResponses.Count)) -ForegroundColor $(if ($toAuth -gt $authResponses.Count) { 'Red' } else { 'Green' })
    Write-Host ("  Not decodable by GW         : {0,6}" -f $decodeFailed) -ForegroundColor $(if ($decodeFailed) { 'Red' } else { 'Green' })
    foreach ($k in ($toAuthNoReply.Keys | Sort-Object)) {
        Write-Host ("  Sent as {0} (a response MTI) : {1,6}  forwarded to Auth, which does not answer it - check the test templates" -f $k, $toAuthNoReply[$k]) -ForegroundColor Yellow
    }

    Write-Host "`n  By response code:" -ForegroundColor Cyan
    Write-Host ("  {0,-4} {1,6} {2,7}  {3,-8} {4}" -f 'RC', 'Count', 'Share', 'Result', 'Reason')
    foreach ($g in ($authResponses | Group-Object Rc | Sort-Object Count -Descending)) {
        $ok = $SuccessResponseCodes -contains $g.Name
        $reason = if ($FiscResponseCodeText.ContainsKey($g.Name)) { $FiscResponseCodeText[$g.Name] }
                  elseif ($ResponseCodeText.ContainsKey($g.Name)) { $ResponseCodeText[$g.Name] }
                  else { 'Network-specific code (see FISC spec)' }
        Write-Host ("  {0,-4} {1,6} {2,7}  {3,-8} {4} (from Auth)" -f $g.Name, $g.Count, (& $pct $g.Count), $(if ($ok) { 'Success' } else { 'Failed' }), $reason) -ForegroundColor $(if ($ok) { 'Green' } else { 'Yellow' })
    }
    if ($gw96) {
        Write-Host ("  {0,-4} {1,6} {2,7}  {3,-8} {4}" -f '96', $gw96, (& $pct $gw96), 'Failed', "System malfunction - GW: no free COLA channel $busy, no online COLA channel $noOnline") -ForegroundColor Red
    }

    # Duplicates alone are expected: the GW's own 0800 echo/sign-on reuses a fixed STAN on every channel.
    if ($dropped) {
        Write-Host "`n  Hint: $duplicateRelations duplicate RelationId registrations - the same STAN/RRN was in flight twice, so" -ForegroundColor DarkGray
        Write-Host "        the GW overwrote the return channel and later responses could not be routed back (dropped)." -ForegroundColor DarkGray
    }
}

function Show-OperationMenu([string]$NetName, [hashtable]$Net) {
    $simName = if ($Net.Group -eq 1) { 'ConsoleStressSimulator' } else { 'WpfETicketClientSimulator' }
    while ($true) {
        Clear-Host
        Write-Host "==========================================================================" -ForegroundColor Green
        Write-Host "  CyberCOLA $NetName Authorization Test Manager (Group $($Net.Group)) - Control Menu" -ForegroundColor Green
        Write-Host "==========================================================================" -ForegroundColor Green
        Write-Host " [Authorization chain processes]:" -ForegroundColor Cyan
        Show-ProcessStatus
        Write-Host "--------------------------------------------------------------------------" -ForegroundColor Yellow
        Write-Host "  [1] 重新整理狀態 (Refresh Status & Socket Ports)" -ForegroundColor Yellow
        Write-Host "  [2] 重啟 $simName (Restart Simulator)" -ForegroundColor Yellow
        Write-Host "  [3] 顯示所有 Socket 連線狀態 (Show All Socket Connections)" -ForegroundColor Yellow
        Write-Host "  [4] 顯示 Socket 連線關係圖 (Show Socket Connection Diagram)" -ForegroundColor Yellow
        Write-Host "  [5] 交易結果統計 (Transaction Summary: success / failed / reason)" -ForegroundColor Yellow
        Write-Host "  [9] 停止所有已啟動服務並離開 (Stop Started Services & Exit)" -ForegroundColor Red
        Write-Host "--------------------------------------------------------------------------" -ForegroundColor Yellow

        switch ((Read-Host "請選擇操作選項 [1, 2, 3, 4, 5, 9]").Trim()) {
            "5" {
                Show-TransactionSummary $NetName $Net
                Write-Host "`nPress any key to return to the menu..." -ForegroundColor Gray
                try { [void][System.Console]::ReadKey($true) } catch { }
            }
            "4" {
                Show-SocketDiagram $NetName
                Write-Host "`nPress any key to return to the menu..." -ForegroundColor Gray
                [void][System.Console]::ReadKey($true)
            }
            "3" {
                Show-SocketConnections $NetName
                Write-Host "`nPress any key to return to the menu..." -ForegroundColor Gray
                [void][System.Console]::ReadKey($true)
            }
            "2" {
                $old = @($Script:RunningProcesses | Where-Object { $_.ServiceName -eq $simName })
                foreach ($p in $old) {
                    if (-not $p.HasExited) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue; $p.WaitForExit(5000) | Out-Null }
                    $null = $Script:RunningProcesses.Remove($p)
                }
                $null = Start-SimulatorStep $NetName $Net 1 1
                Write-Host "`nPress any key to return to the menu..." -ForegroundColor Gray
                [void][System.Console]::ReadKey($true)
            }
            "9" { return }
        }
    }
}

function Stop-StartedProcesses {
    if ($Script:RunningProcesses.Count -eq 0) { return }
    Write-Log "Stopping processes started by this script..." "WARN"
    for ($i = $Script:RunningProcesses.Count - 1; $i -ge 0; $i--) {
        $p = $Script:RunningProcesses[$i]
        try {
            if (-not $p.HasExited) {
                Write-Log "Terminating $($p.ServiceName) (PID: $($p.Id))..." "WARN"
                Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
            }
        } catch { }
    }
    $Script:RunningProcesses.Clear()
    Write-Log "All started processes stopped." "SUCCESS"
}

# ------------------------------------------------------------------------------
# Main workflow
# ------------------------------------------------------------------------------
if (-not $Network) { $Network = Select-Network }
if (-not $Network) { exit 0 }
$net = $NetworkCatalog[$Network]
$total = if ($net.Group -eq 1) { 6 } else { 5 }

Write-Host "==========================================================================" -ForegroundColor Green
Write-Host "   CyberCOLA $Network Authorization System Automated Test (Group $($net.Group))" -ForegroundColor Green
Write-Host "==========================================================================" -ForegroundColor Green

# Returns 0 when the run ends from the menu, otherwise the number of the failed step.
function Invoke-TestFlow {
    # 1. Database (Test-DatabaseConnection.ps1 also runs the GlobalProtect pre-check when the DB is unreachable)
    Write-StepHeader 1 $total "Checking Database Connection..."
    $global:LASTEXITCODE = 0
    & "$ScriptDir\Test-DatabaseConnection.ps1" | Out-Host
    if ($LASTEXITCODE -ne 0) { Write-Log "Database connection failed. Aborting." "ERROR"; return 1 }

    # 2. ServiceRouter, 3. Gateway (the Auth service connects to the GW's COLA port, so GW goes first)
    if (-not (Start-ServiceStep 2 $total 'ServiceRouter')) { return 2 }
    if (-not (Start-ServiceStep 3 $total $net.Gateway)) { return 3 }

    $step = 4
    if ($net.Group -eq 1) {
        # CyberAuthNCCC and CyberAuthFISC both require the HSM (port 8500) at startup.
        if (-not (Start-HsmStep $step $total)) { return $step }
        $step++
    }
    if (-not (Start-ServiceStep $step $total $net.Auth)) { return $step }
    $step++

    if (-not (Start-SimulatorStep $Network $net $step $total)) { return $step }

    Write-Host "`nAll steps completed. Opening the control menu..." -ForegroundColor Green
    Start-Sleep -Seconds 2
    Show-OperationMenu $Network $net
    return 0
}

$failedStep = 0
try {
    $failedStep = Invoke-TestFlow
}
finally {
    if ($failedStep -gt 0) {
        Write-Host "`nPress any key to stop the started processes and exit..." -ForegroundColor Gray
        try { [void][System.Console]::ReadKey($true) } catch { }
    }
    Stop-StartedProcesses
}
exit $failedStep
