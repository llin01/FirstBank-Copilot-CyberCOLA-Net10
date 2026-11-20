<#
.SYNOPSIS
    匯出／匯入 Claude Code 的 user scope MCP server 設定，搬到另一台 Windows 電腦。

.DESCRIPTION
    Export（舊電腦）：讀取 %USERPROFILE%\.claude.json 最上層的 mcpServers，寫成一個 JSON 檔。
      - 只匯出 MCP 設定，不碰 .claude.json 其他內容（登入狀態、OAuth token、專案紀錄）
      - headers / env 中不是 ${變數} 形式的值會列出警告（只列名稱，不印值），請先換成 ${變數} 再帶走

    Import（新電腦）：逐一以 `claude mcp add-json --scope user` 加入。
      - 同名 server 已存在時略過；加 -Force 則先移除再加入
      - 檢查設定中引用的 ${變數} 是否已設定，以及 npx 類 server 需要的 Node.js
      - claude.ai connectors（Gmail、Drive 等）跟著帳號走，不在此檔中，也不需搬

    Windows PowerShell 5.1 與 PowerShell 7 皆可執行。

.PARAMETER Mode
    Export 或 Import。

.PARAMETER Path
    Export：輸出檔（預設 E:\Export\claude-mcp-servers_yyyyMMdd_HHmm.json）。
    Import：要匯入的檔案（必要）。

.PARAMETER Force
    Import 用：同名 server 已存在時先移除再加入。

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\Move-ClaudeMcpServers.ps1 -Mode Export
    舊電腦：匯出到 E:\Export。

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\Move-ClaudeMcpServers.ps1 -Mode Import -Path D:\claude-mcp-servers_20261118_1500.json
    新電腦：匯入，已存在的 server 略過。

.NOTES
    Exit code：0 完成；1 沒有執行（找不到 claude、設定檔或輸入檔）；2 有 server 加入失敗。
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Export', 'Import')]
    [string]$Mode,

    [string]$Path,

    [switch]$Force
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$ClaudeJson = Join-Path $env:USERPROFILE '.claude.json'
$EnvRefPattern = '\$\{(\w+)(:-[^}]*)?\}'

function Get-Prop($obj, [string]$name) {
    # StrictMode-safe property lookup on PSCustomObject
    if ($null -ne $obj -and $obj.PSObject.Properties[$name]) { return $obj.PSObject.Properties[$name].Value }
    return $null
}

function Invoke-Claude {
    # Windows PowerShell 5.1 turns native stderr into a terminating error under 'Stop'; rely on $LASTEXITCODE instead
    $ErrorActionPreference = 'Continue'
    & claude @args 2>&1 | ForEach-Object { "$_" }
}

