using System.Globalization;
using System.Text.RegularExpressions;

namespace Com.Cybersoft4u.Test.Web.WebToolsDashboard.Services;

public enum StageStatus
{
    Pending,
    Running,
    Done,
    Failed,
}

public sealed record FlowStage(string Title, StageStatus Status, string? Time);

public sealed record ResponseCodeRow(string Code, int Count, double Percent, bool Success, string Description);

public sealed record MtiRow(string Mti, int Total, int Success, int Failed);

public sealed class AuthTestSummary
{
    public string? Network { get; set; }
    public string? RunStart { get; set; }
    public int? Requests { get; set; }
    public string? RequestMix { get; set; }
    public int? Responses { get; set; }
    public int? Success { get; set; }
    public double? SuccessPercent { get; set; }
    public int? Failed { get; set; }
    public int? Unanswered { get; set; }

    // FISC prints a different block: what the GW saw instead of what the simulator sent.
    public int? MessagesSeenByGw { get; set; }
    public int? ResponsesProduced { get; set; }
    public int? Delivered { get; set; }
    public int? Dropped { get; set; }
    public int? NoAnswerFromAuth { get; set; }
    public int? NotDecodable { get; set; }

    public List<ResponseCodeRow> ResponseCodes { get; } = [];
    public List<MtiRow> Mtis { get; } = [];

    // Warnings and errors worth showing next to the numbers (credit warning, chain not ready, kill).
    public List<string> Notes { get; } = [];
    public string? TranscriptPath { get; set; }

    public bool HasStatistics => Success.HasValue;

    /// <summary>Requests that got no answer; for FISC the GW-side counters.</summary>
    public int? TotalUnanswered => Unanswered ?? (NoAnswerFromAuth.HasValue || Dropped.HasValue
        ? (NoAnswerFromAuth ?? 0) + (Dropped ?? 0)
        : null);

    public int? TotalRequests => Requests ?? MessagesSeenByGw;
}

/// <summary>
/// Reads the output of Invoke-AuthTestUnattended.ps1 (streamed with -StreamOutput, or a saved transcript)
/// into the run's stages and its transaction summary. Pure functions, so the format is unit tested.
/// </summary>
public static partial class TranscriptParser
{
    [GeneratedRegex(@"\[Step (\d+)/(\d+)\]\s*(.+?)\s*$")]
    private static partial Regex StepHeader();

    [GeneratedRegex(@"^\[Unattended (\d\d:\d\d:\d\d)\]\s*(.*)$")]
    private static partial Regex UnattendedNote();

    [GeneratedRegex(@"^\[\d{4}-\d\d-\d\d (\d\d:\d\d:\d\d)\]")]
    private static partial Regex LogTime();

    [GeneratedRegex(@"Run start (\d{4}-\d\d-\d\d \d\d:\d\d:\d\d), network (\w+)")]
    private static partial Regex RunStart();

    [GeneratedRegex(@"^\s*(Requests from simulator|Responses to simulator|Success|Failed|Unanswered requests|Messages seen by GW|Responses produced|Delivered to simulator|Dropped \(no return channel\)|No answer from Auth|Not decodable by GW)\s*:\s*(\d+)(?:\s+([\d.]+)%)?(?:\s+\((.*)\))?\s*$")]
    private static partial Regex Counter();

    [GeneratedRegex(@"^\s*([0-9A-Z]{2})\s+(\d+)\s+([\d.]+)%\s+(Success|Failed)\s+(.*?)\s*$")]
    private static partial Regex ResponseCode();

    [GeneratedRegex(@"^\s*(0\d{3})\s+(\d+)\s+(\d+)\s+(\d+)\s*$")]
    private static partial Regex MtiLine();

    [GeneratedRegex(@"^Transcript:\s*(.+?)\s*$")]
    private static partial Regex TranscriptLine();

