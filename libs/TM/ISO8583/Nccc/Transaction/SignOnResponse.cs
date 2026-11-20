// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="SignOnResponse.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the SignOnResponse type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583.Category.Attributes;

    #endregion

    [TransactionType("0810")]

    public class SignOnResponse : NcccResponse
    {
        public SignOnResponse(byte[] message = null)
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
