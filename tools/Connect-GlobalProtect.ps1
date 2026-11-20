<#
    Connect GlobalProtect VPN without manual clicking, then check the databases can be reached.
    Works only when the GlobalProtect credential is saved and the portal does not demand MFA.
    Tested against GlobalProtect 5.2.x (Win32 dialog, Connect button control ID 1160).

    Usage:
      .\Connect-GlobalProtect.ps1
      .\Connect-GlobalProtect.ps1 -TimeoutSeconds 180 -Hosts RD-DB,DBServer
      .\Connect-GlobalProtect.ps1 -SkipDbCheck
      .\Connect-GlobalProtect.ps1 -MaxAttempts 5 -RetryDelaySeconds 30
      .\Connect-GlobalProtect.ps1 -ConnectTimeoutMs 5000

    Each database port check gives up after -ConnectTimeoutMs (default 3000 ms).

    Retries connect + DB check until it succeeds, up to -MaxAttempts times. When the VPN is
    connected but a database port does not answer, it disconnects the VPN before the next attempt
    so that attempt reconnects from scratch.
    Exit codes: 0 = connected and all databases reachable, 1 = VPN not connected,
                2 = connected but some databases unreachable (code of the last attempt).
#>
param(
    [int]$TimeoutSeconds = 120,
    [string[]]$Hosts = @('RD-DB', 'DBServer'),
    [int]$Port = 1433,
    [int]$ConnectTimeoutMs = 3000,
    [int]$MaxAttempts = 10,
    [int]$RetryDelaySeconds = 10,
    [switch]$SkipDbCheck
)

$ErrorActionPreference = "Stop"

$GpaPath = "C:\Program Files\Palo Alto Networks\GlobalProtect\PanGPA.exe"
# Control IDs are locale independent, unlike the button caption, which is localized.
$ConnectButtonId = '1160'
$StatusTextId = '1165'
$DetailTextId = '1087'

Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes
Add-Type -Namespace Win32 -Name Native -MemberDefinition @'
[DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
'@
$BM_CLICK = 0x00F5

function Get-VpnAdapter {
    Get-NetAdapter | Where-Object { $_.InterfaceDescription -match 'PANGP' } | Select-Object -First 1
}

function Find-GpControlOnce([string]$automationId) {
    $proc = Get-Process PanGPA -ErrorAction SilentlyContinue | Select-Object -First 1
    if (-not $proc) { return $null }
    $ae = [System.Windows.Automation.AutomationElement]
    $byPid = New-Object System.Windows.Automation.PropertyCondition($ae::ProcessIdProperty, $proc.Id)
    $byId = New-Object System.Windows.Automation.PropertyCondition($ae::AutomationIdProperty, $automationId)
    foreach ($win in $ae::RootElement.FindAll([System.Windows.Automation.TreeScope]::Children, $byPid)) {
        $ctl = $win.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $byId)
        if ($ctl) { return $ctl }
    }
    return $null
}

# The GlobalProtect panel is a popup that closes when it loses focus, and its controls
# are only visible to UI Automation while it is open. Launching PanGPA.exe again
# (it is single-instance) reopens the panel.
function Find-GpControl([string]$automationId) {
    $ctl = Find-GpControlOnce $automationId
    if ($ctl) { return $ctl }
    if (-not (Test-Path $GpaPath)) { throw "GlobalProtect not found: $GpaPath" }
    Start-Process $GpaPath
    for ($i = 0; $i -lt 20 -and -not $ctl; $i++) {
        Start-Sleep -Milliseconds 500
        $ctl = Find-GpControlOnce $automationId
    }
    return $ctl
}

# The PANGP adapter stays Up while the agent is "Restoring VPN Connection", so the
# adapter state alone is not proof of a working tunnel; read the agent's status text.
function Get-GpStatus {
    $ctl = Find-GpControl $StatusTextId
    if ($ctl) { $ctl.Current.Name } else { '' }
}

function Wait-GpConnected([int]$seconds) {
    $deadline = (Get-Date).AddSeconds($seconds)
    do {
        # Status captions: English, and zh-TW built from code points to keep this file ASCII for Windows PowerShell 5.1.
        $zhConnected = -join [char[]](0x5DF2, 0x9023, 0x7DDA)
        if ((Get-GpStatus) -in @('Connected', $zhConnected)) { return $true }
        Start-Sleep -Seconds 2
    } while ((Get-Date) -lt $deadline)
    return $false
}

