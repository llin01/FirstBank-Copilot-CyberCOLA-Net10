using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Com.Cybersoft4u.Test.Web.WebToolsDashboard.Services;

/// <summary>
/// Turns lines carrying ANSI colour codes (SGR) into HTML for the console view. All text is HTML-encoded and
/// only fixed class names are emitted, so tool output can never inject markup.
/// </summary>
public static partial class AnsiText
{
    [GeneratedRegex(@"\x1B\[([0-9;]*)m")]
    private static partial Regex Sgr();

    public static string ToHtml(IEnumerable<string> lines)
    {
        var sb = new StringBuilder();
        foreach (var line in lines)
        {
            AppendLine(sb, line);
            sb.Append('\n');
        }

        return sb.ToString();
    }

    private static void AppendLine(StringBuilder sb, string line)
    {
        int? color = null;
        var bold = false;
        var position = 0;
        foreach (Match m in Sgr().Matches(line))
        {
            AppendText(sb, line[position..m.Index], color, bold);
            position = m.Index + m.Length;

            // An empty parameter list means reset, like 0.
            var codes = m.Groups[1].Value.Length == 0 ? ["0"] : m.Groups[1].Value.Split(';');
            foreach (var code in codes)
            {
                if (!int.TryParse(code, NumberStyles.None, CultureInfo.InvariantCulture, out var n))
                {
                    continue;
                }

                switch (n)
                {
                    case 0: color = null; bold = false; break;
                    case 1: bold = true; break;
                    case 22: bold = false; break;
                    case 39: color = null; break;
                    case >= 30 and <= 37:
                    case >= 90 and <= 97:
                        color = n;
                        break;
                }
            }
        }

        // A colour still open at the end of the line does not carry over: each line stands alone.
        AppendText(sb, line[position..], color, bold);
    }

    private static void AppendText(StringBuilder sb, string text, int? color, bool bold)
    {
        if (text.Length == 0)
        {
            return;
        }

        var encoded = WebUtility.HtmlEncode(text);
        if (color is null && !bold)
        {
            sb.Append(encoded);
            return;
        }

        sb.Append("<span class=\"");
        if (color is not null)
        {
            sb.Append("ansi-").Append(color.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (bold)
        {
            sb.Append(color is null ? "ansi-bold" : " ansi-bold");
        }

        sb.Append("\">").Append(encoded).Append("</span>");
    }
}
