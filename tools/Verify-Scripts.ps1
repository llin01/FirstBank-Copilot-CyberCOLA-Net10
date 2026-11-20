# E:\FirstBank\Copilot\CyberCOLA_Net10\tools\Verify-Scripts.ps1
# Script to verify syntax of all PowerShell scripts in tools/

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
if (-not $scriptDir) { $scriptDir = "." }

# Dynamically enumerate every *.ps1 in this directory instead of a hardcoded file list,
# so coverage never silently falls behind as scripts are added/renamed.
$files = Get-ChildItem -Path $scriptDir -Filter "*.ps1" -File | Sort-Object Name | ForEach-Object { $_.Name }

$hasError = $false

foreach ($f in $files) {
    $filePath = Join-Path $scriptDir $f
    if (-not (Test-Path $filePath)) {
        Write-Host "File not found: $filePath" -ForegroundColor Red
        continue
    }

    $errors = $null
    $tokens = $null
    [System.Management.Automation.Language.Parser]::ParseFile($filePath, [ref]$tokens, [ref]$errors) | Out-Null

    if ($errors) {
        Write-Host "$f : ERR" -ForegroundColor Red
        $hasError = $true
        foreach ($e in $errors) {
            Write-Host "  Line $($e.Extent.StartLineNumber): $($e.Message)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "$f : OK" -ForegroundColor Green
    }
}

if ($hasError) {
    exit 1
} else {
    exit 0
}
