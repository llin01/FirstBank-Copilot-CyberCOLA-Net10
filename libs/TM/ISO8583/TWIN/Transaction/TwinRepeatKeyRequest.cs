// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TwinRepeatKeyRequest.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TwinRepeatKeyRequest type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.TWIN.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.Category.Attributes;

    #endregion

    [TransactionType("0800")]

    public class TwinRepeatKeyRequest : TwinRequest
    {
        internal TwinRepeatKeyRequest(byte[] message = null)
            : base(message)
        {
        }

        protected override void ConstructProcess()
        {
            base.ConstructProcess();
            this.Mti("0800");
            var UtcDateTime = TimeUtils.CurrentIso8583UtcDatetime();
            var stan = SerialGenerator.Stan();
            this.RawVal(FIELD.F_7, UtcDateTime)
                .RawVal(FIELD.F_11, stan)
                .RawVal(FIELD.F_48, TwinTransactionConst.FieldConstant.F48.NetworkConstData)
                .RawVal(FIELD.F_53, "0002000000000000")
                .RawVal(FIELD.F_70, TwinTransactionConst.FieldConstant.F70.RepeatKey);
        }
    }
}
