// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="MasterCardRequest.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the MasterCardRequest type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.MasterCard.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583.Category.Action;

    #endregion

    public class MasterCardRequest : MasterCard, Request
    {
        public MasterCardRequest()
        {
        }

        public MasterCardRequest(byte[] message)
            : base(message)
        {
        }
    }
}
