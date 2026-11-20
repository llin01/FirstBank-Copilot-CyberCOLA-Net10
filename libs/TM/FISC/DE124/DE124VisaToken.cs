using System;
using System.Collections.Generic;
using System.Linq;
using Com.Cybersoft4u.CyberUtils.Utils;
using NLog;

namespace Com.Cybersoft4u.TM.FISC.DE124
{
    public class DE124VisaToken
    {
        #region NLog

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        #endregion //NLog  

        private readonly List<string> _unPackDE124TagFieldDataList;

        public DE124VisaToken(byte[] packDE124TagFieldData)
        {
            _unPackDE124TagFieldDataList = new List<string>();
            try
            {
                int startPos = 0;
                int offSet = 0;
                int subOffSet = 0;
                while (offSet < packDE124TagFieldData.Length)
                {
                    int dataLength = 1;
                    var tagIdByte = CommonUtils.GetSubBytes(packDE124TagFieldData, offSet, dataLength);
                    var tagIdString = CommonUtils.HexBinary2HexString(tagIdByte);
                    startPos += dataLength;
                    offSet += dataLength;

                    dataLength = 2;
                    var tagLengthBytes = CommonUtils.GetSubBytes(packDE124TagFieldData, offSet, dataLength);
                    var tagLength = Convert.ToUInt16(tagLengthBytes[0]) * 64;
                    tagLength += Convert.ToUInt16(tagLengthBytes[1]);
                    startPos += dataLength;
                    offSet += dataLength;
                    subOffSet = 0;
                    while (subOffSet < tagLength)
                    {
                        var subTagIdByte = CommonUtils.GetSubBytes(packDE124TagFieldData, offSet, 1);
                        if (subTagIdByte[0] == 0x1F)
                            dataLength = 2;
                        else
                            dataLength = 1;
                        var subTagId = CommonUtils.GetSubBytes(packDE124TagFieldData, offSet, dataLength);
                        var subTagIdString = CommonUtils.HexBinary2HexString(subTagId);
                        startPos += dataLength;
                        offSet += dataLength;
                        subOffSet += dataLength;

                        dataLength = 1;
                        var subTagLengthBytes = CommonUtils.GetSubBytes(packDE124TagFieldData, offSet, dataLength);
                        var subTagLength = Convert.ToUInt16(subTagLengthBytes[0]);
                        startPos += dataLength;
                        offSet += dataLength;
                        subOffSet += dataLength;

                        dataLength = subTagLength;
                        var subTagDataBytes = CommonUtils.GetSubBytes(packDE124TagFieldData, offSet, dataLength);
                        //_unPackDE124TagFieldDataList.Add($"{tagIdString}{subTagIdString}--{ColaCommUtility.HexBinary2HexString(subTagDataBytes)}");
                        _unPackDE124TagFieldDataList.Add($"{tagIdString}{subTagIdString}--{EbcdicConverter.GetString(CommonUtils.HexBinary2HexString(subTagDataBytes))}");
                        startPos += dataLength;
                        offSet += dataLength;
                        subOffSet += dataLength;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Fatal($"Method:[{new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name}] -- Description[Unpack Visa DE124 Tag Field Error]-- Error:[DE124Tag Constructor(Message) Exception:{ex.Message}]");
                throw new Exception($"DE124VisaToken Constructor(Message) Exception:{ex.Message}");
            }
        }

        /// <summary>
        /// Get Token Field Information
        /// </summary>
        /// <returns>
        /// SortList For Token Field Data Information
        /// </returns>
        public List<string> GetListDE124TagField()
        {
            return _unPackDE124TagFieldDataList;
        }

        /// <summary>
        /// Get DE124 Tag Data By DE124 Tag Id
        /// </summary>
        /// <param name="de124TagId">DE124 Tag Id</param>
        /// <returns>
        /// DE124 Tag Data Information
        /// </returns>
        public List<string> GetDE124TagDataByTokenId(string de124TagId)
        {
            try
            {
                var delTagidLength = de124TagId.Length;
                var findList = _unPackDE124TagFieldDataList.Where(value => value.Substring(0, delTagidLength + 2).Equals(de124TagId + "--")).ToList();
                return (findList);
            }
            catch (Exception ex)
            {
                Log.Fatal($"Method:[{new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name}] -- Description[{"DE124TagId:" + de124TagId}]-- Exception:[{ex.Message}]");

                return (null);
            }
        }

        /// <summary>
        /// Check DE124 Tag Id Exist
        /// </summary>
        /// <returns>
        /// Find DE124 Tag Id Flag
        /// </returns>
        public bool FindDE124TagIdExist(string de124TagId)
        {
            if (_unPackDE124TagFieldDataList == null) return (false);
            var delTagidLength = de124TagId.Length;
            bool exists = _unPackDE124TagFieldDataList.Any(value => value.Substring(0, delTagidLength + 2).Equals(de124TagId + "--"));
            return exists;
        }
    }
}
