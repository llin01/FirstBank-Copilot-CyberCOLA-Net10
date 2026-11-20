using System;
using System.Text;
using System.Xml.Serialization;

namespace Com.Cybersoft4u.ServiceProvider.Resources
{
    [Serializable]
    [XmlType(TypeName = "ChannelSessionInfo")]
    public class ChannelSessionInfo
    {
        public string LocalEndPoint { get; set; }
        public string RemoteEndPoint { get; set; }
        public DateTime InitialCreateDateTime { get; set; }
        public DateTime CumulativeToDate { get; set; }
        public DateTime NowProcessDateTime { get; set; }
        public byte[] TransactionBytes { get; set; }
        public ulong TotalCount { get; set; }
        public ulong DailyTotalCount { get; set; }

        public ChannelSessionInfo()
        {
            DateTime nowDateTime = DateTime.Now;
            LocalEndPoint = string.Empty;
            RemoteEndPoint = string.Empty;
            InitialCreateDateTime = nowDateTime;
            CumulativeToDate = nowDateTime.Date;
            NowProcessDateTime = default(DateTime);
            TransactionBytes = null;
            TotalCount = 0UL;
            DailyTotalCount = 0UL;
        }
        public ChannelSessionInfo(string localEndPoint, string remoteEndPoint)
        {
            DateTime nowDateTime = DateTime.Now;
            LocalEndPoint = localEndPoint;
            RemoteEndPoint = remoteEndPoint;
            InitialCreateDateTime = nowDateTime;
            CumulativeToDate = nowDateTime.Date;
            NowProcessDateTime = default(DateTime);
            TransactionBytes = null;
            TotalCount = 0UL;
            DailyTotalCount = 0UL;
        }

        public void Update(byte[] transactionBytes)
        {
            DateTime nowDateTime = DateTime.Now;
            var processDate = nowDateTime.Date;
            if (!processDate.Equals(CumulativeToDate))
            {
                CumulativeToDate = processDate;
                DailyTotalCount = 0UL;
            }
            NowProcessDateTime = nowDateTime;
            TransactionBytes = transactionBytes;
            if (TotalCount == UInt64.MaxValue)
                TotalCount = 0UL;
            TotalCount++;
            DailyTotalCount++;
        }
        public string GetChannelInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"LocalEnPoint[{LocalEndPoint}]")
              .Append($"RemoteEnPoint[{RemoteEndPoint}]")
              .Append($"InitialCreateDateTime[{InitialCreateDateTime:yyyy-MM-dd HH:mm:ss:fffff}]")
              .Append($"CumulativeToDate[{CumulativeToDate:yyyy-MM-dd}]")
              .Append($"NowProcessDateTime[{NowProcessDateTime:yyyy-MM-dd HH:mm:ss:fffff}]")
              .Append($"TransactionBytes[{BitConverter.ToString(TransactionBytes).Replace("-", "").ToLower()}]")
              .Append($"TotalCount[{TotalCount}]")
              .Append($"DailyTotalCount[{DailyTotalCount}]");
            return sb.ToString();
        }
    }
}
