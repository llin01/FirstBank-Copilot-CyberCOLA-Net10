param([string]$AutoSelect)

<#
    CyberCOLA .NET 10 Simulator Management Menu
    Supports multi-configuration and TCP connection tracking
#>

$ScriptDir = $PSScriptRoot
. "$ScriptDir\CyberCOLA-Common.ps1"
$BaseDir = Get-CyberCOLABaseDir
$SimProjName = if ($env:COLA_SIMULATOR_TYPE) { $env:COLA_SIMULATOR_TYPE } else { "WpfStressSimulator" }
$PublishDir = Join-Path $BaseDir "publish\win-x64"

function Get-FatalMessages {
    # Only read Simulator related Fatal logs (processes starting with Wpf or Web)
    Get-CyberCOLAFatalMessages -IncludePattern @("Wpf*", "Web*")
}

# Define simulator project list
$SimProjects = @("ConsoleStressSimulator", "WpfHighStressSimulator", "WebStressSimulator", "WebFiscMaintSimulator", "WpfETicketClientSimulator", "WpfHSMSimulator", "WpfEAITCPClient", "WpfSMSSimulator", "WpfSMSWebApiServer")

function Show-Simulator-MainMenu {
    Clear-Host
    $dtString = "Current Time: $(Get-Date -format 'yyyy-MM-dd HH:mm:ss')"
    $modeText = if ($SimProjName -like "Web*") { "Web Mode ($SimProjName)" } else { "WPF Desktop Mode ($SimProjName)" }
    $modeColor = if ($SimProjName -like "Web*") { "Cyan" } else { "Yellow" }

    Write-Host "================================================================" -ForegroundColor Cyan
    Write-Host "             CyberCOLA .NET 10 Simulator Main Menu" -ForegroundColor Cyan
    Write-Host (" " * 8 + "Active Platform: $modeText") -ForegroundColor $modeColor
    Write-Host (" " * 15 + $dtString) -ForegroundColor Gray
    Write-Host "================================================================" -ForegroundColor Cyan
    Write-Host " 0. Start NCCC Simulator         |  1. Start NCCC QRCode Simulator"
    Write-Host " 2. Start FISC Simulator         |  3. Start FISC ID Verify Simulator"
    Write-Host " 4. Start SVCS Simulator         |  5. Start ICASH Simulator"
    Write-Host " 6. Start IPASS Simulator        |  7. Start HSM Simulator"
    Write-Host " 8. Start EAI TCPClient          |  9. Start SMS Simulator"
    Write-Host "10. Start SMS Web API Server     | 11. Start ALL Simulators"
    if ($SimProjName -like "Web*") {
        Write-Host "----------------------------------------------------------------" -ForegroundColor Gray
        Write-Host " [Web UI URLs]" -ForegroundColor Cyan
        Write-Host "  NCCC    : http://localhost:5200  | NCCC QR : http://localhost:5201" -ForegroundColor White
        Write-Host "  FISC    : http://localhost:5202  | FISC ID : http://localhost:5203" -ForegroundColor White
    }
    Write-Host "----------------------------------------------------------------" -ForegroundColor Gray
    Write-Host " P. Publish All Simulators       |  S. Display Running Simulators"
    Write-Host " T. Get Simulator TCP Status     |  A. Stop Simulator By ID"
    Write-Host " C. Copy Configuration           |  D. Delete All Logs"
    Write-Host " F. Get Fatal Messages           |  B. Stop All Simulators"
    Write-Host " Q. Quit (Back to Main)          |" -ForegroundColor Yellow
    Write-Host "================================================================" -ForegroundColor Cyan
}

