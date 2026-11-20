<#
.SYNOPSIS
    Sync published linux-x64 services from Windows to WSL2.
#>

. "$PSScriptRoot\CyberCOLA-Common.ps1"

$WslDistro = "Ubuntu-24.04" # Your specified WSL2 version
$WslTargetHome = "/opt/CyberCOLA" # Must match Publish-CyberCOLAServices.ps1's -LinuxDeployBasePath (appsettings.json's GeneralConfig XML paths are baked to this)

# Convert WSL path to Windows network path format
$WslDestPath = "\\wsl.localhost\$WslDistro$($WslTargetHome -replace '/', '\')"

$BaseDir = Get-CyberCOLABaseDir
$SourcePath = Join-Path $BaseDir "publish\linux-x64"
# Shared Config/*.xml + SocketConfig.json (mirrored by Publish-CyberCOLAServices.ps1's Step 3/3b into
# publish\Config, a directory that sits alongside publish\linux-x64, not under it). appsettings.json's
# GeneralConfig "*-xml-file-path" values are baked to $WslTargetHome/Config/<file>, so this must be
# synced too or the services will fail to find their shared XML config at runtime.
$ConfigSourcePath = Join-Path $BaseDir "publish\Config"
$ConfigDestPath = "\\wsl.localhost\$WslDistro$(($WslTargetHome + '/Config') -replace '/', '\')"

Write-Host "`n=== WSL2 Sync Tool ===" -ForegroundColor Cyan
Write-Host "Source: $SourcePath"
Write-Host "Target: $WslDestPath ($WslDistro)"
Write-Host "Config Source: $ConfigSourcePath"
Write-Host "Config Target: $ConfigDestPath ($WslDistro)"

# Check source directory
if (-not (Test-Path $SourcePath)) {
    Write-Error "Source path not found! Please run Publish-CyberCOLAServices.ps1 for linux-x64 first."
    exit
}

# Check if WSL is accessible
if (-not (Test-Path "\\wsl.localhost\$WslDistro")) {
    Write-Warning "WSL Distro '$WslDistro' is not accessible. Please ensure WSL is running."
    exit
}

# Create target directories (via WSL commands)
Write-Host "Checking target directories in WSL..." -ForegroundColor Gray
wsl -d $WslDistro -e mkdir -p $WslTargetHome
wsl -d $WslDistro -e mkdir -p "$WslTargetHome/Config"

# Execute sync (using robocopy, copies only changed files for maximum speed)
Write-Host "Starting synchronization..." -ForegroundColor Green

function Invoke-CyberCOLARobocopySync {
    <#
        Runs robocopy /MIR, discards its native per-file/table output (which
        doesn't align under CJK-width fonts in most terminals - robocopy has
        no working option to suppress that without also suppressing its own
        final Dirs/Files summary in this environment), and instead parses that
        summary's "Dirs :" / "Files :" totals into one clean aligned line.
    #>
    param([string]$Source, [string]$Dest, [string]$Label)

    $output = robocopy $Source $Dest /MIR /R:0 /W:0 /MT:32 /NP 2>&1
    $exitCode = $LASTEXITCODE

    # robocopy's summary label text is localized ("Dirs :"/"Files :" in English, "目錄 :"/"檔案 :"
    # under a Chinese locale, etc.), so matching on the label is fragile. Instead find the summary
    # block by its unique shape: it's always the two lines right after the "Total  Copied  Skipped
    # Mismatch  FAILED  Extras" header row, each holding exactly 6 whitespace-separated numeric
    # tokens (sizes like "397.9 k" on the Bytes row are why this looks at Dirs/Files specifically,
    # not "the next two lines" blindly - those two rows are always plain integers).
    $numericRowPattern = '^\s*\S+\s*:\s*(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s*$'
    $numericRows = $output | Where-Object { $_ -match $numericRowPattern }
    $dirsCopied = if ($numericRows.Count -ge 1 -and $numericRows[0] -match $numericRowPattern) { $Matches[2] } else { '?' }
    $filesCopied = if ($numericRows.Count -ge 2 -and $numericRows[1] -match $numericRowPattern) { $Matches[2] } else { '?' }

    if ($dirsCopied -eq '?' -or $filesCopied -eq '?') {
        Write-Host "  [DEBUG] Could not parse robocopy summary for '$Label'; raw tail:" -ForegroundColor DarkYellow
        $output | Select-Object -Last 12 | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }
    }

    $status = if ($exitCode -lt 8) { 'OK' } else { 'FAILED' }
    $color = if ($exitCode -lt 8) { 'Green' } else { 'Red' }
    Write-Host ("  [{0,-6}] {1,-10} dirs copied: {2,-4} files copied: {3,-4} (exit {4})" -f $status, $Label, $dirsCopied, $filesCopied, $exitCode) -ForegroundColor $color

    return $exitCode
}

$linuxSyncExitCode = Invoke-CyberCOLARobocopySync -Source $SourcePath -Dest $WslDestPath -Label "Services"

if (Test-Path $ConfigSourcePath) {
    $configSyncExitCode = Invoke-CyberCOLARobocopySync -Source $ConfigSourcePath -Dest $ConfigDestPath -Label "Config"
} else {
    Write-Warning "Config source path not found, skipped: $ConfigSourcePath"
    $configSyncExitCode = 0
}

if ($linuxSyncExitCode -lt 8 -and $configSyncExitCode -lt 8) {
    Write-Host "`nSuccessfully synced to WSL2: $WslTargetHome (services + Config)" -ForegroundColor Green
} else {
    Write-Error "Sync failed - Robocopy exit codes: services=$linuxSyncExitCode, config=$configSyncExitCode"
}
