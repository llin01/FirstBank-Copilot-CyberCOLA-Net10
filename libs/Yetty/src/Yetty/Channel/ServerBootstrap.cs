namespace Com.Cybersoft4u.Yetty.Channel
{
    #region DEPENDENCY

    using System;
    using System.Collections.Generic;
    using System.Net;

    using Com.Cybersoft4u.Yetty.Exception;
    using Com.Cybersoft4u.Yetty.Handler;

    #endregion

    /// <summary>
    /// Server bootstrap for configuring and binding TCP server sockets with custom handlers and child channel options.
    /// </summary>
    public class ServerBootstrap : Bootstrap
    {
        protected IChannelHandler ChildChannelHandler;

        public new ServerBootstrap ChannelType(Type channelType)
        {
            this.ChannelTypeClass = channelType;
            return this;
        }

        public new ServerBootstrap Handler(IChannelHandler handler)
        {
            this.ChannelHandler = handler;
            return this;
        }

        public ServerBootstrap ChildHandler(IChannelHandler childHandler)
        {
            this.ChildChannelHandler = childHandler;
            return this;
        }

        public AsyncFuture Bind(EndPoint localEndPoint)
        {
            var serverSocketChannel = Activator.CreateInstance(this.ChannelTypeClass, true) as ServerSocketChannel;
            if (serverSocketChannel == null)
            {
                throw new InvalidTypeException();
            }

            serverSocketChannel.ChildHandler = this.ChildChannelHandler;
            serverSocketChannel.Handler = this.ChannelHandler;
            this.SetChannelOption(serverSocketChannel);
            return serverSocketChannel.Bind(localEndPoint);
        }

        public override AsyncFuture Connect(EndPoint remoteEndPoint)
        {
            throw new NotSupportedException();
        }

        public new ServerBootstrap SetChannelOption(
            ChannelOptionLevel optionLevel,
            ChannelOptionName optionName,
            int optionValue)
        {
            if (!this.LevelNameDictionaryInt.ContainsKey(optionLevel))
            {
                this.LevelNameDictionaryInt.Add(optionLevel, new Dictionary<ChannelOptionName, int>());
            }

            this.LevelNameDictionaryInt[optionLevel][optionName] = optionValue;
            return this;
        }

        public new ServerBootstrap SetChannelOption(
            ChannelOptionLevel optionLevel,
            ChannelOptionName optionName,
            byte[] optionValue)
        {
            if (!this.LevelNameDictionaryByte.ContainsKey(optionLevel))
            {
                this.LevelNameDictionaryByte.Add(optionLevel, new Dictionary<ChannelOptionName, byte[]>());
            }

            this.LevelNameDictionaryByte[optionLevel][optionName] = optionValue;
            return this;
        }

        public new ServerBootstrap SetChannelOption(
            ChannelOptionLevel optionLevel,
            ChannelOptionName optionName,
            bool optionValue)
        {
            if (!this.LevelNameDictionaryBool.ContainsKey(optionLevel))
            {
                this.LevelNameDictionaryBool.Add(optionLevel, new Dictionary<ChannelOptionName, bool>());
            }

            this.LevelNameDictionaryBool[optionLevel][optionName] = optionValue;
            return this;
        }

        public new ServerBootstrap SetChannelOption(
            ChannelOptionLevel optionLevel,
            ChannelOptionName optionName,
            object optionValue)
        {
            if (!this.LevelNameDictionaryObject.ContainsKey(optionLevel))
            {
                this.LevelNameDictionaryObject.Add(optionLevel, new Dictionary<ChannelOptionName, object>());
            }

            this.LevelNameDictionaryObject[optionLevel][optionName] = optionValue;
            return this;
        }
    }
}
