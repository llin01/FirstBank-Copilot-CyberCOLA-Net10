using System;
using NLog;

namespace Com.Cybersoft4u.TM.FISC.DE124
{
    public class DE124MasterCardToken
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private readonly string _messageType;
        private readonly string _correlationID;
        private readonly string _primaryAcctNbrSource;
        private readonly string _paymentAppInstanceId;
        private readonly int _numberOfActiveToken;
        private readonly string _walletServiceProvideAcctID;
        private readonly string _cardholderName;
        private readonly string _tokenType;
        private readonly string _deviceSourceIP;
        private readonly string _walletProviderTokenRecommen;
        private readonly string _walletProviderTokenStandard;
        private readonly int _walletProviderDeviceScore;
        private readonly int _walletProviderAcctScore;
        private readonly string _walletProviderReasonCde;
        private readonly string _deviceLocation;
        private readonly string _mobileNumber;

        public DE124MasterCardToken(string inputDE124String)
        {
            int startPos = 0;
            int dataLength = 0;
            int totalDataLength = inputDE124String.Length;

            dataLength = 2;
            if (startPos + dataLength > totalDataLength)
            {
                Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                return;
            }
            _messageType = inputDE124String.Substring(startPos, dataLength);
            startPos += dataLength;

            if (_messageType.Equals("TE"))
            {
                dataLength = 14;
                if ((startPos + dataLength) > totalDataLength)
                {
                    Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                    return;
                }
                _correlationID = inputDE124String.Substring(startPos, dataLength);
                startPos += dataLength;

                dataLength = 1;
                if ((startPos + dataLength) > totalDataLength)
                {
                    Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                    return;
                }
                _primaryAcctNbrSource = inputDE124String.Substring(startPos, dataLength);
                startPos += dataLength;

                dataLength = 48;
                if ((startPos + dataLength) > totalDataLength)
                {
                    Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                    return;
                }
                _paymentAppInstanceId = inputDE124String.Substring(startPos, dataLength);
                startPos += dataLength;

                dataLength = 2;
                if ((startPos + dataLength) > totalDataLength)
                {
                    Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                    return;
                }
                var tokenActive = inputDE124String.Substring(startPos, dataLength);
                _numberOfActiveToken = !string.IsNullOrWhiteSpace(tokenActive) ? Convert.ToInt16(inputDE124String.Substring(startPos, dataLength)) : 0;

                startPos += dataLength;

                dataLength = 64;
                if ((startPos + dataLength) > totalDataLength)
                {
                    Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                    return;
                }
                _walletServiceProvideAcctID = inputDE124String.Substring(startPos, dataLength);
                startPos += dataLength;

                dataLength = 27;
                if ((startPos + dataLength) > totalDataLength)
                {
                    Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                    return;
                }
                _cardholderName = inputDE124String.Substring(startPos, dataLength);
                startPos += dataLength;

                dataLength = 1;
                if ((startPos + dataLength) > totalDataLength)
                {
                    Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                    return;
                }
                _tokenType = inputDE124String.Substring(startPos, dataLength);
                startPos += dataLength;
            }
            else if (_messageType.Equals("TA"))
            {
                dataLength = 14;
                if (startPos + dataLength > totalDataLength)
                {
                    Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                    return;
                }
                _correlationID = inputDE124String.Substring(startPos, dataLength);
                startPos += dataLength;

                dataLength = 1;
                if ((startPos + dataLength) > totalDataLength)
                {
                    Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                    return;
                }
                _primaryAcctNbrSource = inputDE124String.Substring(startPos, dataLength);
                startPos += dataLength;

                dataLength = 48;
                if ((startPos + dataLength) > totalDataLength)
                {
                    Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                    return;
                }
                _paymentAppInstanceId = inputDE124String.Substring(startPos, dataLength);
                startPos += dataLength;

                dataLength = 12;
                if ((startPos + dataLength) > totalDataLength)
                {
                    Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                    return;
                }
                _deviceSourceIP = inputDE124String.Substring(startPos, dataLength);
                startPos += dataLength;

                dataLength = 64;
                if ((startPos + dataLength) > totalDataLength)
                {
                    Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                    return;
                }
                _walletServiceProvideAcctID = inputDE124String.Substring(startPos, dataLength);
                startPos += dataLength;

                dataLength = 27;
                if ((startPos + dataLength) > totalDataLength)
                {
                    Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                    return;
                }
                _cardholderName = inputDE124String.Substring(startPos, dataLength);
                startPos += dataLength;

                dataLength = 1;
                if ((startPos + dataLength) > totalDataLength)
                {
                    Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                    return;
                }
                _walletProviderTokenRecommen = inputDE124String.Substring(startPos, dataLength);
                startPos += dataLength;

                dataLength = 2;
                if ((startPos + dataLength) > totalDataLength)
                {
                    Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                    return;
                }
                _walletProviderTokenStandard = inputDE124String.Substring(startPos, dataLength);
                startPos += dataLength;

                dataLength = 1;
                if ((startPos + dataLength) > totalDataLength)
                {
                    Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                    return;
                }
                var deviceScore = inputDE124String.Substring(startPos, dataLength);
                _walletProviderDeviceScore = !string.IsNullOrWhiteSpace(deviceScore) ? Convert.ToInt16(inputDE124String.Substring(startPos, dataLength)) : 0;

                startPos += dataLength;


                dataLength = 1;
                if ((startPos + dataLength) > totalDataLength)
                {
                    Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                    return;
                }

                var acctScore = inputDE124String.Substring(startPos, dataLength);
                _walletProviderAcctScore = !string.IsNullOrWhiteSpace(acctScore) ? Convert.ToInt16(inputDE124String.Substring(startPos, dataLength)) : 0;
                startPos += dataLength;

                dataLength = 2;
                if ((startPos + dataLength) > totalDataLength)
                {
                    Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                    return;
                }
                var numOfActToken = inputDE124String.Substring(startPos, dataLength);
                _numberOfActiveToken = !string.IsNullOrWhiteSpace(numOfActToken) ? Convert.ToInt16(inputDE124String.Substring(startPos, dataLength)) : 0;
                startPos += dataLength;

                dataLength = 6;
                if ((startPos + dataLength) > totalDataLength)
                {
                    Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                    return;
                }
                _walletProviderReasonCde = inputDE124String.Substring(startPos, dataLength);
                startPos += dataLength;

                dataLength = 9;
                if ((startPos + dataLength) > totalDataLength)
                {
                    Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                    return;
                }
                _deviceLocation = inputDE124String.Substring(startPos, dataLength);
                startPos += dataLength;

                dataLength = 4;
                if ((startPos + dataLength) > totalDataLength)
                {
                    Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                    return;
                }
                _mobileNumber = inputDE124String.Substring(startPos, dataLength);
                startPos += dataLength;

                dataLength = 1;
                if ((startPos + dataLength) > totalDataLength)
                {
                    Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                    return;
                }
                _tokenType = inputDE124String.Substring(startPos, dataLength);
                startPos += dataLength;
            }



        }
        public string GetMessageType()
        {
            return _messageType;
        }
        public string GetCorrelationID()
        {
            return _correlationID;
        }
        public string GetPrimaryAcctNbrSource()
        {
            return _primaryAcctNbrSource;
        }
        public string GetPaymentAppInstanceId()
        {
            return _paymentAppInstanceId;
        }
        public int GetNumberOfActivateToken()
        {
            return _numberOfActiveToken;
        }
        public string GetWalletServiceProvideAcctID()
        {
            return _walletServiceProvideAcctID;
        }
        public string GetCardholderName()
        {
            return _cardholderName;
        }
        public string GetTokenType()
        {
            return _tokenType;
        }
        public string GetDeviceSourceIP()
        {
            return _deviceSourceIP;
        }
        public string GetWalletProviderTokenRecommen()
        {
            return _walletProviderTokenRecommen;
        }
        public string GetWalletProviderTokenStandard()
        {
            return _walletProviderTokenStandard;
        }
        public int GetWalletProviderDeviceScore()
        {
            return _walletProviderDeviceScore;
        }
        public int GetWalletProviderAcctScore()
        {
            return _walletProviderAcctScore;
        }
        public string GetWalletProviderReasonCde()
        {
            return _walletProviderReasonCde;
        }
        public string GetDeviceLocation()
        {
            return _deviceLocation;
        }
        public string GetMobileNumber()
        {
            return _mobileNumber;
        }

    }
}
