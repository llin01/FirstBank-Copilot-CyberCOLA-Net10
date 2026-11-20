<#
    CyberCOLA Transaction Chart (per network, per socket port)

    Live console chart of transaction volume for NCCC, FISC, SVCS, ICASH and IPASS, grouped by the
    gateway socket port that carries it:
      - COLA port:     CyberAuth<NET> <-> CyberGW<NET>   (ColaServiceConfig.cola-service-bind-port)
      - Network port:  CyberGW<NET> <-> simulator/client (*-service-target-ports for the active
                       gateways NCCC/FISC, server-service-bind-port for SVCS/ICASH/IPASS)
    Counts come from each gateway's log (LogFiles\<NET>\CyberGW<NET>\<date>\CyberGW<NET>.log):
    "[<Session>][WRITE ...] Time[...], MTI[xxxx]" is a message the GW sent, "CHANNELREAD" one it
    received. FISC's network session does not log MTIs at Info level, so its network port is counted
    from the simulator log (WebStressSimulator\Fisc\<date>\Process.log) instead.
    Only lines written after the chart is opened are counted.

    Each series shows a bar of the TPS in the last interval (one scale for all series, so they can
    be compared) and a sparkline of the recent intervals (scaled to that series' own maximum).

    Usage:
      .\Show-CyberCOLATrafficChart.ps1                           (all networks, every 2 s, until Q / Esc)
      .\Show-CyberCOLATrafficChart.ps1 -Network NCCC,FISC        (only these networks)
      .\Show-CyberCOLATrafficChart.ps1 -RefreshSec 5             (redraw every 5 s)
      .\Show-CyberCOLATrafficChart.ps1 -DurationSec 60           (stops by itself after 60 s)
      .\Show-CyberCOLATrafficChart.ps1 -History 30               (keep the last 30 samples)

    Exit code: 0 (1 = unknown -Network value).
#>
param(
    # Also accepts one comma-joined string, which is how pwsh -File passes a list.
    [string[]]$Network = @('NCCC', 'FISC', 'SVCS', 'ICASH', 'IPASS'),
    [ValidateRange(1, 3600)]
    [int]$RefreshSec = 2,
    [int]$DurationSec = 0,
    [int]$History = 0
)

# Common requires PowerShell 7 (Core edition); restart under pwsh when launched from Windows PowerShell 5.x.
if ($PSVersionTable.PSEdition -eq 'Desktop') {
    $pwshCmd = Get-Command pwsh -ErrorAction SilentlyContinue
    if ($pwshCmd) {
        & $pwshCmd.Source -NoProfile -File $MyInvocation.MyCommand.Path -Network ($Network -join ',') -RefreshSec $RefreshSec -DurationSec $DurationSec -History $History
        exit $LASTEXITCODE
    } else {
        Write-Warning "PowerShell 7 (pwsh.exe) is required but was not found."
        exit 1
    }
}

. "$PSScriptRoot\CyberCOLA-Common.ps1"

# Logger (session) names come from the *ChannelInitializer classes in ServiceProvider\EntryPoints.
# NetSession = $null means the network session has no countable MTI line (see SimLog).
$NetworkDefs = [ordered]@{
    NCCC  = @{ Gw = 'CyberGWNCCC';  ColaSession = 'NcccColaSession';  NetSession = 'NcccSession';         Peer = 'Sim' }
    FISC  = @{ Gw = 'CyberGWFISC';  ColaSession = 'FiscColaSession';  NetSession = $null;                 Peer = 'Sim'; SimLog = 'WebStressSimulator\Fisc' }
    SVCS  = @{ Gw = 'CyberGWSVCS';  ColaSession = 'SvcsColaSession';  NetSession = 'CyberGWSVCSSession';  Peer = 'Client' }
    ICASH = @{ Gw = 'CyberGWICASH'; ColaSession = 'IcashColaSession'; NetSession = 'CyberGWICASHSession'; Peer = 'Client' }
    IPASS = @{ Gw = 'CyberGWIPASS'; ColaSession = 'IpassColaSession'; NetSession = 'CyberGWIPASSSession'; Peer = 'Client' }
}

$Network = @($Network -split ',' | ForEach-Object { $_.Trim().ToUpper() } | Where-Object { $_ })
$unknown = @($Network | Where-Object { -not $NetworkDefs.Contains($_) })
if ($unknown.Count -gt 0) {
    Write-Host "Unknown network: $($unknown -join ', '). Valid: $($NetworkDefs.Keys -join ', ')." -ForegroundColor Red
    exit 1
}

$LabelWidth = 29
$BarWidth = 30
# Block characters are built from code points so this file stays plain ASCII.
$FullBlock = [string][char]0x2588
$EmptyBlock = [string][char]0x2591
$SparkChars = @(0x2581..0x2588 | ForEach-Object { [string][char]$_ })

function Get-ConsoleWidth {
    try { return [math]::Max(60, $Host.UI.RawUI.WindowSize.Width) } catch { return 120 }
}

function Format-Bar([double]$Value, [double]$Max) {
    $filled = if ($Max -gt 0) { [int][math]::Round($BarWidth * $Value / $Max) } else { 0 }
    # Show at least one block for any traffic so a small series is not drawn as idle.
    if ($Value -gt 0 -and $filled -eq 0) { $filled = 1 }
    return ($FullBlock * $filled) + ($EmptyBlock * ($BarWidth - $filled))
}

function Format-Sparkline($Samples, [int]$Width) {
    $max = ($Samples | Measure-Object -Maximum).Maximum
    $sb = [System.Text.StringBuilder]::new()
    foreach ($v in $Samples) {
        if ($v -le 0 -or $max -le 0) { [void]$sb.Append(' '); continue }
        $i = [int][math]::Ceiling(($SparkChars.Count) * $v / $max) - 1
        [void]$sb.Append($SparkChars[[math]::Min($SparkChars.Count - 1, [math]::Max(0, $i))])
    }
    # Right-align so the newest sample is always at the right edge, even before the history fills up.
    return $sb.ToString().PadLeft($Width)
}

# COLA and network ports of a gateway, read from its appsettings.json (any section).
function Get-GatewayPorts([string]$GwName) {
    $json = Read-CyberCOLAJsonWithComments (Join-Path (Get-CyberCOLABaseDir) "src\CyberCOLA\$GwName\appsettings.json")
    $result = @{ Cola = @(); Net = @() }
    if (-not $json) { return $result }
    foreach ($section in $json.PSObject.Properties) {
        if ($section.Value -isnot [System.Management.Automation.PSCustomObject]) { continue }
        foreach ($p in $section.Value.PSObject.Properties) {
            if ($p.Name -eq 'cola-service-bind-port') {
                $result.Cola += ConvertTo-CyberCOLAPortList -Value $p.Value
            } elseif ($p.Name -eq 'server-service-bind-port' -or
                      ($p.Name -like '*-service-target-ports' -and $p.Name -notlike 'client-monitor*')) {
                $result.Net += ConvertTo-CyberCOLAPortList -Value $p.Value
            }
        }
    }
    $result.Cola = @($result.Cola | Sort-Object -Unique)
    $result.Net = @($result.Net | Sort-Object -Unique)
    return $result
}

$logRoot = Get-CyberCOLALogRoot
# "[time][level][thread][<Session>][WRITE|CHANNELREAD [a => b] Time[...], MTI[xxxx]". The "Time[" right
# after the endpoints skips the length-only CHANNELREAD lines, which have no MTI.
$gwPattern = [regex]'(?m)^\[[^\]]+\]\[[^\]]+\]\[[^\]]+\]\[(\w+)\]\[(WRITE|CHANNELREAD) \[[^\]]*\] Time\[[^\]]*\], MTI\[(\d{4})\]'
$nets = foreach ($name in $NetworkDefs.Keys) {
    if ($Network -notcontains $name) { continue }
    $def = $NetworkDefs[$name]
    $gwName = $def.Gw
    $ports = Get-GatewayPorts $gwName
    $net = @{
        Name = $name; Def = $def; Ports = $ports
        # Bind the loop values now; the scriptblocks run later on every refresh.
        Gw   = New-LogCounter ({ Join-Path $logRoot "$name\$gwName\$(Get-Date -Format 'yyyy-MM-dd')\$gwName.log" }.GetNewClosure()) `
                   $gwPattern { param($m) "$($m.Groups[1].Value)|$($m.Groups[2].Value)|$($m.Groups[3].Value)" }
        Sim  = $null
    }
    if ($def.SimLog) {
        $simLog = $def.SimLog
        $net.Sim = New-LogCounter ({ Join-Path $logRoot "$simLog\$(Get-Date -Format 'yyyy-MM-dd')\Process.log" }.GetNewClosure()) `
            ([regex]'(?m)\| (Send Message|Server Receive Data)') { param($m) $m.Groups[1].Value }
    }
    $net
}

# Series history: id -> list of TPS samples, oldest first.
$historyMap = @{}
$sampleCount = 0
$openedAt = Get-Date
$deadline = if ($DurationSec -gt 0) { $openedAt.AddSeconds($DurationSec) } else { [datetime]::MaxValue }

while ((Get-Date) -lt $deadline) {
    $table = Get-CyberCOLAFastTCPTable
    $blocks = foreach ($n in $nets) {
        Update-LogCounter $n.Gw
        if ($n.Sim) { Update-LogCounter $n.Sim }
        $def = $n.Def
        $peer = $def.Peer

        $colaSeries = @()
        $netSeries = @()
        if ($n.Sim) {
            $netSeries += @{ Id = "$($n.Name)|sim|send"; Label = "$peer sent requests"; Counter = $n.Sim; Key = 'Send Message' }
            $netSeries += @{ Id = "$($n.Name)|sim|recv"; Label = "$peer received msgs"; Counter = $n.Sim; Key = 'Server Receive Data' }
        }
        # GW series appear once their session / direction / MTI is first seen in the log.
        foreach ($key in ($n.Gw.Counts.Keys | Sort-Object)) {
            $session, $dir, $mti = $key -split '\|'
            $s = @{ Id = "$($n.Name)|$key"; Counter = $n.Gw; Key = $key }
            if ($session -eq $def.ColaSession) {
                $s.Label = if ($dir -eq 'WRITE') { "GW -> Auth  MTI $mti" } else { "Auth -> GW  MTI $mti" }
                $colaSeries += $s
            } elseif ($session -eq $def.NetSession) {
                $s.Label = if ($dir -eq 'WRITE') { "GW -> $peer  MTI $mti" } else { "$peer -> GW  MTI $mti" }
                $netSeries += $s
            }
        }

        # Links seen from the GW process: its connections on the COLA / network ports.
        $gwPids = @(Get-Process $def.Gw -ErrorAction SilentlyContinue | ForEach-Object { [int]$_.Id })
        $countLinks = {
            param($Ports)
            @($table | Where-Object {
                $_.State -eq 'Established' -and $gwPids -contains [int]$_.OwningProcess -and
                ($Ports -contains [int]$_.LocalPort -or $Ports -contains [int]$_.RemotePort)
            }).Count
        }
        @{
            Name = $n.Name; Gw = $def.Gw; Running = $gwPids.Count -gt 0
            Groups = @(
                @{ Title = "Auth <-> GW (COLA)"; Ports = $n.Ports.Cola; Links = (& $countLinks $n.Ports.Cola); Series = $colaSeries }
                @{ Title = "GW <-> $peer";       Ports = $n.Ports.Net;  Links = (& $countLinks $n.Ports.Net);  Series = $netSeries }
            )
        }
    }

    # Record this interval's TPS for every series; a new series is back-filled with zeros so all
    # sparklines stay aligned in time.
    $maxSamples = if ($History -gt 0) { $History } else { [math]::Max(10, (Get-ConsoleWidth) - $LabelWidth - 6) }
    $sampleCount++
    foreach ($s in $blocks.Groups.Series) {
        if (-not $historyMap.ContainsKey($s.Id)) {
            $historyMap[$s.Id] = [System.Collections.Generic.List[double]]::new()
            for ($i = 1; $i -lt [math]::Min($sampleCount, $maxSamples); $i++) { $historyMap[$s.Id].Add(0) }
        }
        $samples = $historyMap[$s.Id]
        $samples.Add([int]$s.Counter.Last[$s.Key] / $RefreshSec)
        while ($samples.Count -gt $maxSamples) { $samples.RemoveAt(0) }
    }
    $scaleMax = 0
    foreach ($samples in $historyMap.Values) {
        $m = ($samples | Measure-Object -Maximum).Maximum
        if ($m -gt $scaleMax) { $scaleMax = $m }
    }

    Clear-CyberCOLAScreen
    Write-Host "================ CyberCOLA Transaction Chart (per port) ================" -ForegroundColor Cyan
    Write-Host (" Now {0:HH:mm:ss}   Since {1:HH:mm:ss}   Refresh {2} s   Scale max {3:N1} TPS   Q / Esc to exit" -f (Get-Date), $openedAt, $RefreshSec, $scaleMax)
    foreach ($b in $blocks) {
        Write-Host ""
        Write-Host (" [{0}] {1} " -f $b.Name, $b.Gw) -ForegroundColor White -NoNewline
        if ($b.Running) { Write-Host "Running" -ForegroundColor Green } else { Write-Host "Stopped" -ForegroundColor Red }
        foreach ($g in $b.Groups) {
            Write-Host ("   Port {0,-12} {1,-20} " -f ($g.Ports -join ','), $g.Title) -ForegroundColor Cyan -NoNewline
            if ($g.Links -gt 0) { Write-Host "links $($g.Links) ESTABLISHED" -ForegroundColor Green } else { Write-Host "DOWN" -ForegroundColor Red }
            if ($g.Series.Count -eq 0) {
                Write-Host "     (no traffic since the chart opened)" -ForegroundColor DarkGray
                continue
            }
            foreach ($s in $g.Series) {
                $samples = $historyMap[$s.Id]
                $tps = $samples[$samples.Count - 1]
                $total = [int]$s.Counter.Counts[$s.Key]
                $color = if ($tps -gt 0) { 'Green' } else { 'DarkGray' }
                Write-Host ("     {0,-$($LabelWidth - 5)}" -f $s.Label) -NoNewline
                Write-Host (Format-Bar $tps $scaleMax) -ForegroundColor $color -NoNewline
                Write-Host (" {0,7:N1} TPS  total {1,8}" -f $tps, $total)
                Write-Host ((' ' * $LabelWidth) + '|') -NoNewline -ForegroundColor DarkGray
                Write-Host (Format-Sparkline $samples $maxSamples) -ForegroundColor Yellow -NoNewline
                Write-Host '|' -ForegroundColor DarkGray
            }
        }
    }
    Write-Host ""
    Write-Host " Sparkline: last $maxSamples samples x $RefreshSec s, oldest on the left." -ForegroundColor DarkGray

    # Wait for the next refresh while watching for Q / Esc.
    $waitUntil = (Get-Date).AddSeconds($RefreshSec)
    if ($waitUntil -gt $deadline) { $waitUntil = $deadline }
    while ((Get-Date) -lt $waitUntil) {
        if (Test-CyberCOLAKeyAvailable) {
            $key = Read-CyberCOLAConsoleKey -Intercept
            if ($key -and ($key.Key -eq 'Escape' -or $key.KeyChar -in 'q', 'Q')) { exit 0 }
        }
        Start-Sleep -Milliseconds 200
    }
}
exit 0
