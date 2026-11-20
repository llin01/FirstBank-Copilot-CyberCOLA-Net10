// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="F_48FieldUsage1a.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the F_48FieldUsage1a type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.VISA.SubFieldCodec.F48
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;

    #endregion

    public class F_48FieldUsage1a : IFunctionalFieldCodec<string, byte[], byte[], string>
    {
        public static readonly F_48FieldUsage1a Default = new F_48FieldUsage1a();

        private F_48FieldUsage1a()
        {
        }

        public byte[] Encode(string data)
        {
            return data.GetHexBytes();
        }

        public string Decode(byte[] data)
        {
            return data.GetHexString();
        }
    }
}
