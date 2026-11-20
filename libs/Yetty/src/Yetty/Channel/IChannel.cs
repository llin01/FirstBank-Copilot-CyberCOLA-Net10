namespace Com.Cybersoft4u.Yetty.Channel
{
    #region DEPENDENCY

    using System.Net;

    #endregion

    public interface IChannel
    {
        bool IsClosed();

        bool IsConnected();

        ChannelCore Core();

        IChannelPipe Pipe();

        IChannel Read();

        IChannel Write(object data);

        IChannel Flush();

        IChannel WriteAndFlush(object data);

        EndPoint RemoteEndPoint();

        EndPoint LocalEndPoint();

        void Disconnect();

        AsyncFuture Connect(EndPoint remoteEndPoint);

        AsyncFuture Connect(EndPoint remoteEndPoint, EndPoint localEndPoint);

        void Close();

        AsyncFuture Bind(EndPoint localEndPoint);

        object Carrier(object key);

        IChannel Carrier(object key, object value);

        void RemoveCarrier(object key);

        /// <summary>
        /// Get value of <code>TCarrierKey</code> Type and convert it to <code>TReturnType</code>.
        /// </summary>
        /// <typeparam name="TCarrierKey"></typeparam>
        /// <typeparam name="TReturnType"></typeparam>
        /// <returns></returns>
        TReturnType Carrier<TCarrierKey, TReturnType>()
            where TCarrierKey : CarrierKey;

        /// <summary>
        /// Set <code>value</code> with Key of <code>TCarrierKey</code> type.
        /// </summary>
        /// <typeparam name="TCarrierKey"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        IChannel Carrier<TCarrierKey>(object value) where TCarrierKey : CarrierKey;

        void RemoveCarrier<TCarrierKey>() where TCarrierKey : CarrierKey;
    }
}
