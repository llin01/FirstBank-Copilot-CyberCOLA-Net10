// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OutField_48.cs" company="Cybersoft">
//   Author: Kaven
// </copyright>
// <summary>
//   Defines the OutField_48 type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.TM.VISA.Converter.OutField
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.MCOLA;
    using Com.Cybersoft4u.TM.VISA.SubFieldCodec.F48;

    #endregion

    internal class OutField_48 : ConvertUnitBase
    {
        protected internal static void ToVisaF48(VISA origin, MCOLA target)
        {
            try
            {
                if (ObjectUtils.IsNull(target.RawVal(FIELD.F_113)))
                {
                    return;
                }

                var fillToF48 = F_48Field.Default.Encode(target.RawVal(FIELD.F_113).ToString());

                origin.RawVal(FIELD.F_48, fillToF48);
            }
            catch (Exception e)
            {
                WriteLogException(typeof(OutField_48), string.Format("Error occur(OutField_48): {0}", e));
            }
        }
    }
}
