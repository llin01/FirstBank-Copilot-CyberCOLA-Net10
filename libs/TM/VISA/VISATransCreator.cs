// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="VISATransCreator.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the VISATransCreator type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.VISA
{
    #region DEPENDENCY

    using Com.Cybersoft4u.TM.VISA.Transaction;

    #endregion

    public class VISATransCreator
    {
        static VISATransCreator()
        {
            HeaderBankCode = "655425";
        }

        public static string HeaderBankCode { get; set; }

        public static VISARequest VISARequest(string sourceId = null)
        {
            return (VISARequest)RequestHeaderPreset(new VISARequest(), sourceId);
        }

        public static VISAResponse VISAResponse()
        {
            return new VISAResponse();
        }

        public static SignOnRequest SignOnRequest(string sourceId = null)
        {
            return (SignOnRequest)RequestHeaderPreset(new SignOnRequest(), sourceId);
        }

        public static SignOnResponse SignOnResponse()
        {
            return new SignOnResponse();
        }

        public static SignOffRequest SignOffRequest(string sourceId = null)
        {
            return (SignOffRequest)RequestHeaderPreset(new SignOffRequest(), sourceId);
        }

        public static SignOffResponse SignOffResponse()
        {
            return new SignOffResponse();
        }

        public static StinSignOnRequest StinSignOnRequest(string sourceId = null)
        {
            return (StinSignOnRequest)RequestHeaderPreset(new StinSignOnRequest(), sourceId);
        }

        public static StinSignOnResponse StinSignOnResponse()
        {
            return new StinSignOnResponse();
        }

        public static StinSignOffRequest StinSignOffRequest(string sourceId = null)
        {
            return (StinSignOffRequest)RequestHeaderPreset(new StinSignOffRequest(), sourceId);
        }

        public static StinSignOffResponse StinSignOffResponse()
        {
            return new StinSignOffResponse();
        }

        public static EchoTestRequest EchoTestRequest(string sourceId = null)
        {
            return (EchoTestRequest)RequestHeaderPreset(new EchoTestRequest(), sourceId);
        }

        public static EchoTestResponse EchoTestResponse()
        {
            return new EchoTestResponse();
        }

        private static VISA RequestHeaderPreset(VISA visa, string sourceId = null)
        {
            visa.Header().SourceId = sourceId ?? HeaderBankCode;
            visa.Header().DestinationId = "000000";
            return visa;
        }
    }
}
