// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InField_124.cs" company="Cybersoft">
//   Author: Kaven
// </copyright>
// <summary>
//   Defines the InField_124 type.
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

    internal class InField_124 : ConvertUnitBase
    {
        protected internal static void ToTargetDE124(VISA origin, MCOLA target)
        {
            try
            {
                //var nccc_f124 = new string[47];

                // for 20170302 for NCPS changed 
                var nccc_f124 = new string[42];

                var f123 = TlvFiller.TryDecodeTlv(FIELD.F_123, origin);
                var f125 = TlvFiller.TryDecodeTlv(FIELD.F_125, origin);
                var f127 = TlvFiller.TryDecodeTlv(FIELD.F_127, origin);

                var condition01 = "0100".Equals(origin.Mti()) || "0200".Equals(origin.Mti()) || "0101".Equals(origin.Mti())
                                  || "0120".Equals(origin.Mti()) || "0220".Equals(origin.Mti()) || "0400".Equals(origin.Mti())
                                  || "0420".Equals(origin.Mti()) || "0620".Equals(origin.Mti());
                var condition03 = ObjectUtils.IsNull(f123) && ObjectUtils.IsNull(f125) && ObjectUtils.IsNull(f127);
                if (condition03)
                {
                    return;
                }

                //nccc_f124[0] = string.Empty;
                //nccc_f124[1] = string.Empty.PadRight(9, ' ');
                //nccc_f124[2] = string.Empty.PadRight(40, ' ');
                //nccc_f124[3] = string.Empty.PadRight(26, ' ');
                //nccc_f124[4] = string.Empty.PadRight(1, ' ');
                //nccc_f124[5] = string.Empty.PadRight(2, ' ');
                //nccc_f124[6] = string.Empty.PadRight(4, ' ');
                //nccc_f124[7] = string.Empty.PadRight(3, ' ');
                //nccc_f124[8] = string.Empty.PadRight(7, ' ');
                //nccc_f124[9] = string.Empty.PadRight(19, ' ');
                //nccc_f124[10] = string.Empty.PadRight(2, ' ');
                //nccc_f124[11] = string.Empty.PadRight(11, ' ');
                //nccc_f124[12] = string.Empty.PadRight(19, ' ');
                //nccc_f124[13] = string.Empty.PadRight(32, ' ');
                //nccc_f124[14] = string.Empty.PadRight(4, ' ');
                //nccc_f124[15] = string.Empty.PadRight(2, ' ');
                //nccc_f124[16] = string.Empty.PadRight(1, ' ');
                //nccc_f124[17] = string.Empty.PadRight(1, ' ');
                //nccc_f124[18] = string.Empty.PadRight(32, ' ');
                //nccc_f124[19] = string.Empty.PadRight(8, ' ');
                //nccc_f124[20] = string.Empty.PadRight(12, ' ');
                //nccc_f124[21] = string.Empty.PadRight(2, ' ');
                //nccc_f124[22] = string.Empty.PadRight(2, ' ');
                //nccc_f124[23] = string.Empty.PadRight(2, ' ');
                //nccc_f124[24] = string.Empty.PadRight(2, ' ');
                //nccc_f124[25] = string.Empty.PadRight(2, ' ');
                //nccc_f124[26] = string.Empty.PadRight(2, ' ');
                //nccc_f124[27] = string.Empty.PadRight(2, ' ');
                //nccc_f124[28] = string.Empty.PadRight(2, ' ');
                //nccc_f124[29] = string.Empty.PadRight(3, ' ');
                //nccc_f124[30] = string.Empty.PadRight(48, ' ');
                //nccc_f124[31] = string.Empty.PadRight(15, ' ');
                //nccc_f124[32] = string.Empty.PadRight(16, ' ');
                //nccc_f124[33] = string.Empty.PadRight(25, ' ');
                //nccc_f124[34] = string.Empty.PadRight(15, ' ');
                //nccc_f124[35] = string.Empty.PadRight(1, ' ');
                //nccc_f124[36] = string.Empty.PadRight(10, ' ');
                //nccc_f124[37] = string.Empty.PadRight(2, ' ');
                //nccc_f124[38] = string.Empty.PadRight(2, ' ');
                //nccc_f124[39] = string.Empty.PadRight(30, ' ');
                //nccc_f124[40] = string.Empty.PadRight(2, ' ');
                //nccc_f124[41] = string.Empty.PadRight(32, ' ');
                //nccc_f124[42] = string.Empty.PadRight(32, ' ');
                //nccc_f124[43] = string.Empty.PadRight(64, ' ');
                //nccc_f124[44] = string.Empty.PadRight(32, ' ');
                //nccc_f124[45] = string.Empty.PadRight(19, ' ');
                //nccc_f124[46] = string.Empty.PadRight(4, ' ');

                // for 20170302 for NCPS changed 

                nccc_f124[0] = string.Empty;
                nccc_f124[1] = string.Empty.PadRight(26, ' ');
                nccc_f124[2] = string.Empty.PadRight(1, ' ');
                nccc_f124[3] = string.Empty.PadRight(2, ' ');
                nccc_f124[4] = string.Empty.PadRight(4, ' ');
                nccc_f124[5] = string.Empty.PadRight(3, ' ');
                nccc_f124[6] = string.Empty.PadRight(7, ' ');
                nccc_f124[7] = string.Empty.PadRight(19, ' ');
                nccc_f124[8] = string.Empty.PadRight(2, ' ');
                nccc_f124[9] = string.Empty.PadRight(11, ' ');
                nccc_f124[10] = string.Empty.PadRight(19, ' ');
                nccc_f124[11] = string.Empty.PadRight(32, ' ');
                nccc_f124[12] = string.Empty.PadRight(4, ' ');
                nccc_f124[13] = string.Empty.PadRight(2, ' ');
                nccc_f124[14] = string.Empty.PadRight(1, ' ');
                nccc_f124[15] = string.Empty.PadRight(1, ' ');
                nccc_f124[16] = string.Empty.PadRight(32, ' ');
                nccc_f124[17] = string.Empty.PadRight(8, ' ');
                nccc_f124[18] = string.Empty.PadRight(12, ' ');
                nccc_f124[19] = string.Empty.PadRight(2, ' ');
                nccc_f124[20] = string.Empty.PadRight(2, ' ');
                nccc_f124[21] = string.Empty.PadRight(2, ' ');
                nccc_f124[22] = string.Empty.PadRight(2, ' ');
                nccc_f124[23] = string.Empty.PadRight(2, ' ');
                nccc_f124[24] = string.Empty.PadRight(2, ' ');
                nccc_f124[25] = string.Empty.PadRight(2, ' ');
                nccc_f124[26] = string.Empty.PadRight(2, ' ');
                nccc_f124[27] = string.Empty.PadRight(3, ' ');
                nccc_f124[28] = string.Empty.PadRight(48, ' ');
                nccc_f124[29] = string.Empty.PadRight(15, ' ');
                nccc_f124[30] = string.Empty.PadRight(16, ' ');
                nccc_f124[31] = string.Empty.PadRight(15, ' ');
                nccc_f124[32] = string.Empty.PadRight(1, ' ');
                nccc_f124[33] = string.Empty.PadRight(10, ' ');
                nccc_f124[34] = string.Empty.PadRight(2, ' ');
                nccc_f124[35] = string.Empty.PadRight(2, ' ');
                nccc_f124[36] = string.Empty.PadRight(30, ' ');
                nccc_f124[37] = string.Empty.PadRight(2, ' ');
                nccc_f124[38] = string.Empty.PadRight(32, ' ');
                nccc_f124[39] = string.Empty.PadRight(32, ' ');
                nccc_f124[40] = string.Empty.PadRight(19, ' ');
                nccc_f124[41] = string.Empty.PadRight(4, ' ');

                if (!ObjectUtils.IsNull(f123))
                {
                    if ("0100".Equals(origin.Mti()) || "0101".Equals(origin.Mti()) || "0120".Equals(origin.Mti())
                        || "0200".Equals(origin.Mti()) || "0220".Equals(origin.Mti())
                        || "0400".Equals(origin.Mti()) || "0420".Equals(origin.Mti()) || "0620".Equals(origin.Mti()))
                    {
                        //nccc_f124[1] = TlvFiller.TryGetEBCDICTag(f123, "66", "C0").Trim().PadRight(9, ' ');
                        //nccc_f124[2] = TlvFiller.TryGetEBCDICTag(f123, "66", "CF").Trim().PadRight(40, ' ');
                        //nccc_f124[3] = TlvFiller.TryGetEBCDICTag(f123, "66", "D4").Trim().PadRight(26, ' ');

                        // for 20170302 for NCPS changed 
                        nccc_f124[1] = TlvFiller.TryGetEBCDICTag(f123, "66", "D4").Trim().PadRight(26, ' ');
                    }

                    if ("0100".Equals(origin.Mti()) || "0101".Equals(origin.Mti()) || "0120".Equals(origin.Mti())
                        || "0200".Equals(origin.Mti()) || "0220".Equals(origin.Mti())
                        || "0400".Equals(origin.Mti()) || "0420".Equals(origin.Mti()) || "0600".Equals(origin.Mti())
                        || "0620".Equals(origin.Mti()))
                    {
                        //nccc_f124[4] = TlvFiller.TryGetEBCDICTag(f123, "67", "03").Trim().PadRight(1, ' ');
                        //nccc_f124[5] = TlvFiller.TryGetEBCDICTag(f123, "67", "04").Trim().PadLeft(2, '0');

                        // for 20170302 for NCPS changed 
                        nccc_f124[2] = TlvFiller.TryGetEBCDICTag(f123, "67", "03").Trim().PadRight(1, ' ');
                        nccc_f124[3] = TlvFiller.TryGetEBCDICTag(f123, "67", "04").Trim().PadLeft(2, '0');
                    }

                    if ("0100".Equals(origin.Mti()) || "0101".Equals(origin.Mti()) || "0120".Equals(origin.Mti())
                        || "0200".Equals(origin.Mti()) || "0220".Equals(origin.Mti())
                        || "0400".Equals(origin.Mti()) || "0420".Equals(origin.Mti()) || "0312".Equals(origin.Mti())
                        || "0620".Equals(origin.Mti()))
                    {
                        //nccc_f124[6] = TlvFiller.TryGetEBCDICTag(f123, "68", "1F31").PadLeft(4, '0');
                        //nccc_f124[7] = TlvFiller.TryGetEBCDICTag(f123, "68", "1F32").PadLeft(3, '0');
                        //nccc_f124[8] = TlvFiller.TryGetEBCDICTag(f123, "68", "1F33").PadLeft(7, '0');
                        //nccc_f124[9] = TlvFiller.TryGetEBCDICTag(f123, "68", "01").Trim().PadRight(19, ' ');
                        //nccc_f124[10] = TlvFiller.TryGetEBCDICTag(f123, "68", "02").Trim().PadRight(2, ' ');
                        //nccc_f124[11] = TlvFiller.TryGetEBCDICTag(f123, "68", "03").PadLeft(11, '0');
                        //nccc_f124[12] = TlvFiller.TryGetEBCDICTag(f123, "68", "04").Trim().PadRight(19, ' ');
                        //nccc_f124[13] = TlvFiller.TryGetEBCDICTag(f123, "68", "05").Trim().PadRight(32, ' ');
                        //nccc_f124[14] = TlvFiller.TryGetEBCDICTag(f123, "68", "06").PadLeft(4, '0');
                        //nccc_f124[15] = TlvFiller.TryGetEBCDICTag(f123, "68", "07").Trim().PadRight(2, ' ');
                        //nccc_f124[16] = TlvFiller.TryGetEBCDICTag(f123, "68", "08").Trim().PadRight(1, ' ');
                        //nccc_f124[17] = TlvFiller.TryGetEBCDICTag(f123, "68", "0A").Trim().PadRight(1, ' ');
                        //nccc_f124[18] = TlvFiller.TryGetEBCDICTag(f123, "68", "0B").Trim().PadRight(32, ' ');
                        //nccc_f124[19] = TlvFiller.TryGetEBCDICTag(f123, "68", "1A").Trim().PadRight(8, ' ');
                        //nccc_f124[20] = TlvFiller.TryGetEBCDICTag(f123, "68", "1B").PadRight(12, ' ');
                        //nccc_f124[21] = TlvFiller.TryGetEBCDICTag(f123, "68", "1C").PadRight(2, ' ');
                        //nccc_f124[22] = TlvFiller.TryGetEBCDICTag(f123, "68", "1D").PadRight(2, ' ');
                        //nccc_f124[23] = TlvFiller.TryGetEBCDICTag(f123, "68", "10").PadLeft(2, '0');
                        //nccc_f124[24] = TlvFiller.TryGetEBCDICTag(f123, "68", "11").Trim().PadRight(2, ' ');
                        //nccc_f124[25] = TlvFiller.TryGetEBCDICTag(f123, "68", "12").PadLeft(2, '0');
                        //nccc_f124[26] = TlvFiller.TryGetEBCDICTag(f123, "68", "13").PadLeft(2, '0');
                        //nccc_f124[27] = TlvFiller.TryGetEBCDICTag(f123, "68", "14").PadLeft(2, '0');

                        // for 20170302 for NCPS changed 
                        nccc_f124[4] = TlvFiller.TryGetEBCDICTag(f123, "68", "1F31").PadLeft(4, '0');
                        nccc_f124[5] = TlvFiller.TryGetEBCDICTag(f123, "68", "1F32").PadLeft(3, '0');
                        nccc_f124[6] = TlvFiller.TryGetEBCDICTag(f123, "68", "1F33").PadLeft(7, '0');
                        nccc_f124[7] = TlvFiller.TryGetEBCDICTag(f123, "68", "01").Trim().PadRight(19, ' ');
                        nccc_f124[8] = TlvFiller.TryGetEBCDICTag(f123, "68", "02").Trim().PadRight(2, ' ');
                        nccc_f124[9] = TlvFiller.TryGetEBCDICTag(f123, "68", "03").PadLeft(11, '0');
                        nccc_f124[10] = TlvFiller.TryGetEBCDICTag(f123, "68", "04").Trim().PadRight(19, ' ');
                        nccc_f124[11] = TlvFiller.TryGetEBCDICTag(f123, "68", "05").Trim().PadRight(32, ' ');
                        nccc_f124[12] = TlvFiller.TryGetEBCDICTag(f123, "68", "06").PadLeft(4, '0');
                        nccc_f124[13] = TlvFiller.TryGetEBCDICTag(f123, "68", "07").Trim().PadRight(2, ' ');
                        nccc_f124[14] = TlvFiller.TryGetEBCDICTag(f123, "68", "08").Trim().PadRight(1, ' ');
                        nccc_f124[15] = TlvFiller.TryGetEBCDICTag(f123, "68", "0A").Trim().PadRight(1, ' ');
                        nccc_f124[16] = TlvFiller.TryGetEBCDICTag(f123, "68", "0B").Trim().PadRight(32, ' ');
                        nccc_f124[17] = TlvFiller.TryGetEBCDICTag(f123, "68", "1A").Trim().PadRight(8, ' ');
                        nccc_f124[18] = TlvFiller.TryGetTag(f123, "68", "1B").PadRight(12, ' ');
                        nccc_f124[19] = TlvFiller.TryGetTag(f123, "68", "1C").PadRight(2, ' ');
                        nccc_f124[20] = TlvFiller.TryGetTag(f123, "68", "1D").PadRight(2, ' ');
                        nccc_f124[21] = TlvFiller.TryGetEBCDICTag(f123, "68", "10").PadLeft(2, '0');
                        nccc_f124[22] = TlvFiller.TryGetEBCDICTag(f123, "68", "11").Trim().PadRight(2, ' ');
                        nccc_f124[23] = TlvFiller.TryGetEBCDICTag(f123, "68", "12").PadLeft(2, '0');
                        nccc_f124[24] = TlvFiller.TryGetEBCDICTag(f123, "68", "13").PadLeft(2, '0');
                        nccc_f124[25] = TlvFiller.TryGetEBCDICTag(f123, "68", "14").PadLeft(2, '0');
                    }
                }

                if (!ObjectUtils.IsNull(f125))
                {
                    if ("0100".Equals(origin.Mti()) || "0101".Equals(origin.Mti()) || "0120".Equals(origin.Mti())
                        || "0200".Equals(origin.Mti()) || "0220".Equals(origin.Mti())
                        || "0400".Equals(origin.Mti()) || "0420".Equals(origin.Mti()) || "0312".Equals(origin.Mti())
                        || "0600".Equals(origin.Mti()) || "0620".Equals(origin.Mti()))
                    {
                        //nccc_f124[28] = TlvFiller.TryGetEBCDICTag(f125, "01", "01").Trim().PadRight(2, ' ');
                        //nccc_f124[29] = TlvFiller.TryGetEBCDICTag(f125, "01", "02").Trim().PadRight(3, ' ');
                        //nccc_f124[30] = TlvFiller.TryGetEBCDICTag(f125, "01", "03").Trim().PadRight(48, ' ');
                        //nccc_f124[31] = TlvFiller.TryGetEBCDICTag(f125, "01", "04").PadRight(15, ' ');
                        //nccc_f124[32] = TlvFiller.TryGetEBCDICTag(f125, "01", "05").Trim().PadRight(16, ' ');
                        //nccc_f124[33] = TlvFiller.TryGetEBCDICTag(f125, "01", "06").Trim().PadRight(25, ' ');
                        //nccc_f124[34] = TlvFiller.TryGetEBCDICTag(f125, "01", "07").Trim().PadRight(15, ' ');
                        //nccc_f124[35] = TlvFiller.TryGetEBCDICTag(f125, "02", "03").Trim().PadRight(1, ' ');
                        //nccc_f124[36] = TlvFiller.TryGetEBCDICTag(f125, "02", "04").Trim().PadRight(10, ' ');
                        //nccc_f124[37] = TlvFiller.TryGetEBCDICTag(f125, "02", "05").PadLeft(2, '0');
                        //nccc_f124[38] = TlvFiller.TryGetEBCDICTag(f125, "02", "06").PadLeft(2, '0');
                        //nccc_f124[39] = TlvFiller.TryGetEBCDICTag(f125, "02", "07").Trim().PadRight(30, ' ');
                        //nccc_f124[40] = TlvFiller.TryGetEBCDICTag(f125, "02", "08").PadLeft(2, '0');
                        //nccc_f124[41] = TlvFiller.TryGetEBCDICTag(f125, "02", "09").Trim().PadRight(32, ' ');
                        //nccc_f124[42] = TlvFiller.TryGetEBCDICTag(f125, "02", "0A").Trim().PadRight(32, ' ');

                        // for 20170302 for NCPS changed 
                        nccc_f124[26] = TlvFiller.TryGetEBCDICTag(f125, "01", "01").Trim().PadRight(2, ' ');
                        nccc_f124[27] = TlvFiller.TryGetEBCDICTag(f125, "01", "02").Trim().PadRight(3, ' ');
                        nccc_f124[28] = TlvFiller.TryGetEBCDICTag(f125, "01", "03").Trim().PadRight(48, ' ');
                        nccc_f124[29] = TlvFiller.TryGetEBCDICTag(f125, "01", "04").PadRight(15, ' ');
                        nccc_f124[30] = TlvFiller.TryGetEBCDICTag(f125, "01", "05").Trim().PadRight(16, ' ');
                        nccc_f124[31] = TlvFiller.TryGetEBCDICTag(f125, "01", "07").Trim().PadRight(15, ' ');
                        nccc_f124[32] = TlvFiller.TryGetEBCDICTag(f125, "02", "03").Trim().PadRight(1, ' ');
                        nccc_f124[33] = TlvFiller.TryGetEBCDICTag(f125, "02", "04").Trim().PadRight(10, ' ');
                        nccc_f124[34] = TlvFiller.TryGetEBCDICTag(f125, "02", "05").PadLeft(2, '0');
                        nccc_f124[35] = TlvFiller.TryGetEBCDICTag(f125, "02", "06").PadLeft(2, '0');
                        nccc_f124[36] = TlvFiller.TryGetEBCDICTag(f125, "02", "07").Trim().PadRight(30, ' ');
                        nccc_f124[37] = TlvFiller.TryGetEBCDICTag(f125, "02", "08").PadLeft(2, '0');
                        nccc_f124[38] = TlvFiller.TryGetEBCDICTag(f125, "02", "09").Trim().PadRight(32, ' ');
                    }
                }

                if (!ObjectUtils.IsNull(f127))
                {
                    if ("0100".Equals(origin.Mti()) || "0101".Equals(origin.Mti()) || "0600".Equals(origin.Mti())
                        || "0200".Equals(origin.Mti())
                        || "0620".Equals(origin.Mti()))
                    {
                        //nccc_f124[43] = TlvFiller.TryGetEBCDICTag(f127, "40", "01").Trim().PadRight(64, ' ');
                        //nccc_f124[44] = TlvFiller.TryGetEBCDICTag(f127, "40", "02").Trim().PadRight(32, '0');

                        // for 20170302 for NCPS changed 
                        nccc_f124[39] = TlvFiller.TryGetEBCDICTag(f127, "40", "02").Trim().PadRight(32, '0');
                    }

                    if ("0100".Equals(origin.Mti()) || "0101".Equals(origin.Mti()) || "0312".Equals(origin.Mti())
                        || "0200".Equals(origin.Mti())
                        || "0620".Equals(origin.Mti()))
                    {
                        //nccc_f124[45] = TlvFiller.TryGetEBCDICTag(f127, "41", "01").PadRight(19, ' ');
                        //nccc_f124[46] = TlvFiller.TryGetEBCDICTag(f127, "41", "02").PadRight(4, ' ');

                        // for 20170302 for NCPS changed 
                        nccc_f124[40] = TlvFiller.TryGetTag(f127, "41", "01").PadRight(19, ' ');
                        nccc_f124[41] = TlvFiller.TryGetTag(f127, "41", "02").PadRight(4, ' ');
                    }
                }

                var outputString = string.Empty;
                //for (var i = 0; i < 47; i++)
                // for 20170302 for NCPS changed 
                for (var i = 0; i < 42; i++)
                {
                    outputString += nccc_f124[i];
                }

                target.RawVal(FIELD.F_124, outputString);
            }
            catch (Exception e)
            {
                WriteLogException(typeof(InField_124), string.Format("Error occur(InField_124): {0}", e));
            }
        }
    }
}
