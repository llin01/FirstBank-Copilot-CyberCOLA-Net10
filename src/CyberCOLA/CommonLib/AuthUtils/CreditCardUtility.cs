using Com.Cybersoft4u.TM.FISC;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using NLog;
using System;
using System.Text.RegularExpressions;

namespace Com.Cybersoft4u.CommonLib.AuthUtils
{
    public static class CreditCardUtility
    {
        private const CreditCardType unknown = CreditCardType.Unknown;
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog
        /// <summary>
        /// Finds the type of credit card based on the card number using regex pattern matching.
        /// </summary>
        /// <param name="cardNumber">The card number to identify.</param>
        /// <returns>The identified credit card type (Visa, MasterCard, AmericanExpress, Discover, Diners, JCB, CUP, or Unknown).</returns>
        public static CreditCardType FindCardType(string cardNumber)
        {
            try
            {
                //https://www.regular-expressions.info/creditcard.html
                if (string.IsNullOrEmpty(cardNumber))
                {
                    Log.Warn($"FindCardType Invalid Input Data(Null Or Empty)");
                    return CreditCardType.Unknown;
                }

                if (Regex.Match(cardNumber, @"^4[0-9]{12}(?:[0-9]{3})?$").Success)
                {
                    return CreditCardType.Visa;
                }

                if (Regex.Match(cardNumber, @"^(?:5[1-5][0-9]{2}|222[1-9]|22[3-9][0-9]|2[3-6][0-9]{2}|27[01][0-9]|2720)[0-9]{12}$").Success)
                {
                    return CreditCardType.MasterCard;
                }

                if (Regex.Match(cardNumber, @"^3[47][0-9]{13}$").Success)
                {
                    return CreditCardType.AmericanExpress;
                }

                if (Regex.Match(cardNumber, @"^6(?:011|5[0-9]{2})[0-9]{12}$").Success)
                {
                    return CreditCardType.Discover;
                }

                if (Regex.Match(cardNumber, @"^(30[05][0-9]{3}|3095[0-9]{2}|36[0-9]{4}|3[89][0-9]{4})[0-9]{8}$").Success)
                {
                    return CreditCardType.Diners;
                }

                if (Regex.Match(cardNumber, @"^(?:2131|1800|35\d{3})\d{11}$").Success)
                {
                    return CreditCardType.JCB;
                }

                if (Regex.Match(cardNumber, @"^62[0-9]{14,17}$").Success)
                {
                    return CreditCardType.CUP;
                }
                var maskedPan = cardNumber.Length >= 4
                    ? $"****-****-****-{cardNumber.Substring(cardNumber.Length - 4)}"
                    : "****-****-****-****";
                Log.Error($"CreditCardType No Found, cardNumber[{maskedPan}], length[{cardNumber.Length}]");
                return CreditCardType.Unknown;
            }
            catch (Exception ex)
            {
                var maskedPan = cardNumber?.Length >= 4
                    ? $"****-****-****-{cardNumber.Substring(cardNumber.Length - 4)}"
                    : "****-****-****-****";
                Log.Fatal(ex, $"FindCardType Exception:{ex.Message}, cardNumber[{maskedPan}]");
                return CreditCardType.Unknown;
            }
        }

