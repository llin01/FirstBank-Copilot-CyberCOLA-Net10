# ==============================================================================
# CyberCOLA One-Click Script: Start FISC Services, HSM, & FISC Simulators
# All comments are in English to maintain international engineering standards.
# ==============================================================================

$ScriptDir = $PSScriptRoot
. "$ScriptDir\CyberCOLA-Common.ps1"

# Database connectivity pre-check. If failed, abort and return to menu immediately.
& "$ScriptDir\Test-DatabaseConnection.ps1"
if ($LASTEXITCODE -ne 0) {
    Write-Host "`n[ERROR] Database connection failed! Aborting FISC Flow." -ForegroundColor Red
    Write-Host "Press any key to return to main menu..." -ForegroundColor Red
    $null = [Console]::ReadKey($true)
    return
}

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "         Starting CyberCOLA FISC Flow" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan

# 0. Adjust Cola session count (read by CyberAuthFISC at startup)
Prompt-CyberCOLASessionCount -AuthProject 'CyberAuthFISC'

# 1. Start FISC Microservices (Includes ServiceRouter if not running)
if (Confirm-CyberCOLA -Prompt 'Start FISC Microservices & Router?') {
    Write-Host "`n>>> [1/5] Starting FISC Microservices & Router..." -ForegroundColor Yellow
    & "$ScriptDir\COLAMainMenu-Console.ps1" -AutoSelect "2"
}

# 2. Start HSM Simulator
if (Confirm-CyberCOLA -Prompt 'Start HSM Simulator?') {
    Write-Host "`n>>> [2/5] Starting HSM Simulator..." -ForegroundColor Yellow
    & "$ScriptDir\COLASimulatorMainMenu.ps1" -AutoSelect "7"
}

# 3. Start FISC Simulator (Forced WPF version as requested)
if (Confirm-CyberCOLA -Prompt 'Start FISC Simulator?') {
    Write-Host "`n>>> [3/5] Starting FISC Simulator (WPF)..." -ForegroundColor Yellow
    $oldSimType = $env:COLA_SIMULATOR_TYPE
    $env:COLA_SIMULATOR_TYPE = "WpfStressSimulator"
    & "$ScriptDir\COLASimulatorMainMenu.ps1" -AutoSelect "2"
    $env:COLA_SIMULATOR_TYPE = $oldSimType
}

# 4. Start FISC ID Verify Simulator (Forced WPF version as requested)
if (Confirm-CyberCOLA -Prompt 'Start FISC ID Verify Simulator?') {
    Write-Host "`n>>> [4/5] Starting FISC ID Verify Simulator (WPF)..." -ForegroundColor Yellow
    $oldSimType = $env:COLA_SIMULATOR_TYPE
    $env:COLA_SIMULATOR_TYPE = "WpfStressSimulator"
    & "$ScriptDir\COLASimulatorMainMenu.ps1" -AutoSelect "3"
    $env:COLA_SIMULATOR_TYPE = $oldSimType
}

# 5. Start SMS Web API Server
if (Confirm-CyberCOLA -Prompt 'Start SMS Web API Server?') {
    Write-Host "`n>>> [5/5] Starting SMS Web API Server..." -ForegroundColor Yellow
    & "$ScriptDir\COLASimulatorMainMenu.ps1" -AutoSelect "10"
}

Write-Host "`n==================================================" -ForegroundColor Green
Write-Host "    [SUCCESS] FISC Flow Initialization Completed." -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Green
