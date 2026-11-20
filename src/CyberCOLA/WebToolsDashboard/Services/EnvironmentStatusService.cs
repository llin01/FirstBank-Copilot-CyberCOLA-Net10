using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Com.Cybersoft4u.Test.Web.WebToolsDashboard.Services;

public sealed record RunningProcess(string Name, int Id);

public sealed record PortCheck(string Host, int Port, bool Open);

public sealed record EnvironmentStatus(
    DateTime At,
    IReadOnlyList<RunningProcess> Processes,
    IReadOnlyList<PortCheck> Databases,
    string? Vpn);

/// <summary>
/// Refreshes, every few seconds, what the auth tests depend on: CyberCOLA processes still running, the DB
/// ports (the only proof the VPN really works) and the GlobalProtect adapter state.
/// </summary>
public sealed class EnvironmentStatusService : BackgroundService
{
    private static readonly string[] ProcessPrefixes =
        ["CyberGW", "CyberAuth", "ServiceRouter", "WpfETicketClientSimulator", "ConsoleStressSimulator", "WpfHSMSimulator"];

    private readonly string[] _dbHosts;
    private readonly ILogger<EnvironmentStatusService> _logger;

    public EnvironmentStatusService(IConfiguration configuration, ILogger<EnvironmentStatusService> logger)
    {
        _dbHosts = configuration.GetSection("Dashboard:DbHosts").Get<string[]>() ?? ["DBServer", "RD-DB"];
        _logger = logger;
    }

    public EnvironmentStatus? Current { get; private set; }

    public event Action? Changed;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));
        do
        {
            try
            {
                Current = await CollectAsync(stoppingToken);
                Changed?.Invoke();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Environment status refresh failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task<EnvironmentStatus> CollectAsync(CancellationToken token)
    {
        var processes = Process.GetProcesses()
            .Where(p => ProcessPrefixes.Any(prefix => p.ProcessName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .Select(p => new RunningProcess(p.ProcessName, p.Id))
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var databases = await Task.WhenAll(_dbHosts.Select(h => CheckPortAsync(h, 1433, token)));

        var vpn = NetworkInterface.GetAllNetworkInterfaces()
            .FirstOrDefault(n => n.Description.Contains("PANGP", StringComparison.OrdinalIgnoreCase))
            ?.OperationalStatus.ToString();

        return new EnvironmentStatus(DateTime.Now, processes, databases, vpn);
    }

    private static async Task<PortCheck> CheckPortAsync(string host, int port, CancellationToken token)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
        cts.CancelAfter(TimeSpan.FromMilliseconds(1500));
        using var client = new TcpClient();
        try
        {
            await client.ConnectAsync(host, port, cts.Token);
            return new PortCheck(host, port, true);
        }
        catch (Exception ex) when (ex is SocketException or OperationCanceledException && !token.IsCancellationRequested)
        {
            return new PortCheck(host, port, false);
        }
    }
}
