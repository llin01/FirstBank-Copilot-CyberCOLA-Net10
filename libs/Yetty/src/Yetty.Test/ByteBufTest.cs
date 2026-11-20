namespace Com.Cybersoft4u.Yetty.Test
{
    #region DEPENDENCY

    using Com.Cybersoft4u.Yetty.Utils;

    using NUnit.Framework;

    #endregion

    /// <summary>
    /// The byte buf test.
    /// </summary>
    [TestFixture]
    public class ByteBufTest
    {
        /// <summary>
        /// The read integer.
        /// </summary>
        [Test]
        public void ReadInteger()
        {
            var data = new ByteBuf(new byte[] { 0x00, 0x00, 0x00, 0x10 });
            Assert.AreEqual(16, data.ReadInteger());
        }

        /// <summary>
        /// The read unsigned short.
        /// </summary>
        [Test]
        public void ReadUnsignedShort()
        {
            var data = new ByteBuf(new byte[] { 0x00, 0x10 });
            Assert.AreEqual(16, data.ReadUnsignedShort());
        }

        [Test]
        public void WriteBytes()
        {
            var bytes = new byte[] { 0x00, 0x00, 0x00, 0x10 };
            var data = new ByteBuf();
            data.WriteBytes(bytes);
            CollectionAssert.AreEqual(bytes, data.Val());
        }

        [Test]
        public void WriteInteger()
        {
            var bytes = new byte[] { 0x00, 0x00, 0x01, 0x01 };
            const int Val = 257;
            var data = new ByteBuf();
            data.WriteInteger(Val);
            CollectionAssert.AreEqual(bytes, data.Val());
        }

        [Test]
        public void WriteSingleByteChar()
        {
            var bytes = new byte[] { 0xF1 };
            const short Val = 241;
            var data = new ByteBuf();
            data.WriteSingleByteChar(Val);
            CollectionAssert.AreEqual(bytes, data.Val());
        }

        [Test]
        public void WriteUnsignedShort()
        {
            var bytes = new byte[] { 0x01, 0x01 };
            const ushort Val = 257;
            var data = new ByteBuf();
            data.WriteUnsignedShort(Val);
            CollectionAssert.AreEqual(bytes, data.Val());
        }

        [Test]
        public void WriteByteBuf()
        {
            var data = new ByteBuf(new byte[] { 0x01, 0x01 });
            var bytes = new ByteBuf(new byte[] { 0xF1 });
            data.WriteByteBuf(bytes);
            CollectionAssert.AreEqual(data.Val(), new[] { 0x01, 0x01, 0xf1 });
        }
    }
}