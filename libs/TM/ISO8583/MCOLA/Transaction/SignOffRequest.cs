// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="SignOnRequest.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the SignOnRequest type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.MCOLA.Transaction
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;

    #endregion

    public class SignOffRequest : MCOLARequest, SignOff
    {
        public override Bytes Encode()
        {
            //this.SetFieldWhenNull(FIELD.F_2, () => string.Empty.PadRight(19, '0'))
            //    .SetFieldWhenNull(FIELD.F_7, TimeUtils.CurrentIso8583Datetime)
            //    .SetFieldWhenNull(FIELD.F_11, SerialGenerator.Stan)
            //    .SetFieldWhenNull(FIELD.F_33, () => string.Empty.PadRight(6, '0'))
            //    .SetFieldWhenNull(FIELD.F_70, () => "002")
            //    .SetFieldWhenNull(FIELD.F_94, () => "0I00000")
            //    .SetFieldWhenNull(FIELD.F_96, () => string.Empty.PadRight(8, '0'))
            //    .CheckNonNullFields(new[] { FIELD.F_2, FIELD.F_7, FIELD.F_11, FIELD.F_33, 
            //                                FIELD.F_70, FIELD.F_94, FIELD.F_96});
            this.SetFieldWhenNull(FIELD.F_7, TimeUtils.CurrentIso8583Datetime)
                .SetFieldWhenNull(FIELD.F_11, SerialGenerator.Stan)
                .SetFieldWhenNull(FIELD.F_70, () => "002")
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
