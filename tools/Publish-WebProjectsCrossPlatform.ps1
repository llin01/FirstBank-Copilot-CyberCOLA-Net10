# Publish all Web* projects cross-platform using FolderProfile.pubxml
param(
    [string]$CustomOutputDir
)

$ErrorActionPreference = "Stop"

$ScriptDir = $PSScriptRoot
$BaseDir = (Get-Item (Join-Path $ScriptDir "..")).FullName
$WebProjects = Get-ChildItem -Path (Join-Path $BaseDir "src\CyberCOLA") -Directory | Where-Object { $_.Name -like "Web*" -or $_.Name -eq "ConsoleStressSimulator" }

$TargetPublishDir = if ($CustomOutputDir) { $CustomOutputDir } else { Join-Path $BaseDir "publish\cross-platform" }

Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host " CyberCOLA .NET 10 Web Projects Cross-Platform Batch Publisher        " -ForegroundColor Cyan
Write-Host " Target Output: $TargetPublishDir                                     " -ForegroundColor Yellow
Write-Host "======================================================================" -ForegroundColor Cyan

if (-not (Test-Path $TargetPublishDir)) {
    New-Item -ItemType Directory -Path $TargetPublishDir -Force | Out-Null
}

$SuccessCount = 0
$FailCount = 0

foreach ($proj in $WebProjects) {
    $projName = $proj.Name
    $csprojPath = Join-Path $proj.FullName "$projName.csproj"
    
    if (-not (Test-Path $csprojPath)) {
        Write-Host "[-] Skipping $projName (csproj not found)" -ForegroundColor Yellow
        continue
    }

    Write-Host "`n[+] Publishing $projName (Cross-Platform)..." -ForegroundColor Green
    
    $outFolder = Join-Path $TargetPublishDir $projName
    
    # Execute dotnet publish using FolderProfile and explicit cross-platform target directory
    $publishCmd = "dotnet publish `"$csprojPath`" -c Release -p:PublishProfile=FolderProfile -o `"$outFolder`""
    Invoke-Expression $publishCmd

    if ($LASTEXITCODE -eq 0) {
        # Verify wwwroot static assets if project has wwwroot source
        $srcWwwroot = Join-Path $proj.FullName "wwwroot"
        $destWwwroot = Join-Path $outFolder "wwwroot"
        if ((Test-Path $srcWwwroot) -and -not (Test-Path $destWwwroot)) {
            Copy-Item -Path $srcWwwroot -Destination $destWwwroot -Recurse -Force
            Write-Host "    -> Replicated wwwroot static assets to published bundle" -ForegroundColor DarkGray
        }

        Write-Host "[OK] $projName published successfully to $outFolder" -ForegroundColor Green
        $SuccessCount++
    } else {
        Write-Host "[FAIL] $projName publish failed!" -ForegroundColor Red
        $FailCount++
    }
}

Write-Host "`n======================================================================" -ForegroundColor Cyan
Write-Host " Publish Summary: $SuccessCount succeeded, $FailCount failed.         " -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan
