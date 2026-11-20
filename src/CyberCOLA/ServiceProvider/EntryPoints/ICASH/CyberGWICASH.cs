// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CyberGWICASH.cs" company="Cybersoft">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the CyberGWICASH type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.ServiceProvider.Resources;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.Handlers;
using Com.Cybersoft4u.ServiceProvider.Utils;
using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.ServiceProvider.Utils.Keys;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.ICASH;
using Com.Cybersoft4u.TM.ISO8583.ICASH.Transaction;
using Com.Cybersoft4u.Yetty.Channel;
using Com.Cybersoft4u.Yetty.Channel.TCP;
using Com.Cybersoft4u.Yetty.Context;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.ICASH
{
    #region DEPENDENCY

    #endregion

    public class CyberGWICASH : QueuedService<TM.ISO8583.ICASH.ICASH>, IReserveable<IChannel>
    {
        protected IChannel TcpChannel;

        private readonly ConcurrentDictionary<IChannel, bool> idleChannels = new ConcurrentDictionary<IChannel, bool>();

        private readonly ConcurrentDictionary<string, IChannel> relationChannels = new ConcurrentDictionary<string, IChannel>();

        private readonly ConcurrentDictionary<ChannelHandlerContext, DateTime> clientOfChannels = new ConcurrentDictionary<ChannelHandlerContext, DateTime>();

        private readonly ConcurrentDictionary<string, int> DispatchTaskKeyValue = new ConcurrentDictionary<string, int>();

        private const int TimeoutLimitMilliseconds = 1000;

        private int DispatchTaskLimitCounter = 1000;

        private volatile uint Round;

        private List<string> allowConnectionRemoteEndPoints = new List<string>();

        private Object objectLock = new Object();

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
            {
                this.Logger.Info($"Add {ctx.Channel.RemoteEndPoint()} Channel At : {dateTimeNow:yyyy-MM-dd HH:mm:ss:fffff}");
                //add for GetIcashChannelInfoStatus
                ctx.Channel.Carrier<IcashChannelInfoKey>(new ChannelSessionInfo(ctx.Channel.LocalEndPoint().ToString(), ctx.Channel.RemoteEndPoint().ToString()));
            }
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
                Logger.Error($"CyberGWICASH RelationChannels[{relationId}] have Channel LocalEndPoint[{this.relationChannels[relationId]?.LocalEndPoint()}] => RemoteEndPoint[{this.relationChannels[relationId]?.RemoteEndPoint()}]");
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
        protected override bool DispatchTask(TM.ISO8583.ICASH.ICASH item)
        {
            DisplayChannelStatus();
            var itemRelationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                              $"{item.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                              $"{item.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                              $"{item.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                              $"{item.Core().Field(FIELD.F_41) ?? $"{0:D16}"}"
                                              );

            var relationId = item.Extend<RelationIdKey, string>();
            var channel = this.RemoveRelation(relationId);
            this.Logger.Info($"1. CyberGWICASH DispatchTask Get channel: {channel?.LocalEndPoint()} => {channel?.RemoteEndPoint()}, MTI[{item.Mti()}], Channel RelationId[{relationId}], Item RelationId[{itemRelationId}]");
            //if channel not null, this message of response from auth. system
            //this test for combined port(CF.Lin 20191127, this process for Auth -> GW Transaction)
            if (ObjectUtils.IsNull(channel) && Convert.ToUInt16(item.Mti()[2]) % 2 == 0)
            {
                if (this.clientOfChannels.Count == 0)
                {
                    this.Logger.Error($"No Icash online channel, drop message: MTI[{item.Mti()}], Channel RelationId[{relationId}], Item RelationId[{itemRelationId}]");
                    return true;
                }

                this.Round++;
                var index = (int)this.Round % this.clientOfChannels.Count;
                while (true)
                {
                    try
                    {
                        channel = (this.clientOfChannels.Keys.ToArray()[index]).Channel;
                        this.Logger.Info($"2. CyberGWICASH DispatchTask Get channel: {channel?.LocalEndPoint()} => {channel?.RemoteEndPoint()}, MTI[{item.Mti()}], Channel RelationId[{relationId}], Item RelationId[{itemRelationId}]");
                        break;
                    }
                    catch (Exception)
                    {
                        index = (int)this.Round % this.clientOfChannels.Count;
                    }
                }
            }

            if (ObjectUtils.IsNull(channel))
            {
                DispatchTaskKeyValue.AddOrUpdate(relationId, 1, (id, currentCount) => currentCount + 1);
                if (DispatchTaskKeyValue.TryGetValue(relationId, out var count))
                {
                    if (count > DispatchTaskLimitCounter)
                    {
                        if (DispatchTaskKeyValue.TryRemove(relationId, out int _))
                        {
                            this.Logger.Error($"Retry limit count exceeded {DispatchTaskLimitCounter}, drop message: MTI[{item.Mti()}], Channel RelationId[{relationId}], Item RelationId[{itemRelationId}]");
                            return true;
                        }
                    }

                }
                Thread.Sleep(20);
                this.Round++;
                if (this.Round == uint.MaxValue)
                    this.Round = 0;
                if (this.Round % 6000 == 0)
                    this.Logger.Warn($"Can't find return channel of message: MTI[{item.Mti()}], Channel RelationId[{relationId}], Item RelationId[{itemRelationId}]");
                return false;
            }
            else
            {
                if (DispatchTaskKeyValue.TryRemove(relationId, out var count))
                {
                    this.Logger.Info($"Remove DispatchTaskKeyValue element of key[{relationId}], count[{count}]");
                }
            }

            channel.Write(new PayloadContext(item));
            return true;
        }

        protected override void PreStartTask()
        {
            var configurationLoaderRoot = new ConfigurationLoaderRoot();
            var serverIp = CommonUtils.GetIPAddressStringByHostName(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ServerServiceConfigSectionName, "server-service-bind-ip").Trim(), AddressFamily.InterNetwork);
            var serverPort = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ServerServiceConfigSectionName, "server-service-bind-port"));
            var backlog = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ServerServiceConfigSectionName, "server-service-backlog"));
            IcashTransactionConst.MessageHeader.IcashIssuerID = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ServerServiceConfigSectionName, "server-tpdu-serverid").Default("0007");
            allowConnectionRemoteEndPoints = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.ServerServiceConfigSectionName, "server-allow-connection-remote-endpoints").Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                                       .Select(data => data.Trim()).Where(s => s != string.Empty).ToList();

            this.TcpChannel = new ServerBootstrap().ChannelType(typeof(TcpServerSocketChannel))
                            .Handler(new ServiceProviderLoggerHandlerICASH("CyberGWICASHSession"))
                            .ChildHandler(new Com.Cybersoft4u.ServiceProvider.EntryPoints.ICASH.Handlers.IcashChannelInitializer(this))
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
            if (clientOfChannels.TryGetValue(ctxKey[0], out var dateTime))
                return Tuple.Create<ChannelHandlerContext, DateTime>(ctxKey[0], dateTime);
            return Tuple.Create<ChannelHandlerContext, DateTime>(null, DateTime.Now);
        }
        private ChannelHandlerContext GetOldestCtx()
        {
            return clientOfChannels.OrderBy(data => data.Value).Select(data => data.Key).FirstOrDefault();
        }

        public bool CheckAllowConnectionEndPoint(string endPoint)
        {
            if (allowConnectionRemoteEndPoints.Count == 0) return true;
            if (allowConnectionRemoteEndPoints.Contains(endPoint))
                return true;
            else
                return false;
        }
        public List<IChannel> GetChannels()
        {
            return this.clientOfChannels.Keys.Select(ctx => ctx.Channel).ToList();
        }
        public void SignOn()
        {
            this.clientOfChannels.Keys.AsParallel().ForAll(ctx => ctx.Channel.Carrier<DisconnectKey>(false));
            this.clientOfChannels.Keys.AsParallel().ForAll(ctx => ctx.Channel.WriteAndFlush(new PayloadContext(IcashTransCreator.IcashSignOnGWRequest())));
        }

        public void SignOn(object objectData)
        {
            bool isFirstTransactionPort = true;
            this.clientOfChannels.Keys.AsParallel().ForAll(ctx => ctx.Channel.Carrier<DisconnectKey>(false));
            var channels = GetChannels();
            foreach (var c1 in channels)
            {
                var remoteEndPoint = c1.RemoteEndPoint().ToString();
                var remotePortNumber = remoteEndPoint.Substring(remoteEndPoint.IndexOf(":", StringComparison.Ordinal) + 1);
                if (isFirstTransactionPort)
                {
                    var payload = (PayloadContext)objectData;
                    var icash = (TM.ISO8583.ICASH.ICASH)payload.Val;
                    var header = icash.Header().Encode();
                    icash = new IcashRequest().Core(icash.Core());
                    if (!ObjectUtils.IsNull(icash.RawVal(FIELD.F_11)))
                    {
                        icash.Core().Field(FIELD.F_11, icash.Core().Field(FIELD.F_11).ToString()?.Remove(0, 1).Insert(0, "9"));
                    }
                    icash.Header().Decode(header);
                    c1?.WriteAndFlush(new PayloadContext(icash));
                    isFirstTransactionPort = false;
                }
                else
                {
                    c1?.WriteAndFlush(new PayloadContext(IcashTransCreator.IcashSignOnGWRequest()));
                }
            }
        }
        public void SignOn(int gatewayRemotePortNumber)
        {
            if (gatewayRemotePortNumber == 0)
            {
                this.clientOfChannels.Keys.AsParallel().ForAll(ctx => ctx.Channel.Carrier<DisconnectKey>(false));
                this.clientOfChannels.Keys.AsParallel().ForAll(ctx => ctx.Channel.WriteAndFlush(new PayloadContext(IcashTransCreator.IcashSignOnGWRequest())));
                return;
            }
            var channels = GetChannels();
            //channels.Find(channel => channel.RemoteEndPoint().ToString().Contains(Convert.ToString(gatewayRemotePortNumber))) ?
            //                   .WriteAndFlush(new PayloadContext(IcashTransCreator.IcashSignOnGWRequest()));
            var targetChannel = channels.Find(channel => channel.RemoteEndPoint().ToString()!.Contains(Convert.ToString(gatewayRemotePortNumber)));
            if (targetChannel != null)
                targetChannel.WriteAndFlush(new PayloadContext(IcashTransCreator.IcashSignOnGWRequest()));
            else
            {
                Logger.Info($"Can not SignOn at remote port number[{gatewayRemotePortNumber}]");
            }
        }
        public void SignOff()
        {
            this.clientOfChannels.Keys.AsParallel().ForAll(ctx => ctx.Channel.Carrier<DisconnectKey>(true));
            this.clientOfChannels.Keys.AsParallel().ForAll(ctx => ctx.Channel.WriteAndFlush(new PayloadContext(IcashTransCreator.IcashSignOffGWRequest())));
        }
        public void SignOff(object objectData)
        {
            bool isFirstTransactionPort = true;
            this.clientOfChannels.Keys.AsParallel().ForAll(ctx => ctx.Channel.Carrier<DisconnectKey>(true));
            var channels = GetChannels();
            foreach (var c1 in channels)
            {
                var remoteEndPoint = c1.RemoteEndPoint().ToString();
                var remotePortNumber = remoteEndPoint.Substring(remoteEndPoint.IndexOf(":", StringComparison.Ordinal) + 1);
                if (isFirstTransactionPort)
                {
                    var payload = (PayloadContext)objectData;
                    var icash = (TM.ISO8583.ICASH.ICASH)payload.Val;
                    var header = icash.Header().Encode();
                    icash = new IcashRequest().Core(icash.Core());
                    icash.Header().Decode(header);
                    if (!ObjectUtils.IsNull(icash.RawVal(FIELD.F_11)))
                    {
                        icash.Core().Field(FIELD.F_11, icash.Core().Field(FIELD.F_11).ToString()?.Remove(0, 1).Insert(0, "9"));
                    }
                    c1?.WriteAndFlush(new PayloadContext(icash));
                    isFirstTransactionPort = false;
                }
                else
                {
                    c1?.WriteAndFlush(new PayloadContext(IcashTransCreator.IcashSignOffGWRequest()));
                }
            }
        }
        public void SignOff(int gatewayRemotePortNumber)
        {
            if (gatewayRemotePortNumber == 0)
            {
                this.clientOfChannels.Keys.AsParallel().ForAll(ctx => ctx.Channel.Carrier<DisconnectKey>(true));
                this.clientOfChannels.Keys.AsParallel().ForAll(ctx => ctx.Channel.WriteAndFlush(new PayloadContext(IcashTransCreator.IcashSignOffGWRequest())));
                return;
            }
            var channels = GetChannels();
            //channels.Find(channel => channel.RemoteEndPoint().ToString().Contains(Convert.ToString(gatewayRemotePortNumber)))?
            //                   .WriteAndFlush(new PayloadContext(IcashTransCreator.IcashSignOffGWRequest()));
            var targetChannel = channels.Find(channel => channel.RemoteEndPoint().ToString()!.Contains(Convert.ToString(gatewayRemotePortNumber)));
            if (targetChannel != null)
                targetChannel.WriteAndFlush(new PayloadContext(IcashTransCreator.IcashSignOffGWRequest()));
            else
            {
                Logger.Info($"Can not SignOff at remote port number[{gatewayRemotePortNumber}]");
            }
        }

        public void EchoTest()
        {
            this.clientOfChannels.Keys.AsParallel().ForAll(ctx => ctx.Channel.WriteAndFlush(new PayloadContext(IcashTransCreator.IcashEchoTestGWRequest())));
        }
        public void EchoTest(object objectData)
        {
            bool isFirstTransactionPort = true;
            var channels = GetChannels();
            foreach (var c1 in channels)
            {
                var remoteEndPoint = c1.RemoteEndPoint().ToString();
                var remotePortNumber = remoteEndPoint.Substring(remoteEndPoint.IndexOf(":", StringComparison.Ordinal) + 1);
                if (isFirstTransactionPort)
                {
                    var payload = (PayloadContext)objectData;
                    var icash = (TM.ISO8583.ICASH.ICASH)payload.Val;
                    var header = icash.Header().Encode();
                    icash = new IcashRequest().Core(icash.Core());
                    if (!ObjectUtils.IsNull(icash.RawVal(FIELD.F_11)))
                    {
                        icash.Core().Field(FIELD.F_11, icash.Core().Field(FIELD.F_11).ToString()?.Remove(0, 1).Insert(0, "9"));
                    }
                    icash.Header().Decode(header);
                    c1?.WriteAndFlush(new PayloadContext(icash));
                    isFirstTransactionPort = false;
                }
                else
                {
                    c1?.WriteAndFlush(new PayloadContext(IcashTransCreator.IcashEchoTestGWRequest()));
                }
            }
        }
        public void EchoTest(int gatewayRemotePortNumber)
        {
            if (gatewayRemotePortNumber == 0)
            {
                this.clientOfChannels.Keys.AsParallel().ForAll(ctx => ctx.Channel.WriteAndFlush(new PayloadContext(IcashTransCreator.IcashEchoTestGWRequest())));
                return;
            }
            var channels = GetChannels();
            //channels.Find(channel => channel.RemoteEndPoint().ToString().Contains(Convert.ToString(gatewayRemotePortNumber)))?
            //                   .WriteAndFlush(new PayloadContext(IcashTransCreator.IcashEchoTestGWRequest()));
            var targetChannel = channels.Find(channel => channel.RemoteEndPoint().ToString()!.Contains(Convert.ToString(gatewayRemotePortNumber)));
            if (targetChannel != null)
                targetChannel.WriteAndFlush(new PayloadContext(IcashTransCreator.IcashEchoTestGWRequest()));
            else
            {
                Logger.Info($"Can not EchoTest at remote port number[{gatewayRemotePortNumber}]");
            }
        }
        public void ChannelWriteMaintenance(object objectData)
        {
            try
            {
                var payload = (PayloadContext)objectData;
                var icash = (TM.ISO8583.ICASH.ICASH)payload.Val;
                var header = icash.Header().Encode();
                var channels = GetChannels();
                if (channels.Count != 0)
                {
                    icash = new IcashRequest().Core(icash.Core());
                    icash.Header().Decode(header);
                    payload.Val = icash;
                    channels[0].WriteAndFlush(payload);
                    this.Logger.Info($"Write Maintenance Data From RemoteEndPoint[{channels[0].RemoteEndPoint()}]");
                    return;
                }
                this.Logger.Info($"No Channel For ChannelWriteMaintenance Process");
            }
            catch (Exception ex)
            {
                Logger.Debug($"{GetType().Name} ChannelWrite Exception:{ex.Message}");
            }
        }

        public string GetIcashChannelInfoStatus()
        {
            string returnString;
            try
            {
                var channels = GetChannels();
                var icashChannelInfoArray = channels.Select(c1 => c1.Carrier<IcashChannelInfoKey, ChannelSessionInfo>()).OrderBy(ep => ep.LocalEndPoint).ToArray();
                //var icashChannelInfoArray = this.clientOfChannels.Keys.ToArray().Select(c1 => c1.Channel.Carrier<IcashChannelInfoKey, ChannelSessionInfo>()).OrderBy(ep => ep.LocalEndPoint).ToArray();
                // Initialize array list.
                ArrayList icashChannelInfoArrayList = new ArrayList();

                // Add the items to array list.
                icashChannelInfoArrayList.AddRange(icashChannelInfoArray);
                XmlDocument doc = new XmlDocument();
                XmlSerializer serializer = new XmlSerializer(typeof(ArrayList), new Type[] { typeof(ChannelSessionInfo) });
                using (MemoryStream stream = new System.IO.MemoryStream())
                {
                    try
                    {
                        serializer.Serialize(stream, icashChannelInfoArrayList);
                        stream.Position = 0;
                        doc.Load(stream);
                        returnString = doc.InnerXml;
                    }
                    catch (Exception ex)
                    {
                        this.Logger.Fatal($"GetIcashChannelInfoStatus Inner Exception:{ex.Message})");
                        returnString = string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                this.Logger.Fatal($"GetIcashChannelInfoStatus Outer Exception:{ex.Message})");
                returnString = string.Empty;
            }
            return returnString;
        }
        public string GetConnectionStatus()
        {
            StringBuilder sb = new StringBuilder();
            var channels = GetChannels();
            sb.Append($"{Environment.NewLine}LocalEndPoint --> RemoteEndPoint{Environment.NewLine}");
            foreach (var channel in channels)
            {
                sb.Append($"[{channel.LocalEndPoint()}] --> [{channel.RemoteEndPoint()}]{Environment.NewLine}");
            }
            return sb.ToString();
        }
    }
}
