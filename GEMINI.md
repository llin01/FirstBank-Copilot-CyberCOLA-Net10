# CyberCOLA System (.NET 10) - Project Context

## Project Overview
CyberCOLA is a modernized, high-performance payment gateway and authorization system developed for FirstBank. Migrated to **.NET 10.0**, it handles transaction processing, message routing, and integration with financial networks (FISC, NCCC) and electronic ticket systems (ICASH, IPASS, etc.).

## System Architecture

### 1. Authorization & Gateway Services
- **CyberAuth***: Authorization logic for FISC, NCCC, ICASH, IPASS, SVCS, and TWIN.
- **CyberGW***: Gateway interfaces for external network connectivity and message transformation.
- Available as **Console** applications, **Daemons**, and **Windows Services**.

### 2. Core Libraries (`libs/` & `CommonLib`)
- **TM (Transaction Manager)**: Core logic for ISO8583, FISC, MasterCard, and VISA protocols.
- **CommonLib**: Shared business logic, encryption utilities (AES-GCM), and networking helpers.
- **Interchange**: Message routing and format definitions.
- **Yetty**: High-concurrency network communication framework.
- **CyberUtils**: General-purpose utilities.

### 3. Simulation & Routing
- **Wpf***: Extensive suite of WPF-based simulators for testing (HSM, FISC, NCCC, MQ, terminal clients).
- **ServiceRouter**: Transaction routing and load balancing.
- **CyberSMS**: SMS notification system.

## Technology Stack
- **Runtime**: .NET 10.0 (x64)
- **UI**: WPF (Windows Presentation Foundation)
- **Messaging**: IBM MQ (`IBMMQDotnetClient`), System.Messaging (MSMQ)
- **Database**: SQL Server (`Microsoft.Data.SqlClient`)
- **Logging**: NLog, log4net
- **Serialization**: Newtonsoft.Json, System.Xml.Serialization

## Building and Running

`global.json` pins the .NET SDK to 10.0.404 (`rollForward: latestFeature`, so any later 10.0.4xx also works); a machine with only a newer SDK such as 11.x fails to build until SDK 10 is installed (`winget install Microsoft.DotNet.SDK.10`).

### Build the Solution
```powershell
dotnet build CyberCOLA.sln
```

### Publish
**Commit before publishing.** The SDK stamps the current commit into every DLL's ProductVersion (`1.0.0+<sha>`) but does not record uncommitted changes, so a build from a dirty tree cannot be traced back to its source. Check what a published binary was built from:
```powershell
[Diagnostics.FileVersionInfo]::GetVersionInfo('publish\win-x64\CyberGWNCCC\CyberGWNCCC.dll').ProductVersion
```

### Run a Specific Service (Example: FISC Auth Console)
```powershell
dotnet run --project src/CyberCOLA/CyberAuthFISCConsole/CyberAuthFISCConsole.csproj
```

### Test the Project
Automated tests (NUnit): `Yetty.Test` (network library) and `CyberCOLA.Test` (card-data log masking, Auth TCP framing, GW busy-channel retry).
```powershell
dotnet test libs/Yetty/src/Yetty.Test/Yetty.Test.csproj
dotnet test src/CyberCOLA/CyberCOLA.Test/CyberCOLA.Test.csproj
```

### PowerShell Operational Tooling
Day-to-day operation goes through the menu-based scripts in `tools/` (entry point: `pwsh tools/COLAMainMenu.ps1`). See `tools/README.md` for a task-oriented catalog of every script and `docs/tools_scripts_guide.md` for the script call graph and per-script details (`docs/TOOLS_ARCHITECTURE.md` keeps the publish data flow).

When a `tools/*.ps1` script is added, removed or changed (parameters, behavior, exit codes, call graph), update `docs/tools_scripts_guide.md` and `tools/README.md` in the same commit.

`src/CyberCOLA/WebToolsDashboard` (Blazor, `http://127.0.0.1:5300`) is a browser front end for the non-interactive scripts; its whitelist with each script's parameters and exit codes is `Services/ToolCatalog.cs`, so a script change that touches those must update the catalog too. It parses `Invoke-AuthTestUnattended.ps1 -StreamOutput` output in `Services/TranscriptParser.cs`.

## Development Conventions

### Coding Style
- **Language**: C#
- **Naming**: Follow standard .NET conventions (PascalCase for classes, methods, and public properties; camelCase for private fields with `_` prefix).
- **Comments**: All source code comments MUST be written in **English** to maintain international engineering standards.
- **Modern Features**: Utilize .NET 10 features and performance optimizations.
- **Logging**: Consistently use `LogManager.GetCurrentClassLogger()` via NLog for diagnostic output.

### Project Structure
- Core protocols reside in `libs/TM`.
- Shared domain logic is centered in `src/CyberCOLA/CommonLib`.
- Service entry points are in `src/CyberCOLA/`.
- UI-based testing tools are prefixed with `Wpf` in `src/CyberCOLA/`.
- `.claude/agents/` (read-only reviewers `cybercola-pci-reviewer`, `cybercola-log-analyzer`, `cybercola-tools-sync-checker`, `cybercola-config-auditor`, `cybercola-publish-checker`, `cybercola-iso8583-decoder`, and `cybercola-sp-reader` (read-only DB, only with the user's consent); runners that start services or tests but never write to the DB or edit files: `cybercola-auth-test-runner` via the `cybercola-auth-test` skill, `cybercola-stress-runner` for TPS/latency, `cybercola-unit-test-runner` for both test suites on Windows and WSL), `.claude/skills/` and `.claude/settings.json` are version-controlled; `.claude/settings.local.json` and `.claude/worktrees/` stay local. Never put credentials, unmasked card numbers or internal IPs in these files.

### Configuration
- Environment-specific settings are typically found in `appsettings.json` or `NLog.config` within project folders.
- Connection strings may be encrypted using system-specific logic (see `CommonLib.AuthUtils.ColaCommUtility`).
- Per-network issuer response deadlines (FISC 4 s, NCCC 6 s, SVCS/ICASH/IPASS 5 s) and the related timeout settings: `docs/AUTH_TIMEOUT_REFERENCE.md`.

## Agent-Specific Instructions
- **語言**：一律使用繁體中文（台灣用語）回答。
- **風格**：精簡扼要，直接給核心重點，避免冗長開場白。
- **術語**：技術術語保留英文原文。
- **注釋說明**：一律用英文說明。
- **錯誤分析**：格式為『原因 $\rightarrow$ 解法』。
- **格式**：程式碼區塊必須標示語言（如  `csharp ）。
- **UI 遷移 Logo 規範**：以後轉換 Logo 時，文字大小比例一律相同：前面「Web」大且粗體（例如 `font-size: 1.8rem; font-weight: 800;`），後面專案名稱小且細體（例如 `font-size: 1.1rem; font-weight: 400;`，搭配次要顏色），並直接在 HTML/Razor 中明確設定 style 鎖定以防繼承失效。
