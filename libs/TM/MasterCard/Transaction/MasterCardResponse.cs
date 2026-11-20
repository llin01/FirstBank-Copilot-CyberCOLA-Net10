// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="MasterCardResponse.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the MasterCardResponse type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.MasterCard.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583.Category.Action;

    #endregion

    public class MasterCardResponse : MasterCard, Response
    {
        public MasterCardResponse()
        {
        }

        public MasterCardResponse(byte[] message)
            : base(message)
        {
        }
    }
}
