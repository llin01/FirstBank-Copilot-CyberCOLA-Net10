param([string]$AutoSelect)

<#
    CyberCOLA .NET 10 System Management Menu (CONSOLE)
    Fixed: Strict Database Pre-check and Exit Flow Control.
#>

$ScriptName = $MyInvocation.MyCommand.Name
$ScriptDir = $PSScriptRoot
. "$ScriptDir\CyberCOLA-Common.ps1"
$BaseDir = Get-CyberCOLABaseDir

# Adjust host console window size for optimal display if running in a standard ConsoleHost
Set-CyberCOLAConsoleSize

$PublishRootDir = Join-Path $BaseDir "publish\win-x64"

# Services and configuration mappings (single source of truth in Common)
$ServiceConfigs = Get-CyberCOLAServicePortConfig

# Static cache to store service port mapping to avoid repetitive appsettings.json disk I/O and JSON parsing
$script:PortCache = @{}


function Show-FirstBank-MainMenu {
    Clear-Host
    $dtString = "Current Time: $(Get-Date -format 'yyyy-MM-dd HH:mm:ss')"
    $padding = [math]::Max(0, [int]((70 - $dtString.Length) / 2))
    $centeredDT = (" " * $padding) + $dtString
    Write-Host "======================================================================" -ForegroundColor Cyan
    Write-Host "             CyberCOLA .NET 10 System Main Menu (CONSOLE)" -ForegroundColor Cyan
    Write-Host $centeredDT -ForegroundColor Gray
    Write-Host "======================================================================" -ForegroundColor Cyan
    Write-Host " 0. Start NCCC & QRCode Service    |  1. Stop NCCC & QRCode Service"
    Write-Host " 2. Start FISC Service             |  3. Stop FISC Service"
    Write-Host " 4. Start SVCS Service             |  5. Stop SVCS Service"
    Write-Host " 6. Start ICASH Service            |  7. Stop ICASH Service"
    Write-Host " 8. Start IPASS Service            |  9. Stop IPASS Service"
    Write-Host "10. Start ALL Services             | 11. Stop ALL Services"
    Write-Host "12. Stop ServiceRouter             | 13. Get TCP Connection Summary"
    Write-Host "14. Start FISC Multi-Node (N1+N2)  | 15. Start NCCC Multi-Node (N1+N2)" -ForegroundColor Cyan
    Write-Host "16. Start SVCS Multi-Node (N1+N2)  | 17. Start ICASH Multi-Node (N1+N2)" -ForegroundColor Cyan
    Write-Host "18. Start IPASS Multi-Node (N1+N2) |" -ForegroundColor Cyan
    Write-Host "--------------------------------------------------"
    Write-Host " C. Publish & Copy Config          |  P. Publish Single Project"
    Write-Host " D. Delete All Logs                |  S. Display Process Status"
    Write-Host " F. Get Fatal Messages             |  T. Get TCP Detailed Connections"
    Write-Host " V. Test Database Connection       |  Q. Quit (Back to Main)" -ForegroundColor Yellow
    Write-Host "======================================================================" -ForegroundColor Cyan
}

