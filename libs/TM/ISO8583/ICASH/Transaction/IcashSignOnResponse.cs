// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="IcashSignOnResponse.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the IcashSignOnResponse type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.TM.ISO8583.ICASH.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583.Category.Attributes;

    #endregion

    [TransactionType("0810")]

    public class IcashSignOnResponse : IcashResponse
    {
        public IcashSignOnResponse(byte[] message = null)
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
