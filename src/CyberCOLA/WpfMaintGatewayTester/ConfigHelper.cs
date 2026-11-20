using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Com.Cybersoft4u.Test.Wpf.WpfMaintGatewayTester
{
    public static class ConfigHelper
    {
        public static IConfiguration? Configuration { get; private set; }

        static ConfigHelper()
        {
            try
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                var builder = new ConfigurationBuilder()
                    .SetBasePath(basePath)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

                Configuration = builder.Build();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Configuration load error: {ex.Message}");
            }
        }

        public static string GetXmlTestDataAsJson()
        {
            if (Configuration == null) return "Error: Configuration not initialized.";

            try
            {
                string? xmlPath = Configuration["GatewaySettings:TestDataFilePath"];
                if (string.IsNullOrEmpty(xmlPath))
                {
                    return "Error: TestDataFilePath not defined in appsettings.json";
                }

                if (!File.Exists(xmlPath))
                {
                    return $"Error: XML file not found at path: {xmlPath}";
                }

                XDocument doc = XDocument.Load(xmlPath);
                string json = JsonConvert.SerializeXNode(doc, Formatting.Indented);

                // --- FIX: Unwrap the root node (WebFileMaintDataXml) ---
                JObject root = JObject.Parse(json);
                if (root.ContainsKey("WebFileMaintDataXml"))
                {
                    var innerContent = root["WebFileMaintDataXml"];
                    return innerContent?.ToString(Formatting.Indented) ?? "Error: WebFileMaintDataXml node not found.";
                }

                return json;
            }
            catch (Exception ex)
            {
                return $"Error converting XML to JSON: {ex.Message}";
            }
        }
    }
}
