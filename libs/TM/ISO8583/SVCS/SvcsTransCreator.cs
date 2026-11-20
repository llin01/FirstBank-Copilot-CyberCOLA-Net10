using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.ISO8583.SVCS.Transaction;

namespace Com.Cybersoft4u.TM.ISO8583.SVCS
{
    #region DEPENDENCY

    #endregion

    public class SvcsTransCreator
    {
        public static SvcsRequest SvcsRequest(byte[] message = null)
        {
            return new SvcsRequest(message);
        }

        public static SvcsResponse SvcsResponse(byte[] message = null)
        {
            return new SvcsResponse(message);
        }

        public static SvcsEchoTestRequest SvcsEchoTestRequest(byte[] message = null)
        {
            return new SvcsEchoTestRequest(message);
        }

        public static SvcsEchoTestRequest SvcsEchoTestGWRequest(byte[] message = null)
        {
            var requestMessage = new SvcsEchoTestRequest(message);
            if (!ObjectUtils.IsNull(requestMessage.RawVal(FIELD.F_11)))
            {
                requestMessage.Core().Field(FIELD.F_11, requestMessage.Core().Field(FIELD.F_11).ToString()?.Remove(0, 1).Insert(0, "9"));
            }
            return requestMessage;
        }

        public static SvcsEchoTestResponse SvcsEchoTestResponse(byte[] message = null)
        {
            return new SvcsEchoTestResponse(message);
        }

        public static SvcsSignOnRequest SvcsSignOnRequest(byte[] message = null)
        {
            return new SvcsSignOnRequest(message);
        }

        public static SvcsSignOnRequest SvcsSignOnGWRequest(byte[] message = null)
        {
            var requestMessage = new SvcsSignOnRequest(message);
            if (!ObjectUtils.IsNull(requestMessage.RawVal(FIELD.F_11)))
            {
                requestMessage.Core().Field(FIELD.F_11, requestMessage.Core().Field(FIELD.F_11).ToString()?.Remove(0, 1).Insert(0, "9"));
            }
            return requestMessage;
        }
        public static SvcsSignOnResponse SvcsSignOnResponse(byte[] message = null)
        {
            return new SvcsSignOnResponse(message);
        }

        public static SvcsSignOffRequest SvcsSignOffRequest(byte[] message = null)
        {
            return new SvcsSignOffRequest(message);
        }
        public static SvcsSignOffRequest SvcsSignOffGWRequest(byte[] message = null)
        {
            var requestMessage = new SvcsSignOffRequest(message);
            if (!ObjectUtils.IsNull(requestMessage.RawVal(FIELD.F_11)))
            {
                requestMessage.Core().Field(FIELD.F_11, requestMessage.Core().Field(FIELD.F_11).ToString()?.Remove(0, 1).Insert(0, "9"));
            }
            return requestMessage;
        }
        public static SvcsSignOffResponse SvcsSignOffResponse(byte[] message = null)
        {
            return new SvcsSignOffResponse(message);
        }
    }
}
