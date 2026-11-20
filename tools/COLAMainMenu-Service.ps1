<#
    CyberCOLA .NET 10 Management Menu (SERVICE VERSION)
    Advanced TCP Connection Tracking
#>

$ScriptName = $MyInvocation.MyCommand.Name
$FATAL_Directory = "C:/LogFiles/CyberCOLA/Fatal/Log"
$ScriptDir = $PSScriptRoot
. "$ScriptDir\CyberCOLA-Common.ps1"

# Adjust host console window size for optimal display if running in a standard ConsoleHost
Set-CyberCOLAConsoleSize

# Check for Administrator privileges

$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "==================================================" -ForegroundColor Red
    Write-Host " [ERROR] ADMINISTRATOR PRIVILEGES REQUIRED" -ForegroundColor Red
    Write-Host " This script manages Windows Services and MUST" -ForegroundColor Red
    Write-Host " be run as an Administrator." -ForegroundColor Red
    Write-Host "==================================================" -ForegroundColor Red
    Write-Host "Please restart PowerShell as Administrator."
    exit
}

# Define services and configuration mappings
$ServiceConfigs = @(
    @{ Name = "CyberAuthFISC";      Key = "cola-service-bind-port"; Section = "ColaServiceConfig" },
    @{ Name = "CyberAuthICASH";     Key = "cola-service-bind-port"; Section = "ColaServiceConfig" },
    @{ Name = "CyberAuthIPASS";     Key = "cola-service-bind-port"; Section = "ColaServiceConfig" },
    @{ Name = "CyberAuthNCCC";      Key = "cola-service-bind-port"; Section = "ColaServiceConfig" },
    @{ Name = "CyberAuthSVCS";      Key = "cola-service-bind-port"; Section = "ColaServiceConfig" },
    @{ Name = "CyberGWFISC";        Key = "cola-service-bind-port"; Section = "ColaServiceConfig" },
    @{ Name = "CyberGWICASH";       Key = "cola-service-bind-port"; Section = "ColaServiceConfig" },
    @{ Name = "CyberGWIPASS";       Key = "cola-service-bind-port"; Section = "ColaServiceConfig" },
    @{ Name = "CyberGWNCCC";        Key = "cola-service-bind-port"; Section = "ColaServiceConfig" },
    @{ Name = "CyberGWSVCS";        Key = "cola-service-bind-port"; Section = "ColaServiceConfig" },
    @{ Name = "CyberGWFISCIDVerify"; Key = "cola-service-bind-port"; Section = "ColaServiceConfig" },
    @{ Name = "CyberGWNCCCQRCode";  Key = "cola-service-bind-port"; Section = "ColaServiceConfig" },
    @{ Name = "ServiceRouter";      Key = "service-router-info-xml-file-path"; Section = "GeneralConfig" }
)

# Static cache to store service port mapping to avoid repetitive appsettings.json disk I/O and JSON parsing
$script:PortCache = @{}


function Show-FirstBank-MainMenu {
    Clear-Host
    $dtString = "Current Time: $(Get-Date -format 'yyyy-MM-dd HH:mm:ss')"
    $padding = [math]::Max(0, [int]((50 - $dtString.Length) / 2))
    $centeredDT = (" " * $padding) + $dtString

    Write-Host "==================================================" -ForegroundColor Cyan
    Write-Host "   CyberCOLA .NET 10 System Main Menu (SERVICE)" -ForegroundColor Cyan
    Write-Host $centeredDT -ForegroundColor Gray
    Write-Host "==================================================" -ForegroundColor Cyan
    Write-Host " 0. Start NCCC & QRCode Service  |  1. Stop NCCC & QRCode Service"
    Write-Host " 2. Start FISC Service           |  3. Stop FISC Service"
    Write-Host " 4. Start SVCS Service           |  5. Stop SVCS Service"
    Write-Host " 6. Start ICASH Service          |  7. Stop ICASH Service"
    Write-Host " 8. Start IPASS Service          |  9. Stop IPASS Service"
    Write-Host "10. Start ALL Services           | 11. Stop ALL Services"
    Write-Host "12. Stop ServiceRouter           | 13. Get TCP Connection Summary"
    Write-Host "--------------------------------------------------"
    Write-Host " C. Publish & Copy Config        |  D. Delete All Logs"
    Write-Host " F. Get Fatal Messages           |  S. Display Service Status"
    Write-Host " T. Get TCP Detailed Connections |  I. Install Manager"
    Write-Host " Q. Quit" -ForegroundColor Yellow
    Write-Host "==================================================" -ForegroundColor Cyan
}

