param([string]$AutoSelect)

<#
    CyberCOLA .NET 10 Integrated Test Flow Menu
    Flow: Simulator -> Gateway -> Auth
    Fixed: Precision naming for Simulators and Dynamic Port Mapping.
#>

$ScriptDir = $PSScriptRoot
. "$ScriptDir\CyberCOLA-Common.ps1"
$BaseDir = Get-CyberCOLABaseDir
$SimProjName = if ($env:COLA_SIMULATOR_TYPE) { $env:COLA_SIMULATOR_TYPE } else { "WpfStressSimulator" }
$PublishRootDir = Join-Path $BaseDir "publish\win-x64"

function Get-FatalMessages {
    # Filter out MaintGateway logs as they are managed in the MaintGateway menu
    Get-CyberCOLAFatalMessages -ExcludePattern "*MaintGateway*"
}

# --- Core launch function ---
function Start-IntegratedFlow {
    param($ModuleName, $SimProj, $SimParams, $SimCfg, $GWProcs, $AuthProcs, $SimPorts)
    
    $global:LASTEXITCODE = 0
    & "$PSScriptRoot\Test-DatabaseConnection.ps1"
    if ($LASTEXITCODE -ne 0) { return }
    
    if (-not (Get-Process "ServiceRouter" -EA SilentlyContinue)) {
        Write-Host "`n[Action] Starting ServiceRouter..." -ForegroundColor Cyan
        Manage-Proc "ServiceRouter" "START"
        Wait-CyberCOLAPorts -Description "ServiceRouter" -Ports @(9901, 9902)
    }

    Write-Host "[Step 1] Starting Simulator ($ModuleName)..." -ForegroundColor Cyan
    $simRes = Start-Sim $SimProj "$ModuleName Sim" $SimCfg $SimParams
    if (-not $simRes) { return }
    Wait-CyberCOLAPorts -Description "$SimProj" -Ports $SimPorts -ProcessName "$SimProj"

    Write-Host "[Step 2] Starting Gateways..." -ForegroundColor Cyan
    foreach ($gw in $GWProcs) { Manage-Proc $gw "START" }
    $gwPorts = @()
    foreach ($gw in $GWProcs) { $gwPorts += Get-CyberCOLAExpectedPorts -Name $gw }
    Wait-CyberCOLAPorts -Description "Gateways" -Ports $gwPorts

    Write-Host "[Step 3] Starting Auth Services..." -ForegroundColor Cyan
    foreach ($auth in $AuthProcs) { Manage-Proc $auth "START" }
    $authPorts = @()
    foreach ($auth in $AuthProcs) { $authPorts += Get-CyberCOLAExpectedPorts -Name $auth }
    Wait-CyberCOLAPorts -Description "Auth Services" -Ports $authPorts -TimeoutSec 15

    Write-Host "`n[SUCCESS] $ModuleName flow is ready." -ForegroundColor Green
    Get-FullStatus
}

