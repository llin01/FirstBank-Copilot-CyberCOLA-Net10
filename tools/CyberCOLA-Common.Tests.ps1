# Pester 3.4 compatible unit tests for CyberCOLA-Common.ps1 helpers.
# These tests only exercise functions that do not require live services, ports, or processes.

$CommonModule = Join-Path $PSScriptRoot 'CyberCOLA-Common.ps1'
. $CommonModule

# Mock Read-Host for non-interactive Confirm-CyberCOLA tests
function Mock-ReadHost {
    param([string]$ReturnValue)
    function Read-Host { return $ReturnValue }
}

Describe 'CyberCOLA-Common Helpers' {
    Context 'Base / Publish / Log Root Paths' {
        It 'Get-CyberCOLABaseDir returns repository root' {
            $base = Get-CyberCOLABaseDir
            $base | Should Not BeNullOrEmpty
            Test-Path (Join-Path $base 'CyberCOLA.sln') | Should Be $true
        }

        It 'Get-CyberCOLAPublishDir returns expected publish path' {
            $pub = Get-CyberCOLAPublishDir -Runtime 'win-x64'
            $expected = Join-Path (Get-CyberCOLABaseDir) 'publish\win-x64'
            $pub | Should Be $expected
        }

        It 'Get-CyberCOLALogRoot honors environment override' {
            $env:CYBERCOLA_LOG_ROOT = 'E:\TestLogs'
            $root = Get-CyberCOLALogRoot
            $root | Should Be 'E:\TestLogs'
            Remove-Item Env:\CYBERCOLA_LOG_ROOT
        }
    }

    Context 'Read-CyberCOLAJsonWithComments' {
        $tmpDir = $null

        BeforeEach {
            $tmpDir = Join-Path $env:TEMP ([System.Guid]::NewGuid().ToString())
            New-Item -ItemType Directory -Path $tmpDir -Force | Out-Null
        }

        AfterEach {
            if ($tmpDir -and (Test-Path $tmpDir)) {
                Remove-Item $tmpDir -Recurse -Force -ErrorAction SilentlyContinue
            }
        }

        It 'Parses JSON with inline comments and http:// preserved' {
            $jsonPath = Join-Path $tmpDir 'appsettings.json'
            @'
{
  // This is a comment
  "GeneralConfig": {
    "ServiceUrl": "http://example.com", // url comment
    "Port": 14801
  }
}
'@ | Set-Content -Path $jsonPath -Encoding UTF8

            $result = Read-CyberCOLAJsonWithComments -Path $jsonPath
            $result.GeneralConfig.ServiceUrl | Should Be 'http://example.com'
            $result.GeneralConfig.Port | Should Be 14801
        }

        It 'Returns null on missing file' {
            $missing = Join-Path $tmpDir 'nonexistent.json'
            $result = Read-CyberCOLAJsonWithComments -Path $missing
            $result | Should BeNullOrEmpty
        }
    }

    Context 'Service Catalog' {
        It 'Get-CyberCOLAServiceCatalog returns non-empty array' {
            $catalog = Get-CyberCOLAServiceCatalog
            $catalog.Count -gt 0 | Should Be $true
        }

        It 'Catalog entries contain Name, Type, Module, ProjectDir' {
            $catalog = Get-CyberCOLAServiceCatalog
            $first = $catalog | Select-Object -First 1
            $first.Name | Should Not BeNullOrEmpty
            $first.Type | Should Not BeNullOrEmpty
            $first.Module | Should Not BeNullOrEmpty
            $first.ProjectDir | Should Not BeNullOrEmpty
        }
    }

    Context 'Confirm-CyberCOLA' {
        It 'Returns true when user answers Y' {
            function Read-Host { return 'Y' }
            Confirm-CyberCOLA -Prompt 'Proceed?' | Should Be $true
        }

        It 'Returns false when user answers N' {
            function Read-Host { return 'N' }
            Confirm-CyberCOLA -Prompt 'Proceed?' | Should Be $false
        }

        It 'Returns default value when user presses Enter' {
            function Read-Host { return '' }
            Confirm-CyberCOLA -Prompt 'Proceed?' -Default 'Y' | Should Be $true
            Confirm-CyberCOLA -Prompt 'Proceed?' -Default 'N' | Should Be $false
        }
    }

    Context 'Clear-CyberCOLAScreen' {
        It 'Swallows Clear-Host exceptions in non-interactive terminals' {
            function Clear-Host { throw 'Clear-Host not supported' }
            { Clear-CyberCOLAScreen } | Should Not Throw
        }
    }

    Context 'Console Input Helpers' {
        It 'Helpers tolerate missing console and return safe defaults' {
            # In non-interactive terminals [Console]::KeyAvailable/ReadKey throw; helpers catch and return safe values.
            { $available = Test-CyberCOLAKeyAvailable } | Should Not Throw
            { $key = Read-CyberCOLAConsoleKey -Intercept } | Should Not Throw
            if (-not $available) { $key | Should BeNullOrEmpty }
        }
    }

    Context 'ConvertTo-CyberCOLAPortList' {
        It 'Normalizes single integer' {
            $result = ConvertTo-CyberCOLAPortList -Value 8080
            $result.Count | Should Be 1
            $result[0] | Should Be 8080
        }

        It 'Normalizes integer array' {
            $result = ConvertTo-CyberCOLAPortList -Value @(80, 443)
            $result.Count | Should Be 2
            $result | Should Be @(80, 443)
        }

        It 'Normalizes comma-separated string' {
            $result = ConvertTo-CyberCOLAPortList -Value '6059,6063,8100'
            $result.Count | Should Be 3
            $result | Should Be @(6059, 6063, 8100)
        }

        It 'Returns empty array for null or empty input' {
            ConvertTo-CyberCOLAPortList -Value $null | Should BeNullOrEmpty
            ConvertTo-CyberCOLAPortList -Value '' | Should BeNullOrEmpty
        }
    }

    Context 'Set-CyberCOLAConsoleSize' {
        It 'Does not throw outside ConsoleHost (Pester host)' {
            # Pester runs under a non-ConsoleHost (or remote) host; the helper early-returns.
            { Set-CyberCOLAConsoleSize } | Should Not Throw
        }
    }

    Context 'Stop-CyberCOLAProcessesByCommandLine' {
        It 'Does not throw when no matching process exists (name-only path)' {
            { Stop-CyberCOLAProcessesByCommandLine -ProjectName 'DefinitelyNoProc9999' } | Should Not Throw
        }

        It 'Does not throw with CommandLine filter and no matches' {
            { Stop-CyberCOLAProcessesByCommandLine -ProjectName 'DefinitelyNoProc9999' -CommandLinePattern 'Fisc' } | Should Not Throw
        }
    }
}
