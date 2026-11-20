<#
.SYNOPSIS
    Builds runs.json and rc.json for the auth test trend dashboard from the unattended auth test transcripts.

.DESCRIPTION
    Reads every LogFiles\AuthTest\*.txt written by Invoke-AuthTestUnattended.ps1 that printed statistics and writes
    two files in the {rows, meta} shape that a Claude "Data" dashboard reads as attached files:
      runs.json  one row per run: requests, responses, success, failed, unanswered, success rate, traffic
                 parameters and whether the run used the default smoke-test parameters
      rc.json    one row per run and response code: count, share, Success/Failed and description
    meta carries this script's full text, so readers of the dashboard can see how every number was derived.

    Only aggregate counts and response-code descriptions are written. Everyone who can open the dashboard can read
    the uploaded files, so nothing is written when the output contains a 13-19 digit run (card number length) or an
    IPv4 address, and no local path is embedded.

.PARAMETER TranscriptDir
    Folder with the transcripts. Default: LogFiles\AuthTest under the repository root.

.PARAMETER OutDir
    Where runs.json and rc.json are written. Default: LogFiles\AuthTest\dashboard (git-ignored).

.EXAMPLE
    pwsh tools/Export-AuthTestDashboardData.ps1

.NOTES
    Exit code: 0 written; 1 nothing to export (no folder or no transcript with statistics);
    2 refused because the output contained a card-number-length digit run or an IPv4 address.
#>
param(
    [string]$TranscriptDir,
    [string]$OutDir
)
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path $PSScriptRoot -Parent
if (-not $TranscriptDir) { $TranscriptDir = Join-Path $repoRoot 'LogFiles\AuthTest' }
if (-not $OutDir) { $OutDir = Join-Path $TranscriptDir 'dashboard' }
if (-not (Test-Path $TranscriptDir)) { Write-Host "Transcript folder not found: $TranscriptDir" -ForegroundColor Red; exit 1 }

function Get-Int([string]$text, [string]$pattern) {
    $m = [regex]::Match($text, $pattern, 'Multiline')
    if ($m.Success) { return [int]$m.Groups[1].Value }
    return $null
}

$runs = [System.Collections.Generic.List[object]]::new()
$rcRows = [System.Collections.Generic.List[object]]::new()

