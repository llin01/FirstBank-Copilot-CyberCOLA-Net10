// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="SvcsResponse.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the SvcsResponse type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.TM.ISO8583.SVCS.Transaction
{
    #region DEPENDENCY

    #endregion

    public class SvcsResponse : SVCS
    {
        public SvcsResponse(byte[] message = null)
            : base(message)
        {
        }
    }
}
