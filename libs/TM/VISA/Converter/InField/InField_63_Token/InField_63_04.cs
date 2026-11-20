// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InField_63_04.cs" company="Cybersoft">
//   Author: Kaven
// </copyright>
// <summary>
//   Defines the InField_63_04 type.
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

    internal class InField_63_04 : ConvertUnitBase
    {
        protected internal static string ToPosAdditionalDataToken04(VISA origin, MCOLA target, string inputString)
        {
            try
            {
                var token = new TokenGroup();
                token.Decode(inputString);
                token.Tokens["04"] = new Token04();
                var token_04 = (Token04)token.Tokens["04"];
                token_04.DataDecode(string.Empty.PadLeft(20, ' '));

                // Crd-very-flg
                var mtiCondition = "0100".Equals(origin.Mti()) || "0101".Equals(origin.Mti())
                                   || "0200".Equals(origin.Mti()) || "0220".Equals(origin.Mti())
                                   || "0120".Equals(origin.Mti()) || "0400".Equals(origin.Mti())
                                   || "0420".Equals(origin.Mti());
                var f44Condition = !ObjectUtils.IsNull(origin.RawVal(FIELD.F_44))
                                   && origin.RawVal(FIELD.F_44).ToString().Length >= 9;
                if (mtiCondition && f44Condition)
                {
                    switch (origin.RawVal(FIELD.F_44).ToString()[8].ToString())
                    {
                        case "2":
                            token_04.CrdVeryFlg = "Y";
                            break;
                        case "1":
                            token_04.CrdVeryFlg = "C";
                            break;
                        case " ":
                            token_04.CrdVeryFlg = " ";
                            break;
                    }
                }

                // Complete - tk2 - data
                token_04.CompleteTk2Data = "N";
                if (!ObjectUtils.IsNull(origin.RawVal(FIELD.F_22)))
                {
                    switch (origin.RawVal(FIELD.F_22).ToString().Substring(0, 2))
                    {
                        case "90":
                        case "91":
                        case "05":
                        case "07":
                            token_04.CompleteTk2Data = "Y";
                            break;
                    }
                }

                var outputString = token.Encode();
                return outputString;
            }
            catch (Exception e)
            {
                WriteLogException(typeof(InField_63_04), string.Format("Error occur(InField_63_04): {0}", e));
                return inputString;
            }
        }
    }
}
