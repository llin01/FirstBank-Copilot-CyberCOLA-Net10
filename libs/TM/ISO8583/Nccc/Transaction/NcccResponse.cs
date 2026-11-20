// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="NcccResponse.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the NcccResponse type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Transaction
{
    #region DEPENDENCY

    #endregion

    public class NcccResponse : NcccIso8583
    {
        public NcccResponse(byte[] message = null)
            : base(message)
        {
        }
    }
}
