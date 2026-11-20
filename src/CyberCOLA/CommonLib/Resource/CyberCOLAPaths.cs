#nullable enable
using System;
using System.IO;

namespace Com.Cybersoft4u.CommonLib.Resource
{
    /// <summary>
    /// Locates folders inside the CyberCOLA repository from wherever the running program sits (src bin folder,
    /// publish\win-x64\&lt;Service&gt;, or a dotnet run), instead of hard-coding the developer's E:\ path, which
    /// broke as soon as the repository was cloned elsewhere or run under Linux.
    /// </summary>
    public static class CyberCOLAPaths
    {
        private const string SolutionFileName = "CyberCOLA.sln";

        private static readonly Lazy<string?> RepoRootFromApp = new(() => FindRepoRoot(AppContext.BaseDirectory));

        /// <summary>The repository root (the folder holding CyberCOLA.sln) above the running program, or null.</summary>
        public static string? RepoRoot => RepoRootFromApp.Value;

        /// <summary>Walks up from <paramref name="startDirectory"/> to the first folder that holds CyberCOLA.sln.</summary>
        public static string? FindRepoRoot(string startDirectory)
        {
            for (var dir = new DirectoryInfo(startDirectory); dir is not null; dir = dir.Parent)
            {
                if (File.Exists(Path.Combine(dir.FullName, SolutionFileName)))
                {
                    return dir.FullName;
                }
            }

            return null;
        }

        /// <summary>
        /// The first of <paramref name="repoRelativeCandidates"/> (paths relative to the repository root, '/' as
        /// separator) that exists; otherwise <paramref name="fallbackUnderApp"/> under the program folder if that
        /// exists; otherwise the first candidate (or the fallback when no repository was found), so a caller that
        /// reports a missing folder still shows a meaningful path.
        /// </summary>
        public static string ResolveDirectory(string fallbackUnderApp, params string[] repoRelativeCandidates) =>
            ResolveDirectoryFrom(RepoRoot, AppContext.BaseDirectory, fallbackUnderApp, repoRelativeCandidates);

        /// <summary><see cref="ResolveDirectory"/> with an explicit repository root and program folder (for tests).</summary>
        public static string ResolveDirectoryFrom(string? repoRoot, string appDirectory, string fallbackUnderApp,
                                                  params string[] repoRelativeCandidates)
        {
            if (repoRoot is not null)
            {
                foreach (var candidate in repoRelativeCandidates)
                {
                    var path = Path.Combine(repoRoot, ToNative(candidate));
                    if (Directory.Exists(path))
                    {
                        return path;
                    }
                }
            }

            var local = Path.Combine(appDirectory, ToNative(fallbackUnderApp));
            if (Directory.Exists(local) || repoRoot is null || repoRelativeCandidates.Length == 0)
            {
                return local;
            }

            return Path.Combine(repoRoot, ToNative(repoRelativeCandidates[0]));
        }

        private static string ToNative(string relative) =>
            relative.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
    }
}
