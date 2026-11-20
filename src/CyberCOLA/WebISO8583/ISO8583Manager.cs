using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.FISC;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using Com.Cybersoft4u.TM.ISO8583.TWIN;
using NLog;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Com.Cybersoft4u.Test.Web.WebISO8583
{
    // --- Model for DataGrid Field ---
    public class ISOFieldInfo
    {
        public string BitNum { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string FieldDataType { get; set; } = string.Empty;
        public string Length { get; set; } = string.Empty;
        public string FieldDataFormat { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public string EntryData { get; set; } = string.Empty;
    }

    // --- XML Config Definition Model ---
    public class BICFieldConfig
    {
        public string BitNum { get; set; } = string.Empty;
        public string FieldName { get; set; } = string.Empty;
        public string FieldDataType { get; set; } = string.Empty;
        public string Length { get; set; } = string.Empty;
        public string FieldDataFormat { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    // --- ISO8583 Manager ---
    public class ISO8583Manager
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static readonly SystemParameterInfo SystemParameterInfo = SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo();
        private readonly IConfiguration _config;

        private BicFieldDefaultValue? _fiscFieldDefaults;
        private BicFieldDefaultValue? _ncccFieldDefaults;
        private BicFieldDefaultValue? _twinFieldDefaults;

        private XDocument? _fiscFieldXml;
        private XDocument? _ncccFieldXml;
        private XDocument? _twinFieldXml;

        private XDocument? _fiscMessageTypeXml;
        private XDocument? _ncccMessageTypeXml;
        private XDocument? _twinMessageTypeXml;

        private string[] _cardTrack2Array = Array.Empty<string>();
        private readonly Random _rnd = new();

        public ISO8583Manager(IConfiguration config)
        {
            _config = config;
            LoadConfigurations();
        }

        private void LoadConfigurations()
        {
            try
            {
                var loader = new ConfigurationLoaderRoot();

                // 1. Load XML Paths
                var fiscDefaultsPath = loader.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "fisc-bic-field-default-value-info-xml-file-path")?.Trim();
                var ncccDefaultsPath = loader.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "nccc-bic-field-default-value-info-xml-file-path")?.Trim();
                var twinDefaultsPath = loader.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "twin-bic-field-default-value-info-xml-file-path")?.Trim();

                var fiscFieldsPath = loader.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "FiscFieldTypeConfig")?.Trim();
                var ncccFieldsPath = loader.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "NcccFieldTypeConfig")?.Trim();
                var twinFieldsPath = loader.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "TwinFieldTypeConfig")?.Trim();

                var fiscMsgTypePath = loader.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "FiscMessageTypeConfig")?.Trim();
                var ncccMsgTypePath = loader.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "NcccMessageTypeConfig")?.Trim();
                var twinMsgTypePath = loader.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "TwinMessageTypeConfig")?.Trim();

                var cardPath = loader.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "CardXmlFilePath")?.Trim();

                // 2. Deserialize Default Values
                _fiscFieldDefaults = DeserializeDefaults(fiscDefaultsPath);
                _ncccFieldDefaults = DeserializeDefaults(ncccDefaultsPath);
                _twinFieldDefaults = DeserializeDefaults(twinDefaultsPath);

                // 3. Load XML Docs
                _fiscFieldXml = LoadXml(fiscFieldsPath);
                _ncccFieldXml = LoadXml(ncccFieldsPath);
                _twinFieldXml = LoadXml(twinFieldsPath);

                _fiscMessageTypeXml = LoadXml(fiscMsgTypePath);
                _ncccMessageTypeXml = LoadXml(ncccMsgTypePath);
                _twinMessageTypeXml = LoadXml(twinMsgTypePath);

                // 4. Load Card information
                if (!string.IsNullOrEmpty(cardPath) && File.Exists(cardPath))
                {
                    var doc = XDocument.Load(cardPath);
                    _cardTrack2Array = doc.Descendants("CardInformation")
                                          .Select(card => card.Element("Track2Data")?.Value)
                                          .Where(val => !string.IsNullOrEmpty(val))
                                          .ToArray()!;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to load configuration XML files");
            }
        }

        private BicFieldDefaultValue? DeserializeDefaults(string? path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            try
            {
                var serializer = new XmlSerializer(typeof(BicFieldDefaultValue));
                using var reader = XmlReader.Create(path);
                return (BicFieldDefaultValue)serializer.Deserialize(reader)!;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to deserialize BicFieldDefaultValue: {path}");
                return null;
            }
        }

        private XDocument? LoadXml(string? path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            try
            {
                return XDocument.Load(path);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to load XML document: {path}");
                return null;
            }
        }

        // --- Fetch available Message Types (MTIs) ---
        public List<string> GetAvailableMessageTypes(string protocol)
        {
            var list = new List<string>();
            var doc = protocol.ToUpper() switch
            {
                "FISC" => _fiscMessageTypeXml,
                "NCCC" => _ncccMessageTypeXml,
                "TWIN" => _twinMessageTypeXml,
                _ => null
            };

            var rootName = protocol.ToUpper() switch
            {
                "FISC" => "FiscMessageTypeInfo",
                "NCCC" => "NcccMessageTypeInfo",
                "TWIN" => "TwinMessageTypeInfo",
                _ => string.Empty
            };

            if (doc == null || string.IsNullOrEmpty(rootName)) return list;

            try
            {
                var elements = doc.Element(rootName)?.Elements("MessageType");
                if (elements != null)
                {
                    var mtiList = elements.Where(x => Convert.ToInt16(x.Element("RequestMessage")?.Value.Substring(2, 1)) % 2 == 0)
                                          .Select(x => x.Element("RequestMessage")?.Value)
                                          .Where(val => !string.IsNullOrEmpty(val));
                    list.AddRange(mtiList!);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"GetAvailableMessageTypes failed for protocol: {protocol}");
            }

            return list;
        }

        // --- Load fields list based on protocol and MTI ---
        public List<ISOFieldInfo> LoadProtocolFields(string protocol, string mti)
        {
            var list = new List<ISOFieldInfo>();

            var fieldDoc = protocol.ToUpper() switch
            {
                "FISC" => _fiscFieldXml,
                "NCCC" => _ncccFieldXml,
                "TWIN" => _twinFieldXml,
                _ => null
            };

            var msgTypeDoc = protocol.ToUpper() switch
            {
                "FISC" => _fiscMessageTypeXml,
                "NCCC" => _ncccMessageTypeXml,
                "TWIN" => _twinMessageTypeXml,
                _ => null
            };

            if (fieldDoc == null || msgTypeDoc == null || string.IsNullOrEmpty(mti)) return list;

            try
            {
                // Parse ArrayOfBicField
                var bicFieldElements = fieldDoc.Element("ArrayOfBicField")?.Elements("BicField");
                if (bicFieldElements == null) return list;

                var bicConfigs = bicFieldElements
                                  .Select(x => new BICFieldConfig
                                  {
                                      BitNum = x.Element("BitNum")?.Value ?? string.Empty,
                                      FieldName = x.Element("FieldName")?.Value ?? string.Empty,
                                      FieldDataType = x.Element("FieldDataType")?.Value ?? string.Empty,
                                      Length = x.Element("Length")?.Value ?? string.Empty,
                                      FieldDataFormat = x.Element("FieldDataFormat")?.Value ?? string.Empty,
                                      Description = x.Element("Description")?.Value ?? string.Empty
                                  })
                                  .OrderBy(x => Convert.ToInt32(x.BitNum))
                                  .ToList();

                // Get fields requested in MessageType XML
                var requestFields = msgTypeDoc.Descendants("MessageType")
                                              .Where(x => x.Element("RequestMessage")?.Value == mti)
                                              .Elements("RequestField")
                                              .Elements("Field")
                                              .Select(f => new
                                              {
                                                  Num = f.Attribute("Number")?.Value ?? string.Empty,
                                                  Cond = f.Attribute("DataCondition")?.Value ?? "O"
                                              })
                                              .ToList();

                foreach (var rf in requestFields)
                {
                    int bitNum = Convert.ToInt32(rf.Num.Trim());
                    if (bitNum == 1) continue; // Skip Bitmap field

                    var config = bicConfigs.FirstOrDefault(c => Convert.ToInt32(c.BitNum) == bitNum);
                    if (config == null) continue;

                    var defaultValue = GetDefaultValue(protocol, bitNum);

                    list.Add(new ISOFieldInfo
                    {
                        BitNum = $"{bitNum:000}",
                        FieldName = config.FieldName,
                        FieldDataType = config.FieldDataType,
                        Length = config.Length,
                        FieldDataFormat = config.FieldDataFormat,
                        Description = config.Description,
                        Condition = rf.Cond.Trim(),
                        EntryData = defaultValue
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"LoadProtocolFields failed for {protocol} - {mti}");
            }

            return list;
        }

        private string GetDefaultValue(string protocol, int bitNum)
        {
            var defaults = protocol.ToUpper() switch
            {
                "FISC" => _fiscFieldDefaults,
                "NCCC" => _ncccFieldDefaults,
                "TWIN" => _twinFieldDefaults,
                _ => null
            };

            string track2Data = _cardTrack2Array.Length > 0
                ? _cardTrack2Array[_rnd.Next(0, _cardTrack2Array.Length)]
                : "1234567890123456=99121200000000000";

            try
            {
                switch (bitNum)
                {
                    case 2:
                        return track2Data.Length >= 16 ? track2Data.Substring(0, 16) : track2Data;
                    case 4:
                        int amt = _rnd.Next(1, 100);
                        return $"{amt:000000000000}";
                    case 7:
                        return $"{DateTime.Now.ToUniversalTime():MMddHHmmss}";
                    case 11:
                        int trace = _rnd.Next(1, 999999);
                        return $"{trace:000000}";
                    case 12:
                        return $"{DateTime.Now:HHmmss}";
                    case 13:
                        return $"{DateTime.Now:MMdd}";
                    case 15:
                        return $"{DateTime.Now:MMdd}";
                    case 17:
                        return $"{DateTime.Now:MMdd}";
                    case 35:
                        return track2Data;
                    case 37:
                        int refNum = _rnd.Next(1, 999999);
                        return $"{DateTime.Now:yyMMdd}{refNum:000000}";
                    case 61:
                        string bankId = "007";
                        try
                        {
                            if (SystemParameterInfo.BankInfo != null && !string.IsNullOrEmpty(SystemParameterInfo.BankInfo.BankID))
                            {
                                bankId = SystemParameterInfo.BankInfo.BankID;
                            }
                        }
                        catch
                        {
                            // fallback
                        }
                        return $"BK{bankId}PRO200000000000";
                    default:
                        if (defaults != null)
                        {
                            var def = defaults.BicField.FirstOrDefault(f => Convert.ToInt32(f.BitNum) == bitNum);
                            if (def != null) return def.Value;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to get default value for bit {bitNum}");
            }

            return string.Empty;
        }

        // --- Build ISO8583 message string ---
        public string BuildISO8583Message(string protocol, string mti, string header, List<ISOFieldInfo> fields, ref string parseLog)
        {
            var logBuilder = new StringBuilder();
            try
            {
                dynamic msg;
                if (protocol.ToUpper() == "FISC") msg = new FISC();
                else if (protocol.ToUpper() == "NCCC") msg = new NcccIso8583();
                else if (protocol.ToUpper() == "TWIN") msg = new TWINIso8583();
                else throw new ArgumentException($"Invalid protocol: {protocol}");

                msg.Mti(mti);

                foreach (var field in fields)
                {
                    if (string.IsNullOrEmpty(field.EntryData)) continue;

                    var entryData = field.EntryData;

                    // Pad automatically if length doesn't match definition (Fixed length fields)
                    if (field.FieldDataFormat.ToUpper() == "FIXED")
                    {
                        var defLen = Convert.ToInt32(field.Length.Trim());
                        if (entryData.Length < defLen)
                        {
                            if (field.FieldDataType.ToUpper() == "N")
                            {
                                entryData = entryData.PadLeft(defLen, '0');
                            }
                            else
                            {
                                entryData = entryData.PadRight(defLen, ' ');
                            }
                        }
                    }

                    // For FISC Field 48, if MTI is not 0800, we should ensure the minimum standard size of 27 
                    // (merchant code + spaces in FISC spec)
                    if (protocol.ToUpper() == "FISC" && field.BitNum == "048" && !mti.StartsWith("08"))
                    {
                        if (entryData.Length < 27)
                        {
                            entryData = entryData.PadRight(27, ' ');
                        }
                    }

                    // Validations matching WPF
                    if (field.Condition == "M" && string.IsNullOrEmpty(entryData.Trim()))
                    {
                        throw new ArgumentException($"Field [{field.BitNum}] is mandatory!");
                    }

                    if (field.FieldDataFormat.ToUpper() == "FIXED")
                    {
                        var defLen = Convert.ToInt32(field.Length.Trim());
                        if (entryData.Length != defLen)
                        {
                            throw new ArgumentException($"Field [{field.BitNum}] entry data length[{entryData.Length}] does not match definition length[{defLen}]!");
                        }
                    }

                    if (field.FieldDataType.ToUpper() == "N" && !entryData.All(char.IsDigit))
                    {
                        throw new ArgumentException($"Field [{field.BitNum}] entry data[{entryData}] must be numeric!");
                    }

                    var key = FIELDMethods.Val(Convert.ToInt32(field.BitNum.Trim()));
                    msg.Core().Field(key, entryData);
                }

                byte[] rawBytes = msg.Encode().Val();
                var buildResult = HexStringConverter.FromByteArray(rawBytes);

                // Re-parse output for verification log
                logBuilder.AppendLine($"--- ISO8583 Message Built Success ({protocol}) ---");
                logBuilder.AppendLine($"Raw Bytes (Hex): {buildResult}");
                logBuilder.AppendLine($"MTI: {mti}");
                logBuilder.AppendLine($"Header: {header}");
                logBuilder.AppendLine("\n--- Verification Decode Breakdown ---");

                dynamic reParsedMsg = protocol.ToUpper() switch
                {
                    "FISC" => new FISC(rawBytes),
                    "NCCC" => new NcccIso8583(rawBytes),
                    "TWIN" => new TWINIso8583(rawBytes),
                    _ => throw new InvalidOperationException()
                };

                foreach (var pair in reParsedMsg.Core().OrderedFields())
                {
                    var k = pair.Key;
                    var v = pair.Value;
                    if (v is byte[] b)
                    {
                        logBuilder.AppendLine($"FIELD [{k}] HexValue: [{HexStringConverter.FromByteArray(b)}]");
                    }
                    else
                    {
                        logBuilder.AppendLine($"FIELD [{k}] (Position {FIELDMethods.Val(k)}): [{v}]");
                    }
                }

                parseLog = logBuilder.ToString();
                return buildResult;
            }
            catch (Exception ex)
            {
                parseLog = $"Build Failed: {ex.Message}";
                return string.Empty;
            }
        }

        // --- Parse Raw Hex/ASCII Message ---
        public string ParseISO8583Message(string protocol, string rawMsg, ref string tokenDetails)
        {
            var mainLog = new StringBuilder();
            var tokenLog = new StringBuilder();

            try
            {
                if (string.IsNullOrEmpty(rawMsg)) return "Error: Input message is empty.";

                // Support both hex pasting and ASCII pasting
                byte[] rawBytes;
                if (rawMsg.Trim().All(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)) && rawMsg.Trim().Length % 2 == 0)
                {
                    rawBytes = HexStringConverter.ToByteArray(rawMsg.Replace(" ", "").Replace("\r", "").Replace("\n", "").Trim());
                }
                else
                {
                    rawBytes = Encoding.ASCII.GetBytes(rawMsg.Trim());
                }

                dynamic msg = protocol.ToUpper() switch
                {
                    "FISC" => new FISC(rawBytes),
                    "NCCC" => new NcccIso8583(rawBytes),
                    "TWIN" => new TWINIso8583(rawBytes),
                    _ => throw new ArgumentException($"Invalid protocol: {protocol}")
                };

                mainLog.AppendLine($"--- Begin Parsing {protocol} Message ---");
                mainLog.AppendLine($"Header: [{msg.Header().Encode()}]");
                mainLog.AppendLine($"BitMap: [{HexStringConverter.FromByteArray(msg.Bitmap())}]");
                mainLog.AppendLine($"MTI: [{msg.Mti()}]");
                mainLog.AppendLine("\n--- Fields List ---");

                foreach (var pair in msg.Core().OrderedFields())
                {
                    var k = pair.Key;
                    var v = pair.Value;
                    if (v is byte[] b)
                    {
                        mainLog.AppendLine($"FIELD [{k}] HexValue: [{HexStringConverter.FromByteArray(b)}]");
                    }
                    else
                    {
                        mainLog.AppendLine($"FIELD [{k}] (Position {FIELDMethods.Val(k)}): [{v}]");
                    }
                }
                mainLog.AppendLine($"--- End Parsing {protocol} Message ---");

                // Parse Tokens (Specifically for NCCC message tokens breakdown)
                if (protocol.ToUpper() == "NCCC")
                {
                    tokenLog.AppendLine("--- Begin NCCC Token Parsing ---");
                    int tokenFieldId = msg.GetTokenFieldId();
                    var tokenGroup = (TokenGroup)msg.Core().GetOthersValByKey(tokenFieldId.ToString());

                    if (tokenGroup != null)
                    {
                        tokenGroup.ShowTokens();
                        foreach (var token in tokenGroup.Tokens)
                        {
                            var tokenHeader = token.Value.Encode().Substring(0, Math.Min(10, token.Value.Encode().Length));
                            var tokenData = token.Value.Encode().Length > 10 ? token.Value.Encode().Substring(10) : "";
                            tokenLog.AppendLine($"[Token {token.Key}] Code: {token.Key}");
                            tokenLog.AppendLine($"  └─ Header: {tokenHeader}");
                            tokenLog.AppendLine($"  └─ Data: {tokenData}");
                            tokenLog.AppendLine($"  └─ Raw Encode: {token.Value.Encode()}\n");
                        }
                    }
                    else
                    {
                        tokenLog.AppendLine("No Token data present in this NCCC message.");
                    }
                    tokenLog.AppendLine("--- End NCCC Token Parsing ---");
                }
                else
                {
                    tokenLog.AppendLine("Token parsing is only supported for NCCC protocol messages.");
                }

                tokenDetails = tokenLog.ToString();
                return mainLog.ToString();
            }
            catch (Exception ex)
            {
                tokenDetails = $"Token Parse Exception: {ex.Message}";
                return $"Parsing Error: {ex.Message}";
            }
        }
    }
}