# --- Status display function ---
function Get-FullStatus {
    Write-Host ""
    Write-Host ("=" * 70) -ForegroundColor Gray
    Write-Host "      INTEGRATED FLOW STATUS REPORT (Precision Monitoring)" -ForegroundColor Cyan
    Write-Host ("=" * 70) -ForegroundColor Gray
    $procs = Get-Process -Name "Cyber*", "Wpf*", "ServiceRouter" -ErrorAction SilentlyContinue
    if ($procs) { 
        $procs | Select-Object @{N="ProcessName";E={$_.ProcessName}}, Id, `
            @{N="CPU";E={ if ($_.CPU -ne $null) { [math]::Round($_.CPU, 1) } else { 0 } }}, `
            @{N="RAM(MB)";E={ if ($_.WorkingSet -ne $null) { [math]::Round($_.WorkingSet / 1MB, 1) } else { 0 } }} | Format-Table 
    }
    
    Write-Host "[TCP Network Status]" -ForegroundColor Yellow
    # Performance Optimization: Query all TCP connections once via Win32 API / optimized netstat
    Write-Host "Fetching network connection states (Fast)..." -ForegroundColor DarkGray -NoNewline
    $netConns = Get-CyberCOLAFastTCPTable
    Write-Host " [Done]" -ForegroundColor Gray
    
    # SQL
    $decryptedConn = Get-CyberCOLADatabaseConnectionString
    $sqlServer = "localhost"
    if ($decryptedConn -and ($decryptedConn -match "(?:Data Source|Server)=([^;]+)")) {
        $sqlServer = ($matches[1] -split '[,\\;]')[0].Trim()
    }
    
    $sqlStatus = "OFFLINE"
    $sqlColor = "Red"
    try {
        $tcpClient = New-Object System.Net.Sockets.TcpClient
        $connect = $tcpClient.BeginConnect($sqlServer, 1433, $null, $null)
        $wait = $connect.AsyncWaitHandle.WaitOne(300, $false) # Fast check: 300ms timeout
        if ($wait) {
            $tcpClient.EndConnect($connect)
            $sqlStatus = "ONLINE"
            $sqlColor = "Green"
        }
        $tcpClient.Close()
    } catch {
        # Keep offline if connection failed or timed out
    }
    Write-Host ("{0,-25} | {1,-6} | {2}" -f "SQL Server", "1433", $sqlStatus) -ForegroundColor $sqlColor
    
    # System service detection (dynamic)
    $allProjects = Get-ChildItem -Path (Join-Path $BaseDir "src\CyberCOLA") -Directory
    foreach ($proj in $allProjects) {
        if ($proj.Name -match "ConsoleApp|TWIN|Wpf") { continue }
        $ports = Get-CyberCOLAExpectedPorts -Name $proj.Name
        foreach ($p in $ports) {
            $listen = $netConns | Where-Object { $_.LocalPort -eq $p -and $_.State -eq 'Listen' }
            $est = $netConns | Where-Object { $_.LocalPort -eq $p -and $_.State -eq 'Established' }
            if ($listen) { 
                $type = if($p -match "6700|5504|4500"){"EXTERNAL"}else{"INTERNAL"}
                Write-Host ("{0,-25} | {1,-6} | ONLINE ($type, $($est.Count))" -f $proj.Name, $p) -ForegroundColor Green 
            }
        }
    }

    # Precise simulator detection (via Process name)
    $simNames = @($SimProjName, "WpfETicketClientSimulator", "WpfHSMSimulator")
    foreach ($sName in $simNames) {
        $p = Get-Process -Name $sName -ErrorAction SilentlyContinue
        if ($p) {
            # Calculate all current connections for this process
            $pConns = $netConns | Where-Object { $_.OwningProcess -eq $p.Id -and $_.State -eq 'Established' }
            $pListen = $netConns | Where-Object { $_.OwningProcess -eq $p.Id -and $_.State -eq 'Listen' }
            
            $status = if ($pConns.Count -gt 0) { "ONLINE (Connections: $($pConns.Count))" } elseif ($pListen) { "LISTENING" } else { "RUNNING" }
            $displayPort = if ($pListen) { $pListen[0].LocalPort } else { "---" }
            
            Write-Host ("{0,-25} | {1,-6} | $status" -f $sName, $displayPort) -ForegroundColor Cyan
        }
    }
    Write-Host ("=" * 70) -ForegroundColor Gray
}

