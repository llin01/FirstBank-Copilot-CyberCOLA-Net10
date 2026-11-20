// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TwinVerifyKeyRequest.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TwinVerifyKeyRequest type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.TWIN.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.Category.Attributes;

    #endregion

    [TransactionType("0800")]

    public class TwinVerifyKeyRequest : TwinRequest
    {
        public TwinVerifyKeyRequest(byte[] message = null)
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
                .RawVal(FIELD.F_53, "0002000000000000")
                .RawVal(FIELD.F_70, TwinTransactionConst.FieldConstant.F70.VerifyKey)
                .RawVal(FIELD.F_123, TwinTransactionConst.FieldConstant.F123.BankID);
        }
    }
}
