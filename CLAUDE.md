# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

A `GEMINI.md` also exists with equivalent context for other agents; keep both in sync if architecture/build facts here change materially.

## Project Overview

CyberCOLA is FirstBank's payment gateway and authorization system, modernized to **.NET 10.0** (`net10.0` for services/libraries, `net10.0-windows` for WPF projects), primary platform **x64**. It processes financial transactions (ISO8583) across multiple Taiwanese networks — **FISC** (財金), **NCCC** (聯卡), **ICASH**, **IPASS**, **SVCS** (電子票證), **TWIN** — plus SMS and maintenance-gateway services, and includes an extensive suite of WPF/Web simulators for testing the whole ecosystem without live external hosts.

- **Configuration:** XML-driven (`src/CyberCOLA/Config/*.xml`) + per-project `appsettings.json`
- **Logging:** NLog (`NLog.config` per executable) via `LoggerAdapter.GetLogger(...)` or injected `ILogger<T>` — never create log4net appenders directly

## Build, Publish, Test, Run

`global.json` pins the .NET SDK to 10.0.404 (`rollForward: latestFeature`, so any later 10.0.4xx also works); a machine with only a newer SDK such as 11.x fails to build until SDK 10 is installed (`winget install Microsoft.DotNet.SDK.10`).

```bash
# Publish (prefer -r win-x64; plain `dotnet build` outputs to bin\Debug\net10.0\ instead of a runtime-specific folder)
dotnet publish CyberCOLA.sln -c Release -r win-x64 --self-contained false -o publish/win-x64
dotnet publish src/CyberCOLA/CyberAuthFISC/CyberAuthFISC.csproj -c Release -r win-x64 --self-contained false -o publish/win-x64/CyberAuthFISC

# Commit before publishing: the SDK stamps the current commit into every DLL's ProductVersion
# (1.0.0+<sha>) but does not record uncommitted changes, so a build from a dirty tree cannot be
# traced back to its source. Check what a published binary was built from:
#   [Diagnostics.FileVersionInfo]::GetVersionInfo('publish\win-x64\CyberGWNCCC\CyberGWNCCC.dll').ProductVersion

# Test — automated test projects (NUnit): Yetty.Test and CyberCOLA.Test
dotnet test libs/Yetty/src/Yetty.Test/Yetty.Test.csproj
dotnet test src/CyberCOLA/CyberCOLA.Test/CyberCOLA.Test.csproj
```

`Yetty.Test` and `CyberCOLA.Test` are the only automated suites; most networking tests run the real GW/COLA/Auth code over loopback, so run them on Windows and on Linux (e.g. WSL). The `*Tester`/`*Simulator`/`Wpf*Test*` projects (e.g. `WpfFunctionTest`, `MaintGatewayTester`) are interactive/manual verification tools, not automated tests. `src/CyberCOLA/CyberCOLABenchmarks` is a BenchmarkDotNet project for perf, not correctness testing.

### PowerShell operational tooling (`tools/`, root)
The system is normally driven through a menu-based PowerShell toolchain rather than raw `dotnet` commands directly:

See `tools/README.md` for a task-oriented catalog of every script (entry point, categories, prerequisites, menu keys), `docs/tools_scripts_guide.md` for the full script call graph and per-script details, `docs/TOOLS_ARCHITECTURE.md` for the publish data flow and `docs/RELEASE_MAPPING_GUIDE.md` for exact Release build/publish output paths per project category.

When a `tools/*.ps1` script is added, removed or changed (parameters, behavior, exit codes, call graph), update `docs/tools_scripts_guide.md` and `tools/README.md` in the same commit.

`src/CyberCOLA/WebToolsDashboard` (Blazor, `http://127.0.0.1:5300`) is a browser front end for the non-interactive scripts; its whitelist with each script's parameters and exit codes is `Services/ToolCatalog.cs`, so a script change that touches those must update the catalog too. It parses `Invoke-AuthTestUnattended.ps1 -StreamOutput` output in `Services/TranscriptParser.cs`.

## Architecture

### Five-tier transaction pipeline
```
Simulator --TCP/ISO8583--> CyberGW --BlockingCollection queue--> internal COLA (ColaService)
    --Internal TCP--> CyberAuth --SqlClient/stored proc--> SQL Server
```
Responses flow back through the same chain in reverse, correlated by a generated `RelationId` (`ColaService.DispatchTask()` on the way in, `RemoveRelation(relationId)` on the way back). Full sequence diagram and per-step description: `docs/transaction_flow.md`.

**Two connection topologies at the Gateway tier**, chosen per network:
- **Active (GW = TCP Client)** — FISC, NCCC: the GW initiates the connection to the external host/simulator and performs a Sign-On handshake (e.g. `FISCTransCreator.FiscSignOnGWRequest()`) before the channel is usable.
- **Passive (GW = TCP Server)** — ICASH, IPASS, SVCS: the GW listens (e.g. ports 6700/5504/4500) and the external simulator connects in; channels are registered via the `Yetty` framework.

### MaintGateway subsystem
The `MaintGateway*` / `WebMaintGatewayTester*` projects are a separate maintenance-gateway subsystem (HTTP/HTTPS/JWT variants); see `docs/maint_gateway_jwt_architecture.md` and `docs/MAINT_GATEWAY_TESTER_FLOW.md`.

### Persistent TCP & correlation
Long-lived TCP connections are used for internal COLA↔Auth and (for active channels) GW↔external links; responses are correlated by identifiers such as **STAN (ISO8583 Field 11)** or the generated `RelationId`, not by raw connection ordering. Per-network issuer response deadlines (FISC 4 s, NCCC 6 s, SVCS/ICASH/IPASS 5 s) and the related CyberCOLA timeout settings are in `docs/AUTH_TIMEOUT_REFERENCE.md`.

### Configuration & secrets
Connection strings and other sensitive config values are encrypted using logic in `CommonLib.AuthUtils.ColaCommUtility` (AES-GCM); `tools/Test-DatabaseConnection.ps1` reimplements the same decryption in PowerShell for standalone diagnostics. Never hardcode or log decrypted secrets (see global credit-card/PCI logging rules).

## Conventions

- All source comments in **English**, regardless of chat language.
- `.claude/agents/` (read-only reviewers `cybercola-pci-reviewer`, `cybercola-log-analyzer`, `cybercola-tools-sync-checker`, `cybercola-config-auditor`, `cybercola-publish-checker`, `cybercola-iso8583-decoder`, and `cybercola-sp-reader` (read-only DB, only with the user's consent); runners that start services or tests but never write to the DB or edit files: `cybercola-auth-test-runner` via the `cybercola-auth-test` skill, `cybercola-stress-runner` for TPS/latency, `cybercola-unit-test-runner` for both test suites on Windows and WSL), `.claude/skills/` and `.claude/settings.json` are version-controlled; `.claude/settings.local.json` and `.claude/worktrees/` stay local. Never put credentials, unmasked card numbers or internal IPs in these files.