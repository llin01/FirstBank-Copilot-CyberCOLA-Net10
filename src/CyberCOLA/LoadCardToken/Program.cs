// See https://aka.ms/new-console-template for more information
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.ISO8583.Category.CardType;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using System.Reflection;
try
{
    Console.WriteLine("Version: {0}", Environment.Version.ToString());
    Console.WriteLine($"Start Load Card Type and Token at {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
    Console.WriteLine("Check TM Assembly is exist");
    var assemblies = AssemblyUtils.GetAllAssemblies().ToList();
    var assembliesName = assemblies.Select(x => x.GetName().Name).ToList();
    if (assembliesName.Contains("TM"))
    {
        Console.WriteLine("TM Assembly is exist");
        Console.WriteLine("No Load Card Type and Token, Return to system");
        return;
    }
    Dictionary<Type, CardType> TypeMaps = new Dictionary<Type, CardType>();
    Dictionary<string, CardType> IdMaps = new Dictionary<string, CardType>();
    Dictionary<string, Type> TokenTypes = new Dictionary<string, Type>();
    string runtimePath = AppDomain.CurrentDomain.BaseDirectory;
    string assemblyPath = $"{runtimePath}\\TM.dll";
    var assembly = Assembly.LoadFrom(assemblyPath);
    var typesCard = assembly.GetTypes()
                            .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(CardType)))
                            .ToList();
    foreach (var type in typesCard)
    {
        var assemblyNmae = assembly.GetName().Name;
        var instance = Activator.CreateInstance(type) as CardType;
        if (instance == null) continue;
        TypeMaps[type] = instance;
        IdMaps[instance.Id()] = instance;
    }

    var typesToken = assembly.GetTypes()
                             .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(Token)))
                             .ToList();
    foreach (var type in typesToken)
    {
        var instance = Activator.CreateInstance(type) as Token;
        if (instance == null) continue;
        TokenTypes[instance.Id] = type;
    }

    Console.WriteLine($"TypesCard  Count [{typesCard.Count}]");
    Console.WriteLine($"TypesToken Count [{typesToken.Count}]");
}
catch (Exception ex)
{
    Console.WriteLine($"Exception : {ex.Message}");
}
finally
{
    Console.WriteLine($"End Load Card Type and Token at {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
}
