// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="EchoTestRequest.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the EchoTestRequest type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.MCOLA.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;

    #endregion

    public class EchoTestRequest : MCOLARequest, EchoTest
    {
        public override Bytes Encode()
        {
            this.SetFieldWhenNull(FIELD.F_7, TimeUtils.CurrentIso8583UtcDatetime)
                .SetFieldWhenNull(FIELD.F_11, SerialGenerator.Stan)
                .SetFieldWhenNull(FIELD.F_70, () => "301")
                .CheckNonNullFields(new[] { FIELD.F_7, FIELD.F_11, FIELD.F_70 });
            return base.Encode();
        }

        protected override void ConstructProcess()
        {
            base.ConstructProcess();
            this.Mti("0800");
        }
    }
}
