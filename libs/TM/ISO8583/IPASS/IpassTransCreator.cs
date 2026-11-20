using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.ISO8583.IPASS.Transaction;

namespace Com.Cybersoft4u.TM.ISO8583.IPASS
{
    #region DEPENDENCY

    #endregion

    public class IpassTransCreator
    {
        public static IpassRequest IpassRequest(byte[] message = null)
        {
            return new IpassRequest(message);
        }

        public static IpassResponse IpassResponse(byte[] message = null)
        {
            return new IpassResponse(message);
        }

        public static IpassEchoTestRequest IpassEchoTestRequest(byte[] message = null)
        {
            return new IpassEchoTestRequest(message);
        }

        public static IpassEchoTestRequest IpassEchoTestGWRequest(byte[] message = null)
        {
            var requestMessage = new IpassEchoTestRequest(message);
            if (!ObjectUtils.IsNull(requestMessage.RawVal(FIELD.F_11)))
            {
                requestMessage.Core().Field(FIELD.F_11, requestMessage.Core().Field(FIELD.F_11).ToString()?.Remove(0, 1).Insert(0, "9"));
            }
            return requestMessage;
        }

        public static IpassEchoTestResponse IpassEchoTestResponse(byte[] message = null)
        {
            return new IpassEchoTestResponse(message);
        }

        public static IpassSignOnRequest IpassSignOnRequest(byte[] message = null)
        {
            return new IpassSignOnRequest(message);
        }

        public static IpassSignOnRequest IpassSignOnGWRequest(byte[] message = null)
        {
            var requestMessage = new IpassSignOnRequest(message);
            if (!ObjectUtils.IsNull(requestMessage.RawVal(FIELD.F_11)))
            {
                requestMessage.Core().Field(FIELD.F_11, requestMessage.Core().Field(FIELD.F_11).ToString()?.Remove(0, 1).Insert(0, "9"));
            }
            return requestMessage;
        }
        public static IpassSignOnResponse IpassSignOnResponse(byte[] message = null)
        {
            return new IpassSignOnResponse(message);
        }

        public static IpassSignOffRequest IpassSignOffRequest(byte[] message = null)
        {
            return new IpassSignOffRequest(message);
        }
        public static IpassSignOffRequest IpassSignOffGWRequest(byte[] message = null)
        {
            var requestMessage = new IpassSignOffRequest(message);
            if (!ObjectUtils.IsNull(requestMessage.RawVal(FIELD.F_11)))
            {
                requestMessage.Core().Field(FIELD.F_11, requestMessage.Core().Field(FIELD.F_11).ToString()?.Remove(0, 1).Insert(0, "9"));
            }
            return requestMessage;
        }
        public static IpassSignOffResponse IpassSignOffResponse(byte[] message = null)
        {
            return new IpassSignOffResponse(message);
        }
    }
}
