// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TwinRequest.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TwinRequest type.
// </summary>
//
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.TWIN.Transaction
{
    #region DEPENDENCY

    #endregion

    public class TwinRequest : TWINIso8583
    {
        public TwinRequest(byte[] message = null)
            : base(message)
        {
        }
    }
}
