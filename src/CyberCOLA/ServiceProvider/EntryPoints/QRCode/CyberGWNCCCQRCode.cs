// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CyberGWNCCC.cs" company="Cybersoft">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the CyberGWNCCC type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------


using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.NCCC;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.QRCode.Handlers;
using Com.Cybersoft4u.ServiceProvider.Resources;
using Com.Cybersoft4u.ServiceProvider.Utils;
using Com.Cybersoft4u.ServiceProvider.Utils.CarrierKeys;
using Com.Cybersoft4u.ServiceProvider.Utils.Keys;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using Com.Cybersoft4u.TM.ISO8583.Nccc.Transaction;
using Com.Cybersoft4u.Yetty.Channel;
using Com.Cybersoft4u.Yetty.Channel.TCP;
using Com.Cybersoft4u.Yetty.Context;

namespace Com.Cybersoft4u.ServiceProvider.EntryPoints.QRCode
{
    public class CyberGWNCCCQRCode : QueuedService<NcccIso8583>, IReviveService<IChannel>
    {
        public bool AutoReSignOn = false;

        private readonly object ChannelLocker = new object();

        private readonly object GetFreeLocalEndPointLocker = new object();

        private readonly object GetFreeNcccEndPointLocker = new object();

        private readonly List<IChannel> Channels = new List<IChannel>();

        private List<string> MaintenanceLocalPorts = new List<string>();

        private readonly ConcurrentDictionary<string, IChannel> RelationChannels = new ConcurrentDictionary<string, IChannel>();

        private readonly ConcurrentDictionary<string, int> DispatchTaskKeyValue = new ConcurrentDictionary<string, int>();