function Get-TCPStatus {
    param([bool]$SummaryOnly = $false)
    
    $title = if ($SummaryOnly) { "Summary Report" } else { "Detailed Report" }
    $headerText = " Full System TCP Connection $title "
    $padLength = [math]::Max(0, [int]((90 - $headerText.Length) / 2))
    $headerLine = ("-" * $padLength) + $headerText + ("-" * (90 - $padLength - $headerText.Length))
    Write-Host "`n$headerLine" -ForegroundColor Cyan
    Write-Host ("{0,-32} | {1,-6} | {2}" -f "Service Name", "Port", "Status / Connections") -ForegroundColor Gray
    Write-Host ("-" * 90) -ForegroundColor Gray

    # Define all TCP configs including microservices and simulators
    $TCPConfigs = @(
        # --- FISC Services ---
        @{ Name = "CyberAuthFISC";      Key = "cola-service-bind-port"; Section = "ColaServiceConfig" },
        @{ Name = "CyberGWFISC";        Key = "cola-service-bind-port"; Section = "ColaServiceConfig" },
        @{ Name = "CyberGWFISCIDVerify"; Key = "cola-service-bind-port"; Section = "ColaServiceConfig" },
        @{ Name = "WpfHighStressSimulator"; DisplayName = "WpfHighStressSim (FISC)"; Key = "ListenPortList"; Section = "GeneralConfig"; SubFolder = "Fisc"; DefaultPort = 8100 },
        @{ Name = "WpfHighStressSimulator"; DisplayName = "WpfHighStressSim (FISC IDVerify)"; Key = "ListenPortList"; Section = "GeneralConfig"; SubFolder = "FiscIDVerify"; DefaultPort = 9100 },
     
        # --- NCCC Services ---
        @{ Name = "CyberAuthNCCC";      Key = "cola-service-bind-port"; Section = "ColaServiceConfig" },
        @{ Name = "CyberGWNCCC";        Key = "cola-service-bind-port"; Section = "ColaServiceConfig" },
        @{ Name = "CyberGWNCCCQRCode";  Key = "cola-service-bind-port"; Section = "ColaServiceConfig" },
        @{ Name = "WpfHighStressSimulator"; DisplayName = "WpfHighStressSim (NCCC)"; Key = "ListenPortList"; Section = "GeneralConfig"; SubFolder = "Nccc"; DefaultPort = 14801 },
        @{ Name = "WpfHighStressSimulator"; DisplayName = "WpfHighStressSim (NCCC QRCode)"; Key = "ListenPortList"; Section = "GeneralConfig"; SubFolder = "NcccQRCode"; DefaultPort = 14851 },

        # --- ICASH Services ---
        @{ Name = "CyberAuthICASH";     Key = "cola-service-bind-port"; Section = "ColaServiceConfig" },
        @{ Name = "CyberGWICASH";       Key = "cola-service-bind-port"; Section = "ColaServiceConfig" },
        @{ Name = "WpfETicketClientSimulator"; DisplayName = "WpfETicketSim (ICASH)"; Key = "ICASHETicketLocalPort"; Section = "GeneralConfig"; SubFolder = "ICASH"; DefaultPort = 3200 },

        # --- IPASS Services ---
        @{ Name = "CyberAuthIPASS";     Key = "cola-service-bind-port"; Section = "ColaServiceConfig" },
        @{ Name = "CyberGWIPASS";       Key = "cola-service-bind-port"; Section = "ColaServiceConfig" },
        @{ Name = "WpfETicketClientSimulator"; DisplayName = "WpfETicketSim (IPASS)"; Key = "IPASSETicketLocalPort"; Section = "GeneralConfig"; SubFolder = "IPASS"; DefaultPort = 3300 },

        # --- SVCS Services ---
        @{ Name = "CyberAuthSVCS";      Key = "cola-service-bind-port"; Section = "ColaServiceConfig" },
        @{ Name = "CyberGWSVCS";        Key = "cola-service-bind-port"; Section = "ColaServiceConfig" },
        @{ Name = "WpfETicketClientSimulator"; DisplayName = "WpfETicketSim (SVCS)"; Key = "SVCSETicketLocalPort"; Section = "GeneralConfig"; SubFolder = "SVCS"; DefaultPort = 3100 },

        # --- Other Services ---
        @{ Name = "CyberSMS";           Key = "cola-service-bind-port"; Section = "ColaServiceConfig" },
        @{ Name = "ServiceRouter";      Key = "service-router-info-xml-file-path"; Section = "GeneralConfig" },
        @{ Name = "WpfSMSSimulator";    DisplayName = "WpfSMSSimulator"; Key = "server-service-listen-port"; Section = "GeneralConfig"; DefaultPort = 10000 },
        @{ Name = "WpfSMSWebApiServer"; DisplayName = "WpfSMSWebApiServer"; Key = ""; Section = ""; DefaultPort = 7293 }
    )

    # Performance Optimization: Query all TCP connections once using high-performance .NET API
    Write-Host "Fetching network connection states (.NET)..." -ForegroundColor DarkGray -NoNewline
    $props = [System.Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties()
    $activeConns = $props.GetActiveTcpConnections()
    $listeners = $props.GetActiveTcpListeners()
    Write-Host " [Done]" -ForegroundColor Gray

    foreach ($svc in $TCPConfigs) {
        $ports = Get-CyberCOLAServicePort -Name $svc.Name -Section $svc.Section -Key $svc.Key -SubFolder $svc.SubFolder -DefaultPort $svc.DefaultPort -Cache $script:PortCache
        $displayName = if ($svc.DisplayName) { $svc.DisplayName } else { $svc.Name }

        foreach ($port in $ports) {
            # Filter in memory using high-performance .NET properties
            $conns = $activeConns | Where-Object { $_.LocalEndPoint.Port -eq $port -and $_.State -eq 'Established' }
            $listen = $listeners | Where-Object { $_.Port -eq $port }
            
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

function Manage-ServiceGroup {
    param([string[]]$Names, [string]$Action)
    foreach ($name in $Names) {
        $svc = Get-Service -Name $name -ErrorAction SilentlyContinue
        if ($svc) {
            Write-Host ("{0} service: {1}..." -f $Action, $name) -ForegroundColor Gray
            if ($Action -eq "START") { Start-Service $name } else { Stop-Service $name }
        } else { Write-Warning "Service $name not installed." }
    }
}

function Start-ServiceRouter-If-Needed {
    $router = Get-Service -Name "ServiceRouter" -ErrorAction SilentlyContinue
    if ($router -and $router.Status -ne 'Running') {
        Write-Host "Starting ServiceRouter..." -ForegroundColor Cyan
        Start-Service "ServiceRouter"
    }
}

do {
    Show-FirstBank-MainMenu
    Write-Host "Please make a selection (Service Mode) ? " -ForegroundColor Red -NoNewline
    $userInput = (Read-Host).ToUpper()

    switch ($userInput) {
        '0' { Start-ServiceRouter-If-Needed; Manage-ServiceGroup @("CyberAuthNCCC", "CyberGWNCCC", "CyberGWNCCCQRCode") "START" }
        '1' { Manage-ServiceGroup @("CyberAuthNCCC", "CyberGWNCCC", "CyberGWNCCCQRCode") "STOP" }
        '2' { Start-ServiceRouter-If-Needed; Manage-ServiceGroup @("CyberAuthFISC", "CyberGWFISC", "CyberGWFISCIDVerify") "START" }
        '3' { Manage-ServiceGroup @("CyberAuthFISC", "CyberGWFISC", "CyberGWFISCIDVerify") "STOP" }
        '4' { Start-ServiceRouter-If-Needed; Manage-ServiceGroup @("CyberAuthSVCS", "CyberGWSVCS") "START" }
        '5' { Manage-ServiceGroup @("CyberAuthSVCS", "CyberGWSVCS") "STOP" }
        '6' { Start-ServiceRouter-If-Needed; Manage-ServiceGroup @("CyberAuthICASH", "CyberGWICASH") "START" }
        '7' { Manage-ServiceGroup @("CyberAuthICASH", "CyberGWICASH") "STOP" }
        '8' { Start-ServiceRouter-If-Needed; Manage-ServiceGroup @("CyberAuthIPASS", "CyberGWIPASS") "START" }
        '9' { Manage-ServiceGroup @("CyberAuthIPASS", "CyberGWIPASS") "STOP" }
        '10' { Start-ServiceRouter-If-Needed; Manage-ServiceGroup ($ServiceConfigs.Name | Where-Object {$_ -ne "ServiceRouter"}) "START" }
        '11' { # Stop All
            Write-Host "Stopping all CyberCOLA services (Excluding ServiceRouter)..." -ForegroundColor Yellow
            Manage-ServiceGroup ($ServiceConfigs.Name | Where-Object {$_ -ne "ServiceRouter"}) "STOP"
        }
        '12' { 
            Write-Host "Stopping ServiceRouter..." -ForegroundColor Yellow
            Stop-Service -Name "ServiceRouter" -ErrorAction SilentlyContinue
        }
        '13' { Get-TCPStatus -SummaryOnly $true }
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
            continue
        }
        'D' { $confirm = Read-Host "Delete ALL Logs? [Y/N]"; if ($confirm -eq "Y") { Remove-Item "C:\LogFiles\*" -Recurse -Force -ErrorAction SilentlyContinue } }
        'F' { 
            # Filter out MaintGateway logs as they are managed in the MaintGateway menu
            $path = Join-Path $FATAL_Directory (Get-Date -format 'yyyy-MM-dd')
            if (Test-Path $path) { 
                $files = Get-ChildItem $path | Where-Object { $_.Name -notlike "*MaintGateway*" }
                if ($files) {
                    $files | ForEach-Object { Write-Host "`n>>> $($_.Name) <<<" -ForegroundColor Yellow; Get-Content $_.FullName -Tail 5 }
                } else {
                    Write-Host "No non-MaintGateway Fatal logs today." -ForegroundColor Green
                }
            } else { Write-Host "No Fatal logs today." }
        }
        'S' {
            Write-Host "`n--- Services Status ---" -ForegroundColor Cyan
            Get-Service -Name "Cyber*", "ServiceRouter" -ErrorAction SilentlyContinue | ForEach-Object { 
                $color = if ($_.Status -eq 'Running') { "Green" } else { "Red" }
                Write-Host ("{0,-30} : {1}" -f $_.Name, $_.Status) -ForegroundColor $color
            }
        }
        'T' { Get-TCPStatus -SummaryOnly $false }
        'I' { & "$ScriptDir\Install-CyberCOLAServices.ps1" }
        'Q' { return }
    }
    Write-Host "`nPress any key to continue..." -ForegroundColor Red -NoNewline
    $null = [Console]::ReadKey($true)
} until ($false)
