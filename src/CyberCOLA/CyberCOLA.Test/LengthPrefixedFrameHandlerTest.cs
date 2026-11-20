using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Com.Cybersoft4u.ServiceProvider.ClientAuth;
using Com.Cybersoft4u.Yetty.Channel;
using Com.Cybersoft4u.Yetty.Channel.TCP;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;
using Com.Cybersoft4u.Yetty.Utils;
using NUnit.Framework;

namespace Com.Cybersoft4u.CyberCOLA.Test
{
    // Runs the handler in a real Yetty pipeline over loopback TCP, so reads are split and coalesced the way
    // they are on the GW-Auth link. A pause between client writes forces them into separate server reads.
    [TestFixture]
    public class LengthPrefixedFrameHandlerTest
    {
        private static readonly TimeSpan Gap = TimeSpan.FromMilliseconds(150);

        private sealed class CaptureHandler : ChannelInboundHandler
        {
            public BlockingCollection<(IChannel Channel, byte[] Frame)> Frames { get; } = new();

            public override void ChannelRead(ChannelHandlerContext ctx, object msg)
            {
                if (msg is ByteBuf buf)
                {
                    Frames.Add((ctx.Channel, buf.Val()));
                }
            }
        }

        private sealed class ServerInitializer : ChannelInitializer
        {
            private readonly CaptureHandler _capture;

            public ServerInitializer(CaptureHandler capture) => _capture = capture;

            // One frame handler per channel, as in the Auth client pipelines.
            public override void InitChannel(IChannel channel) =>
                channel.Pipe().AddLast(new LengthPrefixedFrameHandler()).AddLast(_capture);
        }

        private sealed class ServerSocketHandler : ChannelInboundHandler
        {
        }

        private CaptureHandler _capture = null!;
        private IChannel _server = null!;
        private int _port;

        [SetUp]
        public void StartServer()
        {
            _capture = new CaptureHandler();
            _server = new ServerBootstrap()
                .ChannelType(typeof(TcpServerSocketChannel))
                .Handler(new ServerSocketHandler())
                .ChildHandler(new ServerInitializer(_capture))
                .Bind(new IPEndPoint(IPAddress.Loopback, 0))
                .Sync()
                .Channel();
            _port = ((IPEndPoint)_server.LocalEndPoint()).Port;
        }

        [TearDown]
        public void StopServer() => _server.Close();

        private static byte[] Frame(string body)
        {
            var payload = System.Text.Encoding.ASCII.GetBytes(body);
            return new[] { (byte)(payload.Length >> 8), (byte)payload.Length }.Concat(payload).ToArray();
        }

        private TcpClient Connect()
        {
            var client = new TcpClient { NoDelay = true };
            client.Connect(IPAddress.Loopback, _port);
            return client;
        }

        private static void Send(TcpClient client, byte[] bytes)
        {
            client.GetStream().Write(bytes, 0, bytes.Length);
            client.GetStream().Flush();
            Thread.Sleep(Gap);
        }

        private List<byte[]> Receive(int count)
        {
            var frames = new List<byte[]>();
            while (frames.Count < count && _capture.Frames.TryTake(out var item, TimeSpan.FromSeconds(5)))
            {
                frames.Add(item.Frame);
            }

            return frames;
        }

        [Test]
        public void ChannelRead_TwoFramesInOneRead_FiresEachFrame()
        {
            var first = Frame("0200 first");
            var second = Frame("0200 second message");
            using var client = Connect();

            Send(client, first.Concat(second).ToArray());

            Assert.That(Receive(2), Is.EqualTo(new[] { first, second }));
        }

        [Test]
        public void ChannelRead_FrameSplitAcrossReads_FiresOneCompleteFrame()
        {
            var frame = Frame("0200 a message that arrives in two parts");
            using var client = Connect();

            Send(client, frame[..10]);
            Send(client, frame[10..]);

            Assert.That(Receive(1), Is.EqualTo(new[] { frame }));
        }

        [Test]
        public void ChannelRead_FrameFollowedByPartialFrame_FiresBothInOrder()
        {
            var first = Frame("0420 complete");
            var second = Frame("0200 completed by the next read");
            using var client = Connect();

            Send(client, first.Concat(second[..7]).ToArray());
            Send(client, second[7..]);

            Assert.That(Receive(2), Is.EqualTo(new[] { first, second }));
        }

        [Test]
        public void ChannelRead_LengthHeaderSplitAcrossReads_FiresOneCompleteFrame()
        {
            var frame = Frame("0800 echo");
            using var client = Connect();

            Send(client, frame[..1]);
            Send(client, frame[1..]);

            Assert.That(Receive(1), Is.EqualTo(new[] { frame }));
        }

        [Test]
        public void ChannelRead_TwoConnectionsInterleaved_KeepsPartialDataPerChannel()
        {
            var a = Frame("AAAA from connection A");
            var b = Frame("BBBB from connection B");
            using var clientA = Connect();
            using var clientB = Connect();

            Send(clientA, a[..8]);
            Send(clientB, b[..8]);
            Send(clientA, a[8..]);
            Send(clientB, b[8..]);

            var frames = Receive(2);
            Assert.That(frames, Is.EquivalentTo(new[] { a, b }));
        }
    }
}
