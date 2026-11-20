// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="F_48FieldUsage1b.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the F_48FieldUsage1b type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.VISA.SubFieldCodec.F48
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;

    #endregion

    public class F_48FieldUsage1b : IFunctionalFieldCodec<string, byte[], byte[], string>
    {
        public static readonly F_48FieldUsage1b Default = new F_48FieldUsage1b();

        private F_48FieldUsage1b()
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
