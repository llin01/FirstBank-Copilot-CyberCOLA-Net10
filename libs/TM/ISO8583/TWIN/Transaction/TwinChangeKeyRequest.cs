// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TwinChangeKeyRequest.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TwinChangeKeyRequest type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.TWIN.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.Category.Attributes;

    #endregion

    [TransactionType("0800")]

    public class TwinChangeKeyRequest : TwinRequest
    {
        public TwinChangeKeyRequest(byte[] message = null)
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
                .RawVal(FIELD.F_48, TwinTransactionConst.FieldConstant.F48.NetworkConstData)
                .RawVal(FIELD.F_53, "0002000000000000")
                .RawVal(FIELD.F_70, TwinTransactionConst.FieldConstant.F70.ChangeKey)
                .RawVal(FIELD.F_123, TwinTransactionConst.FieldConstant.F123.BankID);
        }
    }
}
