param([string]$AutoSelect)

# CyberCOLA MaintGateway Unified Management Tool (HTTP/HTTPS)
# Path: tools/Start-MaintGatewayTest.ps1

. "$PSScriptRoot\CyberCOLA-Common.ps1"

$ErrorActionPreference = "Continue"

# --- [Path Definitions - Pointing to Publish] ---
$BaseDir = Get-CyberCOLABaseDir
$PublishDir = Get-CyberCOLAPublishDir

function Get-FatalMessages {
    # Only read MaintGateway related Fatal logs to filter out unrelated core services
    Get-CyberCOLAFatalMessages -IncludePattern "*MaintGateway*"
}

# Simulator Configuration
$SimName = "WpfStressSimulator"
$SimExe = Join-Path $PublishDir "$SimName\$SimName.exe"
$SimConfig = "Nccc" # Fixed to NCCC case as requested
$SimPort = 14801 # Synchronized with COLASimulatorMainMenu.ps1 NCCC Port

# Function to get process details and network status
function Get-NetStatus {
    param([int[]]$Ports)
    # Performance Optimization: Query all TCP connections once using high-performance .NET API
    $props = [System.Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties()
    $activeConns = $props.GetActiveTcpConnections()
    $listeners = $props.GetActiveTcpListeners()
    
    foreach ($p in $Ports) {
        $listen = $listeners | Where-Object { $_.Port -eq $p }
        $est = $activeConns | Where-Object { $_.LocalEndPoint.Port -eq $p -and $_.State -eq 'Established' }
        $status = if ($listen) { "LISTENING" } else { "OFFLINE" }
        if ($est) { $status += " (Established: $($est.Count))" }
        $color = if ($listen) { "Green" } else { "Gray" }
        Write-Host "  Port $p : $status" -ForegroundColor $color
    }
}

function Wait-ForPort($port, $timeout = 30) {
    $hostname = "CFLin-NB-Test"
    Write-Host "  > Waiting for $hostname`:$port to be ready..." -NoNewline
    $start = Get-Date
    while ($true) {
        $socket = New-Object System.Net.Sockets.TcpClient
        try {
            $connect = $socket.BeginConnect($hostname, $port, $null, $null)
            $wait = $connect.AsyncWaitHandle.WaitOne(500, $false)
            if ($wait -and $socket.Connected) {
                $socket.EndConnect($connect); $socket.Close()
                Write-Host " [OK]" -ForegroundColor Green
                return $true
            }
        } catch { 
            $lastError = $_.Exception.Message
            Write-Debug "Connection attempt failed - $lastError"
        }
        
        if ((Get-Date) -gt $start.AddSeconds($timeout)) {
            $ipList = [System.Net.Dns]::GetHostAddresses($hostname) | ForEach-Object { $_.IPAddressToString }
            Write-Host " [Timeout]" -ForegroundColor Red
            Write-Host "    (Diagnostics: Target=$hostname, IPs=$($ipList -join ','), Port=$port)" -ForegroundColor Yellow
            if ($lastError) {
                Write-Host "    (Last Connection Error - $lastError)" -ForegroundColor Yellow
            }
            return $false
        }
        Write-Host "." -NoNewline
        Start-Sleep -Milliseconds 1000
    }
}

function Stop-Everything {
    Write-Host "`n--- Stopping all MaintGateway & Simulator processes ---" -ForegroundColor Yellow
    Stop-CyberCOLAProcesses -Names @("MaintGateway", "MaintGatewayHttps", "MaintGatewayTester", "MaintGatewayTesterHttps", "MaintGatewayJwt", "MaintGatewayTesterJwt", "MaintGatewayUnified", "MaintGatewayUnifiedTester", "WpfStressSimulator", "WpfMaintGatewayTester")
    Write-Host "All processes stopped." -ForegroundColor Green
}

function Show-Status($Mode) {
    Write-Host "`n--- [System Process Status] ---" -ForegroundColor Cyan
    $filter = if ($Mode -eq "HTTPS") { "MaintGatewayHttps|WpfStressSimulator" } elseif ($Mode -eq "JWT") { "MaintGatewayJwt|WpfStressSimulator" } else { "MaintGateway|WpfStressSimulator" }
    $procs = Get-Process | Where-Object { $_.Name -match $filter -and $_.Name -notmatch "Tester" }
    $testers = Get-Process | Where-Object { $_.Name -match "MaintGatewayTester" }
    
    if ($procs -or $testers) {
        ($procs + $testers) | Select-Object Name, Id, @{N="CPU";E={[math]::Round($_.CPU,1)}}, @{N="RAM(MB)";E={[math]::Round($_.WorkingSet / 1MB, 2)}} | Format-Table -AutoSize
    } else {
        Write-Host "No relevant services running." -ForegroundColor Gray
    }

    Write-Host "[Network Status]" -ForegroundColor Yellow
    if ($Mode -eq "HTTPS" -or $Mode -eq "JWT") { Get-NetStatus @(5000, 5001, 5005, $SimPort) }
    else { Get-NetStatus @(5000, 5005, $SimPort) }
}

function Clean-AllLogs {
    Write-Host "--- Cleaning all logs ---" -ForegroundColor Yellow
    # Stop all processes to release file locks
    Stop-Everything

    $targets = @("MaintGateway", "MaintGatewayHttps", "MaintGatewayTester", "MaintGatewayTesterHttps", "MaintGatewayJwt", "MaintGatewayTesterJwt", "MaintGatewayUnified", "MaintGatewayUnifiedTester", "WpfStressSimulator", "WpfMaintGatewayTester")
    
    # 1. Clean central log directory
    $logRoot = Get-CyberCOLALogRoot
    foreach ($t in $targets) {
        $path = Join-Path $logRoot $t
        if (Test-Path $path) {
            Remove-Item "$path\*" -Recurse -Force -ErrorAction SilentlyContinue
            Write-Host "  - Cleaned Centralized: $t" -ForegroundColor Gray
        }
    }

    # 2. Clean local logs in publish directory (publish\win-x64\...\Log)
    foreach ($t in $targets) {
        $localLogPath = Join-Path $PublishDir "$t\Log"
        if (Test-Path $localLogPath) {
            Remove-Item "$localLogPath\*" -Recurse -Force -ErrorAction SilentlyContinue
            Write-Host "  - Cleaned Local: $t" -ForegroundColor Gray
        }
    }
    Write-Host "Log cleanup completed." -ForegroundColor Green
}

function Clean-RelateLogs($Mode) {
    $gwName = if ($Mode -eq "HTTPS") { "MaintGatewayHttps" } elseif ($Mode -eq "JWT") { "MaintGatewayJwt" } else { "MaintGateway" }
    $tstName = if ($Mode -eq "HTTPS") { "MaintGatewayTesterHttps" } elseif ($Mode -eq "JWT") { "MaintGatewayTesterJwt" } else { "MaintGatewayTester" }
    
    $targets = @($SimName, $gwName, $tstName, "WpfMaintGatewayTester")
    
    Write-Host "`n--- Cleaning related logs for $Mode Mode (No Stop Process) ---" -ForegroundColor Yellow

    $logRoot = Get-CyberCOLALogRoot
    foreach ($t in $targets) {
        $pathsToClean = @(
            (Join-Path $logRoot $t),
            (Join-Path $PublishDir "$t\Log")
        )
        
        foreach ($dirPath in $pathsToClean) {
            if (Test-Path $dirPath) {
                $files = Get-ChildItem -Path $dirPath -File -Recurse -ErrorAction SilentlyContinue
                if ($files) {
                    foreach ($file in $files) {
                        try {
                            Remove-Item $file.FullName -Force -ErrorAction Stop
                            Write-Host "  - [DELETED] $($file.FullName)" -ForegroundColor Green
                        } catch {
                            Write-Host "  - [IN USE / LOCKED] $($file.FullName) (Error - $($_.Exception.Message))" -ForegroundColor Yellow
                        }
                    }
                }
            }
        }
    }
    Write-Host "Related log cleanup completed." -ForegroundColor Green
}

function Start-ChromeTest($Mode) {
    $url = "http://localhost:5005/api/test/send-xml"
           
    Write-Host "`nLaunching Chrome with URL: $url" -ForegroundColor Cyan
    try {
        Start-Process "chrome.exe" -ArgumentList $url -ErrorAction Stop
    } catch {
        Write-Warning "Chrome not found or failed to launch. Falling back to default browser."
        Start-Process $url
    }
}

function Ensure-BinariesPublished($gwExe, $tstExe, $wpfTstExe) {
    if (-not (Test-Path $SimExe) -or -not (Test-Path $gwExe) -or -not (Test-Path $tstExe) -or -not (Test-Path $wpfTstExe)) {
        Write-Host "`n[WARNING] One or more required publish binaries are missing." -ForegroundColor Yellow
        Write-Host "  Missing binaries might prevent the services from starting." -ForegroundColor Yellow
        $pubChoice = Read-Host "Would you like to build and publish WebAPI services now? [Y/N] (Default: Y)"
        if ($pubChoice -eq "" -or $pubChoice -eq "Y" -or $pubChoice -eq "y") {
            Write-Host "Launching Publish Script..." -ForegroundColor Cyan
            & "$PSScriptRoot\Publish-CyberCOLAServices.ps1"
        }
    }
}

function Start-Services($Mode) {
    Stop-Everything
    
    $gwName = if ($Mode -eq "HTTPS") { "MaintGatewayHttps" } elseif ($Mode -eq "JWT") { "MaintGatewayJwt" } else { "MaintGateway" }
    $gwExe = Join-Path $PublishDir "$gwName\$gwName.exe"
    $tstName = if ($Mode -eq "HTTPS") { "MaintGatewayTesterHttps" } elseif ($Mode -eq "JWT") { "MaintGatewayTesterJwt" } else { "MaintGatewayTester" }
    $tstExe = Join-Path $PublishDir "$tstName\$tstName.exe"
    
    $wpfTstName = "WpfMaintGatewayTester"
    $wpfTstExe = Join-Path $PublishDir "$wpfTstName\$wpfTstName.exe"
    
    Ensure-BinariesPublished $gwExe $tstExe $wpfTstExe
    
    Write-Host "`n--- Starting Full Stack ($Mode Mode) ---" -ForegroundColor Cyan
    
    # 1. Start Simulator with NCCC Config
    if (Test-Path $SimExe) {
        Write-Host "  [1/3] Starting $SimName (NCCC Case)..."
        # Copy NCCC config before start
        $srcCfg = Join-Path $BaseDir "publish\Config\$SimName\$SimConfig\appsettings.json"
        if (Test-Path $srcCfg) { Copy-Item $srcCfg (Join-Path (Split-Path $SimExe) "appsettings.json") -Force }
        
        Start-Process $SimExe -ArgumentList "$SimPort AUTO" -WorkingDirectory (Split-Path $SimExe) -WindowStyle Minimized
        Wait-ForPort $SimPort
    } else {
        Write-Host "  [!] Simulator Exe not found at: $SimExe" -ForegroundColor Red
        return
    }

    # 2. Start Gateway
    $gwPort = if ($Mode -eq "HTTPS" -or $Mode -eq "JWT") { 5001 } else { 5000 }
    
    if (Test-Path $gwExe) {
        Write-Host "  [2/3] Starting $gwName ($gwPort)..."
        Start-Process "cmd.exe" -ArgumentList "/c title $gwName & `"$gwExe`"" -WorkingDirectory (Split-Path $gwExe) -WindowStyle Minimized
        Wait-ForPort $gwPort
    } else {
        Write-Host "  [!] Gateway Exe not found at: $gwExe" -ForegroundColor Red
        return
    }

    # 3. Start Tester
    $tstPort = 5005

    if (Test-Path $tstExe) {
        Write-Host "  [3/3] Starting $tstName ($tstPort)..."
        Start-Process "cmd.exe" -ArgumentList "/c title $tstName & `"$tstExe`"" -WorkingDirectory (Split-Path $tstExe) -WindowStyle Minimized
        Wait-ForPort $tstPort
    } else {
        Write-Host "  [!] Tester Exe not found at: $tstExe" -ForegroundColor Red
        return
    }

    # 4. Start WPF Tester
    if (Test-Path $wpfTstExe) {
        Write-Host "  [Extra] Starting $wpfTstName..."
        Start-Process $wpfTstExe -WorkingDirectory (Split-Path $wpfTstExe)
    } else {
        Write-Host "  [!] WPF Tester Exe not found at: $wpfTstExe" -ForegroundColor Red
    }

    Write-Host "`n[SUCCESS] $Mode services are Online!" -ForegroundColor Green
}

# --- Unified (single-project, three-mode) variants ---
# The MaintGatewayUnified / MaintGatewayUnifiedTester projects select their mode via
# ASPNETCORE_ENVIRONMENT (Http -> base appsettings.json, Https/Jwt -> appsettings.{Env}.json).
# Gateway listen ports: HTTP 5000 | HTTPS 5100+5101 | JWT 5102+5103
# Tester  listen ports: 5005       | 5006             | 5007
function Start-UnifiedServices($Mode) {
    Stop-Everything

    $gwName = "MaintGatewayUnified"
    $tstName = "MaintGatewayUnifiedTester"
    $gwExe = Join-Path $PublishDir "$gwName\$gwName.exe"
    $tstExe = Join-Path $PublishDir "$tstName\$tstName.exe"
    $wpfTstName = "WpfMaintGatewayTester"
    $wpfTstExe = Join-Path $PublishDir "$wpfTstName\$wpfTstName.exe"

    # Environment name drives appsettings.{Env}.json selection
    $envName = if ($Mode -eq "HTTPS") { "Https" } elseif ($Mode -eq "JWT") { "Jwt" } else { "Http" }

    Ensure-BinariesPublished $gwExe $tstExe $wpfTstExe

    Write-Host "`n--- Starting Unified Stack ($Mode Mode, ASPNETCORE_ENVIRONMENT=$envName) ---" -ForegroundColor Cyan

    # 1. Start Simulator with NCCC Config
    if (Test-Path $SimExe) {
        Write-Host "  [1/3] Starting $SimName (NCCC Case)..."
        $srcCfg = Join-Path $BaseDir "publish\Config\$SimName\$SimConfig\appsettings.json"
        if (Test-Path $srcCfg) { Copy-Item $srcCfg (Join-Path (Split-Path $SimExe) "appsettings.json") -Force }
        Start-Process $SimExe -ArgumentList "$SimPort AUTO" -WorkingDirectory (Split-Path $SimExe) -WindowStyle Minimized
        Wait-ForPort $SimPort
    } else {
        Write-Host "  [!] Simulator Exe not found at: $SimExe" -ForegroundColor Red
        return
    }

    # 2. Start Unified Gateway (mode selected via ASPNETCORE_ENVIRONMENT)
    $gwPort = if ($Mode -eq "HTTPS" -or $Mode -eq "JWT") { 5001 } else { 5000 }
    if (Test-Path $gwExe) {
        Write-Host "  [2/3] Starting $gwName ($gwPort, env=$envName)..."
        Start-Process "cmd.exe" -ArgumentList "/c set ASPNETCORE_ENVIRONMENT=$envName& title $gwName & `"$gwExe`"" -WorkingDirectory (Split-Path $gwExe) -WindowStyle Minimized
        Wait-ForPort $gwPort
    } else {
        Write-Host "  [!] Gateway Exe not found at: $gwExe" -ForegroundColor Red
        return
    }

    # 3. Start Unified Tester (mode selected via ASPNETCORE_ENVIRONMENT)
    $tstPort = 5005
    if (Test-Path $tstExe) {
        Write-Host "  [3/3] Starting $tstName ($tstPort, env=$envName)..."
        Start-Process "cmd.exe" -ArgumentList "/c set ASPNETCORE_ENVIRONMENT=$envName& title $tstName & `"$tstExe`"" -WorkingDirectory (Split-Path $tstExe) -WindowStyle Minimized
        Wait-ForPort $tstPort
    } else {
        Write-Host "  [!] Tester Exe not found at: $tstExe" -ForegroundColor Red
        return
    }

    # 4. Start WPF Tester
    if (Test-Path $wpfTstExe) {
        Write-Host "  [Extra] Starting $wpfTstName..."
        Start-Process $wpfTstExe -WorkingDirectory (Split-Path $wpfTstExe)
    } else {
        Write-Host "  [!] WPF Tester Exe not found at: $wpfTstExe" -ForegroundColor Red
    }

    Write-Host "`n[SUCCESS] Unified $Mode services are Online!" -ForegroundColor Green
}

function Show-UnifiedStatus($Mode) {
    Write-Host "`n--- [System Process Status] ---" -ForegroundColor Cyan
    $procs = Get-Process | Where-Object { $_.Name -eq "MaintGatewayUnified" }
    $testers = Get-Process | Where-Object { $_.Name -eq "MaintGatewayUnifiedTester" }
    $sims = Get-Process | Where-Object { $_.Name -eq $SimName }
    if ($procs -or $testers -or $sims) {
        ($procs + $testers + $sims) | Select-Object Name, Id, @{N="CPU";E={[math]::Round($_.CPU,1)}}, @{N="RAM(MB)";E={[math]::Round($_.WorkingSet / 1MB, 2)}} | Format-Table -AutoSize
    } else {
        Write-Host "No Unified services running." -ForegroundColor Gray
    }

    Write-Host "[Network Status]" -ForegroundColor Yellow
    if ($Mode -eq "HTTPS" -or $Mode -eq "JWT") { Get-NetStatus @(5000, 5001, 5005, $SimPort) }
    else { Get-NetStatus @(5000, 5005, $SimPort) }
}

function Clean-UnifiedLogs($Mode) {
    $targets = @($SimName, "MaintGatewayUnified", "MaintGatewayUnifiedTester", "WpfMaintGatewayTester")
    Write-Host "`n--- Cleaning Unified logs for $Mode Mode (No Stop Process) ---" -ForegroundColor Yellow
    $logRoot = Get-CyberCOLALogRoot
    foreach ($t in $targets) {
        $pathsToClean = @(
            (Join-Path $logRoot $t),
            (Join-Path $PublishDir "$t\Log")
        )
        foreach ($dirPath in $pathsToClean) {
            if (Test-Path $dirPath) {
                $files = Get-ChildItem -Path $dirPath -File -Recurse -ErrorAction SilentlyContinue
                if ($files) {
                    foreach ($file in $files) {
                        try {
                            Remove-Item $file.FullName -Force -ErrorAction Stop
                            Write-Host "  - [DELETED] $($file.FullName)" -ForegroundColor Green
                        } catch {
                            Write-Host "  - [IN USE / LOCKED] $($file.FullName) (Error - $($_.Exception.Message))" -ForegroundColor Yellow
                        }
                    }
                }
            }
        }
    }
    Write-Host "Unified log cleanup completed." -ForegroundColor Green
}

function Start-UnifiedChromeTest($Mode) {
    # Tester always listens on plain HTTP; only the gateway side is HTTPS in HTTPS/JWT modes
    $port = 5005
    $url = "http://localhost:$port/api/test/send-xml"
    Write-Host "`nLaunching Chrome with URL: $url" -ForegroundColor Cyan
    try {
        Start-Process "chrome.exe" -ArgumentList $url -ErrorAction Stop
    } catch {
        Write-Warning "Chrome not found or failed to launch. Falling back to default browser."
        Start-Process $url
    }
}

$isFirstRun = $true
do {
    Clear-Host
    $dt = "Current Time: $(Get-Date -format 'yyyy-MM-dd HH:mm:ss')"
    $dtPad = [math]::Max(0, [int]((54 - $dt.Length) / 2))
    $title = "MaintGateway Management"
    $titlePad = [math]::Max(0, [int]((54 - $title.Length) / 2))

    Write-Host "======================================================" -ForegroundColor Cyan
    Write-Host ((" " * $titlePad) + $title) -ForegroundColor Cyan
    Write-Host ((" " * $dtPad) + $dt) -ForegroundColor Gray
    Write-Host "======================================================" -ForegroundColor Cyan
    Write-Host " Legacy MaintGateway:"
    Write-Host "  1. HTTP  Mode (Port 5000/5005)"
    Write-Host "  2. HTTPS Mode (Port 5001/5005)"
    Write-Host "  3. JWT   Mode (Port 5001/5005)"
    Write-Host " Unified MaintGateway:"
    Write-Host "  4. HTTP  Mode (Port 5000/5005)"
    Write-Host "  5. HTTPS Mode (Port 5001/5005)"
    Write-Host "  6. JWT   Mode (Port 5001/5005)"
    Write-Host "------------------------------------------------------" -ForegroundColor Gray
    Write-Host " S. Stop All Processes       |  F. Get Fatal Messages"
    Write-Host " D. Delete All Logs          |  Q. Exit" -ForegroundColor Yellow
    Write-Host "======================================================" -ForegroundColor Cyan
    
    if ($isFirstRun -and $AutoSelect) {
        $mainChoice = $AutoSelect.ToUpper()
        $isFirstRun = $false
    } else {
        $mainChoice = (Read-Host "Select Option").ToUpper()
    }
    
    if ($mainChoice -eq 'Q') { exit }
    if ($mainChoice -eq 'S') { Stop-Everything; Start-Sleep -S 1; continue }
    if ($mainChoice -eq 'D') { Clean-AllLogs; Start-Sleep -S 1; continue }
    if ($mainChoice -eq 'F') { Get-FatalMessages; Write-Host "`nPress any key to continue..." -ForegroundColor Gray; $null = [Console]::ReadKey($true); continue }
    
    $currentMode = if ($mainChoice -eq '2') { "HTTPS" } elseif ($mainChoice -eq '3') { "JWT" } else { "HTTP" }

    # --- Unified MaintGateway (single-project, mode via ASPNETCORE_ENVIRONMENT) ---
    if ($mainChoice -eq '4' -or $mainChoice -eq '5' -or $mainChoice -eq '6') {
        $uMode = if ($mainChoice -eq '5') { "HTTPS" } elseif ($mainChoice -eq '6') { "JWT" } else { "HTTP" }

        # --- Unified Action Sub Menu ---
        do {
            Clear-Host
            $dt = "Current Time: $(Get-Date -format 'yyyy-MM-dd HH:mm:ss')"
            $dtPad = [math]::Max(0, [int]((54 - $dt.Length) / 2))
            $subTitle = "MaintGateway Unified ($uMode)"
            $subTitlePad = [math]::Max(0, [int]((54 - $subTitle.Length) / 2))
            Write-Host "======================================================" -ForegroundColor Cyan
            Write-Host ((" " * $subTitlePad) + $subTitle) -ForegroundColor Cyan
            Write-Host ((" " * $dtPad) + $dt) -ForegroundColor Gray
            Write-Host "======================================================" -ForegroundColor Cyan
            Write-Host " 1. Start Full Flow (Sim -> Unified GW -> Unified Tester)"
            Write-Host " 2. Stop All Services"
            Write-Host " 3. Show Status"
            Write-Host " 4. Clean Logs"
            Write-Host " 5. Launch Test API in Chrome"
            Write-Host "------------------------------------------------------" -ForegroundColor Gray
            Write-Host " F. Get Fatal Messages"
            Write-Host " B. Back to Mode Selection"
            Write-Host " Q. Exit" -ForegroundColor Yellow
            Write-Host "======================================================" -ForegroundColor Cyan

            $uAction = (Read-Host "Select Action").ToUpper()
            switch ($uAction) {
                "1" { Start-UnifiedServices $uMode }
                "2" { Stop-Everything }
                "3" { Show-UnifiedStatus $uMode }
                "4" { Clean-UnifiedLogs $uMode }
                "5" { Start-UnifiedChromeTest $uMode }
                "F" { Get-FatalMessages }
                "B" { break }
                "Q" { exit }
            }
            if ($uAction -ne "B") {
                Write-Host "`nPress any key to continue..." -ForegroundColor Gray
                $null = [Console]::ReadKey($true)
            }
        } while ($uAction -ne "B")
        continue
    }

    # --- Sub Menu ---
    do {
        Clear-Host
        $dt = "Current Time: $(Get-Date -format 'yyyy-MM-dd HH:mm:ss')"
        $dtPad = [math]::Max(0, [int]((54 - $dt.Length) / 2))
        $subTitle = "MaintGateway Management ($currentMode)"
        $subTitlePad = [math]::Max(0, [int]((54 - $subTitle.Length) / 2))
        
        Write-Host "======================================================" -ForegroundColor Cyan
        Write-Host ((" " * $subTitlePad) + $subTitle) -ForegroundColor Cyan
        Write-Host ((" " * $dtPad) + $dt) -ForegroundColor Gray
        Write-Host "======================================================" -ForegroundColor Cyan
        Write-Host " 1. Start Full Flow (Sim -> GW -> Tester)"
        Write-Host " 2. Stop All Services"
        Write-Host " 3. Show Status"
        Write-Host " 4. Clean Logs"
        Write-Host " 5. Launch Test API in Chrome"
        Write-Host "------------------------------------------------------" -ForegroundColor Gray
        Write-Host " F. Get Fatal Messages"
        Write-Host " B. Back to Mode Selection"
        Write-Host " Q. Exit" -ForegroundColor Yellow
        Write-Host "======================================================" -ForegroundColor Cyan

        $subChoice = (Read-Host "Select Action").ToUpper()
        
        switch ($subChoice) {
            "1" { Start-Services $currentMode }
            "2" { Stop-Everything }
            "3" { Show-Status $currentMode }
            "4" { Clean-RelateLogs $currentMode }
            "5" { Start-ChromeTest $currentMode }
            "F" { Get-FatalMessages }
            "B" { break }
            "Q" { exit }
        }
        
        if ($subChoice -ne "B") {
            Write-Host "`nPress any key to continue..." -ForegroundColor Gray
            $null = [Console]::ReadKey($true)
        }
    } while ($subChoice -ne "B")

} while ($true)
