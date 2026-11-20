using Com.Cybersoft4u.Test.Web.WebToolsDashboard.Services;
using NUnit.Framework;

namespace Com.Cybersoft4u.CyberCOLA.Test
{
    // Fixtures are trimmed copies of real Invoke-AuthTestUnattended transcripts (2026-11-11 runs).
    [TestFixture]
    public class WebToolsDashboardTest
    {
        private static readonly string[] NcccTranscript =
        [
            "[Unattended 11:16:09] WARNING: 1/41 pool cards are below 10000 available (VISA 1/26, MC 0/10, JCB 0/5).",
            "[Unattended 11:16:09] Run start 2026-11-11 11:16:09, network NCCC",
            " [Step 1/6] Checking Database Connection...",
            " [Step 2/6] Starting ServiceRouter...",
            "[2026-11-11 11:16:38] [INFO]    Launching ServiceRouter from: E:\\publish\\ServiceRouter.exe",
            " [Step 3/6] Starting CyberGWNCCC...",
            "[2026-11-11 11:16:40] [INFO]    Launching CyberGWNCCC",
            " [Step 4/6] Starting HSM Simulator (port 8500)...",
            "[2026-11-11 11:16:44] [SUCCESS] HSM Simulator is listening on port 8500.",
            " [Step 5/6] Starting CyberAuthNCCC...",
            "[2026-11-11 11:16:44] [INFO]    Launching CyberAuthNCCC",
            " [Step 6/6] Starting ConsoleStressSimulator (NCCC, listen port 14801)...",
            "[2026-11-11 11:16:52] [INFO]    Launching ConsoleStressSimulator",
            "  [5] 交易結果統計 (Transaction Summary: success / failed / reason)",
            "[Unattended 11:17:00] Control Menu reached.",
            "[Unattended 11:17:00] Letting traffic run for 90 s.",
            "========================= Transaction Summary (NCCC) =========================",
            "  Requests from simulator :    400  (0200 x124, 0220 x144, 0420 x120, 0620 x12)",
            "  Responses to simulator  :    400",
            "  Success                 :    396   99.0%",
            "  Failed                  :      4    1.0%",
            "  Unanswered requests     :      0",
            "  00      368   92.0%  Success  Approved",
            "  85       28    7.0%  Success  No reason to decline",
            "  05        4    1.0%  Failed   Do not honor",
            "  0210     124      120       4",
            "  0230     144      144       0",
            "[2026-11-11 11:18:34] [SUCCESS] All started processes stopped.",
            "[Unattended 11:18:35] Test script exit code: 0",
            "Transcript: E:\\LogFiles\\AuthTest\\NCCC_20261111_111608.txt",
        ];

        private static readonly string[] FiscSummary =
        [
            "[Unattended 11:35:16] Run start 2026-11-11 11:35:16, network FISC",
            "  Messages seen by GW         :    400  (requests to Auth 400, answered 96 by GW 0, not decodable 0, no reply expected 0)",
            "  Responses produced          :    400  (from Auth 400, from GW 0)",
            "  Success                     :    386   96.5%",
            "  Failed                      :     14    3.5%",
            "  Delivered to simulator      :    400",
            "  Dropped (no return channel) :      0",
            "  No answer from Auth         :      0",
            "  Not decodable by GW         :      0",
            "  00      386   96.5%  Success  Approved (from Auth)",
            "  05       14    3.5%  Failed   Do not honor (from Auth)",
        ];

        private static readonly string[] FailedAtDbCheck =
        [
            "[Unattended 11:19:11] Run start 2026-11-11 11:19:11, network FISC",
            " [Step 1/6] Checking Database Connection...",
            "[VPN] Tunnel is up but not Connected after 120 s. Status: ''.",
            "[Unattended 11:24:12] The chain did not reach the Control Menu (test script exited: False). See the output above.",
            "[Unattended 11:25:42] Test script did not stop within 90 s and was killed.",
            "[Unattended 11:25:42] Test script exit code: -1",
        ];

