using System;
using Com.Cybersoft4u.CyberUtils.Utils;
using NLog;

namespace Com.Cybersoft4u.TM.FISC.DE124
{
    public class DE124TagField
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog


        private readonly string _unPackDE124TagFieldData;
        private readonly int _unPackDE124OverallLength;
        private char _cardType = ' ';
        private readonly DE124MasterCardToken _de124MasterCardToken = null;
        private readonly DE124VisaToken _de124VisaToken = null;

        public DE124TagField(char cardType, string inputDE124TagData)
        {
            const int fixOffSet = 3;
            if (inputDE124TagData == null)
            {
                Log.Fatal($"Method:[{new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name}] -- Description[DE124TagField]-- Error:[DE124 Tag Data Is null]");
                throw new ArgumentException("DE124 Tag Data Is null");
            }
            if (inputDE124TagData.Length < fixOffSet)
            {
                Log.Fatal($"Method:[{new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name}] -- Description[DE124TagField]-- Error:[DE124 Tag Data Length Less Than 3]");
                throw new ArgumentException("DE124 Tag Data Length Less Than 3");
            }
            var realDataLength = Convert.ToInt32(inputDE124TagData.Substring(0, 3));
            if ((realDataLength * 2) != (inputDE124TagData.Length - 3))
            {
                Log.Fatal($"Method:[{new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name}] -- Description[DE124TagField]-- Error:[DE124 Tag Data Length No Match]");
                throw new ArgumentException("DE124 Tag Data Length No Match");
            }
            _unPackDE124TagFieldData = inputDE124TagData;
            _unPackDE124OverallLength = inputDE124TagData.Length - 3;
            var packDE124TagFieldData = new byte[(inputDE124TagData.Length - 3) / 2];
            _cardType = cardType;
            try
            {
                switch (_cardType)
                {
                    case 'v':
                    case 'V':
                        packDE124TagFieldData = CommonUtils.HexString2HexBinary(inputDE124TagData.Substring(3));
                        _de124VisaToken = new DE124VisaToken(packDE124TagFieldData);
                        break;
                    case 'm':
                    case 'M':
                        _cardType = 'M';
                        _de124MasterCardToken = new DE124MasterCardToken(EbcdicConverter.GetString(inputDE124TagData.Substring(3)));
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Fatal($"Method:[{new System.Diagnostics.StackTrace(true).GetFrame(0)?.GetMethod()?.Name}] -- Description[Unpack DE124 Tag Field Error]-- Error:[DE124Tag Constructor(Message) Exception:{ex.Message}]");
                throw new Exception($"DE124Tag Constructor(Message) Exception:{ex.Message}");
            }
        }

        /// <summary>
        /// Get DE124 Tag All Field Data Structure
        /// </summary>
        /// <returns>
        /// DE124 Tag All Field Data Structure
        /// </returns>
        public string GetDE124TagData()
        {
            return _unPackDE124TagFieldData;
        }

        /// <summary>
        /// Get Overall Length For DE124
        /// </summary>
        /// <returns>
        /// DE124 Overall Data Length
        /// </returns>
        public int GetDE124OverallLength()
        {
            return (_unPackDE124OverallLength);
        }

        public DE124MasterCardToken GetDE124MasterCardToken()
        {

            return _de124MasterCardToken;
        }
        public DE124VisaToken GetDE124VisaToken()
        {
            return _de124VisaToken;
        }
    }
}
