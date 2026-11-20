namespace Com.Cybersoft4u.Test.Web.WebToolsDashboard.Services;

public enum ToolKind
{
    // Runs to completion in the dashboard; output and exit code are captured.
    Run,

    // Interactive menu: started in its own pwsh window; the dashboard cannot see its result.
    OpenWindow,
}

public enum ParameterType
{
    Choice,
    Int,
    Bool,
    Switch,
}

public sealed record ToolParameter(
    string Name,
    string Label,
    ParameterType Type,
    string Default,
    IReadOnlyList<string>? Choices = null,
    int Min = 0,
    int Max = int.MaxValue,
    string? Hint = null);

public sealed record ToolDefinition(
    string Id,
    string Category,
    string Title,
    string Description,
    string Script,
    ToolKind Kind,
    IReadOnlyList<ToolParameter> Parameters,
    IReadOnlyDictionary<int, string> ExitCodes,
    // Starts or stops the authorization chain: only one such tool may run at a time.
    bool Exclusive = false,
    bool NeedsDb = false,
    bool NeedsPublish = false,
    // The output follows the Invoke-AuthTestUnattended transcript format (steps and summary).
    bool AuthTestOutput = false);

/// <summary>
/// Whitelist of the tools the dashboard offers. Version 1 is read-only: nothing here writes to the database
/// or overwrites files (no -Apply, no publish). Every parameter is a choice, number or flag, so no free text
/// ever reaches the command line.
/// </summary>
public static class ToolCatalog
{
    public static readonly IReadOnlyList<string> Networks = ["NCCC", "FISC", "SVCS", "ICASH", "IPASS"];

    private static readonly IReadOnlyDictionary<int, string> ZeroIsSuccess = new Dictionary<int, string> { [0] = "完成" };

