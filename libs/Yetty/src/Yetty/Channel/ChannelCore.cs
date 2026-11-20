namespace Com.Cybersoft4u.Yetty.Channel
{
    #region DEPENDENCY

    using System.Net;
    using Com.Cybersoft4u.Yetty.Utils;

    #endregion

    public abstract class ChannelCore
    {
        protected internal abstract void Write(ByteBuf data);

        protected internal abstract void Flush();

        protected internal abstract void Bind(EndPoint localEndPoint);

        protected internal abstract void Disconnect();

        protected internal abstract void Connect(EndPoint remoteEndPoint);

        protected internal abstract void Connect(EndPoint remoteEndPoint, EndPoint localEndPoint);

        protected internal abstract void Close();

        protected internal abstract void StartRead();

        protected internal abstract void SetChannelOption(
        ChannelOptionLevel optionLevel,
        ChannelOptionName optionName,
        int optionValue);

        protected internal abstract void SetChannelOption(
            ChannelOptionLevel optionLevel,
            ChannelOptionName optionName,
            byte[] optionValue);

        protected internal abstract void SetChannelOption(
            ChannelOptionLevel optionLevel,
            ChannelOptionName optionName,
            bool optionValue);

        protected internal abstract void SetChannelOption(
            ChannelOptionLevel optionLevel,
            ChannelOptionName optionName,
            object optionValue);

    }
}
