
using System;
using System.Text;

namespace Com.Cybersoft4u.TM.ISO8583.TWIN
{
    #region DEPENDENCY

    using System.Collections.Generic;
    using Com.Cybersoft4u.Utils;

    #endregion

    public class TWINTLV
    {
        public Dictionary<string, string> TLV = new Dictionary<string, string>();

        public TWINTLV Decode(string data)
        {
            try
            {
                var startPointer = 0;
                var totalDataLength = data.Length;
                while (startPointer < totalDataLength)
                {
                    var offsetLength = 2;
                    string id = data.Substring(startPointer, offsetLength);
                    startPointer += offsetLength;

                    offsetLength = 2;
                    int dataLength = Convert.ToInt16(data.Substring(startPointer, offsetLength));
                    startPointer += offsetLength;

                    offsetLength = dataLength;
                    string idData = data.Substring(startPointer, offsetLength);
                    startPointer += offsetLength;
                    this.TLV[id] = idData;
                }
                return this;
            }
            catch (System.Exception ex)
            {
                LoggerAdapter.GetLogger(this.GetType().FullName).Warn($"TWINTLV Decode Exception : {ex.Message}");
                return null;
            }
        }

        public string Encode()
        {
            var sb = new StringBuilder();
            foreach (KeyValuePair<string, string> entry in TLV)
            {
                sb.Append($"{entry.Key}{entry.Value.Length:00}{entry.Value}");
            }
            return sb.ToString();
        }

        public Dictionary<string, string> GetTLV()
        {
            return TLV;
        }

        public string GetIdValue(string id)
        {
            if (TLV == null || TLV.Count == 0) return null;
            return TLV.TryGetValue(id, out var value) ? value : null;
        }

        public void ShowTags()
        {
            foreach (var tag in this.TLV)
            {
                LoggerAdapter.GetLogger(this.GetType().FullName).Info($"[{tag.Key}] = {tag.Value}");
            }
        }
    }
}
