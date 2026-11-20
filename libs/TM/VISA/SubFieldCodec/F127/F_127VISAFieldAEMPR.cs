// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="F_127VISAFieldL1.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the F_127VISAFieldL1 type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.VISA.SubFieldCodec.F127
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583;

    #endregion

    public class F_127VISAFieldAEMPR : IFunctionalFieldCodec<string, byte[], byte[], string>
    {
        public static readonly F_127VISAFieldAEMPR Default = new F_127VISAFieldAEMPR();

        private F_127VISAFieldAEMPR()
        {
        }

        public byte[] Encode(string data)
        {
            return DefaultPacker.EBCDIC.GetBytes(data);
        }

        public string Decode(byte[] data)
        {
            return DefaultPacker.EBCDIC.GetString(data);
        }
    }
}