function Get-SimTCPStatus {
    Write-Host "`n--- Active Simulator TCP Connections ---" -ForegroundColor Cyan
    Write-Host ("{0,-25} | {1,-6} | {2,-15} | {3}" -f "Process Name (PID)", "Local", "State", "Remote EndPoint") -ForegroundColor Gray
    Write-Host ("-" * 85) -ForegroundColor Gray

    $simProcs = Get-Process | Where-Object { $_.ProcessName -like "Wpf*" -or $_.ProcessName -like "Web*" } -ErrorAction SilentlyContinue
    if (-not $simProcs) {
        Write-Host "No running simulators found." -ForegroundColor Yellow
        return
    }

    # Performance Optimization: Query all TCP connections once via Win32 API / optimized netstat
    Write-Host "Fetching network connection states (Fast)..." -ForegroundColor DarkGray -NoNewline
    $allConns = Get-CyberCOLAFastTCPTable
    Write-Host " [Done]" -ForegroundColor Gray

    foreach ($p in $simProcs) {
        $conns = $allConns | Where-Object { $_.OwningProcess -eq $p.Id }
        if ($conns) {
            foreach ($c in $conns) {
                # Format State to match original (Established -> Established / Listen -> Listen)
                $remote = if ($c.RemoteAddress -eq "0.0.0.0" -or $c.RemoteAddress -eq "::" -or $c.State -eq "Listen") { "LISTENING" } else { "$($c.RemoteAddress):$($c.RemotePort)" }
                $color = if ($c.State -eq 'Established') { "Green" } else { "Gray" }
                Write-Host ("{0,-25} | {1,-6} | {2,-15} | {3}" -f "$($p.ProcessName) ($($p.Id))", $c.LocalPort, $c.State, $remote) -ForegroundColor $color
            }
        } else {
            Write-Host ("{0,-25} | No active TCP connections." -f "$($p.ProcessName) ($($p.Id))") -ForegroundColor Gray
        }
    }
}

function Start-Simulator {
    param([string]$ProjectName, [string]$Title, [string]$ConfigSubFolder = $null, [string]$Arguments = "")
    $exePaths = @(
        (Join-Path $BaseDir "publish\cross-platform\$ProjectName\$ProjectName.exe"),
        (Join-Path $PublishDir "$ProjectName\$ProjectName.exe"),
        (Join-Path $BaseDir "src\CyberCOLA\$ProjectName\bin\x64\Release\net10.0-windows\$ProjectName.exe"),
        (Join-Path $BaseDir "src\CyberCOLA\$ProjectName\bin\x64\Debug\net10.0-windows\$ProjectName.exe"),
        (Join-Path $BaseDir "src\CyberCOLA\$ProjectName\bin\x64\Release\net10.0\$ProjectName.exe"),
        (Join-Path $BaseDir "src\CyberCOLA\$ProjectName\bin\x64\Debug\net10.0\$ProjectName.exe"),
        (Join-Path $BaseDir "src\CyberCOLA\$ProjectName\bin\Release\net10.0\$ProjectName.exe"),
        (Join-Path $BaseDir "src\CyberCOLA\$ProjectName\bin\Debug\net10.0\$ProjectName.exe")
    )
    $exe = $null
    foreach ($path in $exePaths) { if (Test-Path $path) { $exe = $path; break } }
    if ($exe) {
        $exeDir = Split-Path $exe
        if ($ConfigSubFolder) {
            # Isolate configuration and execution in a dedicated subfolder to prevent appsettings.json conflicts
            $targetDir = Join-Path $exeDir $ConfigSubFolder
            if (-not (Test-Path $targetDir)) { New-Item -ItemType Directory -Path $targetDir | Out-Null }
            
            # Copy all files from parent executable directory to the isolated folder (exclude folders to avoid recursive copy)
            Get-ChildItem -Path $exeDir -File | ForEach-Object {
                $targetFile = Join-Path $targetDir $_.Name
                if (-not (Test-Path $targetFile) -or (Get-Item $_.FullName).LastWriteTime -gt (Get-Item $targetFile).LastWriteTime) {
                    Copy-Item $_.FullName $targetFile -Force
                }
            }
            
            # Copy wwwroot folder if it exists in parent executable directory
            $srcWwwroot = Join-Path $exeDir "wwwroot"
            $destWwwroot = Join-Path $targetDir "wwwroot"
            if ((Test-Path $srcWwwroot) -and (-not (Test-Path $destWwwroot))) {
                Copy-Item $srcWwwroot $destWwwroot -Recurse -Force
            }
            
            # Copy the specific appsettings.json and NLog.config
            $configProj = $ProjectName
            
            $sourceConfig = Join-Path $BaseDir "publish\Config\$configProj\$ConfigSubFolder\appsettings.json"
            if (Test-Path $sourceConfig) { 
                Copy-Item $sourceConfig (Join-Path $targetDir "appsettings.json") -Force 
            }
            
            $sourceNLog = Join-Path $BaseDir "publish\Config\$configProj\$ConfigSubFolder\NLog.config"
            if (Test-Path $sourceNLog) { 
                Copy-Item $sourceNLog (Join-Path $targetDir "NLog.config") -Force 
            }
            
            # Update paths to run from the isolated subdirectory
            $exe = Join-Path $targetDir "$ProjectName.exe"
            $exeDir = $targetDir
        }
        Write-Host "Starting $Title..." -ForegroundColor Green
        
        # Display Kestrel Web UI URL if starting a Web-based simulator
        $webUrl = $null
        $appsettingsFile = Join-Path $exeDir "appsettings.json"
        if (Test-Path $appsettingsFile) {
            $jsonText = Get-Content $appsettingsFile -Raw -ErrorAction SilentlyContinue
            if ($jsonText -and $jsonText -match '"Url":\s*"([^"]+)"') {
                $rawUrl = $matches[1]
                $webUrl = $rawUrl -replace 'http://0\.0\.0\.0', 'http://localhost' -replace 'https://0\.0\.0\.0', 'https://localhost'
            }
        }
        if ($webUrl) {
            Write-Host "  -> Web UI Access URL: " -NoNewline -ForegroundColor White
            Write-Host "$webUrl" -ForegroundColor Cyan
        }
        
        Start-Process $exe -ArgumentList $Arguments -WorkingDirectory $exeDir -WindowStyle Minimized
    } else { Write-Host "  [!] ERROR: Binary for $ProjectName NOT FOUND!" -ForegroundColor Red }
}

