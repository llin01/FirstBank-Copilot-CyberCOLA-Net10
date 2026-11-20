using Com.Cybersoft4u.CommonLib.AuthUtils;
using NUnit.Framework;

namespace Com.Cybersoft4u.CyberCOLA.Test
{
    // Synthetic values only - never real customer data.
    [TestFixture]
    public class SpReturnStringsMaskTest
    {
        [Test]
        public void DisplaySpReturnStrings_MaskIndexes_HidesValuesButKeepsLength()
        {
            var values = new[] { "123456", "00", "654321", "Y", "SMS text with 654321", "Name", "0900000000", "C0000001" };

            var text = ColaCommUtility.DisplaySpReturnStrings("dbo.NCCC_0200TknOtpProcess", values, 2, 4, 5, 6, 7);

            Assert.That(text, Is.EqualTo(
                "dbo.NCCC_0200TknOtpProcess--->index[0]--Data[123456],index[1]--Data[00],index[2]--Data[***(len 6)]," +
                "index[3]--Data[Y],index[4]--Data[***(len 20)],index[5]--Data[***(len 4)],index[6]--Data[***(len 10)]," +
                "index[7]--Data[***(len 8)]"));
            Assert.That(text, Does.Not.Contain("654321").And.Not.Contain("0900000000"));
        }

        [Test]
        public void DisplaySpReturnStrings_NoMaskIndexes_LogsValuesAndSkipsNulls()
        {
            var text = ColaCommUtility.DisplaySpReturnStrings("dbo.X", new[] { "A", null, "B" });

            Assert.That(text, Is.EqualTo("dbo.X--->index[0]--Data[A],index[2]--Data[B]"));
        }
    }
}
