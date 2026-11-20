<#
    CyberCOLA .NET 10 Unified Entry Point (Launcher)
    Supports hidden 2-layer shortcuts (e.g., 11, 23, 31).
#>

$ScriptDir = $PSScriptRoot

if ($null -eq $env:COLA_SIMULATOR_TYPE) {
    $env:COLA_SIMULATOR_TYPE = "WebStressSimulator"
}

# AesGcm requires PowerShell 7+ (.NET 8+). Auto-restart in pwsh if running under Windows PowerShell 5.x.
if ($PSVersionTable.PSEdition -eq 'Desktop') {
    $pwshCmd = Get-Command pwsh -ErrorAction SilentlyContinue
    if ($pwshCmd) {
        & $pwshCmd.Source -NoProfile -File $MyInvocation.MyCommand.Path @args
        exit $LASTEXITCODE
    } else {
        Write-Warning "PowerShell 7 (pwsh.exe) is required for AES-GCM decryption but was not found."
        Write-Warning "Please install PowerShell 7: https://aka.ms/powershell"
    }
}

# Dot-source shared helpers (must run AFTER the pwsh-7 guard above, since Common requires PowerShell 7/Core)
. "$ScriptDir\CyberCOLA-Common.ps1"
$ProgressPreference = 'SilentlyContinue'
$BaseDir = Get-CyberCOLABaseDir

# Adjust host console window size for optimal display if running in a standard ConsoleHost
Set-CyberCOLAConsoleSize

