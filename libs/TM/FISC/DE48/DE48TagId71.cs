
using System;
using System.Collections.Generic;
using NLog;

namespace Com.Cybersoft4u.TM.FISC.DE48
{
    public class DE48TagId71
    {

        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private readonly SortedList<string, string> _unPackDe48Tag71DataSortedList;

        public DE48TagId71(string de48Tag71FieldString)
        {
            try
            {
                if (string.IsNullOrEmpty(de48Tag71FieldString))
                {
                    Log.Info("DE48Tag71 Data Null Or Empty");
                    return;
                }

                if (de48Tag71FieldString.Length < 3)
                {
                    Log.Info("DE48Tag71 Data Length Less Than 3");
                    return;
                }
                var de48Tag71TotoalLength = de48Tag71FieldString.Length;

                try
                {
                    if (_unPackDe48Tag71DataSortedList == null)
                        _unPackDe48Tag71DataSortedList = new SortedList<string, string>();
                    else
                    {
                        _unPackDe48Tag71DataSortedList.Clear();
                        _unPackDe48Tag71DataSortedList = null;
                        _unPackDe48Tag71DataSortedList = new SortedList<string, string>();
                    }

                    int startPos = 0;
                    int dataLength = 0;
                    int offSet = 0;

                    while (offSet < de48Tag71TotoalLength)
                    {
                        dataLength = 2;
                        var tagSubId = de48Tag71FieldString.Substring(startPos, dataLength);
                        startPos += dataLength;
                        offSet += dataLength;
                        //Skip Not Integer
                        if ((tagSubId.Equals("  ")) || tagSubId.Equals("00"))
                            continue;

                        dataLength = 1;
                        var tagSubIdData = de48Tag71FieldString.Substring(startPos, dataLength);
                        startPos += dataLength;
                        offSet += dataLength;
                        _unPackDe48Tag71DataSortedList.Add(tagSubId, tagSubId + tagSubIdData);

                        dataLength = 1;  //Skip SPACES
                        startPos += dataLength;
                        offSet += dataLength;

                    }
                }
                catch (Exception ex)
                {
                    Log.Fatal(String.Format("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                        new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                        "Unpack Field48Tag71 Field Error",
                        String.Format("Field48Tag71 Constructor(Message) Exception:{0}", ex.Message)));

                    throw new Exception(String.Format("Field48Tag71 Constructor(Message) Exception:{0}", ex.Message));
                }
                return;
            }
            catch (Exception ex)
            {
                Log.Fatal("Method:[{0}] -- Description[{1}]-- Error:[{2}]", new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name, "Unpack DE58TagId32 Error", String.Format("ISOTrack Constructor(Message) Exception:{0}", ex.Message));
            }
        }

        public bool FindDe48Tag71TagIdExist(string TagId)
        {
            if (_unPackDe48Tag71DataSortedList == null) return (false);
            if (_unPackDe48Tag71DataSortedList.ContainsKey(TagId))
                return (true);
            return (false);
        }

        public string GetDe48Tag71DataByTagId(string TagId)
        {
            try
            {
                if (!FindDe48Tag71TagIdExist(TagId)) return (null);
                string de48Tag71TagData = _unPackDe48Tag71DataSortedList[TagId];
                return (de48Tag71TagData);
            }
            catch (Exception ex)
            {
                Log.Fatal(String.Format("Method:[{0}] -- Description[{1}]-- Error:[{2}]",
                    new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name,
                    "Field48Tag71:" + TagId,
                    ex.Message));

                return (null);
            }
        }
    }
}
