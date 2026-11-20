// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="SignOffRequest.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the SignOffRequest type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.VISA.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;

    #endregion

    public class StinSignOffRequest : VISARequest, SignOff
    {
        protected override void ConstructProcess()
        {
            base.ConstructProcess();
            this.Mti("0800");
            var utcDateTime = TimeUtils.CurrentIso8583UtcDatetime();
            var dateTime = TimeUtils.CurrentIso8583Datetime();
            var stan = SerialGenerator.Stan();
            this.RawVal(FIELD.F_7, utcDateTime)
                .RawVal(FIELD.F_11, stan)
                .RawVal(FIELD.F_37, TimeUtils.CurrentYddd() + dateTime.Substring(0, 2) + stan)
                .RawVal(FIELD.F_70, FieldConstant.F70.StinSignOff);
        }
    }
}
