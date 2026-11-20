// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="IcashResponse.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the IcashResponse type.
// </summary>
//
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.TM.ISO8583.ICASH.Transaction
{
    #region DEPENDENCY

    #endregion

    public class IcashResponse : ICASH
    {
        public IcashResponse(byte[] message = null)
            : base(message)
        {
        }
    }
}
