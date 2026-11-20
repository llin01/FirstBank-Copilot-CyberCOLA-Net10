using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.ISO8583.ICASH.Transaction;

namespace Com.Cybersoft4u.TM.ISO8583.ICASH
{
    #region DEPENDENCY

    #endregion

    public class IcashTransCreator
    {
        public static IcashRequest IcashRequest(byte[] message = null)
        {
            return new IcashRequest(message);
        }

        public static IcashResponse IcashResponse(byte[] message = null)
        {
            return new IcashResponse(message);
        }

        public static IcashEchoTestRequest IcashEchoTestRequest(byte[] message = null)
        {
            return new IcashEchoTestRequest(message);
        }

        public static IcashEchoTestRequest IcashEchoTestGWRequest(byte[] message = null)
        {
            var requestMessage = new IcashEchoTestRequest(message);
            if (!ObjectUtils.IsNull(requestMessage.RawVal(FIELD.F_11)))
            {
                requestMessage.Core().Field(FIELD.F_11, requestMessage.Core().Field(FIELD.F_11).ToString()?.Remove(0, 1).Insert(0, "9"));
            }
            return requestMessage;
        }

        public static IcashEchoTestResponse IcashEchoTestResponse(byte[] message = null)
        {
            return new IcashEchoTestResponse(message);
        }

        public static IcashSignOnRequest IcashSignOnRequest(byte[] message = null)
        {
            return new IcashSignOnRequest(message);
        }

        public static IcashSignOnRequest IcashSignOnGWRequest(byte[] message = null)
        {
            var requestMessage = new IcashSignOnRequest(message);
            if (!ObjectUtils.IsNull(requestMessage.RawVal(FIELD.F_11)))
            {
                requestMessage.Core().Field(FIELD.F_11, requestMessage.Core().Field(FIELD.F_11).ToString()?.Remove(0, 1).Insert(0, "9"));
            }
            return requestMessage;
        }
        public static IcashSignOnResponse IcashSignOnResponse(byte[] message = null)
        {
            return new IcashSignOnResponse(message);
        }

        public static IcashSignOffRequest IcashSignOffRequest(byte[] message = null)
        {
            return new IcashSignOffRequest(message);
        }
        public static IcashSignOffRequest IcashSignOffGWRequest(byte[] message = null)
        {
            var requestMessage = new IcashSignOffRequest(message);
            if (!ObjectUtils.IsNull(requestMessage.RawVal(FIELD.F_11)))
            {
                requestMessage.Core().Field(FIELD.F_11, requestMessage.Core().Field(FIELD.F_11).ToString()?.Remove(0, 1).Insert(0, "9"));
            }
            return requestMessage;
        }
        public static IcashSignOffResponse IcashSignOffResponse(byte[] message = null)
        {
            return new IcashSignOffResponse(message);
        }
    }
}
