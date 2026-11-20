using System;
using System.Xml;
using System.Xml.Serialization;
using Com.Cybersoft4u.CommonLib.MQ;
using Com.Cybersoft4u.CyberUtils.Utils;
using NLog;

namespace Com.Cybersoft4u.CommonLib.Resource
{
    public class SingletonOfMQServerInfo
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static SingletonOfMQServerInfo _instance = new SingletonOfMQServerInfo();
        private static readonly object InstanceLock = new object();
        private readonly MQServerInfo _mqServerInfo;
        private SingletonOfMQServerInfo()
        {
            try
            {
                var configurationLoaderRoot = new ConfigurationLoaderRoot();
                string xmlFilePath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "first-bank-mq-server-info-xml-file-path");
                if (string.IsNullOrEmpty(xmlFilePath))
                {
                    Log.Fatal("Can not find parameter[first-bank-mq-server-info-xml-file-path]");
                    return;
                }
                XmlSerializer serializer = new XmlSerializer(typeof(MQServerInfo));
                using (XmlReader reader = XmlReader.Create(xmlFilePath))
                {
                    _mqServerInfo = (MQServerInfo)serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"SingletonOfMQServerInfo Exception:{ex.Message}");
            }
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static SingletonOfMQServerInfo Instance
        {
            get
            {
                lock (InstanceLock)
                {
                    return _instance ?? (_instance = new SingletonOfMQServerInfo());
                }
            }
        }

        /// <summary>
        /// Gets the MQServer information.
        /// </summary>
        /// <returns>MQServerInfo.</returns>
        public MQServerInfo GetMQServerInfo()
        {
            return (_mqServerInfo);
        }
    }
}
