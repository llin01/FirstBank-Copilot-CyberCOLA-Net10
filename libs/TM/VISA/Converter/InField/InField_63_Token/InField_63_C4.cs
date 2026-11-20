// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InField_63_C4.cs" company="Cybersoft">
//   Author: Kaven
// </copyright>
// <summary>
//   Defines the InField_63_C4 type.
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

    internal class InField_63_C4 : ConvertUnitBase
    {
        protected internal static string ToPosAdditionalDataTokenC4(VISA origin, MCOLA target, string inputString)
        {
            try
            {
                var token = new TokenGroup();
                token.Decode(inputString);
                token.Tokens["C4"] = new TokenC4();
                var token_C4 = (TokenC4)token.Tokens["C4"];
                token_C4.DataDecode(string.Empty.PadLeft(12, ' '));

                var f22 = origin.RawVal(FIELD.F_22);
                var f25 = origin.RawVal(FIELD.F_25);
                var f60 = origin.RawVal(FIELD.F_60);

                // Term-Attend-Ind
                if (!ObjectUtils.IsNull(f60) && f60.ToString().Length >= 1)
                {
                    switch (f60.ToString()[0].ToString())
                    {
                        case "0":
                            token_C4.TermAttendInd = "2";
                            break;

                        case "1":
                        case "2":
                        case "3":
                            token_C4.TermAttendInd = "1";
                            break;
                    }
                }

                // Term-Loc-Ind
                token_C4.TermLocInd = "0";

                // C/H-Present-Ind
                token_C4.ChPresentInd = "0";
                if (!ObjectUtils.IsNull(f25))
                {
                    switch (f25.ToString())
                    {
                        case "01":
                            token_C4.ChPresentInd = "1";
                            break;

                        case "59":
                            token_C4.ChPresentInd = "5";
                            break;
                    }
                }

                // CRD-Present-Ind
                if (!ObjectUtils.IsNull(f25))
                {
                    switch (f25.ToString())
                    {
                        case "00":
                        case "02":
                        case "05":
                        case "71":
                            token_C4.CrdPresentInd = "1";
                            break;
                    }
                }

                // CRD-CAPTR-IND
                token_C4.CrdCaptrInd = "1";
                if (!ObjectUtils.IsNull(f60) && f60.ToString().Length >= 2)
                {
                    switch (f60.ToString()[1].ToString())
                    {
                        case "0":
                        case "1":
                            token_C4.CrdCaptrInd = "0";
                            break;
                    }
                }

                // TXN-STAT-IND
                token_C4.TxnStatInd = "0";
                if ("51".Equals(f25))
                {
                    token_C4.TxnStatInd = "8";
                }

                if (!ObjectUtils.IsNull(f60) && f60.ToString().Length >= 7)
                {
                    if ("4".Equals(f60.ToString()[6].ToString()))
                    {
                        token_C4.TxnStatInd = "9";
                    }
                }

                // TXN-SEC-IND
                token_C4.TxnSecInd = "0";
                if ("03".Equals(f25))
                {
                    token_C4.TxnSecInd = "1";
                }

                // C/H-ACTVT-Ind
                if ("5542".Equals(origin.RawVal(FIELD.F_18)) && f22.ToString().Length >= 2)
                {
                    switch (f22.ToString().Substring(0, 2))
                    {
                        case "05":
                        case "07":
                        case "90":
                        case "91":
                            token_C4.ChActvtInd = "2";
                            break;
                    }
                }

                if (!ObjectUtils.IsNull(f22) && f22.ToString().Length >= 2)
                {
                    if ("07".Equals(f22.ToString().Substring(0, 2)))
                    {
                        token_C4.ChActvtInd = "7";
                    }
                }

                if ("59".Equals(f25))
                {
                    token_C4.ChActvtInd = "6";
                }

                if (!ObjectUtils.IsNull(f60) && f60.ToString().Length >= 1)
                {
                    switch (f60.ToString()[0].ToString())
                    {
                        case "1":
                            token_C4.ChActvtInd = "3";
                            break;

                        case "2":
                            token_C4.ChActvtInd = "1";
                            break;
                    }
                }

                // Term-Input-CAP-Ind
                if (!ObjectUtils.IsNull(f22) && f22.ToString().Length >= 2)
                {
                    switch (f22.ToString().Substring(0, 2))
                    {
                        case "01":
                            token_C4.TermInputCapInd = "6";
                            break;

                        case "91":
                            token_C4.TermInputCapInd = "4";
                            break;

                        case "07":
                            token_C4.TermInputCapInd = "3";
                            break;

                        case "05":
                            token_C4.TermInputCapInd = "9";
                            break;
                    }
                }

                if (!ObjectUtils.IsNull(f60) && f60.ToString().Length >= 2)
                {
                    switch (f60.ToString()[1].ToString())
                    {
                        case "0":
                        case "1":
                            token_C4.TermInputCapInd = f60.ToString()[1].ToString();
                            break;

                        case "2":
                            token_C4.TermInputCapInd = "2";

                            if (!ObjectUtils.IsNull(f22) && f22.ToString().Length >= 3)
                            {
                                if ("1".Equals(f22.ToString().Substring(2, 1)))
                                {
                                    token_C4.TermInputCapInd = "7";
                                }
                            }

                            break;
                    }
                }

                var outputString = token.Encode();
                return outputString;
            }
            catch (Exception e)
            {
                WriteLogException(typeof(InField_63_C4), string.Format("Error occur(InField_63_C4): {0}", e));
                return inputString;
            }
        }
    }
}
