
using System;
using System.Collections.Generic;
using NLog;

namespace Com.Cybersoft4u.TM.FISC.DE48
{
    public class DE48TagId66
    {

        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private readonly SortedList<string, string> _unPackDe48Tag66DataSortedList;

        public DE48TagId66(string de48Tag66FieldString)
        {
            try
            {
                if (string.IsNullOrEmpty(de48Tag66FieldString))
                {
                    Log.Info("DE48Tag66 Data Null Or Empty");
                    return;
                }

                if (de48Tag66FieldString.Length < 3)
                {
                    Log.Info("DE48Tag66 Data Length Less Than 3");
                    return;
                }
                var de48Tag66TotalLength = de48Tag66FieldString.Length;
                var dataLengthString = de48Tag66FieldString.Substring(0, 2);

                try
                {
                    if (_unPackDe48Tag66DataSortedList == null)
                        _unPackDe48Tag66DataSortedList = new SortedList<string, string>();
                    else
                    {
                        _unPackDe48Tag66DataSortedList.Clear();
                        _unPackDe48Tag66DataSortedList = null;
                        _unPackDe48Tag66DataSortedList = new SortedList<string, string>();
                    }

                    int startPos = 0;
                    int dataLength = 0;
                    int offSet = 0;

                    while (offSet < de48Tag66TotalLength)
                    {
                        dataLength = 2;
                        var tagSubId = de48Tag66FieldString.Substring(startPos, dataLength);
                        startPos += dataLength;
                        offSet += dataLength;
                        //Skip Not Integer
                        if ((tagSubId.Equals("  ")) || tagSubId.Equals("00"))
                            continue;

                        dataLength = 2;
                        var tagSubIdLength = Convert.ToInt32(de48Tag66FieldString.Substring(startPos, dataLength));
                        startPos += dataLength;
                        offSet += dataLength;

                        var tagSubIdData = de48Tag66FieldString.Substring(startPos, tagSubIdLength);
                        startPos += tagSubIdLength;
                        offSet += tagSubIdLength;

                        _unPackDe48Tag66DataSortedList.Add(tagSubId, tagSubIdData);
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

        public bool FindDe48Tag66TlvTagIdExist(string tlvTagId)
        {
            if (_unPackDe48Tag66DataSortedList == null) return (false);
            if (_unPackDe48Tag66DataSortedList.ContainsKey(tlvTagId))
                return (true);
            return (false);
        }

        public string GetDe48Tag66TlvDataByTlvId(string tlvTagId)
        {
            try
            {
                if (!FindDe48Tag66TlvTagIdExist(tlvTagId)) return (null);
                string de48Tag66TlvData = _unPackDe48Tag66DataSortedList[tlvTagId];
                return (de48Tag66TlvData);
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
