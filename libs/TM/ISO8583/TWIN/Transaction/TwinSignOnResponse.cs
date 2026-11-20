// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TwinSignOnResponse.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TwinSignOnResponse type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.TWIN.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583.Category.Attributes;

    #endregion

    [TransactionType("0810")]

    public class TwinSignOnResponse : TwinResponse
    {
        public TwinSignOnResponse(byte[] message = null)
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
