// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InField_63_CH.cs" company="Cybersoft">
//   Author: Kaven
// </copyright>
// <summary>
//   Defines the InField_63_CH type.
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
    using Com.Cybersoft4u.TM.VISA.SubFieldCodec;

    #endregion

    internal class InField_63_CH : ConvertUnitBase
    {
        protected internal static string ToPosAdditionalDataTokenCH(VISA origin, MCOLA target, string inputString)
        {
            try
            {
                var token = new TokenGroup();
                token.Decode(inputString);
                token.Tokens["CH"] = new TokenCH();
                var token_CH = (TokenCH)token.Tokens["CH"];
                token_CH.DataDecode(string.Empty.PadLeft(40, ' '));
                var isTokenChange = false;


                // RESPONSE SOURCE/REASON CODE
                var f44Condition = !ObjectUtils.IsNull(origin.RawVal(FIELD.F_44))
                                   && origin.RawVal(FIELD.F_44).ToString().Length >= 2;
                if (f44Condition)
                {
                    token_CH.RespSrcRsnCde = origin.RawVal(FIELD.F_44).ToString()[1].ToString();
                }

                // RECUR-PMNT-IND
                if (!ObjectUtils.IsNull(origin.RawVal(FIELD.F_126)))
                {
                    var f126 = origin.RawVal(FIELD.F_126);
                    var f126_13 = (string)new F_126VISABitmapField((byte[])f126).Field(FIELD.F_13);
                    if (!ObjectUtils.IsNull(f126_13))
                    {
                        token_CH.RecurPmntInd = f126_13.Trim().PadLeft(1, ' ');
                    }

                    isTokenChange = true;
                }

                // RVSL-RSN-IND
                if ("11".Equals(origin.RawVal(FIELD.F_25)))
                {
                    token_CH.RvslRsnInd = "1";
                    isTokenChange = true;
                }

                // TERM - TYP
                var f60 = origin.RawVal(FIELD.F_60);
                if (!ObjectUtils.IsNull(f60) && f60.ToString().Length >= 1)
                {
                    token_CH.TermTyp = f60.ToString()[0].ToString().Trim().PadLeft(1, ' ');
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
                WriteLogException(typeof(InField_63_CH), string.Format("Error occur(InField_63_CH): {0}", e));
                return inputString;
            }
        }
    }
}