        /// <summary>
        /// Determines the NCCC transaction type based on the ISO 8583 message content.
        /// </summary>
        /// <param name="ncccRequest">The NCCC ISO 8583 request message.</param>
        /// <returns>The identified NCCC transaction type.</returns>
        public static NCCCTransactionType FindNCCCTransactionType(NcccIso8583 ncccRequest)
        {
            try
            {
                var authInfoData = new NcccAuthInfoStruct(ncccRequest);
                CreditCardType creditCardType;
                switch (ncccRequest.Mti())
                {
                    case "0100":
                    case "0200":
                        creditCardType = CreditCardUtility.FindCardType(authInfoData.isoP2CardNbr);
                        switch (creditCardType)
                        {
                            case CreditCardType.JCB:
                                if (authInfoData.isoP3ProcessCde.Substring(0, 2).Equals("92"))
                                {
                                    return NCCCTransactionType.NCCC0200TknActive;
                                }
                                else if (authInfoData.isoP3ProcessCde.Substring(0, 2).Equals("93"))
                                {
                                    var tlv = authInfoData.tokenQ8Data.GetSubTokenData();
                                    if (tlv.ContainsKey("27"))
                                    {
                                        if (tlv["27"].Equals("AC"))
                                            return NCCCTransactionType.NCCC0200TknOtp;
                                    }
                                    return NCCCTransactionType.NCCC0200TknEvent;
                                }
                                if (authInfoData.tokenS8Data != null && (authInfoData.tokenS8Data.AcctNumInd == "T") && (authInfoData.tokenS8Data.AcctNum.Length >= 16))
                                {
                                    if (!ColaCommUtility.IsJcbTokenPanCard(authInfoData.tokenS8Data.AcctNum.Trim()))
                                        return NCCCTransactionType.NCCC0200HCEAuth;
                                    else
                                        return NCCCTransactionType.NCCC0200TknAuth;
                                }

                                break;
                            case CreditCardType.MasterCard:
                                if (authInfoData.isoP3ProcessCde.Substring(0, 2).Equals("92"))
                                {
                                    return NCCCTransactionType.NCCC0200TknActive;
                                }
                                else if (authInfoData.isoP3ProcessCde.Substring(0, 2).Equals("93"))
                                {
                                    var tlv = authInfoData.tokenQ8Data.GetSubTokenData();
                                    if (tlv.ContainsKey("27"))
                                    {
                                        if (tlv["27"].Equals("AC"))
                                            return NCCCTransactionType.NCCC0200TknOtp;
                                    }
                                    return NCCCTransactionType.NCCC0200TknEvent;
                                }
                                if (authInfoData.tokenS8Data != null && (authInfoData.tokenS8Data.AcctNumInd == "T"))
                                    return NCCCTransactionType.NCCC0200HCEAuth;
                                break;
                            case CreditCardType.Visa:
                                if (authInfoData.tokenQ9Data != null)
                                {
                                    authInfoData.tokenQ9Data.SubDataDecode();
                                    if (authInfoData.isoP3ProcessCde.Substring(0, 2).Equals("81") &&
                                        authInfoData.tokenQ9Data.VisaMsgRsnCde.Equals("3700"))
                                    {
                                        return NCCCTransactionType.NCCC0200TknActive;
                                    }
                                }
                                if (authInfoData.tokenQ8Data != null)
                                {
                                    var tlv = authInfoData.tokenQ8Data.GetSubTokenData();
                                    if (tlv.ContainsKey("QA"))
                                    {
                                        if (tlv["QA"].Equals("02"))
                                            return NCCCTransactionType.NCCC0200TknOtp;
                                        return NCCCTransactionType.NCCC0200TknActive;
                                    }
                                }
                                if (authInfoData.tokenS8Data != null && (authInfoData.tokenS8Data.AcctNumInd == "T"))
                                    return NCCCTransactionType.NCCC0200HCEAuth;
                                break;
                        }
                        if (authInfoData.tokenS8Data != null && (authInfoData.tokenS8Data.AcctNumInd != "T") && (authInfoData.tokenS8Data.AcctNum.Length >= 16))
                            return NCCCTransactionType.NCCC0200TknAuth;
                        return NCCCTransactionType.NCCC0200Auth;
                    case "0120":
                    case "0121":
                    case "0220":
                    case "0221":
                        if (authInfoData.tokenS8Data != null && (authInfoData.tokenS8Data.AcctNumInd == "T") && (authInfoData.tokenS8Data.AcctNum.Length >= 16))
                        {
                            creditCardType = CreditCardUtility.FindCardType(authInfoData.isoP2CardNbr);
                            switch (creditCardType)
                            {
                                case CreditCardType.JCB:
                                    if (ColaCommUtility.IsJcbTokenPanCard(authInfoData.tokenS8Data.AcctNum.Trim()))
                                        return NCCCTransactionType.NCCC0220TknAuth;
                                    break;
                            }
                            return NCCCTransactionType.NCCC0220HCEAuth;
                        }
                        else if (authInfoData.tokenS8Data != null && (authInfoData.tokenS8Data.AcctNumInd != "T") && (authInfoData.tokenS8Data.AcctNum.Length >= 16))
                            return NCCCTransactionType.NCCC0220TknAuth;
                        return NCCCTransactionType.NCCC0220Auth;
                    case "0400":
                    case "0401":
                    case "0420":
                    case "0421":
                        creditCardType = CreditCardUtility.FindCardType(authInfoData.isoP2CardNbr);
                        switch (creditCardType)
                        {
                            case CreditCardType.JCB:
                                if (authInfoData.isoP3ProcessCde.Substring(0, 2).Equals("92"))
                                {
                                    if (authInfoData.tokenQ8Data != null)
                                    {
                                        var tlv = authInfoData.tokenQ8Data.GetSubTokenData();
                                        if (tlv.ContainsKey("27"))
                                        {
                                            if (tlv["27"].Equals("TE") || tlv["27"].Equals("TA"))
                                            {
                                                return NCCCTransactionType.NCCC0420TknActive;
                                            }

                                        }
                                    }
                                }
                                if (authInfoData.tokenS8Data != null && (authInfoData.tokenS8Data.AcctNumInd == "T") && (authInfoData.tokenS8Data.AcctNum.Length >= 16))
                                {
                                    if (!ColaCommUtility.IsJcbTokenPanCard(authInfoData.tokenS8Data.AcctNum.Trim()))
                                        return NCCCTransactionType.NCCC0420HCEAuth;
                                    else
                                        return NCCCTransactionType.NCCC0420TknAuth;
                                }
                                break;
                        }
                        if (authInfoData.tokenS8Data != null && (authInfoData.tokenS8Data.AcctNumInd == "T"))
                            return NCCCTransactionType.NCCC0420HCEAuth;
                        if (authInfoData.tokenS8Data != null && (authInfoData.tokenS8Data.AcctNumInd != "T") && (authInfoData.tokenS8Data.AcctNum.Length >= 16))
                            return NCCCTransactionType.NCCC0420TknAuth;
                        return NCCCTransactionType.NCCC0420Auth;
                    case "0620":
                        //if (authInfoData.isoP3ProcessCde.Substring(0, 2).Equals("93"))
                        //    return NCCCTransactionType.NCCC0620TknEvent;
                        creditCardType = CreditCardUtility.FindCardType(authInfoData.isoP2CardNbr);
                        switch (creditCardType)
                        {
                            case CreditCardType.Visa:
                                return NCCCTransactionType.NCCC0620TknEvent;
                            case CreditCardType.JCB:
                            case CreditCardType.MasterCard:
                                if (authInfoData.isoP3ProcessCde.Substring(0, 2).Equals("93"))
                                {
                                    if (authInfoData.tokenQ8Data != null)
                                    {
                                        var tlv = authInfoData.tokenQ8Data.GetSubTokenData();
                                        if (tlv.ContainsKey("27"))
                                        {
                                            if (tlv["27"].Equals("AC"))
                                            {
                                                return NCCCTransactionType.NCCC0200TknOtp;
                                            }
                                            else
                                                return NCCCTransactionType.NCCC0620TknEvent;
                                        }
                                    }
                                }
                                break;
                        }
                        break;
                }
                return NCCCTransactionType.Unknown;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"FindNCCCTransactionType Exception:{ex.Message}");
                return NCCCTransactionType.Unknown;
            }
        }

