// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CyberGWTWIN.cs" company="Cybersoft">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the CyberGWTWIN type.
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
using Com.Cybersoft4u.ServiceProvider.EntryPoints.TWIN.Handlers;
using Com.Cybersoft4u.ServiceProvider.Resources;
using Com.Cybersoft4u.ServiceProvider.Utils;
using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.ServiceProvider.Utils.Keys;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.TWIN;
using Com.Cybersoft4u.TM.ISO8583.TWIN.Transaction;
using Com.Cybersoft4u.Yetty.Channel;
using Com.Cybersoft4u.Yetty.Channel.TCP;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.CommonLib.AuthUtils;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.TWIN
{
    public class CyberGWTWIN : QueuedService<TWINIso8583>, IReviveService<IChannel>
    {
        public bool AutoReSignOn = false;

        private readonly object ChannelLocker = new object();

        private readonly object GetFreeLocalEndPointLocker = new object();

        private readonly object GetFreeTwinEndPointLocker = new object();

        private readonly List<IChannel> Channels = new List<IChannel>();

        private List<string> MaintenanceLocalPorts = new List<string>();

        private readonly ConcurrentDictionary<string, IChannel> RelationChannels = new ConcurrentDictionary<string, IChannel>();

        private readonly ConcurrentDictionary<string, int> DispatchTaskKeyValue = new ConcurrentDictionary<string, int>();

        private TwinEndPoint[] TwinEndPoints;

        private volatile int ChannelCount;

        private volatile uint Round;

        private Random rnd = new Random();

        private int DispatchTaskLimitCounter = 1000;

        public void Remove(IChannel item)
        {
            Monitor.Enter(this.ChannelLocker);
            this.Channels.Remove(item);
            this.RelationChannels.ToList().ForEach(
                pair =>
                {
                    if (!pair.Value.Equals(item))
                    {
                        return;
                    }

                    IChannel outChannel;
                    this.RelationChannels.TryRemove(pair.Key, out outChannel);
                });

            this.ChannelCount = this.Channels.Count;
            Monitor.Exit(this.ChannelLocker);
        }
        public void Release(IChannel item)
        {
            Monitor.Enter(this.ChannelLocker);
            this.Channels.Add(item);
            this.ChannelCount = this.Channels.Count;
            Monitor.Exit(this.ChannelLocker);
        }

        // Parallel walks run on a copy taken under the lock: Remove()/Release() change the list meanwhile, and
        // Disconnect() makes every channel remove itself while the list is being walked.
        private IChannel[] ChannelsSnapshot()
        {
            lock (this.ChannelLocker)
            {
                return this.Channels.ToArray();
            }
        }
        public void RegisterRelation(string relationId, IChannel channel)
        {
            if (this.RelationChannels.ContainsKey(relationId))
            {
                Logger.Error($"CyberGWTWIN RelationChannels[{relationId}] have Channel LocalEndPoint[{this.RelationChannels[relationId]?.LocalEndPoint()}] => RemoteEndPoint[{this.RelationChannels[relationId]?.RemoteEndPoint()}]");
            }

            this.RelationChannels[relationId] = channel;
        }
        public IChannel RemoveRelation(string relationId)
        {
            this.RelationChannels.TryRemove(relationId, out var outChannel);
            return outChannel;
        }
        public void SignOn()
        {
            this.AutoReSignOn = true;
            this.ChannelsSnapshot().AsParallel().ForAll(channel => channel.Carrier<DisconnectKey>(false));
            this.ChannelsSnapshot().AsParallel().ForAll(channel => channel.WriteAndFlush(new PayloadContext(TwinTransCreator.TwinSignOnGWRequest())));
        }

        public void SignOn(object objectData)
        {
            bool isFirstTransactionPort = true;
            this.AutoReSignOn = true;
            this.ChannelsSnapshot().AsParallel().ForAll(channel => channel.Carrier<DisconnectKey>(false));
            foreach (var c1 in Channels)
            {
                var localEndPoint = c1.LocalEndPoint().ToString();
                var localPortNumber = localEndPoint.Substring(localEndPoint.IndexOf(":", StringComparison.Ordinal) + 1);
                //by pass maintenance port
                if (!MaintenanceLocalPorts.Contains(localPortNumber))
                {
                    if (isFirstTransactionPort)
                    {
                        var payload = (PayloadContext)objectData;
                        var twin = (TWINIso8583)payload.Val;
                        var header = twin.Header().Encode();
                        twin = new TwinRequest().Core(twin.Core());
                        if (!ObjectUtils.IsNull(twin.RawVal(FIELD.F_11)))
                        {
                            twin.Core().Field(FIELD.F_11, twin.Core().Field(FIELD.F_11).ToString()?.Remove(0, 1).Insert(0, "9"));
                        }
                        twin.Header().Decode(header);
                        c1?.WriteAndFlush(new PayloadContext(twin));
                        isFirstTransactionPort = false;
                    }
                    else
                    {
                        c1?.WriteAndFlush(new PayloadContext(TwinTransCreator.TwinSignOnGWRequest()));
                    }
                }
            }
        }

        public void SignOn(int gatewayLocalPortNumber)
        {
            if (gatewayLocalPortNumber == 0)
            {
                this.AutoReSignOn = true;
                this.ChannelsSnapshot().AsParallel().ForAll(channel => channel.Carrier<DisconnectKey>(false));
                this.ChannelsSnapshot().AsParallel().ForAll(channel => channel.WriteAndFlush(new PayloadContext(TwinTransCreator.TwinSignOnGWRequest())));
                return;
            }
            //this.Channels.Find(channel => channel.LocalEndPoint().ToString().Contains(Convert.ToString(gatewayLocalPortNumber)))?
            //                   .WriteAndFlush(new PayloadContext(TwinTransCreator.TwinSignOnGWRequest()));
            var targetChannel = this.Channels.Find(channel => channel.RemoteEndPoint().ToString()!.Contains(Convert.ToString(gatewayLocalPortNumber)));
            if (targetChannel != null)
                targetChannel.WriteAndFlush(new PayloadContext(TwinTransCreator.TwinSignOnGWRequest()));
            else
            {
                Logger.Info($"Can not SignOn at local port number[{gatewayLocalPortNumber}]");
            }
        }
        public List<IChannel> GetChannels()
        {
            return (Channels);
        }
        public void SignOff()
        {
            this.AutoReSignOn = false;
            this.ChannelsSnapshot().AsParallel().ForAll(channel => channel.Carrier<DisconnectKey>(true));
            this.ChannelsSnapshot().AsParallel().ForAll(channel => channel.WriteAndFlush(new PayloadContext(TwinTransCreator.TwinSignOffGWRequest())));
        }
        public void SignOff(object objectData)
        {
            bool isFirstTransactionPort = true;
            this.AutoReSignOn = false;
            this.ChannelsSnapshot().AsParallel().ForAll(channel => channel.Carrier<DisconnectKey>(true));
            foreach (var c1 in Channels)
            {
                var localEndPoint = c1.LocalEndPoint().ToString();
                var localPortNumber = localEndPoint.Substring(localEndPoint.IndexOf(":", StringComparison.Ordinal) + 1);
                //by pass maintenance port
                if (!MaintenanceLocalPorts.Contains(localPortNumber))
                {
                    if (isFirstTransactionPort)
                    {
                        var payload = (PayloadContext)objectData;
                        var twin = (TWINIso8583)payload.Val;
                        var header = twin.Header().Encode();
                        twin = new TwinRequest().Core(twin.Core());
                        twin.Header().Decode(header);
                        if (!ObjectUtils.IsNull(twin.RawVal(FIELD.F_11)))
                        {
                            twin.Core().Field(FIELD.F_11, twin.Core().Field(FIELD.F_11).ToString()?.Remove(0, 1).Insert(0, "9"));
                        }
                        c1?.WriteAndFlush(new PayloadContext(twin));
                        isFirstTransactionPort = false;
                    }
                    else
                    {
                        c1?.WriteAndFlush(new PayloadContext(TwinTransCreator.TwinSignOffGWRequest()));
                    }
                }
            }
        }
        public void SignOff(int gatewayLocalPortNumber)
        {
            if (gatewayLocalPortNumber == 0)
            {
                this.AutoReSignOn = false;
                this.ChannelsSnapshot().AsParallel().ForAll(channel => channel.Carrier<DisconnectKey>(true));
                this.ChannelsSnapshot().AsParallel().ForAll(channel => channel.WriteAndFlush(new PayloadContext(TwinTransCreator.TwinSignOffGWRequest())));
                return;
            }
            //this.Channels.Find(channel => channel.LocalEndPoint().ToString().Contains(Convert.ToString(gatewayLocalPortNumber)))?
            //                   .WriteAndFlush(new PayloadContext(TwinTransCreator.TwinSignOffGWRequest()));
            var targetChannel = this.Channels.Find(channel => channel.RemoteEndPoint().ToString()!.Contains(Convert.ToString(gatewayLocalPortNumber)));
            if (targetChannel != null)
                targetChannel.WriteAndFlush(new PayloadContext(TwinTransCreator.TwinSignOffGWRequest()));
            else
            {
                Logger.Info($"Can not SignOff at local port number[{gatewayLocalPortNumber}]");
            }
        }
        public void ChangeKey()
        {
            IChannel resultChannel = null;
            foreach (var c1 in Channels)
            {
                var localEndPoint = c1.LocalEndPoint().ToString();
                var localPortNumber = localEndPoint.Substring(localEndPoint.IndexOf(":", StringComparison.Ordinal) + 1);
                if (!MaintenanceLocalPorts.Contains(localPortNumber))
                {
                    resultChannel = c1;
                    break;
                }
            }
            if (resultChannel != null)
                resultChannel?.WriteAndFlush(new PayloadContext(new CommonLib.TwinMessage.TwinMessage0800(TwinTransactionConst.NetworkManagementInformationCode.ChangeKey).GetTwinMessage()));
            else
                this.Logger.Info($"No Transaction Channel For ChangeKey Process");
        }
        public void ChangeKey(object objectData)
        {
            IChannel resultChannel = null;
            foreach (var c1 in Channels)
            {
                var localEndPoint = c1.LocalEndPoint().ToString();
                var localPortNumber = localEndPoint.Substring(localEndPoint.IndexOf(":", StringComparison.Ordinal) + 1);
                if (!MaintenanceLocalPorts.Contains(localPortNumber))
                {
                    resultChannel = c1;
                    break;
                }
            }
            if (resultChannel != null)
                resultChannel?.WriteAndFlush(objectData);
            else
                this.Logger.Info($"No Transaction Channel For ChangeKey Process");
        }
        public void RepeatKey()
        {
            IChannel resultChannel = null;
            foreach (var c1 in Channels)
            {
                var localEndPoint = c1.LocalEndPoint().ToString();
                var localPortNumber = localEndPoint.Substring(localEndPoint.IndexOf(":", StringComparison.Ordinal) + 1);
                if (!MaintenanceLocalPorts.Contains(localPortNumber))
                {
                    resultChannel = c1;
                    break;
                }
            }
            if (resultChannel != null)
                resultChannel?.WriteAndFlush(new PayloadContext(new CommonLib.TwinMessage.TwinMessage0800(TwinTransactionConst.NetworkManagementInformationCode.RepeatKey).GetTwinMessage()));
            else
                this.Logger.Info($"No Transaction Channel For RepeatKey Process");
        }
        public void RepeatKey(object objectData)
        {

            IChannel resultChannel = null;
            foreach (var c1 in Channels)
            {
                var localEndPoint = c1.LocalEndPoint().ToString();
                var localPortNumber = localEndPoint.Substring(localEndPoint.IndexOf(":", StringComparison.Ordinal) + 1);
                if (!MaintenanceLocalPorts.Contains(localPortNumber))
                {
                    resultChannel = c1;
                    break;
                }
            }
            if (resultChannel != null)
                resultChannel?.WriteAndFlush(objectData);
            else
                this.Logger.Info($"No Transaction Channel For RepeatKey Process");
        }
        public void VerifyKey()
        {
            IChannel resultChannel = null;
            foreach (var c1 in Channels)
            {
                var localEndPoint = c1.LocalEndPoint().ToString();
                var localPortNumber = localEndPoint.Substring(localEndPoint.IndexOf(":", StringComparison.Ordinal) + 1);
                if (!MaintenanceLocalPorts.Contains(localPortNumber))
                {
                    resultChannel = c1;
                    break;
                }
            }
            if (resultChannel != null)
                resultChannel?.WriteAndFlush(new PayloadContext(new CommonLib.TwinMessage.TwinMessage0800(TwinTransactionConst.NetworkManagementInformationCode.VerifyKey).GetTwinMessage()));
            else
                this.Logger.Info($"No Transaction Channel For VerifyKey Process");
        }
        public void VerifyKey(object objectData)
        {
            IChannel resultChannel = null;
            foreach (var c1 in Channels)
            {
                var localEndPoint = c1.LocalEndPoint().ToString();
                var localPortNumber = localEndPoint.Substring(localEndPoint.IndexOf(":", StringComparison.Ordinal) + 1);
                if (!MaintenanceLocalPorts.Contains(localPortNumber))
                {
                    resultChannel = c1;
                    resultChannel?.WriteAndFlush(objectData);
                    break;
                }
            }
            if (resultChannel != null)
                resultChannel?.WriteAndFlush(objectData);
            else
                this.Logger.Info($"No Transaction Channel For VerifyKey Process");
        }
        public void EchoTest()
        {
            this.ChannelsSnapshot().AsParallel().ForAll(channel => channel.WriteAndFlush(new PayloadContext(TwinTransCreator.TwinEchoTestGWRequest())));
        }
        public void EchoTest(object objectData)
        {
            bool isFirstTransactionPort = true;
            foreach (var c1 in Channels)
            {
                var localEndPoint = c1.LocalEndPoint().ToString();
                var localPortNumber = localEndPoint.Substring(localEndPoint.IndexOf(":", StringComparison.Ordinal) + 1);
                //by pass maintenance port
                if (!MaintenanceLocalPorts.Contains(localPortNumber))
                {
                    if (isFirstTransactionPort)
                    {
                        var payload = (PayloadContext)objectData;
                        var twin = (TWINIso8583)payload.Val;
                        var header = twin.Header().Encode();
                        twin = new TwinRequest().Core(twin.Core());
                        if (!ObjectUtils.IsNull(twin.RawVal(FIELD.F_11)))
                        {
                            twin.Core().Field(FIELD.F_11, twin.Core().Field(FIELD.F_11).ToString()?.Remove(0, 1).Insert(0, "9"));
                        }
                        twin.Header().Decode(header);
                        c1?.WriteAndFlush(new PayloadContext(twin));
                        isFirstTransactionPort = false;
                    }
                    else
                    {
                        c1?.WriteAndFlush(new PayloadContext(TwinTransCreator.TwinEchoTestGWRequest()));
                    }
                }
            }
        }
        public void EchoTest(int gatewayLocalPortNumber)
        {
            if (gatewayLocalPortNumber == 0)
            {
                this.ChannelsSnapshot().AsParallel().ForAll(channel => channel.WriteAndFlush(new PayloadContext(TwinTransCreator.TwinEchoTestGWRequest())));
                return;
            }
            //this.Channels.Find(channel => channel.LocalEndPoint().ToString().Contains(Convert.ToString(gatewayLocalPortNumber)))?
            //                   .WriteAndFlush(new PayloadContext(TwinTransCreator.TwinEchoTestGWRequest()));
            var targetChannel = this.Channels.Find(channel => channel.RemoteEndPoint().ToString()!.Contains(Convert.ToString(gatewayLocalPortNumber)));
            if (targetChannel != null)
                targetChannel.WriteAndFlush(new PayloadContext(TwinTransCreator.TwinEchoTestGWRequest()));
            else
            {
                Logger.Info($"Can not EchoTest at local port number[{gatewayLocalPortNumber}]");
            }
        }
        public void ChannelWriteMaintenance(object objectData)
        {
            try
            {
                bool findMaintChannel = false;
                var payload = (PayloadContext)objectData;
                var twin = (TWINIso8583)payload.Val;
                var header = twin.Header().Encode();
                foreach (var c1 in Channels)
                {
                    var localEndPoint = c1.LocalEndPoint().ToString();
                    var localPortNumber = localEndPoint.Substring(localEndPoint.IndexOf(":", StringComparison.Ordinal) + 1);
                    if (MaintenanceLocalPorts.Contains(localPortNumber))
                    {
                        //add this for correct ISO Header reset default
                        twin = new TwinRequest().Core(twin.Core());
                        twin.Header().Decode(header);
                        payload.Val = twin;
                        c1.WriteAndFlush(payload);
                        this.Logger.Info($"Write Maintenance Data From LocalEndPoint[{c1.LocalEndPoint()}]");
                        findMaintChannel = true;
                        break;
                    }
                }
                if (!findMaintChannel)
                    this.Logger.Info($"No Maintenance Channel For ChannelWriteMaintenance Process");
            }
            catch (Exception ex)
            {
                Logger.Debug($"{GetType().Name} ChannelWrite Exception:{ex.Message}");
            }
        }
        public void ChannelWrite(object objectData)
        {
            try
            {
                if (Channels.Count == 0)
                {
                    Logger.Error("ChannelWrite No Client Channel Use");
                    return;
                }
                var payload = (PayloadContext)objectData;
                var twin = (TWINIso8583)payload.Val;
                var header = twin.Header().Encode();
                //random select localEndPoint
                var channelNumber = ColaCommUtility.GetSecurityNumber(Channels.Count);
                var channel = Channels.ToArray()[channelNumber];
                var localEndPoint = channel.LocalEndPoint().ToString();
                var localPortNumber = localEndPoint.Substring(localEndPoint.IndexOf(":", StringComparison.Ordinal) + 1);
                //add this for correct ISO Header reset default
                twin = new TwinRequest().Core(twin.Core());
                twin.Header().Decode(header);
                payload.Val = twin;
                channel.WriteAndFlush(payload);
                Logger.Info($"Send Request Message From LocalEedPoint[{channel.LocalEndPoint()}] To RemoteEndPoint[{channel.RemoteEndPoint()}]");
            }
            catch (Exception ex)
            {
                Logger.Debug($"{GetType().Name} ChannelWrite Exception:{ex.Message}");
            }
        }
        public void Disconnect()
        {
            // Close() is not called: TcpSocketChannel.Close() throws NotSupportedException, and Disconnect() already
            // unregisters the channel (its revive handler then removes it from the list).
            this.ChannelsSnapshot().AsParallel()
                .ForAll(channel => channel.Disconnect());
        }
        public IPEndPoint GetFreeLocalEndPoint()
        {
            try
            {
                Monitor.Enter(GetFreeLocalEndPointLocker);
                var localEndPointList = new List<IPEndPoint>();
                var copyChannels = new List<IChannel>(Channels.ToArray());
                foreach (var endPoint in TwinEndPoints)
                {
                    var localEndPoint = new IPEndPoint(IPAddress.Parse(endPoint.LocalIp), endPoint.LocalPort);
                    //var channel = Channels.Where(x => (x.LocalEndPoint().ToString().Equals(localEndPoint.ToString()))).ToList();
                    var channel = copyChannels.Where(x => (x.LocalEndPoint().ToString()!.Equals(localEndPoint.ToString()))).ToList();

                    if (channel.Count == 0)
                    {
                        localEndPointList.Add(localEndPoint);
                        //if   ((freeLocalEndPoint != null ) &&
                        //    !(freeLocalEndPoint.ToString().Equals(localEndPoint.ToString())))
                        //    continue;
                        //freeLocalEndPoint = localEndPoint;
                        //Monitor.Exit(GetFreeLocalEndPointLocker);
                        //return localEndPoint;
                    }
                }
                this.Logger.Info($"LocalEndPointCount[{localEndPointList.Count}]");
                localEndPointList.ForEach(endPoint => this.Logger.Info($"FreeLocalEndPoint[{endPoint.ToString()}]"));
                if (localEndPointList.Count == 0)
                    return null;
                //random select localEndPoint
                var localEndPointNumber = ColaCommUtility.GetSecurityNumber(localEndPointList.Count);
                return localEndPointList[localEndPointNumber];
            }
            catch (Exception ex)
            {
                Logger.Warn($"GetFreeLocalEndPoint Exception:{ex.Message})");
                return null;
            }
            finally
            {
                Monitor.Exit(GetFreeLocalEndPointLocker);
            }
        }
        public TwinEndPoint GetFreeTwinEndPoint()
        {
            try
            {
                Monitor.Enter(GetFreeTwinEndPointLocker);
                var twinEndPointList = new List<TwinEndPoint>();
                foreach (var endPoint in TwinEndPoints)
                {
                    var localEndPoint = new IPEndPoint(IPAddress.Parse(endPoint.LocalIp), endPoint.LocalPort);
                    var channel = Channels.Where(x => (x.LocalEndPoint().ToString()!.Equals(localEndPoint.ToString()))).ToList();
                    if (channel.Count == 0)
                    {
                        twinEndPointList.Add(endPoint);
                        //if   ((freeLocalEndPoint != null ) &&
                        //    !(freeLocalEndPoint.ToString().Equals(localEndPoint.ToString())))
                        //    continue;
                        //freeLocalEndPoint = localEndPoint;
                        //Monitor.Exit(GetFreeLocalEndPointLocker);
                        //return localEndPoint;
                    }
                }
                this.Logger.Info($"LocalEndPointCount[{twinEndPointList.Count}]");
                twinEndPointList.ForEach(endPoint => this.Logger.Info($"FreeTwinEndPoint Remote[{endPoint.RemoteIp}:{endPoint.RemotePort}], Local[{endPoint.LocalIp}:{endPoint.LocalPort}]"));
                if (twinEndPointList.Count == 0)
                    return null;
                //random select localEndPoint
                var localEndPointNumber = ColaCommUtility.GetSecurityNumber(twinEndPointList.Count);
                return twinEndPointList[localEndPointNumber];
            }
            catch (Exception ex)
            {
                this.Logger.Warn($"GetFreeTwinEndPoint Exception:{ex.Message})");
                return null;
            }
            finally
            {
                Monitor.Exit(GetFreeTwinEndPointLocker);
            }
        }
        // Prefers the session's own (original) local endpoint, so a revived session keeps its configured port and
        // concurrent revives cannot pick the same one; falls back to any free configured endpoint if it is taken.
        public TwinEndPoint GetFreeTwinEndPoint(IPEndPoint preferredLocalEndPoint)
        {
            if (preferredLocalEndPoint != null)
            {
                var preferred = TwinEndPoints.FirstOrDefault(e => e.LocalPort == preferredLocalEndPoint.Port
                                                               && IPAddress.Parse(e.LocalIp).Equals(preferredLocalEndPoint.Address));
                var inUse = Channels.ToArray().Any(x => x.LocalEndPoint()?.ToString() == preferredLocalEndPoint.ToString());
                if (preferred != null && !inUse)
                {
                    return preferred;
                }
            }

            return GetFreeTwinEndPoint();
        }

        public string GetTwinChannelInfoStatus()
        {
            string returnString;
            try
            {
                var twinChannelInfoArray = this.Channels.Select(c1 => c1.Carrier<TwinChannelInfoKey, ChannelSessionInfo>()).OrderBy(ep => ep.LocalEndPoint).ToArray();
                // Initialize array list.
                ArrayList twinChannelInfoArrayList = new ArrayList();

                // Add the items to array list.
                twinChannelInfoArrayList.AddRange(twinChannelInfoArray);
                XmlDocument doc = new XmlDocument();
                XmlSerializer serializer = new XmlSerializer(typeof(ArrayList), new Type[] { typeof(ChannelSessionInfo) });
                using (MemoryStream stream = new System.IO.MemoryStream())
                {
                    try
                    {
                        serializer.Serialize(stream, twinChannelInfoArrayList);
                        stream.Position = 0;
                        doc.Load(stream);
                        returnString = doc.InnerXml;
                    }
                    catch (Exception ex)
                    {
                        this.Logger.Fatal($"GetTwinChannelInfoStatus Inner Exception:{ex.Message})");
                        returnString = string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                this.Logger.Fatal($"GetTwinChannelInfoStatus Outer Exception:{ex.Message})");
                returnString = string.Empty;
            }
            return returnString;
        }
        public string GetConnectionStatus()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"{Environment.NewLine}LocalEndPoint --> RemoteEndPoint{Environment.NewLine}");
            foreach (var channel in Channels)
            {
                sb.Append($"[{channel.LocalEndPoint()}] --> [{channel.RemoteEndPoint()}]{Environment.NewLine}");
            }
            return sb.ToString();
        }
        public void ChannelForAllWriteAndFlush(object obj)
        {
            this.ChannelsSnapshot().AsParallel().ForAll(channel => channel.WriteAndFlush(obj));
        }
        protected override bool DispatchTask(TWINIso8583 item)
        {
            var itemRelationId = string.Format(SystemConstantData.CommonData.GatewayRelationIdStringFormat,
                                               $"{item.Core().Field(FIELD.F_7) ?? $"{0:D10}"}",
                                               $"{item.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                               $"{item.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                               $"{item.Core().Field(FIELD.F_41) ?? $"{0:D16}"}"
                                               );

            var relationId = item.Extend<RelationIdKey, string>();
            var channel = this.RemoveRelation(relationId);
            this.Logger.Info($"1. CyberGWTWIN DispatchTask Get channel: {channel?.LocalEndPoint()} => {channel?.RemoteEndPoint()}, MTI[{item.Mti()}], Channel RelationId[{relationId}], Item RelationId[{itemRelationId}]");
            //if channel not null, this message of response from auth. system
            //this test for combined port(CF.Lin 20191127, this process for Auth -> GW Transaction)
            if (ObjectUtils.IsNull(channel) && Convert.ToUInt16(item.Mti()[2]) % 2 == 0)
            {
                if (this.Channels.Count == 0)
                {
                    this.Logger.Error($"No online channel, drop message: MTI[{item.Mti()}], Channel RelationId[{relationId}], Item RelationId[{itemRelationId}]");
                    return true;
                }

                this.Round++;
                var index = (int)this.Round % this.ChannelCount;
                while (true)
                {
                    try
                    {
                        channel = this.Channels[index];
                        this.Logger.Info($"2. CyberGWTWIN DispatchTask Get channel: {channel?.LocalEndPoint()} => {channel?.RemoteEndPoint()}, MTI[{item.Mti()}], Channel RelationId[{relationId}], Item RelationId[{itemRelationId}]");
                        break;
                    }
                    catch (Exception)
                    {
                        index = (int)this.Round % this.ChannelCount;
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

            channel?.Write(new PayloadContext(item));
            return true;
        }
        protected override void PreStartTask()
        {
            var configurationLoaderRoot = new ConfigurationLoaderRoot();
            var targetIp = CommonUtils.GetIPAddressStringByHostName(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.TwinServiceConfigSectionName, "twin-service-target-ip").Trim(), AddressFamily.InterNetwork);
            var targetPorts = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.TwinServiceConfigSectionName, "twin-service-target-ports").Split(',');
            var localIp = CommonUtils.GetIPAddressStringByHostName(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.TwinServiceConfigSectionName, "twin-service-local-ip").Trim(), AddressFamily.InterNetwork);
            var localPorts = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.TwinServiceConfigSectionName, "twin-service-local-ports").Split(',');
            var sessions = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.TwinServiceConfigSectionName, "twin-service-sessions").Trim());
            // Wait before reconnecting a session the external host dropped; without the key it stays 10 s.
            var reconnectIntervalMilliseconds = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.TwinServiceConfigSectionName, "twin-service-reconnect-interval-ms").Default("10000"));
            MaintenanceLocalPorts = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.TwinServiceConfigSectionName, "twin-service-maintenance-local-ports").Split(',').ToList();

            if (targetPorts.Length == sessions && localPorts.Length == sessions)
            {
            }
            else
            {
                throw new ArgumentException("twin configuration has error, please fix App.config");
            }
            TwinEndPoints = new TwinEndPoint[sessions];
            for (var i = 0; i < sessions; i++)
            {
                var twinEndPoint = new TwinEndPoint(targetIp,
                                                    Convert.ToInt32(targetPorts[i]),
                                                    localIp,
                                                    Convert.ToInt32(localPorts[i]));
                this.TwinEndPoints[i] = twinEndPoint;
            }
            //var bootstrap = new Bootstrap();
            //bootstrap.ChannelType(typeof(TcpSocketChannel));
            for (var i = 0; i < sessions; i++)
            {
                var remoteEndPoint = new IPEndPoint(IPAddress.Parse(TwinEndPoints[i].RemoteIp), TwinEndPoints[i].RemotePort);
                var localEndPoint = new IPEndPoint(IPAddress.Parse(TwinEndPoints[i].LocalIp), TwinEndPoints[i].LocalPort);
                var maintenanceLocalPortFlag = MaintenanceLocalPorts.Contains(Convert.ToString(TwinEndPoints[i].LocalPort));
                ThreadPool.QueueUserWorkItem(
                    state =>
                    {
                        try
                        {
                            var bootstrap = new Bootstrap();
                            bootstrap.ChannelType(typeof(TcpSocketChannel));
                            bootstrap.Handler(new TwinChannelInitializer(this, bootstrap, remoteEndPoint, localEndPoint, maintenanceLocalPortFlag, reconnectIntervalMilliseconds))
                                             .SetChannelOption(ChannelOptionLevel.Tcp, ChannelOptionName.NoDelay, true)
                                             .SetChannelOption(ChannelOptionLevel.Socket, ChannelOptionName.ReuseAddress, true);
                            var channel = bootstrap.Connect(remoteEndPoint, localEndPoint).Sync().Channel();
                            channel.Carrier<RemoteEndPointKey>(remoteEndPoint);
                            channel.Carrier<LocalEndPointKey>(localEndPoint);
                            channel.Carrier<ReadTransactionTimeKey>(DateTime.Now);
                            channel.Carrier<TwinChannelInfoKey>(new ChannelSessionInfo(localEndPoint.ToString(), remoteEndPoint.ToString()));
                            //var channel = bootstrap.Connect(remoteEndPoint).Sync().Channel();
                            if (channel.IsConnected() && !channel.IsClosed())
                            {
                                this.Release(channel);
                                this.Logger.Info($"Add Chanel Of LocalEndPoint({channel.LocalEndPoint().ToString()})");
                            }

                        }
                        catch (Exception ex)
                        {
                            this.Logger.Fatal($"LocalEndPoint({localEndPoint.ToString()}) => RemoteEndPoint({remoteEndPoint.ToString()}) Exception:{ex.Message}");
                        }
                    });
                Thread.Sleep(100);
            }
        }
    }
}