# --- Helper methods ---
function Manage-Proc {
    param($Name, $Action)
    if ($Action -eq "START") {
        if (-not (Get-Process $Name -EA SilentlyContinue)) {
            $exe = Resolve-CyberCOLAExecutable -Name $Name
            if ($exe) { Start-Process "cmd.exe" -ArgumentList "/c title $Name & `"$exe`"" -WorkingDirectory (Split-Path $exe) -WindowStyle Minimized }
        }
    }
}

function Start-Sim {
    param($Proj, $Title, $Cfg, $Arguments)
    $exePaths = @((Join-Path $PublishRootDir "$Proj\$Proj.exe"), (Join-Path $BaseDir "src\CyberCOLA\$Proj\bin\x64\Release\net10.0-windows\$Proj.exe"))
    $exe = $null
    foreach ($p in $exePaths) { if (Test-Path $p) { $exe = $p; break } }
    if ($exe) {
        $exeDir = Split-Path $exe
        if ($Cfg) { 
            $src = Join-Path $BaseDir "publish\Config\$SimProjName\$Cfg\appsettings.json"
            if (-not (Test-Path $src)) { 
                $fallbackProj = if ($SimProjName -eq "WpfHighStressSimulator") { "WpfStressSimulator" } else { "WpfHighStressSimulator" }
                $src = Join-Path $BaseDir "publish\Config\$fallbackProj\$Cfg\appsettings.json" 
            }
            if (-not (Test-Path $src)) { $src = Join-Path $BaseDir "publish\Config\WpfETicketClientSimulator\$Cfg\appsettings.json" }
            if (Test-Path $src) { Copy-Item $src (Join-Path $exeDir "appsettings.json") -Force }
        }
        Start-Process $exe -ArgumentList $Arguments -WorkingDirectory $exeDir -WindowStyle Minimized
        return $true
    }
    return $false
}

$isFirstRun = $true
do {
    Clear-Host
    $dt = "Current Time: $(Get-Date -format 'yyyy-MM-dd HH:mm:ss')"
    $dtPad = [math]::Max(0, [int]((59 - $dt.Length) / 2))
    
    Write-Host "===========================================================" -ForegroundColor Cyan
    Write-Host "            CyberCOLA Integrated Test Flow Menu" -ForegroundColor Cyan
    Write-Host ((" " * $dtPad) + $dt) -ForegroundColor Gray
    Write-Host "===========================================================" -ForegroundColor Cyan
    Write-Host " 1. Start FISC Flow  (Sim -> GW -> Auth)"
    Write-Host " 2. Start NCCC Flow  (Sim -> GW -> Auth)"
    Write-Host " 3. Start ICASH Flow (Sim -> GW -> Auth)"
    Write-Host " 4. Start IPASS Flow (Sim -> GW -> Auth)"
    Write-Host " 5. Start SVCS Flow  (Sim -> GW -> Auth)"
    Write-Host "-----------------------------------------------------------"
    Write-Host " S. Show Current Status      |  F. Get Fatal Messages"
    Write-Host " B. Stop All Processes       |  Q. Quit (Back to Main)" -ForegroundColor Yellow
    Write-Host "===========================================================" -ForegroundColor Cyan
    
    if ($isFirstRun -and $AutoSelect) { $choice = $AutoSelect.ToUpper(); $isFirstRun = $false }
    else { $choice = (Read-Host "`nFlow > Select").ToUpper() }

    switch ($choice) {
        '1' { Start-IntegratedFlow "FISC" $SimProjName "8100 AUTO" "Fisc" @("CyberGWFISC", "CyberGWFISCIDVerify") @("CyberAuthFISC") @(8100) }
        '2' { Start-IntegratedFlow "NCCC" $SimProjName "14801 AUTO" "Nccc" @("CyberGWNCCC", "CyberGWNCCCQRCode") @("CyberAuthNCCC") @(14801) }
        '3' { Start-IntegratedFlow "ICASH" "WpfETicketClientSimulator" "2 AUTO ICASH" "ICASH" @("CyberGWICASH") @("CyberAuthICASH") @(15735) }
        '4' { Start-IntegratedFlow "IPASS" "WpfETicketClientSimulator" "2 AUTO IPASS" "IPASS" @("CyberGWIPASS") @("CyberAuthIPASS") @(15835) }
        '5' { Start-IntegratedFlow "SVCS" "WpfETicketClientSimulator" "2 AUTO SVCS" "SVCS" @("CyberGWSVCS") @("CyberAuthSVCS") @(15635) }
        'S' { Get-FullStatus }
        'F' { Get-FatalMessages }
        'B' { Stop-CyberCOLAProcesses -Names @("Cyber*", "Wpf*", "ServiceRouter") }
        'Q' { return }
    }
    if ($choice -ne "Q") { Write-Host "`nDone. Press any key..." -ForegroundColor Red; $null = [Console]::ReadKey($true) }
} while ($true)
