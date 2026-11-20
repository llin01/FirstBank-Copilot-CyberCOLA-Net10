# StartStop-WebApps.ps1
# Script to start, stop, build, and monitor all Web* Blazor applications in the CyberCOLA solution.
# Aligned with .NET 10.0 runtime host from Visual Studio.

# CyberCOLA-Common.ps1 requires PowerShell 7+ (Core edition). Auto-restart in pwsh if running under
# Windows PowerShell 5.x (desktop edition). This guard must stay inline, before dot-sourcing Common.
if ($PSVersionTable.PSEdition -eq 'Desktop') {
    $pwshCmd = Get-Command pwsh -ErrorAction SilentlyContinue
    if ($pwshCmd) {
        & $pwshCmd.Source -NoProfile -File $MyInvocation.MyCommand.Path @args
        exit $LASTEXITCODE
    } else {
        Write-Warning "PowerShell 7 (pwsh.exe) is required but was not found."
        exit 1
    }
}

$ErrorActionPreference = "Stop"

. "$PSScriptRoot\CyberCOLA-Common.ps1"

# Define the root path relative to this script
$RootPath = Resolve-Path (Join-Path $PSScriptRoot "..")
$dotnetHost = "dotnet"

# Detect .NET 10.0 Host and MSBuild
$msbuildPath = "MSBuild"
if (Test-Path "C:\Program Files\Microsoft Visual Studio\18\Enterprise\dotnet\net10.0\runtime\dotnet.exe") {
    $dotnetHost = "C:\Program Files\Microsoft Visual Studio\18\Enterprise\dotnet\net10.0\runtime\dotnet.exe"
} elseif (Test-Path "C:\Program Files\Microsoft Visual Studio\2022\Professional\dotnet\net10.0\runtime\dotnet.exe") {
    $dotnetHost = "C:\Program Files\Microsoft Visual Studio\2022\Professional\dotnet\net10.0\runtime\dotnet.exe"
}

if (Test-Path "C:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\MSBuild.exe") {
    $msbuildPath = "C:\Program Files\Microsoft Visual Studio\18\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
} elseif (Test-Path "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe") {
    $msbuildPath = "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
}

# Web* project name/path/URL catalog (single source of truth in Common)
$Apps = Get-CyberCOLAWebAppCatalog

# Dynamic scanner: load active HTTP/HTTPS ports from launchSettings.json in each project folder
function Update-AppsUrlsFromLaunchSettings {
    for ($i = 0; $i -lt $Apps.Count; $i++) {
        $app = $Apps[$i]
        $launchPath = Join-Path $RootPath (Join-Path $app.Path "Properties\launchSettings.json")
        if (Test-Path $launchPath) {
            try {
                $json = Get-Content $launchPath -Raw | ConvertFrom-Json
                $profiles = $json.profiles
                $appUrlStr = $null
                
                # Try to get the profile matching the App Name, or fall back to the first profile
                if ($profiles) {
                    if ($profiles.psobject.Properties[$app.Name]) {
                        $appUrlStr = $profiles.psobject.Properties[$app.Name].Value.applicationUrl
                    } else {
                        # Fallback: get the first profile's applicationUrl
                        foreach ($prop in $profiles.psobject.Properties) {
                            if ($prop.Value.applicationUrl) {
                                $appUrlStr = $prop.Value.applicationUrl
                                break
                            }
                        }
                    }
                }
                
                if ($appUrlStr) {
                    $urls = $appUrlStr -split ';'
                    $httpUrl = ""
                    $httpsUrl = ""
                    foreach ($u in $urls) {
                        $trimmed = $u.Trim()
                        if ($trimmed.StartsWith("https://")) {
                            $httpsUrl = $trimmed
                        } elseif ($trimmed.StartsWith("http://")) {
                            $httpUrl = $trimmed
                        }
                    }
                    if ($httpUrl) { $app.Url = $httpUrl }
                    if ($httpsUrl) { $app.HttpsUrl = $httpsUrl }
                }
            }
            catch {
                # Keep original hardcoded fallback if JSON parsing fails
            }
        }
    }
}

