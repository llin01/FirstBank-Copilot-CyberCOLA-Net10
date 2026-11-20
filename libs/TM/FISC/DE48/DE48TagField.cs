using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Com.Cybersoft4u.CyberUtils.Utils;
using NLog;

namespace Com.Cybersoft4u.TM.FISC.DE48
{
    public class DE48TagField
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private readonly string _de48OriginalData = null;
        private readonly string _dE48TCC;
        private readonly SortedList<string, string> _de48TagFieldSortedList = null;
        public DE48TagField(string inputDE48TagData)
        {
            const int fixOffSet = 1;
            _dE48TCC = null;
            if (inputDE48TagData == null)
            {
                Log.Fatal($"Method:[{new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name}] -- Description[DE48TagField]-- Error:[DE48 Tag Data Is null]");
                throw new ArgumentException("DE48 Tag Data Is null");
            }
            if (inputDE48TagData.Length < fixOffSet)
            {
                Log.Fatal($"Method:[{new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name}] -- Description[DE48TagField]-- Error:[DE48 Tag Data Length Less Than 3]");
                throw new ArgumentException("DE48 Tag Data Length Less Than 1");
            }
            _de48OriginalData = inputDE48TagData;
            var de48OriginalDataLength = inputDE48TagData.Length;
            try
            {

                _de48TagFieldSortedList = new SortedList<string, string>();
                var startPosition = 0;

                var dataLength = 1;
                _dE48TCC = inputDE48TagData.Substring(startPosition, dataLength);
                startPosition += dataLength;

                while (startPosition < de48OriginalDataLength)
                {
                    var offset = 2;
                    var subToken = inputDE48TagData.Substring(startPosition, offset);
                    startPosition += offset;

                    offset = 2;
                    var subTokenLength = Convert.ToInt16(inputDE48TagData.Substring(startPosition, offset));
                    startPosition += offset;

                    offset = subTokenLength;
                    var subTokenData = inputDE48TagData.Substring(startPosition, offset);
                    startPosition += offset;
                    _de48TagFieldSortedList[subToken] = subTokenData;
                }
            }
            catch (Exception ex)
            {
                Log.Fatal($"Method:[{new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name}] -- Description[Unpack DE48 Tag Field Error]-- Error:[DE48Tag Constructor(Message) Exception:{ex.Message}]");
                throw new Exception($"DE48Tag Constructor(Message) Exception:{ex.Message}");
            }
        }

        /// <summary>
        /// Get Token Field Information
        /// </summary>
        /// <returns>
        /// SortList For Token Field Data Information
        /// </returns>
        public SortedList<string, string> GetSortedListDE48TagField()
        {
            return _de48TagFieldSortedList;
        }

        /// <summary>
        /// Get DE48 TCC
        /// </summary>
        /// <returns>
        /// DE48 TCC
        /// </returns>
        public string GetDE48TCC()
        {
            if (string.IsNullOrEmpty(_de48OriginalData)) return (null);
            return (_dE48TCC);
        }
        /// <summary>
        /// Get DE48 Tag Data By DE48 Tag Id
        /// </summary>
        /// <param name="de48TagId">DE48 Tag Id</param>
        /// <returns>
        /// DE48 Tag Data Information
        /// </returns>
        public string GetDE48TagDataByTokenId(string de48TagId)
        {
            try
            {
                if (string.IsNullOrEmpty(_de48OriginalData)) return (null);
                if (!FindDE48TagIdExist(de48TagId)) return (null);
                string de48TagData = _de48TagFieldSortedList[de48TagId];
                if (de48TagData.Equals("44") || de48TagData.Equals("72"))
                {
                    Encoding enc = Encoding.GetEncoding("IBM037");
                    byte[] bytes = enc.GetBytes(de48TagData);
                    de48TagData = CommonUtils.HexBinary2HexString(bytes);
                }
                return (de48TagData);
            }
            catch (Exception ex)
            {
                Log.Fatal($"Method:[{new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name}] -- Description[{"DE48TagId:" + de48TagId}]-- Exception:[{ex.Message}]");

                return (null);
            }
        }
        /// <summary>
        /// Get DE48 Tag All Field Data Structure
        /// </summary>
        /// <returns>
        /// DE48 Tag All Field Data Structure
        /// </returns>
        public string GetDE48TagData()
        {
            return _de48OriginalData;
        }

        /// <summary>
        /// Check DE48 Tag Id Exist
        /// </summary>
        /// <returns>
        /// Find DE48 Tag Id Flag
        /// </returns>
        public bool FindDE48TagIdExist(string de48TagId)
        {
            if (_de48TagFieldSortedList == null) return (false);
            if (_de48TagFieldSortedList.ContainsKey(de48TagId))
                return (true);
            return (false);
        }
    }
}
