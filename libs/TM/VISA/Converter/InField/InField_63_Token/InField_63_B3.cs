// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InField_63_B3.cs" company="Cybersoft">
//   Author: Kaven
// </copyright>
// <summary>
//   Defines the InField_63_B3 type.
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
    using System.Linq;

    #endregion

    internal class InField_63_B3 : ConvertUnitBase
    {
        protected internal static string ToPosAdditionalDataTokenB3(VISA origin, MCOLA target, string inputString)
        {
            try
            {
                var token = new TokenGroup();
                token.Decode(inputString);
                token.Tokens["B3"] = new TokenB3();
                var token_B3 = (TokenB3)token.Tokens["B3"];
                token_B3.DataDecode(string.Empty.PadLeft(80, ' '));
                var f55 = TlvFiller.TryDecodeTlv(FIELD.F_55, origin);
                var isTokenChange = false;

                if (!ObjectUtils.IsNull(f55))
                {
                    //token_B3.TermSerNum = TlvFiller.TryGetTag(f55, "01", "9F1E").Trim().PadRight(8, '0');
                    //var chkTermSerNum = TlvFiller.TryGetTag(f55, "01", "9F1E").Trim().PadRight(16, '0');
                    var chkTermSerNum = TlvFiller.TryGetTag(f55, "01", "9F1E").Trim();
                    //if ("0".PadRight(16, '0').Equals(chkTermSerNum))                   
                    if (chkTermSerNum.Length <= 8)
                    {
                        token_B3.TermSerNum = chkTermSerNum.PadRight(8, '0');
                    }
                    else
                    {
                        var TermSerNumWork = EbcdicConverter.GetString(chkTermSerNum.PadRight(16, '0'));
                        token_B3.TermSerNum = new string(TermSerNumWork.Where(c => !char.IsControl(c)).ToArray()).PadRight(8, '0');
                        //token_B3.TermSerNum = EbcdicConverter.GetString(chkTermSerNum);
                    }
                    token_B3.TermCap = TlvFiller.TryGetTag(f55, "01", "9F33").Trim().PadRight(6, '0');
                    token_B3.EmvTermTyp = TlvFiller.TryGetTag(f55, "01", "9F35").Trim().PadRight(2, '0');
                    token_B3.CvmRslts = TlvFiller.TryGetTag(f55, "01", "9F34").Trim().PadRight(6, '0');
                    token_B3.DfNameLgth = TlvFiller.TryGetTag(f55, "01", "84").Length.ToString().PadRight(4, '0');
                    token_B3.DfName = TlvFiller.TryGetTag(f55, "01", "84").Trim().PadRight(32, '0');

                    isTokenChange = true;
                }

                // TERM-SER-NUM
                if ("0100".Equals(origin.Mti()) || "0101".Equals(origin.Mti()) || "0400".Equals(origin.Mti())
                    || "0200".Equals(origin.Mti())
                    || "0420".Equals(origin.Mti()))
                {
                    if (!ObjectUtils.IsNull(origin.RawVal(FIELD.F_133)))
                    {
                        //token_B3.TermSerNum = ((byte[])origin.RawVal(FIELD.F_133)).GetHexString()
                        token_B3.TermSerNum = origin.RawVal(FIELD.F_133).ToString()
                            .Trim()
                            .PadRight(8, '0');
                        isTokenChange = true;
                    }
                }

                // TERM-CAP
                if ("0100".Equals(origin.Mti()) || "0200".Equals(origin.Mti()) || "0101".Equals(origin.Mti()))
                {
                    if (!ObjectUtils.IsNull(origin.RawVal(FIELD.F_130)))
                    {
                        token_B3.TermCap = ((byte[])origin.RawVal(FIELD.F_130)).GetHexString().Trim().PadRight(6, '0');
                        isTokenChange = true;
                    }
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
                WriteLogException(typeof(InField_63_B3), string.Format("Error occur(InField_63_B3): {0}", e));
                return inputString;
            }
        }
    }
}