function Copy-Simulator-Configs {
    Write-Host "`nCopying simulator configurations from src to publish..." -ForegroundColor Green
    
    # Define config mapping: ProjectName -> Array of subfolders (if any)
    $configMappings = @{
        "WpfHighStressSimulator" = @("Nccc", "NcccQRCode", "Fisc", "FiscIDVerify")
        "WpfStressSimulator" = @("Nccc", "NcccQRCode", "Fisc", "FiscIDVerify")
        "WebStressSimulator" = @("Nccc", "NcccQRCode", "Fisc", "FiscIDVerify")
        "WpfETicketClientSimulator" = @("ICASH", "IPASS", "SVCS")
        "WpfHSMSimulator" = @($null)  # HSM Simulator has no subfolders
        "WpfEAITCPClient" = @($null)  # EAI TCPClient has no subfolders
        "WpfSMSSimulator" = @($null)  # SMS Simulator has no subfolders
        "WpfSMSWebApiServer" = @($null)  # SMS Web API Server has no subfolders
        "WpfFiscMaintSimulator" = @($null)
        "WebFiscMaintSimulator" = @($null)
    }

    foreach ($proj in $configMappings.Keys) {
        $subfolders = $configMappings[$proj]
        $srcDir = Join-Path $BaseDir "src\CyberCOLA\$proj"
        $srcConfig = Join-Path $srcDir "appsettings.json"
        $srcNlog = Join-Path $srcDir "NLog.config"

        foreach ($sub in $subfolders) {
            # Resolve destination directory
            $configProj = $proj
            $destDir = if ($sub) {
                Join-Path $BaseDir "publish\Config\$configProj\$sub"
            } else {
                if ($proj -like "Web*") {
                    Join-Path $BaseDir "publish\cross-platform\$proj"
                } else {
                    Join-Path $PublishDir $proj
                }
            }
            
            # For WebStressSimulator, if config folder does not exist, copy from WpfStressSimulator first!
            if ($proj -eq "WebStressSimulator" -and $sub -and -not (Test-Path $destDir)) {
                $wpfDestDir = Join-Path $BaseDir "publish\Config\WpfStressSimulator\$sub"
                if (Test-Path $wpfDestDir) {
                    New-Item -ItemType Directory -Path $destDir -Force | Out-Null
                    Copy-Item -Path "$wpfDestDir\*" -Destination $destDir -Force -Recurse
                    Write-Host "  - Initialized WebStressSimulator config folder $sub from WpfStressSimulator" -ForegroundColor Green
                }
            }
            # For WebFiscMaintSimulator, if config folder does not exist, copy from WpfFiscMaintSimulator first!
            if ($proj -eq "WebFiscMaintSimulator" -and -not (Test-Path $destDir)) {
                $wpfDestDir = Join-Path $BaseDir "publish\Config\WpfFiscMaintSimulator"
                if (Test-Path $wpfDestDir) {
                    New-Item -ItemType Directory -Path $destDir -Force | Out-Null
                    Copy-Item -Path "$wpfDestDir\*" -Destination $destDir -Force -Recurse
                    Write-Host "  - Initialized WebFiscMaintSimulator config folder from WpfFiscMaintSimulator" -ForegroundColor Green
                }
            }

            if (-not (Test-Path $destDir)) { New-Item -ItemType Directory -Path $destDir -Force | Out-Null }
            
            # Copy appsettings.json if exists in src and target does not exist
            $targetConfig = Join-Path $destDir "appsettings.json"
            if (Test-Path $srcConfig) {
                if (-not (Test-Path $targetConfig)) {
                    Copy-Item $srcConfig $targetConfig -Force
                    Write-Host "  - Copied appsettings.json for $proj $($sub)" -ForegroundColor Gray
                } else {
                    Write-Host "  - appsettings.json for $proj $($sub) already exists, skipping copy to protect custom settings" -ForegroundColor DarkGray
                }
            }

            # Add or update Kestrel port in appsettings.json for WebStressSimulator or WebFiscMaintSimulator
            if (($proj -eq "WebStressSimulator" -or $proj -eq "WebFiscMaintSimulator") -and (Test-Path $targetConfig)) {
                $port = 5200
                if ($proj -eq "WebStressSimulator") {
                    $port = switch ($sub) {
                        "Nccc" { 5200 }
                        "NcccQRCode" { 5201 }
                        "Fisc" { 5202 }
                        "FiscIDVerify" { 5203 }
                        default { 5200 }
                    }
                } else {
                    $port = 5210
                }
                
                $jsonText = Get-Content $targetConfig -Raw
                if ($jsonText -notlike "*Kestrel*") {
                    $kestrelBlock = "`"Kestrel`": {`r`n    `"Endpoints`": {`r`n      `"Http`": {`r`n        `"Url`": `"http://0.0.0.0:$port`"`r`n      }`r`n    }`r`n  },"
                    $jsonText = $jsonText -replace '"GeneralConfig"', "$kestrelBlock`r`n  `"GeneralConfig`""
                    Set-Content $targetConfig $jsonText -Force
                    Write-Host "  - Injected Kestrel configuration with Port $port for $proj $($sub)" -ForegroundColor Green
                } else {
                    $jsonText = $jsonText -replace '"Url":\s*"http://0\.0\.0\.0:\d+"', "`"Url`": `"http://0.0.0.0:$port`""
                    Set-Content $targetConfig $jsonText -Force
                    Write-Host "  - Updated Kestrel Port to $port for $proj $($sub)" -ForegroundColor Green
                }
            }

            # Copy or patch NLog.config
            if (Test-Path $srcNlog) {
                $destNlog = Join-Path $destDir "NLog.config"
                Copy-Item $srcNlog $destNlog -Force
                # Apply Patch-CyberCOLANLog helper to ensure LogDirectory path is 100% correct in the copied config!
                Patch-CyberCOLANLog -OutputDir $destDir -ProjName $proj -Runtime "win-x64" -SubFolder $sub
            }
        }
    }
    Write-Host "[OK] Simulator configurations copied and patched successfully." -ForegroundColor Green
}

