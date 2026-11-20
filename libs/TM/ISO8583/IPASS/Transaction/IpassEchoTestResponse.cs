// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="IpassEchoTestResponse.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the IpassEchoTestResponse type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.IPASS.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583.Category.Attributes;

    #endregion

    [TransactionType("0810")]
    public class IpassEchoTestResponse : IpassResponse
    {
        public IpassEchoTestResponse(byte[] message = null)
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