        /// <summary>
        /// Determines whether the FISC transaction is an HCE (Host Card Emulation) transaction.
        /// </summary>
        /// <param name="ficRequest">The FISC request message.</param>
        /// <returns>True if the transaction is an HCE transaction; otherwise, false.</returns>
        public static bool fiscHceTransaction(FISC ficRequest)
        {
            try
            {
                var authInfoData = new FiscAuthInfoStruct(ficRequest);
                if (authInfoData.isoP58T32Tpan == null) return false;
                return true;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"isFiscHceTransaction Exception:{ex.Message}");
                return false;
            }
        }
    }
    public enum CreditCardType
    {
        MasterCard, Visa, AmericanExpress, Discover, Diners, JCB, CUP, Unknown
    };

    public enum NCCCTransactionType
    {
        NCCC0200TknActive, NCCC0200TknOtp, NCCC0200TknEvent, NCCC0200TknAuth, NCCC0200Auth, NCCC0220TknAuth, NCCC0220Auth, NCCC0420TknActive, NCCC0420TknAuth, NCCC0420Auth, NCCC0620TknEvent, NCCC0200HCEAuth, NCCC0220HCEAuth, NCCC0420HCEAuth, Unknown
    };

    public enum TWINTransactionType
    {
        TWIN0100Auth, TWIN0200Auth, TWIN0220Auth, TWIN0420Auth, Unknown
    };
    public enum ETicketType
    {
        ICASH, IPASS, SVCS, Unknown
    };
}