function Show-ConnectionInfo([TimeSpan]$elapsed) {
    $detail = Find-GpControl $DetailTextId
    $adapter = Get-VpnAdapter
    $portal = (Get-ItemProperty 'HKLM:\SOFTWARE\Palo Alto Networks\GlobalProtect\PanSetup' -ErrorAction SilentlyContinue).Portal
    $ip = '-'
    $dns = '-'
    if ($adapter -and $adapter.Status -eq 'Up') {
        $ipConf = Get-NetIPConfiguration -InterfaceIndex $adapter.ifIndex
        $ip = $ipConf.IPv4Address.IPAddress -join ', '
        $dns = ($ipConf.DNSServer | Where-Object AddressFamily -eq 2).ServerAddresses -join ', '
    }
    Write-Host ("  Time     : {0:yyyy-MM-dd HH:mm:ss}" -f (Get-Date))
    Write-Host ("  Elapsed  : {0:N1} s" -f $elapsed.TotalSeconds)
    Write-Host  "  Portal   : $portal"
    Write-Host  "  Status   : $(Get-GpStatus) / $(if ($detail) { $detail.Current.Name })"
    Write-Host  "  Adapter  : $($adapter.Name) ($($adapter.Status))"
    Write-Host  "  VPN IP   : $ip"
    Write-Host  "  VPN DNS  : $dns"
}

# TCP reachability of the SQL Server port only; it does not log in to the database.
function Test-Database {
    $results = foreach ($h in $Hosts) {
        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        $address = $null
        $interface = $null
        $ok = $false
        # Test-NetConnection waits about 40 s per unreachable host (ping, then the OS connect timeout),
        # which with retries outlasted the unattended auth test's wait; connect with our own timeout.
        try {
            $address = [System.Net.Dns]::GetHostAddresses($h) |
                Where-Object AddressFamily -eq ([System.Net.Sockets.AddressFamily]::InterNetwork) | Select-Object -First 1
            if ($address) {
                $interface = (Find-NetRoute -RemoteIPAddress $address.IPAddressToString -ErrorAction SilentlyContinue |
                    Select-Object -First 1).InterfaceAlias
                $client = New-Object System.Net.Sockets.TcpClient
                try {
                    $connect = $client.ConnectAsync($address, $Port)
                    $ok = $connect.Wait($ConnectTimeoutMs) -and $client.Connected
                }
                catch { $ok = $false }
                finally { $client.Dispose() }
            }
        }
        catch { $ok = $false }
        [pscustomobject]@{
            Database  = "${h}:$Port"
            Address   = $address
            Interface = $interface
            TimeMs    = $sw.ElapsedMilliseconds
            OK        = $ok
        }
    }
    $results | Format-Table -AutoSize | Out-String | Write-Host
    return -not ($results.OK -contains $false)
}

