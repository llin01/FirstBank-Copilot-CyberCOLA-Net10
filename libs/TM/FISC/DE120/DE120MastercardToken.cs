using System;
using NLog;

namespace Com.Cybersoft4u.TM.FISC.DE120
{
    public class DE120MasterCardToken
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private readonly string _token;
        private readonly string _tokenExpirDte;
        private readonly string _pan;
        private readonly string _panExiprDte;
        private readonly string _tokenProviderIdentification;
        private readonly string _tokenAssuranceLevel;
        private readonly string _tokenRequestorId;
        private readonly string _contactlessUsage;
        private readonly string _cardOnFileUsage;
        private readonly string _digitalWalletUsage;
        private readonly string _correlationId;
        private readonly string _numOfActiveToken;
        private readonly string _issuerProductId;
        private readonly string _consumerLanguage;
        private readonly string _deviceName;
        private readonly string _finalTokenDecision;
        private readonly string _finalTokenDecisionInd;
        private readonly string _tcIdentifier;
        private readonly string _tcDteTime;
        private readonly string _numOfActivationAttempts;
        private readonly string _tokenUniqueRefer;
        private readonly string _panUniqueRefer;
        private readonly string _tokenType;
        private readonly string _walletId;
        private readonly string _deviceType;
        private readonly string _tokenEventIndicator;
        private readonly string _tokenEventRsnCode;
        private readonly string _eventRequest;
        private readonly string _activationCode;
        private readonly string _activationCodeExpirDte;
        private readonly string _customerActivationCodeMethod;

        public DE120MasterCardToken(string eventIndicate, string inputDe120Data)
        {
            const int fixOffSet = 0;
            if (inputDe120Data == null)
            {
                Log.Fatal($"Method:[{new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name}] -- Description[DE120MasterCardToken]-- Error:[DE120 Tag Data Is null]");
                throw new ArgumentException("DE120 Tag Data Is null");
            }

            try
            {
                var totalDataLength = inputDe120Data.Length;
                string inputDe120String = inputDe120Data.Substring(fixOffSet, totalDataLength);
                int startPos = 0;
                int dataLength = 0;

                if (eventIndicate.Equals("0250"))
                {
                    //Tokenization Event Notification
                    dataLength = 19;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _pan = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 4;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _panExiprDte = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 14;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _correlationId = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 8;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _activationCode = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 10;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _activationCodeExpirDte = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 165;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _customerActivationCodeMethod = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;
                }
                else if (eventIndicate.Equals("0251"))
                {
                    //Tokenization Complete Notification
                    dataLength = 19;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _token = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 4;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _tokenExpirDte = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 19;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _pan = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 4;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _panExiprDte = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 1;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _tokenProviderIdentification = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 2;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _tokenAssuranceLevel = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 11;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _tokenRequestorId = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 1;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _contactlessUsage = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 1;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _cardOnFileUsage = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 1;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _digitalWalletUsage = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 14;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _correlationId = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 2;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _numOfActiveToken = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 10;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _issuerProductId = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 2;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _consumerLanguage = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 20;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _deviceName = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 1;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _finalTokenDecision = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 1;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _finalTokenDecisionInd = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 32;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _tcIdentifier = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 10;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _tcDteTime = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 1;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _numOfActivationAttempts = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 48;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _tokenUniqueRefer = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 48;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _panUniqueRefer = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 1;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _tokenType = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 3;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _walletId = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 2;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _deviceType = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                }
                else if (eventIndicate.Equals("0252"))
                {
                    //Tokenization Event Notification
                    dataLength = 19;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _pan = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 4;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _panExiprDte = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 19;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _token = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 4;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _tokenExpirDte = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 1;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _tokenProviderIdentification = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 14;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _correlationId = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 1;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _tokenEventIndicator = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 2;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _tokenEventRsnCode = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 1;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _contactlessUsage = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 1;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _cardOnFileUsage = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 1;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _digitalWalletUsage = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 1;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _eventRequest = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 11;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _tokenRequestorId = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 3;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _walletId = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;

                    dataLength = 2;
                    if ((startPos + dataLength) > totalDataLength)
                    {
                        Log.Info($"Offset[{startPos}] + DataLength[{dataLength}] > TotalDataLength[{totalDataLength}]");
                        return;
                    }
                    _deviceType = inputDe120String.Substring(startPos, dataLength);
                    startPos += dataLength;
                }
            }
            catch (Exception ex)
            {
                Log.Fatal($"Method:[{new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name}] -- Description[DE120MasterCardToken Unpack DE120 Tag Field Error]-- Error:[DE120Tag Constructor(Message) Exception:{ex.Message}]");
                throw new Exception($"DE120Tag Constructor(Message) Exception:{ex.Message}");
            }
        }
        public string GetToken()
        {
            return _token;
        }
        public string GetTokenExpirDte()
        {
            return _tokenExpirDte;
        }
        public string GetPan()
        {
            return _pan;
        }
        public string GetPanExiprDte()
        {
            return _panExiprDte;
        }
        public string GetTokenProviderIdentification()
        {
            return _tokenProviderIdentification;
        }
        public string GetTokenAssuranceLevel()
        {
            return _tokenAssuranceLevel;
        }
        public string GetTokenRequestorId()
        {
            return _tokenRequestorId;
        }
        public string GetContactlessUsage()
        {
            return _contactlessUsage;
        }
        public string GetCardOnFileUsage()
        {
            return _cardOnFileUsage;
        }
        public string GetDigitalWalletUsage()
        {
            return _digitalWalletUsage;
        }
        public string GetCorrelationId()
        {
            return _correlationId;
        }
        public string GetNumOfActiveToken()
        {
            return _numOfActiveToken;
        }
        public string GetIssuerProductId()
        {
            return _issuerProductId;
        }
        public string GetConsumerLanguage()
        {
            return _consumerLanguage;
        }
        public string GetDeviceName()
        {
            return _deviceName;
        }
        public string GetFinalTokenDecision()
        {
            return _finalTokenDecision;
        }
        public string GetFinalTokenDecisionInd()
        {
            return _finalTokenDecisionInd;
        }
        public string GetTcIdentifier()
        {
            return _tcIdentifier;
        }
        public string GetTcDteTime()
        {
            return _tcDteTime;
        }
        public string GetNumOfActivationAttempts()
        {
            return _numOfActivationAttempts;
        }
        public string GetTokenUniqueRefer()
        {
            return _tokenUniqueRefer;
        }
        public string GetPanUniqueRefer()
        {
            return _panUniqueRefer;
        }
        public string GetTokenType()
        {
            return _tokenType;
        }
        public string GetWalletId()
        {
            return _walletId;
        }
        public string GetDeviceType()
        {
            return _deviceType;
        }
        public string GetTokenEventIndicator()
        {
            return _tokenEventIndicator;
        }
        public string GetTokenEventRsnCode()
        {
            return _tokenEventRsnCode;
        }
        public string GetEventRequest()
        {
            return _eventRequest;
        }
        public string GetActivationCode()
        {
            return _activationCode;
        }
        public string GetActivationCodeExpirDte()
        {
            return _activationCodeExpirDte;
        }
        public string GetCustomerActivationCodeMethod()
        {
            return _customerActivationCodeMethod;
        }

    }
}
