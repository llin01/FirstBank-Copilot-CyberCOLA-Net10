<#
    Read-only check of every published CyberCOLA build against the current source: which services are up to date,
    which are stale and why, which were never published, and the publish command for the stale ones.

    How a build is traced: dotnet publish stamps the commit into each DLL's ProductVersion (1.0.0+<sha>). For every
    folder under publish\<runtime>, the files changed between that commit and HEAD (plus uncommitted changes) are
    listed for the service's project and every project it references, transitively, and the build props. Status:
      up to date    nothing changed
      stale-code    the service's own code changed (.cs/.csproj/.razor/.xaml)
      stale-shared  only referenced projects (ServiceProvider, CommonLib, libs, ...) or the build props changed
      stale-config  only the service's appsettings.json / NLog.config / data files changed
    File times and DLL hashes are not used: git rewrites times, and every commit changes every DLL.

    Usage:
      pwsh tools\Test-CyberCOLAPublishState.ps1
      pwsh tools\Test-CyberCOLAPublishState.ps1 -Exclude WpfHSMSimulator -Detail
      pwsh tools\Test-CyberCOLAPublishState.ps1 -Runtime linux-x64

    Exit codes: 0 = everything published and up to date, 1 = not run (no git, no publish folder),
                2 = at least one service is stale or never published.
#>
param(
    [ValidateSet('win-x64', 'linux-x64')]
    [string]$Runtime = 'win-x64',
    # Services to leave out of the publish commands (still listed, marked as excluded), e.g. a build kept on purpose.
    [string[]]$Exclude = @(),
    # List every changed file per stale service instead of the first three.
    [switch]$Detail
)

if ($PSVersionTable.PSEdition -eq 'Desktop') {
    Write-Warning "PowerShell 7 (pwsh.exe) is required."
    exit 1
}

. "$PSScriptRoot\CyberCOLA-Common.ps1"
$BaseDir = Get-CyberCOLABaseDir
$publishDir = Join-Path $BaseDir "publish\$Runtime"
$Exclude = @($Exclude | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })

$head = git -C $BaseDir rev-parse --short HEAD 2>$null
if ($LASTEXITCODE -ne 0) { Write-Host "$BaseDir is not a git repository." -ForegroundColor Red; exit 1 }
if (-not (Test-Path $publishDir)) { Write-Host "No publish folder: $publishDir" -ForegroundColor Red; exit 1 }

# The projects Publish-CyberCOLAServices.ps1 publishes, read from its own lists so they never drift apart.
function Get-PublishableProjects {
    $ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot 'Publish-CyberCOLAServices.ps1'), [ref]$null, [ref]$null)
    $ast.FindAll({ param($n) $n -is [Management.Automation.Language.AssignmentStatementAst] -and $n.Left.Extent.Text -in '$WebAPIProjects', '$OtherProjects' }, $true) |
        ForEach-Object { $_.Right.FindAll({ param($n) $n -is [Management.Automation.Language.StringConstantExpressionAst] }, $true) } |
        ForEach-Object Value | Select-Object -Unique
}

