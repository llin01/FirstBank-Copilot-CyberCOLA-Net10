// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="MCOLARequest.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the MCOLARequest type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.MCOLA.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583.Category.Action;

    #endregion

    public class MCOLARequest : MCOLA, Request
    {
        public MCOLARequest()
        {
        }

        public MCOLARequest(byte[] message)
            : base(message)
        {
        }
    }
}
