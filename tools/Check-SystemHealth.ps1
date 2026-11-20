# Check-SystemHealth.ps1
# CyberCOLA .NET 10 System Health Doctor & Diagnostics Tool

# Auto-restart in pwsh if running under Windows PowerShell 5.x (desktop edition)
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
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ScriptDir = $PSScriptRoot
. "$ScriptDir\CyberCOLA-Common.ps1"
$BaseDir = Get-CyberCOLABaseDir

Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host "                CyberCOLA System Diagnostics Doctor" -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host "Executing full environment health check..." -ForegroundColor DarkGray
Write-Host ""

$PassedCount = 0
$FailedCount = 0

# --- Check 1: Database Connectivity ---
Write-Host "[1/3] Database Connectivity Check:" -ForegroundColor Cyan
try {
    $decryptedConn = Get-CyberCOLADatabaseConnectionString
    if (-not $decryptedConn) {
        throw "Could not decrypt connection string or SystemParameterInfo.xml is missing."
    }

    $sqlServer = "localhost"
    if ($decryptedConn -match "(?:Data Source|Server)=([^;]+)") {
        $sqlServer = ($matches[1] -split '[,\\;]')[0].Trim()
    }

    # TCP Port 1433 test (1.5s timeout)
    $tcpClient = New-Object System.Net.Sockets.TcpClient
    $connect = $tcpClient.BeginConnect($sqlServer, 1433, $null, $null)
    $wait = $connect.AsyncWaitHandle.WaitOne(1500, $false)

    if (-not $wait) {
        $tcpClient.Close()
        throw "Connection to SQL Server ($($sqlServer):1433) timed out."
    }

    $tcpClient.EndConnect($connect)
    $tcpClient.Close()

    Write-Host "  STATUS: " -NoNewline
    Write-Host "[PASS]" -ForegroundColor Green -NoNewline
    Write-Host " SQL Server ($sqlServer) is Online." -ForegroundColor White
    $PassedCount++
} catch {
    Write-Host "  STATUS: " -NoNewline
    Write-Host "[FAIL]" -ForegroundColor Red -NoNewline
    Write-Host " SQL Server Connection failed: $($_.Exception.Message)" -ForegroundColor White
    $FailedCount++
}
Write-Host ""

# --- Check 2: Hyper-V Port Exclusion Check ---
Write-Host "[2/3] Hyper-V Kestrel Port Conflicts Check:" -ForegroundColor Cyan
try {
    # Load launchSettings ports from web apps
    $webAppsRoot = Join-Path $BaseDir "src\CyberCOLA"
    $launchSettingsFiles = Get-ChildItem -Path $webAppsRoot -Filter "launchSettings.json" -Recurse | Where-Object { $_.FullName -notlike "*\bin\*" -and $_.FullName -notlike "*\obj\*" }
    
    # Get excluded ports from netsh
    $exclusions = Get-CyberCOLAExcludedPortRanges

    $conflicts = @()
    foreach ($file in $launchSettingsFiles) {
        $projName = $file.Directory.Parent.Name
        $json = Get-Content $file.FullName -Raw | ConvertFrom-Json -ErrorAction SilentlyContinue
        if ($json -and $json.profiles) {
            foreach ($profileName in $json.profiles.psobject.Properties.Name) {
                $profile = $json.profiles.$profileName
                if ($profile.applicationUrl) {
                    $urls = $profile.applicationUrl -split ';'
                    foreach ($url in $urls) {
                        if ($url -match ':(\d+)') {
                            $port = [int]$matches[1]
                            # Check exclusions
                            foreach ($ex in $exclusions) {
                                if ($port -ge $ex.Start -and $port -le $ex.End) {
                                    $conflicts += [PSCustomObject]@{
                                        Project = $projName
                                        Port    = $port
                                        Start   = $ex.Start
                                        End     = $ex.End
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    if ($conflicts.Count -gt 0) {
        Write-Host "  STATUS: " -NoNewline
        Write-Host "[WARN/FAIL]" -ForegroundColor Red -NoNewline
        Write-Host " Found $($conflicts.Count) conflicting ports blocked by Hyper-V:" -ForegroundColor White
        foreach ($c in $conflicts) {
            Write-Host "    - Project: " -NoNewline
            Write-Host "$($c.Project)" -ForegroundColor Yellow -NoNewline
            Write-Host " Port: " -NoNewline
            Write-Host "$($c.Port)" -ForegroundColor Red -NoNewline
            Write-Host " falls in excluded range [$($c.Start)-$($c.End)]" -ForegroundColor Gray
        }
        $FailedCount++
    } else {
        Write-Host "  STATUS: " -NoNewline
        Write-Host "[PASS]" -ForegroundColor Green -NoNewline
        Write-Host " No Kestrel ports conflict with Hyper-V exclusion ranges." -ForegroundColor White
        $PassedCount++
    }
} catch {
    Write-Host "  STATUS: " -NoNewline
    Write-Host "[WARN]" -ForegroundColor Yellow -NoNewline
    Write-Host " Failed to check port conflicts: $($_.Exception.Message)" -ForegroundColor White
}
Write-Host ""



# --- Check 3: NLog Logging Directory ---
Write-Host "[3/3] Centralized Log Directory Check:" -ForegroundColor Cyan
$logDir = Join-Path $BaseDir "LogFiles"
try {
    if (-not (Test-Path $logDir)) {
        New-Item -ItemType Directory -Path $logDir -Force | Out-Null
    }
    
    # Test writing and deleting a temporary diagnostic file
    $testFile = Join-Path $logDir "diag_test.tmp"
    [System.IO.File]::WriteAllText($testFile, "diag test")
    Remove-Item $testFile -Force
    
    Write-Host "  STATUS: " -NoNewline
    Write-Host "[PASS]" -ForegroundColor Green -NoNewline
    Write-Host " Centralized log directory ($logDir) is writable." -ForegroundColor White
    $PassedCount++
} catch {
    Write-Host "  STATUS: " -NoNewline
    Write-Host "[FAIL]" -ForegroundColor Red -NoNewline
    Write-Host " Log directory write test failed: $($_.Exception.Message)" -ForegroundColor White
    $FailedCount++
}

# --- Final Diagnostic Summary ---
Write-Host ""
Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host "                         DIAGNOSTIC REPORT SUMMARY" -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host "Passed: " -NoNewline
Write-Host "$PassedCount" -ForegroundColor Green -NoNewline
Write-Host " | Failed: " -NoNewline
Write-Host "$FailedCount" -ForegroundColor $(if ($FailedCount -gt 0) { "Red" } else { "Green" })

if ($FailedCount -gt 0) {
    Write-Host "`n[Diagnostic Doctor Alert] Some environmental checks failed." -ForegroundColor Red
    Write-Host "Please resolve conflicts or launch missing services before proceeding." -ForegroundColor Yellow
} else {
    Write-Host "`n[Diagnostic Doctor Success] All environment checks passed successfully!" -ForegroundColor Green
}
Write-Host "======================================================================" -ForegroundColor Cyan