# Returns $true once the agent reports Connected; clicks Connect only when the tunnel is down.
function Connect-Vpn {
    # Adapter Up means connected or reconnecting; never click then, because the same button
    # means "Disconnect". Just wait for the agent to report Connected.
    if ((Get-VpnAdapter).Status -eq 'Up') {
        if (Wait-GpConnected $TimeoutSeconds) {
            Write-Host "[VPN] Connected." -ForegroundColor Green
            return $true
        }
        Write-Host "[VPN] Tunnel is up but not Connected after $TimeoutSeconds s. Status: '$(Get-GpStatus)'." -ForegroundColor Red
        return $false
    }

    # Find the Connect button, opening the panel (or starting PanGPA) if needed.
    $button = Find-GpControl $ConnectButtonId
    if (-not $button) { throw "Connect button (ID $ConnectButtonId) not found; GlobalProtect UI may have changed." }

    # The adapter can read not-Up for a moment while the agent is still connected or reconnecting,
    # and then this button reads "Disconnect"; clicking it tore the tunnel down. Click only when
    # the caption is Connect (zh-TW built from code points to keep this file ASCII), otherwise wait.
    $caption = $button.Current.Name
    $zhConnect = -join [char[]](0x9023, 0x7DDA)
    if ($caption -notin @('Connect', $zhConnect)) {
        Write-Host "[VPN] Button reads '$caption', not Connect; waiting instead of clicking." -ForegroundColor Yellow
        if (Wait-GpConnected $TimeoutSeconds) {
            Write-Host "[VPN] Connected." -ForegroundColor Green
            return $true
        }
        Write-Host "[VPN] Not connected after $TimeoutSeconds s. Status: '$(Get-GpStatus)'." -ForegroundColor Red
        return $false
    }

    # Win32 buttons expose no UIA Invoke pattern here, so click via BM_CLICK.
    Write-Host "[VPN] Clicking '$($button.Current.Name)'..." -ForegroundColor Cyan
    [Win32.Native]::SendMessage([IntPtr]$button.Current.NativeWindowHandle, $BM_CLICK, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null

    if (Wait-GpConnected $TimeoutSeconds) {
        Write-Host "[VPN] Connected." -ForegroundColor Green
        return $true
    }
    Write-Host "[VPN] Not connected after $TimeoutSeconds s. Status: '$(Get-GpStatus)'. MFA or sign-in may be required." -ForegroundColor Red
    return $false
}

# The tunnel can report Connected while the databases behind it stay unreachable; only a fresh
# connection has helped, so disconnect before the next attempt. Clicks only when the button reads
# Disconnect (zh-TW built from code points), then waits for the adapter to go down so the next
# Connect-Vpn sees a closed tunnel and clicks Connect.
function Disconnect-Vpn([int]$seconds = 30) {
    $button = Find-GpControl $ConnectButtonId
    if (-not $button) { Write-Host "[VPN] Connect/Disconnect button not found; not disconnecting." -ForegroundColor Yellow; return }
    $caption = $button.Current.Name
    $zhDisconnect = -join [char[]](0x4E2D, 0x65B7, 0x9023, 0x7DDA)
    if ($caption -notin @('Disconnect', $zhDisconnect)) {
        Write-Host "[VPN] Button reads '$caption', not Disconnect; not disconnecting." -ForegroundColor Yellow
        return
    }
    Write-Host "[VPN] Databases unreachable; disconnecting to reconnect..." -ForegroundColor Cyan
    [Win32.Native]::SendMessage([IntPtr]$button.Current.NativeWindowHandle, $BM_CLICK, [IntPtr]::Zero, [IntPtr]::Zero) | Out-Null
    $deadline = (Get-Date).AddSeconds($seconds)
    while ((Get-VpnAdapter).Status -eq 'Up' -and (Get-Date) -lt $deadline) { Start-Sleep -Seconds 1 }
    if ((Get-VpnAdapter).Status -eq 'Up') { Write-Host "[VPN] Adapter still up after $seconds s." -ForegroundColor Yellow }
    else { Write-Host "[VPN] Disconnected." -ForegroundColor DarkGray }
}

# Retry the whole connect + DB check: the tunnel can report Connected and then drop on
# keepalive timeout, so a single pass is not reliable.
$exitCode = 1
for ($attempt = 1; $attempt -le $MaxAttempts; $attempt++) {
    Write-Host ("`n===== Attempt {0} / {1}  ({2:HH:mm:ss}) =====" -f $attempt, $MaxAttempts, (Get-Date)) -ForegroundColor Cyan
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $connected = Connect-Vpn
    Show-ConnectionInfo $sw.Elapsed
    if (-not $connected) {
        $exitCode = 1
    }
    elseif ($SkipDbCheck) {
        exit 0
    }
    else {
        Write-Host "[DB] Checking database port reachability..." -ForegroundColor Cyan
        if (Test-Database) {
            Write-Host "[DB] All databases reachable." -ForegroundColor Green
            exit 0
        }
        Write-Host "[DB] Some databases unreachable." -ForegroundColor Yellow
        $exitCode = 2
        if ($attempt -lt $MaxAttempts) { Disconnect-Vpn }
    }

    if ($attempt -lt $MaxAttempts) {
        Write-Host "[Retry] Waiting $RetryDelaySeconds s before next attempt..." -ForegroundColor DarkGray
        Start-Sleep -Seconds $RetryDelaySeconds
    }
}

Write-Host "`n[Result] Failed after $MaxAttempts attempts." -ForegroundColor Red
exit $exitCode