function Get-TCPStatus {
    param([bool]$SummaryOnly = $false)
    
    $title = if ($SummaryOnly) { "Summary Report" } else { "Detailed Report" }
    $headerText = " Full System TCP Connection $title "
    $padLength = [math]::Max(0, [int]((70 - $headerText.Length) / 2))
    $headerLine = ("-" * $padLength) + $headerText + ("-" * (70 - $padLength - $headerText.Length))
    Write-Host "`n$headerLine" -ForegroundColor Cyan

    # All TCP configs including microservices and simulators (single source of truth in Common)
    $TCPConfigs = Get-CyberCOLATCPServiceCatalog

    # Performance Optimization: Query all TCP connections once using high-performance .NET API
    Write-Host "Fetching network connection states (.NET)..." -ForegroundColor DarkGray -NoNewline
    $props = [System.Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties()
    $activeConns = $props.GetActiveTcpConnections()
    $listenersList = $props.GetActiveTcpListeners()
    $listeners = $listenersList | ForEach-Object { $_.Port }

    # Pre-build a port -> established-connections lookup once, instead of re-scanning $activeConns
    # with Where-Object for every port checked below (O(1) lookup vs O(n) linear scan per port).
    $establishedByPort = @{}
    foreach ($c in $activeConns) {
        if ($c.State -eq 'Established') {
            $p = $c.LocalEndPoint.Port
            if (-not $establishedByPort.ContainsKey($p)) { $establishedByPort[$p] = [System.Collections.Generic.List[object]]::new() }
            $establishedByPort[$p].Add($c)
        }
    }
    Write-Host " [Done]" -ForegroundColor Gray

    if ($SummaryOnly) {
        # Render the Multi-Node Active-Active Status Matrix
        Write-Host "======================================================================" -ForegroundColor Cyan
        Write-Host "                Multi-Node Active-Active Status Matrix" -ForegroundColor Cyan
        Write-Host "======================================================================" -ForegroundColor Cyan
        Write-Host ("{0,-7} | {1,-4} | {2,-6} | {3,-7} | {4,-6} | {5,-7} | {6}" -f "Channel", "Role", "Node 1", "Status", "Node 2", "Status", "Conns (N1/N2)") -ForegroundColor Gray
        Write-Host ("----------------------------------------------------------------------") -ForegroundColor Gray

        $MultiNodeConfigs = @(
            @{ Channel = "NCCC";  Auth1 = 10011; Auth2 = 11011; GW1 = 10001; GW2 = 11001; Sub1 = 10007; Sub2 = 11007; SubLabel = "QR" },
            @{ Channel = "FISC";  Auth1 = 10012; Auth2 = 11012; GW1 = 10002; GW2 = 11002; Sub1 = 10008; Sub2 = 11008; SubLabel = "IDV" },
            @{ Channel = "TWIN";  Auth1 = 10013; Auth2 = 11013; GW1 = 10003; GW2 = 11003 },
            @{ Channel = "SVCS";  Auth1 = 10014; Auth2 = 11014; GW1 = 10004; GW2 = 11004 },
            @{ Channel = "ICASH"; Auth1 = 10015; Auth2 = 11015; GW1 = 10005; GW2 = 11005 },
            @{ Channel = "IPASS"; Auth1 = 10016; Auth2 = 11016; GW1 = 10006; GW2 = 11006 }
        )

        foreach ($cfg in $MultiNodeConfigs) {
            # 1. Auth Row
            $n1Listen = $listeners -contains $cfg.Auth1
            $n2Listen = $listeners -contains $cfg.Auth2
            $n1Status = if ($n1Listen) { "ONLINE" } else { "OFFLINE" }
            $n2Status = if ($n2Listen) { "ONLINE" } else { "OFFLINE" }
            
            $n1Conns = $establishedByPort[$cfg.Auth1].Count
            $n2Conns = $establishedByPort[$cfg.Auth2].Count
            
            $n1StatusColor = if ($n1Listen) { "Green" } else { "Gray" }
            $n2StatusColor = if ($n2Listen) { "Green" } else { "Red" }
            
            Write-Host ("{0,-7} | {1,-4} | {2,-6} | " -f $cfg.Channel, "Auth", $cfg.Auth1) -NoNewline
            Write-Host ("{0,-7}" -f $n1Status) -ForegroundColor $n1StatusColor -NoNewline
            Write-Host (" | {0,-6} | " -f $cfg.Auth2) -NoNewline
            Write-Host ("{0,-7}" -f $n2Status) -ForegroundColor $n2StatusColor -NoNewline
            $n2ConnStr = if ($n2Listen) { $n2Conns } else { "-" }
            Write-Host (" | {0} / {1}" -f $n1Conns, $n2ConnStr)
            
            # 2. GW Row
            $n1GWListen = $listeners -contains $cfg.GW1
            $n2GWListen = $listeners -contains $cfg.GW2
            $n1GWStatus = if ($n1GWListen) { "ONLINE" } else { "OFFLINE" }
            $n2GWStatus = if ($n2GWListen) { "ONLINE" } else { "OFFLINE" }
            
            $n1GWConns = $establishedByPort[$cfg.GW1].Count
            $n2GWConns = $establishedByPort[$cfg.GW2].Count
            
            $n1GWColor = if ($n1GWListen) { "Green" } else { "Gray" }
            $n2GWColor = if ($n2GWListen) { "Green" } else { "Red" }
            
            Write-Host ("{0,-7} | {1,-4} | {2,-6} | " -f $cfg.Channel, "GW", $cfg.GW1) -NoNewline
            Write-Host ("{0,-7}" -f $n1GWStatus) -ForegroundColor $n1GWColor -NoNewline
            Write-Host (" | {0,-6} | " -f $cfg.GW2) -NoNewline
            Write-Host ("{0,-7}" -f $n2GWStatus) -ForegroundColor $n2GWColor -NoNewline
            $n2GWConnStr = if ($n2GWListen) { $n2GWConns } else { "-" }
            Write-Host (" | {0} / {1}" -f $n1GWConns, $n2GWConnStr)

            # 3. Sub GW Row (QRCode/IDVerify if present)
            if ($cfg.Sub1) {
                $n1SubListen = $listeners -contains $cfg.Sub1
                $n2SubListen = $listeners -contains $cfg.Sub2
                $n1SubStatus = if ($n1SubListen) { "ONLINE" } else { "OFFLINE" }
                $n2SubStatus = if ($n2SubListen) { "ONLINE" } else { "OFFLINE" }
                
                $n1SubConns = $establishedByPort[$cfg.Sub1].Count
                $n2SubConns = $establishedByPort[$cfg.Sub2].Count
                
                $n1SubColor = if ($n1SubListen) { "Green" } else { "Gray" }
                $n2SubColor = if ($n2SubListen) { "Green" } else { "Red" }
                
                Write-Host ("{0,-7} | {1,-4} | {2,-6} | " -f $cfg.Channel, $cfg.SubLabel, $cfg.Sub1) -NoNewline
                Write-Host ("{0,-7}" -f $n1SubStatus) -ForegroundColor $n1SubColor -NoNewline
                Write-Host (" | {0,-6} | " -f $cfg.Sub2) -NoNewline
                Write-Host ("{0,-7}" -f $n2SubStatus) -ForegroundColor $n2SubColor -NoNewline
                $n2SubConnStr = if ($n2SubListen) { $n2SubConns } else { "-" }
                Write-Host (" | {0} / {1}" -f $n1SubConns, $n2SubConnStr)
            }
            Write-Host ("----------------------------------------------------------------------") -ForegroundColor Gray
        }
        
        Write-Host "`n======================================================================" -ForegroundColor Cyan
        Write-Host "                Simulators & Miscellaneous Services Status" -ForegroundColor Cyan
        Write-Host "======================================================================" -ForegroundColor Cyan
        Write-Host ("{0,-32} | {1,-6} | {2}" -f "Service Name", "Port", "Status / Connections") -ForegroundColor Gray
        Write-Host ("-" * 70) -ForegroundColor Gray
    } else {
        Write-Host ("{0,-32} | {1,-6} | {2}" -f "Service Name", "Port", "Status / Connections") -ForegroundColor Gray
        Write-Host ("-" * 70) -ForegroundColor Gray
    }

    foreach ($svc in $TCPConfigs) {
        # In summary mode, skip services already represented in the Multi-Node Matrix
        if ($SummaryOnly -and ($svc.Name -like "CyberAuth*" -or $svc.Name -like "CyberGW*")) {
            continue
        }

        $ports = Get-CyberCOLAServicePort -Name $svc.Name -Section $svc.Section -Key $svc.Key -SubFolder $svc.SubFolder -DefaultPort $svc.DefaultPort -Cache $script:PortCache
        $displayName = if ($svc.DisplayName) { $svc.DisplayName } else { $svc.Name }

        foreach ($port in $ports) {
            # Filter in memory using high-performance .NET properties
            $conns = $establishedByPort[$port]
            $listen = $listeners -contains $port
            
            $statusStr = if ($listen) { "LISTENING" } else { "OFFLINE" }
            $color = if ($listen) { "Green" } else { "Gray" }

            if ($conns) {
                if ($SummaryOnly) {
                    Write-Host ("{0,-32} | {1,-6} | Count: {2}" -f $displayName, $port, $conns.Count) -ForegroundColor Green
                } else {
                    foreach ($c in $conns) {
                        Write-Host ("{0,-32} | {1,-6} | Connected: {2}:{3} => {4}:{5}" -f $displayName, $port, $c.LocalEndPoint.Address, $c.LocalEndPoint.Port, $c.RemoteEndPoint.Address, $c.RemoteEndPoint.Port) -ForegroundColor Green
                    }
                }
            } else {
                Write-Host ("{0,-32} | {1,-6} | {2}" -f $displayName, $port, $statusStr) -ForegroundColor $color
            }
        }
    }
}

# --- Core launch function (includes strict pre-check) ---
function Invoke-ServiceGroup {
    param([string[]]$Names)
    & "$PSScriptRoot\Test-DatabaseConnection.ps1"
    if ($LASTEXITCODE -ne 0) { $global:LastOpFailed = $true; return }
    if (-not (Get-Process -Name "ServiceRouter" -EA SilentlyContinue)) {
        Manage-SingleProcess "ServiceRouter" "START"
        Start-Sleep -Seconds 2
    }
    foreach ($n in $Names) { 
        Manage-SingleProcess $n "START" 
        Start-Sleep -Seconds 2
    }
    $global:LastOpFailed = $false
}

function Manage-SingleProcess {
    param($Name, $Action, $Arguments = "")
    if ($Action -eq "START") {
        if (-not (Get-Process $Name -EA SilentlyContinue)) {
            $exe = Resolve-CyberCOLAExecutable -Name $Name
            if ($exe) {
                Write-Host "Starting: $Name $Arguments..." -ForegroundColor Green
                Start-Process "cmd.exe" -ArgumentList "/c title $Name & `"$exe`" $Arguments" -WorkingDirectory (Split-Path $exe) -WindowStyle Minimized
            } else { Write-Warning "Binary not found for $Name" }
        }
    } else {
        Write-Host "Stopping: $Name..." -ForegroundColor Yellow
        Get-Process $Name -EA SilentlyContinue | Stop-Process -Force
    }
}

# Helper to start Node 2 of a service dynamically using command-line arguments override
function Start-Node2-Process {
    param([string]$Name, [string]$Node2Args, [string]$Node2Title = "")

    # 1. Resolve executable path
    $exe = Resolve-CyberCOLAExecutable -Name $Name

    # 2. Launch process
    if ($exe) {
        $title = if ($Node2Title) { $Node2Title } else { "${Name}_Node2" }
        Start-Process "cmd.exe" -ArgumentList "/c title $title & `"$exe`" $Node2Args" -WorkingDirectory (Split-Path $exe) -WindowStyle Minimized
        Write-Host "Started $Name Node 2 (Args: $Node2Args)" -ForegroundColor Green
    } else {
        Write-Warning "Binary not found for $Name"
    }
}

# Define all projects list for selective publishing
$AllProjects = @(
    "MaintGateway", "MaintGatewayHttps", "MaintGatewayTester", "MaintGatewayTesterHttps", 
    "MaintGatewayJwt", "MaintGatewayTesterJwt", "WpfMaintGatewayTester",
    "MaintGatewayUnified", "MaintGatewayUnifiedTester",
    "CyberAuthFISC", "CyberAuthICASH", "CyberAuthIPASS", "CyberAuthNCCC", "CyberAuthSVCS",
    "CyberGWFISC", "CyberGWICASH", "CyberGWIPASS", "CyberGWNCCC", "CyberGWSVCS",
    "ServiceRouter", "CyberGWFISCIDVerify", "CyberGWNCCCQRCode", "CyberSMS",
    "WpfStressSimulator", "WpfETicketClientSimulator", "WpfHighStressSimulator", "WpfHSMSimulator", "WpfEAITCPClient", "WpfSMSSimulator", "WpfSMSWebApiServer", "WebEAISimulator", "WebHSMSimulator", "WebHighStressSimulator"
)

$isFirstRun = $true
$autoExit = $false
do {
    $global:LastOpFailed = $false
    Show-FirstBank-MainMenu
    
    # Support fast auto select from parameters
    if ($isFirstRun -and $AutoSelect) {
        $userInput = $AutoSelect.ToUpper()
        Write-Host "Auto Selecting: $userInput" -ForegroundColor Green
        $isFirstRun = $false
        $autoExit = $true
    } else {
        Write-Host "Please make a selection ? " -ForegroundColor Red -NoNewline
        $userInput = (Read-Host).ToUpper()
    }
 
    switch ($userInput) {
        '0' { Invoke-ServiceGroup @("CyberAuthNCCC", "CyberGWNCCC", "CyberGWNCCCQRCode") }
        '1' { foreach($n in @("CyberGWNCCCQRCode", "CyberGWNCCC", "CyberAuthNCCC")){ Manage-SingleProcess $n "STOP" } }
        '2' { Invoke-ServiceGroup @("CyberAuthFISC", "CyberGWFISC", "CyberGWFISCIDVerify") }
        '3' { foreach($n in @("CyberGWFISCIDVerify", "CyberGWFISC", "CyberAuthFISC")){ Manage-SingleProcess $n "STOP" } }
        '4' { Invoke-ServiceGroup @("CyberAuthSVCS", "CyberGWSVCS") }
        '5' { foreach($n in @("CyberAuthSVCS", "CyberGWSVCS")){ Manage-SingleProcess $n "STOP" } }
        '6' { Invoke-ServiceGroup @("CyberAuthICASH", "CyberGWICASH") }
        '7' { foreach($n in @("CyberAuthICASH", "CyberGWICASH")){ Manage-SingleProcess $n "STOP" } }
        '8' { Invoke-ServiceGroup @("CyberAuthIPASS", "CyberGWIPASS") }
        '9' { foreach($n in @("CyberAuthIPASS", "CyberGWIPASS")){ Manage-SingleProcess $n "STOP" } }
        '10' { Invoke-ServiceGroup ($ServiceConfigs.Name | Where-Object {$_ -ne "ServiceRouter"}) }
        '11' { Stop-CyberCOLAProcesses -Names @("Cyber*") }
        '12' { Manage-SingleProcess "ServiceRouter" "STOP" }
        '13' { Get-TCPStatus -SummaryOnly $true }
        '14' {
            # FISC Multi-Node (Node 1 + Node 2)
            & "$PSScriptRoot\Test-DatabaseConnection.ps1"
            if ($LASTEXITCODE -ne 0) { $global:LastOpFailed = $true; break }
            if (-not (Get-Process -Name "ServiceRouter" -EA SilentlyContinue)) {
                Manage-SingleProcess "ServiceRouter" "START"
                Start-Sleep -Seconds 2
            }
            # Start Node 1 GW, IDVerify, Node 1 Auth
            Manage-SingleProcess "CyberGWFISC" "START" ""
            Manage-SingleProcess "CyberGWFISCIDVerify" "START" ""
            Manage-SingleProcess "CyberAuthFISC" "START" ""
            Start-Sleep -Seconds 2
            # Start Node 2 GW
            Start-Node2-Process "CyberGWFISC" "--ClientMonitorConfig:client-monitor-service-local-port=11002 --ColaServiceConfig:cola-service-bind-port=15436 --FiscServiceConfig:fisc-service-local-ports=18200,18201,18202,18203"
            # Start Node 2 Auth
            Start-Node2-Process "CyberAuthFISC" "--ClientMonitorConfig:client-monitor-service-local-port=11012 --ColaServiceConfig:cola-service-local-port-start=26100"
            Start-Sleep -Seconds 2
        }
        '15' {
            # NCCC Multi-Node (Node 1 + Node 2)
            & "$PSScriptRoot\Test-DatabaseConnection.ps1"
            if ($LASTEXITCODE -ne 0) { $global:LastOpFailed = $true; break }
            if (-not (Get-Process -Name "ServiceRouter" -EA SilentlyContinue)) {
                Manage-SingleProcess "ServiceRouter" "START"
                Start-Sleep -Seconds 2
            }
            # Start Node 1 GW, QRCode, Node 1 Auth
            Manage-SingleProcess "CyberGWNCCC" "START" ""
            Manage-SingleProcess "CyberGWNCCCQRCode" "START" ""
            Manage-SingleProcess "CyberAuthNCCC" "START" ""
            Start-Sleep -Seconds 2
            # Start Node 2 GW
            Start-Node2-Process "CyberGWNCCC" "--ClientMonitorConfig:client-monitor-service-local-port=11001 --ColaServiceConfig:cola-service-bind-port=15336"
            # Start Node 2 QRCode GW
            Start-Node2-Process "CyberGWNCCCQRCode" "--ClientMonitorConfig:client-monitor-service-local-port=11007 --ColaServiceConfig:cola-service-bind-port=15937"
            # Start Node 2 Auth
            Start-Node2-Process "CyberAuthNCCC" "--ClientMonitorConfig:client-monitor-service-local-port=11011 --ColaServiceConfig:cola-service-local-port-start=26000"
            Start-Sleep -Seconds 2
        }
        '16' {
            # SVCS Multi-Node (Node 1 + Node 2)
            & "$PSScriptRoot\Test-DatabaseConnection.ps1"
            if ($LASTEXITCODE -ne 0) { $global:LastOpFailed = $true; break }
            if (-not (Get-Process -Name "ServiceRouter" -EA SilentlyContinue)) {
                Manage-SingleProcess "ServiceRouter" "START"
                Start-Sleep -Seconds 2
            }
            # Start Node 1 GW, Node 1 Auth
            Manage-SingleProcess "CyberGWSVCS" "START" ""
            Manage-SingleProcess "CyberAuthSVCS" "START" ""
            Start-Sleep -Seconds 2
            # Start Node 2 GW
            Start-Node2-Process "CyberGWSVCS" "--ClientMonitorConfig:client-monitor-service-local-port=11004 --ColaServiceConfig:cola-service-bind-port=15636"
            # Start Node 2 Auth
            Start-Node2-Process "CyberAuthSVCS" "--ClientMonitorConfig:client-monitor-service-local-port=11014 --ColaServiceConfig:cola-service-local-port-start=26300"
            Start-Sleep -Seconds 2
        }
        '17' {
            # ICASH Multi-Node (Node 1 + Node 2)
            & "$PSScriptRoot\Test-DatabaseConnection.ps1"
            if ($LASTEXITCODE -ne 0) { $global:LastOpFailed = $true; break }
            if (-not (Get-Process -Name "ServiceRouter" -EA SilentlyContinue)) {
                Manage-SingleProcess "ServiceRouter" "START"
                Start-Sleep -Seconds 2
            }
            # Start Node 1 GW, Node 1 Auth
            Manage-SingleProcess "CyberGWICASH" "START" ""
            Manage-SingleProcess "CyberAuthICASH" "START" ""
            Start-Sleep -Seconds 2
            # Start Node 2 GW
            Start-Node2-Process "CyberGWICASH" "--ClientMonitorConfig:client-monitor-service-local-port=11005 --ColaServiceConfig:cola-service-bind-port=15736"
            # Start Node 2 Auth
            Start-Node2-Process "CyberAuthICASH" "--ClientMonitorConfig:client-monitor-service-local-port=11015 --ColaServiceConfig:cola-service-local-port-start=26400"
            Start-Sleep -Seconds 2
        }
        '18' {
            # IPASS Multi-Node (Node 1 + Node 2)
            & "$PSScriptRoot\Test-DatabaseConnection.ps1"
            if ($LASTEXITCODE -ne 0) { $global:LastOpFailed = $true; break }
            if (-not (Get-Process -Name "ServiceRouter" -EA SilentlyContinue)) {
                Manage-SingleProcess "ServiceRouter" "START"
                Start-Sleep -Seconds 2
            }
            # Start Node 1 GW, Node 1 Auth
            Manage-SingleProcess "CyberGWIPASS" "START" ""
            Manage-SingleProcess "CyberAuthIPASS" "START" ""
            Start-Sleep -Seconds 2
            # Start Node 2 GW
            Start-Node2-Process "CyberGWIPASS" "--ClientMonitorConfig:client-monitor-service-local-port=11006 --ColaServiceConfig:cola-service-bind-port=15836"
            # Start Node 2 Auth
            Start-Node2-Process "CyberAuthIPASS" "--ClientMonitorConfig:client-monitor-service-local-port=11016 --ColaServiceConfig:cola-service-local-port-start=26500"
            Start-Sleep -Seconds 2
        }
        'V' {
            & "$PSScriptRoot\Test-DatabaseConnection.ps1"
            if ($LASTEXITCODE -eq 0) {
                Write-Host "`nPress any key to return..." -ForegroundColor Yellow
                $null = [Console]::ReadKey($true)
            }
        }
        'T' { Get-TCPStatus -SummaryOnly $false }
        'C' { 
            $script:PortCache.Clear()
            & "$ScriptDir\Publish-CyberCOLAServices.ps1"
            Write-Host "`nPress 'q' or 'Q' to return to menu..." -ForegroundColor Yellow
            while ($true) {
                if ([Console]::KeyAvailable) {
                    $key = [Console]::ReadKey($true)
                    if ($key.KeyChar -in @('q', 'Q')) { break }
                }
                Start-Sleep -Milliseconds 100
            }
        }
        'P' {
            Write-Host "`n--- Select Project to Publish ---" -ForegroundColor Cyan
            for ($i = 0; $i -lt $AllProjects.Count; $i++) {
                $idx = $i + 1
                $projName = $AllProjects[$i]
                if ($i % 2 -eq 0) {
                    Write-Host (" {0,2}. {1,-30}" -f $idx, $projName) -NoNewline
                } else {
                    Write-Host (" {0,2}. {1}" -f $idx, $projName)
                }
            }
            if ($AllProjects.Count % 2 -ne 0) { Write-Host "" }
            Write-Host "  Q. Cancel" -ForegroundColor Yellow
            Write-Host "--------------------------------------------------"
            
            $projChoice = Read-Host "Select Project (1-$($AllProjects.Count) or Q, support multi-select like 2,3,6)"
            if ($projChoice -eq 'Q' -or $projChoice -eq 'q' -or [string]::IsNullOrWhiteSpace($projChoice)) {
                $global:LastOpFailed = $true
                break
            }

            # Parse selections, splitting by comma or spaces
            $choices = $projChoice -split '[, ]' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
            $selectedProjs = @()
            $hasInvalid = $false

            foreach ($c in $choices) {
                if ($c -match '^\d+$') {
                    $idx = [int]$c - 1
                    if ($idx -ge 0 -and $idx -lt $AllProjects.Count) {
                        $selectedProjs += $AllProjects[$idx]
                    } else {
                        $hasInvalid = $true
                        Write-Warning "Invalid project index: $c"
                    }
                } else {
                    $hasInvalid = $true
                    Write-Warning "Invalid input: $c"
                }
            }

            if ($hasInvalid -and $selectedProjs.Count -eq 0) {
                $global:LastOpFailed = $true
                break
            }

            if ($selectedProjs.Count -gt 0) {
                Write-Host "`nSelect Target Platform:" -ForegroundColor Cyan
                Write-Host " 1. win-x64 (Windows)"
                Write-Host " 2. linux-x64 (Linux/Systemd)"
                Write-Host " Q. Cancel" -ForegroundColor Yellow
                Write-Host "--------------------------------------------------"
                $platChoice = Read-Host "Enter choice [1-2 or Q]"
                
                if ($platChoice -eq 'Q' -or $platChoice -eq 'q' -or [string]::IsNullOrWhiteSpace($platChoice)) {
                    $global:LastOpFailed = $true
                    break
                }
                
                if ($platChoice -eq '1' -or $platChoice -eq '2') {
                    $selectedPlat = if ($platChoice -eq '1') { "win-x64" } else { "linux-x64" }
                    $script:PortCache.Clear()
                    
                    # Publish selected projects in a single run
                    & "$ScriptDir\Publish-CyberCOLAServices.ps1" -SingleProjectName $selectedProjs -TargetRuntime $selectedPlat

                    Write-Host "`nPublish Single Project complete. Press any key to return to Main Menu..." -ForegroundColor Yellow
                    $null = [Console]::ReadKey($true)
                } else {
                    Write-Warning "Invalid platform selection."
                    $global:LastOpFailed = $true
                }
            }
        }
        'D' { if ((Read-Host "Confirm delete logs? [Y/N]") -eq 'Y') { Remove-Item "$(Get-CyberCOLALogRoot)\*" -Recurse -Force -EA SilentlyContinue } }
        'F' {
            # Filter out MaintGateway logs as they are managed in the MaintGateway menu
            Get-CyberCOLAFatalMessages -ExcludePattern "*MaintGateway*"
        }
        'S' {
            Write-Host "`n--- Console Processes Status ---" -ForegroundColor Cyan
            $ServiceConfigs | ForEach-Object {
                $name = $_.Name
                $procs = Get-Process -Name $name -ErrorAction SilentlyContinue
                
                if ($procs) {
                    $status = "RUNNING"
                    $color = "Green"
                    # Support multiple processes with the same name safely
                    $pids = @()
                    $totalMem = 0
                    foreach ($p in $procs) {
                        $pids += $p.Id
                        $totalMem += $p.WorkingSet
                    }
                    $pidStr = $pids -join ','
                    $memMb = [math]::Round($totalMem / 1MB, 2)
                    Write-Host ("{0,-30} : {1} (PIDs: {2}, RAM: {3}MB)" -f $name, $status, $pidStr, $memMb) -ForegroundColor $color
                } else {
                    $status = "OFFLINE"
                    $color = "Gray"
                    Write-Host ("{0,-30} : {1}" -f $name, $status) -ForegroundColor $color
                }
            }
        }
        'Q' { return }
    }

    if ($autoExit) {
        return
    }
    if ($global:LastOpFailed) {
        Write-Host "`n[ERROR] Database connection failed! Returning to Main Menu..." -ForegroundColor Red
        Start-Sleep -Seconds 2
        return
    }
    if ($userInput -ne "Q" -and $userInput -ne "C") { 
        Write-Host "`nDone. Press any key..." -ForegroundColor Red
        $null = [Console]::ReadKey($true)
    }
} until ($false)