function Publish-Simulators {
    Write-Host "`nStarting Simulator Publish..." -ForegroundColor Green
    dotnet build "$BaseDir\CyberCOLA.sln" -c Release -r win-x64 /p:Platform=x64 --no-incremental | Out-Null
    foreach ($proj in $SimProjects) {
        Write-Host "  - Publishing: $proj..." -ForegroundColor Yellow
        $projFile = Get-ChildItem -Path "$BaseDir\src\CyberCOLA" -Recurse -Filter "$proj.csproj" | Select-Object -First 1
        if ($projFile) {
            $outputDir = if ($proj -like "Web*") {
                Join-Path $BaseDir "publish\cross-platform\$proj"
            } else {
                Join-Path $PublishDir $proj
            }
            dotnet publish $projFile.FullName -c Release -r win-x64 --no-build -o $outputDir /p:DebugType=none /p:DebugSymbols=false | Out-Null
            if ($LASTEXITCODE -eq 0) {
                Patch-CyberCOLANLog -OutputDir $outputDir -ProjName $proj -Runtime "win-x64"
                Write-Host "    [OK] $proj" -ForegroundColor Green
            } else {
                Write-Host "    [FAILED] $proj" -ForegroundColor Red
            }
        }
    }
}

$isFirstRun = $true
$autoExit = $false
do {
    Show-Simulator-MainMenu
    if ($isFirstRun -and $AutoSelect) {
        $userInput = $AutoSelect.ToUpper()
        $isFirstRun = $false
        $autoExit = $true
    } else {
        Write-Host "Please make a selection ? " -ForegroundColor Red -NoNewline
        $userInput = (Read-Host).ToUpper()
    }

    switch ($userInput) {
        '0' { Start-Simulator $SimProjName "NCCC Simulator" "Nccc" "14801 AUTO" }
        '1' { Start-Simulator $SimProjName "NCCC QRCode Simulator" "NcccQRCode" "14851 AUTO" }
        '2' { Start-Simulator $SimProjName "FISC Simulator" "Fisc" "8100 AUTO" }
        '3' { Start-Simulator $SimProjName "FISC ID Verify Simulator" "FiscIDVerify" "9100 AUTO" }
        '4' { Start-Simulator "WpfETicketClientSimulator" "SVCS Simulator" "SVCS" "2 AUTO SVCS" }
        '5' { Start-Simulator "WpfETicketClientSimulator" "ICASH Simulator" "ICASH" "2 AUTO ICASH" }
        '6' { Start-Simulator "WpfETicketClientSimulator" "IPASS Simulator" "IPASS" "2 AUTO IPASS" }
        '7' { Start-Simulator "WpfHSMSimulator" "HSM Simulator" $null "AUTO" }
        '8' { Start-Simulator "WpfEAITCPClient" "EAI TCPClient" $null "AUTO" }
        '9' { Start-Simulator "WpfSMSSimulator" "SMS Simulator" $null "10000 AUTO" }
        '10' { Start-Simulator "WpfSMSWebApiServer" "SMS Web API Server" $null "AUTO" }
        '11' { 
            Start-Simulator $SimProjName "NCCC" "Nccc" "14801 AUTO"
            Start-Simulator $SimProjName "FISC" "Fisc" "8100 AUTO"
            Start-Simulator "WpfHSMSimulator" "HSM" $null "AUTO"
        }
        'P' { Publish-Simulators }
        'T' { Get-SimTCPStatus }
        'F' { Get-FatalMessages }
        'B' { Stop-CyberCOLAProcesses -Names @("Wpf*", "Web*") }
        'S' { Get-Process | Where-Object { $_.ProcessName -like "Wpf*" -or $_.ProcessName -like "Web*" } -ErrorAction SilentlyContinue | ForEach-Object { Write-Host ("{0,-30} : Running (ID: {1})" -f $_.ProcessName, $_.Id) -ForegroundColor Green } }
        'C' { Copy-Simulator-Configs }
        'D' { if ((Read-Host "Confirm delete logs? [Y/N]") -eq 'Y') { Remove-Item "$(Get-CyberCOLALogRoot)\*" -Recurse -Force -EA SilentlyContinue } }
        'A' {
            $id = Read-Host "Enter Process ID (PID) to stop"
            if ($id) {
                Stop-Process -Id $id -Force -ErrorAction SilentlyContinue
                Write-Host "Process $id stopped." -ForegroundColor Green
            }
        }
        'Q' { return }
    }
    if ($autoExit) {
        return
    }
    if ($userInput -ne "Q") {
        Write-Host "`nPress any key to continue..." -ForegroundColor Red -NoNewline
        $null = [Console]::ReadKey($true)
    }
} until ($false)
