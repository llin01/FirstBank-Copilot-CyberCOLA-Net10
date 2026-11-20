<#
    CyberCOLA Database Connection Pre-check Tool
    PURE POWERSHELL AES-GCM DECRYPTION VERSION.
    Fixed: Synchronized with ColaCommUtility.cs encryption logic.
    Requires: PowerShell 7+ (pwsh.exe) for AesGcm support.
#>

# AesGcm requires PowerShell 7+ (.NET 8+). Auto-restart in pwsh if running under Windows PowerShell 5.x.
# This guard must stay inline (before dot-sourcing CyberCOLA-Common.ps1) because Common requires Core edition.
if ($PSVersionTable.PSEdition -eq 'Desktop') {
    $pwshCmd = Get-Command pwsh -ErrorAction SilentlyContinue
    if ($pwshCmd) {
        & $pwshCmd.Source -NoProfile -File $MyInvocation.MyCommand.Path @args
        exit $LASTEXITCODE
    } else {
        Write-Warning "PowerShell 7 (pwsh.exe) is required for AES-GCM decryption but was not found."
        Write-Warning "Please install PowerShell 7: https://aka.ms/powershell"
        exit 1
    }
}


$ErrorActionPreference = "Stop"
$ScriptDir = $PSScriptRoot
. "$ScriptDir\CyberCOLA-Common.ps1"

Write-Host "`n[Pre-check] Verifying Database Connection..." -ForegroundColor Cyan

# Decrypt (or read plain) the connection string from SystemParameterInfo.xml.
# Common resolves publish\Config -> src\CyberCOLA\Config fallback and returns $null if missing/undecryptable.
$decryptedConn = Get-CyberCOLADatabaseConnectionString
if (-not $decryptedConn) {
    Write-Warning "SystemParameterInfo.xml not found or connection string could not be decrypted. Skipping check."
    return
}

try {
    # Extract server address
    $sqlServer = "localhost"
    if ($decryptedConn -match "(?:Data Source|Server)=([^;]+)") {
        $sqlServer = ($matches[1] -split '[,\\;]')[0].Trim()
    }

    Write-Host "  [Detected] Target SQL Server: " -ForegroundColor White -NoNewline
    Write-Host "$sqlServer" -ForegroundColor Yellow

    # Remote DB servers sit behind the GlobalProtect VPN: connect it first and confirm this
    # exact server is reachable. Skipped for a local server or where GlobalProtect is absent.
    $isLocal = $sqlServer -in @('localhost', '.', '(local)', '127.0.0.1', $env:COMPUTERNAME)
    $gpInstalled = $IsWindows -and (Test-Path "C:\Program Files\Palo Alto Networks\GlobalProtect\PanGPA.exe")
    if (-not $isLocal -and $gpInstalled) {
        Write-Host "  [VPN] Ensuring GlobalProtect is connected..." -ForegroundColor Cyan
        & "$ScriptDir\Connect-GlobalProtect.ps1" -Hosts $sqlServer
        if ($LASTEXITCODE -ne 0) {
            throw "GlobalProtect check failed (exit code $LASTEXITCODE); $($sqlServer):1433 is not reachable over VPN."
        }
    }

    # Perform connectivity test (TCP 1433)
    $tcpClient = New-Object System.Net.Sockets.TcpClient
    $connect = $tcpClient.BeginConnect($sqlServer, 1433, $null, $null)
    $wait = $connect.AsyncWaitHandle.WaitOne(1500, $false)

    if (-not $wait) {
        $tcpClient.Close()
        throw "Connection to SQL Server ($($sqlServer):1433) timed out."
    }

    $tcpClient.EndConnect($connect)
    $tcpClient.Close()

    Write-Host "[SUCCESS] Database Connection Verified." -ForegroundColor Green
    exit 0
} catch {
    Write-Host "`n==================================================" -ForegroundColor Yellow
    Write-Host "   WARNING: DATABASE PRE-CHECK FAILED (OFFLINE)" -ForegroundColor Yellow
    Write-Host "==================================================" -ForegroundColor Yellow
    Write-Host "Reason: $($_.Exception.Message)" -ForegroundColor White
    Write-Host "`nPlease ensure SQL Server is running and SystemParameterInfo.xml is valid." -ForegroundColor Yellow
    Write-Host "==================================================" -ForegroundColor Yellow

    $ignore = Read-Host "Would you like to IGNORE this error and continue anyway? [Y/N] (Default: N)"
    if ($ignore -eq "Y" -or $ignore -eq "y") {
        Write-Host "Proceeding with warning (Database Connection Bypassed)..." -ForegroundColor Yellow
        exit 0
    }
    exit 1
}