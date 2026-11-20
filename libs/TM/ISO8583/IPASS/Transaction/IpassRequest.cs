// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="IpassRequest.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the IpassRequest type.
// </summary>
//
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.TM.ISO8583.IPASS.Transaction
{
    #region DEPENDENCY

    #endregion

    public class IpassRequest : IPASS
    {
        public IpassRequest(byte[] message = null)
            : base(message)
        {
        }
    }
}
