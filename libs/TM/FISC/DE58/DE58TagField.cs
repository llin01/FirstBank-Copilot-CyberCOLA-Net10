using System;
using System.Collections.Generic;
using NLog;

namespace Com.Cybersoft4u.TM.FISC.DE58
{
    public class DE58TagField
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private readonly string _de58OriginalData = null;
        private readonly SortedList<string, string> _de58TagFieldSortedList = null;
        public DE58TagField(string inputDE58TagData)
        {
            const int fixOffSet = 3;
            if (inputDE58TagData == null)
            {
                Log.Fatal($"Method:[{(new System.Diagnostics.StackTrace(true)).GetFrame(0).GetMethod().Name}] -- Description[DE58TagField]-- Error:[DE58 Tag Data Is null]");
                throw new ArgumentException("DE58 Tag Data Is null");
            }
            if (inputDE58TagData.Length < fixOffSet)
            {
                Log.Fatal($"Method:[{(new System.Diagnostics.StackTrace(true)).GetFrame(0).GetMethod().Name}] -- Description[DE58TagField]-- Error:[DE58 Tag Data Length Less Than 3]");
                throw new ArgumentException("DE58 Tag Data Length Less Than 3");
            }
            _de58OriginalData = inputDE58TagData;
            var de58OriginalDataLength = inputDE58TagData.Length;
            try
            {

                _de58TagFieldSortedList = new SortedList<string, string>();
                var startPosition = 0;

                while (startPosition < de58OriginalDataLength)
                {
                    var offset = 2;
                    var subToken = inputDE58TagData.Substring(startPosition, offset);
                    startPosition += offset;

                    offset = 2;
                    var subTokenLength = Convert.ToInt16(inputDE58TagData.Substring(startPosition, offset));
                    startPosition += offset;

                    offset = subTokenLength;
                    var subTokenData = inputDE58TagData.Substring(startPosition, offset);
                    startPosition += offset;
                    _de58TagFieldSortedList[subToken] = subTokenData;
                }
            }
            catch (Exception ex)
            {
                Log.Fatal($"Method:[{(new System.Diagnostics.StackTrace(true)).GetFrame(0).GetMethod().Name}] -- Description[Unpack DE58 Tag Field Error]-- Error:[DE58Tag Constructor(Message) Exception:{ex.Message}]");
                throw new Exception($"DE58Tag Constructor(Message) Exception:{ex.Message}");
            }
        }

        /// <summary>
        /// Get Token Field Information
        /// </summary>
        /// <returns>
        /// SortList For Token Field Data Information
        /// </returns>
        public SortedList<string, string> GetSortedListDE58TagField()
        {
            return _de58TagFieldSortedList;
        }

        /// <summary>
        /// Get DE58 Tag Data By DE58 Tag Id
        /// </summary>
        /// <param name="de58TagId">DE58 Tag Id</param>
        /// <returns>
        /// DE58 Tag Data Information
        /// </returns>
        public string GetDE58TagDataByTokenId(string de58TagId)
        {
            try
            {
                if (string.IsNullOrEmpty(_de58OriginalData)) return (null);
                if (!FindDE58TagIdExist(de58TagId)) return (null);
                string de58TagData = _de58TagFieldSortedList[de58TagId];
                return (de58TagData);
            }
            catch (Exception ex)
            {
                Log.Fatal($"Method:[{(new System.Diagnostics.StackTrace(true)).GetFrame(0).GetMethod().Name}] -- Description[{"DE58TagId:" + de58TagId}]-- Error:[{ex.Message}]");

                return (null);
            }
        }
        /// <summary>
        /// Get DE58 Tag All Field Data Structure
        /// </summary>
        /// <returns>
        /// DE58 Tag All Field Data Structure
        /// </returns>
        public string GetDE58TagData()
        {
            return _de58OriginalData;
        }

        /// <summary>
        /// Check DE58 Tag Id Exist
        /// </summary>
        /// <returns>
        /// Find DE58 Tag Id Flag
        /// </returns>
        public bool FindDE58TagIdExist(string de58TagId)
        {
            if (_de58TagFieldSortedList == null) return (false);
            if (_de58TagFieldSortedList.ContainsKey(de58TagId))
                return (true);
            return (false);
        }
    }
}
