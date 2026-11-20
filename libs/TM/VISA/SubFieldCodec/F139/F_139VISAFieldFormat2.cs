// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="F_139VISAFieldFormat1.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the F_139VISAFieldFormat1 type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.VISA.SubFieldCodec.F139
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;

    #endregion

    public class F_139VISAFieldFormat2 : IFieldCodec<byte[], byte[], F_139VISAFieldFormat2>
    {
        public string Arpc = string.Empty;

        public string Csu = string.Empty;

        public string ArpcRc = string.Empty;

        public F_139VISAFieldFormat2()
        {
        }

        public F_139VISAFieldFormat2(byte[] data)
        {
            this.Decode(data);
        }

        public byte[] Encode()
        {
            return new ByteBuffer(this.Arpc.GetHexBytes())
                .WriteBytes(this.Csu.GetHexBytes())
                .WriteBytes(DefaultPacker.EBCDIC.GetBytes(this.ArpcRc)).Val();
        }

        public F_139VISAFieldFormat2 Decode(byte[] data)
        {
            var buf = new ByteBuffer(data);
            this.Arpc = buf.ReadBytes(4).GetHexString();
            this.Csu = buf.ReadBytes(4).GetHexString();
            this.ArpcRc = DefaultPacker.EBCDIC.GetString(buf.ReadBytes(2));
            return this;
        }
    }
}
