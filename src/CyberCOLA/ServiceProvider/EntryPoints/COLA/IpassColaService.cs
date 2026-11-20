// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IpassColaService.cs" company="Cybersoft">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the IpassColaService type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System.Net.Sockets;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CommonLib.IpassMessage;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.COLA.IPASS.Handlers;
using Com.Cybersoft4u.TM.ISO8583;

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

    #endregion

    public class IpassColaService : QueuedService<TM.ISO8583.IPASS.IPASS>, IReserveable<IChannel>
    {
        protected IChannel TcpChannel;

        private readonly ConcurrentDictionary<IChannel, bool> idleChannels = new ConcurrentDictionary<IChannel, bool>();

        private readonly ConcurrentDictionary<string, IChannel> relationChannels = new ConcurrentDictionary<string, IChannel>();

        private readonly ConcurrentDictionary<ChannelHandlerContext, DateTime> clientOfChannels = new ConcurrentDictionary<ChannelHandlerContext, DateTime>();

        private const int TimeoutLimitMilliseconds = 1000;

        // How long a request waits for a busy COLA channel before it is answered with RC 96; 0 answers at once.
        private int idleChannelWaitMilliseconds = IdleChannelRetry.DefaultMaxWaitMilliseconds;

        public bool Take(IChannel item)
        {
            return this.idleChannels.TryUpdate(item, false, true);
        }

        public void Release(IChannel item)
        {
            if (!this.idleChannels.TryAdd(item, true))
            {
                this.idleChannels.TryUpdate(item, true, false);
            }
        }

        public bool IsTake(IChannel item)
        {
            return !this.idleChannels[item];
        }

        public bool Remove(IChannel item)
        {
            bool outItem;
            return this.idleChannels.TryRemove(item, out outItem);
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
                Logger.Error($"IpassColaService RelationChannels[{relationId}] have Channel LocalEndPoint[{this.relationChannels[relationId]?.LocalEndPoint()}] => RemoteEndPoint[{this.relationChannels[relationId]?.RemoteEndPoint()}]");
            }

            this.relationChannels[relationId] = channel;
        }

        public IChannel RemoveRelation(string relationId)
        {
            this.relationChannels.TryRemove(relationId, out var outChannel);
            return outChannel;
        }

        public IChannel GetIdleChannel()
        {
            return this.idleChannels.Keys.FirstOrDefault(this.Take);
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
        protected override bool DispatchTask(TM.ISO8583.IPASS.IPASS item)
        {
            if (!IdleChannelRetry.IsRetry(item))
            {
                DisplayChannelStatus();
            }
            //this.Logger.Debug($"Dispatch Message: {item.Mti()}");
            var relationId = item.Extend<RelationIdKey, string>();
            //this for response channel
            IChannel channel;
            if (Convert.ToInt16(item.Mti()[2]) % 2 == 1)
            {
                channel = this.RemoveRelation(relationId);
                this.Logger.Debug($"IpassColaService Dispatch Message: {item.Mti()}, RelationId: {relationId}, Remove Response Channel: LocalEndPoint[{channel?.LocalEndPoint()}], RemoteEndPoint[{channel?.RemoteEndPoint()}]");
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
                this.Logger.Info($"IpassColaService DispatchTask Get channel: Local[{channel?.LocalEndPoint()}], Remote[{channel?.RemoteEndPoint()}]");
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

                    TM.ISO8583.IPASS.IPASS response;
                    string requestRelationId;
                    if (this.idleChannels.Count == 0)
                    {
                        this.Logger.Fatal($"No online Cola channel, return message: {item.Mti()}");
                        response = new IpassMessageBase(item, "96").GetIpassMessage();
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
                                                             // $"{response.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                                             response.Mti().Substring(0, 2).Equals("08") ? $"{response.Core().Field(FIELD.F_7) ?? $"{0:D10}"}" : $"{0:D10}",
                                                          $"{response.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                                          $"{response.Core().Field(FIELD.F_36) ?? $"{0:D48}"}",
                                                          $"{response.Core().Field(FIELD.F_41) ?? $"{0:D8}"}"
                                                          );

                        response.Extend<RelationIdKey>(requestRelationId);
                        CyberGWIPASSResource.CyberGWIPASS.Put(response);
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
                                                          //     $"{item.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                                          item.Mti().Substring(0, 2).Equals("08") ? $"{item.Core().Field(FIELD.F_7) ?? $"{0:D10}"}" : $"{0:D10}",
                                                       $"{item.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                                       $"{item.Core().Field(FIELD.F_36) ?? $"{0:D48}"}",
                                                       $"{item.Core().Field(FIELD.F_41) ?? $"{0:D8}"}"
                                                       );
                    this.Logger.Warn($"Can't find free Cola channel for message: {item.Mti()}, RelationId[{itemRelationId}]");
                    response = new IpassMessageBase(item, "96").GetIpassMessage();

                    requestRelationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                                          // $"{response.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                                          response.Mti().Substring(0, 2).Equals("08") ? $"{response.Core().Field(FIELD.F_7) ?? $"{0:D10}"}" : $"{0:D10}",
                                                      $"{response.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                                      $"{response.Core().Field(FIELD.F_36) ?? $"{0:D48}"}",
                                                      $"{response.Core().Field(FIELD.F_41) ?? $"{0:D8}"}"
                                                      );

                    response.Extend<RelationIdKey>(requestRelationId);
                    CyberGWIPASSResource.CyberGWIPASS.Put(response);
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
            this.TcpChannel =
                new ServerBootstrap().ChannelType(typeof(TcpServerSocketChannel))
                    .Handler(new ServiceProviderLoggerHandlerIPASS("IpassColaService"))
                    .ChildHandler(new IpassColaChannelInitializer(this))
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
}
