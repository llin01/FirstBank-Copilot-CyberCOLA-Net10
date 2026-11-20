// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InField_63_C0.cs" company="Cybersoft">
//   Author: Kaven
// </copyright>
// <summary>
//   Defines the InField_63_C0 type.
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

    internal class InField_63_C0 : ConvertUnitBase
    {
        protected internal static string ToPosAdditionalDataTokenC0(VISA origin, MCOLA target, string inputString)
        {
            try
            {
                var token = new TokenGroup();
                token.Decode(inputString);
                token.Tokens["C0"] = new TokenC0();
                var token_C0 = (TokenC0)token.Tokens["C0"];
                token_C0.DataDecode(string.Empty.PadLeft(26, ' '));
                var f126 = origin.RawVal(FIELD.F_126);
                var isTokenChange = false;

                if (!ObjectUtils.IsNull(f126))
                {
                    var f126_10 = (string)new F_126VISABitmapField((byte[])f126).Field(FIELD.F_10);

                    if (!ObjectUtils.IsNull(f126_10))
                    {
                        // CVD-FLD
                        //token_C0.CvdFld = f126_10.Substring(2, 4).Trim().PadRight(4, ' ');
                        token_C0.CvdFld = f126_10.Substring(3, 3).Trim().PadRight(4, ' ');

                        // CVD-FLD-PRESENT
                        token_C0.CvdFldPresent = f126_10.Substring(0, 1).Trim().PadLeft(1, ' ');

                        isTokenChange = true;
                    }

                    var f126_20 = (string)new F_126VISABitmapField((byte[])f126).Field(FIELD.F_20);

                    if (!ObjectUtils.IsNull(f126_20))
                    {
                        // 3DS Indicator
                        token_C0.Visa3DSInd = f126_20.Trim().PadRight(1, ' ');

                        isTokenChange = true;
                    }
                }

                // MO/TO or EC flag
                if ("0100".Equals(origin.Mti()) || "0101".Equals(origin.Mti()) || "0120".Equals(origin.Mti())
                    || "0200".Equals(origin.Mti()) || "0220".Equals(origin.Mti())
                    || "0400".Equals(origin.Mti()))
                {
                    token_C0.MotoOrECFlag = "0";

                    var f60 = origin.RawVal(FIELD.F_60);
                    if (!ObjectUtils.IsNull(f60) && f60.ToString().Length >= 10)
                    {
                        token_C0.MotoOrECFlag = f60.ToString()[9].ToString().Trim().PadLeft(1, ' ');
                    }

                    isTokenChange = true;
                }

                // CAVV/AAV Results
                var mtiCondition = "0100".Equals(origin.Mti()) || "0101".Equals(origin.Mti())
                                   || "0200".Equals(origin.Mti()) || "0220".Equals(origin.Mti())
                                   || "0120".Equals(origin.Mti());
                var f44Condition = !ObjectUtils.IsNull(origin.RawVal(FIELD.F_44))
                                   && origin.RawVal(FIELD.F_44).ToString().Length >= 15;
                if (mtiCondition && f44Condition)
                {
                    token_C0.CavvAavResults = origin.RawVal(FIELD.F_44).ToString()[14].ToString();
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
                WriteLogException(typeof(InField_63_C0), string.Format("Error occur(InField_63_C0): {0}", e));
                return inputString;
            }
        }
    }
}
