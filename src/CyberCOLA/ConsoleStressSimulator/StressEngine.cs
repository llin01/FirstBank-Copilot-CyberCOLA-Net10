using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.TM.FISC;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using Com.Cybersoft4u.TM.MasterCard;
using Com.Cybersoft4u.TM.VISA;
using Com.Cybersoft4u.Utils;
using Com.Cybersoft4u.Yetty.Channel;
using Com.Cybersoft4u.Yetty.Channel.TCP;
using Com.Cybersoft4u.Yetty.Context;
using Com.Cybersoft4u.Yetty.Handler;
using Com.Cybersoft4u.Yetty.Utils;

namespace Com.Cybersoft4u.Simulator.Console.ConsoleStressSimulator
{
    public class StressEngine
    {
        private static readonly Random StaticRnd = new Random();

        // Default Fallback Track2 Pools
        private static readonly string[] DefaultVisaPool = new string[]
        {
            "4678925100000604=280620111234123",
            "4693702700000503=290120111234123",
            "4693702700002129=290120111234123"
        };

        private static readonly string[] DefaultMasterCardPool = new string[]
        {
            "5588274700003668=290120111234123",
            "5588274700006257=290120111234123",
            "5588274700008709=290120111234123"
        };

        private static readonly string[] DefaultJcbPool = new string[]
        {
            "3565888200004637=290120111234123"
        };

        private static CardPools _loadedCardPools = new CardPools();

        private class ServerSocketHandler : ChannelInboundHandler
        {
        }

        private class ClientSessionHandler : ChannelInboundHandler
        {
            private readonly Action _onResponseReceived;
            private readonly Action<ChannelHandlerContext> _onClientConnected;
            private readonly Action<ChannelHandlerContext> _onClientDisconnected;
            private readonly ConcurrentDictionary<ChannelHandlerContext, byte[]> _pendingBytes = new ConcurrentDictionary<ChannelHandlerContext, byte[]>();

            public ClientSessionHandler(
                Action onResponseReceived,
                Action<ChannelHandlerContext> onClientConnected,
                Action<ChannelHandlerContext> onClientDisconnected)
            {
                _onResponseReceived = onResponseReceived;
                _onClientConnected = onClientConnected;
                _onClientDisconnected = onClientDisconnected;
            }

            public override void ChannelActived(ChannelHandlerContext ctx)
            {
                _onClientConnected?.Invoke(ctx);
                base.ChannelActived(ctx);
            }

            public override void ChannelInActived(ChannelHandlerContext ctx)
            {
                _pendingBytes.TryRemove(ctx, out _);
                _onClientDisconnected?.Invoke(ctx);
                base.ChannelInActived(ctx);
            }

            public override void ChannelRead(ChannelHandlerContext ctx, object msg)
            {
                // TCP may coalesce several responses into one read (or split one), so count complete
                // 2-byte length-prefixed frames instead of reads. This handler is shared by all
                // connections, hence the per-channel remainder buffer.
                if (msg is ByteBuf buf)
                {
                    byte[] data = _pendingBytes.TryRemove(ctx, out byte[]? pending)
                        ? Concat(pending, buf.Val())
                        : buf.Val();

                    int offset = 0;
                    while (data.Length - offset >= 2)
                    {
                        int frameLength = (data[offset] << 8) | data[offset + 1];
                        if (data.Length - offset - 2 < frameLength)
                        {
                            break;
                        }

                        offset += 2 + frameLength;
                        _onResponseReceived?.Invoke();
                    }

                    if (offset < data.Length)
                    {
                        _pendingBytes[ctx] = data[offset..];
                    }
                }
                else
                {
                    _onResponseReceived?.Invoke();
                }

                base.ChannelRead(ctx, msg);
            }

            private static byte[] Concat(byte[] first, byte[] second)
            {
                var result = new byte[first.Length + second.Length];
                Buffer.BlockCopy(first, 0, result, 0, first.Length);
                Buffer.BlockCopy(second, 0, result, first.Length, second.Length);
                return result;
            }
        }

        private class ServerChildInitializer : ChannelInitializer
        {
            private readonly ClientSessionHandler _handler;

            public ServerChildInitializer(ClientSessionHandler handler)
            {
                _handler = handler;
            }

            public override void InitChannel(IChannel channel)
            {
                channel.Pipe().AddLast(_handler, "client_session_handler");
            }
        }

