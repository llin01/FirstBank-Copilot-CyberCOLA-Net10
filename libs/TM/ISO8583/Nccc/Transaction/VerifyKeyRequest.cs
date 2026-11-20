// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="VerifyKeyRequest.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the VerifyKeyRequest type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.Category.Attributes;

    #endregion

    [TransactionType("0800")]

    public class VerifyKeyRequest : NcccRequest
    {
        public VerifyKeyRequest(byte[] message = null)
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
                .RawVal(FIELD.F_48, NcccTransactionConst.FieldConstant.F48.NetworkConstData)
                .RawVal(FIELD.F_53, "0002000000000000")
                .RawVal(FIELD.F_70, NcccTransactionConst.FieldConstant.F70.VerifyKey)
                .RawVal(FIELD.F_120, "123456");
        }
    }
}
