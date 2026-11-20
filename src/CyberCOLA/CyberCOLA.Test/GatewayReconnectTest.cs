using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Com.Cybersoft4u.Interchange;
using Com.Cybersoft4u.ServiceProvider;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.ClientMonitor;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.FISC;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.ICASH;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.IDVerify;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.IPASS;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.NCCC;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.QRCode;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.SVCS;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.TWIN;
using Com.Cybersoft4u.Yetty.Channel;
using NUnit.Framework;

namespace Com.Cybersoft4u.CyberCOLA.Test
{
    // Runs the real gateways over loopback and drops their external session. The gateways read appsettings.json
    // next to the entry assembly, so each test writes one there and deletes it afterwards.
    //  - Active GWs (FISC, FISC IDVerify, NCCC, NCCC QRCode, TWIN) connect out: after a drop they must wait the
    //    configured interval and reconnect from the session's own local port.
    //  - Passive GWs (ICASH, IPASS, SVCS) are connected to: after a drop they must forget the old channel and
    //    accept the host reconnecting from the same (allow-listed) endpoint.
    [TestFixture]
    [NonParallelizable]
    public class GatewayReconnectTest
    {
        private const int ReconnectIntervalMs = 500;

        public sealed record ActiveGateway(string Name, string Section, string Prefix,
                                           Func<(EntryPoint Service, Action Disconnect, Func<int> ChannelCount)> Create,
                                           Action<EntryPoint?> SetResource)
        {
            public override string ToString() => Name;
        }

        public sealed record PassiveGateway(string Name, Func<(EntryPoint Service, Func<int> ChannelCount)> Create,
                                            Action<EntryPoint?> SetResource)
        {
            public override string ToString() => Name;
        }

        public static IEnumerable<ActiveGateway> ActiveGateways()
        {
            yield return new ActiveGateway("NCCC", "NcccServiceConfig", "nccc",
                () => { var g = new CyberGWNCCC(); return (g, g.Disconnect, () => g.GetChannels().Count); },
                g => CyberGWNCCCResource.CyberGWNCCC = (CyberGWNCCC)g!);
            yield return new ActiveGateway("NCCC QRCode", "NcccServiceConfig", "nccc",
                () => { var g = new CyberGWNCCCQRCode(); return (g, g.Disconnect, () => g.GetChannels().Count); },
                g => CyberGWNCCCQRCodeResource.CyberGWNCCCQRCode = (CyberGWNCCCQRCode)g!);
            yield return new ActiveGateway("FISC", "FiscServiceConfig", "fisc",
                () => { var g = new CyberGWFISC(); return (g, g.Disconnect, () => g.GetChannels().Count); },
                g => CyberGWFISCResource.CyberGWFISC = (CyberGWFISC)g!);
            yield return new ActiveGateway("FISC IDVerify", "FiscServiceConfig", "fisc",
                () => { var g = new CyberGWFISCIDVerify(); return (g, g.Disconnect, () => g.GetChannels().Count); },
                g => CyberGWFISCIDVerifyResource.CyberGWFISCIDVerify = (CyberGWFISCIDVerify)g!);
            yield return new ActiveGateway("TWIN", "TwinServiceConfig", "twin",
                () => { var g = new CyberGWTWIN(); return (g, g.Disconnect, () => g.GetChannels().Count); },
                g => CyberGWTWINResource.CyberGWTWIN = (CyberGWTWIN)g!);
        }

        public static IEnumerable<PassiveGateway> PassiveGateways()
        {
            yield return new PassiveGateway("SVCS",
                () => { var g = new CyberGWSVCS(); return (g, () => g.GetChannels().Count); },
                g => CyberGWSVCSResource.CyberGWSVCS = (CyberGWSVCS)g!);
            yield return new PassiveGateway("ICASH",
                () => { var g = new CyberGWICASH(); return (g, () => g.GetChannels().Count); },
                g => CyberGWICASHResource.CyberGWICASH = (CyberGWICASH)g!);
            yield return new PassiveGateway("IPASS",
                () => { var g = new CyberGWIPASS(); return (g, () => g.GetChannels().Count); },
                g => CyberGWIPASSResource.CyberGWIPASS = (CyberGWIPASS)g!);
        }

