using Com.Cybersoft4u.CommonLib.NcccMessage;
using Com.Cybersoft4u.CyberUtils.Utils;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Xml;
using System.Xml.Serialization;
namespace Com.Cybersoft4u.CommonLib.Resource
{
    public class SingletonOfNcccMessageTypeMap
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static SingletonOfNcccMessageTypeMap instance = new SingletonOfNcccMessageTypeMap();
        private static readonly object InstanceLock = new object();
        private readonly ConcurrentDictionary<string, MessageType> ncccMessageTypeMaps = new ConcurrentDictionary<string, MessageType>();

        private SingletonOfNcccMessageTypeMap()
        {
            try
            {
                var configurationLoaderRoot = new ConfigurationLoaderRoot();
                NcccMessageTypeInfo ncccMessageTypeInfo;
                string xmlFilePath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "nccc-message-type-map-xml-file-path");
                if (string.IsNullOrEmpty(xmlFilePath))
                {
                    Log.Fatal("Can not find parameter[nccc-message-type-map-xml-file-path]");
                    return;
                }
                XmlSerializer serializer = new XmlSerializer(typeof(NcccMessageTypeInfo));
                using (XmlReader reader = XmlReader.Create(xmlFilePath))
                {
                    ncccMessageTypeInfo = (NcccMessageTypeInfo)serializer.Deserialize(reader);
                }
                foreach (var message in ncccMessageTypeInfo.MessageType)
                {
                    ncccMessageTypeMaps.TryAdd(message.RequestMessage, message);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"SingletonOfNcccMessageTypeMap Exception:{ex.Message}");
            }
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static SingletonOfNcccMessageTypeMap Instance
        {
            get
            {
                lock (InstanceLock)
                {
                    return instance ??= new SingletonOfNcccMessageTypeMap();
                }
            }
        }

        /// <summary>
        /// Gets the message type map.
        /// </summary>
        /// <returns>ConcurrentDictionary&lt;System.String, MessageType&gt;.</returns>
        public ConcurrentDictionary<string, MessageType> GetMessageTypeMap()
        {
            return (ncccMessageTypeMaps);
        }
    }

}
