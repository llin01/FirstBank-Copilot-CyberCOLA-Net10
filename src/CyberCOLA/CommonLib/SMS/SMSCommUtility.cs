using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Com.Cybersoft4u.CommonLib.SMS
{
    /// <summary>
    /// Provides utility methods for SMS message creation, formatting, conversion, and transmission.
    /// </summary>
    public static class SMSCommUtility
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog
        private static readonly SingletonOfSMSWebAPIInfo SingletonOfSMSWebAPIInfo = SingletonOfSMSWebAPIInfo.Instance;
        private const string DatabaseKeyName = "TRAN_LOG_KEY";
        private const string DatabaseMessageKeyName = "MQ_MESSAGE";
        private const string DatabaseRtnCodeName = "RTN_CODE";
        private const string XMLHeader = "<?xml version=\"1.0\" encoding=\"BIG5\" ?>";
        /// <summary>
        /// Creates an SMS outgoing message for token OTP (One-Time Password) transactions (message type 0200).
        /// </summary>
        /// <param name="returnStrings">Array of return strings containing SMS data including phone number, name, message body, and customer number.</param>
        /// <returns>StringContent object ready for HTTP transmission, or null if an error occurred.</returns>
        public static StringContent Create0200TknOtpSMSOutgoingMessage(string[] returnStrings)
        {
            try
            {
                var jsonOption = new JsonSerializerOptions()
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                var smsParameterModel = new SMSParameterModel();
                smsParameterModel.SystemId = SingletonOfSMSWebAPIInfo.GetSMSWebAPIInfo().DataInfo.SystemId;
                smsParameterModel.SourceId = SingletonOfSMSWebAPIInfo.GetSMSWebAPIInfo().DataInfo.SourceId;
                smsParameterModel.CellPhone = returnStrings[6];
                smsParameterModel.Name = returnStrings[5]; ;
                smsParameterModel.SmsProvider = SingletonOfSMSWebAPIInfo.GetSMSWebAPIInfo().SMSWebAPI.SmsProvider;
                smsParameterModel.MessageBody = returnStrings[4];
                smsParameterModel.CUST_NBR = returnStrings[7];
                var jsonContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(smsParameterModel, jsonOption), Encoding.UTF8, "application/json");
                return jsonContent;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Create0200TknOtpSMSutgoingMessage Exception:{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates an SMS outgoing message for token event transactions (message type 0200).
        /// </summary>
        /// <param name="returnStrings">Array of return strings containing SMS data including phone number, name, message body, and customer number.</param>
        /// <returns>StringContent object ready for HTTP transmission, or null if an error occurred.</returns>
        public static StringContent Create0200TknEventSMSOutgoingMessage(string[] returnStrings)
        {
            try
            {
                var jsonOption = new JsonSerializerOptions()
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                var smsParameterModel = new SMSParameterModel();
                smsParameterModel.SystemId = SingletonOfSMSWebAPIInfo.GetSMSWebAPIInfo().DataInfo.SystemId;
                smsParameterModel.SourceId = SingletonOfSMSWebAPIInfo.GetSMSWebAPIInfo().DataInfo.SourceId;
                smsParameterModel.CellPhone = returnStrings[5];
                smsParameterModel.Name = returnStrings[4];
                smsParameterModel.SmsProvider = SingletonOfSMSWebAPIInfo.GetSMSWebAPIInfo().SMSWebAPI.SmsProvider;
                smsParameterModel.MessageBody = returnStrings[3];
                smsParameterModel.CUST_NBR = returnStrings[6];
                var jsonContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(smsParameterModel, jsonOption), Encoding.UTF8, "application/json");
                return jsonContent;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Create0200TknEventSMSOutgoingMessage Exception:{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates an SMS outgoing message for token event transactions (message type 0620).
        /// </summary>
        /// <param name="returnStrings">Array of return strings containing SMS data including phone number, name, message body, and customer number.</param>
        /// <returns>StringContent object ready for HTTP transmission, or null if an error occurred.</returns>
        public static StringContent Create0620TknEventSMSOutgoingMessage(string[] returnStrings)
        {
            try
            {
                var jsonOption = new JsonSerializerOptions()
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                var smsParameterModel = new SMSParameterModel();
                smsParameterModel.SystemId = SingletonOfSMSWebAPIInfo.GetSMSWebAPIInfo().DataInfo.SystemId;
                smsParameterModel.SourceId = SingletonOfSMSWebAPIInfo.GetSMSWebAPIInfo().DataInfo.SourceId;
                smsParameterModel.CellPhone = returnStrings[4];
                smsParameterModel.Name = returnStrings[3]; ;
                smsParameterModel.SmsProvider = SingletonOfSMSWebAPIInfo.GetSMSWebAPIInfo().SMSWebAPI.SmsProvider;
                smsParameterModel.MessageBody = returnStrings[2];
                smsParameterModel.CUST_NBR = returnStrings[5];
                var jsonContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(smsParameterModel, jsonOption), Encoding.UTF8, "application/json");
                return jsonContent;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Create0620TknEventSMSOutgoingMessage Exception:{ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Converts XML string to formatted SMS message with proper field lengths and encoding (Big5).
        /// </summary>
        /// <param name="xmlString">The XML string containing SMS field definitions.</param>
        /// <param name="appendNewLine">If true, appends newline characters to the end of the message.</param>
        /// <returns>Formatted SMS message string, or null if an error occurred.</returns>
        public static string ConvertXML2SMSMessage(string xmlString, bool appendNewLine = true)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(XMLHeader).AppendLine().Append(xmlString);
                XElement xElement = XElement.Parse(sb.ToString());
                sb = new StringBuilder();
                var elems = xElement.Descendants();
                var big5encode = Encoding.GetEncoding(950);
                foreach (var item in elems)
                {
                    Log.Debug(item.Name);
                    var fieldLength = item.Attribute("FieldLength") != null ? Convert.ToInt16(item.Attribute("FieldLength")?.Value) : 0;
                    var fieldValue = item.Attribute("FieldValue")?.Value;
                    var fieldName = item.Attribute("FieldName") != null ? item.Attribute("FieldName")?.Value : "NoneName";
                    if (fieldLength == 0 || fieldValue == null)

                    {
                        Log.Error($"ConvertXML2SMSMessage Error, FieldName[{fieldName}]");
                        return null;
                    }
                    var value = item.Attribute("ChField")?.Value;
                    if (value != null && value.ToUpper().Equals("Y"))
                    {
                        //var valueLength = Encoding.Default.GetByteCount(fieldValue);
                        var valueLength = big5encode.GetByteCount(fieldValue);
                        if (valueLength > fieldLength)
                        {
                            //var dstBytes = Encoding.Default.GetBytes(new string(' ', fieldLength));
                            // var srcBytes = Encoding.Default.GetBytes(fieldValue);
                            var dstBytes = big5encode.GetBytes(new string(' ', fieldLength));
                            var srcBytes = big5encode.GetBytes(fieldValue);
                            Buffer.BlockCopy(srcBytes, 0, dstBytes, 0, fieldLength);
                            int byteCompare = dstBytes[fieldLength - 1].CompareTo(0x80);
                            if (byteCompare >= 0) dstBytes[fieldLength - 1] = 0x20;
                            //sb.Append(Encoding.Default.GetString(dstBytes));
                            sb.Append(big5encode.GetString(dstBytes));
                            // Log name and lengths only: field values may hold a card number (PCI DSS).
                            Log.Info($"Field[{fieldName}] truncated, Real Length[{valueLength}] Greater Than Define Length[{fieldLength}]");
                        }
                        else
                        {
                            var appendSpaces = fieldLength - valueLength;
                            sb.Append(item.Attribute("FieldValue")?.Value)
                              .Append(new string(' ', appendSpaces));
                        }
                    }
                    else
                    {
                        var valueLength = Encoding.Default.GetByteCount(fieldValue);
                        if (valueLength > fieldLength)
                        {
                            sb.Append(string.Format(fieldValue.Substring(0, fieldLength)));
                            // Log name and lengths only: field values may hold a card number (PCI DSS).
                            Log.Warn($"Field[{fieldName}] truncated, Real Length[{valueLength}] Greater Than Define Length[{fieldLength}]");
                        }
                        else
                        {
                            var stringFormat = $"{{0,-{fieldLength}}}";
                            sb.Append(string.Format(stringFormat, fieldValue));
                        }
                    }
                }

                if (appendNewLine)
                    return sb.Append(Encoding.ASCII.GetString(new byte[] { 0x0D, 0x1A })).ToString();
                return sb.ToString();
            }
            catch (System.Xml.XmlException ex1)
            {
                Log.Fatal(ex1, $"ConvertXML2SMSMessage Exception:{ex1.Message}");
                Log.Fatal(ex1, $"ConvertXML2SMSMessage Exception:{ex1.StackTrace}");
                return (null);
            }
            catch (Exception ex2)
            {
                Log.Fatal(ex2, $"ConvertXML2SMSMessage Exception:{ex2.Message}");
                return (null);
            }
        }

        /// <summary>
        /// Sends SMS messages to the SMS server and returns a list of transaction keys with their result codes.
        /// </summary>
        /// <param name="smsServerInfo">SMS server configuration information.</param>
        /// <param name="dataTable">DataTable containing SMS messages to send with transaction keys.</param>
        /// <returns>List of strings in the format "transactionKey-resultCode".</returns>
        public static List<string> SMSMessageSendToSMSServerReturnKeyList(Resource.SMSServerInfo smsServerInfo, DataTable dataTable)
        {
            var keyList = new List<string>();
            try
            {
                if (dataTable == null)
                {
                    Log.Error("SMSMessageSendToSMSServerReturnKeyList input dataTable Invalid");
                    return keyList;
                }
                foreach (DataRow dataRow in dataTable.Rows)
                {
                    //Create SMSMessage
                    string xmlStringMessage = dataRow.Field<string>(DatabaseMessageKeyName);
                    if (string.IsNullOrEmpty(xmlStringMessage))
                    {
                        Log.Error("SMSMessageSendToSMSServerReturnKeyList Get XML String Is null ");
                        keyList.Add($"{dataRow.Field<string>(DatabaseKeyName)}-{dataRow.Field<string>(DatabaseRtnCodeName)}");
                        continue;
                    }
                    //check encoding code BIG5
                    var big5encode = Encoding.GetEncoding(950);
                    var smsMessage = ConvertXML2SMSMessage(xmlStringMessage, false);
                    if (string.IsNullOrEmpty(smsMessage))
                    {
                        Log.Error("SMSMessageSendToSMSServerReturnKeyList Convert XML To SMS Format String Error ");
                        // keyList.Add($"{dataRow.Field<string>(DatabaseKeyName)}-{dataRow.Field<string>(DatabaseRtnCodeName)}");
                        keyList.Add($"{dataRow.Field<string>(DatabaseKeyName)}-{"E007"}");
                        continue;
                    }
                    // The message carries the card number field (CAC0010-LOGF-CARD-CARD): log its length only (PCI DSS).
                    Log.Info($"Send SMS Message DataLength[{Encoding.ASCII.GetByteCount(smsMessage)}]");
                    //Send Message To SMS Server
                    var smsSendBytes = big5encode.GetBytes(smsMessage);
                    var sendSMSResult = ColaCommUtility.SendSMSMessageToSMSServer(smsSendBytes, smsServerInfo);
                    Log.Info($"Send SMS Message GetRsp[{sendSMSResult}]");
                    // keyList.Add($"{dataRow.Field<string>(DatabaseKeyName)}-{dataRow.Field<string>(DatabaseRtnCodeName)}");
                    keyList.Add($"{dataRow.Field<string>(DatabaseKeyName)}-{sendSMSResult}");
                }
                return (keyList);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"SMSMessageSendToSMSServerReturnKeyList Exception:{ex.Message}");
                return (keyList);
            }
        }

        /// <summary>
        /// Calculates and appends the Luhn algorithm check digit to a card number.
        /// </summary>
        /// <param name="cardNumber">The card number without check digit (must be numeric).</param>
        /// <returns>The card number with appended check digit, or empty string if input is invalid.</returns>
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
    }
}
