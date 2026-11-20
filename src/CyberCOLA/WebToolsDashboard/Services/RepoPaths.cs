namespace Com.Cybersoft4u.Test.Web.WebToolsDashboard.Services;

/// <summary>
/// Locates the repository (the folder holding CyberCOLA.sln and tools\). Works from src\ under dotnet run and
/// from publish\win-x64\WebToolsDashboard, since both sit inside the repo; Dashboard:RepoRoot overrides it.
/// </summary>
public sealed class RepoPaths
{
    public RepoPaths(IConfiguration configuration, IWebHostEnvironment environment)
    {
        var configured = configuration["Dashboard:RepoRoot"];
        Root = !string.IsNullOrWhiteSpace(configured)
            ? Path.GetFullPath(configured)
            : FindRoot(AppContext.BaseDirectory) ?? FindRoot(environment.ContentRootPath)
              ?? throw new InvalidOperationException(
                  "CyberCOLA.sln not found above the application folder; set Dashboard:RepoRoot in appsettings.json.");
    }

    public string Root { get; }

    public string ToolsDir => Path.Combine(Root, "tools");

    public string AuthTestLogDir => Path.Combine(Root, "LogFiles", "AuthTest");

    /// <summary>Full path of a catalog script; only plain file names inside tools\ are accepted.</summary>
    public string Script(string fileName)
    {
        if (fileName.IndexOfAny(['\\', '/', ':']) >= 0 || !fileName.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Invalid script name '{fileName}'.");
        }

        var path = Path.Combine(ToolsDir, fileName);
        return File.Exists(path) ? path : throw new FileNotFoundException($"Script not found: {path}");
    }

    private static string? FindRoot(string start)
    {
        for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "CyberCOLA.sln")) && Directory.Exists(Path.Combine(dir.FullName, "tools")))
            {
                return dir.FullName;
            }
        }

        return null;
    }
}
