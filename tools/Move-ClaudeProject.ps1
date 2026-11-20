<#
.SYNOPSIS
    將 Claude Code 專案（含使用者設定與對話紀錄）打包搬移到另一台 Windows 電腦。

.DESCRIPTION
    ==================================================================
     執行方法（舊電腦、新電腦都使用同一支腳本）
    ==================================================================

    【事前準備】只需做一次
      1. 把本腳本放到好找的資料夾，例如 D:\Tools\
      2. 按 Win + X → 選「終端機」或「Windows PowerShell」（一般權限即可）
      3. 切換到腳本所在資料夾，並解除下載封鎖：
           cd D:\Tools
           Unblock-File .\Move-ClaudeProject.ps1

    【方式一：互動選單】最簡單，不用記參數
           powershell -ExecutionPolicy Bypass -File .\Move-ClaudeProject.ps1
         或在檔案總管中，對本檔按右鍵 →「用 PowerShell 執行」
         → 依畫面選擇 1 匯出 / 2 匯入

    【方式二：直接下指令】
      ▶ 舊電腦（匯出）
           powershell -ExecutionPolicy Bypass -File .\Move-ClaudeProject.ps1 -Mode Export -OutputDir D:\
         → 產生 D:\CyberCOLA_Net10_ClaudeMove_yyyyMMdd_HHmm.zip，並複製一份本腳本到 D:\
         → 把輸出資料夾裡的「zip 檔」和「本腳本」一起複製到新電腦

      ▶ 新電腦（匯入，還原到原路徑 E:\FirstBank\Claude\CyberCOLA_Net10）
           powershell -ExecutionPolicy Bypass -File .\Move-ClaudeProject.ps1 -Mode Import -ZipPath D:\Tools\CyberCOLA_Net10_ClaudeMove_xxxx.zip
         ※ zip 與腳本放在同一個資料夾時，可省略 -ZipPath，會自動選用最新的 zip

    【匯入後】
           cd E:\FirstBank\Claude\CyberCOLA_Net10
           dotnet restore
           dotnet build
           pwsh tools\Publish-CyberCOLAServices.ps1   # publish 輸出沒有打包，需重新發行
           claude              # 第一次啟動需登入
           claude --resume     # 選擇舊對話接續（或 claude -c 接續最近一次）
         匯入到不同路徑時，先執行 pwsh tools\Set-CyberCOLARepoRoot.ps1 -Apply 修正設定檔內的路徑

    【常見錯誤】
      - 「已停用指令碼執行」     → 指令前面要加 powershell -ExecutionPolicy Bypass -File
      - 「目標資料夾已存在且非空」→ 確定要覆蓋，指令最後加 -Force（設定檔會先備份）
      - 新電腦沒有 E: 槽         → 先建立 E 槽，或 subst E: C:\EDrive
      - 進專案請一律用大寫 E:\，否則 claude --resume 可能找不到舊對話

    【查看本說明】
           Get-Help .\Move-ClaudeProject.ps1 -Full

    ==================================================================
     搬移內容
    ==================================================================
    Export（舊電腦）：
      1. 專案原始碼（含 .git、CLAUDE.md、.claude\），排除 bin/obj/.vs 等建置產物，
         以及 publish\win-x64、publish\linux-x64、publish\cross-platform（可重新發行）和 LogFiles（交易 log）；
         進版控的 publish\Config 照樣打包
      2. 使用者層級設定 %USERPROFILE%\.claude\（CLAUDE.md、settings.json、commands、agents、skills）
      3. 本專案的對話紀錄 %USERPROFILE%\.claude\projects\<編碼路徑>\
      → 打包成單一 zip，並複製一份本腳本到同一個輸出資料夾
      ※ 不會匯出登入憑證（.credentials.json / .claude.json），新電腦需重新登入

    Import（新電腦）：
      解開 zip → 還原專案 → 還原設定（預設不覆蓋既有檔案）→ 還原對話紀錄
      → 若專案路徑改變，自動修正對話紀錄內的路徑 → 檢查 git / claude / .NET 10 SDK

.PARAMETER Mode
    Export（舊電腦匯出）或 Import（新電腦匯入）。省略時顯示互動選單。

.PARAMETER ZipPath
    Import 用：匯出的 zip 檔路徑。省略時自動找腳本資料夾中最新的 *_ClaudeMove_*.zip。

