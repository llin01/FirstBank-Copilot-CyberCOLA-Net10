// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="NcccRequest.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the NcccRequest type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Transaction
{
    #region DEPENDENCY

    #endregion

    public class NcccRequest : NcccIso8583
    {
        public NcccRequest(byte[] message = null)
            : base(message)
        {
        }
    }
}
