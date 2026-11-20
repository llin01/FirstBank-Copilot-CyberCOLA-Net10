namespace Com.Cybersoft4u.TM.ISO8583.MCOLA
{
    #region DEPENDENCY
    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.MCOLA.Transaction;

    #endregion

    public class MCOLATransCreator
    {
        public static SaleRequest SaleRequest()
        {
            return new SaleRequest();
        }

        public static SaleResponse SaleResponse(byte[] message = null)
        {
            return new SaleResponse(message);
        }

        public static PreAuthRequest PreAuthRequest()
        {
            return new PreAuthRequest();
        }

        public static PreAuthResponse PreAuthResponse()
        {
            return new PreAuthResponse();
        }

        public static PreAuthCompleteRequest PreAuthCompleteRequest()
        {
            return new PreAuthCompleteRequest();
        }

        public static PreAuthCompleteResponse PreAuthCompleteResponse()
        {
            return new PreAuthCompleteResponse();
        }

        public static SaleAdviceRequest SaleAdviceRequest()
        {
            return new SaleAdviceRequest();
        }

        public static SaleAdviceResponse SaleAdviceResponse()
        {
            return new SaleAdviceResponse();
        }

        public static AuthorizationResponseNegAck AuthorizationResponseNegAck()
        {
            return new AuthorizationResponseNegAck();
        }

        public static FileUpdateRequest FileUpdateRequest(byte[] message = null)
        {
            return new FileUpdateRequest(message);
        }

        public static FileUpdateResponse FileUpdateResponse()
        {
            return new FileUpdateResponse();
        }

        public static MCOLAPaymentTransactionRequest PaymentTransactionRequest(byte[] message = null)
        {
            return new MCOLAPaymentTransactionRequest(message);
        }

        public static MCOLAPaymentTransactionResponse PaymentTransactionResponse()
        {
            return new MCOLAPaymentTransactionResponse();
        }

        public static ReversalRequest ReversalRequest()
        {
            return new ReversalRequest();
        }

        public static ReversalResponse ReversalResponse()
        {
            return new ReversalResponse();
        }

        public static ReversalAdviceRequest ReversalAdviceRequest()
        {
            return new ReversalAdviceRequest();
        }

        public static AdministrativeRequest AdministrativeRequest()
        {
            return new AdministrativeRequest();
        }

        public static AdministrativeResponse AdministrativeResponse()
        {
            return new AdministrativeResponse();
        }

        public static AdministrativeAdviceRequest AdministrativeAdviceRequest()
        {
            return new AdministrativeAdviceRequest();
        }

        public static AdministrativeAdviceResponse AdministrativeAdviceResponse()
        {
            return new AdministrativeAdviceResponse();
        }

        public static SignOnRequest SignOnRequest()
        {
            return new SignOnRequest();
        }

        public static SignOnRequest SignOnGWRequest()
        {
            var requestMessage = new SignOnRequest();
            requestMessage.Encode();
            if (!ObjectUtils.IsNull(requestMessage.RawVal(FIELD.F_11)))
            {
                requestMessage.Core().Field(FIELD.F_11, requestMessage.Core().Field(FIELD.F_11).ToString().Remove(0, 1).Insert(0, "9"));
            }
            return requestMessage;
        }

        public static SignOnRequest SignOnRequest(string src, string dst)
        {
            var requestMessage = new SignOnRequest { Src = src, Dst = dst };
            return requestMessage;
        }

        public static SignOnRequest SignOnGWRequest(string src, string dst)
        {
            var requestMessage = new SignOnRequest { Src = src, Dst = dst };
            requestMessage.Encode();
            if (!ObjectUtils.IsNull(requestMessage.RawVal(FIELD.F_11)))
            {
                requestMessage.Core().Field(FIELD.F_11, requestMessage.Core().Field(FIELD.F_11).ToString().Remove(0, 1).Insert(0, "9"));
            }
            return requestMessage;
        }
        public static SignOnResponse SignOnResponse()
        {
            return new SignOnResponse();
        }

        public static SignOnResponse SignOnResponse(string src, string dst)
        {
            var responseMessage = new SignOnResponse { Src = src, Dst = dst };
            return responseMessage;
        }

        public static SignOffRequest SignOffRequest()
        {
            return new SignOffRequest();
        }

        public static SignOffRequest SignOffGWRequest()
        {
            var requestMessage = new SignOffRequest();
            requestMessage.Encode();
            if (!ObjectUtils.IsNull(requestMessage.RawVal(FIELD.F_11)))
            {
                requestMessage.Core().Field(FIELD.F_11, requestMessage.Core().Field(FIELD.F_11).ToString().Remove(0, 1).Insert(0, "9"));
            }
            return requestMessage;
        }

        public static SignOffRequest SignOffRequest(string src, string dst)
        {
            var requestMessage = new SignOffRequest { Src = src, Dst = dst };
            return requestMessage;
        }

        public static SignOffRequest SignOffGWRequest(string src, string dst)
        {
            var requestMessage = new SignOffRequest { Src = src, Dst = dst };
            requestMessage.Encode();
            if (!ObjectUtils.IsNull(requestMessage.RawVal(FIELD.F_11)))
            {
                requestMessage.Core().Field(FIELD.F_11, requestMessage.Core().Field(FIELD.F_11).ToString().Remove(0, 1).Insert(0, "9"));
            }
            return requestMessage;
        }

        public static SignOffResponse SignOffResponse()
        {
            return new SignOffResponse();
        }

        public static SignOffResponse SignOffResponse(string src, string dst)
        {
            var responseMessage = new SignOffResponse { Src = src, Dst = dst };
            return responseMessage;
        }

        public static EchoTestRequest EchoTestRequest()
        {
            return new EchoTestRequest();
        }

        public static EchoTestRequest EchoTestGWRequest()
        {
            var requestMessage = new EchoTestRequest();
            requestMessage.Encode();
            if (!ObjectUtils.IsNull(requestMessage.RawVal(FIELD.F_11)))
            {
                requestMessage.Core().Field(FIELD.F_11, requestMessage.Core().Field(FIELD.F_11).ToString().Remove(0, 1).Insert(0, "9"));
            }
            return requestMessage;
        }

        public static EchoTestRequest EchoTestRequest(string src, string dst)
        {
            var requestMessage = new EchoTestRequest { Src = src, Dst = dst };
            return requestMessage;
        }

        public static EchoTestRequest EchoTestGWRequest(string src, string dst)
        {
            var requestMessage = new EchoTestRequest { Src = src, Dst = dst };
            requestMessage.Encode();
            if (!ObjectUtils.IsNull(requestMessage.RawVal(FIELD.F_11)))
            {
                requestMessage.Core().Field(FIELD.F_11, requestMessage.Core().Field(FIELD.F_11).ToString().Remove(0, 1).Insert(0, "9"));
            }
            return requestMessage;
        }

        public static EchoTestResponse EchoTestResponse()
        {
            return new EchoTestResponse();
        }

        public static EchoTestResponse EchoTestResponse(string src, string dst)
        {
            var responseMessage = new EchoTestResponse { Src = src, Dst = dst };
            return responseMessage;
        }

        public static ChangeKeyRequest ChangeKeyRequest(byte[] message = null)
        {
            return new ChangeKeyRequest(message);
        }

        public static ChangeKeyResponse ChangeKeyResponse()
        {
            return new ChangeKeyResponse();
        }

        public static ChangeKeyAdviceRequest ChangeKeyAdviceRequest()
        {
            return new ChangeKeyAdviceRequest();
        }

        public static AlternativeNetworkRoutingResponse AlternativeNetworkRoutingResponse(byte[] message = null)
        {
            return new AlternativeNetworkRoutingResponse(message);
        }

        public static TransactionHistoryAdviceResponse TransactionHistoryAdviceResponse(byte[] message = null)
        {
            return new TransactionHistoryAdviceResponse(message);
        }

        public static MCOLARequest MCOLARequest(byte[] message = null)
        {
            return new MCOLARequest(message);
        }

        public static MCOLAResponse MCOLAResponse(byte[] message = null)
        {
            return new MCOLAResponse(message);
        }
    }
}
