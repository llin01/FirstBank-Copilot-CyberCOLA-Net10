using System.Globalization;
using System.Text;

namespace Com.Cybersoft4u.Test.Web.WebToolsDashboard.Services;

/// <summary>
/// Builds the pwsh -Command text for a catalog tool. Values are validated against the parameter definition
/// (choice list, integer range, flag), so only known literals reach the command line. -Command rather than
/// -File because -File cannot pass a [bool] parameter (-SimReplaceCard:$false fails type conversion).
/// </summary>
public static class CommandBuilder
{
    // Write-Host -ForegroundColor only sets the console's text attribute, which is lost once output is
    // redirected, so the colour a script uses to flag a row (e.g. Invoke-CyberCOLASql's red low-credit rows)
    // never reached the dashboard. This global function shadows the cmdlet (functions win over cmdlets) and
    // writes a coloured line as an ANSI code straight to stdout: through the host, pwsh would strip the code
    // again. $PSStyle.OutputRendering is left alone on purpose: set to Ansi it also colours Format-Table
    // headers, and scripts that parse their own table text (Invoke-CyberCOLASql finds the dashed separator
    // line) then misplace rows. The scripts themselves are unchanged.
    // ConsoleColor order: Black, DarkBlue, ..., Gray, DarkGray, Blue, ..., White.
    internal const string AnsiWriteHost = """
        function global:Write-Host {
            param(
                [Parameter(Position = 0, ValueFromPipeline = $true, ValueFromRemainingArguments = $true)] [object]$Object,
                [switch]$NoNewline,
                [object]$Separator = ' ',
                [ConsoleColor]$ForegroundColor,
                [ConsoleColor]$BackgroundColor)
            process {
                $text = if ($Object -is [System.Collections.IEnumerable] -and $Object -isnot [string]) { ($Object | ForEach-Object { "$_" }) -join "$Separator" } else { "$Object" }
                if (-not $PSBoundParameters.ContainsKey('ForegroundColor')) {
                    Microsoft.PowerShell.Utility\Write-Host $text -NoNewline:$NoNewline
                    return
                }
                $sgr = (30, 34, 32, 36, 31, 35, 33, 37, 90, 94, 92, 96, 91, 95, 93, 97)[[int]$ForegroundColor]
                $text = "$([char]27)[${sgr}m$text$([char]27)[0m"
                if ($NoNewline) { [Console]::Out.Write($text) } else { [Console]::Out.WriteLine($text) }
            }
        }

        """;

    public static string Build(ToolDefinition tool, string scriptPath, IReadOnlyDictionary<string, string> values)
    {
        var sb = new StringBuilder();

        // Child output is decoded as UTF-8; without this pwsh writes the console code page (e.g. 950).
        sb.Append("[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false); ");

        // Only for captured runs: a tool opened in its own window has a real console with real colours.
        if (tool.Kind == ToolKind.Run)
        {
            sb.Append(AnsiWriteHost);
        }

        sb.Append("& ").Append(Quote(scriptPath));

        foreach (var p in tool.Parameters)
        {
            var raw = values.TryGetValue(p.Name, out var v) ? v : p.Default;
            switch (p.Type)
            {
                case ParameterType.Choice:
                    if (p.Choices is null || !p.Choices.Contains(raw, StringComparer.Ordinal))
                    {
                        throw new ArgumentException($"{p.Label}: '{raw}' is not one of the allowed values.");
                    }

                    sb.Append(" -").Append(p.Name).Append(' ').Append(Quote(raw));
                    break;

                case ParameterType.Int:
                    if (!int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
                        || number < p.Min || number > p.Max)
                    {
                        throw new ArgumentException($"{p.Label}: '{raw}' must be an integer between {p.Min} and {p.Max}.");
                    }

                    sb.Append(" -").Append(p.Name).Append(' ').Append(number.ToString(CultureInfo.InvariantCulture));
                    break;

                case ParameterType.Bool:
                    sb.Append(" -").Append(p.Name).Append(ParseFlag(p, raw) ? ":$true" : ":$false");
                    break;

                case ParameterType.Switch:
                    if (ParseFlag(p, raw))
                    {
                        sb.Append(" -").Append(p.Name);
                    }

                    break;
            }
        }

        if (tool.AuthTestOutput)
        {
            sb.Append(" -StreamOutput");
        }

        if (tool.Kind == ToolKind.Run)
        {
            sb.Append("; exit $LASTEXITCODE");
        }

        return sb.ToString();
    }

    private static bool ParseFlag(ToolParameter p, string raw) => raw switch
    {
        "true" or "True" => true,
        "false" or "False" => false,
        _ => throw new ArgumentException($"{p.Label}: '{raw}' must be true or false."),
    };

    // PowerShell single-quoted string: no expansion; a single quote is escaped by doubling it.
    private static string Quote(string value) => "'" + value.Replace("'", "''", StringComparison.Ordinal) + "'";
}
