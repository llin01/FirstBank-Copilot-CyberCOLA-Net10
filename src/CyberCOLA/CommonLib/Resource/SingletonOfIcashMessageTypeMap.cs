using Com.Cybersoft4u.CommonLib.IcashMessage;
using Com.Cybersoft4u.CyberUtils.Utils;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Xml;
using System.Xml.Serialization;

namespace Com.Cybersoft4u.CommonLib.Resource
{
    public class SingletonOfIcashMessageTypeMap
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static SingletonOfIcashMessageTypeMap instance = new SingletonOfIcashMessageTypeMap();
        private static readonly object InstanceLock = new object();
        private readonly ConcurrentDictionary<string, MessageType> icashMessageTypeMaps = new ConcurrentDictionary<string, MessageType>();

        private SingletonOfIcashMessageTypeMap()
        {
            try
            {
                var configurationLoaderRoot = new ConfigurationLoaderRoot();
                IcashMessageTypeInfo icashMessageTypeInfo;
                string xmlFilePath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "icash-message-type-map-xml-file-path");
                if (string.IsNullOrEmpty(xmlFilePath))
                {
                    Log.Fatal("Can not find parameter[icash-message-type-map-xml-file-path]");
                    return;
                }
                XmlSerializer serializer = new XmlSerializer(typeof(IcashMessageTypeInfo));
                using (XmlReader reader = XmlReader.Create(xmlFilePath))
                {
                    icashMessageTypeInfo = (IcashMessageTypeInfo)serializer.Deserialize(reader);
                }
                foreach (var message in icashMessageTypeInfo.MessageType)
                {
                    icashMessageTypeMaps.TryAdd(message.RequestMessage, message);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"SingletonOfIcashMessageTypeMap Exception:{ex.Message}");
            }
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static SingletonOfIcashMessageTypeMap Instance
        {
            get
            {
                lock (InstanceLock)
                {
                    return instance ??= new SingletonOfIcashMessageTypeMap();
                }
            }
        }

        /// <summary>
        /// Gets the message type map.
        /// </summary>
        /// <returns>ConcurrentDictionary&lt;System.String, MessageType&gt;.</returns>
        public ConcurrentDictionary<string, MessageType> GetMessageTypeMap()
        {
            return (icashMessageTypeMaps);
        }
    }

}
