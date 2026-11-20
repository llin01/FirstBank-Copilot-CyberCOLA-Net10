// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="IcashSignOffResponse.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the IcashSignOffResponse type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.ICASH.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583.Category.Attributes;

    #endregion

    [TransactionType("0810")]

    public class IcashSignOffResponse : IcashResponse
    {
        public IcashSignOffResponse(byte[] message = null)
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
