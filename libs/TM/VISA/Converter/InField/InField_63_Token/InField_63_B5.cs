// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InField_63_B5.cs" company="Cybersoft">
//   Author: Kaven
// </copyright>
// <summary>
//   Defines the InField_63_B5 type.
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
    using Com.Cybersoft4u.TM.ISO8583.TLV;
    using Com.Cybersoft4u.TM.VISA.SubFieldCodec.F139;

    #endregion

    internal class InField_63_B5 : ConvertUnitBase
    {
        protected internal static string ToPosAdditionalDataTokenB5(VISA origin, MCOLA target, string inputString)
        {
            try
            {
                var token = new TokenGroup();
                token.Decode(inputString);
                token.Tokens["B5"] = new TokenB5();
                var token_B5 = (TokenB5)token.Tokens["B5"];
                token_B5.DataDecode(string.Empty.PadLeft(42, ' '));
                var isTokenChange = false;

                token_B5.IssAuthDataLgth = "0010";
                token_B5.Info = string.Empty.PadLeft(12, '0');
                token_B5.UserFld1 = "N    3";

                // Tag 91 = ARQC + ISS RESP CDE
                var f139 = origin.RawVal(FIELD.F_139);
                var f55 = origin.RawVal(FIELD.F_55);
                if (!ObjectUtils.IsNull(f139))
                {
                    token_B5.Arpc = new F_139VISAFieldFormat1().Decode((byte[])f139).ToString().Substring(0, 16);
                    token_B5.IssRespCde = new F_139VISAFieldFormat1().Decode((byte[])f139).ToString().Substring(17, 4);
                    isTokenChange = true;
                }
                else if (!ObjectUtils.IsNull(f55))
                {
                    try
                    {
                        var f55D01T91 = new TLV().Decode((byte[])f55).Datasets["01"].Tags["91"];
                        token_B5.Arpc = f55D01T91.Value.GetHexString().Substring(0, 16);
                        token_B5.IssRespCde = DefaultPacker.EBCDIC.GetString(f55D01T91.Value).Substring(17, 4);
                        isTokenChange = true;
                    }
                    catch (Exception)
                    {
                        WriteLogCodecFail(typeof(InField_63_B5), string.Format("InField_63_B5(): Fail to decode VISA DE 55 to TLV format."));
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
                WriteLogException(typeof(InField_63_B5), string.Format("Error occur(InField_63_B5): {0}", e));
                return inputString;
            }
        }
    }
}