foreach ($file in Get-ChildItem $TranscriptDir -Filter '*.txt' | Sort-Object Name) {
    $text = [IO.File]::ReadAllText($file.FullName)
    $success = Get-Int $text '^\s*Success\s+:\s+(\d+)'
    if ($null -eq $success) { continue }   # the run never printed statistics

    $network = $file.BaseName.Split('_')[0]
    $group = if ($network -in 'NCCC', 'FISC') { 1 } else { 2 }

    $startMatch = [regex]::Match($text, 'Run start (\d{4}-\d\d-\d\d \d\d:\d\d:\d\d)')
    $start = if ($startMatch.Success) { $startMatch.Groups[1].Value }
             else { [datetime]::ParseExact($file.BaseName.Split('_', 2)[1], 'yyyyMMdd_HHmmss', $null).ToString('yyyy-MM-dd HH:mm:ss') }

    # FISC prints what the GW saw; the other networks print what the simulator sent.
    if ($network -eq 'FISC') {
        $requests = Get-Int $text '^\s*Messages seen by GW\s+:\s+(\d+)'
        $responses = Get-Int $text '^\s*Responses produced\s+:\s+(\d+)'
        $noAnswer = Get-Int $text '^\s*No answer from Auth\s+:\s+(\d+)'
        $dropped = Get-Int $text '^\s*Dropped \(no return channel\)\s+:\s+(\d+)'
        $unanswered = if ($null -ne $noAnswer -or $null -ne $dropped) { [int]$noAnswer + [int]$dropped } else { $null }
    } else {
        $requests = Get-Int $text '^\s*Requests from simulator\s+:\s+(\d+)'
        $responses = Get-Int $text '^\s*Responses to simulator\s+:\s+(\d+)'
        $unanswered = Get-Int $text '^\s*Unanswered requests\s+:\s+(\d+)'
    }
    $failed = Get-Int $text '^\s*Failed\s+:\s+(\d+)'
    $exitCode = Get-Int $text 'Test script exit code: (\d+)'

    # Traffic parameters decide whether the run is a default smoke test or a stress/special run.
    $simCount = $null; $intervalMs = $null; $replaceCard = $null
    $g1 = [regex]::Match($text, '\[Step 6/6\].*?(\d+) items per connection, interval (\d+)ms, replace card (True|False)')
    $g2 = [regex]::Match($text, 'Launching WpfETicketClientSimulator .*? SEND (\d+) (\d+)')
    if ($g1.Success) {
        $simCount = [int]$g1.Groups[1].Value; $intervalMs = [int]$g1.Groups[2].Value; $replaceCard = $g1.Groups[3].Value -eq 'True'
    } elseif ($g2.Success) {
        $simCount = [int]$g2.Groups[1].Value; $intervalMs = [int]$g2.Groups[2].Value
    }
    $smoke = if ($group -eq 1) { $simCount -eq 100 -and $intervalMs -eq 50 -and $replaceCard -eq $true }
             else { $simCount -eq 10 -and $intervalMs -eq 100 }

    $total = if ($responses) { $responses } else { $requests }
    $runs.Add([ordered]@{
        run_id = $file.BaseName; network = $network; group = $group; start = $start
        requests = $requests; responses = $responses; success = $success; failed = $failed; unanswered = $unanswered
        success_rate = if ($total) { [math]::Round(100.0 * $success / $total, 1) } else { $null }
        sim_count = $simCount; interval_ms = $intervalMs; replace_card = $replaceCard; smoke = [bool]$smoke; exit_code = $exitCode
    })

    # Response-code lines look like "05   14   3.5%  Failed   Do not honor (from Auth)"; a later block wins.
    $byRc = [ordered]@{}
    foreach ($m in [regex]::Matches($text, '^\s+([0-9A-Z]{2})\s+(\d+)\s+([\d\.]+)%\s+(Success|Failed)\s+(.+?)\s*$', 'Multiline')) {
        $byRc[$m.Groups[1].Value] = [ordered]@{
            run_id = $file.BaseName; network = $network; start = $start; rc = $m.Groups[1].Value
            count = [int]$m.Groups[2].Value; pct = [double]$m.Groups[3].Value; result = $m.Groups[4].Value
            description = $m.Groups[5].Value
        }
    }
    foreach ($row in $byRc.Values) { $rcRows.Add($row) }
}

if ($runs.Count -eq 0) { Write-Host "No transcript with statistics in $TranscriptDir" -ForegroundColor Yellow; exit 1 }

$self = [IO.File]::ReadAllText($PSCommandPath)
$meta = @(
    [ordered]@{ label = (Split-Path $PSCommandPath -Leaf); value = $self; type = 'code'; lang = 'powershell' }
    [ordered]@{ label = 'Input'; value = 'LogFiles\AuthTest\*.txt (unattended auth test transcripts)' }
)
$runsJson = [ordered]@{ rows = $runs; meta = $meta } | ConvertTo-Json -Depth 6
$rcJson = [ordered]@{ rows = $rcRows; meta = $meta } | ConvertTo-Json -Depth 6

# PCI gate: no 13-19 digit runs (PAN length) and no IPv4 addresses may reach the upload.
foreach ($pair in @(@('runs.json', $runsJson), @('rc.json', $rcJson))) {
    if ($pair[1] -match '(?<!\d)\d{13,19}(?!\d)' -or $pair[1] -match '\b(?:\d{1,3}\.){3}\d{1,3}\b') {
        Write-Host "Refusing to write $($pair[0]): it contains a card-number-length digit run or an IPv4 address." -ForegroundColor Red
        exit 2
    }
}

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
[IO.File]::WriteAllText((Join-Path $OutDir 'runs.json'), $runsJson, [Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText((Join-Path $OutDir 'rc.json'), $rcJson, [Text.UTF8Encoding]::new($false))
Write-Host "runs: $($runs.Count)  rc rows: $($rcRows.Count)  smoke runs: $(@($runs | Where-Object { $_.smoke }).Count)"
Write-Host "Written to $OutDir"
exit 0
