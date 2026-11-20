namespace Com.Cybersoft4u.TM.MasterCard
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.MasterCard.Transaction;

    #endregion

    public class MasterCardTransCreator
    {
        public static AuthorizationRequest AuthorizationRequest(byte[] message = null)
        {
            return new AuthorizationRequest(message);
        }

        public static AuthorizationResponse AuthorizationResponse(byte[] message = null)
        {
            return new AuthorizationResponse(message);
        }

        public static AuthorizationAdviceRequest AuthorizationAdviceRequest(byte[] message = null)
        {
            return new AuthorizationAdviceRequest(message);
        }

        public static AuthorizationAdviceResponse AuthorizationAdviceResponse(byte[] message = null)
        {
            return new AuthorizationAdviceResponse(message);
        }

        public static ReversalRequest ReversalRequest(byte[] message = null)
        {
            return new ReversalRequest(message);
        }

        public static ReversalResponse ReversalResponse(byte[] message = null)
        {
            return new ReversalResponse(message);
        }

        public static ReversalAdviceRequest ReversalAdviceRequest(byte[] message = null)
        {
            return new ReversalAdviceRequest(message);
        }

        public static ReversalAdviceResponse ReversalAdviceResponse(byte[] message = null)
        {
            return new ReversalAdviceResponse(message);
        }

        public static AuthorizationResponseNegAck AuthorizationResponseNegAck()
        {
            return new AuthorizationResponseNegAck();
        }

        public static IssuerFileUpdateRequest IssuerFileUpdateRequest(byte[] message = null)
        {
            return new IssuerFileUpdateRequest(message);
        }

        public static IssuerFileUpdateResponse IssuerFileUpdateResponse(byte[] message = null)
        {
            return new IssuerFileUpdateResponse(message);
        }

        public static NetworkConnectionStatusRequest NetworkConnectionStatusRequest(byte[] message = null)
        {
            return new NetworkConnectionStatusRequest(message);
        }

        public static NetworkConnectionStatusResponse NetworkConnectionStatusResponse(byte[] message = null)
        {
            return new NetworkConnectionStatusResponse(message);
        }

        public static AdministrativeRequest AdministrativeRequest(byte[] message = null)
        {
            return new AdministrativeRequest(message);
        }

        public static AdministrativeResponse AdministrativeResponse(byte[] message = null)
        {
            return new AdministrativeResponse(message);
        }

        public static AdministrativeAdviceRequest AdministrativeAdviceRequest()
        {
            return new AdministrativeAdviceRequest();
        }

        public static AdministrativeAdviceResponse AdministrativeAdviceResponse()
        {
            return new AdministrativeAdviceResponse();
        }

        public static SignOnRequest SignOnRequest(byte[] message = null)
        {
            return new SignOnRequest(message);
        }

        public static SignOnResponse SignOnResponse(byte[] message = null)
        {
            return new SignOnResponse(message);
        }

        public static SignOffRequest SignOffRequest(byte[] message = null)
        {
            return new SignOffRequest(message);
        }

        public static SignOffResponse SignOffResponse(byte[] message = null)
        {
            return new SignOffResponse(message);
        }

        public static GroupSignOnRequest GroupSignOnRequest(byte[] message = null)
        {
            return new GroupSignOnRequest(message);
        }

        public static GroupSignOnResponse GroupSignOnResponse(byte[] message = null)
        {
            return new GroupSignOnResponse(message);
        }

        public static GroupSignOffRequest GroupSignOffRequest(byte[] message = null)
        {
            return new GroupSignOffRequest(message);
        }

        public static GroupSignOffResponse GroupSignOffResponse(byte[] message = null)
        {
            return new GroupSignOffResponse(message);
        }

        public static PekExchangeRequest PekExchangeRequest(byte[] message = null)
        {
            return new PekExchangeRequest(message);
        }

        public static PekExchangeResponse PekExchangeResponse(byte[] message = null)
        {
            return new PekExchangeResponse(message);
        }

        public static PekExchangeAdvcieRequest PekExchangeAdvcieRequest(byte[] message = null)
        {
            return new PekExchangeAdvcieRequest(message);
        }

        public static AlternativeNetworkRoutingRequest AlternativeNetworkRoutingRequest(byte[] message = null)
        {
            return new AlternativeNetworkRoutingRequest(message);
        }
        public static TransactionHistoryAdviceRequest TransactionHistoryAdviceRequest(byte[] message = null)
        {
            return new TransactionHistoryAdviceRequest(message);
        }

        public static MasterCardRequest MasterCardRequest(byte[] message = null)
        {
            return new MasterCardRequest(message);
        }

        public static MasterCardResponse MasterCardResponse(byte[] message = null)
        {
            return new MasterCardResponse(message);
        }
    }
}