        private NcccEndPoint[] NcccEndPoints;

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
                Logger.Error($"CyberGWNCCCQRCode RelationChannels[{relationId}] have Channel LocalEndPoint[{this.RelationChannels[relationId]?.LocalEndPoint()}] => RemoteEndPoint[{this.RelationChannels[relationId]?.RemoteEndPoint()}]");
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
            this.ChannelsSnapshot().AsParallel().ForAll(channel => channel.WriteAndFlush(new PayloadContext(NcccTransCreator.SignOnGWRequest())));
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
                        var nccc = (NcccIso8583)payload.Val;
                        var header = nccc.Header().Encode();
                        nccc = new NcccRequest().Core(nccc.Core());
                        nccc.Header().Decode(header);
                        if (!ObjectUtils.IsNull(nccc.RawVal(FIELD.F_48)))
                        {
                            nccc.Core().Field(FIELD.F_48, nccc.Core().Field(FIELD.F_48).ToString().Remove(10, 1).Insert(10, "N"));
                        }
                        c1?.WriteAndFlush(new PayloadContext(nccc));
                        isFirstTransactionPort = false;
                    }
                    else
                    {
                        c1?.WriteAndFlush(new PayloadContext(NcccTransCreator.SignOnGWRequest()));
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
                this.ChannelsSnapshot().AsParallel().ForAll(channel => channel.WriteAndFlush(new PayloadContext(NcccTransCreator.SignOnGWRequest())));
                return;
            }
            //this.Channels.Find(channel => channel.LocalEndPoint().ToString().Contains(Convert.ToString(gatewayLocalPortNumber)))?
            //                   .WriteAndFlush(new PayloadContext(NcccTransCreator.SignOnGWRequest()));
            var targetChannel = this.Channels.Find(channel => channel.RemoteEndPoint().ToString().Contains(Convert.ToString(gatewayLocalPortNumber)));
            if (targetChannel != null)
                targetChannel.WriteAndFlush(new PayloadContext(NcccTransCreator.SignOnGWRequest()));
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
            this.ChannelsSnapshot().AsParallel().ForAll(channel => channel.WriteAndFlush(new PayloadContext(NcccTransCreator.SignOffGWRequest())));
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
                        var nccc = (NcccIso8583)payload.Val;
                        var header = nccc.Header().Encode();
                        nccc = new NcccRequest().Core(nccc.Core());
                        nccc.Header().Decode(header);
                        if (!ObjectUtils.IsNull(nccc.RawVal(FIELD.F_48)))
                        {
                            nccc.Core().Field(FIELD.F_48, nccc.Core().Field(FIELD.F_48).ToString().Remove(10, 1).Insert(10, "N"));
                        }
                        c1?.WriteAndFlush(new PayloadContext(nccc));
                        isFirstTransactionPort = false;
                    }
                    else
                    {
                        c1?.WriteAndFlush(new PayloadContext(NcccTransCreator.SignOffGWRequest()));
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
                this.ChannelsSnapshot().AsParallel().ForAll(channel => channel.WriteAndFlush(new PayloadContext(NcccTransCreator.SignOffGWRequest())));
                return;
            }
            //this.Channels.Find(channel => channel.LocalEndPoint().ToString().Contains(Convert.ToString(gatewayLocalPortNumber)))?
            //                   .WriteAndFlush(new PayloadContext(NcccTransCreator.SignOffGWRequest()));
            var targetChannel = this.Channels.Find(channel => channel.RemoteEndPoint().ToString().Contains(Convert.ToString(gatewayLocalPortNumber)));
            if (targetChannel != null)
                targetChannel.WriteAndFlush(new PayloadContext(NcccTransCreator.SignOffGWRequest()));
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
                resultChannel?.WriteAndFlush(new PayloadContext(new CommonLib.NcccMessage.NcccMessage0800(NcccTransactionConst.NetworkManagementInformationCode.ChangeKey).GetNcccMessage()));
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
                resultChannel?.WriteAndFlush(new PayloadContext(new CommonLib.NcccMessage.NcccMessage0800(NcccTransactionConst.NetworkManagementInformationCode.RepeatKey).GetNcccMessage()));
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
                resultChannel?.WriteAndFlush(new PayloadContext(new CommonLib.NcccMessage.NcccMessage0800(NcccTransactionConst.NetworkManagementInformationCode.VerifyKey).GetNcccMessage()));
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
            this.ChannelsSnapshot().AsParallel().ForAll(channel => channel.WriteAndFlush(new PayloadContext(NcccTransCreator.EchoTestGWRequest())));
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
                        var nccc = (NcccIso8583)payload.Val;
                        var header = nccc.Header().Encode();
                        nccc = new NcccRequest().Core(nccc.Core());
                        nccc.Header().Decode(header);
                        if (!ObjectUtils.IsNull(nccc.RawVal(FIELD.F_11)))
                        {
                            nccc.Core().Field(FIELD.F_11, nccc.Core().Field(FIELD.F_11).ToString().Remove(0, 1).Insert(0, "9"));
                        }
                        c1?.WriteAndFlush(new PayloadContext(nccc));
                        isFirstTransactionPort = false;
                    }
                    else
                    {
                        c1?.WriteAndFlush(new PayloadContext(NcccTransCreator.EchoTestGWRequest()));
                    }
                }
            }
        }
        public void EchoTest(int gatewayLocalPortNumber)
        {
            if (gatewayLocalPortNumber == 0)
            {
                this.ChannelsSnapshot().AsParallel().ForAll(channel => channel.WriteAndFlush(new PayloadContext(NcccTransCreator.EchoTestGWRequest())));
                return;
            }
            //this.Channels.Find(channel => channel.LocalEndPoint().ToString().Contains(Convert.ToString(gatewayLocalPortNumber)))?
            //                   .WriteAndFlush(new PayloadContext(NcccTransCreator.EchoTestGWRequest()));
            var targetChannel = this.Channels.Find(channel => channel.RemoteEndPoint().ToString().Contains(Convert.ToString(gatewayLocalPortNumber)));
            if (targetChannel != null)
                targetChannel.WriteAndFlush(new PayloadContext(NcccTransCreator.EchoTestGWRequest()));
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
                var nccc = (NcccIso8583)payload.Val;
                var header = nccc.Header().Encode();
                foreach (var c1 in Channels)
                {
                    var localEndPoint = c1.LocalEndPoint().ToString();
                    var localPortNumber = localEndPoint.Substring(localEndPoint.IndexOf(":", StringComparison.Ordinal) + 1);
                    if (MaintenanceLocalPorts.Contains(localPortNumber))
                    {
                        //add this for correct ISO Header reset default
                        nccc = new NcccRequest().Core(nccc.Core());
                        nccc.Header().Decode(header);
                        payload.Val = nccc;
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
                var nccc = (NcccIso8583)payload.Val;
                var header = nccc.Header().Encode();
                //random select localEndPoint
                var channelNumber = ColaCommUtility.GetSecurityNumber(Channels.Count);
                var channel = Channels.ToArray()[channelNumber];
                var localEndPoint = channel.LocalEndPoint().ToString();
                var localPortNumber = localEndPoint.Substring(localEndPoint.IndexOf(":", StringComparison.Ordinal) + 1);
                //add this for correct ISO Header reset default
                nccc = new NcccRequest().Core(nccc.Core());
                nccc.Header().Decode(header);
                payload.Val = nccc;
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
                foreach (var endPoint in NcccEndPoints)
                {
                    var localEndPoint = new IPEndPoint(IPAddress.Parse(endPoint.LocalIp), endPoint.LocalPort);
                    //var channel = Channels.Where(x => (x.LocalEndPoint().ToString().Equals(localEndPoint.ToString()))).ToList();
                    var channel = copyChannels.Where(x => (x.LocalEndPoint().ToString().Equals(localEndPoint.ToString()))).ToList();

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

        public NcccEndPoint GetFreeNcccEndPoint()
        {
            try
            {
                Monitor.Enter(GetFreeNcccEndPointLocker);
                var ncccEndPointList = new List<NcccEndPoint>();
                foreach (var endPoint in NcccEndPoints)
                {
                    var localEndPoint = new IPEndPoint(IPAddress.Parse(endPoint.LocalIp), endPoint.LocalPort);
                    var channel = Channels.Where(x => (x.LocalEndPoint().ToString().Equals(localEndPoint.ToString()))).ToList();
                    if (channel.Count == 0)
                    {
                        ncccEndPointList.Add(endPoint);
                        //if   ((freeLocalEndPoint != null ) && 
                        //    !(freeLocalEndPoint.ToString().Equals(localEndPoint.ToString())))
                        //    continue;
                        //freeLocalEndPoint = localEndPoint;
                        //Monitor.Exit(GetFreeLocalEndPointLocker);
                        //return localEndPoint;
                    }
                }
                this.Logger.Info($"LocalEndPointCount[{ncccEndPointList.Count}]");
                ncccEndPointList.ForEach(endPoint => this.Logger.Info($"FreeNcccEndPoint Remote[{endPoint.RemoteIp}:{endPoint.RemotePort}], Local[{endPoint.LocalIp}:{endPoint.LocalPort}]"));
                if (ncccEndPointList.Count == 0)
                    return null;
                //random select localEndPoint
                var localEndPointNumber = ColaCommUtility.GetSecurityNumber(ncccEndPointList.Count);
                return ncccEndPointList[localEndPointNumber];
            }
            catch (Exception ex)
            {
                this.Logger.Warn($"GetFreeNcccEndPoint Exception:{ex.Message})");
                return null;
            }
            finally
            {
                Monitor.Exit(GetFreeNcccEndPointLocker);
            }
        }
        // Prefers the session's own (original) local endpoint, so a revived session keeps its configured port and
        // concurrent revives cannot pick the same one; falls back to any free configured endpoint if it is taken.
        public NcccEndPoint GetFreeNcccEndPoint(IPEndPoint preferredLocalEndPoint)
        {
            if (preferredLocalEndPoint != null)
            {
                var preferred = NcccEndPoints.FirstOrDefault(e => e.LocalPort == preferredLocalEndPoint.Port
                                                               && IPAddress.Parse(e.LocalIp).Equals(preferredLocalEndPoint.Address));
                var inUse = Channels.ToArray().Any(x => x.LocalEndPoint()?.ToString() == preferredLocalEndPoint.ToString());
                if (preferred != null && !inUse)
                {
                    return preferred;
                }
            }

            return GetFreeNcccEndPoint();
        }

        public string GetNcccChannelInfoStatus()
        {
            string returnString;
            try
            {
                var ncccChannelInfoArray = this.Channels.Select(c1 => c1.Carrier<NcccChannelInfoKey, ChannelSessionInfo>()).OrderBy(ep => ep.LocalEndPoint).ToArray();
                // Initialize array list.
                ArrayList ncccChannelInfoArrayList = new ArrayList();

                // Add the items to array list.
                ncccChannelInfoArrayList.AddRange(ncccChannelInfoArray);
                XmlDocument doc = new XmlDocument();
                XmlSerializer serializer = new XmlSerializer(typeof(ArrayList), new Type[] { typeof(ChannelSessionInfo) });
                using (MemoryStream stream = new System.IO.MemoryStream())
                {
                    try
                    {
                        serializer.Serialize(stream, ncccChannelInfoArrayList);
                        stream.Position = 0;
                        doc.Load(stream);
                        returnString = doc.InnerXml;
                    }
                    catch (Exception ex)
                    {
                        this.Logger.Fatal($"GetNcccChannelInfoStatus Inner Exception:{ex.Message})");
                        returnString = string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                this.Logger.Fatal($"GetNcccChannelInfoStatus Outer Exception:{ex.Message})");
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
        protected override bool DispatchTask(NcccIso8583 item)
        {
            var itemRelationId = string.Format(SystemConstantData.CommonData.GatewayQRCodeIDVerifyRelationIdStringFormat,
                                               $"{item.Core().Field(FIELD.F_11) ?? $"{0:D6}"}",
                                               $"{item.Core().Field(FIELD.F_37) ?? $"{0:D12}"}",
                                               $"{item.Core().Field(FIELD.F_41) ?? $"{0:D16}"}"
                                               );
            var relationId = item.Extend<RelationIdKey, string>();
            var channel = this.RemoveRelation(relationId);
            this.Logger.Info($"1. CyberGWNCCCQRCode DispatchTask Get channel: {channel?.LocalEndPoint()} => {channel?.RemoteEndPoint()}, MTI[{item.Mti()}], Channel RelationId[{relationId}], Item RelationId[{itemRelationId}]");
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
                        this.Logger.Info($"2. CyberGWNCCCQRCode DispatchTask Get channel: {channel?.LocalEndPoint()} => {channel?.RemoteEndPoint()}, MTI[{item.Mti()}], Channel RelationId[{relationId}], Item RelationId[{itemRelationId}]");
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
            var targetIp = CommonUtils.GetIPAddressStringByHostName(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.NcccServiceConfigSectionName, "nccc-service-target-ip").Trim(), AddressFamily.InterNetwork);
            var targetPorts = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.NcccServiceConfigSectionName, "nccc-service-target-ports").Split(',');
            var localIp = CommonUtils.GetIPAddressStringByHostName(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.NcccServiceConfigSectionName, "nccc-service-local-ip").Trim(), AddressFamily.InterNetwork);
            var localPorts = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.NcccServiceConfigSectionName, "nccc-service-local-ports").Split(',');
            var sessions = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.NcccServiceConfigSectionName, "nccc-service-sessions").Trim());
            // Wait before reconnecting a session the external host dropped; without the key it stays 10 s.
            var reconnectIntervalMilliseconds = Convert.ToInt32(configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.NcccServiceConfigSectionName, "nccc-service-reconnect-interval-ms").Default("10000"));
            MaintenanceLocalPorts = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.NcccServiceConfigSectionName, "nccc-service-maintenance-local-ports").Split(',').ToList();

            if (targetPorts.Length == sessions && localPorts.Length == sessions)
            {
            }
            else
            {
                throw new ArgumentException("nccc configuration has error, please fix App.config");
            }
            NcccEndPoints = new NcccEndPoint[sessions];
            for (var i = 0; i < sessions; i++)
            {
                var ncccEndPoint = new NcccEndPoint(targetIp,
                                                    Convert.ToInt32(targetPorts[i]),
                                                    localIp,
                                                    Convert.ToInt32(localPorts[i]));
                this.NcccEndPoints[i] = ncccEndPoint;
            }
            //var bootstrap = new Bootstrap();
            //bootstrap.ChannelType(typeof(TcpSocketChannel));
            for (var i = 0; i < sessions; i++)
            {
                var remoteEndPoint = new IPEndPoint(IPAddress.Parse(NcccEndPoints[i].RemoteIp), NcccEndPoints[i].RemotePort);
                var localEndPoint = new IPEndPoint(IPAddress.Parse(NcccEndPoints[i].LocalIp), NcccEndPoints[i].LocalPort);
                var ncccEndPoint = NcccEndPoints[i];
                ThreadPool.QueueUserWorkItem(
                    state =>
                    {
                        try
                        {
                            var bootstrap = new Bootstrap();
                            bootstrap.ChannelType(typeof(TcpSocketChannel));
                            bootstrap.Handler(new NcccQRCodeChannelInitializer(this, bootstrap, remoteEndPoint, localEndPoint, reconnectIntervalMilliseconds))
                                             .SetChannelOption(ChannelOptionLevel.Tcp, ChannelOptionName.NoDelay, true)
                                             .SetChannelOption(ChannelOptionLevel.Socket, ChannelOptionName.ReuseAddress, true);
                            var channel = bootstrap.Connect(remoteEndPoint, localEndPoint).Sync().Channel();
                            channel.Carrier<RemoteEndPointKey>(remoteEndPoint);
                            channel.Carrier<LocalEndPointKey>(localEndPoint);
                            channel.Carrier<ReadTransactionTimeKey>(DateTime.Now);
                            channel.Carrier<NcccChannelInfoKey>(new ChannelSessionInfo(localEndPoint.ToString(), remoteEndPoint.ToString()));
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
