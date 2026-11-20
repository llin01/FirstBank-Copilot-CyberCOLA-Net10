<#
    Decodes a CyberCOLA ISO 8583 message (FISC, NCCC, NCCC QRCode, TWIN, ICASH, IPASS, SVCS) with the project's own
    TM library from a published build, and prints the MTI, the network header and the value of each field F1-F127
    (subfields are not split). Card data is always masked (PCI DSS): PAN as ****-****-****-1234, Track 2 as masked
    PAN + expiry + service code, PIN block as ***, any other 13-19 digit run (also inside hex, e.g. EMV tag 5A) as
    ****1234; fields that carry a CVV2 inside (FISC F48; NCCC/NCCCQRCode/TWIN F62, F63) are hidden whole.

    The 2-byte length prefix used on the GW and COLA/Auth links (big-endian binary, not counting itself) is detected,
    reported and checked: equal to the remaining bytes = one message; smaller = several messages (each decoded);
    larger = truncated input.

    Usage:
      pwsh tools\ConvertFrom-Iso8583.ps1 -Network FISC -Path src\CyberCOLA\ConsoleStressSimulator\Data\Fisc\0100#48.dat
      pwsh tools\ConvertFrom-Iso8583.ps1 -Network NCCC -Hex '01 2A 49 53 4F ...'
      pwsh tools\ConvertFrom-Iso8583.ps1 -Network SVCS -Hex '...' -LengthPrefix No

    Exit codes: 0 = decoded, 1 = bad input or TM.dll not found, 2 = a message could not be decoded.
#>
[CmdletBinding(DefaultParameterSetName = 'Path')]
param(
    [Parameter(Mandatory)]
    [ValidateSet('FISC', 'NCCC', 'NCCCQRCode', 'TWIN', 'ICASH', 'IPASS', 'SVCS')]
    [string]$Network,
    [Parameter(Mandatory, ParameterSetName = 'Path')]
    [string]$Path,
    [Parameter(Mandatory, ParameterSetName = 'Hex')]
    [string]$Hex,
    # Auto: strip the 2-byte length only when it is consistent with the input; Yes: always; No: never.
    [ValidateSet('Auto', 'Yes', 'No')]
    [string]$LengthPrefix = 'Auto',
    # Folder of a published build that contains TM.dll; defaults to the simulator that sends this network.
    [string]$TmDir
)

if ($PSVersionTable.PSEdition -eq 'Desktop') {
    Write-Warning "PowerShell 7 (pwsh.exe) is required."
    exit 1
}

. "$PSScriptRoot\CyberCOLA-Common.ps1"
$BaseDir = Get-CyberCOLABaseDir

# --- Input bytes ---------------------------------------------------------------------------------------------------
if ($PSCmdlet.ParameterSetName -eq 'Path') {
    $full = if ([IO.Path]::IsPathRooted($Path)) { $Path } else { Join-Path (Get-Location) $Path }
    if (-not (Test-Path -LiteralPath $full -PathType Leaf)) { Write-Host "File not found: $Path" -ForegroundColor Red; exit 1 }
    $bytes = [IO.File]::ReadAllBytes($full)
    # Template file names may contain a card number.
    $source = (Split-Path $full -Leaf) -replace '\d{9,15}(\d{4})', '****$1'
} else {
    $clean = ($Hex -replace '0x', '' -replace '[\s:\-,]', '')
    if ($clean.Length -eq 0 -or $clean.Length % 2 -ne 0 -or $clean -notmatch '^[0-9A-Fa-f]+$') {
        Write-Host "-Hex must be an even number of hex digits (spaces, '-', ':' and 0x are allowed)." -ForegroundColor Red
        exit 1
    }
    $bytes = [Convert]::FromHexString($clean)
    $source = 'hex input'
}
Write-Host ("Input    : {0}, {1} byte(s), network {2}" -f $source, $bytes.Length, $Network)

