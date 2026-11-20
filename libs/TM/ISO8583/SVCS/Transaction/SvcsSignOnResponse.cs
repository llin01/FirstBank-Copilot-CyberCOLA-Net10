// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="SvcsSignOnResponse.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the SvcsSignOnResponse type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.TM.ISO8583.SVCS.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583.Category.Attributes;

    #endregion

    [TransactionType("0810")]

    public class SvcsSignOnResponse : SvcsResponse
    {
        public SvcsSignOnResponse(byte[] message = null)
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
