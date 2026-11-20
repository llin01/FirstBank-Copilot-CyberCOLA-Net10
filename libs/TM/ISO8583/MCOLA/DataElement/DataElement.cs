//using System.Runtime.Remoting.Metadata.W3cXsd2001;

namespace Com.Cybersoft4u.TM.ISO8583.MCOLA.DataElement
{
    public class DE48Data
    {
        private const int DE48DataLength = 76;
        public DE48Data(string data)
        {
            if (string.IsNullOrEmpty(data)) return;
            var totalDataLength = data.Length;
            int offset = 0;

            //int subFieldLength = 3;
            //if (offset + subFieldLength <= totalDataLength)
            //    Length = data.Substring(offset, subFieldLength);
            ////check data length
            //if (int.TryParse(Length, out var realDataLength))
            //{
            //    if (realDataLength != DE48DataLength)
            //        return;
            //}
            //else
            //    return;

            //offset += subFieldLength;
            int subFieldLength = 3;
            if (offset + subFieldLength <= totalDataLength)
                PANSequenceNumber = data.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 4;
            if (offset + subFieldLength <= totalDataLength)
                FIID = data.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 2;
            if (offset + subFieldLength <= totalDataLength)
                AcctType = data.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 4;
            if (offset + subFieldLength <= totalDataLength)
                ErrorCode = data.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 4;
            if (offset + subFieldLength <= totalDataLength)
                VisaMessageReasonCodes = data.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 1;
            if (offset + subFieldLength <= totalDataLength)
                ResponseSourceReasonCode = data.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 4;
            if (offset + subFieldLength <= totalDataLength)
                STIPSwitchReasonCode = data.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 1;
            if (offset + subFieldLength <= totalDataLength)
                TCC = data.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 1;
            if (offset + subFieldLength <= totalDataLength)
                AccountNumberIndicator = data.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 50;
            if (offset + subFieldLength <= totalDataLength)
            {
                ChipCVRTVRErrorResults = data.Substring(offset, subFieldLength);
                CVROrTVRIdentifier = ChipCVRTVRErrorResults.Substring(0, 1);
                ByteID = ChipCVRTVRErrorResults.Substring(1, 2);
                ByteIdentifier = ChipCVRTVRErrorResults.Substring(3, 1);
                ValueOfBitInError = ChipCVRTVRErrorResults.Substring(4, 1);
            }

            offset += subFieldLength;
            subFieldLength = 1;
            if (offset + subFieldLength <= totalDataLength)
                AuthorizationCharacteristicsIndicator = data.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 1;
            if (offset + subFieldLength <= totalDataLength)
                MarketSpecificDataIdentifier = data.Substring(offset, subFieldLength);

        }

