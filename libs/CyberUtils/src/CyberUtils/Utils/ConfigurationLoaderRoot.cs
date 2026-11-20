// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="ConfigurationLoaderRoot.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the ConfigurationLoaderRoot type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Reflection;

namespace Com.Cybersoft4u.CyberUtils.Utils
{
    #region DEPENDENCY

    #endregion

    /// <summary>
    /// Provides methods to load, read, and update application configuration from appsettings.json.
    /// </summary>
    public class ConfigurationLoaderRoot
    {
        #region NLog
        private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private readonly IConfigurationRoot _configurationRoot;

        /// <summary>
        /// Initializes a new instance of the ConfigurationLoaderRoot class and loads appsettings.json.
        /// </summary>
        public ConfigurationLoaderRoot()
        {
            try
            {
                var builder = new ConfigurationBuilder()
                              //.SetBasePath(Directory.GetCurrentDirectory())
                              .SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location))
                              .AddJsonFile($"appsettings.json", true, false);
                this._configurationRoot = builder.Build();

            }
            catch (System.Exception ex)
            {
                Log.Fatal(ex, $"Create ConfigurationLoaderRoot Exception : {ex.Message}");
            }

        }
        private static string ResolvePathIfNotFound(string val)
        {
            if (string.IsNullOrEmpty(val)) return val;

            // Check if it looks like a path (contains slashes) and doesn't exist locally
            if ((val.Contains("\\") || val.Contains("/")) && !System.IO.File.Exists(val) && !System.IO.Directory.Exists(val))
            {
                string fileName = System.IO.Path.GetFileName(val);
                if (string.IsNullOrEmpty(fileName)) return val;

                var current = new System.IO.DirectoryInfo(AppContext.BaseDirectory);
                for (int i = 0; i < 5; i++)
                {
                    if (current == null) break;

                    string checkPath = System.IO.Path.Combine(current.FullName, fileName);
                    if (System.IO.File.Exists(checkPath) || System.IO.Directory.Exists(checkPath))
                    {
                        Log.Info($"Auto-resolved configured path '{val}' to '{checkPath}'");
                        return checkPath;
                    }

                    string checkConfig = System.IO.Path.Combine(current.FullName, "Config", fileName);
                    if (System.IO.File.Exists(checkConfig))
                    {
                        Log.Info($"Auto-resolved configured path '{val}' to '{checkConfig}'");
                        return checkConfig;
                    }

                    string checkData = System.IO.Path.Combine(current.FullName, "Data", fileName);
                    if (System.IO.File.Exists(checkData) || System.IO.Directory.Exists(checkData))
                    {
                        Log.Info($"Auto-resolved configured path '{val}' to '{checkData}'");
                        return checkData;
                    }

                    string checkSrcConfig = System.IO.Path.Combine(current.FullName, "src", "CyberCOLA", "Config", fileName);
                    if (System.IO.File.Exists(checkSrcConfig))
                    {
                        Log.Info($"Auto-resolved configured path '{val}' to '{checkSrcConfig}'");
                        return checkSrcConfig;
                    }

                    current = current.Parent;
                }
            }
            return val;
        }

        /// <summary>
        /// get value by key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string Get(string key)
        {
            var val = this._configurationRoot[key] == null
                       ? null
                       : this._configurationRoot[key];
            return ResolvePathIfNotFound(val);
        }
        /// <summary>
        /// get value by sectionName and key
        /// </summary>
        /// <param name="sectionName"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public string Get(string sectionName, string key)
        {
            if (this._configurationRoot.GetSection(sectionName) == null) return null;
            var val = this._configurationRoot.GetSection(sectionName).GetSection(key).Value == null
                       ? null
                       : this._configurationRoot.GetSection(sectionName).GetSection(key).Value;
            return ResolvePathIfNotFound(val);
        }
        /// <summary>
        /// set value by key
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Set(string key, string value)
        {
            try
            {
                if (ObjectUtils.IsNull(key) || ObjectUtils.IsNull(value))
                {
                    throw new ArgumentNullException();
                }
                if (ObjectUtils.IsNull(this._configurationRoot[key]))
                {
                    Log.Info($"appsettings.json key[{key}] no exist");
                    this._configurationRoot[key] = value;
                }
                else
                {
                    this._configurationRoot[key] = value;
                }

                var jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "appSettings.json");
                string jsonString = File.ReadAllText(jsonFilePath);
                dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonString);
                if (jsonObj != null)
                {
                    jsonObj[key] = value;
                    string output = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText(jsonFilePath, output);
                    Log.Info($"Write appsettings.json sucessful");
                    return;
                }
                Log.Info($"Write appsettings.json failed");
            }
            catch (System.Exception ex)
            {
                Log.Fatal(ex, $"Set Exception : {ex.Message}");
            }
        }
        /// <summary>
        /// set value by sectionName, key
        /// </summary>
        /// <param name="sectionName"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Set(string sectionName, string key, string value)
        {
            try
            {
                if (ObjectUtils.IsNull(sectionName) || ObjectUtils.IsNull(key) || ObjectUtils.IsNull(value))
                {
                    throw new ArgumentNullException();
                }
                if (ObjectUtils.IsNull(this._configurationRoot.GetSection(sectionName)))
                {
                    Log.Fatal($"Section Nname[{sectionName}] not exist, no set value[{value}]");
                    return;
                }
                if (ObjectUtils.IsNull(this._configurationRoot.GetSection(sectionName).GetSection(key).Value))
                {
                    Log.Info($"appsettings.json sectionName[{sectionName}] key[{key}] no exist");
                    this._configurationRoot.GetSection(sectionName).GetSection(key).Value = value;
                }
                else
                {
                    this._configurationRoot.GetSection(sectionName).GetSection(key).Value = value;
                }

                var jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "appSettings.json");
                string jsonString = File.ReadAllText(jsonFilePath);
                dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonString);
                if (jsonObj != null)
                {
                    jsonObj[sectionName][key] = value;
                    string output = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText(jsonFilePath, output);
                    Log.Info($"Write appsettings.json sucessful");
                    return;
                }
                Log.Info($"Write appsettings.json failed");
            }
            catch (System.Exception ex)
            {
                Log.Fatal(ex, $"Set Exception : {ex.Message}");
            }
        }
    }
}
