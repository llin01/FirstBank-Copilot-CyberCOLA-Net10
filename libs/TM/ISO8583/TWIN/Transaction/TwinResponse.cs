// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TwinResponse.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TwinResponse type.
// </summary>
//
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.TWIN.Transaction
{
    #region DEPENDENCY

    #endregion

    public class TwinResponse : TWINIso8583
    {
        public TwinResponse(byte[] message = null)
            : base(message)
        {
        }
    }
}
