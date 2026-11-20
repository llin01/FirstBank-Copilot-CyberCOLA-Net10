param(
    [string[]]$SingleProjectName,
    [string]$TargetRuntime,
    [string]$LinuxDeployBasePath = "/opt/CyberCOLA",
    # GeneralConfig:sql-column-encryption-setting for the DB services. Omitted: keep each service's value from its
    # previous publish (dotnet publish would otherwise reset it to the src default, enabled).
    [ValidateSet('Enabled', 'Disabled')]
    [string]$SqlColumnEncryption
)

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

. "$PSScriptRoot\CyberCOLA-Common.ps1"
$ProgressPreference = 'SilentlyContinue'

# Under "pwsh -File", "-SingleProjectName A B" binds only A: B lands in -LinuxDeployBasePath (a Linux publish would
# deploy to the path "B") and the rest in $args, and the run then quietly publishes A alone. Accept "A,B" (one
# argument under -File) and refuse leftover arguments instead.
if ($args.Count -gt 0 -or ($LinuxDeployBasePath -and -not $LinuxDeployBasePath.StartsWith('/'))) {
    $extra = @($args)
    if (-not $LinuxDeployBasePath.StartsWith('/')) { $extra = @($LinuxDeployBasePath) + $extra }
    Write-Host "[ERROR] Unexpected argument(s): $($extra -join ' ')" -ForegroundColor Red
    Write-Host "        Separate several projects with commas: -SingleProjectName CyberGWNCCC,CyberAuthNCCC" -ForegroundColor Yellow
    Write-Host "        -LinuxDeployBasePath must be an absolute Linux path such as /opt/CyberCOLA." -ForegroundColor Yellow
    exit 1
}
$SingleProjectName = @($SingleProjectName | ForEach-Object { $_ -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })

# --- Define project groups ---
$WebAPIProjects = @(
    "MaintGateway", "MaintGatewayHttps", "MaintGatewayTester", "MaintGatewayTesterHttps", 
    "MaintGatewayJwt", "MaintGatewayTesterJwt", "WpfMaintGatewayTester",
    "MaintGatewayUnified", "MaintGatewayUnifiedTester"
)

$OtherProjects = @(
    "CyberAuthFISC", "CyberAuthICASH", "CyberAuthIPASS", "CyberAuthNCCC", "CyberAuthSVCS",
    "CyberGWFISC", "CyberGWICASH", "CyberGWIPASS", "CyberGWNCCC", "CyberGWSVCS",
    "ServiceRouter", "CyberGWFISCIDVerify", "CyberGWNCCCQRCode", "CyberSMS",
    "ConsoleStressSimulator", "WpfStressSimulator", "WpfETicketClientSimulator", "WpfHighStressSimulator", "WpfHSMSimulator", "WpfEAITCPClient", "WpfSMSSimulator", "WpfSMSWebApiServer"
)

# Services that open SQL connections and read GeneralConfig:sql-column-encryption-setting (CommonLib.SystemConstantData).
# The value is not kept in src appsettings.json on purpose: production must be checked by a DBA before disabling it.
$DbServiceProjects = @(
    "CyberAuthFISC", "CyberAuthICASH", "CyberAuthIPASS", "CyberAuthNCCC", "CyberAuthSVCS",
    "CyberGWFISC", "CyberGWFISCIDVerify", "CyberGWICASH", "CyberGWIPASS", "CyberGWNCCC", "CyberGWNCCCQRCode", "CyberGWSVCS",
    "CyberSMS", "ServiceRouter"
)
$ColumnEncryptionKeyPattern = '("sql-column-encryption-setting"\s*:\s*")([^"]*)(")'

function Get-PublishedColumnEncryption([string]$AppSettingsPath) {
    if (-not (Test-Path $AppSettingsPath)) { return $null }
    $m = [regex]::Match((Get-Content $AppSettingsPath -Raw), $ColumnEncryptionKeyPattern)
    if ($m.Success) { return $m.Groups[2].Value }
    return $null
}

