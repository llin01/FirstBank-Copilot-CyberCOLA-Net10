<#
    Rewrites the repository root written into the config files (appsettings.json, NLog.config, *.xml, *.config)
    after the repository has been cloned or moved to another folder, so services and simulators run from the
    new location. The checked-in files carry the original developer root (E:\FirstBank\Claude\CyberCOLA_Net10);
    every form of it is replaced: E:\...\..., E:\\...\\... (JSON) and E:/.../... (NLog).

    Default is a dry run that lists what would change. -Apply writes the files, keeping each file's encoding
    (BOM or not) and line endings. Tracked files under src/ and publish/Config/ are always covered;
    -IncludePublished also rewrites the untracked publish\win-x64 output, which republishing would fix too.

    The changes are local to this clone: commit them only if the team's standard location changes.
    Windows only (the configs use Windows paths; Linux deployments get their paths from a linux-x64 publish).

    Usage:
      pwsh tools\Set-CyberCOLARepoRoot.ps1                       (dry run: old root -> this clone)
      pwsh tools\Set-CyberCOLARepoRoot.ps1 -Apply
      pwsh tools\Set-CyberCOLARepoRoot.ps1 -Apply -IncludePublished
      pwsh tools\Set-CyberCOLARepoRoot.ps1 -OldRoot 'D:\Work\CyberCOLA' -NewRoot 'E:\FirstBank\Claude\CyberCOLA_Net10'

    Exit codes: 0 = done (or nothing to change), 1 = not run (not Windows, no git, roots invalid or identical).
#>
param(
    # Root currently written in the config files; detected from them when omitted.
    [string]$OldRoot,
    # Root to write; defaults to this clone (the folder above tools\).
    [string]$NewRoot,
    [switch]$Apply,
    [switch]$IncludePublished
)

if ($PSVersionTable.PSEdition -eq 'Desktop') {
    Write-Warning "PowerShell 7 (pwsh.exe) is required."
    exit 1
}
if (-not $IsWindows) {
    Write-Host "Windows only: Linux deployments get their paths from a linux-x64 publish." -ForegroundColor Yellow
    exit 1
}

. "$PSScriptRoot\CyberCOLA-Common.ps1"
$BaseDir = Get-CyberCOLABaseDir

if (-not $NewRoot) { $NewRoot = $BaseDir }
if (-not $OldRoot) { $OldRoot = Get-CyberCOLAConfiguredRepoRoot -BaseDir $BaseDir }
if (-not $OldRoot) {
    Write-Host "No repository root found in src\CyberCOLA\*\appsettings.json; pass -OldRoot." -ForegroundColor Red
    exit 1
}
$OldRoot = $OldRoot.TrimEnd('\', '/')
$NewRoot = (Resolve-Path -LiteralPath $NewRoot -ErrorAction SilentlyContinue)?.Path ?? $NewRoot
$NewRoot = $NewRoot.TrimEnd('\', '/')
if ($OldRoot -match '[";]' -or $NewRoot -match '[";]') {
    Write-Host "Roots must not contain quotes or semicolons." -ForegroundColor Red
    exit 1
}

Write-Host "Old root : $OldRoot"
Write-Host "New root : $NewRoot"
if ($OldRoot -eq $NewRoot) {
    Write-Host "The config files already point at this location; nothing to change." -ForegroundColor Green
    exit 0
}

# The three spellings of a root found in these files, each mapped to the same spelling of the new root.
$forms = @(
    @{ Old = $OldRoot.Replace('\', '\\'); New = $NewRoot.Replace('\', '\\') },  # JSON-escaped, before the plain form
    @{ Old = $OldRoot; New = $NewRoot },
    @{ Old = $OldRoot.Replace('\', '/'); New = $NewRoot.Replace('\', '/') }
)

$tracked = @(git -C $BaseDir ls-files -- 'src/*.json' 'src/*.config' 'src/*.xml' 'publish/Config/*' 2>$null)
if ($LASTEXITCODE -ne 0) {
    Write-Host "git is not available or $BaseDir is not a git repository." -ForegroundColor Red
    exit 1
}
$files = $tracked | Where-Object { $_ -match '\.(json|config|xml)$' } | ForEach-Object { Join-Path $BaseDir $_ }
if ($IncludePublished) {
    $files += Get-ChildItem (Join-Path $BaseDir 'publish\win-x64') -Recurse -File -Include *.json, *.config, *.xml -ErrorAction SilentlyContinue |
        ForEach-Object FullName
}

$changedFiles = 0
$replacements = 0
foreach ($file in $files) {
    $bytes = [IO.File]::ReadAllBytes($file)
    $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    $offset = if ($hasBom) { 3 } else { 0 }
    $text = [Text.UTF8Encoding]::new($false).GetString($bytes, $offset, $bytes.Length - $offset)

    $count = 0
    foreach ($f in $forms) {
        # Case-insensitive: Windows paths, and files mix E:\ and e:\ spellings. The lookahead keeps a longer
        # sibling folder (e.g. CyberCOLA_Net10_old) from matching as a prefix.
        $pattern = [regex]::Escape($f.Old) + '(?=[\\/"''<;]|$)'
        $count += [regex]::Matches($text, $pattern, 'IgnoreCase').Count
        $text = [regex]::Replace($text, $pattern, $f.New.Replace('$', '$$'), 'IgnoreCase')
    }
    if ($count -eq 0) { continue }

    $changedFiles++
    $replacements += $count
    Write-Host ("  {0,3}  {1}" -f $count, [IO.Path]::GetRelativePath($BaseDir, $file))
    if ($Apply) {
        [IO.File]::WriteAllText($file, $text, [Text.UTF8Encoding]::new($hasBom))
    }
}

$verb = if ($Apply) { 'Rewrote' } else { 'Would rewrite' }
Write-Host "$verb $replacements occurrence(s) in $changedFiles file(s)." -ForegroundColor $(if ($Apply) { 'Green' } else { 'Cyan' })
if (-not $Apply -and $changedFiles -gt 0) {
    Write-Host "Dry run only. Add -Apply to write the files (and -IncludePublished for publish\win-x64)." -ForegroundColor Yellow
}
if ($Apply -and $changedFiles -gt 0) {
    Write-Host "These changes are for this clone; do not commit them unless the team's standard location changes." -ForegroundColor Yellow
}
exit 0
