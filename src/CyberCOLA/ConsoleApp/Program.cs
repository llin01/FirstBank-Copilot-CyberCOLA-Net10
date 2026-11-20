using Com.Cybersoft4u.CyberUtils.Utils;
using Microsoft.Extensions.Configuration;

const string testKey = "configuration";

var configuration = new ConfigurationManager();
Console.WriteLine("Version: {0}", Environment.Version.ToString());
Console.WriteLine($"1{configuration[testKey]}");

configuration.AddInMemoryCollection(new Dictionary<string, string?>()
{
    { testKey, "configuration" }
});

Console.WriteLine($"2{configuration[testKey]}");
var key = "cola-service-local-port-start";
var key1 = "cola-service-local-port-start1";

var _configurationLoaderRoot = new ConfigurationLoaderRoot();
Console.WriteLine($"key[{key}] - value[{_configurationLoaderRoot.Get(key)}]");
_configurationLoaderRoot.Set(key, "1000001");
Console.WriteLine($"key[{key}] - value[{_configurationLoaderRoot.Get(key)}]");
_configurationLoaderRoot.Set(key1, "22222000001");
Console.WriteLine($"key[{key1}] - value[{_configurationLoaderRoot.Get(key1)}]");

Console.ReadLine();
