namespace Com.Cybersoft4u.TM.ISO8583.TWIN
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.TWIN.Transaction;

    #endregion

    public class TwinTransCreator
    {

        public static TwinSaleRequest TwinSaleRequest(byte[] message = null)
        {
            return new TwinSaleRequest(message);
        }

        public static TwinSaleResponse TwinSaleResponse(byte[] message = null)
        {
            return new TwinSaleResponse(message);
        }

        public static TwinRefundRequest TwinRefundRequest(byte[] message = null)
        {
            return new TwinRefundRequest(message);
        }

        public static TwinRefundResponse TwinRefundResponse(byte[] message = null)
        {
            return new TwinRefundResponse(message);
        }

        public static TwinReversalSaleRequest TwinReversalSaleRequest(byte[] message = null)
        {
            return new TwinReversalSaleRequest(message);
        }

        public static TwinEchoTestRequest TwinEchoTestRequest(byte[] message = null)
        {
            return new TwinEchoTestRequest(message);
        }

        public static TwinEchoTestRequest TwinEchoTestGWRequest(byte[] message = null)
        {
            var requestMessage = new TwinEchoTestRequest(message);
            if (!ObjectUtils.IsNull(requestMessage.RawVal(FIELD.F_11)))
            {
                requestMessage.Core().Field(FIELD.F_11, requestMessage.Core().Field(FIELD.F_11).ToString()?.Remove(0, 1).Insert(0, "9"));
            }
            return requestMessage;
        }

        public static TwinEchoTestResponse TwinEchoTestResponse(byte[] message = null)
        {
            return new TwinEchoTestResponse(message);
        }

        public static TwinSignOnRequest TwinSignOnRequest(byte[] message = null)
        {
            return new TwinSignOnRequest(message);
        }

        public static TwinSignOnRequest TwinSignOnGWRequest(byte[] message = null)
        {
            var requestMessage = new TwinSignOnRequest(message);
            if (!ObjectUtils.IsNull(requestMessage.RawVal(FIELD.F_11)))
            {
                requestMessage.Core().Field(FIELD.F_11, requestMessage.Core().Field(FIELD.F_11).ToString()?.Remove(0, 1).Insert(0, "9"));
            }
            return requestMessage;
        }

        public static TwinSignOnResponse TwinSignOnResponse(byte[] message = null)
        {
            return new TwinSignOnResponse(message);
        }

        public static TwinSignOffRequest TwinSignOffRequest(byte[] message = null)
        {
            return new TwinSignOffRequest(message);
        }

        public static TwinSignOffRequest TwinSignOffGWRequest(byte[] message = null)
        {
            var requestMessage = new TwinSignOffRequest(message);
            if (!ObjectUtils.IsNull(requestMessage.RawVal(FIELD.F_11)))
            {
                requestMessage.Core().Field(FIELD.F_11, requestMessage.Core().Field(FIELD.F_11).ToString()?.Remove(0, 1).Insert(0, "9"));
            }
            return requestMessage;
        }

        public static TwinSignOffResponse TwinSignOffResponse(byte[] message = null)
        {
            return new TwinSignOffResponse(message);
        }

        public static TwinChangeKeyRequest TwinChangeKeyRequest(byte[] message = null)
        {
            return new TwinChangeKeyRequest(message);
        }

        public static TwinRepeatKeyRequest TwinRepeatKeyRequest(byte[] message = null)
        {
            return new TwinRepeatKeyRequest(message);
        }

        public static TwinVerifyKeyRequest TwinVerifyKeyRequest(byte[] message = null)
        {
            return new TwinVerifyKeyRequest(message);
        }

        public static TwinRequest TwinRequest(byte[] message = null)
        {
            return new TwinRequest(message);
        }

        public static TwinResponse TwinResponse(byte[] message = null)
        {
            return new TwinResponse(message);
        }

        public static TwinSignOnRequest TWINQRCodeSignOnRequest(byte[] message = null)
        {
            var requestMessage = new TwinSignOnRequest(message);
            if (!ObjectUtils.IsNull(requestMessage.RawVal(FIELD.F_48)))
            {
                requestMessage.Core().Field(FIELD.F_48, requestMessage.Core().Field(FIELD.F_48).ToString()?.Remove(10, 1).Insert(10, "N"));
            }
            return requestMessage;
        }

        public static TwinSignOnRequest ClientGWSignOnRequest(byte[] message = null)
        {
            var requestMessage = new TwinSignOnRequest(message);
            if (!ObjectUtils.IsNull(requestMessage.RawVal(FIELD.F_11)))
            {
                requestMessage.Core().Field(FIELD.F_11, requestMessage.Core().Field(FIELD.F_11).ToString()?.Remove(0, 1).Insert(0, "9"));
            }
            requestMessage.Purge(FIELD.F_48);
            requestMessage.Header().Decode("NCM000100070");
            return requestMessage;
        }

        public static TwinSignOnResponse ClientGWSignOnResponse(byte[] message = null)
        {
            var responseMessage = new TwinSignOnResponse(message);
            responseMessage.Header().Decode("NCM000100077");
            return responseMessage;
        }

        public static TwinSignOffRequest ClientGWSignOffRequest(byte[] message = null)
        {
            var requestMessage = new TwinSignOffRequest(message);
            if (!ObjectUtils.IsNull(requestMessage.RawVal(FIELD.F_11)))
            {
                requestMessage.Core().Field(FIELD.F_11, requestMessage.Core().Field(FIELD.F_11).ToString()?.Remove(0, 1).Insert(0, "9"));
            }
            requestMessage.Purge(FIELD.F_48);
            requestMessage.Header().Decode("NCM000100070");
            return requestMessage;
        }

        public static TwinSignOffResponse ClientGWSignOffResponse(byte[] message = null)
        {
            var responseMessage = new TwinSignOffResponse(message);
            responseMessage.Header().Decode("NCM000100077");
            return responseMessage;
        }
        public static TwinRequest ClientGWTwinRequest(byte[] message = null)
        {
            var requestMessage = new TwinRequest(message);
            if (!ObjectUtils.IsNull(requestMessage.RawVal(FIELD.F_11)))
            {
                requestMessage.Core().Field(FIELD.F_11, requestMessage.Core().Field(FIELD.F_11).ToString()?.Remove(0, 1).Insert(0, "9"));
            }
            requestMessage.Header().Decode("NCM000100070");
            return requestMessage;
        }

        public static TwinResponse ClientGWTwinResponse(byte[] message = null)
        {
            var responseMessage = new TwinResponse(message);
            responseMessage.Header().Decode("NCM000100077");
            return responseMessage;
        }
        public static TwinEchoTestRequest ClientGWEchoTestRequest(byte[] message = null)
        {
            var requestMessage = new TwinEchoTestRequest(message);
            if (!ObjectUtils.IsNull(requestMessage.RawVal(FIELD.F_11)))
            {
                requestMessage.Core().Field(FIELD.F_11, requestMessage.Core().Field(FIELD.F_11).ToString()?.Remove(0, 1).Insert(0, "9"));
            }
            requestMessage.Header().Decode("NCM000100070");
            return requestMessage;
        }

        public static TwinEchoTestResponse ClientGWEchoTestResponse(byte[] message = null)
        {
            var responseMessage = new TwinEchoTestResponse(message);
            responseMessage.Header().Decode("ISO006000077");
            return responseMessage;
        }
    }
}
