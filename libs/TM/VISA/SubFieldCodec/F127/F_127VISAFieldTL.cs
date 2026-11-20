// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="F_127VISAFieldTL.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the F_127VISAFieldTL type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.VISA.SubFieldCodec.F127
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;

    #endregion

    public class F_127VISAFieldTL : IFunctionalFieldCodec<string, byte[], byte[], string>
    {
        public static readonly F_127VISAFieldTL Default = new F_127VISAFieldTL();

        private F_127VISAFieldTL()
        {
        }

        public byte[] Encode(string data)
        {
            return HexStringConverter.ToByteArray(data);
        }

        public string Decode(byte[] data)
        {
            return HexStringConverter.FromByteArray(data);
        }
    }
}
