// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="F_127VISAFieldUsage2.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the F_127VISAFieldUsage2 type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.VISA.SubFieldCodec.F127
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.TLV;

    #endregion

    public class F_127VISAFieldUsage2 : IFunctionalFieldCodec<TLV, byte[], byte[], TLV>
    {
        public static readonly F_127VISAFieldUsage2 Default = new F_127VISAFieldUsage2();

        private F_127VISAFieldUsage2()
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
