// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InField_120.cs" company="Cybersoft">
//   Author: Kaven
// </copyright>
// <summary>
//   Defines the InField_120 type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.TM.VISA.Converter.InField
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.MCOLA;
    using Com.Cybersoft4u.TM.VISA.SubFieldCodec.F127;

    #endregion

    internal class InField_120 : ConvertUnitBase
    {
        protected internal static void ToTargetDE120(VISA origin, MCOLA target)
        {
            try
            {
                var nccc_f120 = string.Empty;

                if (ObjectUtils.IsNull(origin.RawVal(FIELD.F_127)))
                {
                    return;
                }

                var f127 = F_127VISAFieldAEMPR.Default.Decode((byte[])origin.RawVal(FIELD.F_127));

                // PROC-UNIT
                nccc_f120 += "PCAS";

                // UPDATE-CODE
                nccc_f120 += string.Empty.PadLeft(2, ' ');

                // CARD-NO
                nccc_f120 += string.Empty.PadRight(16, '0');

                // FILLER
                nccc_f120 += string.Empty.PadLeft(12, ' ');

                // CNTRY-CODE
                nccc_f120 += string.Empty.PadLeft(3, ' ');

                // OPER-ID
                nccc_f120 += string.Empty.PadLeft(2, ' ');

                if (!ObjectUtils.IsNull(origin.RawVal(FIELD.F_101)))
                {
                    if ("E2".Equals(origin.RawVal(FIELD.F_101).ToString()))
                    {
                        // ACT-CODE
                        nccc_f120 += (f127.Length >= 2)
                                         ? f127.Substring(0, 2).Trim().PadLeft(2, ' ')
                                         : string.Empty.PadLeft(2, ' ');

                        // REGION-CODE
                        nccc_f120 += (f127.Length >= 11)
                                         ? f127.Substring(2, 9).Trim().PadRight(9, ' ')
                                         : string.Empty.PadRight(9, ' ');
                    }
                    else
                    {
                        nccc_f120 += string.Empty.PadLeft(11, ' ');
                    }
                }
                else
                {
                    nccc_f120 += string.Empty.PadLeft(11, ' ');
                }

                target.RawVal(FIELD.F_120, nccc_f120);
            }
            catch (Exception e)
            {
                WriteLogException(typeof(InField_120), string.Format("Error occur(InField_120): {0}", e));
            }
        }
    }
}
