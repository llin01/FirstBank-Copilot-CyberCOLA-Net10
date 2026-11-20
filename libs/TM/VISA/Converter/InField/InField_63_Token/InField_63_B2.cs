// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InField_63_B2.cs" company="Cybersoft">
//   Author: Kaven
// </copyright>
// <summary>
//   Defines the InField_63_B2 type.
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

    internal class InField_63_B2 : ConvertUnitBase
    {
        protected internal static string ToPosAdditionalDataTokenB2(VISA origin, MCOLA target, string inputString)
        {
            try
            {
                var token = new TokenGroup();
                token.Decode(inputString);
                token.Tokens["B2"] = new TokenB2();
                var token_B2 = (TokenB2)token.Tokens["B2"];
                token_B2.DataDecode(string.Empty.PadLeft(158, ' '));
                var f55 = TlvFiller.TryDecodeTlv(FIELD.F_55, origin);
                var isTokenChange = false;

                if (!ObjectUtils.IsNull(f55))
                {
                    token_B2.CryptoInfData = TlvFiller.TryGetTag(f55, "01", "9F27").Trim().PadRight(2, '0');
                    token_B2.Tvr = TlvFiller.TryGetTag(f55, "01", "95").Trim().PadRight(10, '0');
                    token_B2.Arqc = TlvFiller.TryGetTag(f55, "01", "9F26").Trim().PadRight(16, '0');
                    token_B2.AmtAuth = TlvFiller.TryGetTag(f55, "01", "9F02").Trim().PadRight(12, '0');
                    token_B2.AmtOther = TlvFiller.TryGetTag(f55, "01", "9F03").Trim().PadRight(12, '0');
                    token_B2.Aip = TlvFiller.TryGetTag(f55, "01", "82").Trim().PadRight(4, '0');
                    token_B2.Atc = TlvFiller.TryGetTag(f55, "01", "9F36").Trim().PadRight(4, '0');

                    var TermCntryCdeWk = TlvFiller.TryGetTag(f55, "01", "9F1A");

                    if (TermCntryCdeWk.Length > 3)
                    {
                        token_B2.TermCntryCde = TermCntryCdeWk
                            .Substring(1, 3)
                            .Trim()
                            .PadRight(3, '0');
                    }
                    else
                    {
                        token_B2.TermCntryCde = string.Empty.PadRight(3, '0');
                    }

                    var TermCrncyCde = TlvFiller.TryGetTag(f55, "01", "5F2A");

                    if (TermCrncyCde.Length > 3)
                    {
                        token_B2.TermCrncyCde = TermCrncyCde
                        .Substring(1, 3)
                        .Trim()
                        .PadRight(3, '0');
                    }
                    else
                    {
                        token_B2.TermCrncyCde = string.Empty.PadRight(3, '0');
                    }

                    token_B2.TranDat = TlvFiller.TryGetTag(f55, "01", "9A").Trim().PadRight(6, '0');
                    token_B2.TranType = TlvFiller.TryGetTag(f55, "01", "9C").Trim().PadRight(2, '0');
                    token_B2.UnpredictNum = TlvFiller.TryGetTag(f55, "01", "9F37").Trim().PadRight(8, '0');
                    //token_B2.IssApplDataLgth = TlvFiller.TryGetTag(f55, "01", "9F10").Length.ToString().PadLeft(4, '0');
                    token_B2.IssApplDataLgth = ((TlvFiller.TryGetTag(f55, "01", "9F10").Length) / 2).ToString().PadLeft(4, '0');
                    token_B2.IssApplData = TlvFiller.TryGetTag(f55, "01", "9F10").Trim().PadRight(64, '0');

                    isTokenChange = true;
                }

                if ("0100".Equals(origin.Mti()) || "0101".Equals(origin.Mti()) || "0400".Equals(origin.Mti())
                    || "0200".Equals(origin.Mti())
                    || "0420".Equals(origin.Mti()))
                {
                    // TVR 
                    if (!ObjectUtils.IsNull(origin.RawVal(FIELD.F_131)))
                    {
                        token_B2.Tvr = ((byte[])origin.RawVal(FIELD.F_131)).GetHexString().Trim().PadRight(10, '0');
                        isTokenChange = true;
                    }

                    // ATC 
                    if (!ObjectUtils.IsNull(origin.RawVal(FIELD.F_137)))
                    {
                        token_B2.Atc = ((byte[])origin.RawVal(FIELD.F_137)).GetHexString().Trim().PadRight(4, '0');
                        isTokenChange = true;
                    }

                    var f134 = origin.RawVal(FIELD.F_134);
                    if (!ObjectUtils.IsNull(f134))
                    {
                        // ISS-APPL-DATA-LGTH 
                        token_B2.IssApplDataLgth = "64".PadRight(4, '0');

                        // ISS-APPL-DATA
                        var f134Length = f134.ToString().Length;
                        var Dki = (f134Length >= 2)
                                      ? ((byte[])f134).GetHexString().Substring(0, 2).Trim().PadRight(2, '0')
                                      : string.Empty.PadRight(2, '0');
                        var Cvn = (f134Length >= 4)
                                      ? ((byte[])f134).GetHexString().Substring(2, 2).Trim().PadRight(2, '0')
                                      : string.Empty.PadRight(2, '0');
                        var Cvr = (f134Length >= 12)
                                      ? ((byte[])f134).GetHexString().Substring(4, 8).Trim().PadRight(8, '0')
                                      : string.Empty.PadRight(8, '0');
                        var Info = (f134Length >= 13)
                                       ? ((byte[])f134).GetHexString()
                                             .Substring(12, f134Length - 12)
                                             .Trim()
                                             .PadRight(50, '0')
                                       : string.Empty.PadRight(50, '0');
                        token_B2.IssApplData = "12" + Dki + Cvn + Cvr + Info;
                        isTokenChange = true;
                    }
                }

                if ("0100".Equals(origin.Mti()) || "0200".Equals(origin.Mti()) || "0101".Equals(origin.Mti()))
                {
                    // ARQC 
                    if (!ObjectUtils.IsNull(origin.RawVal(FIELD.F_136)))
                    {
                        token_B2.Arqc = ((byte[])origin.RawVal(FIELD.F_136)).GetHexString().Trim().PadRight(16, '0');
                        isTokenChange = true;
                    }

                    // AMT-OTHER 
                    if (!ObjectUtils.IsNull(origin.RawVal(FIELD.F_149)))
                    {
                        token_B2.AmtOther = ((byte[])origin.RawVal(FIELD.F_149)).GetHexString().Trim().PadRight(12, '0');
                        isTokenChange = true;
                    }

                    // AIP
                    if (!ObjectUtils.IsNull(origin.RawVal(FIELD.F_138)))
                    {
                        token_B2.Aip = ((byte[])origin.RawVal(FIELD.F_138)).GetHexString().Trim().PadRight(4, '0');
                        isTokenChange = true;
                    }

                    // TERM-CNTRY-CDE
                    if (!ObjectUtils.IsNull(origin.RawVal(FIELD.F_145)))
                    {
                        token_B2.TermCntryCde = origin.RawVal(FIELD.F_145)
                            .ToString()
                            .Substring(1, 3)
                            .Trim()
                            .PadRight(3, '0');
                        isTokenChange = true;
                    }

                    // TERM-CRNCY-CDE 
                    if (!ObjectUtils.IsNull(origin.RawVal(FIELD.F_148)))
                    {
                        token_B2.TermCrncyCde = origin.RawVal(FIELD.F_148)
                            .ToString()
                            .Substring(1, 3)
                            .Trim()
                            .PadRight(3, '0');
                        isTokenChange = true;
                    }

                    // TRAN-TYPE
                    if (!ObjectUtils.IsNull(origin.RawVal(FIELD.F_144)))
                    {
                        token_B2.TranType = origin.RawVal(FIELD.F_144).ToString().Trim().PadRight(2, '0');
                        isTokenChange = true;
                    }

                    // UNPREDICT-NUM 
                    if (!ObjectUtils.IsNull(origin.RawVal(FIELD.F_132)))
                    {
                        token_B2.UnpredictNum = ((byte[])origin.RawVal(FIELD.F_132)).GetHexString()
                            .Trim()
                            .PadRight(8, '0');
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
                WriteLogException(typeof(InField_63_B2), string.Format("Error occur(InField_63_B2): {0}", e));
                return inputString;
            }
        }
    }
}
