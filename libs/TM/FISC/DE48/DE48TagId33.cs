
using System;
using System.Collections.Generic;
using NLog;

namespace Com.Cybersoft4u.TM.FISC.DE48
{
    public class DE48TagId33
    {

        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private readonly SortedList<string, string> _unPackDe48Tag33DataSortedList;

        public DE48TagId33(string de48Tag33FieldString)
        {
            try
            {
                if (string.IsNullOrEmpty(de48Tag33FieldString))
                {
                    Log.Info("DE48Tag33 Data Null Or Empty");
                    return;
                }

                if (de48Tag33FieldString.Length < 3)
                {
                    Log.Info("DE48Tag33 Data Length Less Than 3");
                    return;
                }
                var de48Tag33TotoalLength = de48Tag33FieldString.Length;
                var dataLengthString = de48Tag33FieldString.Substring(0, 2);

                try
                {
                    if (_unPackDe48Tag33DataSortedList == null)
                        _unPackDe48Tag33DataSortedList = new SortedList<string, string>();
                    else
                    {
                        _unPackDe48Tag33DataSortedList.Clear();
                        _unPackDe48Tag33DataSortedList = null;
                        _unPackDe48Tag33DataSortedList = new SortedList<string, string>();
                    }

                    int startPos = 0;
                    int dataLength = 0;
                    int offSet = 0;

                    while (offSet < de48Tag33TotoalLength)
                    {
                        dataLength = 2;
                        var tagSubId = de48Tag33FieldString.Substring(startPos, dataLength);
                        startPos += dataLength;
                        offSet += dataLength;
                        //Skip Not Integer
                        if ((tagSubId.Equals("  ")) || tagSubId.Equals("00"))
                            continue;

                        dataLength = 2;
                        var tagSubIdLength = Convert.ToInt32(de48Tag33FieldString.Substring(startPos, dataLength));
                        startPos += dataLength;
                        offSet += dataLength;

                        var tagSubIdData = de48Tag33FieldString.Substring(startPos, tagSubIdLength);
                        startPos += tagSubIdLength;
                        offSet += tagSubIdLength;

                        _unPackDe48Tag33DataSortedList.Add(tagSubId, tagSubIdData);
                    }
                }
                catch (Exception ex)
                {
                    Log.Fatal(String.Format("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                        new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                        "Unpack Tag Field Error",
                        String.Format("Tag Constructor(Message) Exception:{0}", ex.Message)));

                    throw new Exception(String.Format("Tag Constructor(Message) Exception:{0}", ex.Message));
                }
                return;
            }
            catch (Exception ex)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]", new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name, "Unpack DE58TagId32 Error", String.Format("ISOTrack Constructor(Message) Exception:{0}", ex.Message));
            }
        }

        public bool FindDe48Tag33TlvTagIdExist(string tlvTagId)
        {
            if (_unPackDe48Tag33DataSortedList == null) return (false);
            if (_unPackDe48Tag33DataSortedList.ContainsKey(tlvTagId))
                return (true);
            return (false);
        }

        public string GetDe48Tag33TlvDataByTlvId(string tlvTagId)
        {
            try
            {
                if (!FindDe48Tag33TlvTagIdExist(tlvTagId)) return (null);
                string de48Tag33TlvData = _unPackDe48Tag33DataSortedList[tlvTagId];
                return (de48Tag33TlvData);
            }
            catch (Exception ex)
            {
                Log.Fatal(String.Format("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                    new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                    "TlvTagId:" + tlvTagId,
                    ex.Message));

                return (null);
            }
        }
    }
}
