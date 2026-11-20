using System.Net;
using System.Net.Sockets;
using System.Text;
using Com.Cybersoft4u.CommonLib.Network.Server;
using NUnit.Framework;

namespace Com.Cybersoft4u.CyberCOLA.Test
{
    // The HSM simulator over loopback, sent requests the way the vendor client (SocketClient.dll) frames them.
    // Synthetic test card number only - never real card data.
    [TestFixture]
    public class HsmSimulatorFuncPrefixTest
    {
        // bizId(8) + bankId(3) + cardNo(16) + expiry(4) + service code(3) = 34, the Generate CVV (01) fields.
        private const string GenerateCvvFields = "MASTER  " + "007" + "4000000000000002" + "2806" + "201";

        private static int FreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static string Send(int port, string request)
        {
            using var client = new TcpClient();
            client.Connect(IPAddress.Loopback, port);
            var stream = client.GetStream();
            var bytes = Encoding.ASCII.GetBytes(request);
            stream.Write(bytes, 0, bytes.Length);
            stream.ReadTimeout = 5000;
            using var response = new MemoryStream();
            var buffer = new byte[256];
            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                response.Write(buffer, 0, read); // the simulator closes the connection after one reply
            }
            return Encoding.ASCII.GetString(response.ToArray());
        }

        private static string GenerateCvvOnFreshServer()
        {
            using var cts = new CancellationTokenSource();
            var server = new AsynTcpServer(cts.Token) { PortNumber = FreePort(), SessionNumber = 10 };
            server.StartServer();
            try
            {
                return Send(server.PortNumber, "\x01" + GenerateCvvFields);
            }
            finally
            {
                server.StopServer();
            }
        }

        [Test]
        public void GenerateCvv_WithFunctionNumberPrefix_IsAnsweredAsFunction01()
        {
            var response = GenerateCvvOnFreshServer();

            // What the vendor client parses: binary return code 0x00, 8-digit reason code, 3-digit CVV
            // (checked against SocketClient.dll's ProHSM.Func01). A 3D CAVV (09) reply would be 13 ASCII characters.
            Assert.That(response, Does.Match("^\0" + "00000000" + "[0-9]{3}$"));
        }

        private static string SendOnFreshServer(string request)
        {
            using var cts = new CancellationTokenSource();
            var server = new AsynTcpServer(cts.Token) { PortNumber = FreePort(), SessionNumber = 10 };
            server.StartServer();
            try
            {
                return Send(server.PortNumber, request);
            }
            finally
            {
                server.StopServer();
            }
        }

        [Test]
        public void GenerateCavv_FunctionByteIsTab_StillSendsBinaryReturnCode()
        {
            // 0x09 is white space: Trim() used to drop it before it was read, so the reply kept the ASCII "00"
            // and the vendor client read return code 30.
            var response = SendOnFreshServer("\x09" + "MASTER  " + "007" + "1" + "4000000000000002" + "2806" + "201");

            Assert.That(response, Does.Match("^\0" + "00000000" + "[0-9]{3}$"));
        }

        [Test]
        public void EncryptBankPin_ReturnsTheSixCharacterPinOffsetTheClientReads()
        {
            // Fields: bizId, bankId, cardNo, pvkIndex, ppkBizId, ppkKeyId, pinBlockFormat, clearPIN (46 characters).
            var response = SendOnFreshServer("\x03" + "MASTER  " + "007" + "4000000000000002" + "1" + "PPKBIZ01" + "001" + "1" + "123456");

            // Return code, 8-digit reason code, 16-character PIN block, 4-digit PVV, 6-digit PIN offset.
            Assert.That(response, Does.Match("^\0" + "00000000" + "[0-9A-F]{16}" + "[0-9]{4}" + "[0-9]{6}$"));
        }