# Fast dashboard scanner that runs in <30ms
function Show-WebPortsDashboard {
    # 1. Define apps list (single source of truth in Common; derive .Port from its .Url)
    $WebApps = Get-CyberCOLAWebAppCatalog
    for ($i = 0; $i -lt $WebApps.Count; $i++) {
        $WebApps[$i].Port = if ($WebApps[$i].Url -match ':(\d+)$') { [int]$Matches[1] } else { $null }
    }

    # Hot-reload actual configured ports from launchSettings.json
    for ($i = 0; $i -lt $WebApps.Count; $i++) {
        $app = $WebApps[$i]
        $launchPath = Join-Path $BaseDir (Join-Path $app.Path "Properties\launchSettings.json")
        if (Test-Path $launchPath) {
            try {
                $json = Get-Content $launchPath -Raw | ConvertFrom-Json
                $profiles = $json.profiles
                $profileName = $app.Name
                if ($profiles.$profileName) {
                    $urlStr = $profiles.$profileName.applicationUrl
                    if ($urlStr -match 'http://localhost:(\d+)') {
                        $WebApps[$i].Port = [int]$matches[1]
                    }
                }
            } catch {}
        }
    }

    # Fetch active local TCP listeners once
    $props = [System.Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties()
    $listeners = $props.GetActiveTcpListeners() | ForEach-Object { $_.Port }
    $excludedRanges = Get-CyberCOLAExcludedPortRanges

    $runningCount = 0
    $blockedCount = 0
    $stoppedCount = 0

    foreach ($app in $WebApps) {
        $port = $app.Port
        $isExcluded = Test-CyberCOLAPortExcluded -Port $port -Ranges $excludedRanges
        
        if ($isExcluded) {
            $blockedCount++
        } elseif ($listeners -contains $port) {
            $runningCount++
        } else {
            $stoppedCount++
        }
    }

    # Draw the dashboard panel (embedded inside 70-character header)
    Write-Host "----------------------------------------------------------------------" -ForegroundColor Cyan
    
    Write-Host "  Web Simulators: " -NoNewline
    Write-Host "22  " -ForegroundColor White -NoNewline
    Write-Host "|  Running: " -NoNewline
    $runColor = if ($runningCount -gt 0) { "Green" } else { "Gray" }
    Write-Host "$runningCount  " -ForegroundColor $runColor -NoNewline
    Write-Host "|  Stopped: " -NoNewline
    Write-Host "$stoppedCount  " -ForegroundColor Gray -NoNewline
    
    Write-Host "|  Blocked: " -NoNewline
    $blockColor = if ($blockedCount -gt 0) { "Red" } else { "Gray" }
    # Padding: total length matches 70 chars perfectly
    Write-Host "$blockedCount" -ForegroundColor $blockColor
    
    if ($blockedCount -gt 0) {
        Write-Host "  [WARNING] $blockedCount Web simulator ports are blocked! Press W to resolve." -ForegroundColor Red
    }
    Write-Host "======================================================================" -ForegroundColor Cyan
}

function Show-SimulatorSummary {
    Write-Host "`n==========================================================================================" -ForegroundColor Cyan
    Write-Host "                           Simulator Operation Summary Report" -ForegroundColor Cyan
    Write-Host "==========================================================================================" -ForegroundColor Cyan
    
    $searchDir = Join-Path $BaseDir "publish\win-x64"
    if (-not (Test-Path $searchDir)) {
        Write-Host "Publish directory not found. No summary to display." -ForegroundColor Yellow
        Write-Host "==========================================================================================" -ForegroundColor Cyan
        return
    }
    
    $summaries = Get-ChildItem -Path $searchDir -Filter "OperationSummary.json" -Recurse -File -ErrorAction SilentlyContinue
    
    if (-not $summaries) {
        Write-Host "No active or recorded simulator operation summary files found." -ForegroundColor Yellow
        Write-Host "Ensure simulators were started (Option 6 or 8) and running." -ForegroundColor Yellow
        Write-Host "==========================================================================================" -ForegroundColor Cyan
        return
    }
    
    # English comments only: Format table headers
    Write-Host ("{0,-15} | {1,-10} | {2,-6} | {3,-12} | {4,-12} | {5,-6} | {6,-19}" -f "Instance Name", "Type", "Port", "Active Conns", "Success Count", "TPS", "Last Updated") -ForegroundColor Gray
    Write-Host ("-" * 90) -ForegroundColor Gray
    
    foreach ($file in $summaries) {
        try {
            $content = Get-Content $file.FullName -Raw -ErrorAction SilentlyContinue
            if ($content) {
                # English comments only: Parse output statistics exported by high performance stress simulator
                $data = ConvertFrom-Json $content
                $name = $file.Directory.Name
                
                $conn = $data.ActiveConnections
                $success = $data.SuccessCount
                $tps = $data.Tps
                $port = $data.ListenPort
                $type = $data.TransactionType
                $updated = $data.LastUpdated
                
                # Highlight active connections/transactions with Green, else use Gray
                $color = if ($conn -gt 0 -or $tps -gt 0) { "Green" } else { "Gray" }
                
                Write-Host ("{0,-15} | {1,-10} | {2,-6} | {3,-12} | {4,-12} | {5,-6} | {6,-19}" -f $name, $type, $port, $conn, $success, $tps, $updated) -ForegroundColor $color
            }
        } catch {
            # English comments only: Silently bypass lock issues during high-speed parallel reads
            Write-Warning "Failed to read simulator summary - $($_.Exception.Message)"
        }
    }
    Write-Host "==========================================================================================" -ForegroundColor Cyan
}

function Show-ColaTranLogPairs {
    $connString = Get-CyberCOLADatabaseConnectionString
    if (-not $connString) {
        Write-Host "Failed to resolve database connection string from SystemParameterInfo.xml." -ForegroundColor Red
        Write-Host "================================================================================================" -ForegroundColor Cyan
        return
    }

    # 1. Try to load Microsoft.Data.SqlClient.dll dynamically for modern driver features (Always Encrypted support)
    $script:UseNewDriver = $false
    $dllPath = Join-Path $BaseDir "publish\win-x64\CyberAuthNCCC\Microsoft.Data.SqlClient.dll"
    if (-not (Test-Path $dllPath)) {
        # Fallback to search inside base workspace recursively
        $dllFile = Get-ChildItem -Path $BaseDir -Filter "Microsoft.Data.SqlClient.dll" -Recurse -File -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($dllFile) { $dllPath = $dllFile.FullName }
    }

    if ($dllPath -and (Test-Path $dllPath)) {
        try {
            Add-Type -Path $dllPath -ErrorAction Stop
            $script:UseNewDriver = $true
        } catch {
            $script:UseNewDriver = $false
        }
    }

    # If falling back to legacy System.Data.SqlClient, filter out modern Column Encryption Setting keyword to prevent connection crash
    if (-not $script:UseNewDriver) {
        $connString = $connString -replace '(?i)Column\s+Encryption\s+Setting\s*=\s*[^;]+;?', ''
    }

    # Verify database connectivity before running the real query.
    try {
        $conn = if ($script:UseNewDriver) {
            New-Object Microsoft.Data.SqlClient.SqlConnection($connString)
        } else {
            New-Object System.Data.SqlClient.SqlConnection($connString)
        }
        $conn.Open()
        
        $pingCmd = if ($script:UseNewDriver) {
            New-Object Microsoft.Data.SqlClient.SqlCommand("SELECT 1", $conn)
        } else {
            New-Object System.Data.SqlClient.SqlCommand("SELECT 1", $conn)
        }
        [void]$pingCmd.ExecuteScalar()
        $conn.Close()
    } catch {
        Write-Host "Database connection check failed: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "================================================================================================" -ForegroundColor Cyan
        return
    }

    $sql = @"
WITH RecentS AS (
    SELECT TOP (1000) TRAN_LOG_KEY, P_039, CREATE_DTE
    FROM COLA_TRAN_LOG
    WHERE DIRECTION = 'S'
    ORDER BY CREATE_DTE DESC
),
RecentR AS (
    SELECT TOP (1000) TRAN_LOG_KEY, SESSION_NAME, CREATE_DTE, MESSAGE_TYPE
    FROM COLA_TRAN_LOG
    WHERE DIRECTION = 'R'
    ORDER BY CREATE_DTE DESC
)
SELECT TOP (10)
    s.TRAN_LOG_KEY,
    r.SESSION_NAME,
    r.CREATE_DTE AS Receive_CREATE_DTE,
    r.MESSAGE_TYPE AS Receive_MESSAGE_TYPE,
    s.P_039 AS Send_P_039
FROM RecentS s
INNER JOIN RecentR r ON s.TRAN_LOG_KEY = r.TRAN_LOG_KEY
ORDER BY r.CREATE_DTE DESC
"@

    $running = $true
    while ($running) {
        try {
            Clear-Host
            $now = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
            $screenWidth = 96
            $title = 'Recent COLA_TRAN_LOG Send/Receive Pairs (Last 10)'
            $titlePad = [math]::Max(0, [int](($screenWidth - $title.Length) / 2))
            $nowPad = [math]::Max(0, [int](($screenWidth - $now.Length) / 2))
            Write-Host "================================================================================================" -ForegroundColor Cyan
            Write-Host ((" " * $titlePad) + $title) -ForegroundColor Cyan
            Write-Host ((" " * $nowPad) + $now) -ForegroundColor Gray
            Write-Host "================================================================================================" -ForegroundColor Cyan
            Write-Host ""

            $conn = if ($script:UseNewDriver) {
                New-Object Microsoft.Data.SqlClient.SqlConnection($connString)
            } else {
                New-Object System.Data.SqlClient.SqlConnection($connString)
            }
            $cmd = if ($script:UseNewDriver) {
                New-Object Microsoft.Data.SqlClient.SqlCommand($sql, $conn)
            } else {
                New-Object System.Data.SqlClient.SqlCommand($sql, $conn)
            }
            $adapter = if ($script:UseNewDriver) {
                New-Object Microsoft.Data.SqlClient.SqlDataAdapter($cmd)
            } else {
                New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
            }
            $table = New-Object System.Data.DataTable
            [void]$adapter.Fill($table)
            $conn.Close()

            if ($table.Rows.Count -eq 0) {
                Write-Host "No complete Send/Receive pairs found in the recent COLA_TRAN_LOG records." -ForegroundColor Yellow
                Write-Host "================================================================================================" -ForegroundColor Cyan
                Write-Host ""
                Write-Host "Q = quit  |  any key = refresh now  |  auto-refresh in 10s" -ForegroundColor Yellow
                $keyInfo = [Console]::ReadKey($true)
                if ($keyInfo -and $keyInfo.KeyChar -in @('q','Q')) { $running = $false }
                continue
            }

            # Map SQL rows directly to paired PSCustomObjects
            $paired = @()
            foreach ($row in $table.Rows) {
                $recvDte = $row['Receive_CREATE_DTE']
                if ([System.DBNull]::Value -eq $recvDte -or -not $recvDte) {
                    $key = $row['TRAN_LOG_KEY'].ToString()
                    $ts = if ($key.Length -ge 14) { $key.Substring(0, 14) } else { $key }
                    if ([DateTime]::TryParseExact($ts, 'yyyyMMddHHmmss', $null, [System.Globalization.DateTimeStyles]::None, [ref]([DateTime]::MinValue))) {
                        $recvDte = [DateTime]::ParseExact($ts, 'yyyyMMddHHmmss', $null)
                    }
                }
                $paired += [PSCustomObject]@{
                    TRAN_LOG_KEY         = $row['TRAN_LOG_KEY'].ToString()
                    SESSION_NAME         = $row['SESSION_NAME'].ToString()
                    Receive_CREATE_DTE   = $recvDte
                    Receive_MESSAGE_TYPE = $row['Receive_MESSAGE_TYPE'].ToString()
                    Send_P_039           = $row['Send_P_039'].ToString()
                }
            }

            $keyWidth = (@($paired | ForEach-Object { $_.TRAN_LOG_KEY.ToString().Length }) + 14 | Measure-Object -Maximum).Maximum
            $sessionWidth = (@($paired | ForEach-Object { $_.SESSION_NAME.ToString().Length }) + 22 | Measure-Object -Maximum).Maximum
            $dteWidth = (@($paired | ForEach-Object { $_.Receive_CREATE_DTE.ToString('yyyy-MM-dd HH:mm:ss.fff').Length }) + 21 | Measure-Object -Maximum).Maximum
            $msgWidth = (@($paired | ForEach-Object { $_.Receive_MESSAGE_TYPE.ToString().Length }) + 8 | Measure-Object -Maximum).Maximum
            $p39Width = (@($paired | ForEach-Object { $_.Send_P_039.ToString().Length }) + 10 | Measure-Object -Maximum).Maximum

            $header = "{0,-$keyWidth} | {1,-$sessionWidth} | {2,-$dteWidth} | {3,-$msgWidth} | {4,-$p39Width}" -f 'TRAN_LOG_KEY', 'SESSION_NAME', 'Receive_CREATE_DTE', 'MSG_TYPE', 'Send_P_039'
            Write-Host $header -ForegroundColor White
            Write-Host ("-" * $header.Length) -ForegroundColor Gray

            foreach ($p in $paired) {
                $dte = $p.Receive_CREATE_DTE.ToString('yyyy-MM-dd HH:mm:ss.fff')
                $p39 = $p.Send_P_039.ToString()
                $row = "{0,-$keyWidth} | {1,-$sessionWidth} | {2,-$dteWidth} | {3,-$msgWidth} | " -f $p.TRAN_LOG_KEY, $p.SESSION_NAME, $dte, $p.Receive_MESSAGE_TYPE
                Write-Host $row -NoNewline
                if ($p39 -ne '00') {
                    Write-Host ("{0,-$p39Width}" -f $p39) -ForegroundColor Red
                } else {
                    Write-Host ("{0,-$p39Width}" -f $p39)
                }
            }

            Write-Host "================================================================================================" -ForegroundColor Cyan
            Write-Host ""
            Write-Host "Q = quit  |  any key = refresh now  |  auto-refresh in 10s" -ForegroundColor Yellow

            # Wait up to 10 seconds for a key press; Q quits, any other key triggers immediate refresh.
            $endTime = (Get-Date).AddSeconds(10)
            $refreshedByKey = $false
            while ((Get-Date) -lt $endTime -and -not $refreshedByKey) {
                if ([Console]::KeyAvailable) {
                    $keyInfo = [Console]::ReadKey($true)
                    if ($keyInfo -and $keyInfo.KeyChar -in @('q','Q')) {
                        $running = $false
                    }
                    $refreshedByKey = $true
                } else {
                    Start-Sleep -Milliseconds 100
                }
            }
        } catch {
            Write-Host "Failed to query COLA_TRAN_LOG: $($_.Exception.Message)" -ForegroundColor Red
            Write-Host "================================================================================================" -ForegroundColor Cyan
            Write-Host ""
            Write-Host "Q = quit  |  any key = retry now" -ForegroundColor Yellow
            $keyInfo = [Console]::ReadKey($true)
            if ($keyInfo -and $keyInfo.KeyChar -in @('q','Q')) { $running = $false }
        }
    }
}

do {
    Show-CyberCOLAHeader -Title "CyberCOLA Unified Control Center" -Width 70
    Show-WebPortsDashboard
    Write-Host " --- Core Management & Sub-Panels ---" -ForegroundColor Cyan
    Write-Host " 1. System Management (Services/Auth/GW)" -ForegroundColor White
    Write-Host " 2. Simulator Management (WPF / Web)" -ForegroundColor White
    Write-Host " 3. Integrated Test Flows (Sim -> GW -> Auth)" -ForegroundColor White
    Write-Host " 4. MaintGateway Management & Test (HTTP/HTTPS/JWT)" -ForegroundColor White
    Write-Host " 5. Clean Build Artifacts & Logs (bin, obj, publish)" -ForegroundColor White
    Write-Host " W. Web Simulators Control Panel (StartStop-WebApps)" -ForegroundColor White
    Write-Host " X. Toggle Simulator (Current: $env:COLA_SIMULATOR_TYPE)" -ForegroundColor White
    Write-Host ""
    Write-Host " --- One-Click Automation Flows ---" -ForegroundColor Cyan
    Write-Host " 6. One-Click: Start NCCC Flow (Services & Simulators)" -ForegroundColor White
    Write-Host " 7. One-Click: Stop NCCC Flow (Services & Simulators)" -ForegroundColor White
    Write-Host " 8. One-Click: Start FISC Flow (Services & Simulators)" -ForegroundColor White
    Write-Host " 9. One-Click: Stop FISC Flow (Services & Simulators)" -ForegroundColor White
    Write-Host " 0. One-Click: Stop All Flow Processes (Services & Simulators)" -ForegroundColor White
    Write-Host ""
    Write-Host " --- Diagnostics & Monitoring ---" -ForegroundColor Cyan
    Write-Host " H. Run System Health Doctor (Diagnostics)" -ForegroundColor White
    Write-Host " M. Display All Process Status (Monitor)" -ForegroundColor White
    Write-Host " T. Display All Service TCP/IP Connections (Detailed)" -ForegroundColor White
    Write-Host " S. Display Simulator Operation Summary" -ForegroundColor White
    Write-Host " D. Display Recent COLA_TRAN_LOG Send/Receive Pairs" -ForegroundColor White
    Write-Host " F. Get WARN|ERROR|FATAL Messages from Logs" -ForegroundColor White
    Write-Host ""
    Write-Host " --- Development & Maintenance ---" -ForegroundColor Cyan
    Write-Host " P. Publish & Package All Services (dotnet publish)" -ForegroundColor White
    Write-Host " K. Sync SocketConfig.json to All Auth/GW appsettings.json" -ForegroundColor White
    Write-Host " E. Export SocketConfig.json to GW/Auth Cross-Table CSV" -ForegroundColor White
    Write-Host " I. Import SocketConfig.json from GW/Auth Cross-Table CSV" -ForegroundColor White
    Write-Host " Y. Project Dependency Visualizer (Tree & Mermaid)" -ForegroundColor White
    Write-Host " O. Code Format & Style Cleanup (dotnet format)" -ForegroundColor White
    Write-Host " L. Clean All Log Files (LogFiles)" -ForegroundColor White
    Write-Host " Q. Quit" -ForegroundColor Yellow
    
    $choice = (Read-Host "`nMain > Select").ToUpper()
    
    # Smart shortcut parsing (Hidden 2-layer logic)
    if ($choice.Length -eq 2) {
        $mainIdx = $choice.Substring(0,1)
        $subIdx = $choice.Substring(1,1)
        
        switch ($mainIdx) {
            '1' { & "$ScriptDir\COLAMainMenu-Console.ps1" -AutoSelect $subIdx }
            '2' { & "$ScriptDir\COLASimulatorMainMenu.ps1" -AutoSelect $subIdx }
            '3' { & "$ScriptDir\COLATestFlowMenu.ps1" -AutoSelect $subIdx }
            '4' { & "$ScriptDir\Start-MaintGatewayTest.ps1" -AutoSelect $subIdx }
            default { 
                Write-Host "`n[Error] Invalid shortcut prefix: $mainIdx" -ForegroundColor Red
                Write-Host "Press any key to return..." -ForegroundColor Red
                $null = [Console]::ReadKey($true)
            }
        }
    } elseif ($choice.Length -eq 1) {
        # Standard 1-digit navigation
        switch ($choice) {
            '1' { & "$ScriptDir\COLAMainMenu-Console.ps1" }
            '2' {
                Write-Host "`nSelect Simulator Platform Mode:" -ForegroundColor Cyan
                Write-Host " 1. WPF Desktop Simulators (WpfStressSimulator)" -ForegroundColor White
                Write-Host " 2. Web Kestrel Simulators (WebStressSimulator - http://localhost:5200)" -ForegroundColor White
                Write-Host " 3. Cancel / Back to Main Menu" -ForegroundColor Yellow
                Write-Host "Please select mode [1-3] (Default: 1) ? " -ForegroundColor Cyan -NoNewline
                $simChoice = Read-Host
                if ($simChoice -eq '2') {
                    $env:COLA_SIMULATOR_TYPE = "WebStressSimulator"
                    & "$ScriptDir\COLASimulatorMainMenu.ps1"
                } elseif ($simChoice -eq '3') {
                    # Back to main menu
                } else {
                    $env:COLA_SIMULATOR_TYPE = "WpfStressSimulator"
                    & "$ScriptDir\COLASimulatorMainMenu.ps1"
                }
            }
            '3' { & "$ScriptDir\COLATestFlowMenu.ps1" }
            '4' { & "$ScriptDir\Start-MaintGatewayTest.ps1" }
            'X' {
                if ($env:COLA_SIMULATOR_TYPE -eq "WpfStressSimulator") {
                    $env:COLA_SIMULATOR_TYPE = "WebStressSimulator"
                } else {
                    $env:COLA_SIMULATOR_TYPE = "WpfStressSimulator"
                }
                Write-Host "`nSimulator switched to: $env:COLA_SIMULATOR_TYPE" -ForegroundColor Green
                Start-Sleep -Seconds 1
            }
            'W' {
                & "$ScriptDir\StartStop-WebApps.ps1"
            }
            'T' {
                & "$ScriptDir\COLAMainMenu-Console.ps1" -AutoSelect T
                Write-Host "`nDone. Press any key to return..." -ForegroundColor Red
                $null = [Console]::ReadKey($true)
            }
            'D' {
                Show-ColaTranLogPairs
                Write-Host "`nDone. Press any key to return..." -ForegroundColor Red
                $null = [Console]::ReadKey($true)
            }
            '5' {
                Write-Host "`n[Action] Cleaning up build artifacts and logs..." -ForegroundColor Cyan
                
                # Stop all running processes first to release file locks
                Write-Host " - Stopping all running services and simulators to prevent file locking..." -ForegroundColor Yellow
                Stop-CyberCOLAProcesses -Names @("Wpf*", "Web*", "Cyber*", "ServiceRouter", "MaintGateway*") -WaitSeconds 5

                # Delete bin and obj folders
                Write-Host " - Removing all bin and obj folders..." -ForegroundColor Gray
                # Use -Filter individually as it is more reliable for recursive directory finding
                Get-ChildItem -Path $BaseDir -Recurse -Directory -Filter bin -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
                Get-ChildItem -Path $BaseDir -Recurse -Directory -Filter obj -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
                
                # Delete publish targets (excluding Config)
                Write-Host " - Removing publish\win-x64 and linux-x64..." -ForegroundColor Gray
                Remove-Item -Path "$BaseDir\publish\win-x64", "$BaseDir\publish\linux-x64" -Recurse -Force -ErrorAction SilentlyContinue
                
                # Delete all *.log files
                Write-Host " - Removing all log files..." -ForegroundColor Gray
                Get-ChildItem -Path $BaseDir -Filter *.log -Recurse -File -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue
                
                Write-Host "`n[Success] Cleanup complete." -ForegroundColor Green
                Write-Host "Press any key to return..." -ForegroundColor White
                $null = [Console]::ReadKey($true)
            }
            'L' {
                Write-Host "`n[Action] Cleaning up all log files..." -ForegroundColor Cyan
                
                # Stop all running processes first to release file locks on log files
                Write-Host " - Stopping all running services and simulators to prevent file locking..." -ForegroundColor Yellow
                Stop-CyberCOLAProcesses -Names @("Wpf*", "Web*", "Cyber*", "ServiceRouter", "MaintGateway*") -WaitSeconds 5

                # 1. Clean workspace LogFiles directory
                $wsLogDir = Join-Path $BaseDir "LogFiles"
                if (Test-Path $wsLogDir) {
                    Write-Host " - Removing workspace LogFiles: $wsLogDir..." -ForegroundColor Gray
                    Remove-Item "$wsLogDir\*" -Recurse -Force -ErrorAction SilentlyContinue
                }

                # 2. Clean local logs in publish folders
                $pubDir = Join-Path $BaseDir "publish\win-x64"
                if (Test-Path $pubDir) {
                    Write-Host " - Removing local logs in publish folders..." -ForegroundColor Gray
                    Get-ChildItem -Path $pubDir -Directory -ErrorAction SilentlyContinue | ForEach-Object {
                        $localLog = Join-Path $_.FullName "Log"
                        if (Test-Path $localLog) {
                            Remove-Item "$localLog\*" -Recurse -Force -ErrorAction SilentlyContinue
                        }
                    }
                }

                # 4. Clean local log folders under src (bin/obj output logs)
                $srcDir = Join-Path $BaseDir "src"
                if (Test-Path $srcDir) {
                    Write-Host " - Removing local logs under src..." -ForegroundColor Gray
                    Get-ChildItem -Path $srcDir -Recurse -Directory -Filter "Log" -ErrorAction SilentlyContinue | ForEach-Object {
                        Remove-Item "$($_.FullName)\*" -Recurse -Force -ErrorAction SilentlyContinue
                    }
                    Get-ChildItem -Path $srcDir -Recurse -Directory -Filter "logs" -ErrorAction SilentlyContinue | ForEach-Object {
                        Remove-Item "$($_.FullName)\*" -Recurse -Force -ErrorAction SilentlyContinue
                    }
                }

                # 5. Clean root *.log files
                Write-Host " - Removing *.log files in root..." -ForegroundColor Gray
                Get-ChildItem -Path $BaseDir -Filter *.log -Recurse -File -ErrorAction SilentlyContinue | ForEach-Object {
                    Remove-Item $_.FullName -Force -ErrorAction SilentlyContinue
                }

                Write-Host "`n[Success] All log files cleaned up successfully." -ForegroundColor Green
                Write-Host "Press any key to return..." -ForegroundColor White
                $null = [Console]::ReadKey($true)
            }
            'F' {
                Write-Host "`n[Action] Searching for WARN|ERROR|FATAL messages in log files..." -ForegroundColor Cyan
                
                $logFolders = @()
                
                # 1. Add workspace LogFiles directory
                $wsLogDir = Join-Path $BaseDir "LogFiles"
                if (Test-Path $wsLogDir) {
                    $logFolders += $wsLogDir
                }
                
                # 2. Add publish win-x64 directory
                $pubDir = Join-Path $BaseDir "publish\win-x64"
                if (Test-Path $pubDir) {
                    $logFolders += $pubDir
                }
                
                if ($logFolders.Count -eq 0) {
                    Write-Host "No log directories found." -ForegroundColor Yellow
                    Write-Host "Press any key to return..." -ForegroundColor White
                    $null = [Console]::ReadKey($true)
                    break
                }
                
                $logFiles = Get-ChildItem -Path $logFolders -Filter *.log -Recurse -File -ErrorAction SilentlyContinue | Group-Object FullName | ForEach-Object { $_.Group[0] }
                
                if (-not $logFiles) {
                    Write-Host "No log files (*.log) found." -ForegroundColor Yellow
                    Write-Host "Press any key to return..." -ForegroundColor White
                    $null = [Console]::ReadKey($true)
                    break
                }
                
                # Q/q stops the scan and returns to the menu. Checked between files and periodically while
                # reading/printing, because a single session log can be hundreds of MB.
                $isCancelRequested = {
                    try {
                        while ([Console]::KeyAvailable) {
                            if ([Console]::ReadKey($true).Key -eq [ConsoleKey]::Q) { return $true }
                        }
                    } catch { }
                    return $false
                }
                Write-Host "Press Q to stop scanning and return to the main menu." -ForegroundColor DarkGray

                $totalMatches = 0
                $cancelled = $false
                foreach ($file in $logFiles) {
                    if (& $isCancelRequested) { $cancelled = $true; break }
                    Write-Host "Scanning: $($file.FullName)..." -ForegroundColor Gray
                    try {
                        # Stream the file instead of loading it whole so Q stays responsive; shared read
                        # lets logs that services are still writing be scanned.
                        $matchedLines = [System.Collections.Generic.List[string]]::new()
                        $reader = [System.IO.StreamReader]::new([System.IO.FileStream]::new($file.FullName, 'Open', 'Read', 'ReadWrite'))
                        try {
                            $lineCount = 0
                            while ($null -ne ($line = $reader.ReadLine())) {
                                if ((++$lineCount % 5000) -eq 0 -and (& $isCancelRequested)) { $cancelled = $true; break }
                                # Match WARN, ERROR, or FATAL (case-insensitive)
                                if ($line -match '\b(WARN|ERROR|FATAL)\b') {
                                    $matchedLines.Add($line)
                                }
                            }
                        } finally {
                            $reader.Dispose()
                        }
                        if ($cancelled) { break }

                        if ($matchedLines.Count -gt 0) {
                            $totalMatches += $matchedLines.Count
                            Write-Host "`n>>> File: $($file.FullName) <<<" -ForegroundColor Yellow
                            $printed = 0
                            foreach ($m in $matchedLines) {
                                if ((++$printed % 100) -eq 0 -and (& $isCancelRequested)) { $cancelled = $true; break }
                                if ($m -match '\b(FATAL)\b') {
                                    Write-Host "  $m" -ForegroundColor Red
                                } elseif ($m -match '\b(ERROR)\b') {
                                    Write-Host "  $m" -ForegroundColor DarkRed
                                } else {
                                    Write-Host "  $m" -ForegroundColor DarkYellow
                                }
                            }
                            if ($cancelled) { break }
                        }
                    } catch {
                        # Skip if file is locked or unreadable
                        Write-Warning "Skipped log file read - $($_.Exception.Message)"
                    }
                }

                if ($cancelled) {
                    Write-Host "`n[Cancelled] Log scan stopped by user, returning to the main menu." -ForegroundColor Yellow
                    Start-Sleep -Milliseconds 800
                    break
                }

                if ($totalMatches -eq 0) {
                    Write-Host "`n[Success] No WARN, ERROR, or FATAL messages found in any log files." -ForegroundColor Green
                } else {
                    Write-Host "`n[Done] Found $totalMatches matching messages." -ForegroundColor Cyan
                }
                
                Write-Host "Press any key to return..." -ForegroundColor White
                $null = [Console]::ReadKey($true)
            }
            '6' { & "$ScriptDir\Start-NCCC.ps1" }
            '7' { 
                & "$ScriptDir\Stop-NCCC.ps1"
                Show-SimulatorSummary
                Write-Host "Press any key to return..." -ForegroundColor White
                $null = [Console]::ReadKey($true)
            }
            '8' { & "$ScriptDir\Start-FISC.ps1" }
            '9' { 
                & "$ScriptDir\Stop-FISC.ps1"
                Show-SimulatorSummary
                Write-Host "Press any key to return..." -ForegroundColor White
                $null = [Console]::ReadKey($true)
            }
            '0' {
                Write-Host "`n[Action] Stopping all running services and simulators..." -ForegroundColor Yellow
                Stop-CyberCOLAProcesses -Names @("Wpf*", "Web*", "Cyber*", "ServiceRouter", "MaintGateway*") -WaitSeconds 5
                Show-SimulatorSummary
                Write-Host "Press any key to return..." -ForegroundColor White
                $null = [Console]::ReadKey($true)
            }
            'S' {
                Show-SimulatorSummary
                Write-Host "Press any key to return..." -ForegroundColor White
                $null = [Console]::ReadKey($true)
            }
            'H' {
                & "$ScriptDir\Check-SystemHealth.ps1"
                Write-Host "Press any key to return..." -ForegroundColor White
                $null = [Console]::ReadKey($true)
            }
            'M' {
                & "$ScriptDir\Monitor-COLAServices.ps1"
            }
            'P' {
                & "$ScriptDir\Publish-CyberCOLAServices.ps1"
                Write-Host "Press any key to return..." -ForegroundColor White
                $null = [Console]::ReadKey($true)
            }
            'K' {
                Write-Host "`n[Action] Syncing SocketConfig.json into all Auth/GW appsettings.json..." -ForegroundColor Cyan
                $socketConfigPath = Join-Path $BaseDir "src\CyberCOLA\Config\SocketConfig.json"
                if (-not (Test-Path $socketConfigPath)) {
                    Write-Host "[Error] SocketConfig.json not found: $socketConfigPath" -ForegroundColor Red
                } else {
                    $map = Get-CyberCOLASocketConfigProjectMap

                    Write-Host "`n--- Source tree (src\CyberCOLA\<Project>\appsettings.json) ---" -ForegroundColor Gray
                    foreach ($projName in $map.Keys) {
                        $network, $role = $map[$projName]
                        $srcPath = Join-Path $BaseDir "src\CyberCOLA\$projName\appsettings.json"
                        if (Test-Path $srcPath) {
                            Sync-CyberCOLASocketConfigFile -SocketConfigPath $socketConfigPath -Network $network -Role $role -TargetAppSettingsPath $srcPath
                        }
                    }

                    $pubDir = Join-Path $BaseDir "publish\win-x64"
                    if (Test-Path $pubDir) {
                        Write-Host "`n--- Published output (publish\win-x64\<Project>\appsettings.json) ---" -ForegroundColor Gray
                        Sync-CyberCOLASocketConfigAll -PublishRootDir $pubDir -SocketConfigPath $socketConfigPath
                    }

                    Write-Host "`n[Success] SocketConfig.json sync complete." -ForegroundColor Green
                }
                Write-Host "Press any key to return..." -ForegroundColor White
                $null = [Console]::ReadKey($true)
            }
            'E' {
                Write-Host "`n[Action] Exporting SocketConfig.json to GW/Auth cross-table CSV..." -ForegroundColor Cyan
                $socketConfigPath = Join-Path $BaseDir "src\CyberCOLA\Config\SocketConfig.json"
                if (-not (Test-Path $socketConfigPath)) {
                    Write-Host "[Error] SocketConfig.json not found: $socketConfigPath" -ForegroundColor Red
                } else {
                    Export-CyberCOLASocketConfigCrossTable -SocketConfigPath $socketConfigPath
                    Write-Host "Edit the Value cells in Excel, save, then use option I to import." -ForegroundColor Yellow
                }
                Write-Host "Press any key to return..." -ForegroundColor White
                $null = [Console]::ReadKey($true)
            }
            'I' {
                Write-Host "`n[Action] Importing SocketConfig.json from GW/Auth cross-table CSV..." -ForegroundColor Cyan
                $socketConfigPath = Join-Path $BaseDir "src\CyberCOLA\Config\SocketConfig.json"
                $gwCsvPath = Join-Path $BaseDir "src\CyberCOLA\Config\SocketConfig.GW.csv"
                $authCsvPath = Join-Path $BaseDir "src\CyberCOLA\Config\SocketConfig.Auth.csv"
                if (-not (Test-Path $gwCsvPath) -and -not (Test-Path $authCsvPath)) {
                    Write-Host "[Error] No CSV found (run option E first)" -ForegroundColor Red
                } elseif (Confirm-CyberCOLA -Prompt "This will update values in $socketConfigPath from the GW/Auth CSVs. Continue?") {
                    Import-CyberCOLASocketConfigCrossTable -GWCsvPath $gwCsvPath -AuthCsvPath $authCsvPath -SocketConfigPath $socketConfigPath
                    Write-Host "Tip: run K to sync the updated values into all appsettings.json." -ForegroundColor Yellow
                } else {
                    Write-Host "Cancelled." -ForegroundColor Yellow
                }
                Write-Host "Press any key to return..." -ForegroundColor White
                $null = [Console]::ReadKey($true)
            }
            'Y' {
                & "$ScriptDir\Get-ProjectDependency.ps1"
                Write-Host "Press any key to return..." -ForegroundColor White
                $null = [Console]::ReadKey($true)
            }
            'O' {
                & "$ScriptDir\Format-CodeStyle.ps1"
                Write-Host "Press any key to return..." -ForegroundColor White
                $null = [Console]::ReadKey($true)
            }
            'Q' { Write-Host "`nExiting..." -F Cyan; Start-Sleep -S 1; exit }
            default { 
                Write-Host "`n[Error] Invalid selection: $choice" -ForegroundColor Red
                Write-Host "Press any key to return..." -ForegroundColor Red
                $null = [Console]::ReadKey($true)
            }
        }
    } else {
        # Handle 114 or other invalid lengths
        if ($choice) {
            Write-Host "`n[Error] Entry is invalid: $choice" -ForegroundColor Red
            Write-Host "Press any key to return..." -ForegroundColor Red
            $null = [Console]::ReadKey($true)
        }
    }
} while ($true)