        [Test]
        public void ParseSummary_NcccTranscript_ReadsCountersCodesAndMtis()
        {
            var s = TranscriptParser.ParseSummary(NcccTranscript);

            Assert.AreEqual("NCCC", s.Network);
            Assert.AreEqual("2026-11-11 11:16:09", s.RunStart);
            Assert.AreEqual(400, s.TotalRequests);
            Assert.AreEqual("0200 x124, 0220 x144, 0420 x120, 0620 x12", s.RequestMix);
            Assert.AreEqual(396, s.Success);
            Assert.AreEqual(99.0, s.SuccessPercent);
            Assert.AreEqual(4, s.Failed);
            Assert.AreEqual(0, s.TotalUnanswered);
            Assert.AreEqual(3, s.ResponseCodes.Count);
            Assert.AreEqual(new ResponseCodeRow("05", 4, 1.0, false, "Do not honor"), s.ResponseCodes[2]);
            Assert.AreEqual(new MtiRow("0210", 124, 120, 4), s.Mtis[0]);
            Assert.AreEqual(@"E:\LogFiles\AuthTest\NCCC_20261111_111608.txt", s.TranscriptPath);
            Assert.AreEqual(1, s.Notes.Count, "The credit warning is kept as a note.");
            Assert.AreEqual(0, TranscriptHistory.InferExitCode(s));
        }

        [Test]
        public void ParseSummary_FiscFormat_UsesGatewayCounters()
        {
            var s = TranscriptParser.ParseSummary(FiscSummary);

            Assert.AreEqual(400, s.TotalRequests, "FISC has no simulator request count; the GW count stands in.");
            Assert.AreEqual(96.5, s.SuccessPercent);
            Assert.AreEqual(0, s.TotalUnanswered, "No answer from Auth + Dropped.");
            Assert.AreEqual(2, s.ResponseCodes.Count);
            Assert.AreEqual(0, s.Mtis.Count);
        }

        [Test]
        public void ParseStages_CompletedRun_AllStagesDoneInOrderWithTimes()
        {
            var stages = TranscriptParser.ParseStages(NcccTranscript, finished: true, exitCode: 0);

            Assert.That(
                stages.Select(s => s.Title).ToArray(),
                Is.EqualTo(new[] { "開始", "1. Checking Database Connection...", "2. Starting ServiceRouter...", "3. Starting CyberGWNCCC...",
                        "4. Starting HSM Simulator (port 8500)...", "5. Starting CyberAuthNCCC...",
                        "6. Starting ConsoleStressSimulator (NCCC, listen port 14801)...", "跑流量", "取統計", "停止全部" }));
            Assert.IsTrue(stages.All(s => s.Status == StageStatus.Done));

            // A step takes the time of the first timestamped line after its header.
            Assert.AreEqual("11:16:38", stages[2].Time);
            Assert.AreEqual("11:18:35", stages[^1].Time);
        }

        [Test]
        public void ParseStages_RunInProgress_LastSeenStageRunningRestPending()
        {
            var partial = NcccTranscript.Take(8).ToArray();   // up to the HSM step header
            var stages = TranscriptParser.ParseStages(partial, finished: false, exitCode: null);

            Assert.AreEqual(StageStatus.Running, stages.Single(s => s.Title.StartsWith("4.", StringComparison.Ordinal)).Status);
            Assert.AreEqual(StageStatus.Pending, stages.Single(s => s.Title == "5. …").Status);
            Assert.AreEqual(StageStatus.Pending, stages.Single(s => s.Title == "跑流量").Status);
            Assert.AreEqual(StageStatus.Done, stages.Single(s => s.Title.StartsWith("3.", StringComparison.Ordinal)).Status);
        }

        [Test]
        public void ParseStages_ChainFailedAtDbCheck_MarksThatStepNotTheStop()
        {
            var stages = TranscriptParser.ParseStages(FailedAtDbCheck, finished: true, exitCode: 3);

            Assert.AreEqual(StageStatus.Failed, stages.Single(s => s.Title.StartsWith("1.", StringComparison.Ordinal)).Status);
            Assert.AreEqual(StageStatus.Done, stages.Single(s => s.Title == "停止全部").Status);
            Assert.AreEqual(StageStatus.Pending, stages.Single(s => s.Title == "跑流量").Status);
            Assert.AreEqual("停止全部", stages[^1].Title, "Unreached stages go before the final stop.");
            Assert.AreEqual(3, TranscriptHistory.InferExitCode(TranscriptParser.ParseSummary(FailedAtDbCheck)));
        }

