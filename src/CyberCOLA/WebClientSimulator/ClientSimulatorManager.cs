using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.ServiceProvider.ClientAuth;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using Com.Cybersoft4u.Utils;
using NLog;
using Logger = NLog.Logger;

namespace Com.Cybersoft4u.Simulator.Web.WebClientSimulator
{
    public class ClientSimulatorManager : IDisposable
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private CyberAuthNCCC? _cyberAuthNCCC;
        private UdpClient? _udpClient;
        private CancellationTokenSource? _udpCts;
        private readonly ConfigurationLoaderRoot _configurationLoaderRoot;

        private string _initialDirectory = "./Data/NCCC";
        private int _udpListenerPort = 9919;
        private bool _udpAutoStartFlag = true;
        private int _ncccListenerPort = 19335;
        private int _ncccLocalPort = 3000;
        private int _ncccSessionCount = 2;

        private static readonly Random StaticTraceRnd = new Random();
        private static readonly Random StaticAmtRnd = new Random();
        private static readonly Random StaticRefRnd = new Random();

        public bool IsNcccRunning => _cyberAuthNCCC != null;
        public bool IsUdpRunning => _udpClient != null;

        public string InitialDirectory => _initialDirectory;
        public int UdpListenerPort => _udpListenerPort;
        public int NcccListenerPort => _ncccListenerPort;
        public int NcccLocalPort => _ncccLocalPort;
        public int NcccSessionCount => _ncccSessionCount;

        public ClientSimulatorManager()
        {
            _configurationLoaderRoot = new ConfigurationLoaderRoot();
            LoadParameters();

            if (_udpAutoStartFlag)
            {
                StartUdpService();
            }
        }

        public void LoadParameters()
        {
            try
            {
                _initialDirectory = _configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "InitialDirectory")?.Trim() ?? "./Data/NCCC";

                var udpPortStr = _configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "UDPListenerPort");
                if (!string.IsNullOrEmpty(udpPortStr)) _udpListenerPort = Convert.ToInt32(udpPortStr.Trim());