        // Func07 (Verify ARQC) as SocketClient.dll 1.0.0.6 frames it: bizId(8) bankId(3) keyIndex(2) cdolDataLength(2,
        // decimal character count) cdolData(n) arqc(16) pan(18) cvn(2), 52 + n bytes after the function byte.
        // 1.0.0.3 always copied 96 CDOL characters, so any other length was rejected in the client itself.
        // The client cannot be called here: its config.ini path is fixed and points at the real HSM.
        private static string VerifyArqcRequest(string cdolData, string arqc = "0123456789ABCDEF")
        {
            return "\x07" + "VISA    " + "007" + "01" + cdolData.Length.ToString("D2") + cdolData + arqc
                   + "400000000000000200" + "10";
        }

        [TestCase(58)]
        [TestCase(96)]
        public void VerifyArqc_VariableCdolLength_IsVerified(int cdolLength)
        {
            var response = SendOnFreshServer(VerifyArqcRequest(new string('A', cdolLength)));

            // Binary return code 0x00 and the 8-digit reason code, which is all Func07 reads back.
            Assert.That(response, Is.EqualTo("\0" + "00000000"));
        }

        [Test]
        public void VerifyArqc_CdolLengthBeyondTheData_IsRejected()
        {
            // Declares 90 CDOL characters but carries 58, so the ARQC, PAN and CVN fields run short.
            var request = VerifyArqcRequest(new string('A', 58)).Remove(14, 2).Insert(14, "90");

            var response = SendOnFreshServer(request);

            Assert.That(response, Does.StartWith("\x03"));
        }

        private static string ExpectedCvv(string cardNo, string expiry)
        {
            var digest = System.Security.Cryptography.SHA256.HashData(Encoding.ASCII.GetBytes(cardNo + expiry));
            return ((BitConverter.ToInt32(digest, 0) & int.MaxValue) % 1000).ToString("D3");
        }

        [Test]
        public void GenerateCvv_ManyConcurrentConnections_EachReplyUsesItsOwnCard()
        {
            using var cts = new CancellationTokenSource();
            var server = new AsynTcpServer(cts.Token) { PortNumber = FreePort(), SessionNumber = 100 };
            server.StartServer();
            try
            {
                // Synthetic 16-digit numbers 4000000000000000-4000000000000079; only the CVV per card matters here.
                var cards = Enumerable.Range(0, 80).Select(i => $"40000000000000{i:D2}").ToArray();
                // Connect all first so every receive is pending at once, then send together: with one shared buffer
                // the requests landed in the same array before each callback read it.
                var clients = cards.Select(_ => { var c = new TcpClient(); c.Connect(IPAddress.Loopback, server.PortNumber); return c; }).ToArray();
                try
                {
                    Thread.Sleep(200);
                    Parallel.For(0, cards.Length, i =>
                    {
                        var bytes = Encoding.ASCII.GetBytes("\x01" + "MASTER  " + "007" + cards[i] + "2806" + "201");
                        clients[i].GetStream().Write(bytes, 0, bytes.Length);
                    });
                    for (var i = 0; i < cards.Length; i++)
                    {
                        var stream = clients[i].GetStream();
                        stream.ReadTimeout = 5000;
                        using var response = new MemoryStream();
                        stream.CopyTo(response); // the simulator closes the connection after one reply
                        Assert.That(Encoding.ASCII.GetString(response.ToArray()),
                                    Is.EqualTo("\0" + "00000000" + ExpectedCvv(cards[i], "2806")), $"card ending {cards[i][^4..]}");
                    }
                }
                finally
                {
                    foreach (var c in clients) c.Dispose();
                }
            }
            finally
            {
                server.StopServer();
            }
        }

        [Test]
        public void GenerateCvv_SameCard_ReturnsTheDigestBasedCvvOnEveryRun()
        {
            // string.GetHashCode() would give the same value twice in one process too, so compare against the
            // process-independent digest of card number + expiry instead of a second call.
            var digest = System.Security.Cryptography.SHA256.HashData(Encoding.ASCII.GetBytes("4000000000000002" + "2806"));
            var expectedCvv = ((BitConverter.ToInt32(digest, 0) & int.MaxValue) % 1000).ToString("D3");

            Assert.That(GenerateCvvOnFreshServer(), Is.EqualTo("\0" + "00000000" + expectedCvv));
        }
    }
}