        [Test]
        public void CommandBuilder_AuthTest_PassesBoolAsLiteralAndStreamsOutput()
        {
            var tool = ToolCatalog.Find("auth-test")!;
            var values = tool.Parameters.ToDictionary(p => p.Name, p => p.Default);
            values["Network"] = "FISC";
            values["SimReplaceCard"] = "false";

            var command = CommandBuilder.Build(tool, @"E:\repo\tools\Invoke-AuthTestUnattended.ps1", values);

            Assert.That(command, Does.Contain(@"& 'E:\repo\tools\Invoke-AuthTestUnattended.ps1' -Network 'FISC'"));
            Assert.That(command, Does.Contain("-SimReplaceCard:$false"));
            Assert.That(command, Does.Not.Contain("-SimIncludeChip"), "A switch left off is omitted.");
            Assert.That(command, Does.Contain("-StreamOutput"));
            Assert.That(command, Does.EndWith("; exit $LASTEXITCODE"));
        }

        [TestCase("Network", "NCCC; Remove-Item C:\\")]
        [TestCase("TrafficWaitSec", "90; calc")]
        [TestCase("TrafficWaitSec", "5")]
        [TestCase("SimReplaceCard", "$true")]
        public void CommandBuilder_ValueOutsideDefinition_Throws(string name, string value)
        {
            var tool = ToolCatalog.Find("auth-test")!;
            var values = tool.Parameters.ToDictionary(p => p.Name, p => p.Default);
            values[name] = value;

            Assert.Throws<ArgumentException>(() => CommandBuilder.Build(tool, @"E:\repo\tools\x.ps1", values));
        }

        [Test]
        public void CommandBuilder_ScriptPathWithQuote_IsEscaped()
        {
            var tool = ToolCatalog.Find("health")!;
            var command = CommandBuilder.Build(tool, @"E:\it's\tools\Check-SystemHealth.ps1", new Dictionary<string, string>());
            Assert.That(command, Does.Contain(@"& 'E:\it''s\tools\Check-SystemHealth.ps1'"));
        }

        [Test]
        public void ToolCatalog_NoToolWritesData()
        {
            // Version 1 is read-only: no -Apply parameter and no write-capable SQL script.
            foreach (var tool in ToolCatalog.All)
            {
                Assert.IsFalse(tool.Parameters.Any(p => p.Name == "Apply"), tool.Id);
                if (tool.Script == "Invoke-CyberCOLASql.ps1")
                {
                    Assert.IsTrue(tool.Parameters.Single(p => p.Name == "Name").Choices!.All(c => c.StartsWith("Preview-", StringComparison.Ordinal)), tool.Id);
                }
            }

            Assert.AreEqual(ToolCatalog.All.Count, ToolCatalog.All.Select(t => t.Id).Distinct().Count(), "Tool ids must be unique.");
        }

        [Test]
        public void ToolCatalog_EveryScriptExistsInTools()
        {
            var tools = FindRepoTools();
            foreach (var tool in ToolCatalog.All)
            {
                FileAssert.Exists(Path.Combine(tools, tool.Script), tool.Id);
            }
        }

        [TestCase("Card 4111111111111111 approved", "Card ****1111 approved")]
        [TestCase("PAN=5555555555554444;", "PAN=****4444;")]
        [TestCase("Server=db;User Id=sa;Password=Secret123;", "Server=db;User Id=sa;Password=***;")]
        [TestCase("STAN 000123 RRN 123456789012", "STAN 000123 RRN 123456789012")]
        [TestCase("\u001b[91m4111111111111111\u001b[0m", "\u001b[91m****1111\u001b[0m")]
        [TestCase("\u001b[32mOK\u001b[0m", "\u001b[32mOK\u001b[0m")]
        [TestCase("\u001b[2K\u001b[1GDone", "Done")]
        public void OutputMasker_Clean_MasksSensitiveDataAndKeepsOnlyColours(string input, string expected)
        {
            Assert.AreEqual(expected, OutputMasker.Clean(input));
        }

