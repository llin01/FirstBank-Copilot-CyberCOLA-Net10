// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TwinSignOnRequest.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TwinSignOnRequest type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.TWIN.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.Category.Attributes;

    #endregion

    [TransactionType("0800")]

    public class TwinSignOnRequest : TwinRequest
    {
        public TwinSignOnRequest(byte[] message = null)
            : base(message)
        {
        }

        protected override void ConstructProcess()
        {
            base.ConstructProcess();
            this.Header().Decode("NCM000100070");
            this.Mti("0800");
            var utcDateTime = TimeUtils.CurrentIso8583UtcDatetime();
            var stan = SerialGenerator.Stan();
            this.RawVal(FIELD.F_7, utcDateTime)
                .RawVal(FIELD.F_11, stan)
                .RawVal(FIELD.F_70, TwinTransactionConst.FieldConstant.F70.SignOn);
        }
    }
}
