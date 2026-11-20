// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InField_63_U7.cs" company="Cybersoft">
//   Author: Kaven
// </copyright>
// <summary>
//   Defines the InField_63_U7 type.
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

    internal class InField_63_U7 : ConvertUnitBase
    {
        protected internal static string ToPosAdditionalDataTokenU7(VISA origin, MCOLA target, string inputString)
        {
            try
            {
                var token = new TokenGroup();
                token.Decode(inputString);
                token.Tokens["U7"] = new TokenU7();
                var token_U7 = (TokenU7)token.Tokens["U7"];
                token_U7.DataDecode(string.Empty.PadLeft(145, ' '));
                var isTokenChange = false;

                // Wallet Provider Risk Assessment
                var f125 = TlvFiller.TryDecodeTlv(FIELD.F_125, origin);
                //Add C.F
                if (f125 != null)
                {
                    var f125d02t03 = TlvFiller.TryGetEBCDICTag(f125, "02", "03");

                    switch (f125d02t03)
                    {
                        case "0":
                            token_U7.WalletProviderRiskAssessment = "G";
                            isTokenChange = true;
                            break;
                        case "1":
                            token_U7.WalletProviderRiskAssessment = "Y";
                            isTokenChange = true;
                            break;
                        case "2":
                            token_U7.WalletProviderRiskAssessment = "R";
                            isTokenChange = true;
                            break;
                    }
                }

                // Wallet Provider ID
                if ("0100".Equals(origin.Mti()) || "0200".Equals(origin.Mti()) || "0101".Equals(origin.Mti()))
                {
                    var f123 = TlvFiller.TryDecodeTlv(FIELD.F_123, origin);
                    //Add C.F
                    if (f123 != null)
                    {
                        var f123d68t03 = TlvFiller.TryGetEBCDICTag(f123, "68", "03");

                        token_U7.WalletProviderId = f123d68t03.Trim().PadRight(11, ' ');
                        isTokenChange = true;
                    }
                }

                // chip transaction indicator
                var f60 = origin.RawVal(FIELD.F_60);
                if (!ObjectUtils.IsNull(f60) && f60.ToString().Length >= 7)
                {
                    token_U7.ChipTransactionIndicator = f60.ToString()[6].ToString();
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
                WriteLogException(typeof(InField_63_U7), string.Format("Error occur(InField_63_U7): {0}", e));
                return inputString;
            }
        }
    }
}
