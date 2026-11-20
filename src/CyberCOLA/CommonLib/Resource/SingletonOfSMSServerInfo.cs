using Com.Cybersoft4u.CyberUtils.Utils;
using NLog;
using System;
using System.Xml;
using System.Xml.Serialization;

namespace Com.Cybersoft4u.CommonLib.Resource
{
    public class SingletonOfSMSServerInfo
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static SingletonOfSMSServerInfo _instance = new SingletonOfSMSServerInfo();
        private static readonly object InstanceLock = new object();
        private readonly SMSServerInfo _smsServerInfo;
        private SingletonOfSMSServerInfo()
        {
            try
            {
                var configurationLoaderRoot = new ConfigurationLoaderRoot();
                string xmlFilePath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "first-bank-sms-server-info-xml-file-path");
                if (string.IsNullOrEmpty(xmlFilePath))
                {
                    Log.Fatal("Can not find parameter[first-bank-sms-server-info-xml-file-path]");
                    return;
                }
                XmlSerializer serializer = new XmlSerializer(typeof(SMSServerInfo));
                using (XmlReader reader = XmlReader.Create(xmlFilePath))
                {
                    _smsServerInfo = (SMSServerInfo)serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"SingletonOfSMSServerInfo Exception:{ex.Message}");
            }
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static SingletonOfSMSServerInfo Instance
        {
            get
            {
                lock (InstanceLock)
                {
                    return _instance ??= new SingletonOfSMSServerInfo();
                }
            }
        }

        /// <summary>
        /// Gets the SMSServer information.
        /// </summary>
        /// <returns>SMSServerInfo.</returns>
        public SMSServerInfo GetSMSServerInfo()
        {
            return (_smsServerInfo);
        }
    }
}
