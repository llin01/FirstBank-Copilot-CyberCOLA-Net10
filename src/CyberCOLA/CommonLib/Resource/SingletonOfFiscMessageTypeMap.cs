using Com.Cybersoft4u.CommonLib.FiscMessage;
using Com.Cybersoft4u.CyberUtils.Utils;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Xml;
using System.Xml.Serialization;
using MessageType = Com.Cybersoft4u.CommonLib.FiscMessage.MessageType;

namespace Com.Cybersoft4u.CommonLib.Resource
{
    public class SingletonOfFiscMessageTypeMap
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static SingletonOfFiscMessageTypeMap instance = new SingletonOfFiscMessageTypeMap();
        private static readonly object InstanceLock = new object();
        private readonly ConcurrentDictionary<string, MessageType> fiscMessageTypeMaps = new ConcurrentDictionary<string, MessageType>();

        private SingletonOfFiscMessageTypeMap()
        {
            try
            {
                var configurationLoaderRoot = new ConfigurationLoaderRoot();
                FiscMessageTypeInfo fiscMessageTypeInfo;
                string xmlFilePath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "fisc-message-type-map-xml-file-path");
                if (string.IsNullOrEmpty(xmlFilePath))
                {
                    Log.Fatal("Can not find parameter[fisc-message-type-map-xml-file-path]");
                    return;
                }
                XmlSerializer serializer = new XmlSerializer(typeof(FiscMessageTypeInfo));
                using (XmlReader reader = XmlReader.Create(xmlFilePath))
                {
                    fiscMessageTypeInfo = (FiscMessageTypeInfo)serializer.Deserialize(reader);
                }
                foreach (var message in fiscMessageTypeInfo.MessageType)
                {
                    fiscMessageTypeMaps.TryAdd(message.RequestMessage, message);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"SingletonOfFiscMessageTypeMap Exception:{ex.Message}");
            }
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static SingletonOfFiscMessageTypeMap Instance
        {
            get
            {
                lock (InstanceLock)
                {
                    return instance ??= new SingletonOfFiscMessageTypeMap();
                }
            }
        }

        /// <summary>
        /// Gets the message type map.
        /// </summary>
        /// <returns>ConcurrentDictionary&lt;System.String, MessageType&gt;.</returns>
        public ConcurrentDictionary<string, MessageType> GetMessageTypeMap()
        {
            return (fiscMessageTypeMaps);
        }
    }

}
