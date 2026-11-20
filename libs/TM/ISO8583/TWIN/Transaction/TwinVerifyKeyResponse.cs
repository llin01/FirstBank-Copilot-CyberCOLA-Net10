// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TwinVerifyKeyResponse.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TwinVerifyKeyResponse type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.TWIN.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583.Category.Attributes;

    #endregion

    [TransactionType("0800")]

    public class TwinVerifyKeyResponse : TwinResponse
    {
        public TwinVerifyKeyResponse(byte[] message = null)
            : base(message)
        {
        }

        protected override void ConstructProcess()
        {
            base.ConstructProcess();
            this.Mti("0810");
        }
    }
}
