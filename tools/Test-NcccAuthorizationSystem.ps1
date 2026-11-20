# ==============================================================================
# CyberCOLA NCCC Authorization System Automated Test & Lifecycle Manager
# (Running Published Binaries Mode with Mapped Service Names)
# ==============================================================================

# AesGcm (used via Get-CyberCOLADatabaseConnectionString) requires PowerShell 7+ (.NET 8+).
# Auto-restart in pwsh if running under Windows PowerShell 5.x (desktop edition).
# This guard must stay inline (before dot-sourcing CyberCOLA-Common.ps1) because Common requires Core edition.
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

# Script Configuration
$ErrorActionPreference = "Stop"
$ScriptDir = $PSScriptRoot
. "$ScriptDir\CyberCOLA-Common.ps1"
$RootDir = Get-CyberCOLABaseDir

# Track all started process handles for clean teardown
$Script:RunningProcesses = @()

function Write-Log {
    param(
        [string]$Message,
        [string]$Level = "INFO"
    )
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    switch ($Level) {
        "SUCCESS" { Write-Host "[$timestamp] [SUCCESS] $Message" -ForegroundColor Green }
        "WARN"    { Write-Host "[$timestamp] [WARN]    $Message" -ForegroundColor Yellow }
        "ERROR"   { Write-Host "[$timestamp] [ERROR]   $Message" -ForegroundColor Red }
        default   { Write-Host "[$timestamp] [INFO]    $Message" -ForegroundColor Cyan }
    }
}

function Stop-AllServices {
    Write-Log "Stopping all running NCCC authorization services..." "WARN"
    
    foreach ($proc in $Script:RunningProcesses) {
        try {
            if ($proc -and -not $proc.HasExited) {
                $name = if ($proc.ServiceName) { $proc.ServiceName } else { $proc.ProcessName }
                Write-Log "Terminating Service Process: $name (PID: $($proc.Id))..." "WARN"
                Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
            }
        } catch { }
    }
    
    # Extra safety cleanup by process name
    $serviceNames = @("ServiceRouter", "CyberAuthNCCC", "CyberGWNCCC", "ConsoleStressSimulator")
    foreach ($name in $serviceNames) {
        $procs = Get-Process -Name $name -ErrorAction SilentlyContinue
        if ($procs) {
            Write-Log "Cleaning up remnant process: $name..." "WARN"
            $procs | Stop-Process -Force -ErrorAction SilentlyContinue
        }
    }
    
    $Script:RunningProcesses = @()
    Write-Log "All services stopped successfully." "SUCCESS"
}

function Get-PublishedDllPath {
    param([string]$ServiceName)

    # 1. Search publish\$ServiceName\$ServiceName.dll
    $path1 = Join-Path $RootDir "publish\$ServiceName\$ServiceName.dll"
    if (Test-Path $path1) { return $path1 }

    # 2. Search publish\win-x64\$ServiceName\$ServiceName.dll
    $path2 = Join-Path $RootDir "publish\win-x64\$ServiceName\$ServiceName.dll"
    if (Test-Path $path2) { return $path2 }

    # 3. Search publish\cross-platform\$ServiceName\$ServiceName.dll
    $path3 = Join-Path $RootDir "publish\cross-platform\$ServiceName\$ServiceName.dll"
    if (Test-Path $path3) { return $path3 }

    # 4. Auto-publish on-the-fly if not found
    Write-Log "Publish binary not found for $ServiceName. Auto-publishing now..." "WARN"
    $csproj = Join-Path $RootDir "src\CyberCOLA\$ServiceName\$ServiceName.csproj"
    $outDir = Join-Path $RootDir "publish\$ServiceName"

    if (Test-Path $csproj) {
        $pubCmd = "dotnet publish `"$csproj`" -c Release -o `"$outDir`""
        Invoke-Expression $pubCmd
        if (Test-Path $path1) { return $path1 }
    }

    return $null
}

