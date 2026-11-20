namespace Com.Cybersoft4u.TM.FISC
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.FISC.Transaction;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.CyberUtils.Utils;

    #endregion

    public class FISCTransCreator
    {
        public static FiscAuthorizationRequest FiscAuthorizationRequest(byte[] message = null)
        {
            return new FiscAuthorizationRequest(message);
        }

        public static FiscAuthorizationResponse FiscAuthorizationResponse(byte[] message = null)
        {
            return new FiscAuthorizationResponse(message);
        }

        public static FiscReversalRequest FiscReversalRequest(byte[] message = null)
        {
            return new FiscReversalRequest(message);
        }

        public static FiscReversalResponse FiscReversalResponse(byte[] message = null)
        {
            return new FiscReversalResponse(message);
        }

        public static FiscIssuerFileUpdateRequest FiscIssuerFileUpdateRequest(byte[] message = null)
        {
            return new FiscIssuerFileUpdateRequest(message);
        }

        public static FiscIssuerFileUpdateResponse FiscIssuerFileUpdateResponse(byte[] message = null)
        {
            return new FiscIssuerFileUpdateResponse(message);
        }

        public static FiscSignOnRequest FiscSignOnRequest(byte[] message = null)
        {
            return new FiscSignOnRequest(message);
        }

        public static FiscSignOnRequest FiscSignOnGWRequest(byte[] message = null)
        {
            var requestMessage = new FiscSignOnRequest(message);
            if (!ObjectUtils.IsNull(requestMessage.RawVal(FIELD.F_11)))
            {
                requestMessage.Core().Field(FIELD.F_11, requestMessage.Core().Field(FIELD.F_11).ToString().Remove(0, 1).Insert(0, "9"));
            }
            return requestMessage;
        }

        public static FiscSignOnResponse FiscSignOnResponse(byte[] message = null)
        {
            return new FiscSignOnResponse(message);
        }

        public static FiscSignOffRequest FiscSignOffRequest(byte[] message = null)
        {
            return new FiscSignOffRequest(message);
        }

        public static FiscSignOffRequest FiscSignOffGWRequest(byte[] message = null)
        {
            var requestMessage = new FiscSignOffRequest(message);
            if (!ObjectUtils.IsNull(requestMessage.RawVal(FIELD.F_11)))
            {
                requestMessage.Core().Field(FIELD.F_11, requestMessage.Core().Field(FIELD.F_11).ToString().Remove(0, 1).Insert(0, "9"));
            }
            return requestMessage;
        }

        public static FiscSignOffResponse FiscSignOffResponse(byte[] message = null)
        {
            return new FiscSignOffResponse(message);
        }
        public static FiscEchoTestRequest FiscEchoTestRequest(byte[] message = null)
        {
            return new FiscEchoTestRequest(message);
        }
        public static FiscEchoTestRequest FiscEchoTestGWRequest(byte[] message = null)
        {
            var requestMessage = new FiscEchoTestRequest(message);
            if (!ObjectUtils.IsNull(requestMessage.RawVal(FIELD.F_11)))
            {
                requestMessage.Core().Field(FIELD.F_11, requestMessage.Core().Field(FIELD.F_11).ToString().Remove(0, 1).Insert(0, "9"));
            }
            return requestMessage;
        }
        public static FiscEchoTestResponse FiscEchoTestResponse(byte[] message = null)
        {
            return new FiscEchoTestResponse(message);
        }
        public static FiscRequest FiscRequest(byte[] message = null)
        {
            return new FiscRequest(message);
        }

        public static FiscResponse FiscResponse(byte[] message = null)
        {
            return new FiscResponse(message);
        }
    }
}
