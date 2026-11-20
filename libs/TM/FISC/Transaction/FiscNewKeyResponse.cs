// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="FiscFiscNewKeyResponse.cs">
//
// </copyright>
// <summary>
//   Defines the FiscNewKeyResponse type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.FISC.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;

    #endregion


    public class FiscNewKeyResponse : FISC, ChangeKey
    {
        public FiscNewKeyResponse(byte[] message = null)
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