# Prefer the apphost .exe next to the DLL so the OS process name is the service name
# (Get-Process -Name and the name-based cleanup in Stop-AllServices can find it);
# fall back to "dotnet <dll>" when no .exe was published.
function Start-PublishedService {
    param([string]$DllPath, [string]$Arguments, [switch]$Hidden)

    $exePath = [System.IO.Path]::ChangeExtension($DllPath, ".exe")
    $startArgs = @{ PassThru = $true }
    if (Test-Path $exePath) {
        $startArgs.FilePath = $exePath
        if ($Arguments) { $startArgs.ArgumentList = $Arguments }
    } else {
        $startArgs.FilePath = "dotnet"
        $startArgs.ArgumentList = "`"$DllPath`" $Arguments".TrimEnd()
    }
    if ($Hidden) { $startArgs.WindowStyle = "Hidden" }
    return Start-Process @startArgs
}

# Snapshot of all TCP listeners as PID -> "addr:port" list.
# Uses the shared Win32 table instead of Get-NetTCPConnection: the CIM-backed cmdlet takes
# ~30s per call on hosts with thousands of TCP connections (e.g. during stress tests).
function Get-ListenSocketSnapshot {
    $map = @{}
    try {
        foreach ($row in (Get-CyberCOLAFastTCPTable | Where-Object State -eq 'Listen')) {
            $ownerPid = [int]$row.OwningProcess
            if (-not $map.ContainsKey($ownerPid)) { $map[$ownerPid] = [System.Collections.Generic.List[string]]::new() }
            $map[$ownerPid].Add("$($row.LocalAddress):$($row.LocalPort)")
        }
    } catch { }
    return $map
}

# Pass a snapshot when querying several processes so the TCP table is read only once.
function Get-ProcessListenPorts {
    param([int]$ProcessId, [hashtable]$Snapshot = (Get-ListenSocketSnapshot))

    if ($Snapshot.ContainsKey($ProcessId)) {
        return ($Snapshot[$ProcessId] -join ", ")
    }
    return "No Active Socket Listener"
}

# ------------------------------------------------------------------------------
# Step 1: Check Database Connection using direct ConnectionString
# ------------------------------------------------------------------------------
function Step1-CheckDatabaseConnection {
    Write-Host "`n==========================================================================" -ForegroundColor Cyan
    Write-Host " [Step 1/7] Checking Database Connection (Port: 1433)..." -ForegroundColor Cyan
    Write-Host "==========================================================================" -ForegroundColor Cyan

    # Decrypt (or read plain) the connection string from SystemParameterInfo.xml instead of
    # hardcoding credentials in source. Common resolves publish\Config -> src\CyberCOLA\Config fallback.
    $connectionString = Get-CyberCOLADatabaseConnectionString
    $dbHost = "DBServer"
    $dbPort = 1433

    if (-not $connectionString) {
        Write-Log "SystemParameterInfo.xml not found or connection string could not be decrypted. Falling back to default target ${dbHost}:${dbPort}." "WARN"
    } else {
        Write-Log "ConnectionString Target resolved from SystemParameterInfo.xml." "INFO"
    }

    if ($connectionString -match "Data Source=([^;,\s]+)") {
        $rawHost = $matches[1]
        if ($rawHost -match "([^,]+),(\d+)") {
            $dbHost = $matches[1]
            $dbPort = [int]$matches[2]
        } else {
            $dbHost = $rawHost
        }
    }

    Write-Log "Testing database connectivity to ${dbHost}:${dbPort}..." "INFO"

    $connectionSuccess = $false

    try {
        $tcpClient = New-Object System.Net.Sockets.TcpClient
        $asyncResult = $tcpClient.BeginConnect($dbHost, $dbPort, $null, $null)
        $waitResult = $asyncResult.AsyncWaitHandle.WaitOne(3000, $false)
        
        if ($waitResult -and $tcpClient.Connected) {
            $tcpClient.Close()
            $connectionSuccess = $true
            Write-Log "Database Server (${dbHost}:${dbPort}) TCP Socket connection SUCCESSFUL!" "SUCCESS"
        } else {
            $tcpClient.Close()
            # Localhost fallback check
            $tcpLocal = New-Object System.Net.Sockets.TcpClient
            $asyncLocal = $tcpLocal.BeginConnect("localhost", 1433, $null, $null)
            if ($asyncLocal.AsyncWaitHandle.WaitOne(2000, $false) -and $tcpLocal.Connected) {
                $tcpLocal.Close()
                $connectionSuccess = $true
                Write-Log "Database Server (Localhost Fallback 127.0.0.1:1433) connection SUCCESSFUL!" "SUCCESS"
            } else {
                $tcpLocal.Close()
            }
        }
    } catch {
        Write-Log "Database connection exception: $_" "ERROR"
    }

    if (-not $connectionSuccess) {
        Write-Log "[STOP] Database connection failed for ${dbHost}:${dbPort}. Aborting all subsequent steps." "ERROR"
        return $false
    }
    return $true
}

# ------------------------------------------------------------------------------
# Step 2: Start ServiceRouter
# ------------------------------------------------------------------------------
function Step2-StartServiceRouter {
    Write-Host "`n==========================================================================" -ForegroundColor Cyan
    Write-Host " [Step 2/7] Starting ServiceRouter..." -ForegroundColor Cyan
    Write-Host "==========================================================================" -ForegroundColor Cyan

    $dllPath = Get-PublishedDllPath "ServiceRouter"
    if (-not $dllPath) {
        Write-Log "[SKIP] Published binary for ServiceRouter not found." "WARN"
        return $false
    }

    try {
        Write-Log "Launching ServiceRouter from: $dllPath" "INFO"
        $proc = Start-PublishedService $dllPath -Hidden

        Start-Sleep -Seconds 3
        if ($proc.HasExited) {
            Write-Log "[SKIP] ServiceRouter failed to start (Process exited immediately). Skipping remaining steps." "WARN"
            return $false
        }

        $proc | Add-Member -NotePropertyName "ServiceName" -NotePropertyValue "ServiceRouter" -Force
        $Script:RunningProcesses += $proc
        $listenPorts = Get-ProcessListenPorts $proc.Id
        Write-Log "ServiceRouter started (PID: $($proc.Id), Socket Listening Port: $listenPorts)!" "SUCCESS"
        return $true
    } catch {
        Write-Log "[SKIP] Failed to launch ServiceRouter: $_" "WARN"
        return $false
    }
}

# ------------------------------------------------------------------------------
# Step 3: Start CyberAuthNCCC
# ------------------------------------------------------------------------------
function Step3-StartCyberAuthNCCC {
    Write-Host "`n==========================================================================" -ForegroundColor Cyan
    Write-Host " [Step 3/7] Starting CyberAuthNCCC..." -ForegroundColor Cyan
    Write-Host "==========================================================================" -ForegroundColor Cyan

    $dllPath = Get-PublishedDllPath "CyberAuthNCCC"
    if (-not $dllPath) {
        Write-Log "[STOP] Published binary for CyberAuthNCCC not found." "ERROR"
        Stop-AllServices
        return $false
    }

    try {
        Write-Log "Launching CyberAuthNCCC from: $dllPath" "INFO"
        $proc = Start-PublishedService $dllPath -Hidden

        Start-Sleep -Seconds 3
        if ($proc.HasExited) {
            Write-Log "[STOP] CyberAuthNCCC failed to start. Stopping all running services and aborting." "ERROR"
            Stop-AllServices
            return $false
        }

        $proc | Add-Member -NotePropertyName "ServiceName" -NotePropertyValue "CyberAuthNCCC" -Force
        $Script:RunningProcesses += $proc
        $listenPorts = Get-ProcessListenPorts $proc.Id
        Write-Log "CyberAuthNCCC started (PID: $($proc.Id), Socket Listening Port: $listenPorts)!" "SUCCESS"
        return $true
    } catch {
        Write-Log "[STOP] Failed to launch CyberAuthNCCC: $_" "ERROR"
        Stop-AllServices
        return $false
    }
}

# ------------------------------------------------------------------------------
# Step 4: Start CyberGWNCCC
# ------------------------------------------------------------------------------
function Step4-StartCyberGWNCCC {
    Write-Host "`n==========================================================================" -ForegroundColor Cyan
    Write-Host " [Step 4/7] Starting CyberGWNCCC Gateway..." -ForegroundColor Cyan
    Write-Host "==========================================================================" -ForegroundColor Cyan

    $dllPath = Get-PublishedDllPath "CyberGWNCCC"
    if (-not $dllPath) {
        Write-Log "[STOP] Published binary for CyberGWNCCC not found." "ERROR"
        Stop-AllServices
        return $false
    }

    try {
        Write-Log "Launching CyberGWNCCC Gateway from: $dllPath" "INFO"
        $proc = Start-PublishedService $dllPath -Hidden

        Start-Sleep -Seconds 3
        if ($proc.HasExited) {
            Write-Log "[STOP] CyberGWNCCC Gateway failed to start. Stopping all running services and aborting." "ERROR"
            Stop-AllServices
            return $false
        }

        $proc | Add-Member -NotePropertyName "ServiceName" -NotePropertyValue "CyberGWNCCC" -Force
        $Script:RunningProcesses += $proc
        $listenPorts = Get-ProcessListenPorts $proc.Id
        Write-Log "CyberGWNCCC Gateway started (PID: $($proc.Id), Socket Listening Port: $listenPorts)!" "SUCCESS"
        return $true
    } catch {
        Write-Log "[STOP] Failed to launch CyberGWNCCC Gateway: $_" "ERROR"
        Stop-AllServices
        return $false
    }
}

# ------------------------------------------------------------------------------
# Step 5: Wait 2 Seconds
# ------------------------------------------------------------------------------
function Step5-WaitTwoSeconds {
    Write-Host "`n==========================================================================" -ForegroundColor Cyan
    Write-Host " [Step 5/7] Waiting 2 seconds for services synchronization..." -ForegroundColor Cyan
    Write-Host "==========================================================================" -ForegroundColor Cyan
    Start-Sleep -Seconds 2
    Write-Log "Synchronization wait complete." "SUCCESS"
}

# ------------------------------------------------------------------------------
# Step 6: Start ConsoleStressSimulator
# ------------------------------------------------------------------------------
function Step6-StartConsoleStressSimulator {
    Write-Host "`n==========================================================================" -ForegroundColor Cyan
    Write-Host " [Step 6/7] Starting ConsoleStressSimulator..." -ForegroundColor Cyan
    Write-Host "==========================================================================" -ForegroundColor Cyan

    $dllPath = Get-PublishedDllPath "ConsoleStressSimulator"
    if (-not $dllPath) {
        Write-Log "[STOP] Published binary for ConsoleStressSimulator not found." "ERROR"
        Stop-AllServices
        return $false
    }

    try {
        Write-Log "Launching ConsoleStressSimulator with --auto-start from: $dllPath" "INFO"
        $proc = Start-PublishedService $dllPath "--auto-start"

        Start-Sleep -Seconds 2
        if ($proc.HasExited) {
            Write-Log "[STOP] ConsoleStressSimulator failed to start. Stopping all running services." "ERROR"
            Stop-AllServices
            return $false
        }

        $proc | Add-Member -NotePropertyName "ServiceName" -NotePropertyValue "ConsoleStressSimulator" -Force
        $Script:RunningProcesses += $proc
        $listenPorts = Get-ProcessListenPorts $proc.Id
        Write-Log "ConsoleStressSimulator started (PID: $($proc.Id), Socket Listening Port: $listenPorts)!" "SUCCESS"
        return $true
    } catch {
        Write-Log "[STOP] Failed to launch ConsoleStressSimulator: $_" "ERROR"
        Stop-AllServices
        return $false
    }
}

# ------------------------------------------------------------------------------
# Step 7: Display Operation Menu & Service Teardown Manager (With Mapped Service Names)
# ------------------------------------------------------------------------------
function Step7-DisplayOperationMenu {
    while ($true) {
        Clear-Host
        Write-Host "==========================================================================" -ForegroundColor Green
        Write-Host "  CyberCOLA NCCC Authorization Test Manager - System Control Menu" -ForegroundColor Green
        Write-Host "==========================================================================" -ForegroundColor Green
        Write-Host " [Active Services Realtime Socket Listening Ports]:" -ForegroundColor Cyan
        $snapshot = Get-ListenSocketSnapshot
        foreach ($p in $Script:RunningProcesses) {
            $serviceName = if ($p.ServiceName) { $p.ServiceName } else { $p.ProcessName }
            if (-not $p.HasExited) {
                $listenSocket = Get-ProcessListenPorts $p.Id $snapshot
                Write-Host "   - $serviceName (PID: $($p.Id)) -> Listening Socket: [$listenSocket]" -ForegroundColor Green
            } else {
                Write-Host "   - $serviceName (PID: $($p.Id)): STOPPED" -ForegroundColor Red
            }
        }
        Write-Host "--------------------------------------------------------------------------" -ForegroundColor Yellow
        Write-Host "  [1] 檢視與掃描所有服務狀態 (Check Services Status & Socket Ports)" -ForegroundColor Yellow
        Write-Host "  [2] 重啟 ConsoleStressSimulator (Restart ConsoleStressSimulator)" -ForegroundColor Yellow
        Write-Host "  [9] 停止所有已啟動服務並離開 (Stop All Services & Exit)" -ForegroundColor Red
        Write-Host "--------------------------------------------------------------------------" -ForegroundColor Yellow
        Write-Host -NoNewline "請選擇操作選項 [1, 2, 9]: "

        $choice = Read-Host
        switch ($choice.Trim()) {
            "1" {
                Write-Host "`n--- OS Realtime NetTCP Socket Listening Report ---" -ForegroundColor Cyan
                $snapshot = Get-ListenSocketSnapshot
                foreach ($p in $Script:RunningProcesses) {
                    $serviceName = if ($p.ServiceName) { $p.ServiceName } else { $p.ProcessName }
                    if (-not $p.HasExited) {
                        $socketInfo = Get-ProcessListenPorts $p.Id $snapshot
                        Write-Host "  Service: $serviceName | PID: $($p.Id) | Socket Listen: $socketInfo" -ForegroundColor Yellow
                    }
                }
                Write-Host "`nPress any key to return to main menu..." -ForegroundColor Gray
                [void][System.Console]::ReadKey($true)
            }
            "2" {
                Write-Log "Restarting ConsoleStressSimulator with --auto-start..." "INFO"
                $dllPath = Get-PublishedDllPath "ConsoleStressSimulator"
                if ($dllPath) {
                    $proc = Start-PublishedService $dllPath "--auto-start"
                    $proc | Add-Member -NotePropertyName "ServiceName" -NotePropertyValue "ConsoleStressSimulator" -Force
                    $Script:RunningProcesses += $proc
                }
                Start-Sleep -Seconds 1
            }
            "9" {
                return
            }
            default {
                # Continue loop for invalid input
            }
        }
    }
}

# ------------------------------------------------------------------------------
# Main Workflow Execution
# ------------------------------------------------------------------------------
Write-Host "==========================================================================" -ForegroundColor Green
Write-Host "   CyberCOLA NCCC Authorization System Automated Test Launcher" -ForegroundColor Green
Write-Host "==========================================================================" -ForegroundColor Green

try {
    # Step 1: Database Check
    if (-not (Step1-CheckDatabaseConnection)) { return }

    # Step 2: Start ServiceRouter
    if (-not (Step2-StartServiceRouter)) { return }

    # Step 3: Start CyberAuthNCCC
    if (-not (Step3-StartCyberAuthNCCC)) { return }

    # Step 4: Start CyberGWNCCC
    if (-not (Step4-StartCyberGWNCCC)) { return }

    # Step 5: Wait 2 Seconds
    Step5-WaitTwoSeconds

    # Step 6: Start ConsoleStressSimulator
    if (-not (Step6-StartConsoleStressSimulator)) { return }

    # Step 7: Display Operation Menu & Lifecycle Management
    Step7-DisplayOperationMenu
}
finally {
    # Ensure all services are stopped cleanly upon exit or user interrupt
    Stop-AllServices
}
