using System.Diagnostics;
using System.Text;

namespace Com.Cybersoft4u.Test.Web.WebToolsDashboard.Services;

public enum JobStatus
{
    Running,
    Succeeded,
    Failed,
    Cancelled,
}

public sealed class Job
{
    // A runaway tool must not grow the process without bound; the tail is what matters.
    private const int MaxLines = 20_000;

    private readonly List<string> _lines = [];
    private readonly object _sync = new();

    internal Job(int id, ToolDefinition tool, string display)
    {
        Id = id;
        Tool = tool;
        Display = display;
        StartedAt = DateTime.Now;
    }

    public int Id { get; }
    public ToolDefinition Tool { get; }
    public string Display { get; }
    public DateTime StartedAt { get; }
    public DateTime? EndedAt { get; internal set; }
    public int? ExitCode { get; internal set; }
    public JobStatus Status { get; internal set; } = JobStatus.Running;
    public bool IsRunning => Status == JobStatus.Running;

    public string ExitMeaning => ExitCode is null ? string.Empty
        : Tool.ExitCodes.TryGetValue(ExitCode.Value, out var text) ? text
        : ExitCode == 0 ? "完成" : "失敗";

    internal Process? Process { get; set; }

    public IReadOnlyList<string> Lines()
    {
        lock (_sync)
        {
            return _lines.ToArray();
        }
    }

    internal void Append(string line)
    {
        lock (_sync)
        {
            _lines.Add(OutputMasker.Clean(line));
            if (_lines.Count > MaxLines)
            {
                _lines.RemoveRange(0, _lines.Count - MaxLines);
            }
        }
    }
}

/// <summary>
/// Runs catalog tools as pwsh child processes and keeps their (masked) output. Tools that drive the
/// authorization chain are exclusive: they start and stop the same services, so two at once would collide.
/// </summary>
public sealed class JobRunner : IDisposable
{
    private const int MaxJobs = 30;

    private readonly RepoPaths _paths;
    private readonly ILogger<JobRunner> _logger;
    private readonly List<Job> _jobs = [];
    private readonly object _sync = new();
    private int _nextId;

    public JobRunner(RepoPaths paths, ILogger<JobRunner> logger)
    {
        _paths = paths;
        _logger = logger;
    }

    public event Action? JobsChanged;

    public IReadOnlyList<Job> Jobs()
    {
        lock (_sync)
        {
            return _jobs.ToArray();
        }
    }

    public Job? Find(int id)
    {
        lock (_sync)
        {
            return _jobs.FirstOrDefault(j => j.Id == id);
        }
    }

    public Job Start(ToolDefinition tool, IReadOnlyDictionary<string, string> values)
    {
        if (tool.Kind != ToolKind.Run)
        {
            throw new InvalidOperationException($"{tool.Title} is an interactive menu; open it in a window.");
        }

        var command = CommandBuilder.Build(tool, _paths.Script(tool.Script), values);
        Job job;
        lock (_sync)
        {
            if (tool.Exclusive && _jobs.Any(j => j.IsRunning && j.Tool.Exclusive))
            {
                throw new InvalidOperationException("已有授權測試在執行，請等它結束或先中止。");
            }

            job = new Job(++_nextId, tool, Describe(tool, values));
            _jobs.Insert(0, job);

            // Drop the oldest finished jobs; a running one is kept even past the limit.
            for (var i = _jobs.Count - 1; i >= 0 && _jobs.Count > MaxJobs; i--)
            {
                if (!_jobs[i].IsRunning)
                {
                    _jobs.RemoveAt(i);
                }
            }
        }

        var psi = new ProcessStartInfo("pwsh")
        {
            WorkingDirectory = _paths.Root,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        // ArgumentList quotes each argument itself, so the command text is passed as one argument.
        psi.ArgumentList.Add("-NoProfile");
        psi.ArgumentList.Add("-NonInteractive");
        psi.ArgumentList.Add("-Command");
        psi.ArgumentList.Add(command);

        var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) job.Append(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) job.Append("ERR: " + e.Data); };
        process.Exited += (_, _) => OnExited(job);

        // Set before Start: Exited can fire before Start returns for a command that ends at once.
        job.Process = process;
        try
        {
            process.Start();
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            job.Process = null;
            job.Append($"無法啟動 pwsh：{ex.Message}");
            job.Status = JobStatus.Failed;
            job.EndedAt = DateTime.Now;
            JobsChanged?.Invoke();
            return job;
        }

        // Closed at once: a script that still asks something reads end-of-input instead of waiting forever.
        process.StandardInput.Close();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        _logger.LogInformation("Job {Id} started: {Display}", job.Id, job.Display);
        JobsChanged?.Invoke();
        return job;
    }

    public void Cancel(Job job)
    {
        if (!job.IsRunning || job.Process is null)
        {
            return;
        }

        job.Status = JobStatus.Cancelled;
        try
        {
            job.Process.Kill(entireProcessTree: true);
            job.Append("—— 已由 dashboard 中止 ——");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException)
        {
            // Exited (and possibly disposed) between the IsRunning check and the kill.
        }
    }

    /// <summary>Starts an interactive menu in its own pwsh window (Windows only).</summary>
    public void OpenWindow(ToolDefinition tool, IReadOnlyDictionary<string, string> values)
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("在新視窗開啟只支援 Windows。");
        }

        var command = CommandBuilder.Build(tool, _paths.Script(tool.Script), values);

        // Shell execute opens a new console window. The command holds only single-quoted literals, so
        // wrapping it in double quotes is safe; reject anything else rather than escape it.
        if (command.Contains('"'))
        {
            throw new InvalidOperationException("Unexpected double quote in the command.");
        }

        Process.Start(new ProcessStartInfo("pwsh", $"-NoProfile -NoExit -Command \"{command}\"")
        {
            UseShellExecute = true,
            WorkingDirectory = _paths.ToolsDir,
        });
        _logger.LogInformation("Opened {Script} in a new window", tool.Script);
    }

    public void Dispose()
    {
        foreach (var job in Jobs().Where(j => j.IsRunning))
        {
            Cancel(job);
        }
    }

    private void OnExited(Job job)
    {
        var process = job.Process!;

        // The parameterless WaitForExit also waits for the asynchronous output readers to drain.
        process.WaitForExit();
        job.ExitCode = process.ExitCode;
        job.EndedAt = DateTime.Now;
        if (job.Status != JobStatus.Cancelled)
        {
            job.Status = process.ExitCode == 0 ? JobStatus.Succeeded : JobStatus.Failed;
        }

        process.Dispose();
        _logger.LogInformation("Job {Id} ended: exit {ExitCode}", job.Id, job.ExitCode);
        JobsChanged?.Invoke();
    }

    // Script plus the parameters that differ from their defaults (Network always), for the job list.
    private static string Describe(ToolDefinition tool, IReadOnlyDictionary<string, string> values)
    {
        var args = tool.Parameters
            .Select(p => (p.Name, Default: p.Default, Value: values.TryGetValue(p.Name, out var v) ? v : p.Default))
            .Where(x => x.Name == "Network" || x.Value != x.Default)
            .Select(x => $"-{x.Name} {x.Value}");
        return string.Join(' ', args.Prepend(tool.Script));
    }
}
