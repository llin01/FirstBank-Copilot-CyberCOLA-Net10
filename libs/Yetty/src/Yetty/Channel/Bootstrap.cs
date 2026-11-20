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
    /// Client bootstrap for configuring and creating TCP socket channels with custom handlers and options.
    /// </summary>
    public class Bootstrap
    {
        protected readonly Dictionary<ChannelOptionLevel, Dictionary<ChannelOptionName, int>> LevelNameDictionaryInt =
            new Dictionary<ChannelOptionLevel, Dictionary<ChannelOptionName, int>>();

        protected readonly Dictionary<ChannelOptionLevel, Dictionary<ChannelOptionName, byte[]>> LevelNameDictionaryByte
            = new Dictionary<ChannelOptionLevel, Dictionary<ChannelOptionName, byte[]>>();

        protected readonly Dictionary<ChannelOptionLevel, Dictionary<ChannelOptionName, bool>> LevelNameDictionaryBool =
            new Dictionary<ChannelOptionLevel, Dictionary<ChannelOptionName, bool>>();

        protected readonly Dictionary<ChannelOptionLevel, Dictionary<ChannelOptionName, object>> LevelNameDictionaryObject = new Dictionary<ChannelOptionLevel, Dictionary<ChannelOptionName, object>>();

        protected Type ChannelTypeClass;

        protected IChannelHandler ChannelHandler;

        public Bootstrap SetChannelOption(ChannelOptionLevel optionLevel, ChannelOptionName optionName, int optionValue)
        {
            if (!this.LevelNameDictionaryInt.ContainsKey(optionLevel))
            {
                this.LevelNameDictionaryInt.Add(optionLevel, new Dictionary<ChannelOptionName, int>());
            }

            this.LevelNameDictionaryInt[optionLevel][optionName] = optionValue;
            return this;
        }

        public Bootstrap SetChannelOption(
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

        public Bootstrap SetChannelOption(
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

        public Bootstrap SetChannelOption(
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

        public Bootstrap ChannelType(Type channelType)
        {
            this.ChannelTypeClass = channelType;
            return this;
        }

        public Bootstrap Handler(IChannelHandler handler)
        {
            this.ChannelHandler = handler;
            return this;
        }

        public virtual AsyncFuture Connect(EndPoint remoteEndPoint)
        {
            var socketChannel = Activator.CreateInstance(this.ChannelTypeClass, true) as SocketChannel;
            if (socketChannel == null)
            {
                throw new InvalidTypeException();
            }

            socketChannel.Handler = this.ChannelHandler;
            this.SetChannelOption(socketChannel);
            return socketChannel.Connect(remoteEndPoint);
        }

        public virtual AsyncFuture Connect(EndPoint remoteEndPoint, EndPoint localEndPoint)
        {
            var socketChannel = Activator.CreateInstance(this.ChannelTypeClass, true) as SocketChannel;
            if (socketChannel == null)
            {
                throw new InvalidTypeException();
            }

            socketChannel.Handler = this.ChannelHandler;
            this.SetChannelOption(socketChannel);
            return socketChannel.Connect(remoteEndPoint, localEndPoint);
        }


        protected void SetChannelOption(IChannel channel)
        {
            foreach (var level in this.LevelNameDictionaryBool.Keys)
            {
                foreach (var name in this.LevelNameDictionaryBool[level].Keys)
                {
                    channel.Core().SetChannelOption(level, name, this.LevelNameDictionaryBool[level][name]);
                }
            }

            foreach (var level in this.LevelNameDictionaryInt.Keys)
            {
                foreach (var name in this.LevelNameDictionaryInt[level].Keys)
                {
                    channel.Core().SetChannelOption(level, name, this.LevelNameDictionaryInt[level][name]);
                }
            }

            foreach (var level in this.LevelNameDictionaryByte.Keys)
            {
                foreach (var name in this.LevelNameDictionaryByte[level].Keys)
                {
                    channel.Core().SetChannelOption(level, name, this.LevelNameDictionaryByte[level][name]);
                }
            }

            foreach (var level in this.LevelNameDictionaryObject.Keys)
            {
                foreach (var name in this.LevelNameDictionaryObject[level].Keys)
                {
                    channel.Core().SetChannelOption(level, name, this.LevelNameDictionaryObject[level][name]);
                }
            }
        }
    }
}
