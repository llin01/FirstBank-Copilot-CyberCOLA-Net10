using Com.Cybersoft4u.CommonLib.Resource;
using NUnit.Framework;

namespace Com.Cybersoft4u.CyberCOLA.Test
{
    [TestFixture]
    public class ColumnEncryptionSettingTest
    {
        [TestCase(null)]
        [TestCase("")]
        [TestCase("Enabled")]
        [TestCase("anything else")]
        public void ResolveColumnEncryptionSetting_NotDisabled_KeepsItEnabled(string? configured)
        {
            Assert.That(SystemConstantData.CommonData.ResolveColumnEncryptionSetting(configured),
                Is.EqualTo("Column Encryption Setting=enabled"));
        }

        [TestCase("Disabled")]
        [TestCase(" disabled ")]
        [TestCase("DISABLED")]
        public void ResolveColumnEncryptionSetting_Disabled_TurnsItOff(string configured)
        {
            Assert.That(SystemConstantData.CommonData.ResolveColumnEncryptionSetting(configured),
                Is.EqualTo("Column Encryption Setting=disabled"));
        }

        [Test]
        public void ColumnEncryptionSetting_TestHostWithoutTheKey_DefaultsToEnabled()
        {
            // The test host's appsettings.json has no sql-column-encryption-setting key: behaviour must not change.
            Assert.That(SystemConstantData.CommonData.ColumnEncryptionSetting, Is.EqualTo("Column Encryption Setting=enabled"));
        }
    }
}
