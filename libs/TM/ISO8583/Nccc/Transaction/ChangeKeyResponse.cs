// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="ChangeKeyResponse.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the ChangeKeyResponse type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583.Category.Attributes;

    #endregion

    [TransactionType("0800")]

    public class ChangeKeyResponse : NcccResponse
    {
        public ChangeKeyResponse(byte[] message = null)
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
