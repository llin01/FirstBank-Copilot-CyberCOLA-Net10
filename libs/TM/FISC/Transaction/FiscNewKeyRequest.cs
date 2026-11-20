// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="FiscFiscNewKeyRequest.cs">
// 
// </copyright>
// <summary>
//   Defines the FiscNewKeyRequest type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.FISC.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;

    #endregion

    public class FiscNewKeyRequest : FISC, ChangeKey
    {
        public FiscNewKeyRequest(byte[] message = null)
            : base(message)
        {
        }

        protected override void ConstructProcess()
        {
            base.ConstructProcess();
            this.Mti("0800");
            var utcDateTime = TimeUtils.CurrentIso8583UtcDatetime();
            var stan = SerialGenerator.Stan();
            this.RawVal(FIELD.F_7, utcDateTime)
                .RawVal(FIELD.F_11, stan)
                .RawVal(FIELD.F_48, FISCTransactionConst.NetworkManagementInformationCode.EncryptionKeyExchangeRequest)
                .RawVal(FIELD.F_53, "0002000000000000")
                .RawVal(FIELD.F_70, FISCTransactionConst.NetworkManagementInformationCode.EncryptionKeyExchangeRequest);
        }
    }
}