function Set-PublishedColumnEncryption([string]$AppSettingsPath, [string]$Value) {
    $raw = Get-Content $AppSettingsPath -Raw
    if ($raw -match $ColumnEncryptionKeyPattern) {
        $raw = [regex]::Replace($raw, $ColumnEncryptionKeyPattern, { param($m) $m.Groups[1].Value + $Value + $m.Groups[3].Value })
    } elseif ($raw -match '"GeneralConfig"\s*:\s*\{') {
        # Insert as the first GeneralConfig key, matching how it was added by hand (targeted edit keeps comments/format)
        $raw = [regex]::Replace($raw, '("GeneralConfig"\s*:\s*\{)(\r?\n)', { param($m) $m.Groups[1].Value + $m.Groups[2].Value + "    `"sql-column-encryption-setting`": `"$Value`"," + $m.Groups[2].Value })
    } else {
        Write-Host "    [WARNING] No GeneralConfig section; sql-column-encryption-setting not written to $AppSettingsPath" -ForegroundColor Yellow
        return
    }
    Set-Content -Path $AppSettingsPath -Value $raw -Encoding utf8 -NoNewline
}

$SuccessList = New-Object System.Collections.Generic.List[string]
$FailureList = New-Object System.Collections.Generic.List[string]
$ConfigSyncFailed = $false

# 1. Select target platform and projects
if ($SingleProjectName) {
    $TargetProjects = $SingleProjectName
} else {
    Write-Host "Select Target Group to Publish:" -ForegroundColor Cyan
    Write-Host "1. All Projects"
    Write-Host "2. WebAPI Group (MaintGateway*)"
    Write-Host "3. Other Group (Auth, GW, Simulators...)"
    Write-Host "4. Quit"
    $groupChoice = Read-Host "Enter choice [1-4]"

    if ($groupChoice -eq "4") {
        $global:LastOpFailed = $true
        exit 1
    }

    $TargetProjects = switch ($groupChoice) {
        "1" { $WebAPIProjects + $OtherProjects }
        "2" { $WebAPIProjects }
        "3" { $OtherProjects }
        default { $WebAPIProjects + $OtherProjects }
    }
}

if ($TargetRuntime) {
    $Runtime = $TargetRuntime
} else {
    Write-Host "`nSelect Target Platform:" -ForegroundColor Cyan
    Write-Host "1. win-x64 (Windows)"
    Write-Host "2. linux-x64 (Linux/Systemd)"
    Write-Host "3. Quit"
    $choice = Read-Host "Enter choice [1-3]"

    if ($choice -eq "3") {
        $global:LastOpFailed = $true
        exit 1
    }

    $Runtime = if ($choice -eq "1") { "win-x64" } else { "linux-x64" }
}

$BaseDir = Get-CyberCOLABaseDir

# The SDK stamps the current commit into each DLL's ProductVersion (1.0.0+<sha>) but not uncommitted
# changes, so a binary built from a dirty tree cannot be traced back to its source.
$dirty = @(git -C $BaseDir status --porcelain -- src libs 2>$null)
if ($LASTEXITCODE -eq 0 -and $dirty.Count -gt 0) {
    Write-Host "`n[WARNING] src/ or libs/ has $($dirty.Count) uncommitted change(s); the published binaries will not match commit $(git -C $BaseDir rev-parse --short HEAD):" -ForegroundColor Yellow
    $dirty | Select-Object -First 15 | ForEach-Object { Write-Host "    $_" -ForegroundColor Yellow }
    if ($dirty.Count -gt 15) { Write-Host "    ... and $($dirty.Count - 15) more" -ForegroundColor Yellow }
    if (-not (Confirm-CyberCOLA -Prompt "Publish anyway?" -Default 'N')) {
        Write-Host "Publish cancelled. Commit the changes first, then publish again." -ForegroundColor Cyan
        $global:LastOpFailed = $true
        exit 1
    }
}

$PublishRootDir = Join-Path $BaseDir "publish\$Runtime"
$GlobalConfigSrc = Join-Path $BaseDir "src\CyberCOLA\Config"
$GlobalConfigDest = Join-Path $BaseDir "publish\Config"

# --- Step 1: Clean old directories ---
Write-Host "`nStep 1: Cleaning up old publish directory..." -ForegroundColor Cyan
if (Test-Path $PublishRootDir) {
    Write-Host "  - Note: Selective publishing chosen. Only target project folders will be updated." -ForegroundColor Yellow
} else {
    New-Item -ItemType Directory -Path $PublishRootDir -Force | Out-Null
}
Write-Host "  [OK] Ready to publish." -ForegroundColor Green

# --- Step 2: Publish binaries ---
Write-Host "`nStep 2: Publishing binaries for $Runtime..." -ForegroundColor Cyan

foreach ($ProjName in $TargetProjects) {
    # Skip Windows-only WPF projects on non-Windows target runtime
    if ($Runtime -ne "win-x64" -and $ProjName -like "Wpf*") {
        continue
    }

    Write-Host "  - Processing: " -NoNewline
    Write-Host "$ProjName" -ForegroundColor Yellow
    
    $ProjFile = Get-ChildItem -Path (Join-Path $BaseDir "src\CyberCOLA") -Filter "$ProjName.csproj" -Recurse | Select-Object -First 1
    
    if (-not $ProjFile) {
        Write-Host "    [NOT FOUND]" -ForegroundColor Red
        $null = $FailureList.Add($ProjName)
        continue
    }

    $OutputDir = Join-Path $PublishRootDir $ProjName
    
    # Try stopping running processes to avoid file locks
    Write-Host "    - Checking for running process: $ProjName..." -ForegroundColor DarkGray
    $runningProc = Get-Process -Name $ProjName -ErrorAction SilentlyContinue
    if ($runningProc) {
        Write-Host "    - Stopping running process: $ProjName..." -ForegroundColor Yellow
        $stoppedIds = $runningProc | Select-Object -ExpandProperty Id
        Stop-Process -Name $ProjName -Force -ErrorAction SilentlyContinue
        # Wait for the specific stopped PIDs to exit (release file handles) instead of a blind sleep.
        # Waiting on captured PIDs, not re-matching by -Name, avoids blocking on an unrelated
        # process that happens to share this name.
        Wait-Process -Id $stoppedIds -Timeout 5 -ErrorAction SilentlyContinue
    }

    # Remember the previous publish's encryption setting before the folder is wiped
    $ColumnEncryptionValue = $null
    if ($DbServiceProjects -contains $ProjName) {
        $ColumnEncryptionValue = if ($SqlColumnEncryption) { $SqlColumnEncryption } else { Get-PublishedColumnEncryption (Join-Path $OutputDir "appsettings.json") }
    }

    # Clean up specific project old directory
    if (Test-Path $OutputDir) {
        try {
            Write-Host "    - Removing old output directory: $OutputDir..." -ForegroundColor DarkGray
            Remove-Item $OutputDir -Recurse -Force -ErrorAction Stop
            Write-Host "    [REMOVED] Cleaned previous build binaries." -ForegroundColor DarkGray
        } catch {
            Write-Host "    [WARNING] Could not fully clean $OutputDir - Some files might be locked. Error: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }

    dotnet publish $ProjFile.FullName `
        -c Release `
        -r $Runtime `
        --self-contained false `
        -o $OutputDir `
        /p:DebugType=none `
        /p:DebugSymbols=false | Out-Null

    if ($LASTEXITCODE -eq 0) {
        # If it is a Tester project, additionally sync the TestData folder
        if ($ProjName -like "*Tester*") {
            $dataSrc = Join-Path $ProjFile.Directory.FullName "TestData"
            $dataDest = Join-Path $OutputDir "TestData"
            if (Test-Path $dataSrc) {
                if (-not (Test-Path $dataDest)) { New-Item -ItemType Directory -Path $dataDest -Force | Out-Null }
                Copy-Item -Path "$dataSrc\*" -Destination $dataDest -Force -Recurse
                Write-Host "    [TESTDATA SYNCED]" -ForegroundColor Cyan
            }
        }
        Patch-CyberCOLANLog -OutputDir $OutputDir -ProjName $ProjName -Runtime $Runtime
        if ($ColumnEncryptionValue) {
            Set-PublishedColumnEncryption (Join-Path $OutputDir "appsettings.json") $ColumnEncryptionValue
            Write-Host "    [SQL COLUMN ENCRYPTION] $ColumnEncryptionValue$(if (-not $SqlColumnEncryption) { ' (kept from previous publish)' })" -ForegroundColor Cyan
        }
        Write-Host "    [OK]" -ForegroundColor Green
        $null = $SuccessList.Add($ProjName)
    } else {
        Write-Host "    [FAILED]" -ForegroundColor Red
        $null = $FailureList.Add($ProjName)
    }
}

# --- Step 3: Sync global configuration ---
Write-Host "`nStep 3: Syncing Global Configurations (XMLs)..." -ForegroundColor Cyan
if (Test-Path $GlobalConfigSrc) {
    # Create directory
    if (-not (Test-Path $GlobalConfigDest)) { New-Item -ItemType Directory -Path $GlobalConfigDest -Force | Out-Null }
    # Sync using robocopy
    $null = robocopy $GlobalConfigSrc $GlobalConfigDest *.xml *.json /S /NJH /NJS /MT:16 /NP
    # Robocopy exit codes 0-7 mean success (bit flags for copied/extra/mismatched files); 8+ is a failure.
    if ($LASTEXITCODE -ge 8) {
        Write-Host "  [FAILED] robocopy exit code $LASTEXITCODE while syncing to $GlobalConfigDest" -ForegroundColor Red
        $ConfigSyncFailed = $true
    } else {
        Write-Host "  - Configuration XMLs and SocketConfig.json synced to $GlobalConfigDest" -ForegroundColor Green
    }
}

# --- Step 3b: Sync shared Socket config (SocketConfig.json) into each published appsettings.json ---
Write-Host "`nStep 3b: Syncing Socket Configuration (SocketConfig.json)..." -ForegroundColor Cyan
$SocketConfigPath = Join-Path $GlobalConfigSrc "SocketConfig.json"
if (Test-Path $SocketConfigPath) {
    Sync-CyberCOLASocketConfigAll -PublishRootDir $PublishRootDir -SocketConfigPath $SocketConfigPath
} else {
    Write-Host "  - SocketConfig.json not found, skipped." -ForegroundColor Yellow
}

# --- Step 4: Correct published paths (switch from src to publish) ---
Write-Host "`nStep 4: Patching paths in published appsettings.json..." -ForegroundColor Cyan
$OldPath = ($GlobalConfigSrc).Replace("\", "\\")
$NewPath = ($GlobalConfigDest).Replace("\", "\\")
# A clone at another location still carries the original root in its config files until
# Set-CyberCOLARepoRoot.ps1 is run; patch that root's src\...\Config too so the publish output is right anyway.
$ConfiguredRoot = Get-CyberCOLAConfiguredRepoRoot -BaseDir $BaseDir
$OldPaths = @($OldPath)
if ($ConfiguredRoot -and $ConfiguredRoot -ne $BaseDir) {
    Write-Host "  - Config files still point at $ConfiguredRoot (run tools\Set-CyberCOLARepoRoot.ps1 to update them); patching both roots." -ForegroundColor Yellow
    $OldPaths += (Join-Path $ConfiguredRoot "src\CyberCOLA\Config").Replace("\", "\\")
}

$ExcludeKeys = @("initial-folder-path", "InitialDirectory", "CardXmlPrefixFileDirectory")

$PublishedConfigs = Get-ChildItem -Path $PublishRootDir -Filter "appsettings.json" -Recurse
foreach ($cfg in $PublishedConfigs) {
    $lines = Get-Content $cfg.FullName
    $newLines = New-Object System.Collections.Generic.List[string]
    $patched = $false

    # Dynamically generate the original TestData path under src for this Tester project
    $OldDataPath = (Join-Path $BaseDir "src\CyberCOLA\$($cfg.Directory.Name)\TestData").Replace("\", "\\")

    # Get current project's Publish TestData path
    $CurrentProjectDataPath = Join-Path $cfg.Directory.FullName "TestData"
    # Convert to JSON string format double backslash path
    $NewDataPathStr = $CurrentProjectDataPath.Replace("\", "\\")

    foreach ($line in $lines) {
        $shouldExclude = $false
        foreach ($key in $ExcludeKeys) {
            if ($line -like "*$key*") { $shouldExclude = $true; break }
        }

        $matchedOld = $OldPaths | Where-Object { $line.Contains($_) } | Select-Object -First 1
        if ($shouldExclude) {
            $null = $newLines.Add($line)
        } elseif ($matchedOld) {
            $null = $newLines.Add($line.Replace($matchedOld, $NewPath))
            $patched = $true
        } elseif ($line.Contains($OldDataPath)) {
            $null = $newLines.Add($line.Replace($OldDataPath, $NewDataPathStr))
            $patched = $true
        } else {
            $null = $newLines.Add($line)
        }
    }

    if ($patched) {
        [System.IO.File]::WriteAllLines($cfg.FullName, $newLines)
        Write-Host "    [PATCHED] $($cfg.Directory.Name)\$($cfg.Name)" -ForegroundColor Green
    }
}

# --- Step 4b: Convert GeneralConfig XML paths to Linux paths (linux-x64 only) ---
if ($Runtime -eq "linux-x64") {
    Write-Host "`nStep 4b: Converting appsettings.json XML paths to Linux ($LinuxDeployBasePath)..." -ForegroundColor Cyan
    Convert-CyberCOLAAppSettingsPathsForLinux -PublishRootDir $PublishRootDir -LinuxDeployBasePath $LinuxDeployBasePath
}


# Final summary
Write-Host "`n==================================================" -ForegroundColor Cyan
Write-Host "PUBLISH SUMMARY" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Total: $($SuccessList.Count + $FailureList.Count), Success: $($SuccessList.Count), Failed: $($FailureList.Count)"

if ($FailureList.Count -gt 0) {
    Write-Host "`nFailed Services:" -ForegroundColor Red
    foreach ($svc in $FailureList) { Write-Host "  [XX] $svc" -ForegroundColor Red }
}
if ($ConfigSyncFailed) {
    Write-Host "`n[XX] Global configuration sync to $GlobalConfigDest failed (see Step 3)." -ForegroundColor Red
}
if ($FailureList.Count -eq 0 -and -not $ConfigSyncFailed) {
    Write-Host "`nAll services, configs, and paths updated successfully!" -ForegroundColor Green
}
Write-Host "==================================================" -ForegroundColor Cyan

# Automatically trigger WSL2 sync prompt
if ($Runtime -eq "linux-x64" -and $SuccessList.Count -gt 0) {
    Write-Host "`nLinux publish completed. Do you want to sync to WSL2 now?" -ForegroundColor Yellow
    $syncChoice = Read-Host "[Y/N] (Default: N)"
    if ($syncChoice -eq "Y" -or $syncChoice -eq "y") {
        & "$PSScriptRoot\Sync-ToWSL.ps1"
    }
}

# Without an explicit exit, the script returned whatever the last native command (robocopy) left in
# $LASTEXITCODE, so a fully successful publish could report exit code 2.
exit $(if ($FailureList.Count -gt 0 -or $ConfigSyncFailed) { 1 } else { 0 })
