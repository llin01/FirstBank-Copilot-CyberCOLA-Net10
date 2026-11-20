using System.Net;
using System.Net.Sockets;
using Com.Cybersoft4u.ServiceProvider;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.ICASH.Transaction;
using Com.Cybersoft4u.TM.ISO8583.IPASS.Transaction;
using Com.Cybersoft4u.TM.ISO8583.SVCS.Transaction;
using Com.Cybersoft4u.Yetty.Channel;
using NUnit.Framework;

namespace Com.Cybersoft4u.CyberCOLA.Test
{
    // After the monitor's STOP command clears a passive GW, the Auth can still answer a request that was in
    // flight. The COLA send-route handler must drop that response and free its COLA channel, or the channel
    // stays reserved for good. Runs the real COLA service over loopback; the test client plays the Auth.
    [TestFixture]
    [NonParallelizable]
    public class PassiveColaDropAfterStopTest
    {
        public sealed record PassiveCola(string Name,
                                         Func<(EntryPoint Service, Func<IChannel?> TakeIdle, Func<IChannel, bool> IsTake)> Create,
                                         Action ClearGateway,
                                         Func<byte[]> Response)
        {
            public override string ToString() => Name;
        }

        public static IEnumerable<PassiveCola> PassiveColas()
        {
            yield return new PassiveCola("SVCS",
                () => { var c = new SvcsColaService(); return (c, c.GetIdleChannel, c.IsTake); },
                () => CyberGWSVCSResource.CyberGWSVCS = null!,
                () => Response(new SvcsResponse()));
            yield return new PassiveCola("ICASH",
                () => { var c = new IcashColaService(); return (c, c.GetIdleChannel, c.IsTake); },
                () => CyberGWICASHResource.CyberGWICASH = null!,
                () => Response(new IcashResponse()));
            yield return new PassiveCola("IPASS",
                () => { var c = new IpassColaService(); return (c, c.GetIdleChannel, c.IsTake); },
                () => CyberGWIPASSResource.CyberGWIPASS = null!,
                () => Response(new IpassResponse(), withRrn: false));
        }

        // A minimal 0110 response with the fields the COLA handlers read for the RelationId; the default header.
        // IPASS defines no field 37.
        private static byte[] Response(Iso8583 message, bool withRrn = true)
        {
            message.Mti("0110");
            message.Val(FIELD.F_7, "1104103000");
            message.Val(FIELD.F_11, "000001");
            if (withRrn) message.Val(FIELD.F_37, "000000000001");
            message.Val(FIELD.F_39, "00");
            message.Val(FIELD.F_41, "00000001");
            var body = message.Encode().Val();
            // The COLA link frames every message with a 2-byte big-endian length.
            return new[] { (byte)(body.Length >> 8), (byte)body.Length }.Concat(body).ToArray();
        }

        private string _settingsPath = null!;
        private EntryPoint? _service;
        private TcpClient? _auth;

        [SetUp]
        public void SetSettingsPath() =>
            _settingsPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly()!.Location)!, "appsettings.json");

        [TearDown]
        public void Stop()
        {
            _auth?.Dispose();
            _service?.Stop();
            File.Delete(_settingsPath);
        }

        [TestCaseSource(nameof(PassiveColas))]
        public void ResponseAfterStop_IsDroppedAndFreesItsColaChannel(PassiveCola cola)
        {
            var port = GetFreePort();
            File.WriteAllText(_settingsPath, $$"""
                {
                  "ColaServiceConfig": {
                    "cola-service-bind-ip": "127.0.0.1",
                    "cola-service-bind-port": "{{port}}",
                    "cola-service-backlog": "16",
                    "cola-service-auto-echo-flag": "false",
                    "cola-service-idle-channel-wait-ms": "300"
                  }
                }
                """);
            cola.ClearGateway();
            var (service, takeIdle, isTake) = cola.Create();
            _service = service;
            service.Start();

            _auth = new TcpClient(AddressFamily.InterNetwork);
            _auth.Connect(IPAddress.Loopback, port);

            // Reserve the channel the way a dispatched request does, then answer it after the STOP.
            IChannel? channel = null;
            Assert.That(WaitFor(() => (channel = takeIdle()) != null), Is.True, "the Auth connection did not become an idle COLA channel");
            Assert.That(isTake(channel!), Is.True);

            var response = cola.Response();
            _auth.GetStream().Write(response, 0, response.Length);

            Assert.That(WaitFor(() => !isTake(channel!)), Is.True, "the dropped response did not free its COLA channel");
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
