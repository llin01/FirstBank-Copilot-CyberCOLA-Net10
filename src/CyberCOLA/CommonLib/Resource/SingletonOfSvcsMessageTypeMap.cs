using Com.Cybersoft4u.CommonLib.SvcsMessage;
using Com.Cybersoft4u.CyberUtils.Utils;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Xml;
using System.Xml.Serialization;

namespace Com.Cybersoft4u.CommonLib.Resource
{
    public class SingletonOfSvcsMessageTypeMap
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static SingletonOfSvcsMessageTypeMap instance = new SingletonOfSvcsMessageTypeMap();
        private static readonly object InstanceLock = new object();
        private readonly ConcurrentDictionary<string, MessageType> svcsMessageTypeMaps = new ConcurrentDictionary<string, MessageType>();

        private SingletonOfSvcsMessageTypeMap()
        {
            try
            {
                var configurationLoaderRoot = new ConfigurationLoaderRoot();
                SvcsMessageTypeInfo svcsMessageTypeInfo;
                string xmlFilePath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "svcs-message-type-map-xml-file-path");
                if (string.IsNullOrEmpty(xmlFilePath))
                {
                    Log.Fatal("Can not find parameter[svcs-message-type-map-xml-file-path]");
                    return;
                }
                XmlSerializer serializer = new XmlSerializer(typeof(SvcsMessageTypeInfo));
                using (XmlReader reader = XmlReader.Create(xmlFilePath))
                {
                    svcsMessageTypeInfo = (SvcsMessageTypeInfo)serializer.Deserialize(reader);
                }
                foreach (var message in svcsMessageTypeInfo.MessageType)
                {
                    svcsMessageTypeMaps.TryAdd(message.RequestMessage, message);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"SingletonOfSvcsMessageTypeMap Exception:{ex.Message}");
            }
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static SingletonOfSvcsMessageTypeMap Instance
        {
            get
            {
                lock (InstanceLock)
                {
                    return instance ??= new SingletonOfSvcsMessageTypeMap();
                }
            }
        }

        /// <summary>
        /// Gets the message type map.
        /// </summary>
        /// <returns>ConcurrentDictionary&lt;System.String, MessageType&gt;.</returns>
        public ConcurrentDictionary<string, MessageType> GetMessageTypeMap()
        {
            return (svcsMessageTypeMaps);
        }
    }

}
