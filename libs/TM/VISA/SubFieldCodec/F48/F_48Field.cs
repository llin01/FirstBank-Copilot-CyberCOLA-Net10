// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="F_48Field.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the F_48Field type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.VISA.SubFieldCodec.F48
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583;

    #endregion

    public class F_48Field : IFunctionalFieldCodec<string, byte[], byte[], string>
    {
        public static readonly F_48Field Default = new F_48Field();

        private F_48Field()
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
