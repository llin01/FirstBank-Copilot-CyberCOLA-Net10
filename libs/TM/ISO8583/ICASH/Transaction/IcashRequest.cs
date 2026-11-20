// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="IcashRequest.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the IcashRequest type.
// </summary>
//
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.TM.ISO8583.ICASH.Transaction
{
    #region DEPENDENCY

    #endregion

    public class IcashRequest : ICASH
    {
        public IcashRequest(byte[] message = null)
            : base(message)
        {
        }
    }
}