        [Test]
        public void OutputMasker_StripAnsi_LeavesPlainTextForParsers()
        {
            Assert.AreEqual("[Unattended 11:16:09] Run start", OutputMasker.StripAnsi("\u001b[96m[Unattended 11:16:09] Run start\u001b[0m"));
        }

        [Test]
        public void AnsiText_ToHtml_ColoursSpansAndEncodesText()
        {
            var html = AnsiText.ToHtml(["\u001b[91m<b>low</b> & red\u001b[0m tail", "plain", "\u001b[1;92mbold green"]);

            Assert.AreEqual(
                "<span class=\"ansi-91\">&lt;b&gt;low&lt;/b&gt; &amp; red</span> tail\n" +
                "plain\n" +
                "<span class=\"ansi-92 ansi-bold\">bold green</span>\n",
                html);
        }

        [Test]
        public void CommandBuilder_RunTool_ShadowsWriteHostOnlyForCapturedRuns()
        {
            var run = CommandBuilder.Build(ToolCatalog.Find("credit-preview")!, @"E:\repo\tools\Invoke-CyberCOLASql.ps1",
                new Dictionary<string, string>());
            var window = CommandBuilder.Build(ToolCatalog.Find("menu-main")!, @"E:\repo\tools\COLAMainMenu.ps1",
                new Dictionary<string, string>());

            Assert.That(run, Does.Contain("function global:Write-Host"));
            Assert.That(run, Does.Not.Contain("OutputRendering"), "Ansi rendering would colour Format-Table text.");
            Assert.That(window, Does.Not.Contain("Write-Host"), "A real console window keeps its own colours.");
            Assert.That(window, Does.Not.Contain("\""), "OpenWindow wraps the command in double quotes.");
        }

        [Test]
        public void AnsiWriteHost_RedirectedPwsh_EmitsColourCodes()
        {
            // The real point of the proxy: a redirected Write-Host -ForegroundColor Red must reach us as ESC[91m.
            var psi = new System.Diagnostics.ProcessStartInfo("pwsh")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
            };
            psi.ArgumentList.Add("-NoProfile");
            psi.ArgumentList.Add("-NonInteractive");
            psi.ArgumentList.Add("-Command");
            // The table line guards Invoke-CyberCOLASql: it finds its own dashed separator line in Format-Table
            // text, which must stay free of colour codes.
            psi.ArgumentList.Add(CommandBuilder.AnsiWriteHost +
                "Write-Host 'low credit' -ForegroundColor Red; Write-Host 'a' 'b'; 'p' | Write-Host -ForegroundColor Green; " +
                "Write-Host 'x ' -ForegroundColor Yellow -NoNewline; Write-Host 'y'; " +
                "Write-Host ((([pscustomobject]@{ col = 1 }) | Format-Table | Out-String).Trim())");

            System.Diagnostics.Process? process;
            try
            {
                process = System.Diagnostics.Process.Start(psi);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                Assert.Ignore("pwsh is not installed on this machine.");
                return;
            }

            var output = process!.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.TrimEnd('\r')).ToArray();
            Assert.That(
                lines,
                Is.EqualTo(new[] { "\u001b[91mlow credit\u001b[0m", "a b", "\u001b[92mp\u001b[0m", "\u001b[93mx \u001b[0my", "col", "---", "  1" }));
        }

        // From this source file rather than the test output folder, which --artifacts-path can put outside the repo.
        private static string FindRepoTools([System.Runtime.CompilerServices.CallerFilePath] string sourceFile = "")
        {
            for (var dir = new DirectoryInfo(Path.GetDirectoryName(sourceFile)!); dir is not null; dir = dir.Parent)
            {
                var tools = Path.Combine(dir.FullName, "tools");
                if (File.Exists(Path.Combine(dir.FullName, "CyberCOLA.sln")) && Directory.Exists(tools))
                {
                    return tools;
                }
            }

            Assert.Fail("Repository root not found above the test directory.");
            return string.Empty;
        }
    }
}
