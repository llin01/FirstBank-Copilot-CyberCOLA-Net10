using Com.Cybersoft4u.CommonLib.EAI;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using Com.Cybersoft4u.Utils;
using Com.Cybersoft4u.Utils.NLog;
using System.Text;

namespace Com.Cybersoft4u.Test.Web.WebEAITCPClient
{
    public class EAITCPClientManager
    {
        private static readonly Com.Cybersoft4u.Utils.Logger Logger;

        static EAITCPClientManager()
        {
            LoggerAdapter.ReBuild(new NLoggerProvider(), "NLog.config");
            Logger = LoggerAdapter.GetLogger("LoggerHandler[WebEAITCPClient]");
        }

        private string? _initialFolderPath;
        private string? _serverName;
        private int _serverListenerPort = 9999;
        private int _connectionTimeout = 2000;
        private int _sendReceiveTimeout = 5000;
        private bool _isInitialized = false;

        public event Action<string, string>? OnSystemLog; // message, color (e.g., "red", "green", "white")

        public string? InitialFolderPath => _initialFolderPath;
        public string? DefaultServerName => _serverName;
        public int DefaultPort => _serverListenerPort;
        public int DefaultConnectionTimeout => _connectionTimeout;
        public int DefaultSendReceiveTimeout => _sendReceiveTimeout;

        public void Init()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                LogSystem("EAI System Manager Initializing...", "cyan");

                var configurationLoaderRoot = new ConfigurationLoaderRoot();
                _initialFolderPath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "initial-folder-path")?.Trim() ?? string.Empty;
                _serverName = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "remote-server-hostname")?.Trim() ?? "localhost";

                var portStr = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "remote-service-target-port")?.Trim();
                if (!string.IsNullOrEmpty(portStr))
                {
                    _serverListenerPort = Convert.ToInt32(portStr);
                }

                var connTimeoutStr = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "connection-timeout-millisecond")?.Trim();
                if (!string.IsNullOrEmpty(connTimeoutStr))
                {
                    _connectionTimeout = Convert.ToInt32(connTimeoutStr);
                }

                var sndRcvTimeoutStr = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "send-receive-timeout-millisecond")?.Trim();
                if (!string.IsNullOrEmpty(sndRcvTimeoutStr))
                {
                    _sendReceiveTimeout = Convert.ToInt32(sndRcvTimeoutStr);
                }

                LogSystem($"EAI System parameters loaded successfully. Initial Path: {_initialFolderPath}", "green");
            }
            catch (Exception ex)
            {
                Logger.Fatal($"Failed to initialize EAITCPClientManager: {ex.Message}");
                LogSystem($"Initialization Error: {ex.Message}", "red");
            }
        }

        public async Task<byte[]?> ProcessTCPTaskAsync(byte[] sendBytes, string serverName, int port, int connTimeout, int sndRcvTimeout)
        {
            return await Task.Run(() =>
            {
                try
                {
                    LogSystem($"Starting EAI Socket request to {serverName}:{port}... (ConnTimeout: {connTimeout}ms, RcvTimeout: {sndRcvTimeout}ms)", "cyan");

                    using SocketClient sa = new SocketClient(serverName, port, connTimeout, sndRcvTimeout);
                    sa.Connect();

                    var receiveBytes = sa.SendReceive(sendBytes);
                    if (receiveBytes == null)
                    {
                        LogSystem("ProcessTask: No message received from server.", "red");
                        return null;
                    }

                    Logger.Info($"Receive Message {Encoding.Default.GetString(receiveBytes)} at : {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}");
                    sa.Disconnect();
                    LogSystem("ProcessTask completed successfully.", "green");

                    return receiveBytes;
                }
                catch (Exception ex)
                {
                    Logger.Error($"ProcessTask Exception: {ex.Message}");
                    LogSystem($"ProcessTask Error: {ex.Message}", "red");
                    return null;
                }
            });
        }

        public List<NcccParsedField> ParseNcccIso8583(FCB9GQ.FCB9GQ_REQ reqObject, out string? header, out string? bitmap, out string? mti, out List<TokenItem> tokens)
        {
            header = null;
            bitmap = null;
            mti = null;
            tokens = new List<TokenItem>();
            var fieldsList = new List<NcccParsedField>();

            try
            {
                var nccc = EAICommUtility.ConvertFCB9GQMessage2NcccIso8583(reqObject, FIELD.F_57);
                if (nccc == null)
                {
                    LogSystem("Convert NCCC ISO8583 error (returned null).", "red");
                    return fieldsList;
                }

                header = nccc.Header().Encode();
                bitmap = HexStringConverter.FromByteArray(nccc.Bitmap());
                mti = nccc.Mti();

                var tokenField = nccc.GetTokenFieldId();
                foreach (var pair in nccc.Core().OrderedFields())
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    string valStr = string.Empty;
                    string hexStr = string.Empty;

                    if (value is byte[] bytes)
                    {
                        hexStr = HexStringConverter.FromByteArray(bytes);
                        valStr = Encoding.Default.GetString(bytes);
                    }
                    else if (value != null)
                    {
                        valStr = value.ToString() ?? string.Empty;
                    }

                    fieldsList.Add(new NcccParsedField
                    {
                        FieldId = key,
                        FieldVal = valStr,
                        FieldHex = hexStr
                    });

                    if (key == tokenField)
                    {
                        var tokenGroup = (TokenGroup)nccc.Core().GetOthersValByKey(tokenField.ToString());
                        if (tokenGroup != null)
                        {
                            foreach (var token in tokenGroup.Tokens)
                            {
                                tokens.Add(new TokenItem
                                {
                                    Key = token.Key,
                                    Value = token.Value.Encode()
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogSystem($"NCCC parsing error: {ex.Message}", "red");
            }

            return fieldsList;
        }

        public string CalculateCardCheckDigit(string inputData)
        {
            try
            {
                var cardNumber = EAICommUtility.CreateCardNumberCheckDigit(inputData);
                LogSystem($"Input Data [{inputData}] -> Card Number Check Digit [{cardNumber}]", "green");
                return cardNumber;
            }
            catch (Exception ex)
            {
                LogSystem($"Check digit calculation error: {ex.Message}", "red");
                return string.Empty;
            }
        }

        private void LogSystem(string msg, string color)
        {
            OnSystemLog?.Invoke(msg, color);
        }
    }

    public class NcccParsedField
    {
        public FIELD FieldId { get; set; }
        public string FieldName => FieldId.ToString();
        public string FieldVal { get; set; } = string.Empty;
        public string FieldHex { get; set; } = string.Empty;
    }

    public class TokenItem
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
