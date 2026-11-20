// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="IpassResponse.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the IpassResponse type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.TM.ISO8583.IPASS.Transaction
{
    #region DEPENDENCY

    #endregion

    public class IpassResponse : IPASS
    {
        public IpassResponse(byte[] message = null)
            : base(message)
        {
        }
    }
}
