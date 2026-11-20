using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.FISC;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens;
using Newtonsoft.Json;
using NLog;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Com.Cybersoft4u.CommonLib.EAI
{
    /// <updatelist>
    /// </updatelist>
    public static class EAICommUtility
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog
        private static readonly SystemParameterInfo SystemParameterInfo = SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo();
        /// <summary>
        /// Check Bytes FCB9GQ Request Message
        /// </summary>
        /// <param name="reqJSONIncomingMessage"></param>
        /// <returns></returns>
        public static string CheckFCB9GQMessageFormat(byte[] reqJSONIncomingMessage)
        {
            try
            {
                var reqJSONObject = JsonConvert.DeserializeObject<FCB9GQ.FCB9GQ_REQ>(Encoding.Default.GetString(reqJSONIncomingMessage));
                if (reqJSONObject == null)
                {
                    Log.Info($"Receive Message Format Error, Sender: [{Encoding.Default.GetString(reqJSONIncomingMessage)}] at {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}. The server has send {reqJSONIncomingMessage.Length} bytes.");
                    return SystemConstantData.EAISystemCodeAndData.SystemError;
                }

                //check data is numeric
                var isNumeric = Regex.IsMatch(reqJSONObject.BODY.CardNo, @"^\d{16}$");
                if (!isNumeric)
                {
                    Log.Error($"CreditCardNo Invalid");
                    return SystemConstantData.EAISystemCodeAndData.FieldError;
                }
                //check data is numeric
                isNumeric = Regex.IsMatch(reqJSONObject.BODY.MerPAN, @"^\d{16}$");
                if (!isNumeric)
                {
                    Log.Error($"MerPAN:{reqJSONObject.BODY.MerPAN}");
                    return SystemConstantData.EAISystemCodeAndData.FieldError;
                }
                //check data length
                var isValid = Regex.IsMatch(reqJSONObject.BODY.TxnSeqNo, @"^[a-zA-Z0-9]{12}$");
                if (!isValid)
                {
                    Log.Error($"reqJSONObject.BODY.TxnSeqNo:{reqJSONObject.BODY.TxnSeqNo}");
                    return SystemConstantData.EAISystemCodeAndData.FieldError;
                }
                //check data is numeric
                isNumeric = Regex.IsMatch(reqJSONObject.BODY.TxnAmtNTD, @"^\d{13}$");
                if (!isNumeric)
                {
                    Log.Error($"TxnAmtNTD:{reqJSONObject.BODY.TxnAmtNTD}");
                    return SystemConstantData.EAISystemCodeAndData.FieldError;
                }
                //check data is numeric
                isNumeric = Regex.IsMatch(reqJSONObject.BODY.MCC, @"^\d{4}$");
                if (!isNumeric)
                {
                    Log.Error($"MCC:{reqJSONObject.BODY.MCC}");
                    return SystemConstantData.EAISystemCodeAndData.FieldError;
                }
                //check data is numeric
                isNumeric = Regex.IsMatch(reqJSONObject.BODY.CurCode, @"^\d{3}$");
                if (!isNumeric)
                {
                    Log.Error($"CurCode:{reqJSONObject.BODY.CurCode}");
                    return SystemConstantData.EAISystemCodeAndData.FieldError;
                }
                //check data is numeric
                isNumeric = Regex.IsMatch(reqJSONObject.BODY.OriTxnAmt, @"^\d{13}$");
                if (!isNumeric)
                {
                    Log.Error($"OriTxnAmt:{reqJSONObject.BODY.OriTxnAmt}");
                    return SystemConstantData.EAISystemCodeAndData.FieldError;
                }
                //check data length
                isValid = Regex.IsMatch(reqJSONObject.BODY.CountryCode, @"^[a-zA-Z]{2}$");
                if (!isValid)
                {
                    Log.Error($"CountryCode:{reqJSONObject.BODY.CountryCode}");
                    return SystemConstantData.EAISystemCodeAndData.FieldError;
                }
                //no check data length
                //if (reqJSONObject.BODY.MerName.Length != 25)
                //{
                //    Log.Error($"MerName:{reqJSONObject.BODY.MerName}");
                //    return SystemConstantData.EAISystemCodeAndData.FieldError;
                //}
                //
                //if (reqJSONObject.BODY.MerCity.Length != 15)
                //{
                //    Log.Error($"MerCity:{reqJSONObject.BODY.MerCity}");
                //    return SystemConstantData.EAISystemCodeAndData.FieldError;
                //}
                //check data is numeric
                isNumeric = Regex.IsMatch(reqJSONObject.BODY.QRLen, @"^\d{3}$");
                if (!isNumeric)
                {
                    Log.Error($"QRLen:{reqJSONObject.BODY.QRLen}");
                    return SystemConstantData.EAISystemCodeAndData.FieldError;
                }

                var qrDataLength = Convert.ToInt16(reqJSONObject.BODY.QRLen);
                if (qrDataLength > 0)
                {
                    if (reqJSONObject.BODY.QRMsg?.Length != qrDataLength)
                    {
                        Log.Error($"QRMsg:{reqJSONObject.BODY?.QRMsg}");
                        return SystemConstantData.EAISystemCodeAndData.FieldError;
                    }
                }

                return SystemConstantData.EAISystemCodeAndData.FormatCheckSuccess;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"CheckFCB9GQMessageFormat Exception:{ex.Message}");
                return SystemConstantData.EAISystemCodeAndData.FieldError;
            }
        }
        /// <summary>
        /// Check FCB9GQ Request Message
        /// </summary>
        /// <param name="reqJSONObject"></param>
        /// <returns></returns>
        public static string CheckFCB9GQMessageFormat(FCB9GQ.FCB9GQ_REQ reqJSONObject)
        {
            try
            {
                //check data is numeric
                var isNumeric = Regex.IsMatch(reqJSONObject.BODY.CardNo, @"^\d{16}$");
                if (!isNumeric)
                {
                    Log.Error($"CreditCardNo Invalid");
                    return SystemConstantData.EAISystemCodeAndData.FieldError;
                }
                //check data is numeric
                isNumeric = Regex.IsMatch(reqJSONObject.BODY.MerPAN, @"^\d{16}$");
                if (!isNumeric)
                {
                    Log.Error($"MerPAN:{reqJSONObject.BODY.MerPAN}");
                    return SystemConstantData.EAISystemCodeAndData.FieldError;
                }
                //check data length
                var isValid = Regex.IsMatch(reqJSONObject.BODY.TxnSeqNo, @"^[a-zA-Z0-9]{12}$");
                if (!isValid)
                {
                    Log.Error($"reqJSONObject.BODY.TxnSeqNo:{reqJSONObject.BODY.TxnSeqNo}");
                    return SystemConstantData.EAISystemCodeAndData.FieldError;
                }
                //check data is numeric
                isNumeric = Regex.IsMatch(reqJSONObject.BODY.TxnAmtNTD, @"^\d{13}$");
                if (!isNumeric)
                {
                    Log.Error($"MerPAN:{reqJSONObject.BODY.TxnAmtNTD}");
                    return SystemConstantData.EAISystemCodeAndData.FieldError;
                }
                //check data is numeric
                isNumeric = Regex.IsMatch(reqJSONObject.BODY.MCC, @"^\d{4}$");
                if (!isNumeric)
                {
                    Log.Error($"MCC:{reqJSONObject.BODY.MCC}");
                    return SystemConstantData.EAISystemCodeAndData.FieldError;
                }
                //check data is numeric
                isNumeric = Regex.IsMatch(reqJSONObject.BODY.CurCode, @"^\d{3}$");
                if (!isNumeric)
                {
                    Log.Error($"CurCode:{reqJSONObject.BODY.CurCode}");
                    return SystemConstantData.EAISystemCodeAndData.FieldError;
                }
                //check data is numeric
                isNumeric = Regex.IsMatch(reqJSONObject.BODY.OriTxnAmt, @"^\d{13}$");
                if (!isNumeric)
                {
                    Log.Error($"MerPAN:{reqJSONObject.BODY.OriTxnAmt}");
                    return SystemConstantData.EAISystemCodeAndData.FieldError;
                }
                //check data length
                isValid = Regex.IsMatch(reqJSONObject.BODY.CountryCode, @"^[a-zA-Z]{2}$");
                if (!isValid)
                {
                    Log.Error($"CountryCode:{reqJSONObject.BODY.CountryCode}");
                    return SystemConstantData.EAISystemCodeAndData.FieldError;
                }

                //no check data length
                //if (reqJSONObject.BODY.MerName.Length != 25)
                //{
                //    Log.Error($"MerName:{reqJSONObject.BODY.MerName}");
                //    return SystemConstantData.EAISystemCodeAndData.FieldError;
                //}
                //
                //if (reqJSONObject.BODY.MerCity.Length != 15)
                //{
                //    Log.Error($"MerCity:{reqJSONObject.BODY.MerCity}");
                //    return SystemConstantData.EAISystemCodeAndData.FieldError;
                //}
                //check data is numeric
                isNumeric = Regex.IsMatch(reqJSONObject.BODY.QRLen, @"^\d{3}$");
                if (!isNumeric)
                {
                    Log.Error($"QRLen:{reqJSONObject.BODY.QRLen}");
                    return SystemConstantData.EAISystemCodeAndData.FieldError;
                }

                var qrDataLength = Convert.ToInt16(reqJSONObject.BODY.QRLen);
                if (reqJSONObject.BODY.QRMsg.Length != qrDataLength)
                {
                    Log.Error($"QRMsg:{reqJSONObject.BODY.QRMsg}");
                    return SystemConstantData.EAISystemCodeAndData.FieldError;
                }
                return SystemConstantData.EAISystemCodeAndData.FormatCheckSuccess;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"CheckFCB9GQMessageFormat Exception:{ex.Message}");
                return SystemConstantData.EAISystemCodeAndData.FieldError;
            }
        }
        /// <summary>
        /// Create Response Error Message
        /// </summary>
        /// <returns></returns>
        public static byte[] CreateQRCodePaymentErrorMessage()
        {
            // set up FCB9GQ_RSP
            var rspJSON = new FCB9GQ.FCB9GQ_RSP();
            rspJSON.FMH = "";
            rspJSON.FN_CODE = "";
            rspJSON.RESPONSE_CODE = "99";
            rspJSON.RESPONSE_DESCRIPTION = "Message Format Error";
            rspJSON.TERMINAL_ID = "";
            rspJSON.USER_ID = "";
            var rspBody = new FCB9GQ.FCB9GQ_RSP_BODY();
            var nowDateTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            rspBody.TxnDate = nowDateTime.Substring(0, 8);
            rspBody.TxnTime = nowDateTime.Substring(8);
            rspBody.CreditCardNo = "";
            rspBody.MerPAN = "";
            rspBody.QRTxnSeqNo = "";
            rspBody.TxnAmtNTD = new string('0', 12);
            rspBody.AuthorizationCode = "";
            rspJSON.BODY = rspBody;
            var rspJSONString = JsonConvert.SerializeObject(rspJSON, Formatting.Indented);
            return Encoding.Default.GetBytes(rspJSONString);
        }

        /// <summary>
        /// Convert FCB9GQ Request Message to NcccIso8583 Format
        /// </summary>
        /// <param name="reqJSONObject"></param>
        /// <param name="tokenField"></param>
        public static NcccIso8583 ConvertFCB9GQMessage2NcccIso8583(FCB9GQ.FCB9GQ_REQ reqJSONObject, FIELD tokenField = FIELD.F_57)
        {
            try
            {
                var dummyExpiredDate = "0000";
                //check card type
                var creditCardType = CreditCardUtility.FindCardType(reqJSONObject.BODY.CardNo);
                var nccc = new NcccIso8583();
                nccc.Header().Decode($"ISO{02:00}{60:00}{000:000}{7:0}{0:0}");
                nccc.Mti("0200");
                switch (creditCardType)
                {
                    case CreditCardType.JCB:
                    case CreditCardType.Visa:
                        nccc.RawVal(FIELD.F_3, "260000");
                        break;
                    case CreditCardType.MasterCard:
                        nccc.RawVal(FIELD.F_3, "280000");
                        break;
                }
                nccc.RawVal(FIELD.F_4, reqJSONObject.BODY.TxnAmtNTD.Substring(1));
                string localDateTimeString = $"{DateTime.Now:yyyyMMddHHmmss}";
                var localTranDateTime = DateTime.ParseExact(localDateTimeString, "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
                var utcTranDateTimeString = localTranDateTime.ToUniversalTime().ToString("MMddHHmmss");
                nccc.Core().Field(FIELD.F_7, utcTranDateTimeString);
                nccc.Core().Field(FIELD.F_11, ColaCommUtility.GetSystemTraceNumber());
                nccc.Core().Field(FIELD.F_12, localDateTimeString.Substring(8));
                nccc.Core().Field(FIELD.F_13, localDateTimeString.Substring(4, 4));
                nccc.Core().Field(FIELD.F_15, ColaCommUtility.GetSettlementDate());
                nccc.Core().Field(FIELD.F_17, ColaCommUtility.GetCaptureDate());
                nccc.Core().Field(FIELD.F_18, reqJSONObject.BODY.MCC);
                switch (creditCardType)
                {
                    case CreditCardType.JCB:
                    case CreditCardType.Visa:
                        nccc.RawVal(FIELD.F_22, "010");
                        break;
                    case CreditCardType.MasterCard:
                        nccc.RawVal(FIELD.F_22, "810");
                        break;
                }
                nccc.Core().Field(FIELD.F_25, "05");
                switch (creditCardType)
                {
                    case CreditCardType.JCB:
                        nccc.Core().Field(FIELD.F_32, "130401");
                        break;
                    case CreditCardType.Visa:
                        nccc.Core().Field(FIELD.F_32, "415585");
                        break;
                    case CreditCardType.MasterCard:
                        nccc.Core().Field(FIELD.F_32, "003742");
                        break;
                }
                var merchantPanLength = Convert.ToInt32(reqJSONObject.BODY.MerPAN.Length);
                var merchantPanData = reqJSONObject.BODY.MerPAN.Substring(0, merchantPanLength);
                //Only MasterCard create CheckDigit
                string merchantPanDataCheckDigit = merchantPanData;
                if (creditCardType == CreditCardType.MasterCard)
                {
                    merchantPanDataCheckDigit = CreateCardNumberCheckDigit(merchantPanData);
                }
                nccc.Core().Field(FIELD.F_35, $"{merchantPanDataCheckDigit}={dummyExpiredDate}");
                nccc.Core().Field(FIELD.F_37, reqJSONObject.BODY.TxnSeqNo.Replace("QR", DateTime.Now.ToString("HHmmss").Substring(4, 2)));
                var isoP41 = string.Format(SystemConstantData.CommonData.NcccP41StringFormat,
                                           "APPRQ001");
                nccc.Core().Field(FIELD.F_41, isoP41);
                nccc.Core().Field(FIELD.F_42,
                                  string.IsNullOrEmpty(reqJSONObject.BODY.MerPAN.Trim())
                                  ? new string(' ', 15)
                                  : reqJSONObject.BODY.MerPAN.Substring(0, 15));
                //var isoP43 = $"{reqJSONObject.BODY.MerName}{reqJSONObject.BODY.MerCity.Substring(0,13)}   {reqJSONObject.BODY.CountryCode.Substring(0,2)}";
                var isoP43 = string.Format(SystemConstantData.CommonData.NcccP43StringFormat,
                                           string.Format("{0,-22}", reqJSONObject.BODY.MerName).Substring(0, 22),
                                           string.Format("{0,-13}", reqJSONObject.BODY.MerCity).Substring(0, 13),
                                           " ",
                                           reqJSONObject.BODY.CountryCode.Substring(0, 2));
                nccc.Core().Field(FIELD.F_43, isoP43);
                nccc.RawVal(FIELD.F_48, string.Format(SystemConstantData.CommonData.NcccP48POSStringFormat, merchantPanData));
                nccc.Core().Field(FIELD.F_49, reqJSONObject.BODY.CurCode);
                //add token data to sort list
                var newTokenGroup = new TokenGroup();
                //S8
                var tokenId = "S8";
                var tokenData = string.Format(SystemConstantData.CommonData.NcccTokenS8StringFormat,
                                              "T",
                                              reqJSONObject.BODY.CardNo,
                                              dummyExpiredDate,
                                              " ",
                                              " ",
                                              " ");
                newTokenGroup.Tokens[tokenId] = new TokenS8();
                var tokenS8 = (TokenS8)newTokenGroup.Tokens[tokenId];
                tokenS8.DataDecode(tokenData);
                //QR
                tokenId = "QR";
                StringBuilder sb = new StringBuilder();
                if (Convert.ToInt32(reqJSONObject.BODY.QRLen) > 2)
                {
                    var tlvData = Get62TLVData(Convert.ToInt32(reqJSONObject.BODY.QRLen) - 2, reqJSONObject.BODY.QRMsg?.Substring(2));
                    if (tlvData != null)
                    {
                        var indexCount = tlvData.Count > 2 ? 2 : tlvData.Count;
                        var fixBytesId = new byte[] { 0x31, 0x31, 0x31, 0x32 };
                        var fixByteSIndex = 0;
                        for (int index = 0; index < indexCount; index++)
                        {
                            var tagId = tlvData.ElementAt(index).Key;
                            var fixBytesIdInx = CommonUtils.GetSubBytes(fixBytesId, fixByteSIndex, 2);
                            fixByteSIndex += 2;
                            var tagIdLength = tlvData[tagId].Length;
                            sb.Append(Encoding.ASCII.GetString(fixBytesIdInx))
                              .Append($"{tagIdLength:000}")
                              .Append(tlvData[tagId]);
                        }
                    }
                    //var tagId = "01";
                    //if (tlvData.ContainsKey(tagId))
                    //{
                    //    var fixBytesId = new byte[] { 0x31, 0x31 };
                    //    var tagIdLength = tlvData[tagId].Length;
                    //    sb.Append(Encoding.ASCII.GetString(fixBytesId))
                    //      .Append($"{tagIdLength:000}")
                    //      .Append(tlvData[tagId]);
                    //}
                    //tagId = "02";
                    //if (tlvData.ContainsKey(tagId))
                    //{
                    //    var fixBytesId = new byte[] { 0x31, 0x32 };
                    //    var tagIdLength = tlvData[tagId].Length;
                    //    sb.Append(Encoding.ASCII.GetString(fixBytesId))
                    //      .Append($"{tagIdLength:000}")
                    //      .Append(tlvData[tagId]);
                    //}

                    if (sb.ToString().Length > 0)
                    {
                        newTokenGroup.Tokens[tokenId] = new TokenQR();
                        var tokenQR = (TokenQR)newTokenGroup.Tokens[tokenId];
                        //tokenQR.DataDecode(reqJSONObject.BODY.QRMsg);
                        //判斷若長度不為偶數時補入空白
                        if (sb.ToString().Length % 2 != 0)
                        {
                            sb.Append(" ");
                        }
                        tokenQR.DataDecode(sb.ToString());
                    }
                }
                nccc.Core().Field(tokenField, newTokenGroup.Encode());
                //fixed TokenGroup bug(20221230)
                nccc.Core().SetOthersValByKey(tokenField.ToString(), new TokenGroup(newTokenGroup.Encode()));
                nccc.Core().Field(FIELD.F_61, "BK48PRO200000000000");
                //var byteArray = nccc.Encode().Val();
                //return new NcccIso8583(byteArray);
                return nccc;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"ConvertEAIMessage2NcccIso8583 Exception:{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Convert FCB9GQ Request Message to NcccIso8583 Format by Return Code
        /// </summary>
        /// <param name="reqJSONObject"></param>
        /// <param name="processCode"></param>
        public static byte[] ConvertFCB9GQIncoming2Outgoing(FCB9GQ.FCB9GQ_REQ reqJSONObject, string processCode)
        {
            try
            {
                var rspJSONObject = new FCB9GQ.FCB9GQ_RSP
                {
                    FMH = "",
                    FN_CODE = "",
                    RESPONSE_CODE = processCode,
                    RESPONSE_DESCRIPTION = "欄位資料錯誤",
                    TERMINAL_ID = reqJSONObject.TERMINAL_ID,
                    USER_ID = reqJSONObject.USER_ID
                };
                var rspBody = new FCB9GQ.FCB9GQ_RSP_BODY();
                var nowDateTime = $"{DateTime.Now:yyyyMMddHHmmss}";
                rspBody.TxnDate = nowDateTime.Substring(0, 8);
                rspBody.TxnTime = nowDateTime.Substring(8);
                rspBody.CreditCardNo = reqJSONObject.BODY.CardNo;
                rspBody.MerPAN = reqJSONObject.BODY.MerPAN;
                rspBody.QRTxnSeqNo = reqJSONObject.BODY.TxnSeqNo;
                rspBody.TxnAmtNTD = reqJSONObject.BODY.TxnAmtNTD;
                rspBody.AuthorizationCode = "";
                rspJSONObject.BODY = rspBody;
                var rspJSONString = JsonConvert.SerializeObject(rspJSONObject, Formatting.Indented);
                var returnBytes = Encoding.Default.GetBytes(rspJSONString);
                return returnBytes;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"ConvertFCB9GQIncoming2Outgoing Exception:{ex.Message}");
                return Array.Empty<byte>();
            }
        }

        /// <summary>
        /// Converts FCB9GQ incoming message to outgoing response message with authorization results.
        /// </summary>
        /// <param name="requestNcccMessage">The NCCC ISO 8583 request message.</param>
        /// <param name="reqJSONObject">The FCB9GQ request JSON object.</param>
        /// <param name="returnStrings">Array of return strings from authorization process.</param>
        /// <param name="processCode">The process code for the response.</param>
        /// <returns>Byte array containing the JSON response message.</returns>
        public static byte[] ConvertFCB9GQIncoming2Outgoing(NcccIso8583 requestNcccMessage, FCB9GQ.FCB9GQ_REQ reqJSONObject, string[] returnStrings, string processCode)
        {
            try
            {
                var rspJSONObject = new FCB9GQ.FCB9GQ_RSP();
                //Set Field Data
                rspJSONObject.FMH = "";
                rspJSONObject.FN_CODE = "";
                rspJSONObject.RESPONSE_CODE = returnStrings[1];
                rspJSONObject.RESPONSE_DESCRIPTION = "成功";
                rspJSONObject.TERMINAL_ID = reqJSONObject.TERMINAL_ID;
                rspJSONObject.USER_ID = reqJSONObject.USER_ID;
                var rspBody = new FCB9GQ.FCB9GQ_RSP_BODY();
                var localNowDateTimeString = $"{DateTime.Now:yyyyMMddHHmmss}";
                string localTranDateTimeString;
                //check change date
                var timeString = $"{(string)requestNcccMessage.Core().Field(FIELD.F_13)}{(string)requestNcccMessage.Core().Field(FIELD.F_12)}";
                var stringCompare = String.CompareOrdinal(timeString, localNowDateTimeString.Substring(4, 10));
                if (stringCompare > 0)
                {
                    //less+ date
                    var yyyyData = Convert.ToInt32(localNowDateTimeString.Substring(0, 4)) - 1;
                    localTranDateTimeString = $"{yyyyData:0000}{timeString}";
                }
                else
                {
                    var yyyyData = Convert.ToInt32(localNowDateTimeString.Substring(0, 4));
                    localTranDateTimeString = $"{yyyyData:0000}{timeString}";
                }
                rspBody.TxnDate = $"{Convert.ToInt16(localTranDateTimeString.Substring(0, 4)) - 1911:0000}{localTranDateTimeString.Substring(4, 4)}";
                rspBody.TxnTime = localTranDateTimeString.Substring(8);
                rspBody.CreditCardNo = reqJSONObject.BODY.CardNo;
                rspBody.MerPAN = reqJSONObject.BODY.MerPAN;
                rspBody.QRTxnSeqNo = reqJSONObject.BODY.TxnSeqNo;
                rspBody.TxnAmtNTD = reqJSONObject.BODY.TxnAmtNTD;
                rspBody.AuthorizationCode = returnStrings[0];
                rspJSONObject.BODY = rspBody;
                var rspJSONString = JsonConvert.SerializeObject(rspJSONObject, Formatting.Indented);
                var returnBytes = Encoding.Default.GetBytes(rspJSONString);
                return returnBytes;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"ConvertFCB9GQIncoming2Outgoing Exception:{ex.Message}");
                return Array.Empty<byte>();
            }
        }

        /// <summary>
        /// Creates a card number with check digit using the Luhn algorithm.
        /// </summary>
        /// <param name="cardNumber">The card number without check digit.</param>
        /// <returns>The complete card number with check digit, or empty string if invalid.</returns>
        public static string CreateCardNumberCheckDigit(string cardNumber)
        {
            try
            {
                var isNumeric = Regex.IsMatch(cardNumber, @"^\d+$");
                if (!isNumeric) return string.Empty;
                if (cardNumber.Length == 16) return cardNumber;
                int sumDigit = 0;
                int shiftNumber = 0;
                for (var index = cardNumber.Length - 1; index >= 0; index--)
                {
                    var digit = Convert.ToInt32(cardNumber.Substring(index, 1));
                    if (shiftNumber % 2 == 0)
                        digit = digit * 2;
                    if (digit > 9)
                        digit -= 9;
                    sumDigit += digit;
                    shiftNumber++;
                }
                var checkDigit = (10 - (sumDigit % 10));
                return $"{cardNumber}{Convert.ToString(checkDigit)}";
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"CreateCardNumberCheckDigit Exception:{ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Update Request NcccIso8583 Format by Expired Date
        /// </summary>
        /// <param name="requestNcccMessage"></param>
        /// <param name="reqJSONObject"></param>
        /// <param name="returnStrings"></param>
        public static void UpdateExpiredDate(ref NcccIso8583 requestNcccMessage, FCB9GQ.FCB9GQ_REQ reqJSONObject, string[] returnStrings)
        {
            try
            {
                var expiredDate = returnStrings[0];
                //check card type
                var creditCardType = CreditCardUtility.FindCardType(reqJSONObject.BODY.CardNo);
                var merchantPanLength = Convert.ToInt32(reqJSONObject.BODY.MerPAN.Length);
                var merchantPanData = reqJSONObject.BODY.MerPAN.Substring(0, merchantPanLength);
                //Only MasterCard create CheckDigit
                string merchantPanDataCheckDigit = merchantPanData;
                if (creditCardType == CreditCardType.MasterCard)
                {
                    merchantPanDataCheckDigit = CreateCardNumberCheckDigit(merchantPanData);
                }
                requestNcccMessage.RawVal(FIELD.F_35, $"{merchantPanDataCheckDigit}={expiredDate}");
                var tokenGroup = (TokenGroup)requestNcccMessage.Core().GetOthersValByKey(requestNcccMessage.GetTokenFieldId().ToString());
                var tokenId = "S8";
                var tokenData = string.Format(SystemConstantData.CommonData.NcccTokenS8StringFormat,
                                              "T",
                                              reqJSONObject.BODY.CardNo,
                                              expiredDate,
                                              " ",
                                              " ",
                                              " ");
                tokenGroup.Tokens[tokenId] = new TokenS8();
                var tokenS8 = (TokenS8)tokenGroup.Tokens[tokenId];
                tokenS8.DataDecode(tokenData);
                requestNcccMessage.Core().Field(requestNcccMessage.GetTokenFieldId(), tokenGroup.Encode());
                requestNcccMessage.Core().SetOthersValByKey(requestNcccMessage.GetTokenFieldId().ToString(), new TokenGroup(tokenGroup.Encode()));
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"CreateCardNumberCheckDigit Exception:{ex.Message}");
                return;
            }
        }

        /// <summary>
        /// Parses TLV (Tag-Length-Value) data from field 62 format string.
        /// </summary>
        /// <param name="dataStringLength">The expected length of the data string.</param>
        /// <param name="dataString">The TLV data string to parse.</param>
        /// <returns>Dictionary containing tag-value pairs, or null if parsing fails.</returns>
        public static Dictionary<string, string> Get62TLVData(int dataStringLength, string dataString)
        {
            if (string.IsNullOrEmpty(dataString))
                return null;
            if (dataStringLength != dataString.Length)
                return null;
            try
            {
                var tlvDictionary = new Dictionary<string, string>();
                int startPosition = 0;
                int totalLength = dataString.Length;
                while (startPosition < totalLength)
                {
                    var offset = 2;
                    if (startPosition + offset > totalLength) break;
                    var subtoken = dataString.Substring(startPosition, offset);
                    startPosition += offset;

                    offset = 2;
                    if (startPosition + offset > totalLength) break;
                    var subtokenLength = Convert.ToInt16(dataString.Substring(startPosition, offset));
                    startPosition += offset;

                    offset = subtokenLength;
                    if (startPosition + offset > totalLength) break;
                    var subtokenData = dataString.Substring(startPosition, offset);
                    startPosition += offset;
                    tlvDictionary[subtoken] = subtokenData;
                }
                return tlvDictionary;
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Check Bytes OASVRFY1 Request Message
        /// </summary>
        /// <param name="reqJSONIncomingMessage"></param>
        /// <returns></returns>
        public static string CheckOASVRFY1MessageFormat(byte[] reqJSONIncomingMessage)
        {
            try
            {
                var reqJSONObject = JsonConvert.DeserializeObject<OASVRFY1.OASVRFY1_REQ>(Encoding.Default.GetString(reqJSONIncomingMessage));
                if (reqJSONObject == null)
                {
                    Log.Info($"Receive Message Format Error, Sender: [{Encoding.Default.GetString(reqJSONIncomingMessage)}] at {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}. The server has send {reqJSONIncomingMessage.Length} bytes.");
                    return SystemConstantData.EAISystemCodeAndData.SystemError;
                }

                //check data is numeric
                var isValid = Regex.IsMatch(reqJSONObject.BODY.CARD_NBR, @"^\d{16}$");
                if (!isValid)
                {
                    Log.Error($"CardNbr:{reqJSONObject.BODY.CARD_NBR}");
                    return SystemConstantData.EAISystemCodeAndData.FieldError;
                }
                //check data is numeric
                isValid = Regex.IsMatch(reqJSONObject.BODY.EXP_DTE, @"^\d{4}$");
                if (!isValid)
                {
                    Log.Error($"ExpDte:{reqJSONObject.BODY.EXP_DTE}");
                    return SystemConstantData.EAISystemCodeAndData.FieldError;
                }
                //check data length
                isValid = Regex.IsMatch(reqJSONObject.BODY.CARD_CVV2, @"^\d{3}$");
                if (!isValid)
                {
                    Log.Error($"CardCVV2:{reqJSONObject.BODY.CARD_CVV2}");
                    return SystemConstantData.EAISystemCodeAndData.FieldError;
                }
                //check data is numeric
                isValid = Regex.IsMatch(reqJSONObject.BODY.ID, @"^[a-zA-Z]{1}\d{9}$");
                if (!isValid)
                {
                    Log.Error($"ID:{reqJSONObject.BODY.ID}");
                    return SystemConstantData.EAISystemCodeAndData.FieldError;
                }
                //check data is numeric
                isValid = Regex.IsMatch(reqJSONObject.BODY.MCC_CODE, @"^\d{4}$");
                if (!isValid)
                {
                    Log.Error($"MccCode:{reqJSONObject.BODY.MCC_CODE}");
                    return SystemConstantData.EAISystemCodeAndData.FieldError;
                }
                //check data is numeric
                isValid = Regex.IsMatch(reqJSONObject.BODY.CELL_PHONE, @"^\d{10}$");
                if (!isValid)
                {
                    Log.Error($"CellPhone:{reqJSONObject.BODY.CELL_PHONE}");
                    return SystemConstantData.EAISystemCodeAndData.FieldError;
                }
                //check data is numeric
                isValid = Regex.IsMatch(reqJSONObject.BODY.CUST_BIRTHDAY, @"^\d{8}$");
                if (!isValid)
                {
                    Log.Error($"CustBirthday:{reqJSONObject.BODY.CUST_BIRTHDAY}");
                    return SystemConstantData.EAISystemCodeAndData.FieldError;
                }
                return SystemConstantData.EAISystemCodeAndData.FormatCheckSuccess;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"CheckOASVRFY1MessageFormat Exception:{ex.Message}");
                return SystemConstantData.EAISystemCodeAndData.FieldError;
            }
        }

        /// <summary>
        /// Check OASVRFY1 Request Message
        /// </summary>
        /// <param name="reqJSONObject"></param>
        /// <returns></returns>
        public static string CheckOASVRFY1MessageFormat(OASVRFY1.OASVRFY1_REQ reqJSONObject)
        {
            try
            {

                //check data is numeric
                var isValid = Regex.IsMatch(reqJSONObject.BODY.CARD_NBR, @"^\d{16}$");
                if (!isValid)
                {
                    Log.Error($"CardNbr:{reqJSONObject.BODY.CARD_NBR}");
                    return SystemConstantData.EAISystemCodeAndData.FieldError;
                }
                //check data is numeric
                isValid = Regex.IsMatch(reqJSONObject.BODY.EXP_DTE, @"^\d{4}$");
                if (!isValid)
                {
                    Log.Error($"ExpDte:{reqJSONObject.BODY.EXP_DTE}");
                    return SystemConstantData.EAISystemCodeAndData.FieldError;
                }
                //check data length
                isValid = Regex.IsMatch(reqJSONObject.BODY.CARD_CVV2, @"^\d{3}$");
                if (!isValid)
                {
                    Log.Error($"CardCVV2:{reqJSONObject.BODY.CARD_CVV2}");
                    return SystemConstantData.EAISystemCodeAndData.FieldError;
                }
                //check data is numeric
                isValid = Regex.IsMatch(reqJSONObject.BODY.ID, @"^[a-zA-Z]{1}\d{9}$");
                if (!isValid)
                {
                    Log.Error($"ID:{reqJSONObject.BODY.ID}");
                    return SystemConstantData.EAISystemCodeAndData.FieldError;
                }
                //check data is numeric
                isValid = Regex.IsMatch(reqJSONObject.BODY.MCC_CODE, @"^\d{4}$");
                if (!isValid)
                {
                    Log.Error($"MccCode:{reqJSONObject.BODY.MCC_CODE}");
                    return SystemConstantData.EAISystemCodeAndData.FieldError;
                }
                //check data is numeric
                if (!string.IsNullOrEmpty(reqJSONObject.BODY.CELL_PHONE))
                {
                    isValid = Regex.IsMatch(reqJSONObject.BODY.CELL_PHONE, @"^\d{10}$");
                    if (!isValid)
                    {
                        Log.Error($"CellPhone:{reqJSONObject.BODY.CELL_PHONE}");
                        return SystemConstantData.EAISystemCodeAndData.FieldError;
                    }
                }
                //check data is numeric
                if (!string.IsNullOrEmpty(reqJSONObject.BODY.CUST_BIRTHDAY))
                {
                    Log.Info($"Check CustBirthday Data:{reqJSONObject.BODY.CUST_BIRTHDAY}");
                    var custBirthday = DateTime.ParseExact(reqJSONObject.BODY.CUST_BIRTHDAY, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd");
                    isValid = Regex.IsMatch(custBirthday, @"^\d{4}-?\d{2}-?\d{2}$");
                    if (!isValid)
                    {
                        Log.Error($"CustBirthday:{reqJSONObject.BODY.CUST_BIRTHDAY}");
                        return SystemConstantData.EAISystemCodeAndData.FieldError;
                    }
                }
                return SystemConstantData.EAISystemCodeAndData.FormatCheckSuccess;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"CheckOASVRFY1MessageFormat Exception:{ex.Message}");
                return SystemConstantData.EAISystemCodeAndData.FieldError;
            }
        }

        /// <summary>
        /// Create Response Error Message
        /// </summary>
        /// <returns></returns>
        public static byte[] CreateIDVerifyErrorMessage()
        {
            // set up OASVRFY1_RSP
            var rspJSON = new OASVRFY1.OASVRFY1_RSP();
            rspJSON.FMH = "";
            rspJSON.FN_CODE = "";
            rspJSON.RESPONSE_CODE = "99";
            rspJSON.RESPONSE_DESCRIPTION = "Message Format Error";
            rspJSON.TERMINAL_ID = "";
            rspJSON.USER_ID = "";
            var rspBody = new OASVRFY1.OASVRFY1_RSP_BODY();
            var nowDateTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            rspBody.CARD_NBR = "";
            rspBody.EXP_DTE = "";
            rspBody.CARD_CVV2 = "";
            rspBody.ID = "";
            rspBody.MCC_CODE = "";
            rspBody.CELL_PHONE = "";
            rspBody.CUST_BIRTHDAY = "";
            rspBody.MERCH_NO = "";
            rspBody.RTN_CODE = "";
            rspBody.AUTH_CODE = "";
            rspBody.ERRORS = "Message Format Error";
            rspJSON.BODY = rspBody;
            var rspJSONString = JsonConvert.SerializeObject(rspJSON, Formatting.Indented);
            return Encoding.Default.GetBytes(rspJSONString);
        }

        /// <summary>
        /// Convert OASVRFY1 Request Message to FISC Format
        /// </summary>
        /// <param name="reqJSONObject"></param>
        /// <param name="tokenField"></param>
        public static FISC ConvertOASVRFY1Message2FISC(OASVRFY1.OASVRFY1_REQ reqJSONObject)
        {
            try
            {
                //check card type
                var creditCardType = CreditCardUtility.FindCardType(reqJSONObject.BODY.CARD_NBR);
                var fisc = new FISC();
                fisc.Mti("0100");
                fisc.Core().Field(FIELD.F_2, reqJSONObject.BODY.CARD_NBR);
                fisc.Core().Field(FIELD.F_3, "000000");
                fisc.Core().Field(FIELD.F_4, String.Format("{0:D12}", 0));
                string localDateTimeString = $"{DateTime.Now:yyyyMMddHHmmss}";
                var localTranDateTime = DateTime.ParseExact(localDateTimeString, "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
                var utcTranDateTimeString = localTranDateTime.ToUniversalTime().ToString("MMddHHmmss");
                fisc.Core().Field(FIELD.F_6, String.Format("{0:D12}", 0));
                fisc.Core().Field(FIELD.F_7, utcTranDateTimeString);
                fisc.Core().Field(FIELD.F_11, ColaCommUtility.GetSystemTraceNumber());
                fisc.Core().Field(FIELD.F_12, localDateTimeString.Substring(8));
                fisc.Core().Field(FIELD.F_13, localDateTimeString.Substring(4, 4));
                fisc.Core().Field(FIELD.F_14, reqJSONObject.BODY.EXP_DTE);
                fisc.Core().Field(FIELD.F_18, reqJSONObject.BODY.MCC_CODE);
                fisc.Core().Field(FIELD.F_22, "012");
                string isoP43 = string.Empty;
                switch (creditCardType)
                {
                    case CreditCardType.Visa:
                        fisc.Core().Field(FIELD.F_32, "404777");
                        isoP43 = string.Format(SystemConstantData.CommonData.FISCP43StringFormat,
                                           string.Format("{0,-22}", "PAYWARE LNC"),
                                           " ",
                                           string.Format("{0,-13}", "KAOHSIUNG"),
                                           " ",
                                           string.Format("{0,-3}", " TW"));
                        fisc.Core().Field(FIELD.F_43, isoP43);
                        break;
                    case CreditCardType.MasterCard:
                        fisc.Core().Field(FIELD.F_32, "008278");
                        fisc.Core().Field(FIELD.F_43, "PAYWARE LNC KAOHSIUNG TWN");
                        isoP43 = string.Format(SystemConstantData.CommonData.FISCP43StringFormat,
                                           string.Format("{0,-22}", "PAYWARE LNC"),
                                           " ",
                                           string.Format("{0,-13}", "KAOHSIUNG"),
                                           " ",
                                           string.Format("{0,-3}", "TWN"));
                        fisc.Core().Field(FIELD.F_43, isoP43);
                        break;
                    case CreditCardType.JCB:
                        fisc.Core().Field(FIELD.F_32, "88523000");
                        isoP43 = string.Format(SystemConstantData.CommonData.FISCP43StringFormat,
                                           string.Format("{0,-22}", "PAYWARE LNC"),
                                           " ",
                                           string.Format("{0,-13}", "KAOHSIUNG"),
                                           " ",
                                           string.Format("{0,-3}", "TWN"));
                        fisc.Core().Field(FIELD.F_43, isoP43);
                        break;
                }
                fisc.Core().Field(FIELD.F_37, $"{localDateTimeString.Substring(2, 6)}{ColaCommUtility.GetSystemTraceNumber()}");
                fisc.Core().Field(FIELD.F_41, "21320001");
                //switch (reqJSONObject.BODY.MCC_CODE)
                //{
                //    case "7375":
                //        fisc.Core().Field(FIELD.F_42, "007292179909001");
                //        break;
                //    default:
                //        fisc.Core().Field(FIELD.F_42, "007292179901001");
                //        break;
                //}
                fisc.Core().Field(FIELD.F_42, reqJSONObject.BODY.MERCH_NO);
                //create tag48
                fisc.Core().Field(FIELD.F_48, $"R92{string.Format("{0:D2}", reqJSONObject.BODY.CARD_CVV2.Length)}{reqJSONObject.BODY.CARD_CVV2}");
                fisc.Core().Field(FIELD.F_49, "901");
                var tag67 = $"67{string.Format("{0:D2}", reqJSONObject.BODY.ID.Length)}{reqJSONObject.BODY.ID}";
                var tag68 = $"68{string.Format("{0:D2}", 2)}{"03"}";
                int tag87length = 0;
                StringBuilder tag87string = new StringBuilder();
                if (!string.IsNullOrEmpty(reqJSONObject.BODY.CUST_BIRTHDAY))
                {
                    var custBirthday = DateTime.ParseExact(reqJSONObject.BODY.CUST_BIRTHDAY, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToString("yyyy-MM-dd");
                    tag87length += 12;
                    tag87string.Append($"01{string.Format("{0:D2}", custBirthday.Replace("-", "").Length)}{custBirthday.Replace("-", "")}");
                }
                if (!string.IsNullOrEmpty(reqJSONObject.BODY.CELL_PHONE))
                {
                    tag87length += 14;
                    tag87string.Append($"02{string.Format("{0:D2}", reqJSONObject.BODY.CELL_PHONE.Length)}{reqJSONObject.BODY.CELL_PHONE}");
                }
                var tag87 = string.Empty;
                if (tag87length > 0)
                {
                    tag87 = $"87{string.Format("{0:D2}", tag87length.ToString())}{tag87string.ToString()}";
                }
                //var tag87 = $"872601{string.Format("{0:D2}", reqJSONObject.BODY.CUST_BIRTHDAY.Replace("-","").Length)}{reqJSONObject.BODY.CUST_BIRTHDAY.Replace("-", "")}02{string.Format("{0:D2}", reqJSONObject.BODY.CELL_PHONE.Length)}{reqJSONObject.BODY.CELL_PHONE}";
                fisc.Core().Field(FIELD.F_58, $"{tag67}{tag68}{tag87}");
                fisc.Core().Field(FIELD.F_61, "9999997999000158");
                return fisc;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"ConvertEAIMessage2FISC Exception:{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Convert OASVRFY1 Request Message to NcccIso8583 Format by Return Code
        /// </summary>
        /// <param name="reqJSONObject"></param>
        /// <param name="processCode"></param>
        public static byte[] ConvertOASVRFY1Incoming2Outgoing(OASVRFY1.OASVRFY1_REQ reqJSONObject, string processCode)
        {
            try
            {
                var rspJSONObject = new OASVRFY1.OASVRFY1_RSP
                {
                    FMH = "",
                    FN_CODE = "",
                    RESPONSE_CODE = processCode,
                    RESPONSE_DESCRIPTION = "欄位資料錯誤",
                    TERMINAL_ID = reqJSONObject.TERMINAL_ID,
                    USER_ID = reqJSONObject.USER_ID
                };
                var rspBody = new OASVRFY1.OASVRFY1_RSP_BODY();
                rspBody.CARD_NBR = reqJSONObject.BODY.CARD_NBR;
                rspBody.EXP_DTE = reqJSONObject.BODY.EXP_DTE;
                rspBody.CARD_CVV2 = reqJSONObject.BODY.CARD_CVV2;
                rspBody.ID = reqJSONObject.BODY.ID;
                rspBody.MCC_CODE = reqJSONObject.BODY.MCC_CODE;
                rspBody.CELL_PHONE = reqJSONObject.BODY.CELL_PHONE;
                rspBody.CUST_BIRTHDAY = reqJSONObject.BODY.CUST_BIRTHDAY;
                rspBody.MERCH_NO = reqJSONObject.BODY.MERCH_NO;
                rspBody.RTN_CODE = reqJSONObject.BODY.RTN_CODE;
                rspBody.AUTH_CODE = reqJSONObject.BODY.AUTH_CODE;
                rspBody.ERRORS = "欄位資料錯誤";
                rspJSONObject.BODY = rspBody;
                var rspJSONString = JsonConvert.SerializeObject(rspJSONObject, Formatting.Indented);
                var returnBytes = Encoding.Default.GetBytes(rspJSONString);
                return returnBytes;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"ConvertOASVRFY1Incoming2Outgoing Exception:{ex.Message}");
                return Array.Empty<byte>();
            }
        }

        /// <summary></summary>
        /// <param name="reqJSONObject"></param>
        /// <param name="returnStrings"></param>
        /// <param name="processCode"></param>
        public static byte[] ConvertOASVRFY1Incoming2Outgoing(FISC responseFiscMessage, OASVRFY1.OASVRFY1_REQ reqJSONObject, string processCode)
        {
            try
            {
                var rspJSONObject = new OASVRFY1.OASVRFY1_RSP();
                //Set Field Data
                rspJSONObject.FMH = "";
                rspJSONObject.FN_CODE = "";
                rspJSONObject.RESPONSE_CODE = processCode;
                rspJSONObject.RESPONSE_DESCRIPTION = "成功";
                rspJSONObject.TERMINAL_ID = reqJSONObject.TERMINAL_ID;
                rspJSONObject.USER_ID = reqJSONObject.USER_ID;
                var rspBody = new OASVRFY1.OASVRFY1_RSP_BODY();
                rspBody.CARD_NBR = reqJSONObject.BODY.CARD_NBR;
                rspBody.EXP_DTE = reqJSONObject.BODY.EXP_DTE;
                rspBody.CARD_CVV2 = reqJSONObject.BODY.CARD_CVV2;
                rspBody.ID = reqJSONObject.BODY.ID;
                rspBody.MCC_CODE = reqJSONObject.BODY.MCC_CODE;
                rspBody.CELL_PHONE = reqJSONObject.BODY.CELL_PHONE;
                rspBody.CUST_BIRTHDAY = reqJSONObject.BODY.CUST_BIRTHDAY;
                rspBody.MERCH_NO = reqJSONObject.BODY.MERCH_NO;
                rspBody.RTN_CODE = "";
                var fieldPos = FIELD.F_39;
                if (!ObjectUtils.IsNull(responseFiscMessage.RawVal(fieldPos)))
                {
                    rspBody.RTN_CODE = responseFiscMessage.RawVal(fieldPos).ToString();  //F39
                }
                rspBody.AUTH_CODE = "";
                fieldPos = FIELD.F_38;
                if (!ObjectUtils.IsNull(responseFiscMessage.RawVal(fieldPos)))
                {
                    rspBody.AUTH_CODE = responseFiscMessage.RawVal(fieldPos).ToString();  //F38
                }
                rspJSONObject.BODY = rspBody;
                var rspJSONString = JsonConvert.SerializeObject(rspJSONObject, Formatting.Indented);
                var returnBytes = Encoding.Default.GetBytes(rspJSONString);
                return returnBytes;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"ConvertOASVRFY1Incoming2Outgoing Exception:{ex.Message}");
                return Array.Empty<byte>();
            }
        }
    }
}
