// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="EchoTestRequest.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the EchoTestRequest type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.Nccc.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.Category.Attributes;

    #endregion

    [TransactionType("0800")]
    public class EchoTestRequest : NcccRequest
    {
        public EchoTestRequest(byte[] message = null)
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
                .RawVal(FIELD.F_70, NcccTransactionConst.FieldConstant.F70.EchoTest);
        }
    }
}