        private string _settingsPath = null!;
        private readonly List<IDisposable> _cleanup = new();
        private Action? _stopGateway;

        [SetUp]
        public void SetSettingsPath() =>
            _settingsPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()!.Location)!, "appsettings.json");

        [TearDown]
        public void StopGateway()
        {
            _stopGateway?.Invoke();
            _stopGateway = null;
            _cleanup.ForEach(d => d.Dispose());
            _cleanup.Clear();
            File.Delete(_settingsPath);
        }

        [TestCaseSource(nameof(ActiveGateways))]
        public async Task DroppedSession_ReconnectsFromOriginalLocalPortAfterConfiguredInterval(ActiveGateway gateway)
        {
            var (host, localPorts, _) = StartActive(gateway, 1);
            var localPort = localPorts[0];

            using var first = await AcceptAsync(host, TimeSpan.FromSeconds(10));
            Assert.That(((IPEndPoint)first.Client.RemoteEndPoint!).Port, Is.EqualTo(localPort));

            // Reset instead of a graceful close, so no TIME_WAIT is left on the same address pair.
            first.Client.LingerState = new LingerOption(true, 0);
            var dropped = Stopwatch.StartNew();
            first.Close();

            using var second = await AcceptAsync(host, TimeSpan.FromSeconds(8));
            dropped.Stop();

            Assert.That(((IPEndPoint)second.Client.RemoteEndPoint!).Port, Is.EqualTo(localPort));
            // Well under the 10 s default, so the configured value was used rather than the fallback.
            Assert.That(dropped.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(ReconnectIntervalMs - 50).And.LessThan(5000));
        }

        // Disconnect() drops every session, and each drop removes its channel from the list being walked.
        // The monitor's STOP command clears the resource first, so no session may come back, and Disconnect() must
        // not wait out the (here long) reconnect interval in every revive handler before returning.
        [TestCaseSource(nameof(ActiveGateways))]
        public async Task Disconnect_ResourceCleared_DropsAllPromptlyWithoutReconnecting(ActiveGateway gateway)
        {
            const int sessions = 4;
            const int longIntervalMs = 3000;
            var (host, _, gw) = StartActive(gateway, sessions, longIntervalMs);
            for (var i = 0; i < sessions; i++)
            {
                _cleanup.Add(await AcceptAsync(host, TimeSpan.FromSeconds(10)));
            }
            Assert.That(WaitFor(() => gw.ChannelCount() == sessions), Is.True, "sessions were not registered");

            gateway.SetResource(null);
            var watch = Stopwatch.StartNew();
            Assert.That(() => gw.Disconnect(), Throws.Nothing);
            Assert.That(watch.Elapsed, Is.LessThan(TimeSpan.FromMilliseconds(longIntervalMs / 2)));
            Assert.That(WaitFor(() => gw.ChannelCount() == 0), Is.True, "sessions were not removed");

            using var cts = new CancellationTokenSource(longIntervalMs * 2);
            Assert.That(async () => (await host.AcceptTcpClientAsync(cts.Token)).Dispose(),
                        Throws.InstanceOf<OperationCanceledException>(), "a stopped gateway reconnected");
        }

        // A service stopped right after starting reaches Stop() before the monitor's connect has returned.
        [Test]
        public void ClientMonitorStop_BeforeConnecting_DoesNotThrow()
        {
            var monitor = new GatewayClientMonitorService("test", new IPEndPoint(IPAddress.Loopback, 1), new IPEndPoint(IPAddress.Loopback, 0),
                                                          GatewayClientAuthServiceType.SVCS, false);

            Assert.That(() => monitor.Stop(), Throws.Nothing);
        }

        [Test]
        public void ClientAuthMonitorStop_BeforeConnecting_DoesNotThrow()
        {
            var monitor = new ClientAuthClientMonitorService("test", new IPEndPoint(IPAddress.Loopback, 1), new IPEndPoint(IPAddress.Loopback, 0),
                                                             GatewayClientAuthServiceType.SVCS, false);

            Assert.That(() => monitor.Stop(), Throws.Nothing);
        }

        private (TcpListener Host, int[] LocalPorts, (EntryPoint Service, Action Disconnect, Func<int> ChannelCount) Gateway)
            StartActive(ActiveGateway gateway, int sessions, int reconnectIntervalMs = ReconnectIntervalMs)
        {
            var host = new TcpListener(IPAddress.Loopback, 0);
            host.Start();
            _cleanup.Add(host);
            var localPorts = Enumerable.Range(0, sessions).Select(_ => GetFreePort()).ToArray();
            var hostPort = ((IPEndPoint)host.LocalEndpoint).Port;
            var p = gateway.Prefix;
            WriteSettings($$"""
                {
                  "{{gateway.Section}}": {
                    "{{p}}-service-target-ip": "127.0.0.1",
                    "{{p}}-service-target-ports": "{{string.Join(",", Enumerable.Repeat(hostPort, sessions))}}",
                    "{{p}}-service-local-ip": "127.0.0.1",
                    "{{p}}-service-local-ports": "{{string.Join(",", localPorts)}}",
                    "{{p}}-service-sessions": "{{sessions}}",
                    "{{p}}-service-reconnect-interval-ms": "{{reconnectIntervalMs}}",
                    "{{p}}-service-maintenance-local-ports": "",
                    "{{p}}-service-auto-signon-flag": "false",
                    "{{p}}-service-auto-echo-flag": "false",
                    "{{p}}-service-maintenance-auto-echo-flag": "false"
                  }
                }
                """);

            var gw = gateway.Create();
            gateway.SetResource(gw.Service);
            // Clearing the resource first makes the revives that Disconnect() triggers give up.
            _stopGateway = () =>
            {
                gateway.SetResource(null);
                gw.Disconnect();
                gw.Service.Stop();
            };
            gw.Service.Start();
            return (host, localPorts, gw);
        }

        [TestCaseSource(nameof(PassiveGateways))]
        public void DroppedHost_IsForgottenAndAcceptedAgainFromSameEndpoint(PassiveGateway gateway)
        {
            var bindPort = GetFreePort();
            var hostPort = GetFreePort();
            WriteSettings($$"""
                {
                  "ServerServiceConfig": {
                    "server-service-bind-ip": "127.0.0.1",
                    "server-service-bind-port": "{{bindPort}}",
                    "server-service-backlog": "16",
                    "server-allow-connection-remote-endpoints": "127.0.0.1:{{hostPort}}",
                    "server-service-auto-signon-flag": "false",
                    "server-service-auto-echo-flag": "false"
                  }
                }
                """);

            var (service, channelCount) = gateway.Create();
            gateway.SetResource(service);
            _stopGateway = () =>
            {
                gateway.SetResource(null);
                service.Stop();
            };
            service.Start();

            var first = ConnectFrom(hostPort, bindPort);
            Assert.That(WaitFor(() => channelCount() == 1), Is.True, "first connection was not registered");

            // Socket.Close(0) resets; TcpClient.Close() shuts down first, leaving this side in TIME_WAIT.
            first.Client.Close(0);
            Assert.That(WaitFor(() => channelCount() == 0), Is.True, "dropped connection was not removed");

            ConnectFrom(hostPort, bindPort);
            Assert.That(WaitFor(() => channelCount() == 1), Is.True, "reconnection was not registered");
        }

        private void WriteSettings(string json) => File.WriteAllText(_settingsPath, json);

        private TcpClient ConnectFrom(int localPort, int remotePort)
        {
            // ReuseAddress like the e-ticket hosts' clients, so the same local port can be bound again right after a drop.
            var client = new TcpClient(AddressFamily.InterNetwork);
            _cleanup.Add(client);
            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.Client.Bind(new IPEndPoint(IPAddress.Loopback, localPort));
            client.Connect(IPAddress.Loopback, remotePort);
            return client;
        }

        private static async Task<TcpClient> AcceptAsync(TcpListener host, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource(timeout);
            return await host.AcceptTcpClientAsync(cts.Token);
        }

        private static bool WaitFor(Func<bool> condition)
        {
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (DateTime.UtcNow < deadline)
            {
                if (condition()) return true;
                Thread.Sleep(50);
            }
            return condition();
        }

        private static int GetFreePort()
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            return port;
        }
    }
}
