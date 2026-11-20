// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="IcashEchoTestResponse.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the IcashEchoTestResponse type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.ICASH.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583.Category.Attributes;

    #endregion

    [TransactionType("0810")]
    public class IcashEchoTestResponse : IcashResponse
    {
        public IcashEchoTestResponse(byte[] message = null)
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
