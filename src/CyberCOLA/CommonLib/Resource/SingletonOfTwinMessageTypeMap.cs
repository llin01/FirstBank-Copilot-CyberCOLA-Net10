using System;
using System.Collections.Concurrent;
using System.Xml;
using System.Xml.Serialization;
using Com.Cybersoft4u.CommonLib.TwinMessage;
using Com.Cybersoft4u.CyberUtils.Utils;
using NLog;
namespace Com.Cybersoft4u.CommonLib.Resource
{
    public class SingletonOfTwinMessageTypeMap
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static SingletonOfTwinMessageTypeMap instance = new SingletonOfTwinMessageTypeMap();
        private static readonly object InstanceLock = new object();
        private readonly ConcurrentDictionary<string, MessageType> twinMessageTypeMaps = new ConcurrentDictionary<string, MessageType>();

        private SingletonOfTwinMessageTypeMap()
        {
            try
            {
                var configurationLoaderRoot = new ConfigurationLoaderRoot();
                TwinMessageTypeInfo twinMessageTypeInfo;
                string xmlFilePath = configurationLoaderRoot.Get("twin-message-type-map-xml-file-path");
                if (string.IsNullOrEmpty(xmlFilePath))
                {
                    Log.Fatal("Can not find parameter[twin-message-type-map-xml-file-path]");
                    return;
                }
                XmlSerializer serializer = new XmlSerializer(typeof(TwinMessageTypeInfo));
                using (XmlReader reader = XmlReader.Create(xmlFilePath))
                {
                    twinMessageTypeInfo = (TwinMessageTypeInfo)serializer.Deserialize(reader);
                }
                foreach (var message in twinMessageTypeInfo.MessageType)
                {
                    twinMessageTypeMaps.TryAdd(message.RequestMessage, message);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"SingletonOfTwinMessageTypeMap Exception:{ex.Message}");
            }
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static SingletonOfTwinMessageTypeMap Instance
        {
            get
            {
                lock (InstanceLock)
                {
                    return instance ??= new SingletonOfTwinMessageTypeMap();
                }
            }
        }

        /// <summary>
        /// Gets the message type map.
        /// </summary>
        /// <returns>ConcurrentDictionary&lt;System.String, MessageType&gt;.</returns>
        public ConcurrentDictionary<string, MessageType> GetMessageTypeMap()
        {
            return (twinMessageTypeMaps);
        }
    }

}
