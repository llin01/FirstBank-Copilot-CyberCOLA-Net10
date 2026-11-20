using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Com.Cybersoft4u.Yetty.Channel;
using Com.Cybersoft4u.Yetty.Channel.TCP;
using Com.Cybersoft4u.Yetty.Handler;
using NUnit.Framework;

namespace Com.Cybersoft4u.Yetty.Test
{
    // Keepalive used to be set only through the Windows-only SIO_KEEPALIVE_VALS IOControl, so on Linux a silently
    // dropped peer was never detected. Both the connecting and the accepted channel must carry the same settings
    // on every platform.
    [TestFixture]
    public class TcpChannelKeepAliveTest
    {
        private static readonly TimeSpan Wait = TimeSpan.FromSeconds(5);

        private class EmptyInitializer : ChannelInitializer
        {
            public override void InitChannel(IChannel channel)
            {
            }
        }

        private class EmptyHandler : ChannelInboundHandler
        {
        }

        private class CapturingInitializer : ChannelInitializer
        {
            public readonly ManualResetEventSlim Captured = new ManualResetEventSlim(false);

            public TcpSocketChannel? Channel;

            public override void InitChannel(IChannel channel)
            {
                this.Channel = (TcpSocketChannel)channel;
                this.Captured.Set();
            }
        }

        [Test]
        public void Connect_ClientChannel_EnablesKeepAlive()
        {
            var peer = new TcpListener(IPAddress.Loopback, 0);
            peer.Start();
            try
            {
                var channel = (TcpSocketChannel)new Bootstrap()
                    .ChannelType(typeof(TcpSocketChannel))
                    .Handler(new EmptyInitializer())
                    .Connect(peer.LocalEndpoint)
                    .Sync()
                    .Channel();
                using var accepted = peer.AcceptSocket();
                try
                {
                    AssertKeepAlive(channel);
                }
                finally
                {
                    channel.Disconnect();
                }
            }
            finally
            {
                peer.Stop();
            }
        }

        [Test]
        public void Accept_ChildChannel_EnablesKeepAlive()
        {
            var initializer = new CapturingInitializer();
            var server = new ServerBootstrap()
                .ChannelType(typeof(TcpServerSocketChannel))
                .Handler(new EmptyHandler())
                .ChildHandler(initializer)
                .Bind(new IPEndPoint(IPAddress.Loopback, 0))
                .Sync()
                .Channel();
            try
            {
                using var client = new TcpClient();
                client.Connect((IPEndPoint)server.LocalEndPoint());

                Assert.That(initializer.Captured.Wait(Wait), Is.True, "no child channel was accepted");
                AssertKeepAlive(initializer.Channel!);
            }
            finally
            {
                server.Close();
            }
        }

        private static void AssertKeepAlive(TcpSocketChannel channel)
        {
            // Reading the TcpKeepAlive* options back needs Windows 10 1709 / Server 2019; on older Windows the
            // channel still sets keepalive through IOControl, which cannot be read back.
            Assume.That(!OperatingSystem.IsWindows() || OperatingSystem.IsWindowsVersionAtLeast(10, 0, 16299),
                "TcpKeepAlive* socket options cannot be read on this Windows version");

            var socket = ((TcpChannelCore)channel.Core()).BaseSocket;
            Assert.Multiple(() =>
            {
                Assert.That(socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive), Is.Not.EqualTo(0));
                Assert.That(socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime),
                    Is.EqualTo(TcpSocketChannel.KeepAliveIdleSeconds));
                Assert.That(socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval),
                    Is.EqualTo(TcpSocketChannel.KeepAliveIntervalSeconds));
                Assert.That(socket.GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount),
                    Is.EqualTo(TcpSocketChannel.KeepAliveRetryCount));
            });
        }
    }
}
