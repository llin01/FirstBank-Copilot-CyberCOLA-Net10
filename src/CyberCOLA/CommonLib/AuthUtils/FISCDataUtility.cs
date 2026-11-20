using System;
using System.Text;
using Com.Cybersoft4u.CommonLib.Resource;
using NLog;

namespace Com.Cybersoft4u.CommonLib.AuthUtils
{
    /// <summary>
    /// Provides utility methods for processing FISC (Financial Information Service Center) EMV chip data fields.
    /// </summary>
    public static class FISCDataUtility
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog
        private static readonly SystemParameterInfo SystemParameterInfo = SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo();
        /// <summary>
        /// Get 9F10 Issuer Application Data
        /// </summary>
        /// <param name="issApplData">Issuer Application Data</param>
        /// <returns>
        /// ISO Field String
        /// </returns>
        public static string ISOGet9F10Data(string issApplData)
        {
            var retnData = new StringBuilder("");
            int dataLength = 0;
            dataLength = (int)Convert.ToUInt32(issApplData.Substring(0, 2), 16);
            if (issApplData.Length != ((dataLength + 1) * 2))
                return (retnData.ToString());
            if (issApplData.Length < 14)
                return (retnData.ToString());
            int startPos = 0;
            dataLength = 2;
            retnData.Append(issApplData.Substring(startPos, dataLength));
            startPos += dataLength;
            retnData.Append(";");

            dataLength = 2;
            retnData.Append(issApplData.Substring(startPos, dataLength));
            startPos += dataLength;
            retnData.Append(";");

            dataLength = 2;
            retnData.Append(issApplData.Substring(startPos, dataLength));
            startPos += dataLength;
            retnData.Append(";");

            retnData.Append(issApplData.Substring(startPos, issApplData.Length - startPos));
            return (retnData.ToString());
        }

        /// <summary>
        /// Get 9F10 Issuer Application Data
        /// 0:DERIV-KEY-INDEX,1:CRYPTO-VER-NUM,2:CRD-VERIF-RSLTS
        /// </summary>
        /// <param name="issApplDataLgth">Issuer Application Data Length</param>
        /// <param name="issApplData">Issuer Application Data</param>
        /// <param name="creditCardType">CardType</param>
        /// <returns>
        /// ISO Field String
        /// </returns>
        public static string ISOGet9F10Data(int issApplDataLgth, string issApplData, CreditCardType creditCardType)
        {
            StringBuilder retnData = new StringBuilder("");
            int dataLength = 0;
            if (creditCardType.Equals(CreditCardType.JCB) || creditCardType.Equals(CreditCardType.Visa))
            {
                dataLength = (int)Convert.ToUInt32(issApplData.Substring(0, 2), 16) * 2;
                if (issApplDataLgth != (dataLength + 2))
                    return (retnData.ToString());
                if ((creditCardType.Equals(CreditCardType.Visa)) && (dataLength == 62))
                {
                    retnData.Append(issApplData.Substring(4, 2));
                    retnData.Append(";");
                    retnData.Append(issApplData.Substring(2, 2));
                    retnData.Append(";");
                    retnData.Append(issApplData.Substring(6, 12));
                }
                else
                {
                    retnData.Append(issApplData.Substring(2, 2));
                    retnData.Append(";");
                    retnData.Append(issApplData.Substring(4, 2));
                    retnData.Append(";");
                    retnData.Append(creditCardType.Equals(CreditCardType.JCB) ? issApplData.Substring(6, 10) : issApplData.Substring(6, 8));
                }
                return (retnData.ToString());
            }
            if (creditCardType.Equals(CreditCardType.MasterCard))
            {
                dataLength = (int)Convert.ToUInt32(issApplData.Substring(0, 2), 16) * 2;
                if (issApplDataLgth == (dataLength + 2))
                {
                    //SELECT 2
                    retnData.Append(issApplData.Substring(2, 2));
                    retnData.Append(";");
                    retnData.Append(issApplData.Substring(4, 2));
                    retnData.Append(";");
                    retnData.Append(issApplData.Substring(6, 8));
                    return (retnData.ToString());
                }
                if (issApplDataLgth > 8)
                {
                    //SELECT M-Chip 4
                    retnData.Append(issApplData.Substring(0, 2));
                    retnData.Append(";");
                    retnData.Append(issApplData.Substring(2, 2));
                    retnData.Append(";");
                    retnData.Append(issApplData.Substring(4, 12));
                    return (retnData.ToString());
                }
                //SELECT M-Chip Lite 2.1
                retnData.Append(issApplData.Substring(0, 2));
                retnData.Append(";");
                retnData.Append(issApplData.Substring(2, 2));
                retnData.Append(";");
                retnData.Append(issApplData.Substring(4, 8));
                return (retnData.ToString());
            }
            return (retnData.ToString());
        }
    }
}
