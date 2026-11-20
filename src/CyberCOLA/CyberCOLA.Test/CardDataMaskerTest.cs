using System.Text;
using Com.Cybersoft4u.Utils;
using NUnit.Framework;

namespace Com.Cybersoft4u.CyberCOLA.Test
{
    // Synthetic test card numbers only - never real card data.
    [TestFixture]
    public class CardDataMaskerTest
    {
        private const string Pan = "4000000000000002";

        private static string Hex(byte[] bytes) => Convert.ToHexString(bytes);

        private static byte[] Ebcdic(string digitsAndEquals)
        {
            // EBCDIC digits are 0xF0-0xF9 and '=' is 0x7E; enough for track-2 style test data.
            return digitsAndEquals.Select(c => c == '=' ? (byte)0x7E : (byte)(0xF0 + (c - '0'))).ToArray();
        }

        // Renders bytes the way PrintBytesMap does: |row| hex ... |ascii|
        private static string Dump(byte[] bytes)
        {
            var sb = new StringBuilder("         +-------------------------------------------------+");
            for (var i = 0; i < bytes.Length; i += 16)
            {
                var chunk = bytes.Skip(i).Take(16).ToArray();
                var hex = string.Concat(chunk.Select(b => $"{b:X2} "));
                var text = new string(chunk.Select(b => b < 32 || b == 94 || b > 126 ? '.' : (char)b).ToArray());
                sb.Append($"\n|{(i / 16).ToString().PadLeft(8, '0')}| {hex.PadRight(48)}|{text.PadRight(16)}|");
            }

            return sb.ToString();
        }

        [Test]
        public void Mask_PlainPan_KeepsOnlyLastFourDigits()
        {
            Assert.That(CardDataMasker.Mask($"CardNbr is :[{Pan}]"), Is.EqualTo("CardNbr is :[************0002]"));
        }

        [TestCase("=")]
        [TestCase("D")]
        [TestCase("^")]
        public void Mask_Track2WithShortDiscretionaryData_MasksEverythingAfterSeparator(string separator)
        {
            // 12 digits after the separator: below the 13-digit PAN threshold, but it holds expiry and CVV data.
            var masked = CardDataMasker.Mask($"Track2[{Pan}{separator}290120111234]");

            Assert.That(masked, Is.EqualTo($"Track2[************0002{separator}************]"));
        }

        [Test]
        public void Mask_NonCardNumbers_LeavesTextUnchanged()
        {
            const string text = "Time[20261021 11454630063], RRN[000000123456], Amount[000000001000], RelationId[1021114546-000014-A2000902]";

            Assert.That(CardDataMasker.Mask(text), Is.EqualTo(text));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("short 1234")]
        public void Mask_NullEmptyOrShortText_ReturnsInput(string? text)
        {
            Assert.That(CardDataMasker.Mask(text!), Is.EqualTo(text));
        }

        [Test]
        public void Mask_AlreadyMaskedText_IsIdempotent()
        {
            var once = CardDataMasker.Mask($"CardInfo[{Pan}=290120111234]");

            Assert.That(CardDataMasker.Mask(once), Is.EqualTo(once));
        }

        [Test]
        public void Mask_AsciiHexString_MasksPanAndTrackBytes()
        {
            var payload = Encoding.ASCII.GetBytes($"0200{Pan}=290120111234X");
            var masked = CardDataMasker.Mask($"Hex String: {Hex(payload)}");

            var bytes = Convert.FromHexString(masked["Hex String: ".Length..]);
            Assert.That(Encoding.ASCII.GetString(bytes), Is.EqualTo("****************0002=************X"));
        }

        [Test]
        public void Mask_EbcdicHexString_MasksPanAndTrackBytes()
        {
            var payload = new byte[] { 0x76, 0x7C }.Concat(Ebcdic($"{Pan}=29012011123")).Concat(new byte[] { 0xC1 }).ToArray();
            var masked = CardDataMasker.Mask($"Hex String: {Hex(payload)}");

            var bytes = Convert.FromHexString(masked["Hex String: ".Length..]);
            var expected = new byte[] { 0x76, 0x7C }
                .Concat(Enumerable.Repeat((byte)0x2A, 12)).Concat(Ebcdic("0002"))
                .Concat(new byte[] { 0x7E }).Concat(Enumerable.Repeat((byte)0x2A, 11))
                .Concat(new byte[] { 0xC1 }).ToArray();
            Assert.That(bytes, Is.EqualTo(expected));
        }

        [Test]
        public void Mask_DumpWithPanAcrossRows_MasksHexAndTextColumns()
        {
            // The PAN starts near the end of row 0 and continues into row 1.
            var payload = Encoding.ASCII.GetBytes($"ISO0260000700X{Pan}=290120111234XYZ");
            var masked = CardDataMasker.Mask(Dump(payload));

            var maskedPayload = Encoding.ASCII.GetBytes("ISO0260000700X************0002=************XYZ");
            Assert.That(masked, Is.EqualTo(Dump(maskedPayload)));
        }

        [Test]
        public void Mask_DumpWithoutCardData_LeavesDumpUnchanged()
        {
            var dump = Dump(Encoding.ASCII.GetBytes("ISO026000070 0800 echo 123456"));

            Assert.That(CardDataMasker.Mask(dump), Is.EqualTo(dump));
        }
    }
}
