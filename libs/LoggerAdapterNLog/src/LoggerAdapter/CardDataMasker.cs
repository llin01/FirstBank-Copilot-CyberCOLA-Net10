// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="CardDataMasker.cs">
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.Utils
{
    #region DEPENDENCY

    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.RegularExpressions;

    #endregion

    /// <summary>
    /// Masks card data (PAN, track data) in log text so it never reaches a log file in clear (PCI DSS).
    /// Any run of 13+ digits - the shortest PAN - keeps only its last 4 digits, and the digits after a track
    /// separator that follows such a run (expiry, service code, discretionary data incl. CVV) are fully masked.
    /// Handled forms: plain text, hex strings of whole messages (ASCII digits are 3x, EBCDIC digits Fx) and
    /// PrintBytesMap dumps, whose digits can span several rows. Over-masking other long numeric runs is accepted.
    /// Packed BCD card numbers cannot be told apart from other binary data and are not masked.
    /// </summary>
    public static class CardDataMasker
    {
        private const int MinRun = 13;

        private const int KeepLast = 4;

        private const string DumpRowMarker = "|00000000| ";

        private static readonly Regex DigitRun = new Regex("[0-9]{13,}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // Long hex tokens; the byte pairs inside are read on even offsets from the token start.
        private static readonly Regex HexToken = new Regex("[0-9A-Fa-f]{26,}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // One PrintBytesMap row: |00000000| 30 31 ... 2F |0123456789/.....|
        private static readonly Regex DumpRow = new Regex(
            @"^\|(\d{8})\| ((?:[0-9A-Fa-f]{2} ){1,16})[ ]*\|",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);

        public static string Mask(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length < MinRun)
            {
                return text;
            }

            // Dumps and hex strings first: their bytes are digit characters that the plain pass would mangle.
            if (text.Contains(DumpRowMarker))
            {
                text = MaskDumpRows(text);
            }

            text = HexToken.Replace(text, m => MaskHexToken(m.Value));
            return DigitRun.IsMatch(text) ? MaskPlain(text) : text;
        }

        // kind 1 = ASCII digit, 2 = EBCDIC digit; a run never mixes the two encodings.
        private static int DigitKind(int b)
        {
            return b >= 0x30 && b <= 0x39 ? 1 : (b >= 0xF0 && b <= 0xF9 ? 2 : 0);
        }

        // Track 2 separators: ASCII '=', 'D', '^' and EBCDIC '=', 'D'.
        private static bool IsTrackSeparator(int b, int kind)
        {
            return kind == 1 ? b == 0x3D || b == 0x44 || b == 0x5E : b == 0x7E || b == 0xC4;
        }

        // Finds card-number runs in a byte (or char) sequence and calls mask() for every position to hide.
        private static void MaskRuns(IReadOnlyList<int> values, Action<int> mask)
        {
            var i = 0;
            while (i < values.Count)
            {
                var kind = DigitKind(values[i]);
                if (kind == 0)
                {
                    i++;
                    continue;
                }

                var start = i;
                while (i < values.Count && DigitKind(values[i]) == kind)
                {
                    i++;
                }

                if (i - start < MinRun)
                {
                    continue;
                }

                for (var p = start; p < i - KeepLast; p++)
                {
                    mask(p);
                }

                if (i < values.Count && IsTrackSeparator(values[i], kind))
                {
                    i++;
                    while (i < values.Count && DigitKind(values[i]) == kind)
                    {
                        mask(i);
                        i++;
                    }
                }
            }
        }

        private static string MaskPlain(string text)
        {
            var values = new int[text.Length];
            for (var i = 0; i < text.Length; i++)
            {
                values[i] = text[i];
            }

            var chars = text.ToCharArray();
            MaskRuns(values, p => chars[p] = '*');
            return new string(chars);
        }

        private static string MaskHexToken(string token)
        {
            var values = new int[token.Length / 2];
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = Convert.ToInt32(token.Substring(i * 2, 2), 16);
            }

            var chars = token.ToCharArray();
            var changed = false;
            MaskRuns(values, p =>
            {
                chars[p * 2] = '2';
                chars[(p * 2) + 1] = 'A';
                changed = true;
            });
            return changed ? new string(chars) : token;
        }

        private static string MaskDumpRows(string text)
        {
            // Every byte of the dump with the text positions of its hex digits and its column character.
            var values = new List<int>();
            var hexPositions = new List<int>();
            var charPositions = new List<int>();
            foreach (Match row in DumpRow.Matches(text))
            {
                var hex = row.Groups[2];
                var count = hex.Length / 3;
                var charStart = row.Index + row.Length;
                for (var k = 0; k < count; k++)
                {
                    var hexPos = hex.Index + (k * 3);
                    values.Add(Convert.ToInt32(text.Substring(hexPos, 2), 16));
                    hexPositions.Add(hexPos);
                    charPositions.Add(charStart + k);
                }
            }

            if (values.Count < MinRun)
            {
                return text;
            }

            var sb = new StringBuilder(text);
            MaskRuns(values, p =>
            {
                sb[hexPositions[p]] = '2';
                sb[hexPositions[p] + 1] = 'A';
                if (charPositions[p] < sb.Length)
                {
                    sb[charPositions[p]] = '*';
                }
            });
            return sb.ToString();
        }
    }
}