# Run the dynamic port scanner immediately on load
Update-AppsUrlsFromLaunchSettings

# Helper to find compiled DLL path
function Get-DllPath($appPath, $appName) {
    $fullAppPath = Join-Path $RootPath $appPath
    $candidates = @(
        "bin\x64\Debug\net10.0\$appName.dll",
        "bin\Debug\net10.0\$appName.dll",
        "bin\x64\Release\net10.0\$appName.dll",
        "bin\Release\net10.0\$appName.dll"
    )
    foreach ($c in $candidates) {
        $full = Join-Path $fullAppPath $c
        if (Test-Path $full) {
            return $c
        }
    }
    return $null
}

# Fetch all dotnet processes running Web apps once (in-memory lookup)
function Get-AppProcessesMap {
    $map = @{}
    $processes = Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" -ErrorAction SilentlyContinue
    if ($processes) {
        foreach ($p in $processes) {
            foreach ($app in $Apps) {
                if ($p.CommandLine -like "*$($app.Name).dll*") {
                    $map[$app.Name] = $p
                }
            }
        }
    }
    return $map
}

# Helper to get single running process (fallback)
function Get-AppProcess($appName) {
    $processes = Get-CimInstance Win32_Process -Filter "Name = 'dotnet.exe'" -ErrorAction SilentlyContinue
    if ($processes) {
        foreach ($p in $processes) {
            if ($p.CommandLine -like "*$appName.dll*") {
                return $p
            }
        }
    }
    return $null
}

# Stop application
function Stop-WebApp($app, $procsMap = $null) {
    $proc = $null
    if ($procsMap) {
        $proc = $procsMap[$app.Name]
    } else {
        $proc = Get-AppProcess $app.Name
    }

    if ($proc) {
        Write-Host "Stopping $($app.Name) (PID: $($proc.ProcessId))..." -NoNewline
        Stop-Process -Id $proc.ProcessId -Force -ErrorAction SilentlyContinue
        Write-Host " [STOPPED]" -ForegroundColor Green
    } else {
        Write-Host "$($app.Name) is not running." -ForegroundColor Gray
    }
}

# Start application
function Start-WebApp($app, $procsMap = $null) {
    $proc = $null
    if ($procsMap) {
        $proc = $procsMap[$app.Name]
    } else {
        $proc = Get-AppProcess $app.Name
    }

    if ($proc) {
        Write-Host "$($app.Name) is already running (PID: $($proc.ProcessId))." -ForegroundColor Yellow
        return
    }

    $appFullPath = Join-Path $RootPath $app.Path
    $relativeDll = Get-DllPath $app.Path $app.Name
    if (-not $relativeDll) {
        Write-Host "Error: Could not find compiled DLL for $($app.Name). Please build the project first." -ForegroundColor Red
        return
    }

    $dllFullPath = Join-Path $appFullPath $relativeDll
    
    # Construct URLs argument
    $urlsArg = $app.Url
    if ($app.HttpsUrl) {
        $urlsArg = "$($app.HttpsUrl);$($app.Url)"
    }

    Write-Host "Starting $($app.Name) on $urlsArg..." -NoNewline

    # Start background process
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $dotnetHost
    $psi.Arguments = """$dllFullPath"" --urls=""$urlsArg"""
    $psi.WorkingDirectory = $appFullPath
    $psi.CreateNoWindow = $true
    $psi.UseShellExecute = $false
    $psi.EnvironmentVariables["ASPNETCORE_ENVIRONMENT"] = "Development"
    
    $p = [System.Diagnostics.Process]::Start($psi)
    if ($p) {
        Write-Host " [STARTED] (PID: $($p.Id))" -ForegroundColor Green
    } else {
        Write-Host " [FAILED]" -ForegroundColor Red
    }
}

# Build application
function Build-WebApp($app) {
    $csprojFullPath = Join-Path $RootPath (Join-Path $app.Path "$($app.Name).csproj")
    if (-not (Test-Path $csprojFullPath)) {
        Write-Host "Error: Project file $csprojFullPath not found." -ForegroundColor Red
        return
    }
    
    Write-Host "Building $($app.Name) using MSBuild..." -ForegroundColor Cyan
    
    # Execute MSBuild command
    & $msbuildPath $csprojFullPath -p:Configuration=Debug -p:Platform=x64
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Build $($app.Name) [SUCCESS]" -ForegroundColor Green
    } else {
        Write-Host "Build $($app.Name) [FAILED]" -ForegroundColor Red
    }
}

