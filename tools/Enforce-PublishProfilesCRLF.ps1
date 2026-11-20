$ScriptDir = $PSScriptRoot
$BaseDir = (Get-Item (Join-Path $ScriptDir "..")).FullName
$webProjects = Get-ChildItem -Path (Join-Path $BaseDir "src\CyberCOLA") -Directory -Filter "Web*"

$pubxmlLines = [string[]]@(
    '<?xml version="1.0" encoding="utf-8"?>',
    '<!--',
    'https://go.microsoft.com/fwlink/?LinkID=208121.',
    '-->',
    '<Project>',
    '  <PropertyGroup>',
    '    <Configuration>Release</Configuration>',
    '    <Platform>x64</Platform>',
    '    <PublishDir>bin\Release\net10.0\publish\</PublishDir>',
    '    <PublishProtocol>FileSystem</PublishProtocol>',
    '    <_TargetId>Folder</_TargetId>',
    '    <TargetFramework>net10.0</TargetFramework>',
    '    <SelfContained>false</SelfContained>',
    '    <PublishSingleFile>false</PublishSingleFile>',
    '    <PublishReadyToRun>false</PublishReadyToRun>',
    '  </PropertyGroup>',
    '</Project>'
)

$pubxmlUserLines = [string[]]@(
    '<?xml version="1.0" encoding="utf-8"?>',
    '<!--',
    'https://go.microsoft.com/fwlink/?LinkID=208121.',
    '-->',
    '<Project>',
    '  <PropertyGroup>',
    '    <History>True|2026-07-21T08:03:00.0000000Z;</History>',
    '    <LastFailureDetails />',
    '  </PropertyGroup>',
    '</Project>'
)

foreach ($proj in $webProjects) {
    $profilesDir = Join-Path $proj.FullName "Properties\PublishProfiles"
    if (-not (Test-Path $profilesDir)) {
        New-Item -ItemType Directory -Path $profilesDir -Force | Out-Null
    }
    
    $pubxmlFile = Join-Path $profilesDir "FolderProfile.pubxml"
    $pubxmlUserFile = Join-Path $profilesDir "FolderProfile.pubxml.user"
    
    [System.IO.File]::WriteAllLines($pubxmlFile, $pubxmlLines, [System.Text.Encoding]::UTF8)
    [System.IO.File]::WriteAllLines($pubxmlUserFile, $pubxmlUserLines, [System.Text.Encoding]::UTF8)
    
    $bytes = [System.IO.File]::ReadAllBytes($pubxmlFile)
    $hasCR = $bytes -contains 13
    $hasLF = $bytes -contains 10
    
    Write-Host "Updated x64 publish profile for $($proj.Name) (CR=$hasCR, LF=$hasLF)" -ForegroundColor Green
}
