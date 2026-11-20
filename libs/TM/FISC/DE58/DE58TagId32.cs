
using System;
using NLog;

namespace Com.Cybersoft4u.TM.FISC.DE58
{
    public class DE58TagId32
    {

        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        public DE58TagId32(string tagIdString)
        {
            try
            {
                GetCardNbr = string.Empty;
                GetExpirDte = string.Empty;
                GetSvc = string.Empty;
                GetPki = string.Empty;
                GetPvv = string.Empty;
                GetCvv = string.Empty;
                GetTspId = string.Empty;
                GetAcValidationResult = string.Empty;
                GetFraudCheckResult = string.Empty;
                GetReasonCode = string.Empty;
                if (string.IsNullOrEmpty(tagIdString))
                {
                    Log.Info("TagId Data Null Or Empty");
                    return;
                }

                if (tagIdString.Length < 2)
                {
                    Log.Info("TagId Data Length Less Than 2");
                    return;
                }

                string dataLengthString = tagIdString.Substring(0, 2);
                bool result = int.TryParse(dataLengthString, out var dataLength);
                if (!result)
                {
                    Log.Info("TagId 32 track data Data Length Invalid");
                    return;
                }
                const int fixedDataLength = (11 + 2 + 2 + 2); //TSP ID (11) + AC Validation Result (2) + Fraud check Result (2) + Reason code (2)
                const int fixedTrackIIlength = 37;
                if (tagIdString.Length != (2 + fixedTrackIIlength + fixedDataLength))
                {
                    Log.Info("TagId Data Length No Match[{0}],[{1}]", tagIdString.Length, (2 + dataLength + fixedDataLength));
                    return;
                }

                string track2Data = tagIdString.Substring(2, dataLength);
                int track2DataLength = track2Data.Length;
                int startIndex = track2Data.IndexOfAny(new char[] { '=', 'D', '~', '^' });
                if (startIndex > 0)
                {
                    GetCardNbr = track2Data.Substring(0, startIndex);
                    startIndex++;
                    dataLength = 4;
                    if ((startIndex + dataLength) <= track2DataLength)
                        GetExpirDte = track2Data.Substring(startIndex, dataLength);

                    startIndex += dataLength;
                    dataLength = 3;
                    if ((startIndex + dataLength) <= track2DataLength)
                        GetSvc = track2Data.Substring(startIndex, dataLength);

                    startIndex += dataLength;
                    dataLength = 1;
                    if ((startIndex + dataLength) <= track2DataLength)
                        GetPki = track2Data.Substring(startIndex, dataLength);

                    startIndex += dataLength;
                    dataLength = 4;
                    if ((startIndex + dataLength) <= track2DataLength)
                        GetPvv = track2Data.Substring(startIndex, dataLength);

                    startIndex += dataLength;
                    dataLength = 3;
                    if ((startIndex + dataLength) <= track2DataLength)
                        GetCvv = track2Data.Substring(startIndex, dataLength);
                }
                int startPosition = (2 + fixedTrackIIlength);
                int copyDataLength = 11;
                GetTspId = tagIdString.Substring(startPosition, copyDataLength);
                startPosition += copyDataLength;
                copyDataLength = 2;
                GetAcValidationResult = tagIdString.Substring(startPosition, copyDataLength);
                startPosition += copyDataLength;
                copyDataLength = 2;
                GetFraudCheckResult = tagIdString.Substring(startPosition, copyDataLength);
                startPosition += copyDataLength;
                copyDataLength = 2;
                GetReasonCode = tagIdString.Substring(startPosition, copyDataLength); ;
                return;
            }
            catch (Exception ex)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]", (new System.Diagnostics.StackTrace(true)).GetFrame(0).GetMethod().Name, "Unpack DE58TagId32 Error", String.Format("ISOTrack Constructor(Message) Exception:{0}", ex.Message));
            }
        }

        public string GetCardNbr { get; }

        public string GetExpirDte { get; }

        public string GetSvc { get; }

        public string GetPki { get; }

        public string GetPvv { get; }

        public string GetCvv { get; }

        public string GetTspId { get; }

        public string GetAcValidationResult { get; }

        public string GetFraudCheckResult { get; }

        public string GetReasonCode { get; }
    }
}
