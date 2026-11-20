using System.Runtime.CompilerServices;
using Com.Cybersoft4u.CommonLib.Resource;
using NUnit.Framework;

namespace Com.Cybersoft4u.CyberCOLA.Test
{
    [TestFixture]
    public class CyberCOLAPathsTest
    {
        private string _root = string.Empty;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "CyberCOLAPathsTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }

        [Test]
        public void FindRepoRoot_FromInsideTheRepository_ReturnsFolderWithSolution()
        {
            var root = CyberCOLAPaths.FindRepoRoot(Path.GetDirectoryName(SourceFile())!);

            Assert.That(root, Is.Not.Null);
            Assert.That(File.Exists(Path.Combine(root!, "CyberCOLA.sln")), Is.True);
        }

        [Test]
        public void FindRepoRoot_FromNestedFolderOfAFakeRepo_WalksUpToIt()
        {
            File.WriteAllText(Path.Combine(_root, "CyberCOLA.sln"), string.Empty);
            var nested = Directory.CreateDirectory(Path.Combine(_root, "publish", "win-x64", "CyberGWNCCC")).FullName;

            Assert.That(CyberCOLAPaths.FindRepoRoot(nested), Is.EqualTo(new DirectoryInfo(_root).FullName));
        }

        [Test]
        public void ResolveDirectoryFrom_FirstExistingCandidateWins()
        {
            Directory.CreateDirectory(Path.Combine(_root, "publish", "Config", "Sim", "Data"));
            var app = Directory.CreateDirectory(Path.Combine(_root, "app")).FullName;

            var path = CyberCOLAPaths.ResolveDirectoryFrom(_root, app, "Data", "src/CyberCOLA/Sim/Data", "publish/Config/Sim/Data");

            Assert.That(path, Is.EqualTo(Path.Combine(_root, "publish", "Config", "Sim", "Data")));
        }

        [Test]
        public void ResolveDirectoryFrom_NoCandidateButAppFolderExists_UsesAppFolder()
        {
            var app = Directory.CreateDirectory(Path.Combine(_root, "app")).FullName;
            Directory.CreateDirectory(Path.Combine(app, "Data"));

            var path = CyberCOLAPaths.ResolveDirectoryFrom(_root, app, "Data", "src/CyberCOLA/Sim/Data");

            Assert.That(path, Is.EqualTo(Path.Combine(app, "Data")));
        }

        [Test]
        public void ResolveDirectoryFrom_NothingExists_ReturnsFirstCandidateForAMeaningfulMessage()
        {
            var app = Directory.CreateDirectory(Path.Combine(_root, "app")).FullName;

            var path = CyberCOLAPaths.ResolveDirectoryFrom(_root, app, "Data", "src/CyberCOLA/Sim/Data", "publish/Config/Sim/Data");

            Assert.That(path, Is.EqualTo(Path.Combine(_root, "src", "CyberCOLA", "Sim", "Data")));
        }

        [Test]
        public void ResolveDirectoryFrom_NoRepository_UsesAppFolder()
        {
            var app = Directory.CreateDirectory(Path.Combine(_root, "app")).FullName;

            var path = CyberCOLAPaths.ResolveDirectoryFrom(null, app, "Log", "LogFiles");

            Assert.That(path, Is.EqualTo(Path.Combine(app, "Log")));
        }

        private static string SourceFile([CallerFilePath] string path = "") => path;
    }
}
