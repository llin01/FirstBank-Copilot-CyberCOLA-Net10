// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="SvcsRequest.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the SvcsRequest type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.TM.ISO8583.SVCS.Transaction
{
    #region DEPENDENCY

    #endregion

    public class SvcsRequest : SVCS
    {
        public SvcsRequest(byte[] message = null)
            : base(message)
        {
        }
    }
}
