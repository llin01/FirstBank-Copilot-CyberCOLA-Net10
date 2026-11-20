// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InField_63_B4.cs" company="Cybersoft">
//   Author: Kaven
// </copyright>
// <summary>
//   Defines the InField_63_B4 type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.TM.VISA.Converter.InField.InField_63_Token
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.MCOLA;
    using Com.Cybersoft4u.TM.ISO8583.Nccc;
    using Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens;

    #endregion

    internal class InField_63_B4 : ConvertUnitBase
    {
        protected internal static string ToPosAdditionalDataTokenB4(VISA origin, MCOLA target, string inputString)
        {
            try
            {
                var token = new TokenGroup();
                token.Decode(inputString);
                token.Tokens["B4"] = new TokenB4();
                var token_B4 = (TokenB4)token.Tokens["B4"];
                token_B4.DataDecode(string.Empty.PadLeft(20, ' '));
                var isTokenChange = false;

                // PT-SRV-ENTRY-MDE
                if (!ObjectUtils.IsNull(origin.RawVal(FIELD.F_22)))
                {
                    token_B4.PtSrvEntryMde = origin.RawVal(FIELD.F_22).ToString().Substring(0, 3).Trim().PadLeft(3, ' ');
                    isTokenChange = true;
                }

                var f60 = origin.RawVal(FIELD.F_60);
                if (!ObjectUtils.IsNull(f60))
                {
                    // TERM-ENTRY-CAP
                    if (f60.ToString().Length >= 2)
                    {
                        token_B4.TermEntryCap = f60.ToString()[1].ToString().Trim().PadLeft(1, ' ');
                    }

                    // LAST_EMV_STAT
                    if (f60.ToString().Length >= 3)
                    {
                        token_B4.LastEmvStat = f60.ToString()[2].ToString().Trim().PadLeft(1, ' ');
                    }

                    // DATA_SUSPECT
                    if (f60.ToString().Length >= 8)
                    {
                        token_B4.DataSuspect = f60.ToString()[7].ToString().Trim().PadLeft(1, ' ');
                    }

                    isTokenChange = true;
                }

                // APPL-PAN-SEQ-NUM
                if (!ObjectUtils.IsNull(origin.RawVal(FIELD.F_23)))
                {
                    token_B4.ApplPanSeqNum = origin.RawVal(FIELD.F_23).ToString().Trim().PadLeft(4, '0').Substring(2, 2);
                    isTokenChange = true;
                }

                // ARQC-VERIFY
                var mtiCondition = "01".Equals(origin.Mti().Substring(0, 2))
                                   || "02".Equals(origin.Mti().Substring(0, 2))
                                   || "04".Equals(origin.Mti().Substring(0, 2));
                var f44Condition = !ObjectUtils.IsNull(origin.RawVal(FIELD.F_44))
                                   && origin.RawVal(FIELD.F_44).ToString().Length >= 9;
                if (mtiCondition && f44Condition)
                {
                    token_B4.ArqcVerify = origin.RawVal(FIELD.F_44).ToString()[8].ToString().Trim().PadLeft(1, ' ');
                    isTokenChange = true;
                }

                if (isTokenChange)
                {
                    var outputString = token.Encode();
                    return outputString;
                }

                return inputString;
            }
            catch (Exception e)
            {
                WriteLogException(typeof(InField_63_B4), string.Format("Error occur(InField_63_B4): {0}", e));
                return inputString;
            }
        }
    }
}
