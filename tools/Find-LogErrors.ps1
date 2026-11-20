<#
.SYNOPSIS
    Analyzes build or runtime log files to locate and output clickable source links to the original code locations.
.DESCRIPTION
    This script searches for compiler errors or runtime stack trace exceptions inside log files.
    If no path is provided, it automatically finds the most recently updated log files in the workspace.
#>

param (
    [string]$Path
)

. "$PSScriptRoot\CyberCOLA-Common.ps1"

# English comment only: Define regex patterns for build errors and runtime stack trace frames
$buildPattern = [regex]"^(.*?)\((\d+),(\d+)\):\s+(error|warning)\s+([A-Z0-9]+):\s+(.*)$"
$netStackPattern = [regex]"in\s+(.*?\.cs):line\s+(\d+)"
$pyStackPattern = [regex]"File\s+`"(.*?\.py)`",\s+line\s+(\d+)"
$nodeStackPattern = [regex]"\((.*?\.[\w]+):(\d+)(?::\d+)?\)"

# English comment only: Resolve workspace base directory
$BaseDir = Get-CyberCOLABaseDir

# English comment only: Auto-detect latest log file if Path is not specified
if (-not $Path) {
    Write-Host "No log path specified. Searching for the most recently modified log files..." -ForegroundColor Gray
    $logFiles = Get-ChildItem -Path $BaseDir -Recurse -File -Include "*.log", "*.txt" -ErrorAction SilentlyContinue | 
                Where-Object { $_.LastWriteTime -gt (Get-Date).AddDays(-1) } | 
                Sort-Object LastWriteTime -Descending
    
    if (-not $logFiles) {
        Write-Host "[WARNING] No modified log files found in the last 24 hours." -ForegroundColor Yellow
        Write-Host "Please specify a log file path: .\tools\Find-LogErrors.ps1 <path_to_log>" -ForegroundColor Gray
        return
    }
    
    $Path = $logFiles[0].FullName
    Write-Host "Found most recent log: $Path" -ForegroundColor Cyan
}

if (-not (Test-Path $Path)) {
    Write-Error "Log file path does not exist: $Path"
    return
}

# English comment only: Load log file content
$lines = Get-Content -Path $Path -ErrorAction SilentlyContinue
if (-not $lines) {
    Write-Host "Log file is empty or cannot be read: $Path" -ForegroundColor Yellow
    return
}

Write-Host "`n================================================================================" -ForegroundColor Cyan
Write-Host "                           Log Error & Code Mapper" -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "Target Log: $Path" -ForegroundColor Gray
Write-Host ("-" * 80) -ForegroundColor Gray

$errorCount = 0

foreach ($line in $lines) {
    $lineTrimmed = $line.Trim()
    
    # 1. Check Build Error
    $match = $buildPattern.Match($lineTrimmed)
    if ($match.Success) {
        $errorCount++
        $filePath = $match.Groups[1].Value.Trim()
        $lineNum = $match.Groups[2].Value
        $colNum = $match.Groups[3].Value
        $severity = $match.Groups[4].Value.ToUpper()
        $code = $match.Groups[5].Value
        $msg = $match.Groups[6].Value
        
        $fileName = Split-Path $filePath -Leaf
        $absPath = (Resolve-Path $filePath -ErrorAction SilentlyContinue).Path
        if (-not $absPath) { $absPath = $filePath }
        $absPath = $absPath.Replace('\', '/')
        
        $link = "[${fileName}:L${lineNum}](file:///${absPath}#L${lineNum})"
        
        Write-Host "$errorCount. [$severity] ${code}: $msg" -ForegroundColor Red
        Write-Host "   Code Location: $link (Col: $colNum)" -ForegroundColor White
        Write-Host ("-" * 80) -ForegroundColor Gray
        continue
    }
    
    # 2. Check Runtime Stack Trace (.NET)
    $match = $netStackPattern.Match($lineTrimmed)
    if ($match.Success) {
        $errorCount++
        $filePath = $match.Groups[1].Value.Trim()
        $lineNum = $match.Groups[2].Value
        
        $fileName = Split-Path $filePath -Leaf
        $absPath = (Resolve-Path $filePath -ErrorAction SilentlyContinue).Path
        if (-not $absPath) { $absPath = $filePath }
        $absPath = $absPath.Replace('\', '/')
        
        $link = "[${fileName}:L${lineNum}](file:///${absPath}#L${lineNum})"
        
        Write-Host "$errorCount. [RUNTIME EXCEPTION]" -ForegroundColor DarkRed
        Write-Host "   Context: $lineTrimmed" -ForegroundColor DarkGray
        Write-Host "   Code Location: $link" -ForegroundColor White
        Write-Host ("-" * 80) -ForegroundColor Gray
        continue
    }

    # 3. Check Runtime Stack Trace (Python)
    $match = $pyStackPattern.Match($lineTrimmed)
    if ($match.Success) {
        $errorCount++
        $filePath = $match.Groups[1].Value.Trim()
        $lineNum = $match.Groups[2].Value
        
        $fileName = Split-Path $filePath -Leaf
        $absPath = (Resolve-Path $filePath -ErrorAction SilentlyContinue).Path
        if (-not $absPath) { $absPath = $filePath }
        $absPath = $absPath.Replace('\', '/')
        
        $link = "[${fileName}:L${lineNum}](file:///${absPath}#L${lineNum})"
        
        Write-Host "$errorCount. [PYTHON RUNTIME ERROR]" -ForegroundColor DarkRed
        Write-Host "   Context: $lineTrimmed" -ForegroundColor DarkGray
        Write-Host "   Code Location: $link" -ForegroundColor White
        Write-Host ("-" * 80) -ForegroundColor Gray
        continue
    }
}

if ($errorCount -eq 0) {
    Write-Host "No compiler errors or runtime exceptions detected in this log." -ForegroundColor Green
} else {
    Write-Host "Total detected issues: $errorCount" -ForegroundColor Cyan
}
Write-Host "================================================================================" -ForegroundColor Cyan