    /// <summary>
    /// Stages of an auth test run. <paramref name="finished"/> and <paramref name="exitCode"/> describe the
    /// dashboard job (null exit code while it runs); for a saved transcript pass finished = true.
    /// </summary>
    public static List<FlowStage> ParseStages(IReadOnlyList<string> lines, bool finished, int? exitCode)
    {
        var seen = new List<(string Title, string? Time)>();
        var stepTitles = new SortedDictionary<int, string>();
        var totalSteps = 0;
        string? lastTime = null;
        var reachedMenu = false;
        var trafficSeen = false;
        var summarySeen = false;
        string? stopTime = null;

        // A [Step n/N] header carries no time; the first timestamped line after it does.
        var untimedStep = -1;

        foreach (var line in lines)
        {
            var log = LogTime().Match(line);
            if (log.Success)
            {
                lastTime = log.Groups[1].Value;
                if (untimedStep >= 0)
                {
                    seen[untimedStep] = (seen[untimedStep].Title, lastTime);
                    untimedStep = -1;
                }
            }

            var note = UnattendedNote().Match(line);
            if (note.Success)
            {
                lastTime = note.Groups[1].Value;
                if (untimedStep >= 0)
                {
                    seen[untimedStep] = (seen[untimedStep].Title, lastTime);
                    untimedStep = -1;
                }

                var text = note.Groups[2].Value;
                if (text.StartsWith("Run start", StringComparison.Ordinal))
                {
                    seen.Add(("開始", lastTime));
                }
                else if (text.StartsWith("Control Menu reached", StringComparison.Ordinal))
                {
                    reachedMenu = true;
                }
                else if (text.StartsWith("Letting traffic run", StringComparison.Ordinal) && !trafficSeen)
                {
                    trafficSeen = true;
                    seen.Add(("跑流量", lastTime));
                }
                else if (text.StartsWith("Test script exit code", StringComparison.Ordinal))
                {
                    stopTime = lastTime;
                }

                continue;
            }

            var step = StepHeader().Match(line);
            if (step.Success)
            {
                var number = int.Parse(step.Groups[1].Value, CultureInfo.InvariantCulture);
                totalSteps = int.Parse(step.Groups[2].Value, CultureInfo.InvariantCulture);
                if (!stepTitles.ContainsKey(number))
                {
                    stepTitles[number] = step.Groups[3].Value;
                    seen.Add(($"{number}. {step.Groups[3].Value}", null));
                    untimedStep = seen.Count - 1;
                }

                continue;
            }

            // The summary's own banner ("=== Transaction Summary (NCCC) ==="), not the Control Menu entry
            // "[5] ... (Transaction Summary: success / failed / reason)" printed before any traffic.
            if (!summarySeen && (line.Contains("Transaction Summary (", StringComparison.Ordinal) || Counter().IsMatch(line)))
            {
                summarySeen = true;
                seen.Add(("取統計", lastTime));
            }
        }

        if (stopTime is not null)
        {
            seen.Add(("停止全部", stopTime));
        }

        // Stages not reached yet, in the order the run goes through them.
        var pending = new List<string>();
        if (seen.Count == 0)
        {
            pending.Add("開始");
        }

        for (var n = 1; n <= totalSteps; n++)
        {
            if (!stepTitles.ContainsKey(n))
            {
                pending.Add($"{n}. …");
            }
        }

        if (totalSteps == 0)
        {
            pending.Add("啟動授權鏈");
        }

        if (!trafficSeen)
        {
            pending.Add("跑流量");
        }

        if (!summarySeen)
        {
            pending.Add("取統計");
        }

        if (stopTime is null)
        {
            pending.Add("停止全部");
        }

        // The stage a failed run broke in. "Stop everything" always follows a failure, so it is never the culprit:
        // before the Control Menu it is the last chain step reached, after it the last stage before stopping.
        var failedIndex = -1;
        if (finished && exitCode is not null and not 0)
        {
            for (var i = seen.Count - 1; i >= 0; i--)
            {
                var title = seen[i].Title;
                var isChainStep = title == "開始" || char.IsDigit(title[0]);
                if (title != "停止全部" && (reachedMenu || isChainStep))
                {
                    failedIndex = i;
                    break;
                }
            }
        }

        var stages = new List<FlowStage>(seen.Count + pending.Count);
        for (var i = 0; i < seen.Count; i++)
        {
            var status = i == failedIndex ? StageStatus.Failed
                : !finished && i == seen.Count - 1 ? StageStatus.Running
                : StageStatus.Done;
            stages.Add(new FlowStage(seen[i].Title, status, seen[i].Time));
        }

        // Stages never reached go before "stop everything", which always ends the run.
        var insertAt = stages.Count > 0 && stages[^1].Title == "停止全部" ? stages.Count - 1 : stages.Count;
        stages.InsertRange(insertAt, pending.Select(t => new FlowStage(t, StageStatus.Pending, null)));
        return stages;
    }

