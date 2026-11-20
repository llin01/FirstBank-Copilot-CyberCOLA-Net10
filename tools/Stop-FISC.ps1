# ==============================================================================
# CyberCOLA One-Click Script: Stop FISC Services & Simulators
# All comments are in English to maintain international engineering standards.
# ==============================================================================

$ScriptDir = $PSScriptRoot
. "$ScriptDir\CyberCOLA-Common.ps1"
$SimProjName = if ($env:COLA_SIMULATOR_TYPE) { $env:COLA_SIMULATOR_TYPE } else { "WpfHighStressSimulator" }

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "         Stopping CyberCOLA FISC Flow" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan

# 1. Stop SMS Web API Server
Write-Host "`n>>> [1/5] Stopping SMS Web API Server..." -ForegroundColor Yellow
Stop-CyberCOLAProcessesByCommandLine -ProjectName 'WpfSMSWebApiServer'

# 2. Stop FISC ID Verify Simulator
Write-Host "`n>>> [2/5] Stopping FISC ID Verify Simulator..." -ForegroundColor Yellow
Stop-CyberCOLAProcessesByCommandLine -ProjectName $SimProjName -CommandLinePattern 'FiscIDVerify'

# 3. Stop FISC Simulator
Write-Host "`n>>> [3/5] Stopping FISC Simulator..." -ForegroundColor Yellow
Stop-CyberCOLAProcessesByCommandLine -ProjectName $SimProjName -CommandLinePattern 'Fisc'

# 4. Stop HSM Simulator
Write-Host "`n>>> [4/5] Stopping HSM Simulator..." -ForegroundColor Yellow
Stop-CyberCOLAProcessesByCommandLine -ProjectName 'WpfHSMSimulator'

# 5. Stop FISC Microservices & Router
Write-Host "`n>>> [5/5] Stopping FISC Microservices..." -ForegroundColor Yellow
& "$ScriptDir\COLAMainMenu-Console.ps1" -AutoSelect "3"
Write-Host "Stopping ServiceRouter..." -ForegroundColor Yellow
& "$ScriptDir\COLAMainMenu-Console.ps1" -AutoSelect "12"

Write-Host "`n==================================================" -ForegroundColor Green
Write-Host "    [SUCCESS] FISC Flow Stopped Successfully." -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Green