function Invoke-ClaudeAddJson([string]$name, [string]$json) {
    # PowerShell's native argument passing mangles a JSON argument that has both quotes and spaces (5.1 and
    # pwsh < 7.3 differ), so build the command line ourselves with CommandLineToArgvW quoting rules:
    # escape quotes, and double any backslashes that precede a quote or the closing quote.
    $quoted = '"' + (($json -replace '(\\*)"', '$1$1\"') -replace '(\\+)$', '$1$1') + '"'
    $psi = New-Object Diagnostics.ProcessStartInfo
    $psi.FileName = (Get-Command claude).Source
    $psi.Arguments = "mcp add-json --scope user `"$name`" $quoted"
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $proc = [Diagnostics.Process]::Start($psi)
    $stderr = $proc.StandardError.ReadToEndAsync()
    $stdout = $proc.StandardOutput.ReadToEnd()
    $proc.WaitForExit()
    return [pscustomobject]@{ ExitCode = $proc.ExitCode; Output = ($stdout + $stderr.Result).Trim() }
}

function Get-UserMcpServersJson([string]$file) {
    # ConvertFrom-Json cannot parse the whole .claude.json: its "projects" map can hold keys that differ only
    # in case (D:/x vs d:/x). Extract just the top-level mcpServers with a case-sensitive parser.
    $text = [IO.File]::ReadAllText($file)
    if ($PSVersionTable.PSVersion.Major -ge 7) {
        $root = [Text.Json.JsonDocument]::Parse($text).RootElement
        $el = [Text.Json.JsonElement]::new()
        if ($root.TryGetProperty('mcpServers', [ref]$el)) { return $el.GetRawText() }
        return $null
    }
    Add-Type -AssemblyName System.Web.Extensions
    $ser = New-Object Web.Script.Serialization.JavaScriptSerializer
    $ser.MaxJsonLength = [int]::MaxValue
    $d = $ser.DeserializeObject($text)
    if ($d.ContainsKey('mcpServers')) { return $ser.Serialize($d['mcpServers']) }
    return $null
}

if ($Mode -eq 'Export') {
    if (-not (Test-Path $ClaudeJson)) { Write-Host "找不到 $ClaudeJson" -ForegroundColor Red; exit 1 }
    $raw = Get-UserMcpServersJson $ClaudeJson
    $servers = if ($raw) { $raw | ConvertFrom-Json } else { $null }
    if ($null -eq $servers -or @($servers.PSObject.Properties).Count -eq 0) {
        Write-Host '沒有 user scope 的 MCP server 可匯出' -ForegroundColor Yellow; exit 1
    }

    if (-not $Path) { $Path = "E:\Export\claude-mcp-servers_$(Get-Date -Format yyyyMMdd_HHmm).json" }
    $dir = Split-Path $Path -Parent
    if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }

    foreach ($p in $servers.PSObject.Properties) {
        Write-Host "  $($p.Name)" -NoNewline
        Write-Host "  ($(if (Get-Prop $p.Value 'url') { Get-Prop $p.Value 'url' } else { Get-Prop $p.Value 'command' }))" -ForegroundColor Gray
        # Values that are not pure ${VAR} references may be literal secrets; report names only, never values
        foreach ($section in 'headers', 'env') {
            $kv = Get-Prop $p.Value $section
            if ($null -eq $kv) { continue }
            foreach ($e in $kv.PSObject.Properties) {
                if ([string]$e.Value -notmatch $EnvRefPattern) {
                    Write-Host "    ⚠ $section.$($e.Name) 不是 `${變數}，可能是明文 secret，帶走前請先改掉" -ForegroundColor Yellow
                }
            }
        }
    }

    $servers | ConvertTo-Json -Depth 20 | Set-Content -Path $Path -Encoding UTF8
    Write-Host "`n已匯出 $(@($servers.PSObject.Properties).Count) 個 server → $Path" -ForegroundColor Green
    Write-Host '新電腦匯入：powershell -ExecutionPolicy Bypass -File .\Move-ClaudeMcpServers.ps1 -Mode Import -Path <檔案>' -ForegroundColor Gray
    exit 0
}

# ---------- Import ----------
if (-not $Path -or -not (Test-Path $Path)) { Write-Host "找不到輸入檔：$Path（請用 -Path 指定）" -ForegroundColor Red; exit 1 }
if (-not (Get-Command claude -ErrorAction SilentlyContinue)) { Write-Host '找不到 claude，請先安裝 Claude Code' -ForegroundColor Red; exit 1 }

$servers = Get-Content $Path -Raw -Encoding UTF8 | ConvertFrom-Json
$failed = 0
$envRefs = @{}
$needsNode = $false

foreach ($p in $servers.PSObject.Properties) {
    $name = $p.Name
    $json = $p.Value | ConvertTo-Json -Depth 20 -Compress
    foreach ($m in [regex]::Matches($json, $EnvRefPattern)) { $envRefs[$m.Groups[1].Value] = $true }
    if ($json -match '"npx"') { $needsNode = $true }

    Invoke-Claude mcp get $name | Out-Null
    if ($LASTEXITCODE -eq 0) {
        if (-not $Force) { Write-Host "  略過 $name（已存在，加 -Force 取代）" -ForegroundColor Gray; continue }
        Invoke-Claude mcp remove $name -s user | Out-Null
    }

    $r = Invoke-ClaudeAddJson $name $json
    if ($r.ExitCode -eq 0) { Write-Host "  ✔ $name" -ForegroundColor Green }
    else { Write-Host "  ✘ $name：$($r.Output)" -ForegroundColor Red; $failed++ }
}

Write-Host ''
foreach ($v in $envRefs.Keys) {
    $set = [Environment]::GetEnvironmentVariable($v, 'User') -or [Environment]::GetEnvironmentVariable($v, 'Machine')
    if ($set) { Write-Host "  ✔ 環境變數 $v 已設定" -ForegroundColor Green }
    else { Write-Host "  ⚠ 環境變數 $v 未設定 → [Environment]::SetEnvironmentVariable('$v', '<值>', 'User')" -ForegroundColor Yellow }
}
if ($needsNode -and -not (Get-Command npx -ErrorAction SilentlyContinue)) {
    Write-Host '  ⚠ 有 npx 類 server 但找不到 Node.js → winget install OpenJS.NodeJS.LTS' -ForegroundColor Yellow
}
Write-Host "`n完成。設定環境變數後請完全關閉 terminal / VS Code 再重開，然後執行 claude mcp list 確認。" -ForegroundColor Green
exit $(if ($failed) { 2 } else { 0 })
