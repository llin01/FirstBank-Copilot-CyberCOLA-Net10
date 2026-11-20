using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Com.Cybersoft4u.Yetty.Channel;
using Com.Cybersoft4u.Yetty.Channel.TCP;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;
using Com.Cybersoft4u.Yetty.Utils;
using NUnit.Framework;

namespace Com.Cybersoft4u.Yetty.Test
{
    [TestFixture]
    public class PipelinesChannelTest
    {
        private class ServerChannelInitializer : ChannelInitializer
        {
            private readonly TestServerHandler _handler;

            public ServerChannelInitializer(TestServerHandler handler)
            {
                _handler = handler;
            }

            public override void InitChannel(IChannel channel)
            {
                channel.Pipe().AddLast(_handler, "test_server_handler");
            }
        }

        private class TestServerHandler : ChannelInboundHandler
        {
            private readonly ManualResetEventSlim _receivedEvent;
            private byte[]? _receivedData;

            public TestServerHandler(ManualResetEventSlim receivedEvent)
            {
                _receivedEvent = receivedEvent;
            }

            public byte[]? ReceivedData => _receivedData;

            public override void ChannelRead(ChannelHandlerContext ctx, object msg)
            {
                if (msg is ByteBuf buf)
                {
                    _receivedData = buf.Val();
                    _receivedEvent.Set();
                }
                base.ChannelRead(ctx, msg);
            }
        }

        private class ClientChannelInitializer : ChannelInitializer
        {
            public override void InitChannel(IChannel channel)
            {
                // No handlers needed for basic outbound writing in this test
            }
        }

        private class ServerSocketHandler : ChannelInboundHandler
        {
            // Empty handler for server socket channel itself
        }

        [Test]
        public void TestPipelinesDataTransmission()
        {
            var receivedEvent = new ManualResetEventSlim(false);
            var serverHandler = new TestServerHandler(receivedEvent);

            // 1. Initialize and bind TCP server on localhost with dynamic port using ServerBootstrap
            var serverBootstrap = new ServerBootstrap()
                .ChannelType(typeof(TcpServerSocketChannel))
                .Handler(new ServerSocketHandler())
                .ChildHandler(new ServerChannelInitializer(serverHandler));
            
            var serverAddr = new IPEndPoint(IPAddress.Loopback, 0);
            var serverFuture = serverBootstrap.Bind(serverAddr).Sync();
            IChannel serverChannel = serverFuture.Channel();
            var boundEndPoint = serverChannel.LocalEndPoint();
            Assert.IsNotNull(boundEndPoint);

            // 2. Initialize and connect TCP client using Bootstrap
            var clientBootstrap = new Bootstrap()
                .ChannelType(typeof(TcpSocketChannel))
                .Handler(new ClientChannelInitializer());
            
            var clientFuture = clientBootstrap.Connect(boundEndPoint).Sync();
            IChannel clientChannel = clientFuture.Channel();
            Assert.IsTrue(clientChannel.IsConnected());

            // 3. Client writes data to Server
            var payload = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD, 0xEE, 0xFF };
            clientChannel.WriteAndFlush(ByteBuf.From(payload, 0, payload.Length));

            // 4. Wait for server to receive data (timeout after 5 seconds)
            bool received = receivedEvent.Wait(TimeSpan.FromSeconds(5));
            Assert.IsTrue(received, "Server failed to receive data via Pipelines channel within timeout.");

            // 5. Verify data integrity
            var receivedData = serverHandler.ReceivedData;
            Assert.IsNotNull(receivedData, "Server signalled receipt but captured no data.");
            CollectionAssert.AreEqual(payload, receivedData!, "Data received by server is corrupted or incomplete.");

            // 6. Tear down channels
            clientChannel.Disconnect();
            serverChannel.Close();
        }
    }
}