        public class TestResultMetrics
        {
            public long TotalSent;
            public long TotalSuccess;
            public long TotalFailed;
            public double ElapsedSeconds;
            public double Tps => ElapsedSeconds > 0 ? TotalSuccess / ElapsedSeconds : 0;
        }

        public static async Task<TestResultMetrics> RunServerStressTestAsync(
            ConcurrentQueue<byte[]> masterTemplateQueue,
            TransactionType tranType,
            int listenPort,
            int sendIntervalMs,
            bool replaceCardNumber,
            CancellationToken token)
        {
            var metrics = new TestResultMetrics();
            int itemsPerClient = masterTemplateQueue.Count;

            if (itemsPerClient == 0)
            {
                System.Console.WriteLine("Warning: Master Test Queue is empty! Please generate test data first.");
                return metrics;
            }

            // Reference WpfStressSimulator CardXmlFilePath.xml loading
            _loadedCardPools = TestDataGenerator.LoadCardPoolsFromXml(tranType);
            int totalLoadedCards = _loadedCardPools.VisaList.Count + _loadedCardPools.MasterCardList.Count + _loadedCardPools.JcbList.Count;

            System.Console.WriteLine($"\n[Server Engine] Starting Server Listener on Port: {listenPort}...");
            System.Console.WriteLine($"[Server Engine] Mode: Per-Client Dedicated Queue | Replace Card Number: {replaceCardNumber} ({itemsPerClient:N0} items per client)");
            if (replaceCardNumber)
            {
                System.Console.WriteLine($"[Server Engine] Loaded Card Pool: {totalLoadedCards:N0} cards from XML (VISA: {_loadedCardPools.VisaList.Count}, MC: {_loadedCardPools.MasterCardList.Count}, JCB: {_loadedCardPools.JcbList.Count})");
            }

            var activeClients = new ConcurrentDictionary<ChannelHandlerContext, Task>();
            var clientDedicatedQueues = new ConcurrentDictionary<string, ConcurrentQueue<byte[]>>();
            var sw = Stopwatch.StartNew();

            int seqCounter = 0;

            var sessionHandler = new ClientSessionHandler(
                onResponseReceived: () => Interlocked.Increment(ref metrics.TotalSuccess),
                onClientConnected: (ctx) =>
                {
                    string clientAddress = ctx.Channel?.RemoteEndPoint()?.ToString() ?? Guid.NewGuid().ToString();
                    
                    var dedicatedQueue = TestDataGenerator.CloneQueue(masterTemplateQueue);
                    clientDedicatedQueues[clientAddress] = dedicatedQueue;

                    System.Console.WriteLine($"\n[Server Engine] Client connected: {clientAddress} -> Dedicated Queue ({dedicatedQueue.Count:N0} items)");

                    var clientTask = Task.Run(async () =>
                    {
                        while (!token.IsCancellationRequested && dedicatedQueue.TryDequeue(out byte[]? rawTemplate))
                        {
                            try
                            {
                                int currentSeq = Interlocked.Increment(ref seqCounter);

                                // Parse -> Dynamic Field Update (and Card Number Replacement if enabled) -> Re-encode -> Pack Header
                                byte[] finalSendBytes = ProcessAndPackMessage(rawTemplate, tranType, currentSeq, replaceCardNumber);

                                ctx.Channel?.WriteAndFlush(ByteBuf.From(finalSendBytes, 0, finalSendBytes.Length));
                                Interlocked.Increment(ref metrics.TotalSent);
                            }
                            catch (Exception ex)
                            {
                                System.Console.WriteLine($"\n[Server Engine] Warning sending message to {clientAddress}: {ex.Message}");
                                Interlocked.Increment(ref metrics.TotalFailed);
                                break;
                            }

                            if (sendIntervalMs > 0)
                            {
                                await Task.Delay(sendIntervalMs, token);
                            }
                        }

                        System.Console.WriteLine($"\n[Server Engine] Client {clientAddress} finished processing dedicated queue ({dedicatedQueue.Count:N0} items remaining).");
                    }, token);

                    activeClients.TryAdd(ctx, clientTask);
                },
                onClientDisconnected: (ctx) =>
                {
                    string clientAddress = ctx.Channel?.RemoteEndPoint()?.ToString() ?? "Unknown";
                    System.Console.WriteLine($"\n[Server Engine] Client disconnected: {clientAddress}");
                    activeClients.TryRemove(ctx, out _);
                    clientDedicatedQueues.TryRemove(clientAddress, out _);
                }
            );

            // Bind Server Listener
            var serverBootstrap = new ServerBootstrap()
                .ChannelType(typeof(TcpServerSocketChannel))
                .Handler(new ServerSocketHandler())
                .ChildHandler(new ServerChildInitializer(sessionHandler));

            var bindFuture = serverBootstrap.Bind(new IPEndPoint(IPAddress.Any, listenPort)).Sync();
            IChannel serverChannel = bindFuture.Channel();

            // Progress Display Task
            var displayTask = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(500, token);
                    double currentTps = sw.Elapsed.TotalSeconds > 0 ? metrics.TotalSent / sw.Elapsed.TotalSeconds : 0;
                    System.Console.Write($"\r[Progress] Connected Clients: {activeClients.Count} | Total Sent: {metrics.TotalSent:N0} | Resp Success: {metrics.TotalSuccess:N0} | Failed: {metrics.TotalFailed:N0} | Speed: {currentTps:F1} msg/sec  ");
                }
            }, token);

            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(500, token);
                }
            }
            catch (OperationCanceledException) { }

            sw.Stop();
            metrics.ElapsedSeconds = sw.Elapsed.TotalSeconds;

            serverChannel?.Close();
            System.Console.WriteLine($"\n[Server Engine] Server stress test service stopped after {metrics.ElapsedSeconds:F2} seconds.");
            foreach (var failure in _fieldFailureCounts)
            {
                System.Console.WriteLine($"[Server Engine] Field update failures: {failure.Key} x{failure.Value}");
            }
            return metrics;
        }

        private static byte[] ProcessAndPackMessage(byte[] rawTemplateBytes, TransactionType tranType, int sequenceNumber, bool replaceCardNumber)
        {
            try
            {
                byte[] isoPayloadBytes = Strip2ByteHeader(rawTemplateBytes);
                byte[] encodedPayload;

                switch (tranType)
                {
                    case TransactionType.NCCC:
                        var nccc = new NcccIso8583(isoPayloadBytes);
                        UpdateCommonIsoFieldsAndCardNumber(nccc, sequenceNumber, tranType, replaceCardNumber);
                        encodedPayload = nccc.Encode().Val();
                        break;

                    case TransactionType.FISC:
                        var fisc = new FISC(isoPayloadBytes);
                        UpdateCommonIsoFieldsAndCardNumber(fisc, sequenceNumber, tranType, replaceCardNumber);
                        encodedPayload = fisc.Encode().Val();
                        break;

                    case TransactionType.VISA:
                        var visa = new VISA(isoPayloadBytes);
                        UpdateCommonIsoFieldsAndCardNumber(visa, sequenceNumber, tranType, replaceCardNumber);
                        encodedPayload = visa.Encode().Val();
                        break;

                    case TransactionType.MasterCard:
                        var masterCard = new MasterCard(isoPayloadBytes);
                        UpdateCommonIsoFieldsAndCardNumber(masterCard, sequenceNumber, tranType, replaceCardNumber);
                        encodedPayload = masterCard.Encode().Val();
                        break;

                    default:
                        encodedPayload = isoPayloadBytes;
                        break;
                }

                return ColaCommUtility.AddBinaryLength2BytesArray(encodedPayload);
            }
            catch (Exception ex)
            {
                // The raw template is still sent, but say so: unchanged STAN/RRN make the GW see duplicate
                // RelationIds, which otherwise looks like a GW defect. Only the first few are printed.
                if (Interlocked.Increment(ref _packFailureCount) <= MaxPackFailureWarnings)
                {
                    System.Console.WriteLine($"\n[PackMessage Warning] {tranType} template sent unmodified (fields not updated): {ex.GetType().Name}: {CardDataMasker.Mask(ex.Message)}");
                }
                return rawTemplateBytes;
            }
        }

        private const int MaxPackFailureWarnings = 5;
        private static int _packFailureCount;

        // Field updates used to swallow every exception, so a template whose STAN/RRN/card was never
        // changed looked like a GW or Auth defect. The first failure per network and field is printed,
        // the rest are only counted and summarised when the run stops.
        private static readonly ConcurrentDictionary<string, int> _fieldFailureCounts = new ConcurrentDictionary<string, int>();

        private static void ReportFieldFailure(TransactionType tranType, string field, Exception ex)
        {
            string key = $"{tranType} {field}";
            if (_fieldFailureCounts.AddOrUpdate(key, 1, (_, count) => count + 1) == 1)
            {
                System.Console.WriteLine($"\n[UpdateIsoFields Warning] {key} not updated: {ex.GetType().Name}: {CardDataMasker.Mask(ex.Message)} (further failures are counted only)");
            }
        }

        private static void TryUpdateField(dynamic core, FIELD field, string value, TransactionType tranType)
        {
            try
            {
                if (core.Field(field) != null) core.Field(field, value);
            }
            catch (Exception ex)
            {
                ReportFieldFailure(tranType, field.ToString(), ex);
            }
        }

        private static byte[] Strip2ByteHeader(byte[] rawBytes)
        {
            if (rawBytes.Length > 2)
            {
                int headerLength = (rawBytes[0] << 8) | rawBytes[1];
                if (headerLength == rawBytes.Length - 2)
                {
                    byte[] payload = new byte[headerLength];
                    Buffer.BlockCopy(rawBytes, 2, payload, 0, headerLength);
                    return payload;
                }
            }
            return rawBytes;
        }

        private static void UpdateCommonIsoFieldsAndCardNumber(object isoObj, int sequenceNumber, TransactionType tranType, bool replaceCardNumber)
        {
            try
            {
                dynamic iso = isoObj;
                var core = iso.Core();

                // Dynamic Card Number replacement (Field 2 & Field 35) ONLY if replaceCardNumber is true
                if (replaceCardNumber && _loadedCardPools != null)
                {
                    string[] visaArray = _loadedCardPools.VisaList.Count > 0 ? _loadedCardPools.VisaList.ToArray() : DefaultVisaPool;
                    string[] masterArray = _loadedCardPools.MasterCardList.Count > 0 ? _loadedCardPools.MasterCardList.ToArray() : DefaultMasterCardPool;
                    string[] jcbArray = _loadedCardPools.JcbList.Count > 0 ? _loadedCardPools.JcbList.ToArray() : DefaultJcbPool;

                    // Keep the template's card brand: the Auth classifies some messages by brand (e.g. a Visa-only
                    // 0620 token event), so swapping brands turned valid templates into rejected transactions.
                    string originalPan = string.Empty;
                    // Field values are not strings, so read them via ToString(); a cast throws and was swallowed.
                    try { object? f2 = core.Field(FIELD.F_2); object? f35 = core.Field(FIELD.F_35); originalPan = (f2 ?? f35)?.ToString() ?? string.Empty; }
                    catch (Exception ex) { ReportFieldFailure(tranType, "F_2/F_35 brand read", ex); }
                    string[]? brandPool = originalPan.StartsWith("4") ? visaArray
                        : originalPan.StartsWith("35") ? jcbArray
                        : originalPan.StartsWith("5") || originalPan.StartsWith("2") ? masterArray
                        : null;

                    string[] targetPool = tranType switch
                    {
                        TransactionType.MasterCard => masterArray,
                        TransactionType.FISC => visaArray,
                        _ => brandPool ?? (sequenceNumber % 3 == 0 ? jcbArray : (sequenceNumber % 2 == 0 ? masterArray : visaArray))
                    };

                    if (targetPool.Length > 0)
                    {
                        string track2 = targetPool[StaticRnd.Next(0, targetPool.Length)];
                        string pan = track2.Split('=', 'D', '~', '^')[0];

                        TryUpdateField(core, FIELD.F_2, pan, tranType);
                        TryUpdateField(core, FIELD.F_35, track2, tranType);
                        // The Auth checks the template's own expiry (F14) against the new card, which failed
                        // every FISC template that carries F14 (RC 05/5C, check code 032).
                        int sep = track2.IndexOfAny(new[] { '=', 'D' });
                        if (sep > 0 && track2.Length >= sep + 5)
                        {
                            TryUpdateField(core, FIELD.F_14, track2.Substring(sep + 1, 4), tranType);
                        }
                    }
                }

                // Dynamic Update Field 7, Field 11, Field 12, Field 13, Field 37
                var now = DateTime.Now;
                TryUpdateField(core, FIELD.F_7, now.ToString("MMddHHmmss"), tranType);
                TryUpdateField(core, FIELD.F_11, (sequenceNumber % 1000000).ToString("D6"), tranType);
                TryUpdateField(core, FIELD.F_12, now.ToString("HHmmss"), tranType);
                TryUpdateField(core, FIELD.F_13, now.ToString("MMdd"), tranType);
                TryUpdateField(core, FIELD.F_37, now.ToString("yyMMdd") + (sequenceNumber % 100000).ToString("D6"), tranType);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[UpdateIsoFields Warning] {CardDataMasker.Mask(ex.Message)}");
            }
        }
    }
}
