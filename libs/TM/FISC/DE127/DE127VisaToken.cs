using System;
using System.Collections.Generic;
using System.Linq;
using Com.Cybersoft4u.CyberUtils.Utils;
using NLog;

namespace Com.Cybersoft4u.TM.FISC.DE127
{
    public class DE127VisaToken
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private readonly List<string> _unPackDE127TagFieldDataList;

        public DE127VisaToken(string inputDE127TagData)
        {
            if (inputDE127TagData == null)
            {
                Log.Fatal($"Method:[{new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name}] -- Description[DE127VisaToken]-- Error:[DE127 Tag Data Is null]");
                throw new ArgumentException("DE127 Tag Data Is null");
            }

            var realDataLength = inputDE127TagData.Length;
            var packDE127TagFieldData = new byte[(inputDE127TagData.Length) / 2];
            packDE127TagFieldData = CommonUtils.HexString2HexBinary(inputDE127TagData);

            _unPackDE127TagFieldDataList = new List<string>();
            try
            {
                int startPos = 0;
                int offSet = 0;
                int subOffSet = 0;
                while (offSet < packDE127TagFieldData.Length)
                {
                    int dataLength = 1;
                    var tagIdByte = CommonUtils.GetSubBytes(packDE127TagFieldData, offSet, dataLength);
                    var tagIdString = CommonUtils.HexBinary2HexString(tagIdByte);
                    startPos += dataLength;
                    offSet += dataLength;

                    dataLength = 2;
                    var tagLengthBytes = CommonUtils.GetSubBytes(packDE127TagFieldData, offSet, dataLength);
                    var tagLength = Convert.ToUInt16(tagLengthBytes[0]) * 64;
                    tagLength += Convert.ToUInt16(tagLengthBytes[1]);
                    startPos += dataLength;
                    offSet += dataLength;
                    subOffSet = 0;
                    while (subOffSet < tagLength)
                    {
                        var subTagIdByte = CommonUtils.GetSubBytes(packDE127TagFieldData, offSet, 1);
                        if (subTagIdByte[0] == 0x1F)
                            dataLength = 2;
                        else
                            dataLength = 1;
                        var subTagId = CommonUtils.GetSubBytes(packDE127TagFieldData, offSet, dataLength);
                        var subTagIdString = CommonUtils.HexBinary2HexString(subTagId);
                        startPos += dataLength;
                        offSet += dataLength;
                        subOffSet += dataLength;

                        dataLength = 1;
                        var subTagLengthBytes = CommonUtils.GetSubBytes(packDE127TagFieldData, offSet, dataLength);
                        var subTagLength = Convert.ToUInt16(subTagLengthBytes[0]);
                        startPos += dataLength;
                        offSet += dataLength;
                        subOffSet += dataLength;

                        dataLength = subTagLength;
                        var subTagDataBytes = CommonUtils.GetSubBytes(packDE127TagFieldData, offSet, dataLength);
                        //_unPackDE127TagFieldDataList.Add($"{tagIdString}{subTagIdString}--{ColaCommUtility.HexBinary2HexString(subTagDataBytes)}");
                        _unPackDE127TagFieldDataList.Add($"{tagIdString}{subTagIdString}--{EbcdicConverter.GetString(CommonUtils.HexBinary2HexString(subTagDataBytes))}");
                        startPos += dataLength;
                        offSet += dataLength;
                        subOffSet += dataLength;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Fatal($"Method:[{new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name}] -- Description[DE127VisaToken DE127 Tag Field Error]-- Error:[DE127Tag Constructor(Message) Exception:{ex.Message}]");
                throw new Exception($"DE127Tag Constructor(Message) Exception:{ex.Message}");
            }
        }

        /// <summary>
        /// Get Token Field Information
        /// </summary>
        /// <returns>
        /// SortList For Token Field Data Information
        /// </returns>
        public List<string> GetListDE127TagField()
        {
            return _unPackDE127TagFieldDataList;
        }

        /// <summary>
        /// Get DE127 Tag Data By DE127 Tag Id
        /// </summary>
        /// <param name="de127TagId">DE127 Tag Id</param>
        /// <returns>
        /// DE127 Tag Data Information
        /// </returns>
        public List<string> GetDE127TagDataByTokenId(string de127TagId)
        {
            try
            {
                var delTagidLength = de127TagId.Length;
                var findList = _unPackDE127TagFieldDataList.Where(value => value.Substring(0, delTagidLength + 2).Equals(de127TagId + "--")).ToList();
                return (findList);
            }
            catch (Exception ex)
            {
                Log.Fatal($"Method:[{new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name}] -- Description[{"GetDE127TagDataByTokenId:" + de127TagId}]-- Exception:[{ex.Message}]");

                return (null);
            }
        }

        /// <summary>
        /// Check DE127 Tag Id Exist
        /// </summary>
        /// <returns>
        /// Find DE127 Tag Id Flag
        /// </returns>
        public bool FindDE127TagIdExist(string de127TagId)
        {
            if (_unPackDE127TagFieldDataList == null) return (false);
            var delTagidLength = de127TagId.Length;
            bool exists = _unPackDE127TagFieldDataList.Any(value => value.Substring(0, delTagidLength + 2).Equals(de127TagId + "--"));
            return exists;
        }
    }
}