                var udpAutoStr = _configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "UDPAutoStartFlag");
                if (!string.IsNullOrEmpty(udpAutoStr)) _udpAutoStartFlag = Convert.ToBoolean(udpAutoStr.Trim());

                var ncccPortStr = _configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "NCCCListenerPort");
                if (!string.IsNullOrEmpty(ncccPortStr)) _ncccListenerPort = Convert.ToInt32(ncccPortStr.Trim());

                var ncccLocalPortStr = _configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "NCCCLocalPort");
                if (!string.IsNullOrEmpty(ncccLocalPortStr)) _ncccLocalPort = Convert.ToInt32(ncccLocalPortStr.Trim());

                var ncccSessionStr = _configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "NCCCSessionCount");
                if (!string.IsNullOrEmpty(ncccSessionStr)) _ncccSessionCount = Convert.ToInt32(ncccSessionStr.Trim());

                Log.Info($"Parameters loaded. InitialDirectory: {_initialDirectory}, NCCCListenerPort: {_ncccListenerPort}, NCCCLocalPort: {_ncccLocalPort}, NCCCSessionCount: {_ncccSessionCount}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load parameters: " + ex.Message);
            }
        }

        public void StartNcccService(int? port = null, int? localPort = null, int? sessions = null)
        {
            if (IsNcccRunning) return;

            if (port.HasValue) _ncccListenerPort = port.Value;
            if (localPort.HasValue) _ncccLocalPort = localPort.Value;
            if (sessions.HasValue) _ncccSessionCount = sessions.Value;

            try
            {
                _cyberAuthNCCC = new CyberAuthNCCC();
                Task.Run(() => _cyberAuthNCCC.Start());
                Log.Info($"NCCC CyberAuth service started at: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Failed to start NCCC service: " + ex.Message);
                _cyberAuthNCCC = null;
                throw;
            }
        }

        public void StopNcccService()
        {
            if (!IsNcccRunning) return;

            try
            {
                _cyberAuthNCCC?.Stop();
                _cyberAuthNCCC = null;
                Log.Info($"NCCC CyberAuth service stopped at: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffff}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to stop NCCC service: " + ex.Message);
                throw;
            }
        }

        public void StartUdpService()
        {
            if (IsUdpRunning) return;

            try
            {
                _udpClient = new UdpClient(_udpListenerPort);
                _udpCts = new CancellationTokenSource();
                var token = _udpCts.Token;

                Task.Run(async () =>
                {
                    var groupEP = new IPEndPoint(IPAddress.Any, _udpListenerPort);
                    Log.Info($"UDP Log Server started on port {_udpListenerPort}");

                    while (!token.IsCancellationRequested)
                    {
                        try
                        {
                            if (_udpClient.Available > 0)
                            {
                                var result = await _udpClient.ReceiveAsync(token);
                                var msg = Encoding.UTF8.GetString(result.Buffer);
                                ClientLogBroadcaster.Log(msg);
                            }
                            else
                            {
                                await Task.Delay(100, token);
                            }
                        }
                        catch (ObjectDisposedException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"UDP Receive Error: {ex.Message}");
                        }
                    }
                }, token);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to start UDP service on port {_udpListenerPort}: {ex.Message}");
                _udpClient = null;
            }
        }

        public void StopUdpService()
        {
            if (!IsUdpRunning) return;

            try
            {
                _udpCts?.Cancel();
                _udpClient?.Close();
                _udpClient = null;
                _udpCts = null;
                Log.Info("UDP Log Server stopped.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to stop UDP service: " + ex.Message);
            }
        }

        public List<string> GetTemplateFiles()
        {
            var files = new List<string>();
            try
            {
                var dir = Path.Combine(AppContext.BaseDirectory, _initialDirectory);
                if (Directory.Exists(dir))
                {
                    var paths = Directory.GetFiles(dir, "*.dat", SearchOption.AllDirectories);
                    foreach (var path in paths)
                    {
                        files.Add(Path.GetRelativePath(dir, path));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to get template files: " + ex.Message);
            }
            return files;
        }

        public async Task SendTemplateFiles(List<string> fileNames, int intervalMilliseconds)
        {
            if (_cyberAuthNCCC == null || !_cyberAuthNCCC.IsHaveChannels())
            {
                throw new InvalidOperationException("NCCC has no connection channels. Please check connection.");
            }

            try
            {
                var dir = Path.Combine(AppContext.BaseDirectory, _initialDirectory);
                foreach (var fileName in fileNames)
                {
                    var filePath = Path.Combine(dir, fileName);
                    if (!File.Exists(filePath))
                    {
                        Log.Warn($"File not found: {filePath}");
                        continue;
                    }

                    var dataBytes = await File.ReadAllBytesAsync(filePath);
                    var requestMessage = CreateRequestMessage(dataBytes);

                    Log.Info($"Sending request: Header=[{((NcccIso8583)requestMessage).Header().Encode()}], MTI=[{((NcccIso8583)requestMessage).Mti()}]");

                    await Task.Run(() => _cyberAuthNCCC.AllChannelSendMessageToGateway(requestMessage));

                    if (intervalMilliseconds > 0)
                    {
                        await Task.Delay(intervalMilliseconds);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to send template files: " + ex.Message);
                throw;
            }
        }

        private NcccIso8583 CreateRequestMessage(byte[] originalMessage)
        {
            var requestMessage = new NcccIso8583(originalMessage);
            var fieldName = FIELD.F_4;
            if (requestMessage.Core().Field(fieldName) != null)
            {
                int amtMount = StaticAmtRnd.Next(1, 100);
                requestMessage.Core().Field(fieldName, $"{amtMount:000000000000}");
            }
            fieldName = FIELD.F_7;
            requestMessage.Core().Field(fieldName, $"{DateTime.Now.ToUniversalTime():MMddHHmmss}");

            fieldName = FIELD.F_11;
            if (requestMessage.Core().Field(fieldName) != null)
            {
                int traceNumber = StaticTraceRnd.Next(1, 999999);
                requestMessage.Core().Field(fieldName, $"{traceNumber:000000}");
            }
            fieldName = FIELD.F_12;
            if (requestMessage.Core().Field(fieldName) != null)
            {
                requestMessage.Core().Field(fieldName, $"{DateTime.Now:HHmmss}");
            }
            fieldName = FIELD.F_13;
            if (requestMessage.Core().Field(fieldName) != null)
            {
                requestMessage.Core().Field(fieldName, $"{DateTime.Now:MMdd}");
            }
            fieldName = FIELD.F_15;
            if (requestMessage.Core().Field(fieldName) != null)
            {
                requestMessage.Core().Field(fieldName, $"{DateTime.Now:MMdd}");
            }
            fieldName = FIELD.F_17;
            if (requestMessage.Core().Field(fieldName) != null)
            {
                requestMessage.Core().Field(fieldName, $"{DateTime.Now:MMdd}");
            }
            fieldName = FIELD.F_37;
            if (requestMessage.Core().Field(fieldName) != null)
            {
                int refNumber = StaticRefRnd.Next(1, 999999);
                requestMessage.Core().Field(fieldName, $"{DateTime.Now:yyMMdd}{refNumber:000000}");
            }
            return requestMessage;
        }



        public void Dispose()
        {
            StopNcccService();
            StopUdpService();
            GC.SuppressFinalize(this);
        }
    }
}
