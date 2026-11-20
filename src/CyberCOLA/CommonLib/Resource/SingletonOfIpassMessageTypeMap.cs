using Com.Cybersoft4u.CommonLib.IpassMessage;
using Com.Cybersoft4u.CyberUtils.Utils;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Xml;
using System.Xml.Serialization;

namespace Com.Cybersoft4u.CommonLib.Resource
{
    public class SingletonOfIpassMessageTypeMap
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static SingletonOfIpassMessageTypeMap instance = new SingletonOfIpassMessageTypeMap();
        private static readonly object InstanceLock = new object();
        private readonly ConcurrentDictionary<string, MessageType> ipassMessageTypeMaps = new ConcurrentDictionary<string, MessageType>();

        private SingletonOfIpassMessageTypeMap()
        {
            try
            {
                var configurationLoaderRoot = new ConfigurationLoaderRoot();
                IpassMessageTypeInfo ipassMessageTypeInfo;
                string xmlFilePath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "ipass-message-type-map-xml-file-path");
                if (string.IsNullOrEmpty(xmlFilePath))
                {
                    Log.Fatal("Can not find parameter[ipass-message-type-map-xml-file-path]");
                    return;
                }
                XmlSerializer serializer = new XmlSerializer(typeof(IpassMessageTypeInfo));
                using (XmlReader reader = XmlReader.Create(xmlFilePath))
                {
                    ipassMessageTypeInfo = (IpassMessageTypeInfo)serializer.Deserialize(reader);
                }
                foreach (var message in ipassMessageTypeInfo.MessageType)
                {
                    ipassMessageTypeMaps.TryAdd(message.RequestMessage, message);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"SingletonOfIpassMessageTypeMap Exception:{ex.Message}");
            }
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static SingletonOfIpassMessageTypeMap Instance
        {
            get
            {
                lock (InstanceLock)
                {
                    return instance ??= new SingletonOfIpassMessageTypeMap();
                }
            }
        }

        /// <summary>
        /// Gets the message type map.
        /// </summary>
        /// <returns>ConcurrentDictionary&lt;System.String, MessageType&gt;.</returns>
        public ConcurrentDictionary<string, MessageType> GetMessageTypeMap()
        {
            return (ipassMessageTypeMaps);
        }
    }

}
