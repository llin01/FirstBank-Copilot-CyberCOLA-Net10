using Com.Cybersoft4u.CyberUtils.Utils;
using NLog;
using System;
using System.Xml;
using System.Xml.Serialization;

namespace Com.Cybersoft4u.CommonLib.Resource
{
    public class SingletonOfSMSWebAPIInfo
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static SingletonOfSMSWebAPIInfo _instance = new SingletonOfSMSWebAPIInfo();
        private static readonly object InstanceLock = new object();
        private readonly SMSWebAPIInfo _smsWebAPIInfo;
        private SingletonOfSMSWebAPIInfo()
        {
            try
            {
                var configurationLoaderRoot = new ConfigurationLoaderRoot();
                string xmlFilePath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "first-bank-sms-web-api-info-xml-file-path");
                if (string.IsNullOrEmpty(xmlFilePath))
                {
                    Log.Fatal("Can not find parameter[first-bank-sms-web-api-info-xml-file-path]");
                    return;
                }
                XmlSerializer serializer = new XmlSerializer(typeof(SMSWebAPIInfo));
                using (XmlReader reader = XmlReader.Create(xmlFilePath))
                {
                    _smsWebAPIInfo = (SMSWebAPIInfo)serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"SingletonOfSMSWebAPIInfo Exception:{ex.Message}");
            }
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static SingletonOfSMSWebAPIInfo Instance
        {
            get
            {
                lock (InstanceLock)
                {
                    return _instance ??= new SingletonOfSMSWebAPIInfo();
                }
            }
        }

        /// <summary>
        /// Gets the SMSWebAPI information.
        /// </summary>
        /// <returns>SMSWebAPIInfo.</returns>
        public SMSWebAPIInfo GetSMSWebAPIInfo()
        {
            return (_smsWebAPIInfo);
        }
    }
}
