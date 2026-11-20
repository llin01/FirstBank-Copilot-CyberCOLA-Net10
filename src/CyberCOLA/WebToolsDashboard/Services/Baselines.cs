namespace Com.Cybersoft4u.Test.Web.WebToolsDashboard.Services;

public sealed record Baseline(string Date, int Requests, double SuccessPercent, string Note);

/// <summary>
/// Reference results of the default-parameter auth test (2026-11-09, re-confirmed 2026-11-11 after the
/// Yetty TCP changes). A run well below these usually means used-up test-card credit (check code 062).
/// </summary>
public static class Baselines
{
    private static readonly Dictionary<string, Baseline> ByNetwork = new(StringComparer.OrdinalIgnoreCase)
    {
        ["NCCC"] = new("2026-11-09", 400, 99.0, "RC 05×4：092 CVV2（920000）"),
        ["FISC"] = new("2026-11-09", 400, 96.5, "RC 05×14：072 PVV×8、012/RSN 24×4、132×2"),
        ["SVCS"] = new("2026-11-09", 60, 100.0, "—"),
        ["ICASH"] = new("2026-11-09", 40, 100.0, "—"),
        ["IPASS"] = new("2026-11-09", 60, 100.0, "—"),
    };

    public static Baseline? For(string? network) =>
        network is not null && ByNetwork.TryGetValue(network, out var b) ? b : null;
}
