// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="F_127VISAFieldL3.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the F_127VISAFieldL3 type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.VISA.SubFieldCodec.F127
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.TLV;

    #endregion

    public class F_127VISAFieldL3 : IFunctionalFieldCodec<TLV, byte[], byte[], TLV>
    {
        public static readonly F_127VISAFieldL3 Default = new F_127VISAFieldL3();

        private F_127VISAFieldL3()
        {
        }

        public byte[] Encode(TLV data)
        {
            return data.Encode().Val();
        }

        public TLV Decode(byte[] data)
        {
            return new TLV().Decode(data);
        }
    }
}
