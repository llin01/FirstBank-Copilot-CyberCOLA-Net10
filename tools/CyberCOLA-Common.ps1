#requires -Version 7
#requires -PSEdition Core

<#
    CyberCOLA Shared PowerShell Helpers
    Dot-source this from any script in tools/:
        . $PSScriptRoot\CyberCOLA-Common.ps1

    Contains no side effects until functions are called.
#>

# ---------------------------------------------------------------------------
# Path helpers
# ---------------------------------------------------------------------------

function Get-CyberCOLABaseDir {
    # Resolve the repository root assuming this module lives in <repo>/tools
    return Split-Path -Parent $PSScriptRoot
}

function Get-CyberCOLAPublishDir {
    param([string]$Runtime = 'win-x64')
    return Join-Path (Get-CyberCOLABaseDir) "publish\$Runtime"
}

function Get-CyberCOLAConfiguredRepoRoot {
    <#
        The repository root that the checked-in config files were written for, read from the files themselves:
        the part before "\src\CyberCOLA\Config\" in the first service appsettings.json that has one. It differs
        from Get-CyberCOLABaseDir when the repository was cloned somewhere else and Set-CyberCOLARepoRoot.ps1 has
        not been run yet. Returns $null when no such path is found.
    #>
    param([string]$BaseDir = (Get-CyberCOLABaseDir))
    $pattern = '"([A-Za-z]:(?:\\\\[^"\\]+)+?)\\\\src\\\\CyberCOLA\\\\Config\\\\'
    foreach ($file in Get-ChildItem (Join-Path $BaseDir 'src\CyberCOLA') -Filter appsettings.json -Recurse -Depth 1 -ErrorAction SilentlyContinue) {
        $m = [regex]::Match([IO.File]::ReadAllText($file.FullName), $pattern)
        if ($m.Success) { return $m.Groups[1].Value.Replace('\\', '\') }
    }
    return $null
}

function Get-CyberCOLALogRoot {
    param([string]$Default = (Join-Path (Get-CyberCOLABaseDir) 'LogFiles'))
    # Prefer env override; otherwise default to <repo>\LogFiles so published logs land under the project tree.
    if ($env:CYBERCOLA_LOG_ROOT) {
        return $env:CYBERCOLA_LOG_ROOT
    } else {
        return $Default
    }
}

function Get-CyberCOLAFatalDir {
    return Join-Path (Get-CyberCOLALogRoot) 'CyberCOLA/Fatal/Log'
}

# ---------------------------------------------------------------------------
# Log scanning helpers (shared by COLAMainMenu F/E options and Find-CyberCOLALogSourceErrors)
# ---------------------------------------------------------------------------

function Get-CyberCOLALogLevelRegex {
    # Single cached regex instance so callers don't each compile their own.
    if (-not $Global:CyberCOLALogLevelRegex) {
        $Global:CyberCOLALogLevelRegex = [regex]::new('\b(FATAL|ERROR|WARN)\b', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    }
    return $Global:CyberCOLALogLevelRegex
}

function Get-CyberCOLALogSeverityColor {
    param([string]$Level)
    # WARN / exception-only lines all map to DarkYellow via the default.
    switch ($Level) {
        'FATAL' { return 'Red' }
        'ERROR' { return 'DarkRed' }
        default { return 'DarkYellow' }
    }
}

function Get-CyberCOLALogFiles {
    param([Parameter(Mandatory)][string[]]$Folders)
    # Group-Object FullName dedups files reachable from multiple folders; Sort-Object gives stable scan order.
    Get-ChildItem -Path $Folders -Filter '*.log' -Recurse -File -ErrorAction SilentlyContinue |
        Group-Object FullName | ForEach-Object { $_.Group[0] } |
        Sort-Object FullName
}

function New-CyberCOLALogReader {
    param([Parameter(Mandatory)][string]$Path)
    # FileShare.ReadWrite lets us read logs that NLog is actively writing; the default StreamReader ctor
    # uses FileShare.Read, which collides with NLog's write handle and throws "being used by another process".
    $stream = [System.IO.File]::Open($Path, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
    return [System.IO.StreamReader]::new($stream, [System.Text.Encoding]::UTF8)
}

# Incremental log reader: counts only lines appended after it is created, follows the daily
# folder roll-over and restarts from 0 when NLog archives (truncates) the file.
function New-LogCounter([scriptblock]$GetPath, [regex]$Pattern, [scriptblock]$GetKey) {
    $path = & $GetPath
    $offset = if (Test-Path $path) { (Get-Item $path).Length } else { 0 }
    return @{ GetPath = $GetPath; Path = $path; Offset = [long]$offset; Pattern = $Pattern; GetKey = $GetKey
              Counts = @{}; Last = @{} }
}

function Update-LogCounter($counter) {
    $counter.Last = @{}
    $path = & $counter.GetPath
    if ($path -ne $counter.Path) { $counter.Path = $path; $counter.Offset = 0 }
    if (-not (Test-Path $path)) { return }

    $stream = [System.IO.File]::Open($path, 'Open', 'Read', 'ReadWrite, Delete')
    try {
        if ($stream.Length -lt $counter.Offset) { $counter.Offset = 0 }
        $toRead = $stream.Length - $counter.Offset
        if ($toRead -le 0) { return }
        $buffer = New-Object byte[] $toRead
        $stream.Seek($counter.Offset, 'Begin') | Out-Null
        $read = $stream.Read($buffer, 0, $buffer.Length)
        # Only consume complete lines; a half-written last line is read on the next refresh.
        $end = [Array]::LastIndexOf($buffer, [byte]10, $read - 1)
        if ($end -lt 0) { return }
        $counter.Offset += $end + 1
        $text = [System.Text.Encoding]::UTF8.GetString($buffer, 0, $end + 1)
        foreach ($m in $counter.Pattern.Matches($text)) {
            $key = & $counter.GetKey $m
            $counter.Counts[$key] = 1 + [int]$counter.Counts[$key]
            $counter.Last[$key] = 1 + [int]$counter.Last[$key]
        }
    } finally {
        $stream.Dispose()
    }
}

# ---------------------------------------------------------------------------
# UI helpers
# ---------------------------------------------------------------------------

function Clear-CyberCOLAScreen {
    # Safely clear the console; tolerates non-interactive terminals where Clear-Host throws.
    try {
        Clear-Host
    } catch {
        # Non-interactive terminal (e.g., pwsh -NonInteractive); ignore and continue.
    }
}

function Set-CyberCOLAConsoleSize {
    # Resize the console buffer/window for menu readability. No-op outside ConsoleHost.
    param([int]$Width = 125, [int]$Height = 40, [int]$BufferHeight = 1000)
    if ($Host.Name -ne 'ConsoleHost') { return }
    try {
        $rawUI = $Host.UI.RawUI
        $buffer = $rawUI.BufferSize
        $window = $rawUI.WindowSize
        if ($buffer.Width -lt $Width) { $buffer.Width = $Width }
        $buffer.Height = $BufferHeight
        $rawUI.BufferSize = $buffer
        $window.Width = $Width
        $window.Height = $Height
        $rawUI.WindowSize = $window
    } catch {
        # Non-interactive or headless host; ignore and continue.
    }
}

function Test-CyberCOLAKeyAvailable {
    # Query whether a key is available, tolerating non-interactive consoles.
    try {
        return [Console]::KeyAvailable
    } catch {
        return $false
    }
}

function Read-CyberCOLAConsoleKey {
    param([switch]$Intercept)
    # Read a console key, tolerating non-interactive consoles.
    try {
        return [Console]::ReadKey($Intercept)
    } catch {
        return $null
    }
}

function Show-CyberCOLAHeader {
    param(
        [string]$Title,
        [int]$Width = 60,
        [switch]$NoDate
    )
    Clear-CyberCOLAScreen
    $dt = if (-not $NoDate) { "Current Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" } else { $null }

    $titlePad = [math]::Max(0, [int](($Width - $Title.Length) / 2))
    $dtPad = if ($dt) { [math]::Max(0, [int](($Width - $dt.Length) / 2)) } else { 0 }

    Write-Host ('=' * $Width) -ForegroundColor Cyan
    Write-Host ((" " * $titlePad) + $Title) -ForegroundColor Cyan
    if ($dt) { Write-Host ((" " * $dtPad) + $dt) -ForegroundColor Gray }
    Write-Host ('=' * $Width) -ForegroundColor Cyan
}

function Confirm-CyberCOLA {
    param(
        [string]$Prompt = 'Proceed?',
        [string]$Default = 'Y'
    )
    $reply = Read-Host "$Prompt [Y/n] (Default: $Default)"
    if ([string]::IsNullOrWhiteSpace($reply)) { $reply = $Default }
    return $reply.Trim().ToUpper().StartsWith('Y')
}

function Require-CyberCOLAAdministrator {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
        Write-Host 'This script requires Administrator privileges.' -ForegroundColor Red
        exit 1
    }
}

# ---------------------------------------------------------------------------
# JSON / XML config helpers
# ---------------------------------------------------------------------------

function Read-CyberCOLAJsonWithComments {
    param([string]$Path)

    if (-not (Test-Path $Path)) { return $null }
    try {
        $raw = Get-Content $Path -Raw
        # Strip single-line comments but preserve http:// and https://
        $clean = $raw -replace '(?m)(?<!http:|https:)\/\/.*$', ''
        return $clean | ConvertFrom-Json
    } catch {
        Write-Warning "Failed to parse JSON with comments: $Path - $($_.Exception.Message)"
        return $null
    }
}

# ---------------------------------------------------------------------------
# Socket config sync: src/CyberCOLA/Config/SocketConfig.json is the single
# source of truth for GW/Auth socket settings (port, IP, sessions, etc.) across
# all six networks. Each project's appsettings.json keeps its existing flat
# section shape (ColaServiceConfig, ClientMonitorConfig, ...) so every existing
# ConfigurationLoaderRoot.Get(sectionName, key) call site is unaffected -
# this just overwrites each such value with the one from SocketConfig.json,
# preserving comments/formatting via targeted regex replace (same approach as
# Prompt-CyberCOLASessionCount above), not a full ConvertTo-Json rewrite.
# ---------------------------------------------------------------------------

function Get-CyberCOLASocketConfigProjectMap {
    # ProjectName -> (network code in SocketConfig.json, role key in SocketConfig.json)
    return [ordered]@{
        "CyberAuthFISC"        = @("FISC", "Auth")
        "CyberAuthNCCC"        = @("NCCC", "Auth")
        "CyberAuthTWIN"        = @("TWIN", "Auth")
        "CyberAuthSVCS"        = @("SVCS", "Auth")
        "CyberAuthIPASS"       = @("IPASS", "Auth")
        "CyberAuthICASH"       = @("ICASH", "Auth")
        "CyberGWFISC"          = @("FISC", "GW")
        "CyberGWFISCIDVerify"  = @("FISC", "GWIDVerify")
        "CyberGWNCCC"          = @("NCCC", "GW")
        "CyberGWNCCCQRCode"    = @("NCCC", "GWQRCode")
        "CyberGWTWIN"          = @("TWIN", "GW")
        "CyberGWSVCS"          = @("SVCS", "GW")
        "CyberGWIPASS"         = @("IPASS", "GW")
        "CyberGWICASH"         = @("ICASH", "GW")
    }
}

function Sync-CyberCOLASocketConfigFile {
    <#
        Overwrites, in-place, every value in $TargetAppSettingsPath whose section+key
        matches a value found under SocketConfig.json's $Network/$Role slice.
        Keys/sections not present in SocketConfig.json (e.g. GeneralConfig, Logging)
        are left untouched. Comments and formatting in the target file are preserved.
    #>
    param(
        [Parameter(Mandatory)][string]$SocketConfigPath,
        [Parameter(Mandatory)][string]$Network,
        [Parameter(Mandatory)][string]$Role,
        [Parameter(Mandatory)][string]$TargetAppSettingsPath
    )

    if (-not (Test-Path $SocketConfigPath)) {
        Write-Warning "SocketConfig.json not found: $SocketConfigPath"
        return
    }
    if (-not (Test-Path $TargetAppSettingsPath)) {
        Write-Warning "appsettings.json not found, skipped: $TargetAppSettingsPath"
        return
    }

    $socketConfig = Read-CyberCOLAJsonWithComments $SocketConfigPath
    $networkNode = $socketConfig.$Network
    if (-not $networkNode) { Write-Warning "SocketConfig.json has no network '$Network'"; return }
    $roleNode = $networkNode.$Role
    if (-not $roleNode) { Write-Warning "SocketConfig.json has no role '$Role' under network '$Network'"; return }

    $raw = Get-Content $TargetAppSettingsPath -Raw
    $changedCount = 0

    foreach ($sectionProp in $roleNode.PSObject.Properties) {
        $sectionName = $sectionProp.Name
        foreach ($keyProp in $sectionProp.Value.PSObject.Properties) {
            $key = $keyProp.Name
            $value = [string]$keyProp.Value

            # Match "<key>": "<anything>" only within the named section object.
            # Sections in appsettings.json are not nested, so a straightforward
            # per-key regex anchored on the key name is sufficient and mirrors
            # the existing Prompt-CyberCOLASessionCount approach.
            $escapedKey = [regex]::Escape($key)
            $escapedValue = $value -replace '\$', '$$$$'
            $pattern = '("' + $escapedKey + '"\s*:\s*")[^"]*(")'
            $replacement = '${1}' + $escapedValue + '${2}'

            if ($raw -match $pattern) {
                $newRaw = $raw -replace $pattern, $replacement
                if ($newRaw -ne $raw) { $changedCount++ }
                $raw = $newRaw
            }
        }
    }

    Set-Content -Path $TargetAppSettingsPath -Value $raw -Encoding utf8 -NoNewline
    Write-Host "  - Socket config synced ($changedCount values) -> $TargetAppSettingsPath" -ForegroundColor Green
}

function Sync-CyberCOLASocketConfigAll {
    <#
        Syncs SocketConfig.json into every published project's appsettings.json
        under the given publish root (e.g. publish\win-x64). Call after publish
        binaries and appsettings.json exist in the output directories.
    #>
    param(
        [Parameter(Mandatory)][string]$PublishRootDir,
        [Parameter(Mandatory)][string]$SocketConfigPath
    )

    $map = Get-CyberCOLASocketConfigProjectMap
    foreach ($projName in $map.Keys) {
        $targetPath = Join-Path $PublishRootDir "$projName\appsettings.json"
        if (-not (Test-Path $targetPath)) { continue }
        $network, $role = $map[$projName]
        Sync-CyberCOLASocketConfigFile -SocketConfigPath $SocketConfigPath -Network $network -Role $role -TargetAppSettingsPath $targetPath
    }
}

function Convert-CyberCOLAAppSettingsPathsForLinux {
    <#
        Rewrites GeneralConfig's "*-xml-file-path" values in every published appsettings.json
        from the Windows publish-time path (e.g. E:\FirstBank\...\publish\Config\SystemParameterInfo.xml)
        to a Linux path under $LinuxDeployBasePath/Config/<same filename>. Only touches keys whose
        name ends in "-xml-file-path" (the GeneralConfig shared-config-file references); everything
        else in appsettings.json (ConnectionStrings, Kestrel endpoints, Socket sections, etc.) is left
        untouched, since only those keys point at the shared Config/ folder mirrored by
        Publish-CyberCOLAServices.ps1's Step 3 (robocopy of src/CyberCOLA/Config into publish/Config).

        Call this only for a linux-x64 publish, after Step 4's win-x64 path patch has already run
        (so the Windows publish/Config path is what's actually in the file to replace).
    #>
    param(
        [Parameter(Mandatory)][string]$PublishRootDir,
        [Parameter(Mandatory)][string]$LinuxDeployBasePath
    )

    $linuxConfigDir = "$($LinuxDeployBasePath.TrimEnd('/'))/Config"
    $pattern = '("[a-z0-9-]*-xml-file-path"\s*:\s*")[^"]*(")'

    $patchedCount = 0
    $configs = Get-ChildItem -Path $PublishRootDir -Filter "appsettings.json" -Recurse
    foreach ($cfg in $configs) {
        $lines = Get-Content $cfg.FullName
        $changed = $false

        for ($i = 0; $i -lt $lines.Count; $i++) {
            if ($lines[$i] -match $pattern) {
                $oldValue = $Matches[0]
                # Extract just the filename from the existing (Windows-style) path and rebuild it
                # under the Linux Config directory, regardless of what separator the old value used.
                $fileName = ($oldValue -split '[\\/]')[-1].TrimEnd('"')
                $replacement = '${1}' + "$linuxConfigDir/$fileName" + '${2}'
                $newLine = $lines[$i] -replace $pattern, $replacement
                if ($newLine -ne $lines[$i]) {
                    $lines[$i] = $newLine
                    $changed = $true
                }
            }
        }

        if ($changed) {
            [System.IO.File]::WriteAllLines($cfg.FullName, $lines)
            $patchedCount++
            Write-Host "    [LINUX PATH] $($cfg.Directory.Name)\$($cfg.Name)" -ForegroundColor Green
        }
    }

    Write-Host "  - Converted GeneralConfig XML paths to Linux ($linuxConfigDir) in $patchedCount file(s)." -ForegroundColor Green
}

# ---------------------------------------------------------------------------
# Cross-table (wide) CSV export/import for SocketConfig.json: one row per
# Section+Key, one column per Network.Role combination, split into a GW table
# and an Auth table since their section shapes differ. Lets you compare the
# same parameter across all networks side by side in Excel.
# ---------------------------------------------------------------------------

function Get-CyberCOLASocketConfigGWColumns {
    # (Network, Role) pairs for the GW cross-table, in column order.
    return @(
        @("FISC", "GW"), @("FISC", "GWIDVerify"),
        @("NCCC", "GW"), @("NCCC", "GWQRCode"),
        @("TWIN", "GW"),
        @("ICASH", "GW"),
        @("IPASS", "GW"),
        @("SVCS", "GW")
    )
}

function Get-CyberCOLASocketConfigAuthColumns {
    # (Network, Role) pairs for the Auth cross-table, in column order.
    return @(
        @("FISC", "Auth"), @("NCCC", "Auth"), @("TWIN", "Auth"),
        @("ICASH", "Auth"), @("IPASS", "Auth"), @("SVCS", "Auth")
    )
}

function Export-CyberCOLASocketConfigCrossTable {
    <#
        Writes two wide CSVs from SocketConfig.json: SocketConfig.GW.csv (8 columns,
        one per GW-family Network.Role) and SocketConfig.Auth.csv (6 columns, one
        per Auth Network.Role). Each row is a distinct Section+Key pair seen in any
        of that table's columns; a column with no such Section+Key is left blank -
        that reflects real structural differences (e.g. TWIN.GW has no
        ServerServiceConfig), not an error.
    #>
    param(
        [string]$SocketConfigPath = (Join-Path (Get-CyberCOLABaseDir) "src\CyberCOLA\Config\SocketConfig.json"),
        [string]$GWCsvPath = (Join-Path (Get-CyberCOLABaseDir) "src\CyberCOLA\Config\SocketConfig.GW.csv"),
        [string]$AuthCsvPath = (Join-Path (Get-CyberCOLABaseDir) "src\CyberCOLA\Config\SocketConfig.Auth.csv")
    )

    $json = Read-CyberCOLAJsonWithComments $SocketConfigPath
    if (-not $json) { Write-Warning "Failed to read: $SocketConfigPath"; return $false }

    function Write-CrossTable($columns, $csvPath) {
        # Collect the union of (Section,Key) pairs across all columns, preserving first-seen order.
        $rowKeys = New-Object System.Collections.Generic.List[object]
        $rowKeySeen = @{}
        foreach ($col in $columns) {
            $network, $role = $col
            $roleNode = $json.$network.$role
            if (-not $roleNode) { continue }
            foreach ($sectionProp in $roleNode.PSObject.Properties) {
                foreach ($keyProp in $sectionProp.Value.PSObject.Properties) {
                    $rowKey = "$($sectionProp.Name)|$($keyProp.Name)"
                    if (-not $rowKeySeen.ContainsKey($rowKey)) {
                        $rowKeySeen[$rowKey] = $true
                        $rowKeys.Add(@($sectionProp.Name, $keyProp.Name))
                    }
                }
            }
        }

        $rows = New-Object System.Collections.Generic.List[object]
        foreach ($rk in $rowKeys) {
            $section, $key = $rk
            $row = [ordered]@{ Section = $section; Key = $key }
            foreach ($col in $columns) {
                $network, $role = $col
                $colName = "$network.$role"
                $roleNode = $json.$network.$role
                $sectionNode = if ($roleNode) { $roleNode.$section } else { $null }
                $valueProp = if ($sectionNode) { $sectionNode.PSObject.Properties[$key] } else { $null }
                $rawValue = if ($valueProp) { [string]$valueProp.Value } else { "" }
                # Guard against Excel auto-detecting a digit-only string as a number and mangling it on
                # save: comma-separated port lists like "14801,14801,14801,14801" get their commas
                # stripped (e.g. "14801148011480100000"), and leading-zero IDs like "0007" lose the
                # zero padding (e.g. "7"). Wrapping as an Excel text-formula forces Excel to keep it as
                # a literal string; Import-Csv unwraps it back to the plain value below.
                $looksNumericRisky = ($rawValue -match '^\d+(,\d+)+$') -or ($rawValue -match '^0\d+$')
                $row[$colName] = if ($looksNumericRisky) { '="' + $rawValue + '"' } else { $rawValue }
            }
            $rows.Add([PSCustomObject]$row)
        }

        $rows | Export-Csv -Path $csvPath -NoTypeInformation -Encoding UTF8
        Write-Host "Exported $($rows.Count) rows -> $csvPath" -ForegroundColor Green
    }

    Write-CrossTable (Get-CyberCOLASocketConfigGWColumns) $GWCsvPath
    Write-CrossTable (Get-CyberCOLASocketConfigAuthColumns) $AuthCsvPath
    return $true
}

function Find-CyberCOLAJsonBlockLineRange {
    <#
        Locates the 0-based [Start, End] line range (inclusive) of the JSON object
        block reached by following PropertyPath (e.g. @("FISC","GW","ColaServiceConfig"))
        from the file's top-level object, by tracking brace depth line-by-line.
        Returns $null if the path cannot be found.
    #>
    param(
        [Parameter(Mandatory)][string[]]$Lines,
        [Parameter(Mandatory)][string[]]$PropertyPath
    )

    $searchStart = 0
    $searchEnd = $Lines.Count - 1
    $blockStart = $null
    $blockEnd = $null

    foreach ($propName in $PropertyPath) {
        $escapedName = [regex]::Escape($propName)
        $headerPattern = '^\s*"' + $escapedName + '"\s*:\s*\{'
        $foundIdx = $null

        for ($i = $searchStart; $i -le $searchEnd; $i++) {
            if ($Lines[$i] -match $headerPattern) { $foundIdx = $i; break }
        }
        if ($null -eq $foundIdx) { return $null }

        # Track brace depth starting at the opening "{" on the header line to find this block's closing "}".
        $depth = 0
        $closeIdx = $null
        for ($i = $foundIdx; $i -le $searchEnd; $i++) {
            $depth += ([regex]::Matches($Lines[$i], '\{')).Count
            $depth -= ([regex]::Matches($Lines[$i], '\}')).Count
            if ($depth -eq 0) { $closeIdx = $i; break }
        }
        if ($null -eq $closeIdx) { return $null }

        $blockStart = $foundIdx
        $blockEnd = $closeIdx
        $searchStart = $foundIdx + 1
        $searchEnd = $closeIdx - 1
    }

    if ($null -eq $blockStart) { return $null }
    return [PSCustomObject]@{ Start = $blockStart; End = $blockEnd }
}

function Import-CyberCOLASocketConfigCrossTable {
    <#
        Reads SocketConfig.GW.csv and SocketConfig.Auth.csv (from
        Export-CyberCOLASocketConfigCrossTable) and writes each non-blank cell's
        value back into SocketConfig.json. A cell is skipped (no write) when it is
        blank, or when its Network.Role.Section.Key does not already exist in
        SocketConfig.json - this only updates existing values, it never adds or
        removes keys/sections, so a stray CSV edit cannot corrupt the JSON
        structure.

        Many key names repeat across different Network/Role/Section blocks in this
        file, so each cell's replacement is scoped to its own Network -> Role ->
        Section brace-delimited block (via Find-CyberCOLAJsonBlockLineRange) before
        the key replace runs, rather than doing a naive whole-file regex replace
        that would touch every occurrence of that key name.
    #>
    param(
        [string]$GWCsvPath = (Join-Path (Get-CyberCOLABaseDir) "src\CyberCOLA\Config\SocketConfig.GW.csv"),
        [string]$AuthCsvPath = (Join-Path (Get-CyberCOLABaseDir) "src\CyberCOLA\Config\SocketConfig.Auth.csv"),
        [string]$SocketConfigPath = (Join-Path (Get-CyberCOLABaseDir) "src\CyberCOLA\Config\SocketConfig.json")
    )

    if (-not (Test-Path $SocketConfigPath)) { Write-Warning "SocketConfig.json not found: $SocketConfigPath"; return $false }

    $json = Read-CyberCOLAJsonWithComments $SocketConfigPath
    if (-not $json) { Write-Warning "Failed to parse: $SocketConfigPath"; return $false }

    $lines = Get-Content $SocketConfigPath
    $appliedCount = 0
    $skippedCount = 0

    foreach ($tableSpec in @(
        @{ CsvPath = $GWCsvPath; Columns = (Get-CyberCOLASocketConfigGWColumns) },
        @{ CsvPath = $AuthCsvPath; Columns = (Get-CyberCOLASocketConfigAuthColumns) }
    )) {
        $csvPath = $tableSpec.CsvPath
        $columns = $tableSpec.Columns
        if (-not (Test-Path $csvPath)) { Write-Warning "CSV not found, skipped: $csvPath"; continue }
        $rows = Import-Csv -Path $csvPath

        foreach ($row in $rows) {
            $section = $row.Section
            $key = $row.Key

            foreach ($col in $columns) {
                $network, $role = $col
                $colName = "$network.$role"
                $cellValue = $row.$colName

                if ([string]::IsNullOrEmpty($cellValue)) { continue }

                # Unwrap the ="..." Excel-text-formula guard that Export-CyberCOLASocketConfigCrossTable
                # applies to comma-separated digit strings (see there for why) back to the plain value.
                if ($cellValue -match '^="(.*)"$') { $cellValue = $Matches[1] }

                # Excel auto-capitalizes bare true/false cells (TRUE/FALSE) when the sheet is saved.
                # Every *-flag value in appsettings.json/SocketConfig.json is lowercase, so normalize
                # case-insensitively back to lowercase here regardless of how Excel stored it.
                if ($cellValue -eq 'true' -or $cellValue -eq 'false') { $cellValue = $cellValue.ToLowerInvariant() }

                $networkNode = $json.$network
                $roleNode = if ($networkNode) { $networkNode.$role } else { $null }
                $sectionNode = if ($roleNode) { $roleNode.$section } else { $null }
                $existingKey = if ($sectionNode) { $sectionNode.PSObject.Properties[$key] } else { $null }

                if (-not $existingKey) { $skippedCount++; continue }

                $blockRange = Find-CyberCOLAJsonBlockLineRange -Lines $lines -PropertyPath @($network, $role, $section)
                if (-not $blockRange) { $skippedCount++; continue }

                $escapedKey = [regex]::Escape($key)
                $escapedValue = ([string]$cellValue) -replace '\$', '$$$$'
                $pattern = '("' + $escapedKey + '"\s*:\s*")[^"]*(")'
                $replacement = '${1}' + $escapedValue + '${2}'

                $matchedInRange = $false
                for ($i = $blockRange.Start; $i -le $blockRange.End; $i++) {
                    if ($lines[$i] -match $pattern) {
                        $lines[$i] = $lines[$i] -replace $pattern, $replacement
                        $matchedInRange = $true
                        break
                    }
                }

                if ($matchedInRange) { $appliedCount++ } else { $skippedCount++ }
            }
        }
    }

    Set-Content -Path $SocketConfigPath -Value $lines -Encoding utf8
    Write-Host "Applied $appliedCount value(s), skipped $skippedCount blank/unmatched cell(s) -> $SocketConfigPath" -ForegroundColor Green
    return $true
}

# ---------------------------------------------------------------------------
# Cola session-count prompt: updates cola-service-sessions in the given Auth
# project's appsettings.json (source + publish) before services start, so the
# Auth worker picks up the new pool size at startup. Surgical regex replace
# preserves comments and formatting (no ConvertTo-Json rewrite).
# ---------------------------------------------------------------------------

function Prompt-CyberCOLASessionCount {
    param([Parameter(Mandatory)][string]$AuthProject)

    $baseDir = Get-CyberCOLABaseDir
    $srcPath = Join-Path $baseDir "src\CyberCOLA\$AuthProject\appsettings.json"

    # Show current value via the comment-stripping reader.
    $json = Read-CyberCOLAJsonWithComments $srcPath
    $current = if ($json -and $json.ColaServiceConfig.'cola-service-sessions') { [int]$json.ColaServiceConfig.'cola-service-sessions' } else { 0 }
    Write-Host "`n[Cola Session Count] current = $current  ($AuthProject appsettings.json: cola-service-sessions)" -ForegroundColor Cyan
    $reply = Read-Host "Enter new session count [Enter to keep current]"
    if ([string]::IsNullOrWhiteSpace($reply)) { return }

    $n = 0
    if (-not [int]::TryParse($reply.Trim(), [ref]$n) -or $n -le 0) {
        Write-Warning "Invalid session count '$reply'. Kept current ($current)."
        return
    }
    if ($n -eq $current) { Write-Host "  - unchanged ($n)" -ForegroundColor Gray; return }

    # ${1}/${2} are regex backreferences (single-quoted so PowerShell does not
    # expand them); $n is the new count.
    $pattern = '("cola-service-sessions"\s*:\s*")\d+(")'
    $replacement = '${1}' + $n + '${2}'
    $targets = @(
        $srcPath,
        (Join-Path $baseDir "publish\win-x64\$AuthProject\appsettings.json")
    )
    foreach ($path in $targets) {
        if (-not (Test-Path $path)) { Write-Warning "appsettings not found, skipped: $path"; continue }
        $raw = Get-Content $path -Raw
        if ($raw -notmatch $pattern) { Write-Warning "cola-service-sessions not found in $path"; continue }
        Set-Content -Path $path -Value ($raw -replace $pattern, $replacement) -Encoding utf8 -NoNewline
        Write-Host "  - $AuthProject session count -> $n  ($path)" -ForegroundColor Green
    }
}

function Get-CyberCOLADatabaseConnectionString {
    param([string]$XmlPath)

    if (-not $XmlPath) {
        $baseDir = Get-CyberCOLABaseDir
        $XmlPath = Join-Path $baseDir "publish\Config\SystemParameterInfo.xml"
        if (-not (Test-Path $XmlPath)) { $XmlPath = Join-Path $baseDir "src\CyberCOLA\Config\SystemParameterInfo.xml" }
    }

    if (-not (Test-Path $XmlPath)) {
        Write-Warning "SystemParameterInfo.xml not found."
        return $null
    }

    try {
        [xml]$xml = Get-Content $XmlPath
        $dbInfo = $xml.SystemParameterInfo.DatabaseInfo
        $encryptedConn = $dbInfo.ConnectionString.Trim()
        $isEncrypted = $dbInfo.IsEncrypted -eq "true"
        $keyString = $dbInfo.EncryptedKey.Trim()

        if (-not $isEncrypted) { return $encryptedConn }

        $cipherBytes = [System.Convert]::FromBase64String($encryptedConn)
        $finalKeyString = $keyString.PadRight(32)
        $keyBytes = [System.Text.Encoding]::UTF8.GetBytes($finalKeyString)
        $ivString = "87654321".PadRight(12)
        $ivBytes = [System.Text.Encoding]::UTF8.GetBytes($ivString)

        $tag = New-Object byte[] 16
        $cipherBytesWithoutTag = New-Object byte[] ($cipherBytes.Length - 16)
        [System.Array]::Copy($cipherBytes, ($cipherBytes.Length - 16), $tag, 0, 16)
        [System.Array]::Copy($cipherBytes, 0, $cipherBytesWithoutTag, 0, ($cipherBytes.Length - 16))

        $aesGcm = New-Object System.Security.Cryptography.AesGcm($keyBytes, 16)
        $plaintextBytes = New-Object byte[] $cipherBytesWithoutTag.Length
        $aesGcm.Decrypt($ivBytes, $cipherBytesWithoutTag, $tag, $plaintextBytes)
        $aesGcm.Dispose()

        $conn = [System.Text.Encoding]::UTF8.GetString($plaintextBytes)
        # System.Data.SqlClient does not support the Always Encrypted keyword; strip it.
        $conn = $conn -replace '\s*;?\s*Column Encryption Setting\s*=\s*[^;]+', ''
        return $conn
    } catch {
        Write-Warning "Failed to decrypt database connection string: $($_.Exception.Message)"
        return $null
    }
}

# ---------------------------------------------------------------------------
# Service / project catalog
# ---------------------------------------------------------------------------

function Get-CyberCOLAServiceCatalog {
    return @(
        @{ Name = 'CyberAuthFISC';      Type = 'Auth';   Module = 'FISC';         ProjectDir = 'CyberAuthFISC' }
        @{ Name = 'CyberAuthICASH';     Type = 'Auth';   Module = 'ICASH';        ProjectDir = 'CyberAuthICASH' }
        @{ Name = 'CyberAuthIPASS';     Type = 'Auth';   Module = 'IPASS';        ProjectDir = 'CyberAuthIPASS' }
        @{ Name = 'CyberAuthNCCC';      Type = 'Auth';   Module = 'NCCC';         ProjectDir = 'CyberAuthNCCC' }
        @{ Name = 'CyberAuthSVCS';      Type = 'Auth';   Module = 'SVCS';         ProjectDir = 'CyberAuthSVCS' }
        @{ Name = 'CyberGWFISC';        Type = 'GW';     Module = 'FISC';         ProjectDir = 'CyberGWFISC' }
        @{ Name = 'CyberGWICASH';       Type = 'GW';     Module = 'ICASH';        ProjectDir = 'CyberGWICASH' }
        @{ Name = 'CyberGWIPASS';       Type = 'GW';     Module = 'IPASS';        ProjectDir = 'CyberGWIPASS' }
        @{ Name = 'CyberGWNCCC';        Type = 'GW';     Module = 'NCCC';         ProjectDir = 'CyberGWNCCC' }
        @{ Name = 'CyberGWSVCS';        Type = 'GW';     Module = 'SVCS';         ProjectDir = 'CyberGWSVCS' }
        @{ Name = 'CyberGWFISCIDVerify'; Type = 'GW';     Module = 'FISCIDVerify'; ProjectDir = 'CyberGWFISCIDVerify' }
        @{ Name = 'CyberGWNCCCQRCode';  Type = 'GW';     Module = 'NCCCQRCode';   ProjectDir = 'CyberGWNCCCQRCode' }
        @{ Name = 'CyberSMS';           Type = 'SMS';    Module = 'Core';         ProjectDir = 'CyberSMS' }
        @{ Name = 'ServiceRouter';      Type = 'Router'; Module = 'Core';         ProjectDir = 'ServiceRouter' }
    )
}

function Get-CyberCOLAServicePortConfig {
    # Used by console/service menus and monitors
    return @(
        @{ Name = 'CyberAuthFISC';      Key = 'cola-service-bind-port'; Section = 'ColaServiceConfig' }
        @{ Name = 'CyberAuthICASH';     Key = 'cola-service-bind-port'; Section = 'ColaServiceConfig' }
        @{ Name = 'CyberAuthIPASS';     Key = 'cola-service-bind-port'; Section = 'ColaServiceConfig' }
        @{ Name = 'CyberAuthNCCC';      Key = 'cola-service-bind-port'; Section = 'ColaServiceConfig' }
        @{ Name = 'CyberAuthSVCS';      Key = 'cola-service-bind-port'; Section = 'ColaServiceConfig' }
        @{ Name = 'CyberGWFISC';        Key = 'cola-service-bind-port'; Section = 'ColaServiceConfig' }
        @{ Name = 'CyberGWICASH';       Key = 'cola-service-bind-port'; Section = 'ColaServiceConfig' }
        @{ Name = 'CyberGWIPASS';       Key = 'cola-service-bind-port'; Section = 'ColaServiceConfig' }
        @{ Name = 'CyberGWNCCC';        Key = 'cola-service-bind-port'; Section = 'ColaServiceConfig' }
        @{ Name = 'CyberGWSVCS';        Key = 'cola-service-bind-port'; Section = 'ColaServiceConfig' }
        @{ Name = 'CyberGWFISCIDVerify'; Key = 'cola-service-bind-port'; Section = 'ColaServiceConfig' }
        @{ Name = 'CyberGWNCCCQRCode';  Key = 'cola-service-bind-port'; Section = 'ColaServiceConfig' }
        @{ Name = 'CyberSMS';           Key = 'cola-service-bind-port'; Section = 'ColaServiceConfig' }
        @{ Name = 'ServiceRouter';      Key = 'service-router-info-xml-file-path'; Section = 'GeneralConfig' }
    )
}

function Get-CyberCOLATCPServiceCatalog {
    # Extended catalog for TCP/status dashboards: the core services from Get-CyberCOLAServicePortConfig
    # plus the WPF simulator entries (DisplayName/SubFolder/DefaultPort) shown alongside them.
    # Used by COLAMainMenu-Console.ps1 (Get-TCPStatus) and Monitor-COLAServices.ps1.
    return @(
        # --- FISC Services ---
        @{ Name = 'CyberAuthFISC';      Key = 'cola-service-bind-port'; Section = 'ColaServiceConfig' }
        @{ Name = 'CyberGWFISC';        Key = 'cola-service-bind-port'; Section = 'ColaServiceConfig' }
        @{ Name = 'CyberGWFISCIDVerify'; Key = 'cola-service-bind-port'; Section = 'ColaServiceConfig' }
        @{ Name = 'WpfHighStressSimulator'; DisplayName = 'WpfHighStressSim (FISC)'; Key = 'ListenPortList'; Section = 'GeneralConfig'; SubFolder = 'Fisc'; DefaultPort = 8100 }
        @{ Name = 'WpfHighStressSimulator'; DisplayName = 'WpfHighStressSim (FISC IDVerify)'; Key = 'ListenPortList'; Section = 'GeneralConfig'; SubFolder = 'FiscIDVerify'; DefaultPort = 9100 }

        # --- NCCC Services ---
        @{ Name = 'CyberAuthNCCC';      Key = 'cola-service-bind-port'; Section = 'ColaServiceConfig' }
        @{ Name = 'CyberGWNCCC';        Key = 'cola-service-bind-port'; Section = 'ColaServiceConfig' }
        @{ Name = 'CyberGWNCCCQRCode';  Key = 'cola-service-bind-port'; Section = 'ColaServiceConfig' }
        @{ Name = 'WpfHighStressSimulator'; DisplayName = 'WpfHighStressSim (NCCC)'; Key = 'ListenPortList'; Section = 'GeneralConfig'; SubFolder = 'Nccc'; DefaultPort = 14801 }
        @{ Name = 'WpfHighStressSimulator'; DisplayName = 'WpfHighStressSim (NCCC QRCode)'; Key = 'ListenPortList'; Section = 'GeneralConfig'; SubFolder = 'NcccQRCode'; DefaultPort = 14851 }

        # --- ICASH Services ---
        @{ Name = 'CyberAuthICASH';     Key = 'cola-service-bind-port'; Section = 'ColaServiceConfig' }
        @{ Name = 'CyberGWICASH';       Key = 'cola-service-bind-port'; Section = 'ColaServiceConfig' }
        @{ Name = 'WpfETicketClientSimulator'; DisplayName = 'WpfETicketSim (ICASH)'; Key = 'ICASHETicketLocalPort'; Section = 'GeneralConfig'; SubFolder = 'ICASH'; DefaultPort = 3200 }

        # --- IPASS Services ---
        @{ Name = 'CyberAuthIPASS';     Key = 'cola-service-bind-port'; Section = 'ColaServiceConfig' }
        @{ Name = 'CyberGWIPASS';       Key = 'cola-service-bind-port'; Section = 'ColaServiceConfig' }
        @{ Name = 'WpfETicketClientSimulator'; DisplayName = 'WpfETicketSim (IPASS)'; Key = 'IPASSETicketLocalPort'; Section = 'GeneralConfig'; SubFolder = 'IPASS'; DefaultPort = 3300 }

        # --- SVCS Services ---
        @{ Name = 'CyberAuthSVCS';      Key = 'cola-service-bind-port'; Section = 'ColaServiceConfig' }
        @{ Name = 'CyberGWSVCS';        Key = 'cola-service-bind-port'; Section = 'ColaServiceConfig' }
        @{ Name = 'WpfETicketClientSimulator'; DisplayName = 'WpfETicketSim (SVCS)'; Key = 'SVCSETicketLocalPort'; Section = 'GeneralConfig'; SubFolder = 'SVCS'; DefaultPort = 3100 }

        # --- Other Services ---
        @{ Name = 'CyberSMS';           Key = 'cola-service-bind-port'; Section = 'ColaServiceConfig' }
        @{ Name = 'ServiceRouter';      Key = 'service-router-info-xml-file-path'; Section = 'GeneralConfig' }
        @{ Name = 'WpfSMSSimulator';    DisplayName = 'WpfSMSSimulator'; Key = 'server-service-listen-port'; Section = 'GeneralConfig'; DefaultPort = 10000 }
        @{ Name = 'WpfSMSWebApiServer'; DisplayName = 'WpfSMSWebApiServer'; Key = ''; Section = ''; DefaultPort = 7293 }
    )
}

function Get-CyberCOLAWebAppCatalog {
    # The 22 Web* Kestrel/Blazor project name/path/URL entries, single source of truth for
    # COLAMainMenu.ps1's dashboard and StartStop-WebApps.ps1's start/stop/port-conflict tooling.
    # Default HTTP/HTTPS ports mirror each project's launchSettings.json at time of writing;
    # callers that need the live configured port should re-read launchSettings.json per app.
    return @(
        @{ Name = 'WebClientSimulator'; Path = 'src\CyberCOLA\WebClientSimulator'; Url = 'http://localhost:5220'; HttpsUrl = 'https://localhost:5219' }
        @{ Name = 'WebEAISimulator'; Path = 'src\CyberCOLA\WebEAISimulator'; Url = 'http://localhost:5080'; HttpsUrl = 'https://localhost:5079' }
        @{ Name = 'WebEAITCPClient'; Path = 'src\CyberCOLA\WebEAITCPClient'; Url = 'http://localhost:50820'; HttpsUrl = 'https://localhost:50819' }
        @{ Name = 'WebETicketClientSimulator'; Path = 'src\CyberCOLA\WebETicketClientSimulator'; Url = 'http://localhost:5100'; HttpsUrl = 'https://localhost:5099' }
        @{ Name = 'WebFiscMaintSimulator'; Path = 'src\CyberCOLA\WebFiscMaintSimulator'; Url = 'http://localhost:5210'; HttpsUrl = 'https://localhost:5209' }
        @{ Name = 'WebHighStressSimulator'; Path = 'src\CyberCOLA\WebHighStressSimulator'; Url = 'http://localhost:5090'; HttpsUrl = 'https://localhost:5089' }
        @{ Name = 'WebHSMSimulator'; Path = 'src\CyberCOLA\WebHSMSimulator'; Url = 'http://localhost:5085'; HttpsUrl = 'https://localhost:5084' }
        @{ Name = 'WebISO8583'; Path = 'src\CyberCOLA\WebISO8583'; Url = 'http://localhost:5237'; HttpsUrl = '' }
        @{ Name = 'WebMaintGatewayTester'; Path = 'src\CyberCOLA\WebMaintGatewayTester'; Url = 'http://localhost:5073'; HttpsUrl = '' }
        @{ Name = 'WebMonitorService'; Path = 'src\CyberCOLA\WebMonitorService'; Url = 'http://localhost:48817'; HttpsUrl = 'https://localhost:48816' }
        @{ Name = 'WebMQTest'; Path = 'src\CyberCOLA\WebMQTest'; Url = 'http://localhost:5269'; HttpsUrl = '' }
        @{ Name = 'WebServerMonitor'; Path = 'src\CyberCOLA\WebServerMonitor'; Url = 'http://localhost:5024'; HttpsUrl = '' }
        @{ Name = 'WebServerSimulator'; Path = 'src\CyberCOLA\WebServerSimulator'; Url = 'http://localhost:5235'; HttpsUrl = 'https://localhost:5234' }
        @{ Name = 'WebSMSSimulator'; Path = 'src\CyberCOLA\WebSMSSimulator'; Url = 'http://localhost:5095'; HttpsUrl = 'https://localhost:5094' }
        @{ Name = 'WebSMSWebApiServer'; Path = 'src\CyberCOLA\WebSMSWebApiServer'; Url = 'http://localhost:60132'; HttpsUrl = 'https://localhost:60130' }
        @{ Name = 'WebStressSimulator'; Path = 'src\CyberCOLA\WebStressSimulator'; Url = 'http://localhost:5200'; HttpsUrl = 'https://localhost:5199' }
        @{ Name = 'WebSVCSClientSimulator'; Path = 'src\CyberCOLA\WebSVCSClientSimulator'; Url = 'http://localhost:5230'; HttpsUrl = 'https://localhost:5229' }
        @{ Name = 'WebTCPClient'; Path = 'src\CyberCOLA\WebTCPClient'; Url = 'http://localhost:55764'; HttpsUrl = 'https://localhost:55763' }
        @{ Name = 'WebTCPClientAuthRouterClient'; Path = 'src\CyberCOLA\WebTCPClientAuthRouterClient'; Url = 'http://localhost:49736'; HttpsUrl = 'https://localhost:49735' }
        @{ Name = 'WebTCPClientService'; Path = 'src\CyberCOLA\WebTCPClientService'; Url = 'http://localhost:63958'; HttpsUrl = 'https://localhost:63957' }
        @{ Name = 'WebTCPConnectionMonitor'; Path = 'src\CyberCOLA\WebTCPConnectionMonitor'; Url = 'http://localhost:5149'; HttpsUrl = '' }
        @{ Name = 'WebVDMQSimulator'; Path = 'src\CyberCOLA\WebVDMQSimulator'; Url = 'http://localhost:5249'; HttpsUrl = '' }
    )
}

function Get-CyberCOLAMaintGatewayModes {
    return @{
        HTTP  = @{ Gateway = 'MaintGateway'; Tester = 'MaintGatewayTester';         Ports = @(5000, 5005); SimulatorPort = 14801; Url = 'http://localhost:5000' }
        HTTPS = @{ Gateway = 'MaintGatewayHttps'; Tester = 'MaintGatewayTesterHttps'; Ports = @(5000, 5001, 5005); SimulatorPort = 14801; Url = 'https://localhost:5001' }
        JWT   = @{ Gateway = 'MaintGatewayJwt'; Tester = 'MaintGatewayTesterJwt';     Ports = @(5000, 5001, 5005); SimulatorPort = 14801; Url = 'https://localhost:5001' }
    }
}

function Get-CyberCOLAMaintGatewayUnifiedModes {
    # Ports/URLs mirror MaintGatewayUnified appsettings.{Mode}.json:
    #   HTTP  -> gateway Kestrel 5000,                  tester 5005
    #   HTTPS -> gateway Kestrel 5000(http)+5001(https), tester 5005
    #   JWT   -> gateway Kestrel 5000(http)+5001(https), tester 5005
    # The single project selects its mode via ASPNETCORE_ENVIRONMENT ({Mode} loads appsettings.{Mode}.json).
    return @{
        HTTP  = @{ Gateway = 'MaintGatewayUnified'; Tester = 'MaintGatewayUnifiedTester'; Environment = 'Http';  Ports = @(5000, 5005); SimulatorPort = 14801; Url = 'http://localhost:5000' }
        HTTPS = @{ Gateway = 'MaintGatewayUnified'; Tester = 'MaintGatewayUnifiedTester'; Environment = 'Https'; Ports = @(5000, 5001, 5005); SimulatorPort = 14801; Url = 'https://localhost:5001' }
        JWT   = @{ Gateway = 'MaintGatewayUnified'; Tester = 'MaintGatewayUnifiedTester'; Environment = 'Jwt';   Ports = @(5000, 5001, 5005); SimulatorPort = 14801; Url = 'https://localhost:5001' }
    }
}

# ---------------------------------------------------------------------------
# Port resolution
# ---------------------------------------------------------------------------

function ConvertTo-CyberCOLAPortList {
    # Normalize a JSON config port value (int, int[], comma-separated string) into [int[]].
    param($Value)
    if (-not $Value) { return @() }
    if ($Value -is [array]) { return $Value | ForEach-Object { [int]$_ } }
    if ($Value -is [string] -and $Value.Contains(',')) { return $Value -split ',' | ForEach-Object { [int]$_.Trim() } }
    return @([int]$Value)
}

function Get-CyberCOLAServicePort {
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [string]$Section = 'ColaServiceConfig',
        [string]$Key = 'cola-service-bind-port',
        [string]$SubFolder = $null,
        [int]$DefaultPort = 0,
        [hashtable]$Cache = $null
    )

    $baseDir = Get-CyberCOLABaseDir
    $cacheKey = if ($SubFolder) { "$Name-$SubFolder" } else { $Name }
    if ($Cache -and $Cache.ContainsKey($cacheKey)) {
        return $Cache[$cacheKey]
    }

    $ports = @()

    switch ($Name) {
        'ServiceRouter' {
            $jsonPath = Join-Path $baseDir 'src\CyberCOLA\ServiceRouter\appsettings.json'
            $json = Read-CyberCOLAJsonWithComments $jsonPath
            $xmlPath = $json?.GeneralConfig.'service-router-info-xml-file-path'
            if (-not $xmlPath -or -not (Test-Path $xmlPath)) {
                $xmlPath = Join-Path $baseDir 'src\CyberCOLA\Config\ServiceRouterInfo.xml'
            }
            if (Test-Path $xmlPath) {
                try {
                    [xml]$xml = Get-Content $xmlPath
                    if ($xml.ServiceRouterInfo.ServerServiceInfo.ListenerPort) { $ports += [int]$xml.ServiceRouterInfo.ServerServiceInfo.ListenerPort }
                    if ($xml.ServiceRouterInfo.ClientServiceInfo.ListenerPort) { $ports += [int]$xml.ServiceRouterInfo.ClientServiceInfo.ListenerPort }
                } catch {
                    Write-Warning "Failed to parse ServiceRouter XML: $xmlPath"
                }
            }
            if ($ports.Count -eq 0) { $ports = @(9901, 9902) }
        }
        'WpfSMSWebApiServer' {
            $ports = @(7293)
        }
        { $_ -in 'WpfHighStressSimulator', 'WpfStressSimulator', 'WpfETicketClientSimulator' } {
            $jsonPath = Join-Path $baseDir "publish\Config\$Name\$SubFolder\appsettings.json"
            if (-not (Test-Path $jsonPath)) {
                $jsonPath = Join-Path $baseDir "src\CyberCOLA\$Name\appsettings.json"
            }
            $json = Read-CyberCOLAJsonWithComments $jsonPath
            if ($json -and $Section -and $Key) {
                $ports += ConvertTo-CyberCOLAPortList -Value $json.$Section.$Key
            }
            if ($ports.Count -eq 0 -and $DefaultPort -gt 0) { $ports = @($DefaultPort) }
        }
        default {
            $jsonPath = Join-Path $baseDir "src\CyberCOLA\$Name\appsettings.json"
            $json = Read-CyberCOLAJsonWithComments $jsonPath
            if ($json -and $Section -and $Key) {
                $ports += ConvertTo-CyberCOLAPortList -Value $json.$Section.$Key
            }
            if ($ports.Count -eq 0 -and $DefaultPort -gt 0) { $ports = @($DefaultPort) }
        }
    }

    if ($Cache) { $Cache[$cacheKey] = $ports }
    return $ports
}

# ---------------------------------------------------------------------------
# TCP helpers
# ---------------------------------------------------------------------------

function Get-CyberCOLATCPTable {
    # Returns hashtable with Listeners and Connections (as arrays)
    $props = [System.Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties()
    return @{
        Listeners = $props.GetActiveTcpListeners()
        Connections = $props.GetActiveTcpConnections()
    }
}

function Get-CyberCOLAListenerPortSet {
    param($Listeners)
    $set = @{}
    foreach ($l in $Listeners) { $set[$l.Port] = $true }
    return $set
}

function Test-CyberCOLAPortListening {
    param(
        [Parameter(Mandatory)]
        [int]$Port,

        $Listeners = $null
    )
    if (-not $Listeners) { $Listeners = (Get-CyberCOLATCPTable).Listeners }
    foreach ($l in $Listeners) {
        if ($l.Port -eq $Port) { return $true }
    }
    return $false
}

# ---------------------------------------------------------------------------
# Hyper-V dynamic port exclusion (netsh) helpers
# ---------------------------------------------------------------------------

function Get-CyberCOLAExcludedPortRanges {
    # Returns TCP port ranges reserved by Windows/Hyper-V (netsh interface ipv4 show excludedportrange).
    # Note: netsh is a native executable, not a cmdlet -- it does not understand -ErrorAction,
    # so that flag must not be passed to it (PowerShell would otherwise pass it through literally
    # as a netsh argument, causing netsh to print a usage error instead of port data).
    $ranges = @()
    try {
        $netshOutput = netsh interface ipv4 show excludedportrange protocol=tcp
        foreach ($line in $netshOutput) {
            if ($line -match '^\s*(\d+)\s+(\d+)\s*(?:\*)?\s*$') {
                $ranges += [PSCustomObject]@{
                    Start = [int]$Matches[1]
                    End = [int]$Matches[2]
                }
            }
        }
    } catch {}
    return $ranges
}

function Test-CyberCOLAPortExcluded {
    param(
        [Parameter(Mandatory)]
        [int]$Port,

        $Ranges
    )
    if (-not $Ranges) { return $false }
    foreach ($r in $Ranges) {
        if ($Port -ge $r.Start -and $Port -le $r.End) { return $true }
    }
    return $false
}

function Wait-CyberCOLAPorts {
    param(
        [string]$Description = 'Services',
        [Parameter(Mandatory)]
        [int[]]$Ports,

        [string]$ProcessName = $null,
        [int]$TimeoutSec = 10,
        [int]$PollMs = 500
    )

    if ($Ports.Count -eq 0) { return $true }

    Write-Host "  -> Waiting for $Description to be ready" -ForegroundColor Gray -NoNewline
    $start = Get-Date
    while (((Get-Date) - $start).TotalSeconds -lt $TimeoutSec) {
        if (Test-CyberCOLAKeyAvailable) { $null = Read-CyberCOLAConsoleKey -Intercept; break }
        if ($ProcessName -and -not (Get-Process $ProcessName -ErrorAction SilentlyContinue)) {
            Write-Host ' [FAILED: Process Terminated]' -ForegroundColor Red
            return $false
        }

        $listeners = [System.Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners()
        $portSet = Get-CyberCOLAListenerPortSet -Listeners $listeners
        $allReady = $true
        foreach ($p in $Ports) {
            if (-not $portSet.ContainsKey($p)) { $allReady = $false; break }
        }
        if ($allReady) {
            $elapsed = [math]::Round(((Get-Date) - $start).TotalSeconds, 1)
            Write-Host " OK (${elapsed}s)." -ForegroundColor Green
            return $true
        }

        Write-Host '.' -ForegroundColor Gray -NoNewline
        Start-Sleep -Milliseconds $PollMs
    }

    Write-Host ' Timeout (Still Offline).' -ForegroundColor Yellow
    return $false
}

function Wait-CyberCOLAPortConnectable {
    param(
        [Parameter(Mandatory)]
        [string]$HostName,

        [Parameter(Mandatory)]
        [int]$Port,

        [int]$TimeoutSec = 30,
        [int]$PollMs = 1000
    )

    Write-Host "  > Waiting for $HostName`:$Port to be ready..." -NoNewline
    $start = Get-Date
    $lastError = $null
    while (((Get-Date) - $start).TotalSeconds -lt $TimeoutSec) {
        $socket = $null
        try {
            $socket = [System.Net.Sockets.TcpClient]::new()
            $task = $socket.ConnectAsync($HostName, $Port)
            if ($task.Wait($PollMs)) {
                if ($socket.Connected) {
                    Write-Host ' [OK]' -ForegroundColor Green
                    return $true
                }
            }
        } catch {
            $lastError = $_.Exception.Message
        } finally {
            if ($socket) { $socket.Dispose() }
        }

        Write-Host '.' -NoNewline
        Start-Sleep -Milliseconds $PollMs
    }

    try {
        $ipList = [System.Net.Dns]::GetHostAddresses($HostName) | ForEach-Object { $_.IPAddressToString }
    } catch { $ipList = @('(resolution failed)') }

    Write-Host ' [Timeout]' -ForegroundColor Red
    Write-Host "    (Diagnostics: Target=$HostName, IPs=$($ipList -join ','), Port=$Port)" -ForegroundColor Yellow
    if ($lastError) { Write-Host "    (Last Connection Error - $lastError)" -ForegroundColor Yellow }
    return $false
}

# ---------------------------------------------------------------------------
# Process helpers
# ---------------------------------------------------------------------------

function Resolve-CyberCOLAExecutable {
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [string]$Runtime = 'win-x64'
    )

    $baseDir = Get-CyberCOLABaseDir
    $publishDir = Get-CyberCOLAPublishDir -Runtime $Runtime

    $candidates = @(
        Join-Path $publishDir "$Name\$Name.exe"
        Join-Path $baseDir "src\CyberCOLA\$Name\bin\x64\Release\net10.0\$Name.exe"
        Join-Path $baseDir "src\CyberCOLA\$Name\bin\Release\net10.0\win-x64\$Name.exe"
        Join-Path $baseDir "src\CyberCOLA\$Name\bin\x64\Debug\net10.0\$Name.exe"
    )

    foreach ($p in $candidates) {
        if (Test-Path $p) { return $p }
    }
    return $null
}

function Start-CyberCOLAProcess {
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [string]$Runtime = 'win-x64',
        [switch]$NoWait
    )

    $exe = Resolve-CyberCOLAExecutable -Name $Name -Runtime $Runtime
    if (-not $exe) {
        Write-Warning "Executable not found for $Name"
        return $false
    }

    if (-not (Get-Process $Name -ErrorAction SilentlyContinue)) {
        Start-Process cmd.exe -ArgumentList "/c title $Name & `"$exe`"" -WindowStyle Normal
        if (-not $NoWait) { Start-Sleep -Seconds 1 }
        return $true
    }
    return $true
}

function Stop-CyberCOLAProcesses {
    param(
        [Parameter(Mandatory)][string[]]$Names,

        # When set, wait (up to this many seconds) for the specific processes that were actually
        # stopped by this call to exit, instead of returning immediately after signaling Stop-Process.
        # Waits only on the captured PIDs (not by re-matching $Names), so it never blocks on an
        # unrelated process that happens to share a name/wildcard match.
        [int]$WaitSeconds = 0
    )
    $stoppedIds = [System.Collections.Generic.List[int]]::new()
    foreach ($n in $Names) {
        $procs = Get-Process -Name $n -ErrorAction SilentlyContinue
        if ($procs) {
            foreach ($p in $procs) { $stoppedIds.Add($p.Id) }
            $procs | Stop-Process -Force -ErrorAction SilentlyContinue
        }
    }
    if ($WaitSeconds -gt 0 -and $stoppedIds.Count -gt 0) {
        Wait-Process -Id $stoppedIds -Timeout $WaitSeconds -ErrorAction SilentlyContinue
    }
}

function Stop-CyberCOLAProcessesByCommandLine {
    # Stop processes by name, optionally filtered by a CommandLine substring (via Win32_Process).
    # The CommandLine filter targets only instances launched with a specific config subfolder,
    # e.g. stopping WpfHighStressSimulator instances tagged with "Fisc" but not "NCCC".
    param(
        [Parameter(Mandatory)][string]$ProjectName,
        [string]$CommandLinePattern = $null
    )
    if ($CommandLinePattern) {
        $procs = Get-CimInstance Win32_Process -Filter "Name = '$ProjectName.exe'" -ErrorAction SilentlyContinue
        foreach ($p in $procs) {
            if ($p.CommandLine -like "*$CommandLinePattern*") {
                Stop-Process -Id $p.ProcessId -Force -ErrorAction SilentlyContinue
                Write-Host "Stopped $ProjectName ($CommandLinePattern) [PID: $($p.ProcessId)]" -ForegroundColor Gray
            }
        }
    } else {
        $procs = Get-Process -Name $ProjectName -ErrorAction SilentlyContinue
        if ($procs) {
            $procs | Stop-Process -Force -ErrorAction SilentlyContinue
            Write-Host "Stopped all instances of $ProjectName" -ForegroundColor Gray
        }
    }
}

function Close-CyberCOLAConsoleWindows {
    # Close the standalone console windows that Start-CyberCOLAProcess opens for each service.
    # Each window is launched as `cmd.exe /c title <Name> & <exe>`, so its window title carries the
    # service name. Stop-CyberCOLAProcesses kills the underlying exe, but on Windows 11 the default
    # console host is Windows Terminal: after the exe exits the cmd.exe process is gone yet the
    # WindowsTerminal window lingers on the exit prompt, so matching `Get-Process cmd` finds nothing.
    # Use Win32 EnumWindows to locate top-level windows by title (regardless of owner process) and
    # PostMessage WM_CLOSE so the host window closes whether it is cmd / conhost / Windows Terminal.
    # Matching is substring + case-insensitive on the distinctive service name; unrelated windows are
    # left untouched.
    param([Parameter(Mandatory)][string[]]$Names)

    $csharpSource = @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

public static class CyberCOLAWindowCloser {
    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern int GetWindowTextLength(IntPtr hWnd);
    [DllImport("user32.dll")]
    public static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    public const uint WM_CLOSE = 0x0010;

    public static int CloseByTitles(string[] titles) {
        var result = new List<IntPtr>();
        EnumWindows((hWnd, lParam) => {
            if (!IsWindowVisible(hWnd)) return true;
            int len = GetWindowTextLength(hWnd);
            if (len <= 0) return true;
            var sb = new StringBuilder(len + 1);
            GetWindowText(hWnd, sb, sb.Capacity);
            string title = sb.ToString();
            foreach (string t in titles) {
                if (title.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0) {
                    result.Add(hWnd);
                    break;
                }
            }
            return true;
        }, IntPtr.Zero);
        foreach (var hWnd in result) {
            PostMessage(hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        }
        return result.Count;
    }

    public static List<string> ListVisibleTitles() {
        var result = new List<string>();
        EnumWindows((hWnd, lParam) => {
            if (!IsWindowVisible(hWnd)) return true;
            int len = GetWindowTextLength(hWnd);
            if (len <= 0) return true;
            var sb = new StringBuilder(len + 1);
            GetWindowText(hWnd, sb, sb.Capacity);
            result.Add(sb.ToString());
            return true;
        }, IntPtr.Zero);
        return result;
    }
}
"@
    try {
        if (-not ([System.Management.Automation.PSTypeName]'CyberCOLAWindowCloser').Type) {
            Add-Type -TypeDefinition $csharpSource -ErrorAction Stop
        }
        $closed = [CyberCOLAWindowCloser]::CloseByTitles($Names)
        Write-Host "  -> Closed $closed lingering console window(s)." -ForegroundColor Gray
        if ($closed -eq 0) {
            # Diagnostic: no title matched. Dump visible window titles that look CyberCOLA-related so the
            # actual Windows Terminal title can be reported back and the matcher adjusted.
            $candidates = [CyberCOLAWindowCloser]::ListVisibleTitles() |
                Where-Object { $_ -match 'Cyber|Service|Router|NCCC|FISC|GW|cmd|terminal' }
            if ($candidates) {
                Write-Host "  -> No match. Visible candidate window titles:" -ForegroundColor Yellow
                $candidates | ForEach-Object { Write-Host "       - $_" -ForegroundColor Yellow }
            }
        }
    } catch {
        # Fallback for environments without Add-Type (e.g. constrained runspace): match cmd.exe by title.
        foreach ($n in $Names) {
            Get-Process -Name cmd -ErrorAction SilentlyContinue |
                Where-Object { $_.MainWindowTitle -ieq $n } |
                ForEach-Object { try { Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue } catch { } }
        }
    }
}

# ---------------------------------------------------------------------------
# Extended port resolution (multiple section/key pairs)
# ---------------------------------------------------------------------------

function Get-CyberCOLAExpectedPorts {
    param(
        [Parameter(Mandatory)]
        [string]$Name
    )

    $baseDir = Get-CyberCOLABaseDir
    $ports = @()
    $jsonPath = Join-Path $baseDir "src\CyberCOLA\$Name\appsettings.json"
    $json = Read-CyberCOLAJsonWithComments $jsonPath

    if ($json) {
        $candidates = @(
            @{ Section = 'ColaServiceConfig';          Key = 'cola-service-bind-port' }
            @{ Section = 'ServerServiceConfig';        Key = 'server-service-bind-port' }
            @{ Section = 'ClientAuthRouterConfig';     Key = 'clientauth-router-service-listener-port' }
            @{ Section = 'IDVerifyServerServiceConfig'; Key = 'server-service-listen-port' }
        )
        foreach ($c in $candidates) {
            $ports += ConvertTo-CyberCOLAPortList -Value $json.($c.Section).($c.Key)
        }
    }

    if ($Name -eq 'ServiceRouter' -and $ports.Count -eq 0) { $ports = @(9901, 9902) }
    return $ports
}

# ---------------------------------------------------------------------------
# Detect running CyberCOLA processes and resolve their config-derived ports
# Returns objects: @{ ProcessName; ProcessId; Service; Endpoint; Port; Type }
# ---------------------------------------------------------------------------

function Test-CyberCOLAIsNumeric {
    param([string]$Value)
    return $Value -match '^\s*\d+\s*$'
}

function Get-CyberCOLARunningEndpoints {
    $baseDir = Get-CyberCOLABaseDir
    $endpoints = [System.Collections.Generic.List[object]]::new()
    $catalog = Get-CyberCOLAServiceCatalog
    $portConfig = Get-CyberCOLAServicePortConfig

    # Only report ports that are actually bound or have active connections right now.
    $tcpRows = Get-CyberCOLAFastTCPTable
    $activeLocalPorts = @{}
    foreach ($r in $tcpRows) {
        if ($r.LocalPort -gt 0) { $activeLocalPorts[$r.LocalPort] = $true }
    }

    # Core services that have matching process names
    $processNames = @(
        'CyberAuthFISC','CyberAuthICASH','CyberAuthIPASS','CyberAuthNCCC','CyberAuthSVCS','CyberAuthTWIN',
        'CyberGWFISC','CyberGWICASH','CyberGWIPASS','CyberGWNCCC','CyberGWSVCS','CyberGWFISCIDVerify','CyberGWNCCCQRCode',
        'CyberSMS','ServiceRouter',
        'MaintGateway','MaintGatewayHttps','MaintGatewayJwt','MaintGatewayUnified',
        'MaintGatewayTester','MaintGatewayTesterHttps','MaintGatewayTesterJwt','MaintGatewayUnifiedTester',
        'WpfHighStressSimulator','WpfStressSimulator','WpfETicketClientSimulator','WpfSMSSimulator','WpfSMSWebApiServer','WpfHSMSimulator'
    )

    foreach ($procName in $processNames) {
        $procs = Get-Process -Name $procName -ErrorAction SilentlyContinue
        if (-not $procs) { continue }

        foreach ($proc in $procs) {
            $procPath = $null
            try { $procPath = $proc.Path } catch {}
            $configDir = $null
            $subFolder = $null

            if ($procPath) {
                $configDir = Split-Path -Parent $procPath
                # Detect simulator subfolder from published path (e.g. ...\WpfHighStressSimulator\Fisc\WpfHighStressSimulator.exe)
                if ($procName -in @('WpfHighStressSimulator','WpfStressSimulator','WpfETicketClientSimulator')) {
                    $parts = $procPath -split '\\'
                    for ($i = $parts.Length - 2; $i -ge 0; $i--) {
                        if ($parts[$i] -eq $procName -and $i -gt 0) {
                            $subFolder = $parts[$i - 1]
                            if ($subFolder -in @('win-x64','publish','Debug','Release','net10.0','net10.0-windows')) { $subFolder = $null }
                            break
                        }
                    }
                }
            }

            # Resolve appsettings.json path
            $jsonPath = $null
            if ($configDir) {
                $candidate = Join-Path $configDir 'appsettings.json'
                if (Test-Path $candidate) { $jsonPath = $candidate }
            }
            if (-not $jsonPath) {
                $jsonPath = Join-Path $baseDir "src\CyberCOLA\$procName\appsettings.json"
            }

            if (-not (Test-Path $jsonPath)) { continue }
            $json = Read-CyberCOLAJsonWithComments $jsonPath
            if (-not $json) { continue }

            $displayService = if ($subFolder) { "$procName-$subFolder" } else { $procName }

            # Extract listen ports
            $listenPorts = @()

            # Additional section/key extraction for Auth/GW services
            if ($procName -match '^(CyberAuth|CyberGW)') {
                if ($json.ColaServiceConfig) {
                    $listenPorts += ConvertTo-CyberCOLAPortList -Value $json.ColaServiceConfig.'cola-service-bind-port'
                    $listenPorts += ConvertTo-CyberCOLAPortList -Value $json.ColaServiceConfig.'cola-service-target-port'
                }
                if ($json.ServerServiceConfig) { $listenPorts += ConvertTo-CyberCOLAPortList -Value $json.ServerServiceConfig.'server-service-bind-port' }
                if ($json.ClientAuthRouterConfig) { $listenPorts += ConvertTo-CyberCOLAPortList -Value $json.ClientAuthRouterConfig.'clientauth-router-service-listener-port' }
                if ($json.IDVerifyServerServiceConfig) { $listenPorts += ConvertTo-CyberCOLAPortList -Value $json.IDVerifyServerServiceConfig.'server-service-listen-port' }
                if ($json.QRCodeServerServiceConfig) { $listenPorts += ConvertTo-CyberCOLAPortList -Value $json.QRCodeServerServiceConfig.'server-service-listen-port' }
                if ($json.ClientMonitorConfig) { $listenPorts += ConvertTo-CyberCOLAPortList -Value $json.ClientMonitorConfig.'client-monitor-service-local-port' }
            }

            if ($procName -eq 'ServiceRouter') {
                $xmlPath = $json.GeneralConfig.'service-router-info-xml-file-path'
                if (-not $xmlPath -or -not (Test-Path $xmlPath)) { $xmlPath = Join-Path $baseDir 'src\CyberCOLA\Config\ServiceRouterInfo.xml' }
                if (Test-Path $xmlPath) {
                    try {
                        [xml]$xml = Get-Content $xmlPath
                        $listenPorts += ConvertTo-CyberCOLAPortList -Value $xml.ServiceRouterInfo.ServerServiceInfo.ListenerPort
                        $listenPorts += ConvertTo-CyberCOLAPortList -Value $xml.ServiceRouterInfo.ClientServiceInfo.ListenerPort
                    } catch {}
                }
            }

            # MaintGateway variants
            if ($procName -match '^MaintGateway') {
                if ($json.Kestrel -and $json.Kestrel.Endpoints) {
                    foreach ($epName in $json.Kestrel.Endpoints.PSObject.Properties.Name) {
                        $url = $json.Kestrel.Endpoints.$epName.Url
                        if ($url -match ':(\d+)$') { $listenPorts += [int]$Matches[1] }
                    }
                }
                if ($json.ConnectionSettings) {
                    $listenPorts += ConvertTo-CyberCOLAPortList -Value $json.ConnectionSettings.LocalServer.LocalPortNum
                }
            }

            # Simulators
            if ($procName -in @('WpfHighStressSimulator','WpfStressSimulator','WpfETicketClientSimulator') -and $json.GeneralConfig) {
                $listenPorts += ConvertTo-CyberCOLAPortList -Value $json.GeneralConfig.ListenPortList
                $listenPorts += ConvertTo-CyberCOLAPortList -Value $json.GeneralConfig.ICASHETicketLocalPort
                $listenPorts += ConvertTo-CyberCOLAPortList -Value $json.GeneralConfig.IPASSETicketLocalPort
                $listenPorts += ConvertTo-CyberCOLAPortList -Value $json.GeneralConfig.SVCSETicketLocalPort
            }

            if ($procName -eq 'WpfSMSSimulator' -and $json.GeneralConfig) {
                $xmlPath = $json.GeneralConfig.'first-bank-sms-server-info-xml-file-path'
                if (-not $xmlPath -or -not (Test-Path $xmlPath)) { $xmlPath = Join-Path $baseDir 'src\CyberCOLA\Config\SMSServerInfo.xml' }
                if (Test-Path $xmlPath) {
                    try {
                        [xml]$smsXml = Get-Content $xmlPath
                        foreach ($s in $smsXml.SMSServerInfo.SMSServers.SMSServer) {
                            $listenPorts += ConvertTo-CyberCOLAPortList -Value $s.ListenerPort
                        }
                    } catch {}
                }
            }

            if ($procName -eq 'WpfSMSWebApiServer' -and $json.GeneralConfig) {
                if ($json.Kestrel -and $json.Kestrel.Endpoints) {
                    foreach ($epName in $json.Kestrel.Endpoints.PSObject.Properties.Name) {
                        $url = $json.Kestrel.Endpoints.$epName.Url
                        if ($url -match ':(\d+)$') { $listenPorts += [int]$Matches[1] }
                    }
                }
            }

            # Deduplicate and emit only ports that are actually in use on this machine.
            $seen = @{}
            foreach ($p in ($listenPorts | Where-Object { $_ -gt 0 })) {
                if ($seen.ContainsKey($p)) { continue }
                if (-not $activeLocalPorts.ContainsKey($p)) { continue }
                $seen[$p] = $true
                $endpoints.Add([PSCustomObject]@{
                    ProcessName = $procName
                    ProcessId   = $proc.Id
                    Service     = $displayService
                    Endpoint    = "Local:$p"
                    Port        = $p
                    Type        = 'Listen'
                })
            }
        }
    }

    return $endpoints.ToArray()
}

# ---------------------------------------------------------------------------
# Fast TCP table (P/Invoke Win32 API + netstat fallback)
# ---------------------------------------------------------------------------

function Get-CyberCOLAFastTCPTable {
    # Try high-performance P/Invoke Win32 API
    try {
        # Type renamed when LocalAddress was added: Add-Type cannot redefine a type already loaded
        # in the session, so a session holding the old FastTCPTableHelper still gets the new shape.
        if (-not ([System.Management.Automation.PSTypeName]'FastTCPTableHelper2').Type) {
            $csharpSource = @"
using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public class FastTCPTableHelper2 {
    public enum TCP_TABLE_CLASS {
        TCP_TABLE_BASIC_LISTENER,
        TCP_TABLE_BASIC_CONNECTIONS,
        TCP_TABLE_BASIC_ALL,
        TCP_TABLE_OWNER_PID_LISTENER,
        TCP_TABLE_OWNER_PID_CONNECTIONS,
        TCP_TABLE_OWNER_PID_ALL,
        TCP_TABLE_OWNER_MODULE_LISTENER,
        TCP_TABLE_OWNER_MODULE_CONNECTIONS,
        TCP_TABLE_OWNER_MODULE_ALL
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_TCPROW_OWNER_PID {
        public uint state;
        public uint localAddr;
        public byte localPort1;
        public byte localPort2;
        public byte localPort3;
        public byte localPort4;
        public uint remoteAddr;
        public byte remotePort1;
        public byte remotePort2;
        public byte remotePort3;
        public byte remotePort4;
        public uint owningPid;

        public ushort LocalPort {
            get { return (ushort)((localPort1 << 8) + localPort2); }
        }
        public ushort RemotePort {
            get { return (ushort)((remotePort1 << 8) + remotePort2); }
        }
    }

    [DllImport("iphlpapi.dll", SetLastError = true)]
    public static extern uint GetExtendedTcpTable(
        IntPtr pTcpTable,
        ref int pdwSize,
        bool bOrder,
        uint ulAf,
        TCP_TABLE_CLASS TableClass,
        uint Reserved
    );

    public class TcpRow {
        public string LocalAddress { get; set; }
        public int LocalPort { get; set; }
        public string RemoteAddress { get; set; }
        public int RemotePort { get; set; }
        public string State { get; set; }
        public int OwningProcess { get; set; }
    }

    public static TcpRow[] GetTcpTable() {
        int AF_INET = 2;
        int size = 0;
        uint ret = GetExtendedTcpTable(IntPtr.Zero, ref size, true, (uint)AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);
        IntPtr buffer = Marshal.AllocHGlobal(size);
        try {
            ret = GetExtendedTcpTable(buffer, ref size, true, (uint)AF_INET, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);
            if (ret != 0) return new TcpRow[0];

            int numEntries = Marshal.ReadInt32(buffer);
            IntPtr rowPtr = (IntPtr)((long)buffer + 4);
            List<TcpRow> list = new List<TcpRow>();
            int structSize = Marshal.SizeOf(typeof(MIB_TCPROW_OWNER_PID));

            for (int i = 0; i < numEntries; i++) {
                MIB_TCPROW_OWNER_PID row = (MIB_TCPROW_OWNER_PID)Marshal.PtrToStructure(rowPtr, typeof(MIB_TCPROW_OWNER_PID));
                rowPtr = (IntPtr)((long)rowPtr + structSize);

                string remoteAddr = new System.Net.IPAddress(row.remoteAddr).ToString();
                string stateStr = "Unknown";
                switch (row.state) {
                    case 1: stateStr = "Closed"; break;
                    case 2: stateStr = "Listen"; break;
                    case 3: stateStr = "SynSent"; break;
                    case 4: stateStr = "SynReceived"; break;
                    case 5: stateStr = "Established"; break;
                    case 6: stateStr = "FinWait1"; break;
                    case 7: stateStr = "FinWait2"; break;
                    case 8: stateStr = "CloseWait"; break;
                    case 9: stateStr = "Closing"; break;
                    case 10: stateStr = "LastAck"; break;
                    case 11: stateStr = "TimeWait"; break;
                }

                list.Add(new TcpRow {
                    LocalAddress = new System.Net.IPAddress(row.localAddr).ToString(),
                    LocalPort = row.LocalPort,
                    RemoteAddress = remoteAddr,
                    RemotePort = row.RemotePort,
                    State = stateStr,
                    OwningProcess = (int)row.owningPid
                });
            }
            return list.ToArray();
        } finally {
            Marshal.FreeHGlobal(buffer);
        }
    }
}
"@
            Add-Type -TypeDefinition $csharpSource -ErrorAction Stop
        }
        return [FastTCPTableHelper2]::GetTcpTable()
    } catch {
        Write-Warning "FastTCPTableHelper failed, using Get-NetTCPConnection fallback - $($_.Exception.Message)"
        return Get-NetTCPConnection -ErrorAction Stop |
            Select-Object @{N='LocalAddress'; E={"$($_.LocalAddress)"}},
                          @{N='LocalPort'; E={$_.LocalPort}},
                          @{N='RemoteAddress'; E={if ($_.RemoteAddress) { $_.RemoteAddress.IPAddressToString } else { [string]::Empty }}},
                          @{N='RemotePort'; E={$_.RemotePort}},
                          @{N='State'; E={$_.State.ToString()}},
                          @{N='OwningProcess'; E={$_.OwningProcess}}
    }
}

# ---------------------------------------------------------------------------
# NLog.config patching
# ---------------------------------------------------------------------------

function Resolve-CyberCOLALogCategory {
    param([string]$ProjName)
    switch -Regex ($ProjName) {
        'FISC'  { return 'FISC' }
        'NCCC'  { return 'NCCC' }
        'ICASH' { return 'ICASH' }
        'IPASS' { return 'IPASS' }
        'SVCS'  { return 'SVCS' }
        default { return $null }
    }
}

function Patch-CyberCOLANLog {
    param(
        [Parameter(Mandatory)]
        [string]$OutputDir,

        [Parameter(Mandatory)]
        [string]$ProjName,

        [Parameter(Mandatory)]
        [string]$Runtime,

        [string]$SubFolder = $null,
        [string]$BaseDir = (Get-CyberCOLABaseDir),
        [string]$LinuxDeployBasePath = "/opt/CyberCOLA"
    )

    $nlogPath = Join-Path $OutputDir 'NLog.config'
    $srcNlog = Join-Path $BaseDir "src\CyberCOLA\$ProjName\NLog.config"

    if (-not (Test-Path $nlogPath) -and (Test-Path $srcNlog)) {
        Copy-Item $srcNlog $nlogPath -Force | Out-Null
    }

    $logRoot = if ($Runtime -eq 'linux-x64') { "$($LinuxDeployBasePath.TrimEnd('/'))/LogFiles" } else { (Get-CyberCOLALogRoot).Replace('/', '\') }
    $category = Resolve-CyberCOLALogCategory -ProjName $ProjName
    $targetLogDir = if ($category) { "$logRoot\$category\$ProjName" } else { "$logRoot\$ProjName" }
    if ($SubFolder) { $targetLogDir = Join-Path $targetLogDir $SubFolder }
    if ($Runtime -eq 'linux-x64') { $targetLogDir = $targetLogDir -replace '\\', '/' }

    if (-not (Test-Path $nlogPath)) {
        $defaultXml = @"
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
      autoReload="true"
      throwExceptions="false">
  <variable name="LogDirectory" value="$targetLogDir\`${shortdate}" />
  <targets>
    <target name="Console" xsi:type="Console" />
    <target name="File" xsi:type="File" fileName="`${LogDirectory}\Process.log" layout="`${longdate} [`${level:uppercase=true}] `${message} `${exception:format=tostring}" />
  </targets>
  <rules>
    <logger name="*" minlevel="Info" writeTo="Console,File" />
  </rules>
</nlog>
"@
        if ($Runtime -eq 'linux-x64') { $defaultXml = $defaultXml -replace '\\', '/' }
        [System.IO.File]::WriteAllText($nlogPath, $defaultXml)
        Write-Host "    [NLOG CREATED] Created default NLog.config with LogDirectory: $targetLogDir" -ForegroundColor Cyan
    } else {
        $lines = Get-Content $nlogPath
        $newLines = New-Object System.Collections.Generic.List[string]
        $patched = $false
        $normalizedLogRoot = Get-CyberCOLALogRoot
        # Known placeholder some NLog.config files use literally; the actual dev-machine LogFiles
        # root (e.g. E:/FirstBank/Claude/CyberCOLA_Net10/LogFiles), which is what most checked-in
        # NLog.config files actually contain; and /var/log, a previously-used Linux target root that
        # an already-published file may still have from before $logRoot was made configurable via
        # -LinuxDeployBasePath. All need recognizing as "old" so re-running this against a file
        # already patched to a different target (Linux or Windows) still converges on $logRoot.
        $devLogRootForward = $normalizedLogRoot -replace '\\', '/'
        $devLogRootBack = $normalizedLogRoot -replace '/', '\'
        $knownLogRoots = @('C:/LogFiles', 'C:\LogFiles', $devLogRootForward, $devLogRootBack, '/var/log', $logRoot)
        # A clone at another location still has the original root's LogFiles in its NLog.config files until
        # Set-CyberCOLARepoRoot.ps1 is run; recognise that root as "old" too.
        $configuredRoot = Get-CyberCOLAConfiguredRepoRoot -BaseDir $BaseDir
        if ($configuredRoot) {
            $configuredLogRoot = Join-Path $configuredRoot 'LogFiles'
            $knownLogRoots += @(($configuredLogRoot -replace '\\', '/'), ($configuredLogRoot -replace '/', '\'))
        }
        $knownLogRoots = $knownLogRoots | Select-Object -Unique

        foreach ($line in $lines) {
            if ($line -match 'internalLogFile="([^"]+)"') {
                # NLog's own internal diagnostic log (not app output) - defaults to a Windows temp path
                # like c:\temp\nlog-internal.log in checked-in NLog.config files. Rewritten to live next
                # to the deployed app on Linux, under $LinuxDeployBasePath (the same base as
                # Convert-CyberCOLAAppSettingsPathsForLinux uses for appsettings.json's Config paths).
                $oldVal = $Matches[1]
                $newVal = if ($Runtime -eq 'linux-x64') {
                    "$($LinuxDeployBasePath.TrimEnd('/'))/nlog-internal.log"
                } else {
                    $oldVal
                }
                if ($oldVal -ne $newVal) {
                    $newLine = $line -replace [regex]::Escape("internalLogFile=`"$oldVal`""), "internalLogFile=`"$newVal`""
                    $null = $newLines.Add($newLine)
                    $patched = $true
                } else {
                    $null = $newLines.Add($line)
                }
            } elseif ($line -match '<variable\s+name="LogDirectory"\s+value="([^"]+)"') {
                $oldVal = $Matches[1]
                if ($Runtime -eq 'linux-x64') {
                    $newVal = $oldVal
                    foreach ($root in $knownLogRoots) { $newVal = $newVal -replace [regex]::Escape($root), $logRoot }
                    $newVal = $newVal -replace '\\', '/'
                    $newVal = $newVal -replace '//', '/'
                } else {
                    $newVal = $oldVal
                    foreach ($root in $knownLogRoots) { $newVal = $newVal -replace [regex]::Escape($root), $normalizedLogRoot }
                    $newVal = $newVal -replace '/', '\'
                    $newVal = $newVal -replace '\\\\', '\'
                    if ($oldVal -like '*Log/*' -or $oldVal -like '*Log\*') {
                        $newVal = "$targetLogDir\`${shortdate}"
                    }
                }

                if ($oldVal -ne $newVal) {
                    $newLine = $line -replace [regex]::Escape($oldVal), $newVal
                    $null = $newLines.Add($newLine)
                    $patched = $true
                } else {
                    $null = $newLines.Add($line)
                }
            } else {
                $matchedRoot = $knownLogRoots | Where-Object { $line -like "*$_*" } | Select-Object -First 1
                if ($Runtime -eq 'linux-x64' -and $matchedRoot) {
                    $newLine = $line -replace [regex]::Escape($matchedRoot), $logRoot
                    $newLine = $newLine -replace '\\', '/'
                    $newLine = $newLine -replace '//', '/'
                    $null = $newLines.Add($newLine)
                    $patched = $true
                } elseif ($Runtime -ne 'linux-x64' -and $matchedRoot) {
                    $newLine = $line -replace [regex]::Escape($matchedRoot), $normalizedLogRoot
                    $newLine = $newLine -replace '/', '\'
                    $newLine = $newLine -replace '\\\\', '\'
                    if ($newLine -match '\\>$') { $newLine = $newLine -replace '\\>$', '/>' }
                    $null = $newLines.Add($newLine)
                    $patched = $true
                } else {
                    $null = $newLines.Add($line)
                }
            }
        }

        if ($patched) {
            [System.IO.File]::WriteAllLines($nlogPath, $newLines)
            Write-Host "    [NLOG PATCHED] LogDirectory value set to: $targetLogDir" -ForegroundColor Green
        }
    }
}

# ---------------------------------------------------------------------------
# Fatal log reader
# ---------------------------------------------------------------------------

function Get-CyberCOLAFatalMessages {
    param(
        [string[]]$IncludePattern = @('*'),
        [string]$ExcludePattern = $null,
        [int]$TailLines = 5
    )

    $fatalDir = Get-CyberCOLAFatalDir
    $todayDir = Join-Path $fatalDir (Get-Date -Format 'yyyy-MM-dd')
    if (-not (Test-Path $todayDir)) {
        Write-Host 'No Fatal logs today.' -ForegroundColor Green
        return
    }

    # IncludePattern may be one or more wildcard patterns (OR'd together), e.g. @('Wpf*', 'Web*').
    $files = Get-ChildItem $todayDir -File |
        Where-Object { $name = $_.Name; ($IncludePattern | Where-Object { $name -like $_ }).Count -gt 0 }
    if ($ExcludePattern) {
        $files = $files | Where-Object { $_.Name -notlike $ExcludePattern }
    }

    if (-not $files) {
        Write-Host 'No matching fatal logs today.' -ForegroundColor Green
        return
    }

    $files | ForEach-Object {
        Write-Host "`n>>> $($_.Name) <<<" -ForegroundColor Yellow
        Get-Content $_.FullName -Tail $TailLines
    }
}

# ---------------------------------------------------------------------------
# Status display helpers
# ---------------------------------------------------------------------------

function Show-CyberCOLAProcessStatus {
    param(
        [Parameter(Mandatory)]
        [string]$Title,

        [string[]]$Names = @('Cyber*', 'ServiceRouter')
    )

    Write-Host "`n--- $Title ---" -ForegroundColor Cyan
    $allProcs = Get-Process -Name $Names -ErrorAction SilentlyContinue
    if (-not $allProcs) {
        Write-Host 'No matching processes running.' -ForegroundColor Yellow
        return
    }

    $allProcs | Select-Object @{N='Name'; E={$_.ProcessName}}, Id, `
        @{N='CPU(s)'; E={if ($_.CPU -ne $null) { [math]::Round($_.CPU, 1) } else { 0 }}}, `
        @{N='RAM(MB)'; E={if ($_.WorkingSet -ne $null) { [math]::Round($_.WorkingSet / 1MB, 1) } else { 0 }}} | Format-Table
}

function Show-CyberCOLAServiceStatus {
    param(
        [Parameter(Mandatory)]
        [string]$Title,

        [string[]]$Names = @('Cyber*', 'ServiceRouter')
    )

    Write-Host "`n--- $Title ---" -ForegroundColor Cyan
    $services = Get-Service -Name $Names -ErrorAction SilentlyContinue
    if (-not $services) {
        Write-Host 'No matching services installed.' -ForegroundColor Yellow
        return
    }

    foreach ($svc in $services) {
        $color = if ($svc.Status -eq 'Running') { 'Green' } elseif ($svc.Status -eq 'Stopped') { 'Red' } else { 'Yellow' }
        Write-Host ('{0,-30} : {1}' -f $svc.Name, $svc.Status) -ForegroundColor $color
    }
}