# Repo-relative folders of a project and everything it references, transitively.
$refCache = @{}
function Get-ProjectDirs([string]$csproj) {
    $full = [IO.Path]::GetFullPath($csproj)
    if ($refCache.ContainsKey($full)) { return $refCache[$full] }
    $refCache[$full] = @()   # guards against reference cycles
    $dirs = [Collections.Generic.HashSet[string]]::new()
    [void]$dirs.Add([IO.Path]::GetRelativePath($BaseDir, (Split-Path $full)).Replace('\', '/'))
    [xml]$xml = Get-Content -LiteralPath $full -Raw
    foreach ($r in $xml.SelectNodes('//ProjectReference')) {
        $child = [IO.Path]::GetFullPath((Join-Path (Split-Path $full) $r.Include))
        if (Test-Path -LiteralPath $child) { foreach ($d in Get-ProjectDirs $child) { [void]$dirs.Add($d) } }
    }
    $refCache[$full] = @($dirs)
    return $refCache[$full]
}

# One git diff per distinct published commit (a handful) instead of one per service, filtered by path below.
$diffCache = @{}
function Get-ChangedSince([string]$sha) {
    if (-not $diffCache.ContainsKey($sha)) { $diffCache[$sha] = @(git -C $BaseDir diff --name-only $sha HEAD 2>$null) }
    return $diffCache[$sha]
}
$uncommitted = @(git -C $BaseDir status --porcelain | ForEach-Object { $_.Substring(3).Trim('"') })

# Services the authorization tests start (Test-AuthorizationSystem.ps1).
$authTestPattern = '^(CyberGW|CyberAuth|ServiceRouter$|ConsoleStressSimulator$|WpfETicketClientSimulator$|WpfHSMSimulator$)'

$rows = [Collections.Generic.List[object]]::new()
foreach ($dir in Get-ChildItem $publishDir -Directory) {
    $svc = $dir.Name
    $row = [pscustomobject]@{ Service = $svc; Commit = '-'; Published = '-'; Status = ''; Changed = @(); AuthTest = $svc -match $authTestPattern }
    $rows.Add($row)
    $dll = Join-Path $dir.FullName "$svc.dll"
    if (-not (Test-Path $dll)) { $row.Status = 'no main DLL'; continue }
    $row.Published = (Get-Item $dll).LastWriteTime.ToString('MM-dd HH:mm')
    $sha = ([Diagnostics.FileVersionInfo]::GetVersionInfo($dll).ProductVersion -split '\+')[1]
    if (-not $sha) { $row.Status = 'no commit stamp'; continue }
    $row.Commit = $sha.Substring(0, [Math]::Min(7, $sha.Length))
    $proj = Join-Path $BaseDir "src\CyberCOLA\$svc\$svc.csproj"
    if (-not (Test-Path $proj)) { $row.Status = 'project not found'; continue }

    $prefixes = @(Get-ProjectDirs $proj | ForEach-Object { "$_/" })
    $props = 'Directory.Build.props', 'Directory.Packages.props'
    $inScope = { param($f) $f -in $props -or @($prefixes | Where-Object { $f.StartsWith($_, [StringComparison]::OrdinalIgnoreCase) }).Count }
    $changed = @(Get-ChangedSince $sha) + $uncommitted |
        Where-Object { $_ -and $_ -notmatch '\.(md|txt)$' -and $_ -notmatch '\.Test/' -and (& $inScope $_) } | Select-Object -Unique
    $row.Changed = @($changed)
    $own = "src/CyberCOLA/$svc/"
    $row.Status = if (-not $changed) { 'up to date' }
                  elseif (@($changed | Where-Object { $_.StartsWith($own) -and $_ -match '\.(cs|csproj|razor|xaml)$' })) { 'stale-code' }
                  elseif (@($changed | Where-Object { -not $_.StartsWith($own) })) { 'stale-shared' }
                  else { 'stale-config' }
}

$published = @($rows.Service)
foreach ($p in Get-PublishableProjects) {
    if ($p -notin $published) { $rows.Add([pscustomobject]@{ Service = $p; Commit = '-'; Published = '-'; Status = 'never published'; Changed = @(); AuthTest = $p -match $authTestPattern }) }
}

# --- Report ----------------------------------------------------------------------------------------------------------
$dirty = @(git -C $BaseDir status --porcelain -- src libs)
Write-Host "HEAD $head, runtime $Runtime, $($rows.Count) service(s)" -ForegroundColor Cyan
if ($dirty.Count) { Write-Host "Working tree has $($dirty.Count) uncommitted change(s) in src/libs: commit before publishing (the publish script stops otherwise)." -ForegroundColor Yellow }

$table = $rows | Sort-Object @{ e = { -not $_.AuthTest } }, Service | ForEach-Object {
    $files = if (-not $_.Changed.Count) { '' }
             elseif ($Detail) { "$($_.Changed.Count): " + ($_.Changed -join ', ') }
             else { "$($_.Changed.Count): " + (($_.Changed | Select-Object -First 3) -join ', ') + $(if ($_.Changed.Count -gt 3) { ', ...' } else { '' }) }
    [pscustomobject]@{
        Group = if ($_.AuthTest) { 'auth-test' } else { 'other' }
        Service = $_.Service + $(if ($_.Service -in $Exclude) { ' (excluded)' } else { '' })
        Commit = $_.Commit; Published = $_.Published; Status = $_.Status
        Changed = $files -replace 'src/CyberCOLA/', '' -replace 'libs/', 'libs:'
    }
}
$table | Format-Table -AutoSize -Wrap | Out-String -Width 250 | ForEach-Object { $_.TrimEnd() } | Write-Host

$counts = $rows | Group-Object Status | Sort-Object Name | ForEach-Object { "$($_.Name) $($_.Count)" }
Write-Host ("Summary: " + ($counts -join ', '))

$toPublish = $rows | Where-Object { $_.Status -ne 'up to date' -and $_.Status -notin 'no main DLL', 'project not found' -and $_.Service -notin $Exclude }
foreach ($group in @(@{ Name = 'auth-test services'; Auth = $true }, @{ Name = 'other services'; Auth = $false })) {
    $names = @($toPublish | Where-Object { $_.AuthTest -eq $group.Auth } | Sort-Object Service | ForEach-Object Service)
    if ($names.Count) {
        Write-Host "`nPublish $($group.Name) ($($names.Count)):" -ForegroundColor Cyan
        Write-Host "  pwsh -NoProfile -File tools\Publish-CyberCOLAServices.ps1 -SingleProjectName $($names -join ',') -TargetRuntime $Runtime"
    }
}

exit $(if (@($rows | Where-Object { $_.Status -ne 'up to date' -and $_.Service -notin $Exclude }).Count) { 2 } else { 0 })
