using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.Network.Client;
using Com.Cybersoft4u.CommonLib.Resource;
using NUnit.Framework;

namespace Com.Cybersoft4u.CyberCOLA.Test
{
    // ClientService tries the primary router and then the backup. A failed attempt used to keep its socket open
    // (the caller disconnects only on "00") and to leave the connect event set, so the backup could be judged
    // before it had a chance to connect.
    [TestFixture]
    public class ClientServiceFailoverTest
    {
        // Nothing listens on these loopback addresses: the router listener is bound to 127.0.0.1 only.
        private const string RefusingPrimary = "127.0.0.2";
        private const string RefusingBackup = "127.0.0.3";

        private TcpListener _router = null!;
        private ClientAuthRouterServices? _savedRouterServices;
        private string? _xmlPath;

        [SetUp]
        public void SetUp()
        {
            _router = new TcpListener(IPAddress.Loopback, 0);
            _router.Start();

            var info = SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo();
            if (info == null)
            {
                // No system parameter file in the test run: load a minimal one so only the routers are configured.
                _xmlPath = Path.Combine(Path.GetTempPath(), $"ClientServiceFailoverTest_{Guid.NewGuid():N}.xml");
                File.WriteAllText(_xmlPath, "<SystemParameterInfo><ClientAuthRouterServices /></SystemParameterInfo>");
                SingletonOfSystemParameterInfo.Instance.Load(_xmlPath);
                info = SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo();
            }

            _savedRouterServices = info.ClientAuthRouterServices;
        }

        [TearDown]
        public void TearDown()
        {
            SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices = _savedRouterServices!;
            _router.Dispose();
            if (_xmlPath != null)
            {
                File.Delete(_xmlPath);
            }
        }

        [Test]
        public void GetClientService_PrimaryRefuses_ConnectsToBackup()
        {
            UseRouters(RefusingPrimary, "127.0.0.1");
            var service = new ClientService(1);

            var result = service.GetClientService();
            try
            {
                Assert.That(Encoding.ASCII.GetString(result), Is.EqualTo("00"));
                using var accepted = _router.AcceptSocket();
                Assert.That(CurrentSocket(service).Connected, Is.True);
            }
            finally
            {
                service.DisconnectToServer();
            }
        }

        [Test]
        public void GetClientService_BothRefuse_ClosesTheSocket()
        {
            UseRouters(RefusingPrimary, RefusingBackup);
            var service = new ClientService(1);

            var result = service.GetClientService();

            Assert.That(Encoding.ASCII.GetString(result), Is.EqualTo("02"));
            Assert.That(CurrentSocket(service).SafeHandle.IsClosed, Is.True,
                "the socket of the last failed attempt is still open");
        }

        private void UseRouters(string primary, string backup)
        {
            var port = ((IPEndPoint)_router.LocalEndpoint).Port.ToString();
            SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo().ClientAuthRouterServices = new ClientAuthRouterServices
            {
                NCCCClientAuthRouterService = new NCCCClientAuthRouterService
                {
                    PrimaryServerName = primary,
                    BackupServerName = backup,
                    ListenerPortNumber = port,
                    ConnectionTimeOut = "2",
                    SendTimeOut = "2",
                    ReceiveTimeOut = "2",
                },
            };
        }

        private static Socket CurrentSocket(ClientService service)
        {
            var field = typeof(ClientService).GetField("_client", BindingFlags.NonPublic | BindingFlags.Instance)!;
            return (Socket)field.GetValue(service)!;
        }
    }
}
