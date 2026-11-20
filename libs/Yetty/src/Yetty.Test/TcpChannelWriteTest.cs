using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Com.Cybersoft4u.Yetty.Channel;
using Com.Cybersoft4u.Yetty.Channel.TCP;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;
using Com.Cybersoft4u.Yetty.Utils;
using NUnit.Framework;

namespace Com.Cybersoft4u.Yetty.Test
{
    [TestFixture]
    public class TcpChannelWriteTest
    {
        private class HandlerInitializer : ChannelInitializer
        {
            private readonly IChannelHandler? _handler;

            public HandlerInitializer(IChannelHandler? handler = null)
            {
                _handler = handler;
            }

            public override void InitChannel(IChannel channel)
            {
                if (_handler != null)
                {
                    channel.Pipe().AddLast(_handler, "test_handler");
                }
            }
        }

        private class CollectHandler : ChannelInboundHandler
        {
            private readonly MemoryStream _received = new MemoryStream();

            public int Length
            {
                get
                {
                    lock (_received)
                    {
                        return (int)_received.Length;
                    }
                }
            }

            public byte[] Received()
            {
                lock (_received)
                {
                    return _received.ToArray();
                }
            }

            public override void ChannelRead(ChannelHandlerContext ctx, object msg)
            {
                if (msg is ByteBuf buf)
                {
                    var bytes = buf.Val();
                    lock (_received)
                    {
                        _received.Write(bytes, 0, bytes.Length);
                    }
                }

                base.ChannelRead(ctx, msg);
            }
        }

        private static IChannel ConnectClient(EndPoint remote, IChannelHandler? handler = null)
        {
            var clientChannel = new Bootstrap()
                .ChannelType(typeof(TcpSocketChannel))
                .Handler(new HandlerInitializer(handler))
                .Connect(remote)
                .Sync()
                .Channel();
            Assert.IsTrue(clientChannel.IsConnected());
            return clientChannel;
        }

        private static byte[] ReceiveExactly(Socket socket, int length, TimeSpan timeout)
        {
            socket.ReceiveTimeout = (int)timeout.TotalMilliseconds;
            var buffer = new byte[length];
            var offset = 0;
            while (offset < length)
            {
                var read = socket.Receive(buffer, offset, length - offset, SocketFlags.None);
                if (read == 0)
                {
                    break;
                }

                offset += read;
            }

            Assert.AreEqual(length, offset, "Peer closed before every byte arrived.");
            return buffer;
        }

        [Test]
        public void Write_ConcurrentWritersWithoutFlush_EveryFrameArrivesInWriterOrder()
        {
            // Many threads calling Write() at once on one channel is the race where a writer could
            // take InWrite on an already drained queue and stall the channel, and it makes the
            // gathered send batch frames from different writers.
            const int writerCount = 8;
            const int framesPerWriter = 5000;
            const int frameLength = 8;

            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var clientChannel = ConnectClient(listener.LocalEndpoint);
            using var peer = listener.AcceptSocket();

            using var start = new Barrier(writerCount);
            var writers = Enumerable.Range(0, writerCount)
                .Select(writerId => new Thread(() =>
                {
                    start.SignalAndWait();
                    for (var seq = 0; seq < framesPerWriter; seq++)
                    {
                        var frame = new byte[frameLength];
                        BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(0, 4), writerId);
                        BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(4, 4), seq);
                        clientChannel.Write(new ByteBuf(frame));
                    }
                }) { IsBackground = true })
                .ToList();
            writers.ForEach(t => t.Start());

            // A stalled channel leaves bytes unsent, so the receive times out instead of hanging.
            var stream = ReceiveExactly(peer, writerCount * framesPerWriter * frameLength, TimeSpan.FromSeconds(30));
            writers.ForEach(t => Assert.IsTrue(t.Join(TimeSpan.FromSeconds(5))));

            var nextSeq = new int[writerCount];
            for (var offset = 0; offset < stream.Length; offset += frameLength)
            {
                var writerId = BinaryPrimitives.ReadInt32BigEndian(stream.AsSpan(offset, 4));
                var seq = BinaryPrimitives.ReadInt32BigEndian(stream.AsSpan(offset + 4, 4));
                Assert.That(writerId, Is.InRange(0, writerCount - 1), $"Corrupt frame at offset {offset}.");
                Assert.AreEqual(nextSeq[writerId], seq, $"Writer {writerId} frame out of order at offset {offset}.");
                nextSeq[writerId]++;
            }

            clientChannel.Disconnect();
            listener.Stop();
        }

        [Test]
        public void WriteAndFlush_LargeBuffersBehindSlowReader_StreamArrivesIntact()
        {
            // Buffers far larger than the socket send buffer, with the peer not reading at first,
            // so sends complete only in part and the unsent tail must be resent from the right offset.
            const int bufferCount = 16;
            const int bufferLength = 256 * 1024;

            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var clientChannel = ConnectClient(listener.LocalEndpoint);
            using var peer = listener.AcceptSocket();

            var expected = new byte[bufferCount * bufferLength];
            for (var i = 0; i < expected.Length; i++)
            {
                // Not a multiple of 256, so a slice resent from a wrong offset breaks the pattern.
                expected[i] = (byte)(i % 251);
            }

            var writer = new Thread(() =>
            {
                for (var i = 0; i < bufferCount; i++)
                {
                    clientChannel.Write(ByteBuf.From(expected, i * bufferLength, bufferLength));
                }

                clientChannel.Flush();
            }) { IsBackground = true };
            writer.Start();

            Thread.Sleep(500);
            var stream = ReceiveExactly(peer, expected.Length, TimeSpan.FromSeconds(30));

            Assert.IsTrue(writer.Join(TimeSpan.FromSeconds(5)), "Flush did not return after every byte was received.");
            Assert.IsTrue(expected.AsSpan().SequenceEqual(stream), "Received stream differs from what was written.");

            clientChannel.Disconnect();
            listener.Stop();
        }

        [Test]
        public void ChannelRead_ManySmallSendsFromPeer_ReceivedStreamIsIntact()
        {
            // Small back-to-back sends pile up in the pipe and are delivered as one multi-segment read.
            const int frameCount = 20000;
            const int frameLength = 37;

            var collector = new CollectHandler();
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var clientChannel = ConnectClient(listener.LocalEndpoint, collector);
            using var peer = listener.AcceptSocket();
            peer.NoDelay = true;

            var expected = new byte[frameCount * frameLength];
            for (var i = 0; i < expected.Length; i++)
            {
                expected[i] = (byte)(i % 251);
            }

            for (var i = 0; i < frameCount; i++)
            {
                peer.Send(expected, i * frameLength, frameLength, SocketFlags.None);
            }

            var deadline = DateTime.UtcNow.AddSeconds(30);
            while (collector.Length < expected.Length && DateTime.UtcNow < deadline)
            {
                Thread.Sleep(20);
            }

            Assert.IsTrue(expected.AsSpan().SequenceEqual(collector.Received()), "Received stream differs from what the peer sent.");

            clientChannel.Disconnect();
            listener.Stop();
        }
    }
}