# --- 2-byte length prefix --------------------------------------------------------------------------------------------
# Returns the messages to decode: one per frame when the prefix is used, otherwise the whole input.
function Split-Frames([byte[]]$data, [string]$mode) {
    $frames = [Collections.Generic.List[byte[]]]::new()
    if ($mode -eq 'No' -or $data.Length -lt 3) {
        Write-Host "Prefix   : not used"
        $frames.Add($data); return , $frames
    }

    # Walk the frames; Auto accepts the prefix only when the frames end exactly at the end of the input.
    $offset = 0; $ok = $true; $parts = @()
    while ($offset -lt $data.Length) {
        if ($data.Length - $offset -lt 2) { $ok = $false; break }
        $len = $data[$offset] * 256 + $data[$offset + 1]
        $parts += [pscustomobject]@{ Offset = $offset; Length = $len; Available = $data.Length - $offset - 2 }
        if ($len -eq 0 -or $len -gt $data.Length - $offset - 2) { $ok = $false; break }
        $offset += 2 + $len
    }

    if (-not $ok -and $mode -eq 'Auto') {
        $first = $data[0] * 256 + $data[1]
        Write-Host ("Prefix   : not used (first two bytes 0x{0:X4} = {0} do not frame the input of {1} byte(s))" -f $first, $data.Length)
        $frames.Add($data); return , $frames
    }

    foreach ($p in $parts) {
        $status = if ($p.Length -eq $p.Available -and $parts.Count -eq 1) { 'matches the remaining bytes' }
                  elseif ($p.Length -le $p.Available) { 'one of several messages' }
                  else { "longer than the $($p.Available) byte(s) left: input truncated by $($p.Length - $p.Available)" }
        Write-Host ("Prefix   : 0x{0:X4} = {0} at offset {1} ({2})" -f $p.Length, $p.Offset, $status)
        $take = [Math]::Min($p.Length, $p.Available)
        if ($take -gt 0) { $frames.Add([byte[]]$data[($p.Offset + 2)..($p.Offset + 1 + $take)]) }
    }
    return , $frames
}
$frames = Split-Frames $bytes $LengthPrefix

# --- TM library ------------------------------------------------------------------------------------------------------
$className = @{ FISC = 'Com.Cybersoft4u.TM.FISC.FISC'; NCCC = 'Com.Cybersoft4u.TM.ISO8583.Nccc.NcccIso8583'
                NCCCQRCode = 'Com.Cybersoft4u.TM.ISO8583.Nccc.NcccIso8583'; TWIN = 'Com.Cybersoft4u.TM.ISO8583.TWIN.TWINIso8583'
                ICASH = 'Com.Cybersoft4u.TM.ISO8583.ICASH.ICASH'; IPASS = 'Com.Cybersoft4u.TM.ISO8583.IPASS.IPASS'
                SVCS = 'Com.Cybersoft4u.TM.ISO8583.SVCS.SVCS' }[$Network]
if (-not $TmDir) {
    $preferred = if ($Network -in 'ICASH', 'IPASS', 'SVCS') { 'WpfETicketClientSimulator' } else { 'ConsoleStressSimulator' }
    $TmDir = Join-Path $BaseDir "publish\win-x64\$preferred"
    if (-not (Test-Path (Join-Path $TmDir 'TM.dll'))) {
        $any = Get-ChildItem (Join-Path $BaseDir 'publish\win-x64') -Filter TM.dll -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($any) { $TmDir = $any.DirectoryName }
    }
}
if (-not (Test-Path (Join-Path $TmDir 'TM.dll'))) {
    Write-Host "TM.dll not found under $TmDir. Publish a simulator first: pwsh tools\Publish-CyberCOLAServices.ps1 -SingleProjectName ConsoleStressSimulator -TargetRuntime win-x64" -ForegroundColor Red
    exit 1
}
# TM.dll depends on the other assemblies of the same build.
Get-ChildItem $TmDir -Filter *.dll | ForEach-Object { try { [void][Reflection.Assembly]::LoadFrom($_.FullName) } catch { } }
$tm = [Reflection.Assembly]::LoadFrom((Join-Path $TmDir 'TM.dll'))
$types = try { $tm.GetTypes() } catch [Reflection.ReflectionTypeLoadException] { $_.Exception.Types | Where-Object { $_ } }
$type = $types | Where-Object { $_.FullName -eq $className } | Select-Object -First 1
if (-not $type) { Write-Host "$className not found in $TmDir\TM.dll." -ForegroundColor Red; exit 1 }
$ctor = $type.GetConstructor([type[]]@([byte[]]))

