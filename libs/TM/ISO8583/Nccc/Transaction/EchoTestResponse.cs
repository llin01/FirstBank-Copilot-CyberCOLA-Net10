// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="EchoTestResponse.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the EchoTestResponse type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583.Category.Attributes;

    #endregion

    [TransactionType("0810")]
    public class EchoTestResponse : NcccResponse
    {
        public EchoTestResponse(byte[] message = null)
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
