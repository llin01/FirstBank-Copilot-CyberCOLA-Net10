using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using NLog;
using System;

namespace Com.Cybersoft4u.CommonLib.NcccMessage
{
    public class BicTrack2
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        public BicTrack2(string track2Data)
        {
            IsoCardNbr = string.Empty;
            IsoExpirDte = string.Empty;
            IsoSvc = string.Empty;
            IsoPki = string.Empty;
            IsoPvv = string.Empty;
            IsoCvv = string.Empty;
            IsoCardType = CreditCardType.Unknown;
            if (string.IsNullOrEmpty(track2Data))
                return;
            try
            {
                int delimiterPos = track2Data.IndexOfAny(SystemConstantData.CommonData.CardDelimiterChars);
                if (delimiterPos < 0)
                {
                    Log.Info("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                             new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                             "TrackII Delimiter Not Found",
                             "TrackII Delimiter Not Found");
                    IsoCardNbr = track2Data;
                    IsoCardType = CreditCardUtility.FindCardType(IsoCardNbr);
                    return;
                }
                IsoCardNbr = track2Data.Substring(0, delimiterPos);
                IsoCardType = CreditCardUtility.FindCardType(IsoCardNbr);

                int startIndex = track2Data.IndexOfAny(SystemConstantData.CommonData.CardDelimiterChars);
                if (startIndex < 0)
                {
                    Log.Info("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                             new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                             "TrackII Delimiter Not Found",
                             startIndex);

                    return;
                }
                startIndex++;
                int dataLength = 4;
                if (track2Data.Length < (startIndex + dataLength))
                {
                    Log.Info("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                             new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                             "TrackII At : " + (startIndex + dataLength),
                             "TrackII Length No Match");

                    return;
                }
                IsoExpirDte = track2Data.Substring(startIndex, dataLength);

                startIndex += dataLength;
                //Check TrackII Data Length
                if (track2Data.Length == startIndex)
                {
                    Log.Info("Method:[{0}] -- Description[{1}]",
                             new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                             "TrackII Data Stop At Expired Date");

                    return;
                }
                dataLength = 3;
                if (track2Data.Length < (startIndex + dataLength))
                {
                    Log.Info("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                             new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                             "TrackII At : " + (startIndex + dataLength),
                             "TrackII Length No Match");

                    return;
                }
                IsoSvc = track2Data.Substring(startIndex, dataLength);

                startIndex += dataLength;
                //Check TrackII Data Length
                if (track2Data.Length == startIndex)
                {
                    Log.Info("Method:[{0}] -- Description[{1}]",
                             new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                             "TrackII Data Stop At SVCe");

                    return;
                }
                dataLength = 1;
                if (track2Data.Length < (startIndex + dataLength))
                {
                    Log.Info("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                             new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                             "TrackII At : " + (startIndex + dataLength),
                             "TrackII Length No Match");

                    return;
                }
                IsoPki = track2Data.Substring(startIndex, dataLength);

                startIndex += dataLength;
                //Check TrackII Data Length
                if (track2Data.Length == startIndex)
                {
                    Log.Info("Method:[{0}] -- Description[{1}]",
                             new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                             "TrackII Data Stop At PKI");

                    return;
                }
                dataLength = 4;
                if (track2Data.Length < (startIndex + dataLength))
                {
                    Log.Info("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                             new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                             "TrackII At : " + (startIndex + dataLength),
                             "TrackII Length No Match");
                    return;
                }
                IsoPvv = track2Data.Substring(startIndex, dataLength);

                startIndex += dataLength;
                //Check TrackII Data Length
                if (track2Data.Length == startIndex)
                {
                    Log.Info("Method:[{0}] -- Description[{1}]",
                             new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                             "TrackII Data Stop At PVV");

                    return;
                }
                dataLength = 3;
                if (track2Data.Length < (startIndex + dataLength))
                {
                    Log.Info("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                             new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                             "TrackII At : " + (startIndex + dataLength),
                             "TrackII Length No Match");

                    return;
                }
                IsoCvv = track2Data.Substring(startIndex, dataLength);
                return;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"BicTrack2 Exception:{ex.Message}");
            }
        }

        public string IsoCardNbr { get; }

        public string IsoExpirDte { get; }

        public string IsoSvc { get; }

        public string IsoPki { get; }

        public string IsoPvv { get; }

        public string IsoCvv { get; }

        public CreditCardType IsoCardType { get; }
    }
}
