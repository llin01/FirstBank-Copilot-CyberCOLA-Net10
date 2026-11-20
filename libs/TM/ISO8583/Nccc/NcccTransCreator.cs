namespace Com.Cybersoft4u.TM.ISO8583.Nccc
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.Nccc.Transaction;

    #endregion

    public class NcccTransCreator
    {

        public static SaleRequest SaleRequest(byte[] message = null)
        {
            return new SaleRequest(message);
        }

        public static SaleResponse SaleResponse(byte[] message = null)
        {
            return new SaleResponse(message);
        }

        public static RefundRequest RefundRequest(byte[] message = null)
        {
            return new RefundRequest(message);
        }

        public static RefundResponse RefundResponse(byte[] message = null)
        {
            return new RefundResponse(message);
        }

        public static ReversalSaleRequest ReversalSaleRequest(byte[] message = null)
        {
            return new ReversalSaleRequest(message);
        }

        public static EchoTestRequest EchoTestRequest(byte[] message = null)
        {
            return new EchoTestRequest(message);
        }

        public static EchoTestRequest EchoTestGWRequest(byte[] message = null)
        {
            var requestMessage = new EchoTestRequest(message);
            if (!ObjectUtils.IsNull(requestMessage.RawVal(FIELD.F_11)))
            {
                requestMessage.Core().Field(FIELD.F_11, requestMessage.Core().Field(FIELD.F_11).ToString()?.Remove(0, 1).Insert(0, "9"));
            }
            return requestMessage;
        }

        public static EchoTestResponse EchoTestResponse(byte[] message = null)
        {
            return new EchoTestResponse(message);
        }

        public static SignOnRequest SignOnRequest(byte[] message = null)
        {
            return new SignOnRequest(message);
        }

        public static SignOnRequest SignOnGWRequest(byte[] message = null)
        {
            var requestMessage = new SignOnRequest(message);
            if (!ObjectUtils.IsNull(requestMessage.RawVal(FIELD.F_11)))
            {
                requestMessage.Core().Field(FIELD.F_11, requestMessage.Core().Field(FIELD.F_11).ToString()?.Remove(0, 1).Insert(0, "9"));
            }
            return requestMessage;
        }

        public static SignOnResponse SignOnResponse(byte[] message = null)
        {
            return new SignOnResponse(message);
        }

        public static SignOffRequest SignOffRequest(byte[] message = null)
        {
            return new SignOffRequest(message);
        }

        public static SignOffRequest SignOffGWRequest(byte[] message = null)
        {
            var requestMessage = new SignOffRequest(message);
            if (!ObjectUtils.IsNull(requestMessage.RawVal(FIELD.F_11)))
            {
                requestMessage.Core().Field(FIELD.F_11, requestMessage.Core().Field(FIELD.F_11).ToString()?.Remove(0, 1).Insert(0, "9"));
            }
            return requestMessage;
        }

        public static SignOffResponse SignOffResponse(byte[] message = null)
        {
            return new SignOffResponse(message);
        }

        public static ChangeKeyRequest ChangeKeyRequest(byte[] message = null)
        {
            return new ChangeKeyRequest(message);
        }

        public static RepeatKeyRequest RepeatKeyRequest(byte[] message = null)
        {
            return new RepeatKeyRequest(message);
        }

        public static VerifyKeyRequest VerifyKeyRequest(byte[] message = null)
        {
            return new VerifyKeyRequest(message);
        }

        public static NcccRequest NcccRequest(byte[] message = null)
        {
            return new NcccRequest(message);
        }

        public static NcccResponse NcccResponse(byte[] message = null)
        {
            return new NcccResponse(message);
        }

        public static SignOnRequest NCCCQRCodeSignOnRequest(byte[] message = null)
        {
            var requestMessage = new SignOnRequest(message);
            if (!ObjectUtils.IsNull(requestMessage.RawVal(FIELD.F_48)))
            {
                requestMessage.Core().Field(FIELD.F_48, requestMessage.Core().Field(FIELD.F_48).ToString()?.Remove(10, 1).Insert(10, "N"));
            }
            return requestMessage;
        }

        public static SignOnRequest ClientGWSignOnRequest(byte[] message = null)
        {
            var requestMessage = new SignOnRequest(message);
            if (!ObjectUtils.IsNull(requestMessage.RawVal(FIELD.F_11)))
            {
                requestMessage.Core().Field(FIELD.F_11, requestMessage.Core().Field(FIELD.F_11).ToString()?.Remove(0, 1).Insert(0, "9"));
            }
            requestMessage.Purge(FIELD.F_48);
            requestMessage.Header().Decode("ISO006000070");
            return requestMessage;
        }

        public static SignOnResponse ClientGWSignOnResponse(byte[] message = null)
        {
            var responseMessage = new SignOnResponse(message);
            responseMessage.Header().Decode("ISO006000077");
            return responseMessage;
        }

        public static SignOffRequest ClientGWSignOffRequest(byte[] message = null)
        {
            var requestMessage = new SignOffRequest(message);
            if (!ObjectUtils.IsNull(requestMessage.RawVal(FIELD.F_11)))
            {
                requestMessage.Core().Field(FIELD.F_11, requestMessage.Core().Field(FIELD.F_11).ToString()?.Remove(0, 1).Insert(0, "9"));
            }
            requestMessage.Purge(FIELD.F_48);
            requestMessage.Header().Decode("ISO006000070");
            return requestMessage;
        }

        public static SignOffResponse ClientGWSignOffResponse(byte[] message = null)
        {
            var responseMessage = new SignOffResponse(message);
            responseMessage.Header().Decode("ISO006000077");
            return responseMessage;
        }
        public static NcccRequest ClientGWNcccRequest(byte[] message = null)
        {
            var requestMessage = new NcccRequest(message);
            if (!ObjectUtils.IsNull(requestMessage.RawVal(FIELD.F_11)))
            {
                requestMessage.Core().Field(FIELD.F_11, requestMessage.Core().Field(FIELD.F_11).ToString()?.Remove(0, 1).Insert(0, "9"));
            }
            requestMessage.Header().Decode("ISO026000070");
            return requestMessage;
        }

        public static NcccResponse ClientGWNcccResponse(byte[] message = null)
        {
            var responseMessage = new NcccResponse(message);
            responseMessage.Header().Decode("ISO026000077");
            return responseMessage;
        }
        public static EchoTestRequest ClientGWEchoTestRequest(byte[] message = null)
        {
            var requestMessage = new EchoTestRequest(message);
            if (!ObjectUtils.IsNull(requestMessage.RawVal(FIELD.F_11)))
            {
                requestMessage.Core().Field(FIELD.F_11, requestMessage.Core().Field(FIELD.F_11).ToString()?.Remove(0, 1).Insert(0, "9"));
            }
            requestMessage.Header().Decode("ISO006000070");
            return requestMessage;
        }

        public static EchoTestResponse ClientGWEchoTestResponse(byte[] message = null)
        {
            var responseMessage = new EchoTestResponse(message);
            responseMessage.Header().Decode("ISO006000077");
            return responseMessage;
        }
    }
}
