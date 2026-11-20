# ==============================================================================
# CyberCOLA One-Click Script: Stop NCCC Services & Simulators
# All comments are in English to maintain international engineering standards.
# ==============================================================================

$ScriptDir = $PSScriptRoot
. "$ScriptDir\CyberCOLA-Common.ps1"
$SimProjName = if ($env:COLA_SIMULATOR_TYPE) { $env:COLA_SIMULATOR_TYPE } else { "WpfHighStressSimulator" }

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "         Stopping CyberCOLA NCCC Flow" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan

# 1. Stop SMS Web API Server
Write-Host "`n>>> [1/6] Stopping SMS Web API Server..." -ForegroundColor Yellow
Stop-CyberCOLAProcessesByCommandLine -ProjectName 'WpfSMSWebApiServer'

# 2. Stop SMS Simulator
Write-Host "`n>>> [2/6] Stopping SMS Simulator..." -ForegroundColor Yellow
Stop-CyberCOLAProcessesByCommandLine -ProjectName 'WpfSMSSimulator'

# 3. Stop NCCC QRCode Simulator
Write-Host "`n>>> [3/6] Stopping NCCC QRCode Simulator..." -ForegroundColor Yellow
Stop-CyberCOLAProcessesByCommandLine -ProjectName $SimProjName -CommandLinePattern 'NcccQRCode'

# 4. Stop NCCC Simulator
Write-Host "`n>>> [4/6] Stopping NCCC Simulator..." -ForegroundColor Yellow
Stop-CyberCOLAProcessesByCommandLine -ProjectName $SimProjName -CommandLinePattern 'Nccc'

# 5. Stop HSM Simulator
Write-Host "`n>>> [5/6] Stopping HSM Simulator..." -ForegroundColor Yellow
Stop-CyberCOLAProcessesByCommandLine -ProjectName 'WpfHSMSimulator'

# 6. Stop NCCC Gateway & Authorization Microservices & Router
Write-Host "`n>>> [6/6] Stopping NCCC & QRCode Microservices..." -ForegroundColor Yellow
& "$ScriptDir\COLAMainMenu-Console.ps1" -AutoSelect "1"
Write-Host "Stopping ServiceRouter..." -ForegroundColor Yellow
& "$ScriptDir\COLAMainMenu-Console.ps1" -AutoSelect "12"

Write-Host "`n==================================================" -ForegroundColor Green
Write-Host "    [SUCCESS] NCCC Flow Stopped Successfully." -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Green
