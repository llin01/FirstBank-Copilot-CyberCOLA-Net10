// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="F_127VISAFieldPF.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the F_127VISAFieldPF type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.VISA.SubFieldCodec.F127
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.TLV;

    #endregion

    public class F_127VISAFieldPF : IFunctionalFieldCodec<TLV, byte[], byte[], TLV>
    {
        public static readonly F_127VISAFieldPF Default = new F_127VISAFieldPF();

        private F_127VISAFieldPF()
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