.PARAMETER Force
    Import 用：允許覆蓋既有專案資料夾與使用者設定（覆蓋前先備份）。

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\Move-ClaudeProject.ps1
    顯示互動選單。

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\Move-ClaudeProject.ps1 -Mode Export -OutputDir D:\
    舊電腦：匯出到 D:\（未指定 -OutputDir 時輸出到 E:\Export，不存在會自動建立）。

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\Move-ClaudeProject.ps1 -Mode Import -ZipPath D:\Tools\CyberCOLA_Net10_ClaudeMove_20261114_1430.zip
    新電腦：匯入到原路徑。
#>
[CmdletBinding()]
param(
    [ValidateSet('Export', 'Import', '')]
    [string]$Mode = '',

    # Export: source project folder; default resolved below (Windows PowerShell 5.1 has no $PSScriptRoot here)
    [string]$ProjectPath,

    # Export: folder for the zip
    [string]$OutputDir = 'E:\Export',

    # Import: zip file
    [string]$ZipPath,

    # Import: restore target (default = original path, which keeps the session history easiest to resume)
    [string]$TargetPath,

    # Import: allow overwriting an existing project folder / user settings (backed up first)
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Default project: the repository this script lives in (tools\..), otherwise the usual location, so a copy of
# the script on a new machine still has a sensible default.
if (-not $ProjectPath) {
    $repo = Split-Path $PSScriptRoot -Parent
    $ProjectPath = if ($repo -and (Test-Path (Join-Path $repo 'CyberCOLA.sln'))) { $repo } else { 'E:\FirstBank\Claude\CyberCOLA_Net10' }
}

# ---------- Shared settings ----------
$ClaudeHome = Join-Path $env:USERPROFILE '.claude'

# Build output / IDE caches are not packed (robocopy /XD matches a folder name at any depth)
$ExcludeDirs = @('bin', 'obj', '.vs', 'node_modules', 'TestResults', 'packages', '.idea')
$ExcludeFiles = @('*.user', '*.suo')

# Folders excluded by path relative to the project: published binaries (git-ignored, rebuilt by
# Publish-CyberCOLAServices.ps1 / Publish-WebProjectsCrossPlatform.ps1) and runtime transaction logs.
# The tracked publish\Config is still packed.
$ExcludeProjectDirs = @('publish\win-x64', 'publish\linux-x64', 'publish\cross-platform', 'LogFiles')

# User-level items to move (deliberately without .credentials.json)
$UserConfigItems = @('CLAUDE.md', 'settings.json', 'commands', 'agents', 'skills', 'output-styles')

function Write-Step([string]$msg) { Write-Host "`n==> $msg" -ForegroundColor Cyan }
function Write-Ok([string]$msg) { Write-Host "    [OK] $msg" -ForegroundColor Green }
function Write-Warn2([string]$msg) { Write-Host "    [!] $msg" -ForegroundColor Yellow }

# Claude Code names a project's history folder after its absolute path with every non-alphanumeric character as '-'
#   E:\FirstBank\Claude\CyberCOLA_Net10 -> E--FirstBank-Claude-CyberCOLA-Net10
function Get-EncodedProjectName([string]$path) {
    $full = [IO.Path]::GetFullPath($path).TrimEnd('\')
    return ($full -replace '[^a-zA-Z0-9]', '-')
}

# robocopy wrapper: only exit codes >= 8 are errors
function Invoke-Robocopy([string]$src, [string]$dst, [string[]]$extraArgs = @()) {
    $rcArgs = @($src, $dst, '/E', '/R:1', '/W:1', '/NFL', '/NDL', '/NJH', '/NJS', '/NP') + $extraArgs
    & robocopy @rcArgs | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy 失敗 (exit $LASTEXITCODE)：$src -> $dst" }
}

function Test-Command([string]$name) { [bool](Get-Command $name -ErrorAction SilentlyContinue) }

# Run an external command ignoring stderr (PS 5.1 can throw on 2>$null under ErrorActionPreference Stop)
function Invoke-Quiet([scriptblock]$cmd) {
    $prev = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try { & $cmd 2>$null } catch { $null } finally { $ErrorActionPreference = $prev }
}

# =====================================================================
#  Export
# =====================================================================
function Invoke-Export {
    if (-not (Test-Path $ProjectPath)) { throw "找不到專案路徑：$ProjectPath" }
    $ProjectPath = [IO.Path]::GetFullPath($ProjectPath).TrimEnd('\')
    $projName = Split-Path $ProjectPath -Leaf
    $ts = Get-Date -Format 'yyyyMMdd_HHmm'

    # --- Git status check (warns only, never blocks) ---
    Write-Step 'Git 狀態檢查'
    if ((Test-Path (Join-Path $ProjectPath '.git')) -and (Test-Command git)) {
        $dirty = git -C $ProjectPath status --porcelain
        if ($dirty) {
            Write-Warn2 '有未 commit 的變更（仍會一併打包，但建議先 commit）：'
            $dirty | Select-Object -First 15 | ForEach-Object { Write-Host "        $_" }
        } else { Write-Ok '工作目錄乾淨' }

        $unpushed = Invoke-Quiet { git -C $ProjectPath log '@{u}..' --oneline }
        if ($unpushed) {
            Write-Warn2 "有 $(@($unpushed).Count) 個 commit 尚未 push"
        }
    } else {
        Write-Warn2 '非 Git 專案或未安裝 git，略過檢查'
    }

    if (-not (Test-Path (Join-Path $ProjectPath 'CLAUDE.md'))) {
        Write-Warn2 '專案沒有 CLAUDE.md。建議搬遷前在 Claude Code 中請它把進度寫進 CLAUDE.md'
    }

    $stage = Join-Path $env:TEMP "ClaudeMove_$ts"
    New-Item -ItemType Directory -Path $stage -Force | Out-Null

    try {
        # --- 1. Project ---
        Write-Step "複製專案：$ProjectPath"
        # robocopy /XD takes full paths for folders that must be excluded at one location only
        $excludeFull = @($ExcludeProjectDirs | ForEach-Object { Join-Path $ProjectPath $_ })
        Invoke-Robocopy $ProjectPath (Join-Path $stage 'project') (@('/XD') + $ExcludeDirs + $excludeFull + @('/XF') + $ExcludeFiles)
        Write-Ok "已排除：$($ExcludeDirs -join ', ')"
        Write-Ok "已排除：$($ExcludeProjectDirs -join ', ')（新電腦需重新發行）"

        # --- 2. User-level settings ---
        Write-Step "複製使用者設定：$ClaudeHome"
        $userStage = Join-Path $stage 'user-claude'
        New-Item -ItemType Directory -Path $userStage -Force | Out-Null
        foreach ($item in $UserConfigItems) {
            $src = Join-Path $ClaudeHome $item
            if (Test-Path $src) {
                Copy-Item $src -Destination $userStage -Recurse -Force
                Write-Ok $item
            }
        }

        # --- 3. Session history ---
        Write-Step '複製對話紀錄'
        $encoded = Get-EncodedProjectName $ProjectPath
        $histSrc = Join-Path $ClaudeHome "projects\$encoded"
        $sessionCount = 0
        if (Test-Path $histSrc) {
            Invoke-Robocopy $histSrc (Join-Path $stage 'history')
            $sessionCount = @(Get-ChildItem $histSrc -Filter *.jsonl -File).Count
            Write-Ok "$sessionCount 個 session（$encoded）"
        } else {
            Write-Warn2 "找不到對話紀錄：$histSrc"
        }

        # --- manifest ---
        [ordered]@{
            ProjectName   = $projName
            ProjectPath   = $ProjectPath
            EncodedName   = $encoded
            SessionCount  = $sessionCount
            ExportedAt    = (Get-Date).ToString('s')
            ExportedFrom  = $env:COMPUTERNAME
            ClaudeVersion = $(if (Test-Command claude) { (Invoke-Quiet { claude --version }) -join ' ' } else { '' })
            DotnetSdks    = $(if (Test-Command dotnet) { (dotnet --list-sdks) -join '; ' } else { '' })
        } | ConvertTo-Json | Set-Content (Join-Path $stage 'manifest.json') -Encoding UTF8

        # --- Pack (tar.exe ships with Windows 11; faster than Compress-Archive and no 2 GB limit) ---
        Write-Step '壓縮中'
        if (-not (Test-Path $OutputDir)) { New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null }
        $zip = Join-Path ([IO.Path]::GetFullPath($OutputDir)) "${projName}_ClaudeMove_$ts.zip"
        & tar.exe -a -c -f $zip -C $stage .
        if ($LASTEXITCODE -ne 0) { throw "tar 壓縮失敗 (exit $LASTEXITCODE)" }

        # Put a copy of this script next to the zip so the output folder alone is enough on the new machine
        # (Import then picks up the newest zip in its own folder without -ZipPath)
        $outDir = Split-Path $zip -Parent
        $scriptCopy = Join-Path $outDir (Split-Path $PSCommandPath -Leaf)
        if ([IO.Path]::GetFullPath($PSCommandPath) -ne [IO.Path]::GetFullPath($scriptCopy)) {
            Copy-Item $PSCommandPath $scriptCopy -Force
        }

        $sizeMB = [math]::Round((Get-Item $zip).Length / 1MB, 1)
        Write-Host "`n完成！輸出檔：$zip（$sizeMB MB）" -ForegroundColor Green
        Write-Host "腳本副本：$scriptCopy" -ForegroundColor Green
        Write-Host "把 $outDir 裡的 zip 和腳本一起帶到新電腦，在該資料夾執行：" -ForegroundColor Gray
        Write-Host "  powershell -ExecutionPolicy Bypass -File .\Move-ClaudeProject.ps1 -Mode Import" -ForegroundColor Gray
    }
    finally {
        Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
    }
}

# =====================================================================
#  Import
# =====================================================================
function Invoke-Import {
    if (-not $ZipPath) {
        # Not given: take the newest export next to the script
        $found = Get-ChildItem $PSScriptRoot -Filter '*_ClaudeMove_*.zip' -File -ErrorAction SilentlyContinue |
                 Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if (-not $found) { throw "找不到 zip 檔，請用 -ZipPath 指定，或把 zip 放到腳本所在資料夾：$PSScriptRoot" }
        $ZipPath = $found.FullName
        Write-Ok "自動選用：$ZipPath"
    }
    if (-not (Test-Path $ZipPath)) { throw "找不到 zip 檔：$ZipPath" }

    $ts = Get-Date -Format 'yyyyMMdd_HHmmss'
    $stage = Join-Path $env:TEMP "ClaudeMoveImport_$ts"
    New-Item -ItemType Directory -Path $stage -Force | Out-Null

    try {
        Write-Step '解壓縮'
        & tar.exe -x -f $ZipPath -C $stage
        if ($LASTEXITCODE -ne 0) { throw "tar 解壓失敗 (exit $LASTEXITCODE)" }

        $manifest = Get-Content (Join-Path $stage 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
        $origPath = $manifest.ProjectPath
        if (-not $TargetPath) { $TargetPath = $origPath }
        $TargetPath = [IO.Path]::GetFullPath($TargetPath).TrimEnd('\')
        Write-Ok "來源：$($manifest.ExportedFrom)  $origPath  ($($manifest.ExportedAt))"
        Write-Ok "目標：$TargetPath"

        # --- 1. Project ---
        Write-Step '還原專案'
        if ((Test-Path $TargetPath) -and (Get-ChildItem $TargetPath -Force | Select-Object -First 1)) {
            if (-not $Force) { throw "目標資料夾已存在且非空：$TargetPath（要覆蓋請加 -Force）" }
            Write-Warn2 '目標已存在，以 -Force 合併覆蓋'
        }
        Invoke-Robocopy (Join-Path $stage 'project') $TargetPath
        Write-Ok '專案已還原'

        # --- 2. User settings (never overwritten by default; backed up first with -Force) ---
        Write-Step "還原使用者設定到 $ClaudeHome"
        New-Item -ItemType Directory -Path $ClaudeHome -Force | Out-Null
        $userStage = Join-Path $stage 'user-claude'
        $backupDir = Join-Path $ClaudeHome "backup_$ts"
        if (Test-Path $userStage) {
            foreach ($item in Get-ChildItem $userStage -Force) {
                $dst = Join-Path $ClaudeHome $item.Name
                if (Test-Path $dst) {
                    if (-not $Force) { Write-Warn2 "$($item.Name) 已存在，略過（加 -Force 可覆蓋）"; continue }
                    New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
                    Copy-Item $dst -Destination $backupDir -Recurse -Force
                    Remove-Item $dst -Recurse -Force
                    Write-Warn2 "$($item.Name) 已備份至 $backupDir"
                }
                Copy-Item $item.FullName -Destination $ClaudeHome -Recurse -Force
                Write-Ok $item.Name
            }
        }

        # --- 3. Session history ---
        Write-Step '還原對話紀錄'
        $histStage = Join-Path $stage 'history'
        if (Test-Path $histStage) {
            $newEncoded = Get-EncodedProjectName $TargetPath
            $histDst = Join-Path $ClaudeHome "projects\$newEncoded"
            Invoke-Robocopy $histStage $histDst
            Write-Ok "已還原到 projects\$newEncoded"

            # On a new path, rewrite the cwd recorded in the jsonl files (backslashes are \\ in JSON)
            if ($TargetPath -ne $origPath) {
                $oldJson = $origPath.Replace('\', '\\')
                $newJson = $TargetPath.Replace('\', '\\')
                $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
                $fixed = 0
                Get-ChildItem $histDst -Filter *.jsonl -File -Recurse | ForEach-Object {
                    $text = [IO.File]::ReadAllText($_.FullName, $utf8NoBom)
                    if ($text.Contains($oldJson)) {
                        [IO.File]::WriteAllText($_.FullName, $text.Replace($oldJson, $newJson), $utf8NoBom)
                        $fixed++
                    }
                }
                Write-Ok "路徑已變更，修正 $fixed 個 session 檔內的路徑"
            }
        } else {
            Write-Warn2 'zip 內沒有對話紀錄'
        }

        # --- 4. Environment check ---
        Write-Step '環境檢查'
        if (Test-Command git) { Write-Ok "git：$(git --version)" }
        else { Write-Warn2 'git 未安裝 → winget install Git.Git（Claude Code 在 Windows 需要 Git Bash）' }

        if (Test-Command claude) { Write-Ok "claude：$((Invoke-Quiet { claude --version }) -join ' ')" }
        else { Write-Warn2 'Claude Code 未安裝 → irm https://claude.ai/install.ps1 | iex' }

        if (Test-Command dotnet) {
            $sdks = dotnet --list-sdks
            if ($sdks -match '^10\.') { Write-Ok ".NET 10 SDK：$(($sdks -match '^10\.') -join ', ')" }
            else { Write-Warn2 '未找到 .NET 10 SDK → winget install Microsoft.DotNet.SDK.10' }
        } else { Write-Warn2 'dotnet 未安裝 → winget install Microsoft.DotNet.SDK.10' }

        Write-Host "`n完成！接下來：" -ForegroundColor Green
        Write-Host "  cd `"$TargetPath`""
        if ($TargetPath -ne $origPath) {
            Write-Host '  pwsh tools\Set-CyberCOLARepoRoot.ps1 -Apply     # 路徑改變：修正設定檔內寫死的 repo 路徑'
        }
        Write-Host '  dotnet restore'
        Write-Host '  pwsh tools\Publish-CyberCOLAServices.ps1       # publish 輸出沒有打包，需重新發行'
        Write-Host '  claude              # 第一次啟動需登入'
        Write-Host '  claude --resume     # 選擇要接續的舊對話（或 claude -c 接續最近一次）'
        Write-Host '  ※ user scope 的 MCP server 不在 zip 內，請用 tools\Move-ClaudeMcpServers.ps1 匯出／匯入' -ForegroundColor Gray
    }
    finally {
        Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
    }
}

# =====================================================================
#  Interactive menu (when -Mode is not given)
# =====================================================================
function Show-Menu {
    Write-Host ''
    Write-Host '==============================================================' -ForegroundColor Cyan
    Write-Host '  Claude Code 專案搬移工具' -ForegroundColor Cyan
    Write-Host "  專案路徑：$ProjectPath" -ForegroundColor Cyan
    Write-Host '==============================================================' -ForegroundColor Cyan
    Write-Host '  [1] 匯出（在「舊電腦」執行）→ 產生 zip'
    Write-Host '  [2] 匯入（在「新電腦」執行）→ 從 zip 還原'
    Write-Host '  [3] 顯示完整使用說明'
    Write-Host '  [Q] 離開'
    Write-Host ''
    return (Read-Host '請選擇').Trim().ToUpper()
}

# ---------- Entry point ----------
$interactive = -not $Mode
try {
    if ($interactive) {
        $choice = Show-Menu
        switch ($choice) {
            '1' {
                $in = Read-Host "zip 輸出資料夾（直接 Enter = $OutputDir）"
                if ($in) { $OutputDir = $in.Trim('"', ' ') }
                $Mode = 'Export'
            }
            '2' {
                $in = Read-Host 'zip 檔路徑（直接 Enter = 自動找腳本資料夾中最新的 zip）'
                if ($in) { $ZipPath = $in.Trim('"', ' ') }
                $f = Read-Host '若目標資料夾或設定已存在，要覆蓋嗎？(y/N)'
                if ($f -match '^[Yy]') { $Force = [switch]$true }
                $Mode = 'Import'
            }
            '3' { Get-Help $PSCommandPath -Full | Out-Host }
            default { Write-Host '已離開。' }
        }
    }

    switch ($Mode) {
        'Export' { Invoke-Export }
        'Import' { Invoke-Import }
    }
}
catch {
    Write-Host "`n[錯誤] $($_.Exception.Message)" -ForegroundColor Red
    if (-not $interactive) { exit 1 }
}
finally {
    # Keep the window open when started from Explorer ("Run with PowerShell")
    if ($interactive) { Read-Host "`n按 Enter 關閉視窗" | Out-Null }
}
