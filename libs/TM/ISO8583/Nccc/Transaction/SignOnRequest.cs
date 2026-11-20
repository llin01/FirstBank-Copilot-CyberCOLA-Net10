// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="SignOnRequest.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the SignOnRequest type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.Category.Attributes;

    #endregion

    [TransactionType("0800")]

    public class SignOnRequest : NcccRequest
    {
        public SignOnRequest(byte[] message = null)
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
                .RawVal(FIELD.F_48, NcccTransactionConst.FieldConstant.F48.NetworkConstData)
                .RawVal(FIELD.F_70, NcccTransactionConst.FieldConstant.F70.SignOn);
        }
    }
}
