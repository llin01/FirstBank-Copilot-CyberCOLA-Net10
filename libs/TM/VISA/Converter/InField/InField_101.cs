// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InField_101.cs" company="Cybersoft">
//   Author: Kaven
// </copyright>
// <summary>
//   Defines the InField_101 type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.TM.VISA.Converter.InField
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.MCOLA;

    #endregion

    internal class InField_101 : ConvertUnitBase
    {
        protected internal static void ToFileName(VISA origin, MCOLA target)
        {
            try
            {
                if (ObjectUtils.IsNull(origin.RawVal(FIELD.F_101)))
                {
                    switch (origin.Mti())
                    {
                        case "0312":
                            WriteLogCodecFail(typeof(InField_101), "InField_101(): VISA DE 101 cannot found.");
                            break;
                    }

                    return;
                }

                var fileName = string.Empty;

                // PROC-UNIT
                var procUnit = origin.RawVal(FIELD.F_101).ToString();
                if ("0312".Equals(origin.Mti()))
                {
                    if ("E2".Equals(procUnit))
                    {
                        procUnit = "VP";
                    }
                }

                fileName = procUnit.PadRight(17, ' ');

                target.RawVal(FIELD.F_101, fileName);
            }
            catch (Exception e)
            {
                WriteLogException(typeof(InField_101), string.Format("Error occur(InField_101): {0}", e));
            }
        }
    }
}
