// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="IpassSignOffResponse.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the IpassSignOffResponse type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.IPASS.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583.Category.Attributes;

    #endregion

    [TransactionType("0810")]

    public class IpassSignOffResponse : IpassResponse
    {
        public IpassSignOffResponse(byte[] message = null)
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
