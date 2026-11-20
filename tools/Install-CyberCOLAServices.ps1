<#
.SYNOPSIS
    Manage CyberCOLA .NET 10 Windows Services (Interactive Loop).
#>

. "$PSScriptRoot\CyberCOLA-Common.ps1"
Require-CyberCOLAAdministrator

$SystemName = "CyberCOLA"
$BaseDir = Get-CyberCOLABaseDir
$BinPathSuffix = Join-Path $BaseDir "publish\win-x64"

# Define service list
$AllServices = Get-CyberCOLAServiceCatalog

while ($true) {
    Write-Host "`n==================================================" -ForegroundColor Cyan
    Write-Host "      CyberCOLA Service Management Console" -ForegroundColor Cyan
    Write-Host "==================================================" -ForegroundColor Cyan
    Write-Host "1. Install / Reinstall"
    Write-Host "2. Uninstall"
    Write-Host "3. Display Status"
    Write-Host "q. Quit" -ForegroundColor Yellow
    
    $action = Read-Host "`nSelect Action [1, 2, 3, q]"
    
    if ($action -eq "q") { 
        Write-Host "Goodbye!" -ForegroundColor Green
        break 
    }

    if ($action -notmatch "^[1-3]$") {
        Write-Warning "Invalid action choice."
        continue
    }

    # 2. Select service
    Write-Host "`n--- Service Menu ---" -ForegroundColor Cyan
    Write-Host "0. [ALL SERVICES]" -ForegroundColor Yellow
    for ($i = 0; $i -lt $AllServices.Count; $i++) {
        Write-Host ("{0,2}. {1}" -f ($i + 1), $AllServices[$i].Name)
    }
    Write-Host "b. Back to main menu" -ForegroundColor Yellow

    $inputSelection = Read-Host "`nEnter service number (e.g. 1,7,15) or '0' for all"
    
    if ($inputSelection -eq "b") { continue }

    $selection = $inputSelection.Split(',') | ForEach-Object { $_.Trim() }
    $TargetServices = @()

    if ($selection -contains "0") {
        $TargetServices = $AllServices
    } else {
        foreach ($idx in $selection) {
            $num = 0
            if ([int]::TryParse($idx, [ref]$num)) {
                $num = $num - 1
                if ($num -ge 0 -and $num -lt $AllServices.Count) {
                    $TargetServices += $AllServices[$num]
                }
            }
        }
    }

    if ($TargetServices.Count -eq 0) {
        Write-Warning "No valid selection made."
        continue
    }

    # 3. Perform action
    foreach ($Svc in $TargetServices) {
        $ServiceName = $Svc.Name
        $DisplayName = "$SystemName $($Svc.Type) $($Svc.Module)"
        $BinaryPath = Join-Path $BinPathSuffix "$($Svc.ProjectDir)\$($Svc.Name).exe"

        $Existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue

        if ($action -eq "1") { # INSTALL
            Write-Host "--------------------------------------------------" -ForegroundColor Gray
            Write-Host "Installing: $ServiceName"
            if (-not (Test-Path $BinaryPath)) {
                Write-Host "  -> FAILED: Binary not found at $BinaryPath" -ForegroundColor Red
                continue
            }
            if ($Existing) {
                Write-Host "  -> Removing existing service..." -ForegroundColor Gray
                sc.exe delete $ServiceName | Out-Null
                # Poll for SCM to actually commit the deletion instead of a blind fixed sleep;
                # give up after 5s (same practical ceiling as the old delay was meant to cover).
                $deleteDeadline = (Get-Date).AddSeconds(5)
                while ((Get-Date) -lt $deleteDeadline -and (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue)) {
                    Start-Sleep -Milliseconds 200
                }
            }
            try {
                New-Service -Name $ServiceName `
                            -BinaryPathName $BinaryPath `
                            -DisplayName $DisplayName `
                            -Description "CyberCOLA .NET 10 System - $($Svc.Type) for $($Svc.Module)" `
                            -StartupType Manual -ErrorAction Stop
                sc.exe failure $ServiceName reset= 0 actions= restart/60000/restart/60000/restart/60000 | Out-Null
                Write-Host "  -> SUCCESS" -ForegroundColor Green
            } catch {
                Write-Host "  -> FAILED: $($_.Exception.Message)" -ForegroundColor Red
            }
        }
        elseif ($action -eq "2") { # UNINSTALL
            Write-Host "--------------------------------------------------" -ForegroundColor Gray
            Write-Host "Uninstalling: $ServiceName"
            if ($Existing) {
                try {
                    if ($Existing.Status -eq 'Running') {
                        Write-Host "  -> Stopping service..." -ForegroundColor Gray
                        Stop-Service -Name $ServiceName -Force
                    }
                    sc.exe delete $ServiceName | Out-Null
                    Write-Host "  -> SUCCESS" -ForegroundColor Green
                } catch {
                    Write-Host "  -> FAILED: $($_.Exception.Message)" -ForegroundColor Red
                }
            } else {
                Write-Host "  -> Service not found, skip." -ForegroundColor Yellow
            }
        }
        elseif ($action -eq "3") { # DISPLAY
            if ($Existing) {
                $color = if ($Existing.Status -eq 'Running') { "Green" } else { "Yellow" }
                Write-Host ("{0,-25} : " -f $ServiceName) -NoNewline
                Write-Host ("[{0}]" -f $Existing.Status) -ForegroundColor $color
            } else {
                Write-Host ("{0,-25} : " -f $ServiceName) -NoNewline
                Write-Host "[NOT INSTALLED]" -ForegroundColor Red
            }
        }
    }
    
    Read-Host "`nPress Enter to continue..."
}
