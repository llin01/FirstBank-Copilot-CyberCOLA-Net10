using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.CyberUtils;
using NLog;
using System.Text;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using Com.Cybersoft4u.CyberUtils.Utils;
using System;

namespace Com.Cybersoft4u.CommonLib.Resource
{
    public static class Iso8583Logger
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Logs all fields of an ISO8583 message in a formatted list.
        /// </summary>
        /// <param name="message">The ISO8583 message (ncccReq or ncccRsp)</param>
        /// <param name="title">Custom title for the log block (e.g., "ncccReq Data")</param>
        public static void LogFields(NcccIso8583 message, string title = "ISO8583 Data")
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"\n=== {title} ===");
                sb.AppendLine($"Header[{message.Header().Encode()}]");

                // Get Bitmap as Hex string
                string bitmapHex = "";
                try
                {
                    bitmapHex = HexStringConverter.FromByteArray(message.Bitmap());
                }
                catch { bitmapHex = "Error encoding bitmap"; }

                sb.AppendLine($"BitMap[{bitmapHex}]");
                sb.AppendLine($"MTI [{message.Mti()}]");

                foreach (var pair in message.Core().OrderedFields())
                {
                    var key = pair.Key;
                    var value = pair.Value;

                    if (value is byte[] bytes)
                    {
                        var tempString = HexStringConverter.FromByteArray(bytes);
                        sb.AppendLine($"FIELD [{key}] VALUE [{tempString}] (HEX)");
                    }
                    else
                    {
                        // Mimic the sample format: FIELD [Key] Field Position [Int] VALUE [String]
                        int fieldPos = FIELDMethods.Val(key);
                        sb.AppendLine($"FIELD [{key}] Field Position [{fieldPos}] VALUE [{value}]");
                    }
                }
                sb.AppendLine("==============================\n");

                Logger.Info(sb.ToString());
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Error logging ISO8583 fields for {title}");
            }
        }
    }
}