    public static readonly IReadOnlyList<ToolDefinition> All =
    [
        new("auth-test", "授權測試", "授權流程測試（無人值守）",
            "啟動授權鏈 → 跑流量 → 取統計 → 停止全部，並存成 transcript。",
            "Invoke-AuthTestUnattended.ps1", ToolKind.Run,
            [
                new("Network", "網路", ParameterType.Choice, "NCCC", Networks),
                new("TrafficWaitSec", "流量時間（秒）", ParameterType.Int, "90", Min: 10, Max: 1800),
                new("SimCount", "每條連線筆數（Group 1）", ParameterType.Int, "100", Min: 1, Max: 10000),
                new("SimInterval", "每筆間隔 ms（Group 1）", ParameterType.Int, "50", Min: 0, Max: 10000),
                new("SimReplaceCard", "換卡（Group 1）", ParameterType.Bool, "true"),
                new("SimIncludeChip", "送晶片範本（Group 1）", ParameterType.Switch, "false",
                    Hint: "預期會有 102／232 拒絕"),
                new("RepeatCount", "重複次數（Group 2）", ParameterType.Int, "10", Min: 1, Max: 1000),
                new("IntervalMs", "每筆間隔 ms（Group 2）", ParameterType.Int, "100", Min: 0, Max: 10000),
            ],
            new Dictionary<int, string>
            {
                [0] = "成功取得統計",
                [1] = "授權鏈沒到 Control Menu（環境問題）",
                [2] = "沒有印出統計",
                [3] = "測試腳本被強制結束",
            },
            Exclusive: true, NeedsDb: true, NeedsPublish: true, AuthTestOutput: true),

        new("maint-test", "MaintGateway", "MaintGateway 無人值守測試",
            "依序啟動 NCCC 模擬器 → GW → Tester，打 send-xml，確認模擬器收到 0420、送出 0430，最後停止全部。",
            "Invoke-MaintGatewayTestUnattended.ps1", ToolKind.Run,
            [new("Mode", "模式", ParameterType.Choice, "All",
                ["All", "HTTP", "HTTPS", "JWT", "UnifiedHTTP", "UnifiedHTTPS", "UnifiedJWT"])],
            new Dictionary<int, string> { [0] = "全部通過", [1] = "沒有執行（缺少發行檔）", [2] = "有模式失敗" },
            Exclusive: true, NeedsPublish: true),

        new("db-check", "環境檢查", "資料庫連線", "解密連線字串並測試 DB 1433（會先確認 GlobalProtect）。",
            "Test-DatabaseConnection.ps1", ToolKind.Run, [], new Dictionary<int, string> { [0] = "DB 可連線" },
            NeedsDb: true),
        new("health", "環境檢查", "系統健康診斷", "DB 連線、Kestrel port 衝突、log 目錄寫入權限。",
            "Check-SystemHealth.ps1", ToolKind.Run, [], ZeroIsSuccess),
        new("vpn", "環境檢查", "GlobalProtect 連線", "自動連 GlobalProtect 並確認 DB 連線埠。",
            "Connect-GlobalProtect.ps1", ToolKind.Run,
            [
                new("TimeoutSeconds", "逾時（秒）", ParameterType.Int, "120", Min: 10, Max: 600),
                new("MaxAttempts", "最多嘗試次數", ParameterType.Int, "3", Min: 1, Max: 10),
            ],
            new Dictionary<int, string> { [0] = "VPN 與 DB 都通", [1] = "VPN 未連線", [2] = "DB 不通" }),
        new("publish-state", "環境檢查", "發行版檢查", "比對 publish 裡每個服務的發行 commit 和 HEAD，列出過期原因與發行指令（不發行）。",
            "Test-CyberCOLAPublishState.ps1", ToolKind.Run,
            [new("Runtime", "平台", ParameterType.Choice, "win-x64", ["win-x64", "linux-x64"])],
            new Dictionary<int, string> { [0] = "全部最新", [1] = "沒有執行（沒有 git 或發行目錄）", [2] = "有過期或從沒發行的服務" }),

        new("credit-preview", "測試資料（唯讀）", "測試卡額度預覽", "列出卡號池的可用額度，低於 10,000 的列標紅。",
            "Invoke-CyberCOLASql.ps1", ToolKind.Run,
            [new("Name", "SQL", ParameterType.Choice, "Preview-AuthTestCardCredit", ["Preview-AuthTestCardCredit"])],
            new Dictionary<int, string> { [0] = "完成", [1] = "DB 連不到", [4] = "SQL 錯誤" }, NeedsDb: true),
        new("icash-preview", "測試資料（唯讀）", "ICASH 票卡預覽", "檢查 ICASH 唯一票卡的管制碼與效期。",
            "Invoke-CyberCOLASql.ps1", ToolKind.Run,
            [new("Name", "SQL", ParameterType.Choice, "Preview-ICashTestCard", ["Preview-ICashTestCard"])],
            new Dictionary<int, string> { [0] = "完成", [1] = "DB 連不到", [4] = "SQL 錯誤" }, NeedsDb: true),
        new("card-pool", "測試資料（唯讀）", "卡號池候選卡", "列出授權 SP 會接受的卡（不寫檔）。",
            "New-AuthTestCardPool.ps1", ToolKind.Run,
            [new("MinCreditAvail", "最低可用額度", ParameterType.Int, "10000", Min: 0, Max: 10_000_000)],
            new Dictionary<int, string> { [0] = "完成", [1] = "DB 連不到", [2] = "沒有合格的卡" }, NeedsDb: true),
        new("eticket-cards", "測試資料（唯讀）", "電子票證範本票卡", "檢查 ICASH／IPASS／SVCS 範本的票卡（不寫檔）。",
            "Set-ETicketTestCards.ps1", ToolKind.Run, [],
            new Dictionary<int, string> { [0] = "完成", [1] = "DB 或模擬器不可用", [2] = "有網路沒有合格的票卡" },
            NeedsDb: true, NeedsPublish: true),

        new("log-errors", "診斷", "找 log 錯誤", "從最新的 log 找出錯誤與 stack trace。",
            "Find-LogErrors.ps1", ToolKind.Run, [], ZeroIsSuccess),
        new("verify-scripts", "診斷", "檢查 script 語法", "檢查 tools/ 內所有 .ps1 的語法。",
            "Verify-Scripts.ps1", ToolKind.Run, [], ZeroIsSuccess),
        new("dependency", "診斷", "專案相依性", "掃描 .csproj 的專案參考，輸出樹狀圖與 Mermaid 圖。",
            "Get-ProjectDependency.ps1", ToolKind.Run, [], ZeroIsSuccess),

        new("menu-main", "互動選單（新視窗）", "主選單", "Unified Control Center。", "COLAMainMenu.ps1",
            ToolKind.OpenWindow, [], ZeroIsSuccess),
        new("menu-console", "互動選單（新視窗）", "服務管理", "個別啟停各網路服務。", "COLAMainMenu-Console.ps1",
            ToolKind.OpenWindow, [], ZeroIsSuccess),
        new("menu-sim", "互動選單（新視窗）", "模擬器管理", "啟停各網路模擬器、HSM、SMS、EAI。",
            "COLASimulatorMainMenu.ps1", ToolKind.OpenWindow, [], ZeroIsSuccess),
        new("menu-flow", "互動選單（新視窗）", "整合測試流程", "依序啟動 Sim → GW → Auth。", "COLATestFlowMenu.ps1",
            ToolKind.OpenWindow, [], ZeroIsSuccess),
        new("menu-auth", "互動選單（新視窗）", "授權鏈（互動）", "啟動授權鏈並進入狀態選單。",
            "Test-AuthorizationSystem.ps1", ToolKind.OpenWindow,
            [new("Network", "網路", ParameterType.Choice, "NCCC", Networks)], ZeroIsSuccess),
        new("start-nccc", "互動選單（新視窗）", "一鍵啟動 NCCC", "每一步都會詢問是否執行。", "Start-NCCC.ps1",
            ToolKind.OpenWindow, [], ZeroIsSuccess),
        new("start-fisc", "互動選單（新視窗）", "一鍵啟動 FISC", "每一步都會詢問是否執行。", "Start-FISC.ps1",
            ToolKind.OpenWindow, [], ZeroIsSuccess),
        new("monitor", "互動選單（新視窗）", "程序與 TCP 監控", "即時顯示服務程序狀態與 TCP 連線。",
            "Monitor-COLAServices.ps1", ToolKind.OpenWindow, [], ZeroIsSuccess),
        new("traffic", "互動選單（新視窗）", "交易量圖", "從 GW log 統計各網路的即時 TPS。",
            "Show-CyberCOLATrafficChart.ps1", ToolKind.OpenWindow, [], ZeroIsSuccess),
    ];

    public static ToolDefinition? Find(string id) => All.FirstOrDefault(t => t.Id == id);
}
