# ==============================================================================
# CyberCOLA One-Click Script: Start NCCC Services, HSM, & NCCC Simulators
# All comments are in English to maintain international engineering standards.
# ==============================================================================

$ScriptDir = $PSScriptRoot
. "$ScriptDir\CyberCOLA-Common.ps1"

# Database connectivity pre-check. If failed, abort and return to menu immediately.
& "$ScriptDir\Test-DatabaseConnection.ps1"
if ($LASTEXITCODE -ne 0) {
    Write-Host "`n[ERROR] Database connection failed! Aborting NCCC Flow." -ForegroundColor Red
    Write-Host "Press any key to return to main menu..." -ForegroundColor Red
    $null = [Console]::ReadKey($true)
    return
}

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "         Starting CyberCOLA NCCC Flow" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan

# 0. Adjust Cola session count (read by CyberAuthNCCC at startup)
Prompt-CyberCOLASessionCount -AuthProject 'CyberAuthNCCC'

# 1. Start NCCC Gateway & Authorization (Includes ServiceRouter if not running)
if (Confirm-CyberCOLA -Prompt 'Start NCCC Gateway & Authorization?') {
    Write-Host "`n>>> [1/6] Starting NCCC Gateway & Authorization..." -ForegroundColor Yellow
    & "$ScriptDir\COLAMainMenu-Console.ps1" -AutoSelect "0"
}

# 2. Start HSM Simulator
if (Confirm-CyberCOLA -Prompt 'Start HSM Simulator?') {
    Write-Host "`n>>> [2/6] Starting HSM Simulator..." -ForegroundColor Yellow
    & "$ScriptDir\COLASimulatorMainMenu.ps1" -AutoSelect "7"
}

# 3. Start NCCC Simulator (Forced WPF version as requested)
if (Confirm-CyberCOLA -Prompt 'Start NCCC Simulator?') {
    Write-Host "`n>>> [3/6] Starting NCCC Simulator (WPF)..." -ForegroundColor Yellow
    $oldSimType = $env:COLA_SIMULATOR_TYPE
    $env:COLA_SIMULATOR_TYPE = "WpfStressSimulator"
    & "$ScriptDir\COLASimulatorMainMenu.ps1" -AutoSelect "0"
    $env:COLA_SIMULATOR_TYPE = $oldSimType
}

# 4. Start NCCC QRCode Simulator (Forced WPF version as requested)
if (Confirm-CyberCOLA -Prompt 'Start NCCC QRCode Simulator?') {
    Write-Host "`n>>> [4/6] Starting NCCC QRCode Simulator (WPF)..." -ForegroundColor Yellow
    $oldSimType = $env:COLA_SIMULATOR_TYPE
    $env:COLA_SIMULATOR_TYPE = "WpfStressSimulator"
    & "$ScriptDir\COLASimulatorMainMenu.ps1" -AutoSelect "1"
    $env:COLA_SIMULATOR_TYPE = $oldSimType
}

# 5. Start SMS Simulator
if (Confirm-CyberCOLA -Prompt 'Start SMS Simulator?') {
    Write-Host "`n>>> [5/6] Starting SMS Simulator..." -ForegroundColor Yellow
    & "$ScriptDir\COLASimulatorMainMenu.ps1" -AutoSelect "9"
}

# 6. Start SMS Web API Server
if (Confirm-CyberCOLA -Prompt 'Start SMS Web API Server?') {
    Write-Host "`n>>> [6/6] Starting SMS Web API Server..." -ForegroundColor Yellow
    & "$ScriptDir\COLASimulatorMainMenu.ps1" -AutoSelect "10"
}

Write-Host "`n==================================================" -ForegroundColor Green
Write-Host "    [SUCCESS] NCCC Flow Initialization Completed." -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Green
