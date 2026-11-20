// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TwinNewKeyResponse.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TwinNewKeyResponse type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.TWIN.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583.Category.Attributes;

    #endregion

    [TransactionType("0800")]

    public class TwinNewKeyResponse : TwinResponse
    {
        public TwinNewKeyResponse(byte[] message = null)
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
