# Get-ProjectDependency.ps1
# CyberCOLA .NET 10 Project Reference Dependency Visualizer

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$ScriptDir = $PSScriptRoot
. "$ScriptDir\CyberCOLA-Common.ps1"
$BaseDir = Get-CyberCOLABaseDir

Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host "                CyberCOLA Project Dependency Visualizer" -ForegroundColor Cyan
Write-Host "======================================================================" -ForegroundColor Cyan
Write-Host "Scanning all project files in solution..." -ForegroundColor DarkGray

# 1. Scan all .csproj files
# Performance: prune build-output and non-source directories (bin/obj/publish/etc.) *before*
# descending into them, instead of enumerating the whole tree and filtering afterward -- on this
# repo (with a populated publish\ and many bin/obj folders under src\), that cuts the scan from
# ~35s to ~2s. .csproj files only ever live under src\ and libs\.
function Find-CyberCOLACsprojFiles {
    param([string]$Root)
    $excludeDirs = @('bin', 'obj', 'publish', '.git', '.vs', 'Backup', 'LogFiles', 'BenchmarkDotNet.Artifacts', 'DLL', 'node_modules')
    $results = [System.Collections.Generic.List[System.IO.FileInfo]]::new()
    $stack = [System.Collections.Generic.Stack[string]]::new()
    $stack.Push($Root)
    while ($stack.Count -gt 0) {
        $dir = $stack.Pop()
        try {
            foreach ($f in [System.IO.Directory]::EnumerateFiles($dir, '*.csproj')) {
                $results.Add([System.IO.FileInfo]::new($f))
            }
            foreach ($sub in [System.IO.Directory]::EnumerateDirectories($dir)) {
                if ($excludeDirs -notcontains (Split-Path $sub -Leaf)) {
                    $stack.Push($sub)
                }
            }
        } catch {}
    }
    return $results
}

$searchRoots = @('src', 'libs') | ForEach-Object { Join-Path $BaseDir $_ } | Where-Object { Test-Path $_ }
$projects = @($searchRoots | ForEach-Object { Find-CyberCOLACsprojFiles -Root $_ })

$depDict = @{}
$projPathMap = @{}

foreach ($p in $projects) {
    $projName = $p.BaseName
    $projPathMap[$projName] = $p.FullName
    $depDict[$projName] = @()
    
    try {
        # Parse XML project file safely
        [xml]$xml = Get-Content $p.FullName -Raw
        $refs = $xml.Project.ItemGroup.ProjectReference
        if ($refs) {
            foreach ($ref in $refs) {
                # Extract target project filename
                $includePath = $ref.Include
                if ($includePath) {
                    $refName = [System.IO.Path]::GetFileNameWithoutExtension($includePath)
                    $depDict[$projName] += $refName
                }
            }
        }
    } catch {
        Write-Warning "Failed to parse project: $($p.FullName) - $($_.Exception.Message)"
    }
}

Write-Host "Found $($projects.Count) projects in the repository.`n" -ForegroundColor Green

# 2. Recursive function to print Dependency Tree
function Show-DependencyTree {
    param(
        [string]$Name,
        [string]$Indent = "",
        [System.Collections.Generic.HashSet[string]]$Visited = (New-Object 'System.Collections.Generic.HashSet[string]')
    )
    
    $deps = $depDict[$Name]
    if (-not $deps -or $deps.Count -eq 0) { return }
    
    if ($Visited.Contains($Name)) {
        Write-Host "$Indent  └─ [Circular Reference Warning: $Name]" -ForegroundColor Red
        return
    }
    
    $Visited.Add($Name) | Out-Null
    
    for ($i = 0; $i -lt $deps.Count; $i++) {
        $dep = $deps[$i]
        $isLast = $i -eq ($deps.Count - 1)
        
        $branch = if ($isLast) { "└─ " } else { "├─ " }
        $nextIndent = $Indent + $(if ($isLast) { "     " } else { "│    " })
        
        # Color code: Core libraries (TM, Yetty, CommonLib) are highlighted in Green
        $color = if ($dep -in @("CommonLib", "Yetty", "TM", "Interchange", "CyberUtils")) { "Green" } else { "White" }
        
        Write-Host "$Indent$branch" -NoNewline
        Write-Host $dep -ForegroundColor $color
        
        # Recurse down
        Show-DependencyTree -Name $dep -Indent $nextIndent -Visited $Visited
    }
    
    $Visited.Remove($Name) | Out-Null
}

# Print visual trees for major entry points
$EntryPoints = @("CyberAuthFISC", "CyberGWFISC", "CyberAuthNCCC", "CyberGWNCCC", "ServiceRouter", "WpfClientSimulator")

Write-Host "--- Hierarchical Dependency Tree (Core Entry Points) ---" -ForegroundColor Yellow
foreach ($entry in $EntryPoints) {
    if ($depDict.ContainsKey($entry)) {
        Write-Host "[+] $entry" -ForegroundColor Cyan
        Show-DependencyTree -Name $entry -Indent "  "
        Write-Host ""
    }
}

# 3. Generate Mermaid Graph
$graphPath = Join-Path $BaseDir "LogFiles\DependencyGraph.md"
Write-Host "Generating Mermaid Dependency Graph Markdown..." -ForegroundColor DarkGray

$mermaid = @"
# CyberCOLA Project Dependency Architecture Graph

```mermaid
graph TD
    classDef core fill:#d4edda,stroke:#28a745,stroke-width:2px;
    classDef entry fill:#cce5ff,stroke:#004085,stroke-width:2px;

    %% Projects definitions
"@

# Define project relations in mermaid format
$relations = @()
$nodeDefs = @()
foreach ($key in $depDict.Keys) {
    # Check if this node is reference or has references
    $deps = $depDict[$key]
    
    # Stylize nodes
    if ($key -in @("CommonLib", "Yetty", "TM", "Interchange", "CyberUtils")) {
        $nodeDefs += "    $key[$key]:::core"
    } elseif ($key -in $EntryPoints) {
        $nodeDefs += "    $key[$key]:::entry"
    }
    
    foreach ($dep in $deps) {
        $relations += "    $key --> $dep"
    }
}

$mermaid += "`n" + ($nodeDefs | Sort-Object | Out-String)
$mermaid += "`n" + ($relations | Sort-Object | Out-String)
$mermaid += "````n"

# Create LogFiles directory if missing
$logFilesDir = Join-Path $BaseDir "LogFiles"
if (-not (Test-Path $logFilesDir)) {
    New-Item -ItemType Directory -Path $logFilesDir | Out-Null
}

[System.IO.File]::WriteAllText($graphPath, $mermaid)

Write-Host "Mermaid graph generated successfully at:" -ForegroundColor Green
Write-Host "  $graphPath" -ForegroundColor White
Write-Host "======================================================================" -ForegroundColor Cyan
