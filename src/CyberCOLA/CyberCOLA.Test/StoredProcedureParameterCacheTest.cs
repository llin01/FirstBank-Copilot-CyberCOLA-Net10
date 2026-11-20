using Com.Cybersoft4u.CommonLib.DBUtils;
using Microsoft.Data.SqlClient;
using NUnit.Framework;
using System.Data;

namespace Com.Cybersoft4u.CyberCOLA.Test
{
    // No database: the derive step is a fake that adds the parameters DeriveParameters would return.
    [TestFixture]
    [NonParallelizable]
    public class StoredProcedureParameterCacheTest
    {
        private int _deriveCalls;

        [SetUp]
        public void SetUp()
        {
            StoredProcedureParameterCache.Clear();
            _deriveCalls = 0;
        }

        [TearDown]
        public void TearDown() => StoredProcedureParameterCache.Clear();

        private void FakeDerive(SqlCommand command)
        {
            _deriveCalls++;
            command.Parameters.Clear();
            command.Parameters.Add(new SqlParameter("@RETURN_VALUE", SqlDbType.Int) { Direction = ParameterDirection.ReturnValue });
            command.Parameters.Add(new SqlParameter("@cardNbr", SqlDbType.VarChar, 19));
            command.Parameters.Add(new SqlParameter("@rtnCode", SqlDbType.VarChar, 4) { Direction = ParameterDirection.Output });
        }

        private static SqlCommand NewCommand() => new SqlCommand("dbo.TEST_PROC") { CommandType = CommandType.StoredProcedure };

        [Test]
        public void Apply_FirstCall_DerivesAndReportsCacheMiss()
        {
            var key = StoredProcedureParameterCache.Key("Server=x", "dbo.TEST_PROC");
            using var command = NewCommand();

            var hit = StoredProcedureParameterCache.Apply(key, command, FakeDerive);

            Assert.That(hit, Is.False);
            Assert.That(_deriveCalls, Is.EqualTo(1));
            Assert.That(command.Parameters.Count, Is.EqualTo(3));
        }

        [Test]
        public void Apply_SecondCallSameKey_CopiesCachedParametersWithoutDeriving()
        {
            var key = StoredProcedureParameterCache.Key("Server=x", "dbo.TEST_PROC");
            using var first = NewCommand();
            StoredProcedureParameterCache.Apply(key, first, FakeDerive);
            first.Parameters[1].Value = "4000000000000002";   // synthetic test value

            using var second = NewCommand();
            var hit = StoredProcedureParameterCache.Apply(key, second, FakeDerive);

            Assert.That(hit, Is.True);
            Assert.That(_deriveCalls, Is.EqualTo(1));
            Assert.That(second.Parameters.Count, Is.EqualTo(3));
            for (var i = 0; i < 3; i++)
            {
                Assert.That(second.Parameters[i].ParameterName, Is.EqualTo(first.Parameters[i].ParameterName));
                Assert.That(second.Parameters[i].Direction, Is.EqualTo(first.Parameters[i].Direction));
                Assert.That(second.Parameters[i].SqlDbType, Is.EqualTo(first.Parameters[i].SqlDbType));
                Assert.That(second.Parameters[i].Size, Is.EqualTo(first.Parameters[i].Size));
                Assert.That(second.Parameters[i], Is.Not.SameAs(first.Parameters[i]));
            }
            // A value set on one command must never reach the next one.
            Assert.That(second.Parameters[1].Value, Is.Null);
        }

        [Test]
        public void Invalidate_AfterCaching_NextCallDerivesAgain()
        {
            var key = StoredProcedureParameterCache.Key("Server=x", "dbo.TEST_PROC");
            using (var command = NewCommand()) StoredProcedureParameterCache.Apply(key, command, FakeDerive);

            StoredProcedureParameterCache.Invalidate(key);
            using var again = NewCommand();
            var hit = StoredProcedureParameterCache.Apply(key, again, FakeDerive);

            Assert.That(hit, Is.False);
            Assert.That(_deriveCalls, Is.EqualTo(2));
        }

        [Test]
        public void Apply_DifferentConnectionStrings_CachesSeparately()
        {
            using (var a = NewCommand()) StoredProcedureParameterCache.Apply(StoredProcedureParameterCache.Key("Server=a", "dbo.TEST_PROC"), a, FakeDerive);
            using var b = NewCommand();

            var hit = StoredProcedureParameterCache.Apply(StoredProcedureParameterCache.Key("Server=b", "dbo.TEST_PROC"), b, FakeDerive);

            Assert.That(hit, Is.False);
            Assert.That(_deriveCalls, Is.EqualTo(2));
        }
    }
}
