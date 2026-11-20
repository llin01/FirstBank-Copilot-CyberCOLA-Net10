# ==============================================================================
# CyberCOLA .NET 10 System Real-time Process & TCP Connection Monitor Script
# All comments are in English to maintain international engineering standards.
# ==============================================================================

param(
    [int]$IntervalSeconds = 2
)

$ScriptName = $MyInvocation.MyCommand.Name
$ScriptDir = $PSScriptRoot
. "$ScriptDir\CyberCOLA-Common.ps1"

# Adjust host console window size for optimal display if running in a standard ConsoleHost
Set-CyberCOLAConsoleSize

# Define the managed CyberCOLA services (single source of truth in Common)
$ServiceConfigs = Get-CyberCOLATCPServiceCatalog

# Port cache to avoid redundant I/O and parsing overhead
$script:PortCache = @{}

# Resolve and cache ports before monitoring loop begins (Common handles config reads)
Write-Host "Resolving service ports from configurations..." -ForegroundColor Cyan
foreach ($svc in $ServiceConfigs) {
    $null = Get-CyberCOLAServicePort -Name $svc.Name -Section $svc.Section -Key $svc.Key -SubFolder $svc.SubFolder -DefaultPort $svc.DefaultPort -Cache $script:PortCache
}

# Real-time monitoring loop
$loop = $true
$firstRun = $true

# Clear the host first to start with a clean dashboard
Clear-Host

