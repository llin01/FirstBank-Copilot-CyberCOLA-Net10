using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CyberUtils.Utils;
using NLog;
using System;
using System.Xml;
using System.Xml.Serialization;

namespace Com.Cybersoft4u.CommonLib.Resource
{
    public class SingletonOfSystemParameterInfo
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private static SingletonOfSystemParameterInfo instance = new SingletonOfSystemParameterInfo();
        private static readonly object InstanceLock = new object();
        private SystemParameterInfo _systemParameterInfo;

        private SingletonOfSystemParameterInfo()
        {
            try
            {
                var configurationLoaderRoot = new ConfigurationLoaderRoot();
                string xmlFilePath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "first-bank-system-parameter-info-xml-file-path");
                if (String.IsNullOrEmpty(xmlFilePath))
                {
                    Log.Fatal("Can not find parameter[first-bank-system-parameter-info-xml-file-path]");
                    return;
                }
                XmlSerializer serializer = new XmlSerializer(typeof(SystemParameterInfo));
                using (XmlReader reader = XmlReader.Create(xmlFilePath))
                {
                    _systemParameterInfo = (SystemParameterInfo)serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"SingletonOfSystemParameterInfo Error:{ex.Message}");
            }

        }

        private SingletonOfSystemParameterInfo(string xmlFilePath)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(SystemParameterInfo));
                using (XmlReader reader = XmlReader.Create(xmlFilePath))
                {
                    _systemParameterInfo = (SystemParameterInfo)serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"SingletonOfSystemParameterInfo(path) Error:{ex.Message}");
            }
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static SingletonOfSystemParameterInfo Instance
        {
            get
            {
                lock (InstanceLock)
                {
                    return instance ??= new SingletonOfSystemParameterInfo();
                }
            }
        }

        /// <summary>
        /// Force reload SystemParameterInfo from a specific XML file path.
        /// </summary>
        public void Load(string xmlFilePath)
        {
            lock (InstanceLock)
            {
                instance = new SingletonOfSystemParameterInfo(xmlFilePath);
            }
        }


        /// <summary>
        /// Gets the system parameter information.
        /// </summary>
        /// <returns>SystemParameterInfo.</returns>
        public SystemParameterInfo GetSystemParameterInfo()
        {
            return (_systemParameterInfo);
        }
    }

}
