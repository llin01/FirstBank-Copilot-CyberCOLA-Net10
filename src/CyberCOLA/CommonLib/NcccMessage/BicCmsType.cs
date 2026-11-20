using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using NLog;
using System;

namespace Com.Cybersoft4u.CommonLib.NcccMessage
{
    /// <summary>
    /// Define Cryptographic Service Message Type
    /// /// </summary>
    ///
    public class BicCsmType
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private readonly string _checkDigit;
        private readonly string _messageClass;
        private readonly string _receiveOfTheMessage;
        private readonly string _originatorOfTheMessage;
        private readonly string _serviceRequest;
        private readonly string _newKeyValue;
        private readonly string _hexadecimalKeyCounter;
        private readonly string _errorReference;
        public enum BicCsmClass
        {
            BicCsmClassRsi,
            BicCsmClassKsm,
            BicCsmClassRsm,
            BicCsmClassEsm
        }

        /// <summary>
        /// It initializes a new Cryptographic Service Message
        /// </summary>
        //public BicCsmType(string s120FieldData, string s123FieldData)
        public BicCsmType(NcccIso8583 ncccMessage)
        {
            _checkDigit = string.Empty;
            _messageClass = string.Empty;
            _receiveOfTheMessage = string.Empty;
            _originatorOfTheMessage = string.Empty;
            _serviceRequest = string.Empty;
            _newKeyValue = string.Empty;
            _hexadecimalKeyCounter = string.Empty;
            _errorReference = string.Empty;
            try
            {
                FIELD field = FIELD.F_120;
                string s120FieldData;

                if (!ObjectUtils.IsNull(ncccMessage.RawVal(field)))
                {
                    s120FieldData = (string)ncccMessage.Core().Field(field);
                }
                else
                {
                    return;
                }
                field = FIELD.F_123;
                string s123FieldData;
                if (!ObjectUtils.IsNull(ncccMessage.RawVal(field)))
                {
                    s123FieldData = (string)ncccMessage.Core().Field(field);
                }
                else
                {
                    return;
                }
                _checkDigit = s120FieldData.Substring(0, 4);
                int startPos = s123FieldData.IndexOf('(');
                int endPos = s123FieldData.IndexOf(')');
                if ((startPos == -1) || (endPos == -1) || (endPos <= startPos))
                {
                    Log.Info("BicCsmType Constructor Error,StartPos[{0}],EndPos[{1}]", startPos, endPos);
                    return;
                }
                string[] segmentArray = s123FieldData.Substring(startPos + 1, endPos - startPos - 2).Split(' ');
                foreach (string csmClass in SystemConstantData.CommonData.NcccCsmClass)
                {
                    foreach (string data in segmentArray)
                    {
                        if (data.IndexOf(csmClass) != -1)
                        {
                            int indexPos;
                            switch (csmClass)
                            {
                                case "MCL":
                                    indexPos = data.IndexOf('/');
                                    if (indexPos != -1)
                                        _messageClass = data.Substring(indexPos + 1, data.Trim().Length - indexPos - 1);
                                    break;
                                case "RCV":
                                    indexPos = data.IndexOf('/');
                                    if (indexPos != -1)
                                        _receiveOfTheMessage = data.Substring(indexPos + 1,
                                                                              data.Trim().Length - indexPos - 1);
                                    break;
                                case "ORG":
                                    indexPos = data.IndexOf('/');
                                    if (indexPos != -1)
                                        _originatorOfTheMessage = data.Substring(indexPos + 1,
                                                                                 data.Trim().Length - indexPos - 1);
                                    break;
                                case "SVR":
                                    indexPos = data.IndexOf('/');
                                    if (indexPos != -1)
                                        _serviceRequest = data.Substring(indexPos + 1, data.Trim().Length - indexPos - 1);
                                    break;
                                case "KD":
                                    indexPos = data.IndexOf('/');
                                    if (indexPos != -1)
                                        _newKeyValue = data.Substring(indexPos + 1, data.Trim().Length - indexPos - 1);
                                    break;
                                case "CTP":
                                    indexPos = data.IndexOf('/');
                                    if (indexPos != -1)
                                        _hexadecimalKeyCounter = data.Substring(indexPos + 1, data.Trim().Length - indexPos - 1);
                                    break;
                                case "ERF":
                                    indexPos = data.IndexOf('/');
                                    if (indexPos != -1)
                                        _errorReference = data.Substring(indexPos + 1, data.Trim().Length - indexPos - 1);
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"BicCsmType Exception:{ex.Message}");

                _checkDigit = string.Empty;
                _messageClass = string.Empty;
                _receiveOfTheMessage = string.Empty;
                _originatorOfTheMessage = string.Empty;
                _serviceRequest = string.Empty;
                _newKeyValue = string.Empty;
                _hexadecimalKeyCounter = string.Empty;
                _errorReference = string.Empty;
            }
        }

        /// <summary>
        /// Get Check Digit
        /// </summary>
        /// <returns>
        /// Check Digit Value
        /// </returns>
        public string CheckDigit
        {
            get
            {
                return (_checkDigit);
            }
        }

        /// <summary>
        /// Get Message Class
        /// </summary>
        /// <returns>
        /// Message Class Value
        /// </returns>
        public string MessageClass
        {
            get
            {
                return (_messageClass);
            }
        }

        /// <summary>
        /// Get Receive Of The Message
        /// </summary>
        /// <returns>
        /// Receive Of The Message Value
        /// </returns>
        public string ReceiveOfTheMessage
        {
            get
            {
                return (_receiveOfTheMessage);
            }
        }

        /// <summary>
        /// Get Originator Of The Message
        /// </summary>
        /// <returns>
        /// Originator Of The Message Value
        /// </returns>
        public string OriginatorOfTheMessage
        {
            get
            {
                return (_originatorOfTheMessage);
            }
        }

        /// <summary>
        /// Get Service Request
        /// </summary>
        /// <returns>
        /// Service Request Value
        /// </returns>
        public string ServiceRequest
        {
            get
            {
                return (_serviceRequest);
            }
        }

        /// <summary>
        /// Get New Key Value
        /// </summary>
        /// <returns>
        /// Get New Key Value
        /// </returns>
        public string NewKeyValue
        {
            get
            {
                return (_newKeyValue);
            }
        }

        /// <summary>
        /// Get Hexadecimal Key Counter
        /// </summary>
        /// <returns>
        /// Get Hexadecimal Key Counter Value
        /// </returns>
        public string HexadecimalKeyCounter
        {
            get
            {
                return (_hexadecimalKeyCounter);
            }
        }

        /// <summary>
        /// Get Error Reference
        /// </summary>
        /// <returns>
        /// Error Reference Value
        /// </returns>
        public string ErrorReference
        {
            get
            {
                return (_errorReference);
            }
        }
    }
}