        public string Length { get; } = string.Empty;
        public string PANSequenceNumber { get; } = string.Empty;
        public string FIID { get; } = string.Empty;
        public string AcctType { get; } = string.Empty;
        public string ErrorCode { get; } = string.Empty;
        public string VisaMessageReasonCodes { get; } = string.Empty;
        public string ResponseSourceReasonCode { get; } = string.Empty;
        public string STIPSwitchReasonCode { get; } = string.Empty;
        public string TCC { get; set; } = string.Empty;
        public string AccountNumberIndicator { get; } = string.Empty;
        public string ChipCVRTVRErrorResults { get; } = string.Empty;
        public string CVROrTVRIdentifier { get; } = string.Empty;
        public string ByteID { get; } = string.Empty;
        public string ByteIdentifier { get; } = string.Empty;
        public string ValueOfBitInError { get; } = string.Empty;
        public string AuthorizationCharacteristicsIndicator { get; } = string.Empty;
        public string MarketSpecificDataIdentifier { get; } = string.Empty;

    }
    public class DE120MasterCardACNData
    {
        private const int DE120MasterCardACNDataLength = 236;
        public DE120MasterCardACNData(string data)
        {
            if (string.IsNullOrEmpty(data)) return;
            var processData = $"{data.Length:000}{data}";  //include LLL
            var totalDataLength = processData.Length;
            int offset = 0;

            int subFieldLength = 3;
            if (offset + subFieldLength <= totalDataLength)
                Length = processData.Substring(offset, subFieldLength);

            //check data length
            if (int.TryParse(Length, out var realDataLength))
            {
                if (realDataLength != DE120MasterCardACNDataLength)
                    return;
            }
            else
                return;

            offset += subFieldLength;
            subFieldLength = 19;
            if (offset + subFieldLength <= totalDataLength)
                PrimaryAccountNumber = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 4;
            if (offset + subFieldLength <= totalDataLength)
                PrimaryAccountNumberExpiryDate = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 14;
            if (offset + subFieldLength <= totalDataLength)
                CorrelationID = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 8;
            if (offset + subFieldLength <= totalDataLength)
                ActivationCode = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 10;
            if (offset + subFieldLength <= totalDataLength)
                ActivationCodeExpirationDateTime = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 165;
            if (offset + subFieldLength <= totalDataLength)
                ConsumerActivationMethodPreference = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 11;
            if (offset + subFieldLength <= totalDataLength)
                TokenRequestorID = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 3;
            if (offset + subFieldLength <= totalDataLength)
                WalletID = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 2;
            if (offset + subFieldLength <= totalDataLength)
                DeviceType = processData.Substring(offset, subFieldLength);
        }
        public string Length { get; } = string.Empty;
        public string PrimaryAccountNumber { get; } = string.Empty;
        public string PrimaryAccountNumberExpiryDate { get; } = string.Empty;
        public string CorrelationID { get; } = string.Empty;
        public string ActivationCode { get; } = string.Empty;
        public string ActivationCodeExpirationDateTime { get; } = string.Empty;
        public string ConsumerActivationMethodPreference { get; } = string.Empty;
        public string TokenRequestorID { get; } = string.Empty;
        public string WalletID { get; } = string.Empty;
        public string DeviceType { get; } = string.Empty;

    }
    public class DE120MasterCardTCNData
    {
        private const int DE120MasterCardTCNDataLength = 260;
        public DE120MasterCardTCNData(string data)
        {
            if (string.IsNullOrEmpty(data)) return;
            var processData = $"{data.Length:000}{data}";  //include LLL
            var totalDataLength = processData.Length;
            int offset = 0;

            int subFieldLength = 3;
            if (offset + subFieldLength <= totalDataLength)
                Length = processData.Substring(offset, subFieldLength);

            //check data length
            if (int.TryParse(Length, out var realDataLength))
            {
                if (realDataLength != DE120MasterCardTCNDataLength)
                    return;
            }
            else
                return;

            offset += subFieldLength;
            subFieldLength = 19;
            if (offset + subFieldLength <= totalDataLength)
                Token = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 4;
            if (offset + subFieldLength <= totalDataLength)
                TokenExpirationDate = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 19;
            if (offset + subFieldLength <= totalDataLength)
                PrimaryAccountNumber = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 4;
            if (offset + subFieldLength <= totalDataLength)
                PrimaryAccountNumberExpiryDate = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 1;
            if (offset + subFieldLength <= totalDataLength)
                TokenServiceProviderIdentification = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 2;
            if (offset + subFieldLength <= totalDataLength)
                TokenAssuranceLevel = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 11;
            if (offset + subFieldLength <= totalDataLength)
                TokenRequestorID = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 1;
            if (offset + subFieldLength <= totalDataLength)
                ContactlessUsage = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 1;
            if (offset + subFieldLength <= totalDataLength)
                CardOnFileElectronicCommerceUsage = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 1;
            if (offset + subFieldLength <= totalDataLength)
                MobileDigitalWalletElectronicCommerceUsage = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 14;
            if (offset + subFieldLength <= totalDataLength)
                CorrelationID = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 2;
            if (offset + subFieldLength <= totalDataLength)
                NumberOfActiveTokensForThePrimaryAccountNumber = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 10;
            if (offset + subFieldLength <= totalDataLength)
                IssuerProductConfigurationID = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 2;
            if (offset + subFieldLength <= totalDataLength)
                ConsumerLanguage = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 20;
            if (offset + subFieldLength <= totalDataLength)
                DeviceName = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 1;
            if (offset + subFieldLength <= totalDataLength)
                FinalTokenizationDecision = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 1;
            if (offset + subFieldLength <= totalDataLength)
                FinalTokenizationDecisionIndicator = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 32;
            if (offset + subFieldLength <= totalDataLength)
                TCIdentifier = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 10;
            if (offset + subFieldLength <= totalDataLength)
                TCDateAndTime = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 1;
            if (offset + subFieldLength <= totalDataLength)
                NumberOfActivationAttempts = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 48;
            if (offset + subFieldLength <= totalDataLength)
                TokenUniqueReference = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 48;
            if (offset + subFieldLength <= totalDataLength)
                PrimaryAccountNumberUniqueReference = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 1;
            if (offset + subFieldLength <= totalDataLength)
                TokenType = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 3;
            if (offset + subFieldLength <= totalDataLength)
                WalletID = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 2;
            if (offset + subFieldLength <= totalDataLength)
                DeviceType = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 2;
            if (offset + subFieldLength <= totalDataLength)
                StorageTechnology = processData.Substring(offset, subFieldLength);

        }
        public string Length { get; } = string.Empty;
        public string Token { get; } = string.Empty;
        public string TokenExpirationDate { get; } = string.Empty;
        public string PrimaryAccountNumber { get; } = string.Empty;
        public string PrimaryAccountNumberExpiryDate { get; } = string.Empty;
        public string TokenServiceProviderIdentification { get; } = string.Empty;
        public string TokenAssuranceLevel { get; } = string.Empty;
        public string TokenRequestorID { get; } = string.Empty;
        public string ContactlessUsage { get; } = string.Empty;
        public string CardOnFileElectronicCommerceUsage { get; } = string.Empty;
        public string MobileDigitalWalletElectronicCommerceUsage { get; } = string.Empty;
        public string CorrelationID { get; } = string.Empty;
        public string NumberOfActiveTokensForThePrimaryAccountNumber { get; } = string.Empty;
        public string IssuerProductConfigurationID { get; } = string.Empty;
        public string ConsumerLanguage { get; } = string.Empty;
        public string DeviceName { get; } = string.Empty;
        public string FinalTokenizationDecision { get; } = string.Empty;
        public string FinalTokenizationDecisionIndicator { get; } = string.Empty;
        public string TCIdentifier { get; } = string.Empty;
        public string TCDateAndTime { get; } = string.Empty;
        public string NumberOfActivationAttempts { get; } = string.Empty;
        public string TokenUniqueReference { get; } = string.Empty;
        public string PrimaryAccountNumberUniqueReference { get; } = string.Empty;
        public string TokenType { get; } = string.Empty;
        public string WalletID { get; } = string.Empty;
        public string DeviceType { get; } = string.Empty;
        public string StorageTechnology { get; } = string.Empty;
    }
    public class DE120MasterCardTVNData
    {
        private const int DE120MasterCardTVNDataLength = 84;
        public DE120MasterCardTVNData(string data)
        {
            if (string.IsNullOrEmpty(data)) return;
            var processData = $"{data.Length:000}{data}";  //include LLL
            var totalDataLength = processData.Length;
            int offset = 0;

            int subFieldLength = 3;
            if (offset + subFieldLength <= totalDataLength)
                Length = processData.Substring(offset, subFieldLength);

            //check data length
            if (int.TryParse(Length, out var realDataLength))
            {
                if (realDataLength != DE120MasterCardTVNDataLength)
                    return;
            }
            else
                return;

            offset += subFieldLength;
            subFieldLength = 19;
            if (offset + subFieldLength <= totalDataLength)
                PrimaryAccountNumber = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 4;
            if (offset + subFieldLength <= totalDataLength)
                PrimaryAccountNumberExpiryDate = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 19;
            if (offset + subFieldLength <= totalDataLength)
                Token = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 4;
            if (offset + subFieldLength <= totalDataLength)
                TokenExpirationDate = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 1;
            if (offset + subFieldLength <= totalDataLength)
                TokenServiceProviderIdentification = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 14;
            if (offset + subFieldLength <= totalDataLength)
                CorrelationID = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 1;
            if (offset + subFieldLength <= totalDataLength)
                TokenizationEventIndicator = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 2;
            if (offset + subFieldLength <= totalDataLength)
                TokenizationEventReasonCode = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 1;
            if (offset + subFieldLength <= totalDataLength)
                ContactlessUsage = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 1;
            if (offset + subFieldLength <= totalDataLength)
                CardOnFileElectronicCommerceUsage = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 1;
            if (offset + subFieldLength <= totalDataLength)
                MobileDigitalWalletElectronicCommerceUsage = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 1;
            if (offset + subFieldLength <= totalDataLength)
                EventRequestor = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 11;
            if (offset + subFieldLength <= totalDataLength)
                TokenRequestorID = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 3;
            if (offset + subFieldLength <= totalDataLength)
                WalletID = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 2;
            if (offset + subFieldLength <= totalDataLength)
                DeviceType = processData.Substring(offset, subFieldLength);
        }
        public string Length { get; } = string.Empty;
        public string PrimaryAccountNumber { get; } = string.Empty;
        public string PrimaryAccountNumberExpiryDate { get; } = string.Empty;
        public string Token { get; } = string.Empty;
        public string TokenExpirationDate { get; } = string.Empty;
        public string TokenServiceProviderIdentification { get; } = string.Empty;
        public string CorrelationID { get; } = string.Empty;
        public string TokenizationEventIndicator { get; } = string.Empty;
        public string TokenizationEventReasonCode { get; } = string.Empty;
        public string ContactlessUsage { get; } = string.Empty;
        public string CardOnFileElectronicCommerceUsage { get; } = string.Empty;
        public string MobileDigitalWalletElectronicCommerceUsage { get; } = string.Empty;
        public string EventRequestor { get; } = string.Empty;
        public string TokenRequestorID { get; } = string.Empty;
        public string WalletID { get; } = string.Empty;
        public string DeviceType { get; } = string.Empty;

    }
    public class DE120MasterCardTokenizedData
    {
        private const int DE120MasterCardTokenizedDataLength = 100;
        public DE120MasterCardTokenizedData(string data)
        {
            if (string.IsNullOrEmpty(data)) return;
            var processData = $"{data.Length:000}{data}";  //include LLL
            var totalDataLength = processData.Length;
            int offset = 0;

            int subFieldLength = 3;
            if (offset + subFieldLength <= totalDataLength)
                Length = processData.Substring(offset, subFieldLength);

            //check data length
            if (int.TryParse(Length, out var realDataLength))
            {
                if (realDataLength != DE120MasterCardTokenizedDataLength)
                    return;
            }
            else
                return;

            offset += subFieldLength;
            subFieldLength = 19;
            if (offset + subFieldLength <= totalDataLength)
                Token = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 4;
            if (offset + subFieldLength <= totalDataLength)
                TokenExpirationDate = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 2;
            if (offset + subFieldLength <= totalDataLength)
                TokenAssuranceLevel = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 11;
            if (offset + subFieldLength <= totalDataLength)
                TokenRequestorID = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 64;
            if (offset + subFieldLength <= totalDataLength)
                TokenTransactionIdentifier = processData.Substring(offset, subFieldLength);

        }