while ($loop) {
    # Check for quit key ('q' or 'Q') asynchronously
    if ([Console]::KeyAvailable) {
        $key = [Console]::ReadKey($true)
        if ($key.KeyChar -eq 'q' -or $key.KeyChar -eq 'Q') {
            $loop = $false
            break
        }
    }

    # Fetch TCP connection details using .NET API for ultra-high performance
    $props = [System.Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties()
    $activeConns = $props.GetActiveTcpConnections()
    $listeners = $props.GetActiveTcpListeners()

    # Pre-build lookups once per refresh instead of re-scanning $listeners/$activeConns with
    # Where-Object for every port of every service below (O(1) lookup vs O(n) linear scan per port).
    $listeningPorts = @{}
    foreach ($l in $listeners) { $listeningPorts[$l.Port] = $true }
    $establishedByPort = @{}
    foreach ($c in $activeConns) {
        if ($c.State -eq 'Established') {
            $p = $c.LocalEndPoint.Port
            if (-not $establishedByPort.ContainsKey($p)) { $establishedByPort[$p] = [System.Collections.Generic.List[object]]::new() }
            $establishedByPort[$p].Add($c)
        }
    }

    # Clear the console screen completely in each iteration to prevent scroll shift, overlap ghosting, and line debris.
    # In modern terminals (like Windows Terminal), Clear-Host / [Console]::Clear() is extremely fast and flicker-free.
    Clear-Host

    # Render Header Dashboard with high-performance native .NET clock
    $currentTime = [DateTime]::Now.ToString("yyyy-MM-dd HH:mm:ss")
    $title = "CyberCOLA System Real-time Process Monitor"
    $subTitle = "Current Time: $currentTime"
    $titlePad = [math]::Max(0, [int]((116 - $title.Length) / 2))
    $subTitlePad = [math]::Max(0, [int]((116 - $subTitle.Length) / 2))

    Write-Host ("=" * 116) -ForegroundColor Cyan
    Write-Host ((" " * $titlePad) + $title) -ForegroundColor Cyan
    Write-Host ((" " * $subTitlePad) + $subTitle) -ForegroundColor Gray
    Write-Host ("=" * 116) -ForegroundColor Cyan

    # Print Table Headers
    Write-Host ("{0,-32} | {1,-10} | {2,-7} | {3,-10} | {4,-12} | {5,-9} | {6,-10}" -f `
        "Service Name", "Status", "PID(s)", "Memory(MB)", "Port(s)", "TCP State", "Conns (EST)") -ForegroundColor White
    Write-Host ("-" * 116) -ForegroundColor Gray

    $runningCount = 0
    $offlineCount = 0

    foreach ($svc in $ServiceConfigs) {
        $name = $svc.Name
        $displayName = if ($svc.DisplayName) { $svc.DisplayName } else { $name }

        # Query process status with precise path filtering for WPF Simulators
        $allProcs = Get-Process -Name $name -ErrorAction SilentlyContinue
        $procs = @()
        if ($allProcs) {
            foreach ($p in $allProcs) {
                if (($name -eq "WpfHighStressSimulator" -or $name -eq "WpfStressSimulator" -or $name -eq "WpfETicketClientSimulator") -and $svc.SubFolder) {
                    try {
                        # Resolve process executable path safely
                        $procPath = $p.Path
                        # Support checking process path to distinguish individual simulators
                        $expectedPathPart = "\$name\$($svc.SubFolder)\"
                        if ($procPath -and $procPath.Contains($expectedPathPart)) {
                            $procs += $p
                        }
                    } catch {
                        # In case permission issues block Path query, fallback to keeping it
                        Write-Warning "Permission issue while querying path of process $($p.Id) - $($_.Exception.Message)"
                        $procs += $p
                    }
                } else {
                    $procs += $p
                }
            }
        }
        
        # Determine status representation
        $statusStr = "OFFLINE"
        $statusColor = "DarkGray"
        $pidStr = "-"
        $memMbStr = "-"
        
        if ($procs) {
            $runningCount++
            $statusStr = "RUNNING"
            $statusColor = "Green"
            
            # Aggregate PIDs and Working Set Memory for all instances
            $pids = @()
            $totalWS = 0
            foreach ($p in $procs) {
                $pids += $p.Id
                $totalWS += $p.WorkingSet64
            }
            $pidStr = $pids -join ","
            $memMbStr = [string][math]::Round($totalWS / 1MB, 1)
        } else {
            $offlineCount++
        }

        # Query TCP Port Status
        $tcpState = "OFFLINE"
        $tcpColor = "DarkGray"
        $connCount = 0
        $portStr = "-"

        $cacheKey = $name
        if ($svc.SubFolder) { $cacheKey = "$name-$($svc.SubFolder)" }
        $ports = $script:PortCache[$cacheKey]
        if ($ports -and $ports.Count -gt 0) {
            $portStr = $ports -join ","
            $anyListen = $false
            $totalConns = 0
            
            foreach ($pVal in $ports) {
                # Check if listening
                if ($listeningPorts.ContainsKey($pVal)) {
                    $anyListen = $true
                    # Check for active established connections
                    $conns = $establishedByPort[$pVal]
                    if ($conns) {
                        $totalConns += $conns.Count
                    }
                }
            }
            
            if ($anyListen) {
                $tcpState = "LISTEN"
                $tcpColor = "Green"
                $connCount = $totalConns
            }
        }

        # Print service row
        Write-Host ("{0,-32} | " -f $displayName) -NoNewline
        Write-Host ("{0,-10}" -f $statusStr) -ForegroundColor $statusColor -NoNewline
        Write-Host (" | {0,-7} | {1,-10} | " -f $pidStr, $memMbStr) -NoNewline
        
        $displayPortStr = $portStr
        if ($ports -and $ports.Count -gt 2) {
            # Format to show first two ports followed by ellipsis
            $displayPortStr = "$($ports[0]),$($ports[1])..."
        }
        if ($name -eq "WpfETicketClientSimulator") {
            $displayPortStr = "$displayPortStr (Local)"
        }
        if ($displayPortStr.Length -gt 12) {
            # Hard limit of 12 characters to avoid push formatting issues
            $displayPortStr = $displayPortStr.Substring(0, 9) + "..."
        }

        if ($ports -and $ports.Count -gt 0) {
            Write-Host ("{0,-12} | " -f $displayPortStr) -NoNewline
            Write-Host ("{0,-9}" -f $tcpState) -ForegroundColor $tcpColor -NoNewline
            
            if ($tcpState -eq "LISTEN" -and $connCount -gt 0) {
                Write-Host (" | ") -NoNewline
                Write-Host ("{0,-10}" -f $connCount) -ForegroundColor Green
            } else {
                Write-Host (" | {0,-10}" -f "-")
            }
        } else {
            Write-Host ("{0,-12} | {1,-9} | {2,-10}" -f "-", "-", "-")
        }
    }

    Write-Host ("-" * 116) -ForegroundColor Gray
    
    # Render Footer Summary Stats
    $totalServices = $ServiceConfigs.Count
    Write-Host "Summary: " -NoNewline
    Write-Host "Active: $runningCount " -ForegroundColor Green -NoNewline
    Write-Host "| " -NoNewline
    Write-Host "Offline: $offlineCount " -ForegroundColor Red -NoNewline
    Write-Host "| " -NoNewline
    Write-Host "Total: $totalServices              " -ForegroundColor Gray
    Write-Host ("=" * 116) -ForegroundColor Cyan
    Write-Host "Refresh Interval: ${IntervalSeconds}s | press q or Q return main menu" -ForegroundColor Yellow

    # Refresh Delay
    Start-Sleep -Seconds $IntervalSeconds
}