# Helper to extract port from URL string
function Get-PortFromUrl($url) {
    if ($url -match ':(\d+)') {
        return [int]$Matches[1]
    }
    return $null
}

# Check single port availability status
function Test-PortStatus($port) {
    $listener = $null
    try {
        $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $port)
        $listener.Start()
        $listener.Stop()
        return "OK"
    }
    catch {
        $inner = $_.Exception
        if ($inner.InnerException) { $inner = $inner.InnerException }
        
        if ($inner -is [System.Net.Sockets.SocketException]) {
            $errCode = $inner.ErrorCode
            if ($errCode -eq 10013) {
                return "RESTRICTED (OS/Hyper-V Reserved)"
            } elseif ($errCode -eq 10048) {
                return "IN USE (By another process)"
            }
            return "ERROR ($errCode)"
        }
        return "ERROR ($($inner.Message))"
    }
}

# Find the next safe port starting from a baseline
function Find-NextAvailablePort($startPort, $excludedRanges) {
    $port = $startPort
    while ($true) {
        $isExcluded = Test-CyberCOLAPortExcluded -Port $port -Ranges $excludedRanges
        $status = Test-PortStatus $port
        if (-not $isExcluded -and $status -eq "OK") {
            return $port
        }
        $port++
        if ($port -gt 65535) {
            $port = 1024 # wrap around
        }
    }
}

