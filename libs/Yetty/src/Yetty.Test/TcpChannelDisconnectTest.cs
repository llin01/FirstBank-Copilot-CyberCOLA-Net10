using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Com.Cybersoft4u.Yetty.Channel;
using Com.Cybersoft4u.Yetty.Channel.TCP;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;
using NUnit.Framework;

namespace Com.Cybersoft4u.Yetty.Test
{
    // Disconnecting a channel while the peer keeps its side open: the channel closes first, so on Windows a
    // reusable disconnect (Socket.Disconnect(true)) waited for TIME_WAIT to end, about 120 s.
    [TestFixture]
    public class TcpChannelDisconnectTest
    {
        private static readonly TimeSpan Quick = TimeSpan.FromSeconds(2);

        private class EmptyInitializer : ChannelInitializer
        {
            public override void InitChannel(IChannel channel)
            {
            }
        }

        private class EmptyHandler : ChannelInboundHandler
        {
        }

        [Test]
        public void Disconnect_PeerStillOpen_ReturnsQuicklyAndSendsFin()
        {
            var peer = new TcpListener(IPAddress.Loopback, 0);
            peer.Start();
            try
            {
                var channel = new Bootstrap()
                    .ChannelType(typeof(TcpSocketChannel))
                    .Handler(new EmptyInitializer())
                    .Connect(peer.LocalEndpoint)
                    .Sync()
                    .Channel();
                using var accepted = peer.AcceptSocket();

                var watch = Stopwatch.StartNew();
                channel.Disconnect();
                watch.Stop();

                Assert.That(watch.Elapsed, Is.LessThan(Quick));
                // A graceful close: the peer reads end-of-stream rather than a reset.
                accepted.ReceiveTimeout = (int)Quick.TotalMilliseconds;
                Assert.That(accepted.Receive(new byte[1]), Is.EqualTo(0));
            }
            finally
            {
                peer.Stop();
            }
        }

        private class UnRegisteredCounter : ChannelInboundHandler
        {
            public int Count;

            public override void ChannelUnRegistered(ChannelHandlerContext ctx)
            {
                Interlocked.Increment(ref this.Count);
                base.ChannelUnRegistered(ctx);
            }
        }

        private class CounterInitializer : ChannelInitializer
        {
            private readonly UnRegisteredCounter counter;

            public CounterInitializer(UnRegisteredCounter counter)
            {
                this.counter = counter;
            }

            public override void InitChannel(IChannel channel)
            {
                channel.Pipe().AddLast(this.counter);
            }
        }

        // The owner's Disconnect and the receive callback that closing the socket triggers used to both get through,
        // so handlers saw ChannelUnRegistered twice, concurrently (SvcsEchoTestHandler then threw on a removed carrier).
        [Test]
        public void Disconnect_CalledConcurrently_FiresUnRegisteredOnce()
        {
            var peer = new TcpListener(IPAddress.Loopback, 0);
            peer.Start();
            try
            {
                var counter = new UnRegisteredCounter();
                var channel = new Bootstrap()
                    .ChannelType(typeof(TcpSocketChannel))
                    .Handler(new CounterInitializer(counter))
                    .Connect(peer.LocalEndpoint)
                    .Sync()
                    .Channel();
                using var accepted = peer.AcceptSocket();

                const int callers = 8;
                using var start = new Barrier(callers);
                var threads = new Thread[callers];
                for (var i = 0; i < callers; i++)
                {
                    threads[i] = new Thread(() => { start.SignalAndWait(); channel.Disconnect(); });
                    threads[i].Start();
                }
                foreach (var thread in threads)
                {
                    thread.Join();
                }
                // Let the receive callback, completed by the socket close, run its own disconnect attempt.
                Thread.Sleep(300);

                Assert.That(counter.Count, Is.EqualTo(1));
            }
            finally
            {
                peer.Stop();
            }
        }

        [Test]
        public void ServerClose_ChildPeerStillOpen_ReturnsQuickly()
        {
            var server = new ServerBootstrap()
                .ChannelType(typeof(TcpServerSocketChannel))
                .Handler(new EmptyHandler())
                .ChildHandler(new EmptyInitializer())
                .Bind(new IPEndPoint(IPAddress.Loopback, 0))
                .Sync()
                .Channel();
            using var client = new TcpClient();
            client.Connect((IPEndPoint)server.LocalEndPoint());
            // Give the server time to register the accepted child channel.
            Thread.Sleep(300);

            var watch = Stopwatch.StartNew();
            server.Close();
            watch.Stop();

            Assert.That(watch.Elapsed, Is.LessThan(Quick));
        }
    }
}