# --- Masking ---------------------------------------------------------------------------------------------------------
# Fields are listed as decoded (F1-F127), without splitting subfields. The 13-19 digit masking cannot catch a 3-digit
# CVV2, so fields that carry one inside are hidden whole: FISC DE48 (SE92 CVC2, SE44 3-D Secure value) and, on the
# BASE24-token networks, F62 and F63 (token C0 starts with the CVD field, TokenC0.DataDecode).
$HiddenFields = @{
    FISC       = @('F_48')
    NCCC       = @('F_62', 'F_63')
    NCCCQRCode = @('F_62', 'F_63')
    TWIN       = @('F_62', 'F_63')
}
function Hide-Digits([string]$s) { $s -replace '(?<!\d)\d{9,15}(\d{4})(?!\d)', '****$1' }

function Format-Value($field, $value) {
    $text = if ($value -is [byte[]]) { [Convert]::ToHexString($value) } else { "$value" }
    if ($HiddenFields[$Network] -contains $field) { return "*** (may contain CVV2, hidden; $($text.Length) chars)" }
    switch ($field) {
        'F_2' { if ($text -match '(\d{4})\s*$') { return "****-****-****-$($Matches[1])" } return '***' }
        'F_35' {
            # PAN = YYMM + service code, then discretionary data that is dropped.
            if ($text -match '^\D*\d*?(\d{4})[=D](\d{4})(\d{3})?') { return "****-****-****-$($Matches[1])=$($Matches[2])$($Matches[3])..." }
            return '***'
        }
        'F_45' { return Hide-Digits ($text -replace '\^[^\^]*\^', '^***^') }
        'F_52' { return '***' }
        default { return Hide-Digits $text }
    }
}

# --- Decode ----------------------------------------------------------------------------------------------------------
$failed = 0
for ($i = 0; $i -lt $frames.Count; $i++) {
    $frame = $frames[$i]
    Write-Host ""
    if ($frames.Count -gt 1) { Write-Host ("=== Message {0} of {1} ({2} byte(s))" -f ($i + 1), $frames.Count, $frame.Length) -ForegroundColor Cyan }
    try {
        $msg = $ctor.Invoke(@(, [byte[]]$frame))
    } catch {
        $reason = if ($_.Exception.InnerException) { $_.Exception.InnerException.Message } else { $_.Exception.Message }
        Write-Host "Decode failed: $(Hide-Digits $reason)" -ForegroundColor Red
        Write-Host ("First bytes: {0}" -f [Convert]::ToHexString($frame[0..([Math]::Min(15, $frame.Length - 1))]))
        $failed++
        continue
    }

    $headerMethod = $type.GetMethod('Header', [type[]]@())
    if ($headerMethod) {
        $header = $headerMethod.Invoke($msg, @())
        if ($header) { Write-Host "Header   : $(Hide-Digits "$($header.Encode())")" }
    }
    Write-Host "MTI      : $($msg.Mti())" -ForegroundColor Green

    $rows = foreach ($pair in $msg.Core().OrderedFields()) {
        $key = "$($pair.Key)"
        # Only the standard data elements F1-F127.
        if ($key -notmatch '^F_(\d+)$' -or [int]$Matches[1] -lt 1 -or [int]$Matches[1] -gt 127) { continue }
        [pscustomobject]@{ Field = [int]$Matches[1]; Value = Format-Value $key $pair.Value }
    }
    $rows | Format-Table -AutoSize -Wrap | Out-String -Width 200 | ForEach-Object { $_.TrimEnd() } | Write-Host
}

exit $(if ($failed -gt 0) { 2 } else { 0 })
