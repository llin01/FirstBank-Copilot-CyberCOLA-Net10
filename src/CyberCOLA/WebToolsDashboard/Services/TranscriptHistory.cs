using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Com.Cybersoft4u.Test.Web.WebToolsDashboard.Services;

public sealed record HistoryEntry(
    string Path,
    string FileName,
    DateTime WrittenAt,
    int ExitCode,
    AuthTestSummary Summary,
    IReadOnlyList<FlowStage> Stages);

/// <summary>
/// Saved Invoke-AuthTestUnattended transcripts (LogFiles\AuthTest\*.txt), parsed once per file version.
/// </summary>
public sealed partial class TranscriptHistory
{
    private const int MaxEntries = 100;

    private readonly RepoPaths _paths;
    private readonly ConcurrentDictionary<string, HistoryEntry> _cache = new(StringComparer.OrdinalIgnoreCase);

    public TranscriptHistory(RepoPaths paths)
    {
        _paths = paths;
    }

    [GeneratedRegex(@"^[A-Z]+_\d{8}_\d{6}\.txt$")]
    private static partial Regex TranscriptName();

    public IReadOnlyList<HistoryEntry> List()
    {
        var dir = new DirectoryInfo(_paths.AuthTestLogDir);
        if (!dir.Exists)
        {
            return [];
        }

        return dir.EnumerateFiles("*.txt")
            .Where(f => TranscriptName().IsMatch(f.Name))
            .OrderByDescending(f => f.LastWriteTime)
            .Take(MaxEntries)
            .Select(Load)
            .ToList();
    }

    public HistoryEntry? Get(string fileName)
    {
        // Only a bare transcript file name: no path can be smuggled in through the URL.
        if (!TranscriptName().IsMatch(fileName))
        {
            return null;
        }

        var file = new FileInfo(Path.Combine(_paths.AuthTestLogDir, fileName));
        return file.Exists ? Load(file) : null;
    }

    public IReadOnlyList<string> ReadLines(HistoryEntry entry) =>
        File.ReadLines(entry.Path).Select(OutputMasker.Clean).ToList();

    private HistoryEntry Load(FileInfo file)
    {
        if (_cache.TryGetValue(file.FullName, out var cached) && cached.WrittenAt == file.LastWriteTime)
        {
            return cached;
        }

        var lines = File.ReadLines(file.FullName).Select(OutputMasker.Clean).ToList();
        var summary = TranscriptParser.ParseSummary(lines);
        var exitCode = InferExitCode(summary);
        var entry = new HistoryEntry(file.FullName, file.Name, file.LastWriteTime, exitCode, summary,
            TranscriptParser.ParseStages(lines, finished: true, exitCode));
        _cache[file.FullName] = entry;
        return entry;
    }

    /// <summary>
    /// The runner's exit code is not written into the transcript, but each failure leaves its own note.
    /// </summary>
    internal static int InferExitCode(AuthTestSummary summary)
    {
        if (summary.Notes.Any(n => n.Contains("killed", StringComparison.Ordinal)))
        {
            return 3;
        }

        if (summary.Notes.Any(n => n.Contains("did not reach the Control Menu", StringComparison.Ordinal)))
        {
            return 1;
        }

        if (summary.Notes.Any(n => n.Contains("not printed", StringComparison.Ordinal)) || !summary.HasStatistics)
        {
            return 2;
        }

        return 0;
    }

    public static string FormatTime(HistoryEntry entry) =>
        entry.Summary.RunStart ?? entry.WrittenAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
}
