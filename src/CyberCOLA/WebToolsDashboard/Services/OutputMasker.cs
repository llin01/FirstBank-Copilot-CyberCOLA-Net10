using System.Text.RegularExpressions;

namespace Com.Cybersoft4u.Test.Web.WebToolsDashboard.Services;

/// <summary>
/// Cleans one line of tool output before it is kept or shown: masks anything that could be a card number
/// (PCI DSS) or a password, and drops terminal control sequences except colours (SGR), which the console
/// view renders. Applied to every line, whatever the tool.
/// </summary>
public static partial class OutputMasker
{
    // Any CSI sequence (cursor moves, erase, ...) except SGR, which ends in 'm'.
    [GeneratedRegex(@"\x1B\[[0-9;?]*[ -/]*[@-ln-~]")]
    private static partial Regex ControlSequence();

    [GeneratedRegex(@"\x1B\[[0-9;?]*[ -/]*[@-~]")]
    private static partial Regex AnyAnsi();

    // 13-19 digits not inside a longer digit run: PAN lengths. Timestamps such as 20261111114301 (14 digits)
    // are masked too; losing them is the safe side.
    [GeneratedRegex(@"(?<!\d)\d{9,15}(\d{4})(?!\d)")]
    private static partial Regex CardNumber();

    [GeneratedRegex(@"(?i)\b(password|pwd)\s*=\s*[^;\s\x1B]*")]
    private static partial Regex Password();

    public static string Clean(string line)
    {
        var text = ControlSequence().Replace(line, string.Empty);
        text = Password().Replace(text, "$1=***");
        return CardNumber().Replace(text, "****$1");
    }

    /// <summary>Plain text of a cleaned line, for parsers that match on the line's own characters.</summary>
    public static string StripAnsi(string line) => AnyAnsi().Replace(line, string.Empty);
}
