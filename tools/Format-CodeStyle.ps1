# Format-CodeStyle.ps1
# CyberCOLA .NET 10 Solution Code Formatting & Style Cleanup Wrapper

# Auto-restart in pwsh if running under Windows PowerShell 5.x (desktop edition)
if ($PSVersionTable.PSEdition -eq 'Desktop') {
    $pwshCmd = Get-Command pwsh -ErrorAction SilentlyContinue
    if ($pwshCmd) {
        & $pwshCmd.Source -NoProfile -File $MyInvocation.MyCommand.Path @args
        exit $LASTEXITCODE
    } else {
        Write-Warning "PowerShell 7 (pwsh.exe) is required but was not found."
        exit 1
    }
}

$ErrorActionPreference = "Stop"
$ScriptDir = $PSScriptRoot
. "$ScriptDir\CyberCOLA-Common.ps1"
$BaseDir = Get-CyberCOLABaseDir

Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host "                CyberCOLA C# Code Format & Styling" -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host "Executing dotnet format on CyberCOLA.sln..." -ForegroundColor Yellow

$slnPath = Join-Path $BaseDir "CyberCOLA.sln"

try {
    # Run standard .NET SDK formatting tool
    dotnet format $slnPath --verbosity normal
    
    Write-Host "`n[SUCCESS] Code styling & formatting completed successfully." -ForegroundColor Green
} catch {
    Write-Warning "Code formatting encountered an error: $($_.Exception.Message)"
}
Write-Host "======================================================================" -ForegroundColor Cyan