# Update launchSettings.json for conflicting web apps
function Reconfigure-ConflictingPorts($app, $httpPort, $newHttpPort, $httpsPort, $newHttpsPort) {
    $launchPath = Join-Path $RootPath (Join-Path $app.Path "Properties\launchSettings.json")
    if (Test-Path $launchPath) {
        try {
            $content = [System.IO.File]::ReadAllText($launchPath)
            
            # Replace ports in applicationUrl
            $newContent = $content
            if ($httpPort -and $newHttpPort) {
                $newContent = $newContent -replace "localhost:$httpPort", "localhost:$newHttpPort"
            }
            if ($httpsPort -and $newHttpsPort) {
                $newContent = $newContent -replace "localhost:$httpsPort", "localhost:$newHttpsPort"
            }
            
            [System.IO.File]::WriteAllText($launchPath, $newContent)
            Write-Host "  Updated launchSettings.json for $($app.Name)" -ForegroundColor Green
        } catch {
            Write-Host "  Failed to update launchSettings.json for $($app.Name): $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

# Print ports check dashboard with active Hyper-V exclusion scanning & auto reconfiguration
function Show-PortsCheck {
    Clear-Host
    Write-Host "========================================================================================" -ForegroundColor Cyan
    Write-Host "                CyberCOLA Web Ports Availability & Hyper-V Diagnostics                   " -ForegroundColor Cyan
    Write-Host "========================================================================================" -ForegroundColor Cyan
    Write-Host "Scanning local HTTP/HTTPS ports & matching system exclusion ranges...`n" -ForegroundColor Gray
    
    $excludedRanges = Get-CyberCOLAExcludedPortRanges
    $hasConflicts = $false
    
    $format = "{0,-5} | {1,-28} | {2,-12} | {3,-10} | "
    Write-Host ([string]::Format($format + "{4}", "Index", "Application Name", "Protocol", "Port", "Port Status")) -ForegroundColor White
    Write-Host "----------------------------------------------------------------------------------------"
    
    for ($i = 0; $i -lt $Apps.Count; $i++) {
        $app = $Apps[$i]
        
        # Test HTTP
        $httpPort = Get-PortFromUrl $app.Url
        if ($httpPort) {
            $isExcluded = Test-CyberCOLAPortExcluded -Port $httpPort -Ranges $excludedRanges
            $status = Test-PortStatus $httpPort
            
            if ($isExcluded) {
                $status = "RESTRICTED (Hyper-V Excluded)"
                $color = "Red"
                $hasConflicts = $true
            } else {
                if ($status -eq "OK") {
                    $color = "Green"
                } elseif ($status -like "*IN USE*") {
                    $color = "Yellow"
                } else {
                    $color = "Red"
                }
            }
            
            Write-Host ([string]::Format($format, ($i + 1), $app.Name, "HTTP", $httpPort)) -NoNewline
            Write-Host $status -ForegroundColor $color
        }
        
        # Test HTTPS
        if ($app.HttpsUrl) {
            $httpsPort = Get-PortFromUrl $app.HttpsUrl
            if ($httpsPort) {
                $isExcluded = Test-CyberCOLAPortExcluded -Port $httpsPort -Ranges $excludedRanges
                $status = Test-PortStatus $httpsPort
                
                if ($isExcluded) {
                    $status = "RESTRICTED (Hyper-V Excluded)"
                    $color = "Red"
                    $hasConflicts = $true
                } else {
                    if ($status -eq "OK") {
                        $color = "Green"
                    } elseif ($status -like "*IN USE*") {
                        $color = "Yellow"
                    } else {
                        $color = "Red"
                    }
                }
                
                Write-Host ([string]::Format($format, "", "", "HTTPS", $httpsPort)) -NoNewline
                Write-Host $status -ForegroundColor $color
            }
        }
    }
    
    # Display system excluded ranges at the bottom for debugging
    if ($excludedRanges.Count -gt 0) {
        Write-Host "`n----------------------------------------------------------------------------------------"
        Write-Host "Detected System TCP Port Exclusion Ranges (Hyper-V / OS Reserved):" -ForegroundColor Gray
        foreach ($r in $excludedRanges) {
            Write-Host "  * Range: $($r.Start) - $($r.End)" -ForegroundColor DarkYellow
        }
    }
    
    Write-Host "========================================================================================" -ForegroundColor Cyan
    
    if ($hasConflicts) {
        Write-Host "`n[!] Hyper-V Port Conflicts Detected!" -ForegroundColor Red
        $choice = Read-Host "Would you like to automatically reconfigure conflicting ports to safe ranges? (Y/N)"
        if ($choice -eq "Y" -or $choice -eq "y") {
            Write-Host "`nReconfiguring conflicting ports..." -ForegroundColor Yellow
            
            for ($i = 0; $i -lt $Apps.Count; $i++) {
                $app = $Apps[$i]
                $httpPort = Get-PortFromUrl $app.Url
                $newHttpPort = $null
                $httpsPort = $null
                $newHttpsPort = $null
                
                # Check HTTP Port
                if ($httpPort -and (Test-CyberCOLAPortExcluded -Port $httpPort -Ranges $excludedRanges)) {
                    $newHttpPort = Find-NextAvailablePort ($httpPort + 1) $excludedRanges
                    Write-Host "  HTTP Port conflict for $($app.Name): $httpPort -> $newHttpPort" -ForegroundColor Yellow
                }
                
                # Check HTTPS Port
                if ($app.HttpsUrl) {
                    $httpsPort = Get-PortFromUrl $app.HttpsUrl
                    if ($httpsPort -and (Test-CyberCOLAPortExcluded -Port $httpsPort -Ranges $excludedRanges)) {
                        $newHttpsPort = Find-NextAvailablePort ($httpsPort + 1) $excludedRanges
                        Write-Host "  HTTPS Port conflict for $($app.Name): $httpsPort -> $newHttpsPort" -ForegroundColor Yellow
                    }
                }
                
                if ($newHttpPort -or $newHttpsPort) {
                    Reconfigure-ConflictingPorts $app $httpPort $newHttpPort $httpsPort $newHttpsPort
                }
            }
            
            # Hot reload updated launchSettings configuration back to script memory
            Update-AppsUrlsFromLaunchSettings
            Write-Host "`nReconfiguration complete! Press Enter to re-scan." -ForegroundColor Green
            Read-Host
            Show-PortsCheck # Recursive call to display updated dashboard
            return
        }
    } else {
        Read-Host "`nPress Enter to return to main menu"
    }
}

# Check port consistency between launchSettings.json and appsettings.json
function Show-PortsAlignment {
    Clear-Host
    Write-Host "========================================================================================" -ForegroundColor Cyan
    Write-Host "                       CyberCOLA Web Ports VS/Script Alignment Check                    " -ForegroundColor Cyan
    Write-Host "========================================================================================" -ForegroundColor Cyan
    Write-Host "Scanning configuration files for port consistency...`n" -ForegroundColor Gray
    
    $format = "{0,-5} | {1,-28} | {2,-15} | {3,-15} | "
    Write-Host ([string]::Format($format + "{4}", "Index", "Application Name", "launchSettings", "appsettings", "Alignment Status")) -ForegroundColor White
    Write-Host "----------------------------------------------------------------------------------------"
    
    $allAligned = $true
    for ($i = 0; $i -lt $Apps.Count; $i++) {
        $app = $Apps[$i]
        $launchPath = Join-Path $RootPath (Join-Path $app.Path "Properties\launchSettings.json")
        $appSettingsPath = Join-Path $RootPath (Join-Path $app.Path "appsettings.json")
        
        $launchHttpPort = Get-PortFromUrl $app.Url
        $kestrelHttpPort = $null
        
        # Parse appsettings.json if it has Kestrel section
        if (Test-Path $appSettingsPath) {
            try {
                $json = Get-Content $appSettingsPath -Raw | ConvertFrom-Json
                if ($json.Kestrel -and $json.Kestrel.Endpoints) {
                    foreach ($ep in $json.Kestrel.Endpoints.psobject.Properties) {
                        $url = $ep.Value.Url
                        if ($url -and $url.StartsWith("http://")) {
                            $kestrelHttpPort = Get-PortFromUrl $url
                            break
                        }
                    }
                }
            } catch {}
        }
        
        $dispKestrel = if ($kestrelHttpPort) { $kestrelHttpPort } else { "Not Defined" }
        $dispLaunch = if ($launchHttpPort) { $launchHttpPort } else { "-" }
        
        $aligned = $true
        if ($kestrelHttpPort -and $launchHttpPort -and $kestrelHttpPort -ne $launchHttpPort) {
            $aligned = $false
            $allAligned = $false
        }
        
        $statusText = if ($aligned) { "ALIGNED" } else { "MISMATCH" }
        $color = if ($aligned) { "Green" } else { "Red" }
        
        Write-Host ([string]::Format($format, ($i + 1), $app.Name, $dispLaunch, $dispKestrel)) -NoNewline
        Write-Host $statusText -ForegroundColor $color
    }
    
    Write-Host "----------------------------------------------------------------------------------------"
    if ($allAligned) {
        Write-Host "SUCCESS: All application ports are perfectly aligned!" -ForegroundColor Green
    } else {
        Write-Host "WARNING: Mismatch detected! VS Kestrel port differs from launchSettings port." -ForegroundColor Red
    }
    Write-Host "========================================================================================`n" -ForegroundColor Cyan
    Read-Host "Press Enter to return to main menu"
}

# Diagnostic test to start all apps and verify their running & listening status
function Test-AppsStartup {
    Clear-Host
    Write-Host "========================================================================================" -ForegroundColor Cyan
    Write-Host "                       CyberCOLA Web Apps Start & Status Diagnostics                     " -ForegroundColor Cyan
    Write-Host "========================================================================================" -ForegroundColor Cyan
    Write-Host "Checking current running status..." -ForegroundColor Gray
    
    $initialProcsMap = Get-AppProcessesMap
    $startedApps = New-Object System.Collections.ArrayList
    
    # 1. Start all stopped apps for testing
    Write-Host "`n[STEP 1/3] Starting all stopped applications..." -ForegroundColor Cyan
    foreach ($app in $Apps) {
        $proc = $initialProcsMap[$app.Name]
        if (-not $proc) {
            Start-WebApp $app $initialProcsMap
            $startedApps.Add($app) > $null
        } else {
            Write-Host "$($app.Name) is already running. [SKIPPED START]" -ForegroundColor Gray
        }
    }
    
    # 2. Wait for Kestrel to initialize
    Write-Host "`n[STEP 2/3] Waiting 5 seconds for applications to initialize and bind ports..." -ForegroundColor Cyan
    for ($seconds = 5; $seconds -gt 0; $seconds--) {
        Write-Host "Waiting... $seconds seconds remaining" -ForegroundColor Gray
        Start-Sleep -Seconds 1
    }
    
    # 3. Verify status
    Write-Host "`n[STEP 3/3] Verifying process status and TCP port connectivity..." -ForegroundColor Cyan
    
    $format = "{0,-5} | {1,-28} | {2,-10} | {3,-10} | {4,-10} | {5}"
    Write-Host ([string]::Format($format, "Index", "Application Name", "Port", "Process", "Port Conn", "Result")) -ForegroundColor White
    Write-Host "----------------------------------------------------------------------------------------"
    
    $currentProcsMap = Get-AppProcessesMap
    $allPass = $true
    
    for ($i = 0; $i -lt $Apps.Count; $i++) {
        $app = $Apps[$i]
        $proc = $currentProcsMap[$app.Name]
        $port = Get-PortFromUrl $app.Url
        
        $procRunning = $null -ne $proc
        $portListening = $false
        
        if ($port) {
            # Try to establish TCP connection
            try {
                $tcp = New-Object System.Net.Sockets.TcpClient
                $connectResult = $tcp.BeginConnect("127.0.0.1", $port, $null, $null)
                # Wait 500ms for connection to establish
                $success = $connectResult.AsyncWaitHandle.WaitOne(500, $true)
                if ($success -and $tcp.Connected) {
                    $tcp.EndConnect($connectResult)
                    $portListening = $true
                }
                $tcp.Close()
            } catch {}
        }
        
        $procText = if ($procRunning) { "RUNNING" } else { "STOPPED" }
        $procColor = if ($procRunning) { "Green" } else { "Red" }
        $portText = if ($portListening) { "CONNECTED" } else { "FAILED" }
        $portColor = if ($portListening) { "Green" } else { "Red" }
        
        $overallPass = $procRunning -and $portListening
        $overallText = if ($overallPass) { "PASS" } else { "FAIL" }
        $overallColor = if ($overallPass) { "Green" } else { "Red" }
        
        if (-not $overallPass) {
            $allPass = $false
        }
        
        # Output columns with aligned formatting
        Write-Host ("{0,-5} | {1,-28} | {2,-10} | " -f ($i + 1), $app.Name, $port) -NoNewline
        Write-Host ("{0,-10}" -f $procText) -ForegroundColor $procColor -NoNewline
        Write-Host " | " -NoNewline
        Write-Host ("{0,-10}" -f $portText) -ForegroundColor $portColor -NoNewline
        Write-Host " | " -NoNewline
        Write-Host $overallText -ForegroundColor $overallColor
    }
    
    Write-Host "----------------------------------------------------------------------------------------"
    if ($allPass) {
        Write-Host "DIAGNOSTICS SUCCESS: All applications started and responded correctly!" -ForegroundColor Green
    } else {
        Write-Host "DIAGNOSTICS FAILED: Some applications failed to start or bind to their ports." -ForegroundColor Red
    }
    Write-Host "========================================================================================`n" -ForegroundColor Cyan
    
    # 4. Clean up newly started apps if requested
    if ($startedApps.Count -gt 0) {
        $cleanupChoice = Read-Host "Do you want to stop the $($startedApps.Count) applications started for this test? (Y/N)"
        if ($cleanupChoice -eq "Y" -or $cleanupChoice -eq "y" -or [string]::IsNullOrEmpty($cleanupChoice)) {
            Write-Host "`nStopping diagnostic applications..." -ForegroundColor Cyan
            # Re-fetch process map just in case
            $cleanupProcsMap = Get-AppProcessesMap
            foreach ($app in $startedApps) {
                Stop-WebApp $app $cleanupProcsMap
            }
            Write-Host "Clean up complete." -ForegroundColor Gray
            Start-Sleep -Seconds 2
        } else {
            Write-Host "`nDiagnostic applications kept running." -ForegroundColor Yellow
            Start-Sleep -Seconds 2
        }
    } else {
        Read-Host "Press Enter to return to main menu"
    }
}

# Show all status
function Show-Status($procsMap = $null) {
    Clear-Host
    $titleText = "CyberCOLA Web Applications Status"
    $padLength = [math]::Max(0, [int]((90 - $titleText.Length) / 2))
    $headerLine = ("=" * 90)
    
    Write-Host $headerLine -ForegroundColor Cyan
    Write-Host ((" " * $padLength) + $titleText) -ForegroundColor Cyan
    Write-Host $headerLine -ForegroundColor Cyan
    Write-Host "Runtime Host: $dotnetHost`n" -ForegroundColor Gray
    
    # Table headers in Gray
    Write-Host ("{0,-5} | {1,-28} | {2,-10} | {3,-10} | {4}" -f "Index", "Application Name", "Status", "PID", "HTTP URL") -ForegroundColor Gray
    Write-Host ("-" * 90) -ForegroundColor Gray
    
    # Use cached process map if provided, otherwise fetch it once
    $activeMap = if ($procsMap) { $procsMap } else { Get-AppProcessesMap }
    
    for ($i = 0; $i -lt $Apps.Count; $i++) {
        $app = $Apps[$i]
        $proc = $activeMap[$app.Name]
        if ($proc) {
            $status = "RUNNING"
            $color = "Green"
            $appPid = $proc.ProcessId
        } else {
            $status = "STOPPED"
            $color = "DarkGray"
            $appPid = "-"
        }
        
        # Output columns with aligned formatting and colored status
        Write-Host ("{0,-5} | {1,-28} | " -f ($i + 1), $app.Name) -NoNewline
        Write-Host ("{0,-10}" -f $status) -ForegroundColor $color -NoNewline
        Write-Host (" | {0,-10} | {1}" -f $appPid, $app.Url)
    }
    Write-Host ("=" * 90) -ForegroundColor Cyan
    Write-Host ""
}

# Interactive CLI menu
function Show-Menu {
    while ($true) {
        # Fetch status map once per loop iteration
        $procsMap = Get-AppProcessesMap
        Show-Status $procsMap
        
        $dtString = "Current Time: $(Get-Date -format 'yyyy-MM-dd HH:mm:ss')"
        $padding = [math]::Max(0, [int]((50 - $dtString.Length) / 2))
        $centeredDT = (" " * $padding) + $dtString
        
        Write-Host "==================================================" -ForegroundColor Cyan
        Write-Host "         Web Applications Control Menu" -ForegroundColor Cyan
        Write-Host $centeredDT -ForegroundColor Gray
        Write-Host "==================================================" -ForegroundColor Cyan
        Write-Host " 1. Start ALL applications"
        Write-Host " 2. Stop ALL applications"
        Write-Host " 3. Start specific apps (e.g. 3 or 3,6,7)"
        Write-Host " 4. Stop specific apps (e.g. 4 or 4,6,7)"
        Write-Host " 5. Build specific apps (e.g. 5 or 3,6,7)"
        Write-Host " 6. Check port status of all applications"
        Write-Host " 7. Check VS/Script port alignment consistency"
        Write-Host " 8. Test starting all applications & verify status"
        Write-Host " 9. Refresh status"
        Write-Host " Q. Exit (Back to OS)" -ForegroundColor Yellow
        Write-Host "==================================================" -ForegroundColor Cyan
        Write-Host ""
        
        $choice = Read-Host "Select Option (1-9, Q to Exit)"
        
        switch ($choice) {
            "1" {
                Write-Host "`nStarting all applications..." -ForegroundColor Cyan
                foreach ($app in $Apps) {
                    Start-WebApp $app $procsMap
                }
                Write-Host "Done starting all apps. Waiting 3 seconds..." -ForegroundColor Gray
                Start-Sleep -Seconds 3
            }
            "2" {
                Write-Host "`nStopping all applications..." -ForegroundColor Cyan
                foreach ($app in $Apps) {
                    Stop-WebApp $app $procsMap
                }
                Write-Host "Done stopping all apps. Waiting 3 seconds..." -ForegroundColor Gray
                Start-Sleep -Seconds 3
            }
            "3" {
                $idxStr = Read-Host "`nEnter Index/Indices to start (e.g. 3 or 3,6,7)"
                if ($idxStr) {
                    $indices = $idxStr -split ',' | ForEach-Object { $_.Trim() }
                    foreach ($item in $indices) {
                        if ($item -match '^\d+$') {
                            $idx = [int]$item - 1
                            if ($idx -ge 0 -and $idx -lt $Apps.Count) {
                                Start-WebApp $Apps[$idx] $procsMap
                            } else {
                                Write-Host "Index $item is out of range." -ForegroundColor Red
                            }
                        } else {
                            Write-Host "Invalid index format: $item" -ForegroundColor Red
                        }
                    }
                    Write-Host "`nWaiting 2 seconds..." -ForegroundColor Gray
                    Start-Sleep -Seconds 2
                }
            }
            "4" {
                $idxStr = Read-Host "`nEnter Index/Indices to stop (e.g. 4 or 4,6,7)"
                if ($idxStr) {
                    $indices = $idxStr -split ',' | ForEach-Object { $_.Trim() }
                    foreach ($item in $indices) {
                        if ($item -match '^\d+$') {
                            $idx = [int]$item - 1
                            if ($idx -ge 0 -and $idx -lt $Apps.Count) {
                                Stop-WebApp $Apps[$idx] $procsMap
                            } else {
                                Write-Host "Index $item is out of range." -ForegroundColor Red
                            }
                        } else {
                            Write-Host "Invalid index format: $item" -ForegroundColor Red
                        }
                    }
                    Write-Host "`nWaiting 2 seconds..." -ForegroundColor Gray
                    Start-Sleep -Seconds 2
                }
            }
            "5" {
                $idxStr = Read-Host "`nEnter Index/Indices to build (e.g. 5 or 3,6,7)"
                if ($idxStr) {
                    $indices = $idxStr -split ',' | ForEach-Object { $_.Trim() }
                    foreach ($item in $indices) {
                        if ($item -match '^\d+$') {
                            $idx = [int]$item - 1
                            if ($idx -ge 0 -and $idx -lt $Apps.Count) {
                                Build-WebApp $Apps[$idx]
                            } else {
                                Write-Host "Index $item is out of range." -ForegroundColor Red
                            }
                        } else {
                            Write-Host "Invalid index format: $item" -ForegroundColor Red
                        }
                    }
                    Write-Host "`nWaiting 2 seconds..." -ForegroundColor Gray
                    Start-Sleep -Seconds 2
                }
            }
            "6" {
                Show-PortsCheck
            }
            "7" {
                Show-PortsAlignment
            }
            "8" {
                Test-AppsStartup
            }
            "9" {
                # Refresh map and redraw in next iteration
            }
            "q" {
                Write-Host "Exiting." -ForegroundColor Yellow
                exit
            }
            "Q" {
                Write-Host "Exiting." -ForegroundColor Yellow
                exit
            }
            default {
                Write-Host "Invalid choice." -ForegroundColor Red
                Start-Sleep -Seconds 1
            }
        }
    }
}

# Run from arguments if specified
if ($args.Count -gt 0) {
    $action = $args[0].ToLower()
    if ($action -eq "startall") {
        $procsMap = Get-AppProcessesMap
        foreach ($app in $Apps) {
            Start-WebApp $app $procsMap
        }
    } elseif ($action -eq "stopall") {
        $procsMap = Get-AppProcessesMap
        foreach ($app in $Apps) {
            Stop-WebApp $app $procsMap
        }
    } elseif ($action -eq "status") {
        $procsMap = Get-AppProcessesMap
        for ($i = 0; $i -lt $Apps.Count; $i++) {
            $app = $Apps[$i]
            $proc = $procsMap[$app.Name]
            $status = if ($proc) { "RUNNING (PID: $($proc.ProcessId))" } else { "STOPPED" }
            Write-Host "$($app.Name) [$status] - $($app.Url)"
        }
    } else {
        Write-Host "Usage: .\StartStop-WebApps.ps1 [startall | stopall | status]"
    }
} else {
    Show-Menu
}