        public string Length { get; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string TokenExpirationDate { get; set; } = string.Empty;
        public string TokenAssuranceLevel { get; } = string.Empty;
        public string TokenRequestorID { get; } = string.Empty;
        public string TokenTransactionIdentifier { get; set; } = string.Empty;
        public string Encode()
        {
            return $"{Token,-19}{TokenExpirationDate,-4}{TokenAssuranceLevel,-2}{TokenRequestorID,-11}{TokenTransactionIdentifier,-64}";
        }

    }
    public class DE124VisaTokenData
    {
        private const int DE124VisaTokenDataLength = 484;
        public DE124VisaTokenData(string data)
        {
            if (string.IsNullOrEmpty(data)) return;
            var processData = $"{data.Length:000}{data}";  //include LLL
            var totalDataLength = processData.Length;
            int offset = 0;

            int subFieldLength = 3;
            if (offset + subFieldLength <= totalDataLength)
                Length = processData.Substring(offset, subFieldLength);

            //check data length
            if (int.TryParse(Length, out var realDataLength))
            {
                if (realDataLength != DE124VisaTokenDataLength)
                    return;
            }
            else
                return;

            offset += subFieldLength;
            subFieldLength = 26;
            if (offset + subFieldLength <= totalDataLength)
                CardholderName = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 1;
            if (offset + subFieldLength <= totalDataLength)
                ActivationVerificationResult = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 2;
            if (offset + subFieldLength <= totalDataLength)
                ActiveAccountManagementVelocityCheckingResult = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 4;
            if (offset + subFieldLength <= totalDataLength)
                ElapsedTimeToLive = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 3;
            if (offset + subFieldLength <= totalDataLength)
                CountOfNumberOfTransactions = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 7;
            if (offset + subFieldLength <= totalDataLength)
                CumulativeTransactionAmount = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 19;
            if (offset + subFieldLength <= totalDataLength)
                Token = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 2;
            if (offset + subFieldLength <= totalDataLength)
                TokenAssuranceLevel = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 11;
            if (offset + subFieldLength <= totalDataLength)
                TokenRequestorID = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 19;
            if (offset + subFieldLength <= totalDataLength)
                PrimaryAccountNumberAccountRange = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 32;
            if (offset + subFieldLength <= totalDataLength)
                TokenReferenceID = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 4;
            if (offset + subFieldLength <= totalDataLength)
                TokenExpirationDate = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 2;
            if (offset + subFieldLength <= totalDataLength)
                TokenType = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 1;
            if (offset + subFieldLength <= totalDataLength)
                TokenStatus = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 1;
            if (offset + subFieldLength <= totalDataLength)
                LastUpdatedBy = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 32;
            if (offset + subFieldLength <= totalDataLength)
                PANReferenceID = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 8;
            if (offset + subFieldLength <= totalDataLength)
                ActivationCode = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 12;
            if (offset + subFieldLength <= totalDataLength)
                ActivationCodeExpiryDateTime = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 2;
            if (offset + subFieldLength <= totalDataLength)
                ActivationCodeVerificationAttempts = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 2;
            if (offset + subFieldLength <= totalDataLength)
                NumberOfActivationCodesIssued = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 2;
            if (offset + subFieldLength <= totalDataLength)
                VisaTokenScore = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 2;
            if (offset + subFieldLength <= totalDataLength)
                VisaTokenDecision = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 2;
            if (offset + subFieldLength <= totalDataLength)
                NumberOfActiveTokens = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 2;
            if (offset + subFieldLength <= totalDataLength)
                NumberOfInactiveTokens = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 2;
            if (offset + subFieldLength <= totalDataLength)
                NumberOfSuspendedTokens = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 2;
            if (offset + subFieldLength <= totalDataLength)
                DeviceType = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 3;
            if (offset + subFieldLength <= totalDataLength)
                DeviceLanguageCode = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 48;
            if (offset + subFieldLength <= totalDataLength)
                DeviceID = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 15;
            if (offset + subFieldLength <= totalDataLength)
                DeviceNumber = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 16;
            if (offset + subFieldLength <= totalDataLength)
                DeviceName = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 15;
            if (offset + subFieldLength <= totalDataLength)
                IPAddress = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 1;
            if (offset + subFieldLength <= totalDataLength)
                WalletProviderRiskAssessment = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 10;
            if (offset + subFieldLength <= totalDataLength)
                WalletProviderRiskAssessmentVersion = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 2;
            if (offset + subFieldLength <= totalDataLength)
                WalletProviderDeviceScore = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 2;
            if (offset + subFieldLength <= totalDataLength)
                WalletProviderAccountScore = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 30;
            if (offset + subFieldLength <= totalDataLength)
                WalletProviderReasonCodes = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 2;
            if (offset + subFieldLength <= totalDataLength)
                PANSource = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 32;
            if (offset + subFieldLength <= totalDataLength)
                WalletAccountID = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 32;
            if (offset + subFieldLength <= totalDataLength)
                IssuerTermsandConditionsDateTime = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 19;
            if (offset + subFieldLength <= totalDataLength)
                ReplacementPAN = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 4;
            if (offset + subFieldLength <= totalDataLength)
                ReplacementPANExpirationDate = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 2;
            if (offset + subFieldLength <= totalDataLength)
                BoundDeviceIndex = processData.Substring(offset, subFieldLength);
            offset += subFieldLength;
            subFieldLength = 11;
            if (offset + subFieldLength <= totalDataLength)
                TokenUserIdentifier = processData.Substring(offset, subFieldLength);
            offset += subFieldLength;
            subFieldLength = 2;
            if (offset + subFieldLength <= totalDataLength)
                TokenUserApplicationType = processData.Substring(offset, subFieldLength);
            offset += subFieldLength;
            subFieldLength = 11;
            if (offset + subFieldLength <= totalDataLength)
                TokenServiceProviderID = processData.Substring(offset, subFieldLength);
            offset += subFieldLength;
            subFieldLength = 25;
            if (offset + subFieldLength <= totalDataLength)
                DeviceLocation = processData.Substring(offset, subFieldLength);
        }
        public string Length { get; } = string.Empty;
        public string CardholderName { get; } = string.Empty;
        public string ActivationVerificationResult { get; } = string.Empty;
        public string ActiveAccountManagementVelocityCheckingResult { get; } = string.Empty;
        public string ElapsedTimeToLive { get; } = string.Empty;
        public string CountOfNumberOfTransactions { get; } = string.Empty;
        public string CumulativeTransactionAmount { get; } = string.Empty;
        public string Token { get; } = string.Empty;
        public string TokenAssuranceLevel { get; } = string.Empty;
        public string TokenRequestorID { get; } = string.Empty;
        public string PrimaryAccountNumberAccountRange { get; } = string.Empty;
        public string TokenReferenceID { get; } = string.Empty;
        public string TokenExpirationDate { get; } = string.Empty;
        public string TokenType { get; } = string.Empty;
        public string TokenStatus { get; } = string.Empty;
        public string LastUpdatedBy { get; } = string.Empty;
        public string PANReferenceID { get; } = string.Empty;
        public string ActivationCode { get; } = string.Empty;
        public string ActivationCodeExpiryDateTime { get; } = string.Empty;
        public string ActivationCodeVerificationAttempts { get; } = string.Empty;
        public string NumberOfActivationCodesIssued { get; } = string.Empty;
        public string VisaTokenScore { get; } = string.Empty;
        public string VisaTokenDecision { get; } = string.Empty;
        public string NumberOfActiveTokens { get; } = string.Empty;
        public string NumberOfInactiveTokens { get; } = string.Empty;
        public string NumberOfSuspendedTokens { get; } = string.Empty;
        public string DeviceType { get; } = string.Empty;
        public string DeviceLanguageCode { get; } = string.Empty;
        public string DeviceID { get; } = string.Empty;
        public string DeviceNumber { get; } = string.Empty;
        public string DeviceName { get; } = string.Empty;
        public string IPAddress { get; } = string.Empty;
        public string WalletProviderRiskAssessment { get; } = string.Empty;
        public string WalletProviderRiskAssessmentVersion { get; } = string.Empty;
        public string WalletProviderDeviceScore { get; } = string.Empty;
        public string WalletProviderAccountScore { get; } = string.Empty;
        public string WalletProviderReasonCodes { get; } = string.Empty;
        public string PANSource { get; } = string.Empty;
        public string WalletAccountID { get; } = string.Empty;
        public string IssuerTermsandConditionsDateTime { get; } = string.Empty;
        public string ReplacementPAN { get; } = string.Empty;
        public string ReplacementPANExpirationDate { get; } = string.Empty;
        public string BoundDeviceIndex { get; } = string.Empty;
        public string TokenUserIdentifier { get; } = string.Empty;
        public string TokenUserApplicationType { get; } = string.Empty;
        public string TokenServiceProviderID { get; } = string.Empty;
        public string DeviceLocation { get; } = string.Empty;
    }
    public class DE124MasterCardTARData
    {
        private const int DE124MasterCardTARDataLength = 195;
        public DE124MasterCardTARData(string data)
        {
            if (string.IsNullOrEmpty(data)) return;
            var processData = $"{data.Length:000}{data}";  //include LLL
            var totalDataLength = processData.Length;
            int offset = 0;

            int subFieldLength = 3;
            if (offset + subFieldLength <= totalDataLength)
                Length = processData.Substring(offset, subFieldLength);

            //check data length
            if (int.TryParse(Length, out var realDataLength))
            {
                if (realDataLength != DE124MasterCardTARDataLength)
                    return;
            }
            else
                return;

            offset += subFieldLength;
            subFieldLength = 2;
            if (offset + subFieldLength <= totalDataLength)
                MessageType = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 14;
            if (offset + subFieldLength <= totalDataLength)
                CorrelationID = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 1;
            if (offset + subFieldLength <= totalDataLength)
                PrimaryAccountNumberSource = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 48;
            if (offset + subFieldLength <= totalDataLength)
                PaymentApplicationInstanceID = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 12;
            if (offset + subFieldLength <= totalDataLength)
                DeviceSourceIPAddress = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 64;
            if (offset + subFieldLength <= totalDataLength)
                WalletServiceProviderAccountIDHash = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 27;
            if (offset + subFieldLength <= totalDataLength)
                CardholderName = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 1;
            if (offset + subFieldLength <= totalDataLength)
                WalletProviderTokenizationRecommendation = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 2;
            if (offset + subFieldLength <= totalDataLength)
                WalletProviderTokenizationRecommendationStandardVersion = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 1;
            if (offset + subFieldLength <= totalDataLength)
                WalletProviderDeviceScore = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 1;
            if (offset + subFieldLength <= totalDataLength)
                WalletProviderAccountScore = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 2;
            if (offset + subFieldLength <= totalDataLength)
                NumberOfActiveTokensForThePrimaryAccountNumber = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 6;
            if (offset + subFieldLength <= totalDataLength)
                WalletProviderTokenizationRecommendationReasonCodes = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 9;
            if (offset + subFieldLength <= totalDataLength)
                DeviceLocation = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 4;
            if (offset + subFieldLength <= totalDataLength)
                MobileNumberLast4Digits = processData.Substring(offset, subFieldLength);

            offset += subFieldLength;
            subFieldLength = 1;
            if (offset + subFieldLength <= totalDataLength)
                TokenType = processData.Substring(offset, subFieldLength);
        }
        public string Length { get; } = string.Empty;
        public string MessageType { get; } = string.Empty;
        public string CorrelationID { get; } = string.Empty;
        public string PrimaryAccountNumberSource { get; } = string.Empty;
        public string PaymentApplicationInstanceID { get; } = string.Empty;
        public string DeviceSourceIPAddress { get; } = string.Empty;
        public string WalletServiceProviderAccountIDHash { get; } = string.Empty;
        public string CardholderName { get; } = string.Empty;
        public string WalletProviderTokenizationRecommendation { get; } = string.Empty;
        public string WalletProviderTokenizationRecommendationStandardVersion { get; } = string.Empty;
        public string WalletProviderDeviceScore { get; } = string.Empty;
        public string WalletProviderAccountScore { get; } = string.Empty;
        public string NumberOfActiveTokensForThePrimaryAccountNumber { get; } = string.Empty;
        public string WalletProviderTokenizationRecommendationReasonCodes { get; } = string.Empty;
        public string DeviceLocation { get; } = string.Empty;
        public string MobileNumberLast4Digits { get; } = string.Empty;
        public string TokenType { get; } = string.Empty;
    }
}