    public static AuthTestSummary ParseSummary(IEnumerable<string> lines)
    {
        var s = new AuthTestSummary();
        foreach (var line in lines)
        {
            var transcript = TranscriptLine().Match(line);
            if (transcript.Success)
            {
                s.TranscriptPath = transcript.Groups[1].Value;
                continue;
            }

            var note = UnattendedNote().Match(line);
            if (note.Success)
            {
                var text = note.Groups[2].Value;
                var start = RunStart().Match(text);
                if (start.Success)
                {
                    s.RunStart = start.Groups[1].Value;
                    s.Network = start.Groups[2].Value;
                }
                else if (text.StartsWith("WARNING", StringComparison.Ordinal)
                    || text.Contains("did not", StringComparison.Ordinal)
                    || text.Contains("not printed", StringComparison.Ordinal)
                    || text.Contains("killed", StringComparison.Ordinal)
                    || text.StartsWith("Credit check skipped", StringComparison.Ordinal)
                    || text.StartsWith("Still running", StringComparison.Ordinal))
                {
                    s.Notes.Add(text);
                }

                continue;
            }

            var counter = Counter().Match(line);
            if (counter.Success)
            {
                var value = int.Parse(counter.Groups[2].Value, CultureInfo.InvariantCulture);
                switch (counter.Groups[1].Value)
                {
                    case "Requests from simulator":
                        s.Requests = value;
                        s.RequestMix = counter.Groups[4].Success ? counter.Groups[4].Value : null;
                        break;
                    case "Responses to simulator": s.Responses = value; break;
                    case "Success":
                        s.Success = value;
                        s.SuccessPercent = ParsePercent(counter.Groups[3]);
                        break;
                    case "Failed": s.Failed = value; break;
                    case "Unanswered requests": s.Unanswered = value; break;
                    case "Messages seen by GW": s.MessagesSeenByGw = value; break;
                    case "Responses produced": s.ResponsesProduced = value; break;
                    case "Delivered to simulator": s.Delivered = value; break;
                    case "Dropped (no return channel)": s.Dropped = value; break;
                    case "No answer from Auth": s.NoAnswerFromAuth = value; break;
                    case "Not decodable by GW": s.NotDecodable = value; break;
                }

                continue;
            }

            var rc = ResponseCode().Match(line);
            if (rc.Success)
            {
                s.ResponseCodes.Add(new ResponseCodeRow(
                    rc.Groups[1].Value,
                    int.Parse(rc.Groups[2].Value, CultureInfo.InvariantCulture),
                    double.Parse(rc.Groups[3].Value, CultureInfo.InvariantCulture),
                    rc.Groups[4].Value == "Success",
                    rc.Groups[5].Value));
                continue;
            }

            var mti = MtiLine().Match(line);
            if (mti.Success)
            {
                s.Mtis.Add(new MtiRow(
                    mti.Groups[1].Value,
                    int.Parse(mti.Groups[2].Value, CultureInfo.InvariantCulture),
                    int.Parse(mti.Groups[3].Value, CultureInfo.InvariantCulture),
                    int.Parse(mti.Groups[4].Value, CultureInfo.InvariantCulture)));
            }
        }

        return s;
    }

    private static double? ParsePercent(Group g) =>
        g.Success ? double.Parse(g.Value, CultureInfo.InvariantCulture) : null;
}
