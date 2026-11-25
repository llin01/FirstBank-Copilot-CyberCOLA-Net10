// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FiscColaService.cs" company="Cybersoft">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the FiscColaService type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------


namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA
{
    #region DEPENDENCY

    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Net;
    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.ServiceProvider.EntryPoints.Handlers;
    using Com.Cybersoft4u.ServiceProvider.Utils;
    using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
    using Com.Cybersoft4u.ServiceProvider.Utils.Keys;
    using Com.Cybersoft4u.Yetty.Channel;
    using Com.Cybersoft4u.Yetty.Channel.TCP;
    using Com.Cybersoft4u.Yetty.Context;
    using System.Net.Sockets;
    using Com.Cybersoft4u.CommonLib.FiscMessage;
    using Com.Cybersoft4u.CommonLib.Resource;
    using Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.FISC.Handlers;
    using Com.Cybersoft4u.TM.ISO8583;

    #endregion

    public class FiscColaService : QueuedService<TM.FISC.FISC>, IReserveable<IChannel>
    {
        protected IChannel TcpChannel;

        private readonly ConcurrentDictionary<IChannel, bool> idleChannels = new ConcurrentDictionary<IChannel, bool>();

        private readonly ConcurrentDictionary<string, IChannel> relationChannels = new ConcurrentDictionary<string, IChannel>();

        // Track when each relationId was registered for timeout/cleanup
        private readonly ConcurrentDictionary<string, DateTime> relationIdTimestamps = new ConcurrentDictionary<string, DateTime>();

        private readonly ConcurrentDictionary<ChannelHandlerContext, DateTime> clientOfChannels = new ConcurrentDictionary<ChannelHandlerContext, DateTime>();

        // Track channel state: used to verify channels before reusing revived connections
        private readonly ConcurrentDictionary<IChannel, ChannelStateInfo> channelStates = new ConcurrentDictionary<IChannel, ChannelStateInfo>();

        private const int TimeoutLimitMilliseconds = 1000;

        // RelationId timeout: clean up mappings older than this (60 seconds)
        private const int RelationIdTimeoutMilliseconds = 60000;

        // How long a request waits for a busy COLA channel before it is answered with RC 96; 0 answers at once.
        private int idleChannelWaitMilliseconds = IdleChannelRetry.DefaultMaxWaitMilliseconds;

        // Minimum free channels before triggering circuit breaker (10% of total)
        private int circuitBreakerThreshold = 1;

        private Object objectLock = new Object();
        public bool Take(IChannel item)
        {
            return this.idleChannels.TryUpdate(item, false, true);
        }

        public void Release(IChannel item)
        {
            // Update channel state before releasing back to pool
            ChannelStateInfo state = null;
            
            // Try to get state from carrier (set by revival handler)
            try
            {
                var carrierState = item.Carrier<ChannelStateKey, ChannelStateInfo>();
                if (carrierState != null)
                {
                    state = carrierState;
                }
            }
            catch
            {
                // Carrier not set, will try dictionary
            }
            
            if (state == null && channelStates.TryGetValue(item, out var dictState))
            {
                state = dictState;
            }
            
            if (state != null)
            {
                state.LastReleaseTime = DateTime.Now;
                state.ReleaseCount++;
                // Also update in dictionary
                channelStates[item] = state;
            }
            else
            {
                // First release - initialize state
                state = new ChannelStateInfo 
                { 
                    LastReleaseTime = DateTime.Now,
                    ReleaseCount = 1,
                    IsRevived = false
                };
                channelStates[item] = state;
            }

            if (!this.idleChannels.TryAdd(item, true))
            {
                this.idleChannels.TryUpdate(item, true, false);
            }
        }

        // Verify channel is healthy before reusing (especially revived channels)
        private bool VerifyChannelHealth(IChannel channel)
        {
            if (channel == null || !channel.IsConnected() || channel.IsClosed())
            {
                Logger.Warn($"Channel health check failed: Connected={channel?.IsConnected()}, Closed={channel?.IsClosed()}");
                return false;
            }

            // For revived channels, verify state
            ChannelStateInfo state = null;
            
            // Try to get state from carrier first
            try
            {
                var carrierState = channel.Carrier<ChannelStateKey, ChannelStateInfo>();
                if (carrierState != null)
                {
                    state = carrierState;
                }
            }
            catch
            {
                // Carrier not set, will try dictionary
            }
            
            if (state == null && channelStates.TryGetValue(channel, out var dictState))
            {
                state = dictState;
            }
            
            if (state != null && state.IsRevived)
            {
                // Revived channel should have been released successfully at least once
                if (state.ReleaseCount == 0)
                {
                    Logger.Warn($"Revived channel has never been released successfully");
                    return false;
                }
                // Revived channel needs warm-up period (1 second) after revival
                var timeSinceRevival = (DateTime.Now - state.RevivalTime).TotalMilliseconds;
                if (timeSinceRevival < 1000)
                {
                    Logger.Debug($"Revived channel in warm-up period: {timeSinceRevival:F0}ms");
                    return false;
                }
            }

            return true;
        }

        public bool IsTake(IChannel item)
        {
            return !this.idleChannels[item];
        }

        public bool Remove(IChannel item)
        {
            bool outItem;
            var removed = this.idleChannels.TryRemove(item, out outItem);
            if (removed)
            {
                channelStates.TryRemove(item, out _);
            }
            return removed;
        }

        public void AddChannel(ChannelHandlerContext ctx, DateTime dateTimeNow)
        {
            if (!clientOfChannels.TryAdd(ctx, dateTimeNow))
            {
                this.Logger.Info($"Add {ctx.Channel.RemoteEndPoint()} Channel Error");
                clientOfChannels.TryUpdate(ctx, dateTimeNow, dateTimeNow);
            }
            else
                this.Logger.Info($"Add {ctx.Channel.RemoteEndPoint()} Channel At : {dateTimeNow:yyyy-MM-dd HH:mm:ss:fffff}");
        }

        public void RemoveChannel(ChannelHandlerContext ctx)
        {
            if (!clientOfChannels.TryRemove(ctx, out var dateTime))
            {
                this.Logger.Info($"Remove Channel {ctx.Channel.RemoteEndPoint()} At : {dateTime:yyyy-MM-dd HH:mm:ss:fffff}");
            }
        }

        public void RegisterRelation(string relationId, IChannel channel)
        {
            if (this.relationChannels.ContainsKey(relationId))
            {
                Logger.Error($"FiscColaService RelationChannels[{relationId}] have Channel LocalEndPoint[{this.relationChannels[relationId]?.LocalEndPoint()}] => RemoteEndPoint[{this.relationChannels[relationId]?.RemoteEndPoint()}]");
                // Clean up stale mapping before registering new one
                this.relationChannels.TryRemove(relationId, out _);
                this.relationIdTimestamps.TryRemove(relationId, out _);
            }

            this.relationChannels[relationId] = channel;
            this.relationIdTimestamps[relationId] = DateTime.Now;
        }

        public IChannel RemoveRelation(string relationId)
        {
            this.relationChannels.TryRemove(relationId, out var outChannel);
            this.relationIdTimestamps.TryRemove(relationId, out _);
            return outChannel;
        }

        // Clean up expired relationId mappings to prevent memory leak
        private void CleanupExpiredRelations()
        {
            var now = DateTime.Now;
            foreach (var kvp in this.relationIdTimestamps)
            {
                var elapsed = (now - kvp.Value).TotalMilliseconds;
                if (elapsed > RelationIdTimeoutMilliseconds)
                {
                    this.relationChannels.TryRemove(kvp.Key, out _);
                    this.relationIdTimestamps.TryRemove(kvp.Key, out _);
                    Logger.Warn($"Cleaned up expired relationId[{kvp.Key}] after {elapsed:F0}ms");
                }
            }
        }

        public IChannel GetIdleChannel()
        {
            // Try to get a healthy idle channel with state verification
            foreach (var channel in this.idleChannels.Keys.ToList())
            {
                // Verify channel is healthy before attempting to take it
                if (!VerifyChannelHealth(channel))
                {
                    // Remove unhealthy channel from pool
                    this.idleChannels.TryRemove(channel, out _);
                    channelStates.TryRemove(channel, out _);
                    Logger.Info($"Removed unhealthy channel from pool");
                    continue;
                }

                // Try to acquire this channel
                if (this.Take(channel))
                {
                    return channel;
                }
            }
            return null;
        }

        public void ChannelAutoEcho(bool flag)
        {
            this.idleChannels.Keys.ToList().ForEach(channel => channel.Carrier<AutoEchoKey>(flag));
        }

        public bool ChannelAutoEcho()
        {
            return this.idleChannels.Keys.ToList().Any() && this.idleChannels.Keys.ToList().First().Carrier<AutoEchoKey, bool>().Default(false);
        }

        protected override void StopTask()
        {
            base.StopTask();
            this.TcpChannel.Close();
        }

        /// <summary>
        /// Get item form queue and dispatch it.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>false for put item back to queue, and true means success.</returns>
        protected override bool DispatchTask(TM.FISC.FISC item)
        {
            // Clean up expired relationId mappings periodically
            if (IdleChannelRetry.IsRetry(item) == false)
            {
                CleanupExpiredRelations();
                DisplayChannelStatus();
            }
            
            //this.Logger.Debug($"Dispatch Message: {item.Mti()}");
            var relationId = item.Extend<RelationIdKey, string>();
            
            // Apply circuit breaker: if too few channels are free, drop low-priority requests
            var freeChannelCount = this.idleChannels.Count(kvp => kvp.Value);
            if (freeChannelCount <= circuitBreakerThreshold && Convert.ToInt16(item.Mti()[2]) % 2 == 0)
            {
                Logger.Warn($"Circuit breaker triggered: only {freeChannelCount} free channels");
                // For requests (even MTI), when circuit breaker active
                TM.FISC.FISC response = new MessageBase(item, "96").GetFiscMessage();
                string requestRelationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                                      $"{response.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                                      $"{response.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                                      $"{response.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                                      $"{response.Core().Field(FIELD.F_41) ?? $"{0:D8}"}"
                                                      );
                response.Extend<RelationIdKey>(requestRelationId);
                CyberGWFISCResource.CyberGWFISC.Put(response);
                Logger.Debug($"Circuit breaker response: MTI[{response.Mti()}], RelationId[{requestRelationId}]");
                return true;
            }
            
            //this for response channel
            IChannel channel;
            if (Convert.ToInt16(item.Mti()[2]) % 2 == 1)
            {
                channel = this.RemoveRelation(relationId);
                this.Logger.Debug($"FiscColaService Dispatch Message: {item.Mti()}, RelationId: {relationId}, Remove Response Channel: LocalEndPoint[{channel?.LocalEndPoint()}], RemoteEndPoint[{channel?.RemoteEndPoint()}]");
                //this for response channel
                if (channel != null && (Convert.ToInt16(item.Mti()[2]) % 2 == 1))
                {
                    channel.Write(new PayloadContext(item));
                    this.Logger.Info($"Write Message[{item.Mti()}] From Channel LocalEndPoint[{channel.LocalEndPoint()}], RemoteEndPoint[{channel.RemoteEndPoint()}]");
                    this.Release(channel);
                    return true;
                }

                channel = null;
                channel = this.GetIdleChannel();
                this.Logger.Info($"FiscColaService DispatchTask Get channel: Local[{channel?.LocalEndPoint()}], Remote[{channel?.RemoteEndPoint()}]");
                if (channel != null)
                {
                    channel.Write(new PayloadContext(item));
                    this.Logger.Info($"Write Message[{item.Mti()}] From Channel LocalEndPoint[{channel.LocalEndPoint()}], RemoteEndPoint[{channel.RemoteEndPoint()}]");
                    this.Release(channel);
                }
                else
                {
                    this.Logger.Info($"Not Find Response channel[{relationId}], MTI[{item.Mti()}]");
                }
                return true;
            }
            channel = null;
            if (!ObjectUtils.IsNull(relationId))
            {
                channel = this.RemoveRelation(relationId);
            }

            if (ObjectUtils.IsNull(channel))
            {
                channel = this.GetIdleChannel();
                if (ObjectUtils.IsNull(channel))
                {
                    // Every channel is busy: retry shortly instead of answering RC 96 at once.
                    if (!this.idleChannels.IsEmpty && this.idleChannelWaitMilliseconds > 0)
                    {
                        if (IdleChannelRetry.TrySchedule(item, this.Put, this.idleChannelWaitMilliseconds, out var waitedMilliseconds))
                        {
                            return true;
                        }

                        this.Logger.Info($"No free Cola channel after waiting {waitedMilliseconds} ms");
                    }

                    TM.FISC.FISC response;
                    string requestRelationId;
                    if (this.idleChannels.Count == 0)
                    {
                        this.Logger.Fatal($"No online Cola channel, return message: {item.Mti()}");
                        response = new MessageBase(item, "96").GetFiscMessage();
                        foreach (var pair in response.Core().OrderedFields())
                        {
                            var key = pair.Key;
                            var value = pair.Value;
                            if (value is byte[] bytes)
                            {
                                var tempString = HexStringConverter.FromByteArray(bytes);
                                this.Logger.Info($"FIELD [{key}] VALUE [{tempString}]");
                            }
                            else
                            {
                                this.Logger.Info($"FIELD [{key}] Field Position [{FIELDMethods.Val(key)}] VALUE [{value}]");
                            }
                        }
                        requestRelationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                                          $"{response.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                                          $"{response.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                                          $"{response.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                                          $"{response.Core().Field(FIELD.F_41) ?? $"{0:D8}"}"
                                                          );

                        response.Extend<RelationIdKey>(requestRelationId);
                        CyberGWFISCResource.CyberGWFISC.Put(response);
                        this.Logger.Debug($"1. Put Response :MTI[{response.Mti()}], Item RelationId[{requestRelationId}]");
                        return true;
                    }
                    //Thread.Sleep(100);
                    //Check Channel Create DataTime
                    //lock (objectLock)
                    //{
                    //    var getTimeoutChannel = GetTimeoutCtx();
                    //    if (getTimeoutChannel != null)
                    //    {
                    //        this.Logger.Warn($"UnRegister Channel LocalEndPoint[{getTimeoutChannel.Item1.Channel.LocalEndPoint().ToString()}], RemoteEndPoint[{getTimeoutChannel.Item1.Channel.RemoteEndPoint().ToString()}] Time [{getTimeoutChannel.Item2:yyyy-MM-dd HH:mm:ss:fffff}]");
                    //        getTimeoutChannel.Item1.Channel.Disconnect();
                    //        getTimeoutChannel.Item1.Channel.Close();
                    //        return false;
                    //    }
                    //}
                    var itemRelationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                                       $"{item.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                                       $"{item.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                                       $"{item.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                                       $"{item.Core().Field(FIELD.F_41) ?? $"{0:D8}"}"
                                                       );

                    this.Logger.Warn($"Can't find free Cola channel for message: {item.Mti()}, RelationId[{itemRelationId}]");
                    response = new MessageBase(item, "96").GetFiscMessage();

                    requestRelationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                                      $"{response.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                                      $"{response.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                                      $"{response.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                                      $"{response.Core().Field(FIELD.F_41) ?? $"{0:D8}"}"
                                                      );

                    response.Extend<RelationIdKey>(requestRelationId);
                    CyberGWFISCResource.CyberGWFISC.Put(response);
                    this.Logger.Debug($"2. Put Response :MTI[{response.Mti()}], Item RelationId[{requestRelationId}]");
                    return true;
                }
            }
            var childChannel = GetChildChannel(channel.RemoteEndPoint().ToString());
            if (childChannel.Item1 != null)
            {
                var nowDateTime = DateTime.Now;
                if (clientOfChannels.TryUpdate(childChannel.Item1, nowDateTime, childChannel.Item2))
                {
                    this.Logger.Info($"Update Child Channel RemoteEndPoint[{childChannel.Item1.Channel.RemoteEndPoint().ToString()}] Time From[{childChannel.Item2:yyyy-MM-dd HH:mm:ss:fffff} To [{nowDateTime:yyyy-MM-dd HH:mm:ss:fffff}]");
                }
                else
                {
                    this.Logger.Info($"Update Child Channel Failed");
                }
            }
            channel.Write(new PayloadContext(item));
            this.Logger.Info($"Write Message From Channel LocalEndPoint[{channel.LocalEndPoint()}], RemoteEndPoint[{channel.RemoteEndPoint()}]");
            return true;
        }

        protected override void PreStartTask()
        {
            var configurationLoaderRoot = new ConfigurationLoaderRoot();
            var serverIp = CommonUtils.GetIPAddressStringByHostName(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-bind-ip").Trim(), AddressFamily.InterNetwork);
            var serverPort = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-bind-port"));
            var backlog = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-backlog"));
            this.idleChannelWaitMilliseconds = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ColaServiceConfigSectionName, "cola-service-idle-channel-wait-ms").Default(IdleChannelRetry.DefaultMaxWaitMilliseconds.ToString()));
            
            // Initialize circuit breaker threshold (minimum 1, max 10% of backlog)
            this.circuitBreakerThreshold = Math.Max(1, backlog / 10);
            
            this.TcpChannel =
                new ServerBootstrap().ChannelType(typeof(TcpServerSocketChannel))
                    .Handler(new ServiceProviderLoggerHandler("FiscColaService"))
                    .ChildHandler(new FiscColaChannelInitializer(this))
                    .SetChannelOption(ChannelOptionLevel.Tcp, ChannelOptionName.NoDelay, true)
                    .SetChannelOption(ChannelOptionLevel.Socket, ChannelOptionName.Backlog, backlog)
                    .Bind(new IPEndPoint(IPAddress.Parse(serverIp), serverPort))
                    .Sync()
                    .Channel();
        }

        private void DisplayChannelStatus()
        {
            this.Logger.Info($"Client Channel Count: {clientOfChannels.Count}");
            if (this.Logger.IsDebugEnabled)
            {
                using (var clientOfChannelsGetEnumerator = clientOfChannels.GetEnumerator())
                {
                    while (clientOfChannelsGetEnumerator.MoveNext())
                    {
                        this.Logger.Debug($"LocalEndPoint[{clientOfChannelsGetEnumerator.Current.Key.Channel.LocalEndPoint()}], RemoteEndPoint[{clientOfChannelsGetEnumerator.Current.Key.Channel.RemoteEndPoint()}] Time : {clientOfChannelsGetEnumerator.Current.Value:yyyy-MM-dd HH:mm:ss:fffff}");
                    }
                }
            }
        }
        private Tuple<ChannelHandlerContext, DateTime> GetChildChannel(string remoteEntryPoint)
        {
            var ctxKey = clientOfChannels.Keys.Where(item => item.Channel.RemoteEndPoint().ToString()!.Equals(remoteEntryPoint)).ToArray();
            if (ctxKey.Length != 1) return Tuple.Create<ChannelHandlerContext, DateTime>(null, DateTime.Now);
            DateTime dateTime;
            if (clientOfChannels.TryGetValue(ctxKey[0], out dateTime))
                return Tuple.Create<ChannelHandlerContext, DateTime>(ctxKey[0], dateTime);
            return Tuple.Create<ChannelHandlerContext, DateTime>(null, DateTime.Now);
        }
        private ChannelHandlerContext GetOldestCtx()
        {
            return clientOfChannels.OrderBy(data => data.Value).Select(data => data.Key).FirstOrDefault();
        }
    }

    // Helper class to track channel state and health
    public class ChannelStateInfo
    {
        public DateTime LastReleaseTime { get; set; }
        public DateTime RevivalTime { get; set; }
        public int ReleaseCount { get; set; }
        public bool IsRevived { get; set; }
    }
}
