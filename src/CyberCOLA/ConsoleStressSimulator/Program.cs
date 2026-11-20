using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Com.Cybersoft4u.Simulator.Console.ConsoleStressSimulator
{
    public class Program
    {
        private static ConcurrentQueue<byte[]> _currentTestQueue = new ConcurrentQueue<byte[]>();
        private static TransactionType _selectedType = TransactionType.NCCC;
        private static int _generatedCount = 10000;
        private static string _dataSourceInfo = "Not initialized";

        // Server Configuration
        private static int _listenPort = 14801;
        private static int _sendIntervalMs = 0;
        private static bool _autoStart = false;
        private static bool _replaceCardNumber = true;
        // Off by default: with card replacement, chip templates only fail (ARQC 102 / ATC 232); --include-chip-templates
        // keeps them to exercise the chip path. The HSM's ARQC check (Func07) also needs HSMCheckFlags IsARQCCheck=Y.
        private static bool _includeChipTemplates = false;

        private static StressEngine.TestResultMetrics? _lastMetrics;

        public static async Task Main(string[] args)
        {
            System.Console.Title = "CyberCOLA High-Performance Console Stress Server Simulator v1.0 (.NET 10.0)";

            // FISC messages are EBCDIC (IBM037); .NET only ships those code pages via this provider. Without it
            // every FISC template failed to decode and was sent unmodified (same STAN/RRN on every message).
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            // 1. Load appsettings.json configuration
            LoadConfiguration();

            // 2. Parse CLI Arguments to override configuration
            ParseCommandLineArguments(args);

            // 3. Pre-generate test data automatically if queue is empty
            AutoPreGenerateData();

            // 4. Check for Auto-Start mode: Auto start stress service, then proceed to Main Menu
            if (_autoStart)
            {
                System.Console.ForegroundColor = ConsoleColor.Green;
                System.Console.WriteLine($"\n==========================================================================");
                System.Console.WriteLine($" [Auto-Start Triggered] Automatically starting Server Stress Test Service...");
                System.Console.WriteLine($" [Data Source] {_dataSourceInfo}");
                System.Console.WriteLine($" [Card Replace] Replace Card Number: {(_replaceCardNumber ? "ON" : "OFF")}");
                System.Console.WriteLine($"==========================================================================\n");
                System.Console.ResetColor();

                await StartServerStressTestMenuAsync(isAutoStarted: true);
            }

            // 5. Interactive Main Menu Mode (Always displays after auto-start or manual start)
            bool running = true;
            while (running)
            {
                RenderMainMenu();
                string? choice = System.Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1":
                        GenerateDataMenu();
                        break;
                    case "2":
                        await StartServerStressTestMenuAsync();
                        break;
                    case "3":
                        RegenerateData();
                        break;
                    case "4":
                        ConfigureParametersMenu();
                        break;
                    case "5":
                        ViewMetricsSummary();
                        break;
                    case "0":
                        running = false;
                        System.Console.WriteLine("\nThank you for using CyberCOLA Stress Server Simulator. Goodbye!");
                        break;
                    default:
                        System.Console.WriteLine("\n[Error] Invalid choice. Press any key to continue...");
                        System.Console.ReadKey();
                        break;
                }
            }
        }

        private static void LoadConfiguration()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("StressSimulator", out var section))
                    {
                        if (section.TryGetProperty("ListenPort", out var port)) _listenPort = port.GetInt32();
                        if (section.TryGetProperty("SendIntervalMs", out var interval)) _sendIntervalMs = interval.GetInt32();
                        if (section.TryGetProperty("GenerateCount", out var count)) _generatedCount = count.GetInt32();
                        if (section.TryGetProperty("AutoStart", out var auto)) _autoStart = auto.GetBoolean();
                        if (section.TryGetProperty("ReplaceCardNumber", out var replaceCard)) _replaceCardNumber = replaceCard.GetBoolean();
                        if (section.TryGetProperty("TransactionType", out var typeStr))
                        {
                            if (Enum.TryParse<TransactionType>(typeStr.GetString(), true, out var t))
                            {
                                _selectedType = t;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[Config] Warning: Failed to load appsettings.json ({ex.Message}). Using default values.");
            }
        }

        private static void ParseCommandLineArguments(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].ToLower();
                switch (arg)
                {
                    case "--listen-port":
                    case "--port":
                    case "-p":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out int p)) _listenPort = p;
                        break;
                    case "--interval":
                    case "-i":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out int interval)) _sendIntervalMs = interval;
                        break;
                    case "--count":
                    case "-n":
                        if (i + 1 < args.Length && int.TryParse(args[++i], out int count)) _generatedCount = count;
                        break;
                    case "--type":
                    case "-t":
                        if (i + 1 < args.Length && Enum.TryParse<TransactionType>(args[++i], true, out var type)) _selectedType = type;
                        break;
                    case "--replace-card":
                    case "-rc":
                        if (i + 1 < args.Length && bool.TryParse(args[++i], out bool rc)) _replaceCardNumber = rc;
                        else _replaceCardNumber = true;
                        break;
                    case "--auto-start":
                    case "-a":
                        _autoStart = true;
                        break;
                    case "--include-chip-templates":
                        _includeChipTemplates = true;
                        break;
                }
            }
        }

        private static void AutoPreGenerateData()
        {
            if (_currentTestQueue.Count == 0 && _generatedCount > 0)
            {
                System.Console.WriteLine($"[Auto-PreGen] Building {_generatedCount:N0} {_selectedType} test items...");
                var genResult = TestDataGenerator.GenerateTestData(_selectedType, _generatedCount, skipOwnCardTemplates: _replaceCardNumber,
                                                                keepChipTemplates: _includeChipTemplates);
                _currentTestQueue = genResult.Queue;
                _dataSourceInfo = genResult.SourceDescription;
            }
        }

        private static void RenderMainMenu()
        {
            System.Console.Clear();
            System.Console.ForegroundColor = ConsoleColor.Cyan;
            System.Console.WriteLine("==========================================================================");
            System.Console.WriteLine("  CyberCOLA Console Stress Server Simulator v1.0 (.NET 10.0 High Speed)");
            System.Console.WriteLine("==========================================================================");
            System.Console.ResetColor();

            System.Console.ForegroundColor = ConsoleColor.Yellow;
            System.Console.WriteLine($" [Current Queue State] Type: {_selectedType} | Items: {_currentTestQueue.Count:N0} / {_generatedCount:N0}");
            System.Console.WriteLine($" [Queue Data Source]   {_dataSourceInfo}");
            System.Console.WriteLine($" [Server Parameters]   Port: {_listenPort} | Interval: {_sendIntervalMs}ms | Replace Card: {(_replaceCardNumber ? "ON" : "OFF")}{(_includeChipTemplates ? " | Chip templates: kept" : "")}");
            System.Console.ResetColor();
            System.Console.WriteLine("--------------------------------------------------------------------------");

            System.Console.WriteLine("  [1] 產生測試資料 (Generate Test Data)");
            System.Console.WriteLine("  [2] 啟動 Server 壓測服務 (Start Server Stress Test Service)");
            System.Console.WriteLine("  [3] 重新產生測試資料 (Regenerate Test Data / Clear & Reload Queue)");
            System.Console.WriteLine("  [4] 設定測試參數 (Set Listen Port, Interval, Card Replace Option)");
            System.Console.WriteLine("  [5] 檢視壓測結果與 Metrics 摘要 (View Last Metrics Summary)");
            System.Console.WriteLine("  [0] 離開系統 (Exit)");
            System.Console.WriteLine("--------------------------------------------------------------------------");
            System.Console.Write("請選擇操作選項 [0-5]: ");
        }

        private static void GenerateDataMenu()
        {
            System.Console.Clear();
            System.Console.WriteLine("=== [1] 產生測試資料 ===");
            System.Console.WriteLine("選擇交易種類 (Transaction Type):");
            System.Console.WriteLine(" 1. NCCC (聯信交易)");
            System.Console.WriteLine(" 2. FISC (財金交易)");
            System.Console.WriteLine(" 3. VISA (國際組織 VISA)");
            System.Console.WriteLine(" 4. MasterCard (國際組織 MasterCard)");
            System.Console.Write("請選擇 [1-4, 預設 1]: ");

            string? typeInput = System.Console.ReadLine()?.Trim();
            _selectedType = typeInput switch
            {
                "2" => TransactionType.FISC,
                "3" => TransactionType.VISA,
                "4" => TransactionType.MasterCard,
                _ => TransactionType.NCCC
            };

            System.Console.Write($"請輸入生成數量 (Generate Count) [預設 {_generatedCount:N0}]: ");
            string? countInput = System.Console.ReadLine()?.Trim();
            if (int.TryParse(countInput, out int count) && count > 0)
            {
                _generatedCount = count;
            }

            RegenerateData();
        }

        private static void RegenerateData()
        {
            System.Console.WriteLine($"\n[Generating] Building {_generatedCount:N0} {_selectedType} test items into memory queue...");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var genResult = TestDataGenerator.GenerateTestData(_selectedType, _generatedCount, skipOwnCardTemplates: _replaceCardNumber,
                                                                keepChipTemplates: _includeChipTemplates);
            _currentTestQueue = genResult.Queue;
            _dataSourceInfo = genResult.SourceDescription;
            sw.Stop();

            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine($"[Success] Pre-generated {_currentTestQueue.Count:N0} items in {sw.ElapsedMilliseconds} ms!");
            System.Console.WriteLine($"[Data Source] {_dataSourceInfo}");
            System.Console.ResetColor();
            System.Console.WriteLine("Press any key to return to Main Menu...");
            System.Console.ReadKey();
        }

        private static async Task StartServerStressTestMenuAsync(bool isAutoStarted = false)
        {
            if (_currentTestQueue.Count == 0)
            {
                AutoPreGenerateData();
            }

            using var cts = new CancellationTokenSource();
            System.Console.WriteLine("\nPress 'C' to cancel/stop server stress test anytime and proceed to Main Menu.\n");
            
            var testTask = StressEngine.RunServerStressTestAsync(
                _currentTestQueue,
                _selectedType,
                _listenPort,
                _sendIntervalMs,
                _replaceCardNumber,
                cts.Token);

            var keyMonitor = Task.Run(() =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    if (System.Console.KeyAvailable)
                    {
                        var key = System.Console.ReadKey(intercept: true);
                        if (key.Key == ConsoleKey.C)
                        {
                            System.Console.WriteLine("\n[User Cancel] Stopping server stress test execution...");
                            cts.Cancel();
                            break;
                        }
                    }
                    Thread.Sleep(100);
                }
            });

            _lastMetrics = await testTask;

            System.Console.ForegroundColor = ConsoleColor.Cyan;
            System.Console.WriteLine("\n================== Server 壓力測試結果摘要 ==================");
            System.Console.WriteLine($"  資料來源 (Data Source)   : {_dataSourceInfo}");
            System.Console.WriteLine($"  卡號替換 (Replace Card)  : {(_replaceCardNumber ? "ON (Enabled)" : "OFF (Disabled)")}");
            System.Console.WriteLine($"  總發送數量 (Total Sent)   : {_lastMetrics.TotalSent:N0}");
            System.Console.WriteLine($"  成功接收數量 (Success)    : {_lastMetrics.TotalSuccess:N0}");
            System.Console.WriteLine($"  失敗數量 (Failed)        : {_lastMetrics.TotalFailed:N0}");
            System.Console.WriteLine($"  總耗時 (Elapsed Seconds) : {_lastMetrics.ElapsedSeconds:F2} 秒");
            System.Console.WriteLine($"  平均 TPS (Throughput)    : {_lastMetrics.Tps:F2} msg/sec");
            System.Console.WriteLine("============================================================");
            System.Console.ResetColor();

            if (!isAutoStarted)
            {
                System.Console.WriteLine("\nPress any key to return to Main Menu...");
                System.Console.ReadKey();
            }
            else
            {
                System.Console.WriteLine("\n[Auto-Start] Proceeding directly to Main Menu in 2 seconds...");
                await Task.Delay(2000);
            }
        }

        private static void ConfigureParametersMenu()
        {
            System.Console.Clear();
            System.Console.WriteLine("=== [4] 設定測試參數 ===");

            System.Console.Write($"伺服器監聽埠號 (Listen Port) [目前: {_listenPort}]: ");
            string? portInput = System.Console.ReadLine()?.Trim();
            if (int.TryParse(portInput, out int p)) _listenPort = p;

            System.Console.Write($"每筆發送間隔毫秒 (Interval ms, 0=最大極速) [目前: {_sendIntervalMs}]: ");
            string? intInput = System.Console.ReadLine()?.Trim();
            if (int.TryParse(intInput, out int i)) _sendIntervalMs = i;

            System.Console.Write($"是否開啟卡號動態替換 (Replace Card Number Y/N) [目前: {(_replaceCardNumber ? "Y" : "N")}]: ");
            string? rcInput = System.Console.ReadLine()?.Trim()?.ToUpper();
            if (!string.IsNullOrEmpty(rcInput))
            {
                _replaceCardNumber = (rcInput == "Y" || rcInput == "YES" || rcInput == "TRUE");
            }

            System.Console.ForegroundColor = ConsoleColor.Green;
            System.Console.WriteLine("\n[Success] Configuration updated successfully!");
            System.Console.ResetColor();
            System.Console.WriteLine("Press any key to return to Main Menu...");
            System.Console.ReadKey();
        }

        private static void ViewMetricsSummary()
        {
            System.Console.Clear();
            System.Console.WriteLine("=== [5] 檢視最後一次壓測報告 ===");
            if (_lastMetrics == null)
            {
                System.Console.WriteLine("尚未進行過任何壓力測試。");
            }
            else
            {
                System.Console.WriteLine($"  資料來源   : {_dataSourceInfo}");
                System.Console.WriteLine($"  卡號替換   : {(_replaceCardNumber ? "ON (Enabled)" : "OFF (Disabled)")}");
                System.Console.WriteLine($"  發送總數   : {_lastMetrics.TotalSent:N0}");
                System.Console.WriteLine($"  成功數量   : {_lastMetrics.TotalSuccess:N0}");
                System.Console.WriteLine($"  失敗數量   : {_lastMetrics.TotalFailed:N0}");
                System.Console.WriteLine($"  總耗時     : {_lastMetrics.ElapsedSeconds:F2} 秒");
                System.Console.WriteLine($"  平均 TPS   : {_lastMetrics.Tps:F2} msg/sec");
            }
            System.Console.WriteLine("\nPress any key to return to Main Menu...");
            System.Console.ReadKey();
        }
    }
}
