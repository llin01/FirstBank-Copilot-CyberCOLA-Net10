using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.NcccMessage;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.Interchange;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.ClientMonitor;
using Com.Cybersoft4u.TM.FISC;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using Com.Cybersoft4u.TM.ISO8583.SVCS;
using Com.Cybersoft4u.Utils;
using Com.Cybersoft4u.Utils.NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using Application = System.Windows.Application;
using Cursors = System.Windows.Input.Cursors;
using RichTextBox = System.Windows.Controls.RichTextBox;
using Newtonsoft.Json;
using Com.Cybersoft4u.CommonLib.EAI;
using Com.Cybersoft4u.CommonLib.SMS;
using System.Text.Json;
using System.Net.Http;
using System.Text.Json.Serialization;
using Com.Cybersoft4u.TM.FISC.DE48;
using Com.Cybersoft4u.TM.FISC.DE58;
using System.Linq;
using Com.Cybersoft4u.TM.ISO8583.Category.CardType;
using Com.Cybersoft4u.CommonLib.MQ;

namespace Com.Cybersoft4u.Test.Wpf.WpfFunctionTest1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly SingletonOfSMSServerInfo SingletonOfSMSServerInfo = SingletonOfSMSServerInfo.Instance;
        private static readonly SingletonOfSMSWebAPIInfo SingletonOfSMSWebAPIInfo = SingletonOfSMSWebAPIInfo.Instance;
        private static readonly SystemParameterInfo SystemParameterInfo = SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo();
        private delegate void UpdateRichTextBox(System.Windows.Controls.Control richTextBox, string message, Brush brush);
        private readonly Logger Logger = null!;
        private ConcurrentDictionary<string, Com.Cybersoft4u.CommonLib.NcccMessage.MessageType> ncccMessageTypeInfo = null!;
        private string? _initialDirectory;
        private byte[]? _getISOBytes;
        private List<string> _selectMTILists = new List<string>();
        private static readonly Dictionary<Type, CardType> TypeMaps = new Dictionary<Type, CardType>();
        private static readonly Dictionary<string, CardType> IdMaps = new Dictionary<string, CardType>();
        private static readonly Dictionary<string, Type> TokenTypes = new Dictionary<string, Type>();

        public MainWindow()
        {
            try
            {
                LoggerAdapter.ReBuild(new NLoggerProvider(), "NLog.config");
                Logger = LoggerAdapter.GetLogger("LoggerHandler[WpfFunctionTest]");
                InitializeComponent();
                var dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                dispatcherTimer.Tick += dispatcherTimer_Tick;
                dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
                dispatcherTimer.Start();
                InitialSystemParameter();
            }
            catch (Exception ex)
            {
                Logger?.Fatal(ex);
            }
        }
        private void dispatcherTimer_Tick(object? sender, EventArgs e)
        {
            // Updating the Label which displays the current second
            TextBlockTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Forcing the CommandManager to raise the RequerySuggested event
            CommandManager.InvalidateRequerySuggested();
        }

        private void InitialSystemParameter()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            LoggerAdapter.ReBuild(new NLoggerProvider(), "NLog.config");
            Logger.Info("MainWindow Start");
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                ncccMessageTypeInfo = SingletonOfNcccMessageTypeMap.Instance.GetMessageTypeMap();
                foreach (var message in ncccMessageTypeInfo)
                {
                    string key = (String)message.Key;
                    MessageType messageType = (MessageType)message.Value;
                    //SetMessageText(RichTextBox1, $"MessageType{key}", Brushes.Red);
                    //messageType?.ResponseField.Field.ForEach(var => SetMessageText(RichTextBox1, $"{FIELDMethods.Val(Convert.ToInt32(var.Number)).ToString()}", Brushes.Red));
                }

                var configurationLoaderRoot = new ConfigurationLoaderRoot();
                _initialDirectory = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "InitialDirectory").Trim();
                TextBoxSelectFileName.Text = _initialDirectory;
                TextBoxSelectLocation.Text = _initialDirectory;
                var mtiLists = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "SelectMTILists").Trim();
                TextBoxMTI.Text = mtiLists;
                _selectMTILists = new List<string>(mtiLists.Split(';'));
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox2, ex.Message, Brushes.Red);
                Logger.Fatal(ex.Message);
                TextBlockMessage.Text = ex.Message;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }
        private void SetMessageText(System.Windows.Controls.Control richTextBox, string message, Brush brush)
        {
            if (richTextBox is not RichTextBox rtb) return;
            if (rtb.Dispatcher.CheckAccess())
            {
                TextRange rangeOfText1 = new TextRange(rtb.Document.ContentEnd, rtb.Document.ContentEnd);
                rangeOfText1.Text = message + '\r';
                rangeOfText1.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
                rangeOfText1.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
                rtb.ScrollToEnd();
            } //if
            else
            {
                rtb.Dispatcher.BeginInvoke(new UpdateRichTextBox(SetMessageText), richTextBox, message, brush);
            }
        }
        private void GetISOBytes()
        {
            try
            {
                _getISOBytes = null;
                var fileName = TextBoxSelectFileName.Text.Trim();
                if (!File.Exists(fileName))
                {
                    SetMessageText(RichTextBox1, $"File[{fileName}] Not Found", Brushes.Red);
                    TextBoxSelectFileName.Focus();
                    _getISOBytes = null;
                }
                var readBytes = File.ReadAllBytes(fileName);
                _getISOBytes = new byte[readBytes.Length];
                Buffer.BlockCopy(readBytes, 0, _getISOBytes, 0, readBytes.Length);
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox1, ex.Message, Brushes.Red);
                _getISOBytes = null;
            }
        }
        private void BtnCleanMessage_Click(object sender, RoutedEventArgs e)
        {
            TextBlockMessage.Text = "Clear Message Content";
            RichTextBox1.Document.Blocks.Clear();
            RichTextBox2.Document.Blocks.Clear();
        }
        private void BtnClosed_Click(object sender, RoutedEventArgs e)
        {
            for (int intCounter = Application.Current.Windows.Count - 1; intCounter >= 0; intCounter--)
                Application.Current.Windows[intCounter]?.Close();
            TextBlockMessage.Text = "Closed Window";
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            for (int intCounter = Application.Current.Windows.Count - 1; intCounter >= 0; intCounter--)
                Application.Current.Windows[intCounter]?.Close();
            TextBlockMessage.Text = "Closed Window";
        }

        private void BtnSearchLog_Click(object sender, RoutedEventArgs e)
        {
            // change big5 -> ms950
            //string big5Code = "81DD";
            //byte[] codeBytes = getCurrentTrueType(big5Code);
            ////使用BIG5編碼
            //string output = System.Text.Encoding.GetEncoding("BIG5").GetString(codeBytes);
            //SetMessageText(RichTextBox1, $"81DD的中文字為[{output}]", Brushes.Red);
            //string bigHexString = "81DD";
            //byte[] origBytes = new byte[2];
            //origBytes[1] = Convert.ToByte(bigHexString.Substring(0, 2), 16);
            //origBytes[0] = Convert.ToByte(bigHexString.Substring(2, 2), 16);
            //SetMessageText(RichTextBox1, $"81DD的中文字為[{Encoding.GetEncoding(950).GetString(origBytes)}]", Brushes.Red);
            //var returnData = Encoding.Convert(Encoding.GetEncoding(950),Encoding.Unicode, origBytes);

            //string text = "81dd";
            //byte[] bytes = new byte[2];
            //bytes[1] = Convert.ToByte(text.Substring(0, 2), 16);
            //bytes[0] = Convert.ToByte(text.Substring(2, 2), 16);
            //string result = Encoding.Unicode.GetString(bytes);
            //SetMessageText(RichTextBox1, $"81DD的中文字為[{result}]", Brushes.Red);
            try
            {
                Regex generalPattern = new Regex(@"\[(?<datetime>(.*?))\]\[(?<loggerlevel>(.*?))\]\[(?<thread>(.*?))\]\[(?<logger>(.*?))\]\[(?<message>(.*?))\]?");
                string inputFilePath = @"E:\YuantaBank\Development\CombineFileService\Data\IAGLookup_201706271321.log";
                string outputFilePath = @"E:\YuantaBank\Development\CombineFileService\Data\IAGLookup_201706271321.log.Out";
                var encode = Encoding.GetEncoding("big5");
                using (var output = new StreamWriter(outputFilePath, false, encode))
                {
                    using (var reader = new StreamReader(inputFilePath, encode))
                    {
                        while (reader.ReadLine() is { } readString)
                        {
                            SetMessageText(RichTextBox1, readString, Brushes.Red);
                            Match match = generalPattern.Match(readString);
                            if (!match.Success) continue;
                            string loggerLevel = match.Groups["loggerlevel"].Value;
                            var sb = new StringBuilder("AP.");
                            sb.Append(readString)
                              .Append(',')
                              .Append(loggerLevel.ToUpper().Equals("FATAL") ? "1" : "0");
                            SetMessageText(RichTextBox1, sb.ToString(), Brushes.Red);
                            output.WriteLine(sb.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox1, ex.Message, Brushes.Red);
            }
        }

        private void BtnFunctionTest4_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var fileName = @"E:\FirstBank\Development\CyberCOLA\src\CyberCOLA\WpfFunctionTest\Data\SMS\sms_big5.txt";
                if (!File.Exists(fileName))
                {
                    SetMessageText(RichTextBox1, $"File[{fileName}] not exist", Brushes.Red);
                    return;
                }
                SetMessageText(RichTextBox1, $"Default Encoding[{Encoding.Default}]", Brushes.Red);
                string readString = File.ReadAllText(fileName, Encoding.GetEncoding("big5"));
                //string readString = File.ReadAllText(fileName, CodePagesEncodingProvider.Instance.GetEncoding("big5"));
                SetMessageText(RichTextBox1, $"Data[{readString}]", Brushes.Red);
                string smsMessage = CommonLib.SMS.SMSCommUtility.ConvertXML2SMSMessage(readString, false);
                SetMessageText(RichTextBox1, $"Data[{smsMessage}]", Brushes.Red);
                var smsSendBytes = Encoding.Default.GetBytes(smsMessage);
                //Task.Run(() => ColaCommUtility.SendSMSMessageToSMSServer(smsSendBytes, SingletonOfSMSServerInfo.GetSMSServerInfo().SMSServers.SMSServer[0]));
                Task.Run(() => ColaCommUtility.SendSMSMessageToSMSServer(smsSendBytes, SingletonOfSMSServerInfo.GetSMSServerInfo()));
                return;
                string tempString1 = @"080999900302E23C00000000004000000030000000001645630100100020008903990401101010000011000011000011000011030080000000000000020210101000000101";
                var svcsRequestMessage = new SVCS(Encoding.ASCII.GetBytes(tempString1));
                SetMessageText(RichTextBox1, "--- Request Message ---", Brushes.Black);
                SetMessageText(RichTextBox1, $"Header[{svcsRequestMessage.Header().Encode()}]", Brushes.Black);
                SetMessageText(RichTextBox1, $"BitMap[{HexStringConverter.FromByteArray(svcsRequestMessage.Bitmap())}]", Brushes.Black);
                SetMessageText(RichTextBox1, $"MTI [{svcsRequestMessage.Mti()}]", Brushes.Black);
                foreach (var pair in svcsRequestMessage.Core().OrderedFields())
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    if (value is byte[] bytes)
                    {
                        var tempString = HexStringConverter.FromByteArray(bytes);
                        SetMessageText(RichTextBox1, $"FIELD [{key}] VALUE [{tempString}]", Brushes.Black);
                    }
                    else
                    {
                        SetMessageText(RichTextBox1,
                            $"FIELD [{key}] Field Position [{FIELDMethods.Val(key)}] VALUE [{value}]", Brushes.Black);
                    }
                }
                return;
                //Parsing CHB Log Data
                var outputFilePath = @"F:\Download\ChromeDownload\20201215 Session\";
                string[] files = null;
                using (var fbd = new FolderBrowserDialog())
                {
                    DialogResult result = fbd.ShowDialog();

                    if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                    {
                        files = Directory.GetFiles(fbd.SelectedPath);
                    }
                }

                if (files == null)
                {
                    SetMessageText(RichTextBox1, $"Select Files Invalid", Brushes.Black);
                    return;
                }

                var writeFileCount = 0;
                foreach (var file in files)
                {
                    byte[] fileBytes = File.ReadAllBytes(file);
                    int fiscDataCounter = 0;
                    if (fileBytes.Length < 4)
                    {
                        SetMessageText(RichTextBox1, $"File[{file}] Content error", Brushes.Black);
                        continue;
                    }
                    int offset = 0;
                    while (offset < fileBytes.Length)
                    {
                        var lengthDataLength = 4;
                        if (offset + lengthDataLength > fileBytes.Length) break;
                        int readDataLength = Convert.ToInt16(Encoding.ASCII.GetString(ColaCommUtility.GetSubBytes(fileBytes, offset, lengthDataLength)));

                        offset += lengthDataLength;
                        if (offset + readDataLength > fileBytes.Length) break;
                        var readBytes = ColaCommUtility.GetSubBytes(fileBytes, offset, readDataLength);
                        offset += readDataLength;
                        fiscDataCounter++;
                        try
                        {
                            var fisc = new FISC(ColaCommUtility.GetSubBytes(readBytes, 2, readBytes.Length - 2));
                            if ((!fisc.Mti().Substring(0, 2).Equals("08")) && (Convert.ToInt16(fisc.Mti().Substring(2, 1)) % 2 == 0))
                                System.Threading.Tasks.Task.Run(() => WriteFile($"{outputFilePath}{fisc.Mti()}_{++writeFileCount:00000}.dat", readBytes));
                        }
                        catch (Exception ex)
                        {
                            SetMessageText(RichTextBox1, $"File[{file}]-offset[{offset}]-lengthDataLength[{readDataLength}], Exception : {ex.Message}", Brushes.Red);
                            SetMessageText(RichTextBox1, $"HexString[{HexStringConverter.FromByteArray(readBytes)}]", Brushes.Red);
                            break;
                        }
                    }
                    SetMessageText(RichTextBox1, $"File[{file}] Read Data Counter[{fiscDataCounter}]", Brushes.Black);
                }
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox1, ex.Message, Brushes.Red);
            }

            return;
            //var now = DateTime.Now;
            //SetMessageText(RichTextBox1, $"Millisecond [{ now.Millisecond}],[{now:ffff}]", Brushes.Black);
            //SetMessageText(RichTextBox1, $"Random [{StaticRandom.Rand()}]", Brushes.Black);
            //return;
            //var nccc = new NcccMessage0800("164").GetNcccMessage();
            //foreach (var pair in nccc.Core().OrderedFields())
            //{
            //    var key = pair.Key;
            //    var value = pair.Value;
            //    if (value is byte[] bytes)
            //    {
            //        var tempString = HexStringConverter.FromByteArray(bytes);
            //        SetMessageText(RichTextBox1, $"FIELD [{key}] VALUE [{tempString}]", Brushes.Black);
            //    }
            //    else
            //    {
            //        SetMessageText(RichTextBox1, $"FIELD [{key}] Field Position [{FIELDMethods.Val(key)}] VALUE [{value}]", Brushes.Black);
            //    }
            //}
            //var dataBytes = nccc.Encode().Val();
            //var tempString1 = Encoding.ASCII.GetString(dataBytes);
            //SetMessageText(RichTextBox1, $"HexString [{tempString1}]", Brushes.Black);
            //var nccc1 = new NcccIso8583();
            //nccc1.Decode(dataBytes);
            //foreach (var pair in nccc1.Core().OrderedFields())
            //{
            //    var key = pair.Key;
            //    var value = pair.Value;
            //    if (value is byte[] bytes)
            //    {
            //        var tempString = HexStringConverter.FromByteArray(bytes);
            //        SetMessageText(RichTextBox1, $"FIELD [{key}] VALUE [{tempString}]", Brushes.Black);
            //    }
            //    else
            //    {
            //        SetMessageText(RichTextBox1, $"FIELD [{key}] Field Position [{FIELDMethods.Val(key)}] VALUE [{value}]", Brushes.Black);
            //    }
            //}
            //return;
            //nccc = new NcccMessage0800("002").GetNcccMessage();
            //foreach (var pair in nccc.Core().OrderedFields())
            //{
            //    var key = pair.Key;
            //    var value = pair.Value;
            //    if (value is byte[] bytes)
            //    {
            //        var tempString = HexStringConverter.FromByteArray(bytes);
            //        SetMessageText(RichTextBox1, $"FIELD [{key}] VALUE [{tempString}]", Brushes.Black);
            //    }
            //    else
            //    {
            //        SetMessageText(RichTextBox1, $"FIELD [{key}] Field Position [{FIELDMethods.Val(key)}] VALUE [{value}]", Brushes.Black);
            //    }
            //}
            //return;
            ////Start Test For TokenGroup
            //var token = new TokenGroup();
            //token.Tokens["04"] = new Token04();
            //var token_04 = (Token04)token.Tokens["04"];
            //SetMessageText(RichTextBox1, $"Token04[{token_04.Encode()}]", Brushes.Red);
            //token_04.DataDecode(string.Empty.PadLeft(20, ' '));
            //SetMessageText(RichTextBox1, $"Token04[{token_04.Encode()}]", Brushes.Red);
            //SetMessageText(RichTextBox1, $"Token[{token.Encode()}]", Brushes.Red);
            //End Test For TokenGroup

            //Start Test For ClientMonitor
            //ClientMonitorService(string entryPointName, EndPoint serverMonitorEndPoint, bool autoSignOn = true)

            var remoteEndPoint = new IPEndPoint(IPAddress.Parse("192.168.199.1"), 9901);
            var localEndPoint = new IPEndPoint(IPAddress.Parse("192.168.199.1"), 9901);
            var clientMonitorService = new GatewayClientMonitorService("FuncTest", remoteEndPoint, localEndPoint, GatewayClientAuthServiceType.NCCC);
            clientMonitorService.EnableLogger("GatewayClientMonitor", LogLevel.Debug);
            clientMonitorService.Start();
            var interchangeMessageOriginal = new InterchangeMessage();
            var dateTimeString = DateTime.Now.ToString("yyyyMMddHHmmss");
            interchangeMessageOriginal.StartingPoint = 'S';
            interchangeMessageOriginal.MessageId = ColaCommUtility.GetUniqueKey(dateTimeString);
            interchangeMessageOriginal.CreateDateTime = dateTimeString;
            interchangeMessageOriginal.SourceEndPoint = "127.0.0.1:0001";
            interchangeMessageOriginal.DestinationEndPoint = "127.0.0.1:0001";
            interchangeMessageOriginal.Category = InterChangeMessageCategory.ClientAuthNCCCClient;
            interchangeMessageOriginal.CommandCode = InterChangeMessageCommandCode.NCCCClientCommandCode.FileMaintUpdate;
            interchangeMessageOriginal.ReturnOption = true;
            // ReSharper disable once UseObjectOrCollectionInitializer
            var fileMaintDataOriginal = new FileMaintData();
            fileMaintDataOriginal.TxKeySeq = ColaCommUtility.GetUniqueKey(DateTime.Now.ToString("yyyyMMddHHmmss"));
            fileMaintDataOriginal.IsoP2CardNbr = "4563000100020003";
            DateTime dateNow = DateTime.Now;
            var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(dateNow);
            fileMaintDataOriginal.IsoP7TxDteTime = utcDateTime.ToString("MMddHHmmss");
            SetMessageText(RichTextBox1, fileMaintDataOriginal.IsoP7TxDteTime, Brushes.Black);
            fileMaintDataOriginal.IsoP11TraceNo = ColaCommUtility.GetSystemTraceNumber();
            fileMaintDataOriginal.IsoP39Rsp = "00";
            fileMaintDataOriginal.IsoP48AdditionalData = new string('0', 76);
            fileMaintDataOriginal.IsoP49TranCurCode = "901";
            fileMaintDataOriginal.IsoP58T05TxDte = DateTime.Now.ToString("yyyyMMddHHmmss");
            fileMaintDataOriginal.IsoP60FromHostMaintData = new string('0', 58);
            fileMaintDataOriginal.IsoS73ActDte = DateTime.Now.ToString("yyyyMMdd");
            fileMaintDataOriginal.IsoS91FileUpdCode = "1";
            fileMaintDataOriginal.IsoS92FileSecurityCode = "57";
            fileMaintDataOriginal.IsoS101FileName = "NF";
            fileMaintDataOriginal.IsoS120FileUpd = new string('0', 15);
            fileMaintDataOriginal.IsoS122HostMaint = $"R{DateTime.Now:yyyyMMdd}{DateTime.Now.Add(new TimeSpan(-1)):yyyyMMdd}";
            interchangeMessageOriginal.InterchangeDataFormat = InterChangeDataFormatType.Object;
            //interchangeMessageOriginal.InterchangeData = ColaCommUtility.ObjectSerialize<FileMaintData>(fileMaintDataOriginal);
            interchangeMessageOriginal.InterchangeData = ColaCommUtility.ObjectToXMLBytes(fileMaintDataOriginal);
            //var interchangeMessageBytes = ColaCommUtility.ObjectSerialize<InterchangeMessage>(interchangeMessageOriginal);
            var interchangeMessageBytes = ColaCommUtility.ObjectToXMLBytes(interchangeMessageOriginal);
            var interchangeMessage = ColaCommUtility.XMLBytesToObject<InterchangeMessage>(interchangeMessageBytes);
            Logger.Info(ColaCommUtility.GetObjectProperty(interchangeMessage));
            var fileMaintData = ColaCommUtility.XMLBytesToObject<FileMaintData>(interchangeMessage.InterchangeData);
            Logger.Info(ColaCommUtility.GetObjectProperty(fileMaintData));
            var nccc = new NcccMessage0300(fileMaintData).GetNcccMessage();
            SetMessageText(RichTextBox1, $"Header[{nccc.Header().Encode()}]", Brushes.Black);
            SetMessageText(RichTextBox1, $"BitMap[{HexStringConverter.FromByteArray(nccc.Bitmap())}]", Brushes.Black);
            SetMessageText(RichTextBox1, $"MTI [{nccc.Mti()}]", Brushes.Black);
            foreach (var pair in nccc.Core().OrderedFields())
            {
                var key = pair.Key;
                var value = pair.Value;
                if (value is byte[] bytes)
                {
                    var tempString = HexStringConverter.FromByteArray(bytes);
                    SetMessageText(RichTextBox1, $"FIELD [{key}] VALUE [{tempString}]", Brushes.Black);
                }
                else
                {
                    SetMessageText(RichTextBox1, $"FIELD [{key}] Field Position [{FIELDMethods.Val(key)}] VALUE [{value}]", Brushes.Black);
                }
            }
        }

        private void BtnSelectFile_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog1 = new System.Windows.Forms.OpenFileDialog();

            openFileDialog1.InitialDirectory = _initialDirectory;
            openFileDialog1.Title = @"Browse Process Files";

            openFileDialog1.CheckFileExists = true;
            openFileDialog1.CheckPathExists = true;

            openFileDialog1.DefaultExt = "dat";
            openFileDialog1.Filter = @"Text files (*.dat)|*.dat|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;

            openFileDialog1.ReadOnlyChecked = true;
            openFileDialog1.ShowReadOnly = true;

            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TextBoxSelectFileName.Text = openFileDialog1.FileName;
            }
        }

        private void TableItem2BtnNcccAuthDFlow_Click(object sender, RoutedEventArgs e)
        {
            var ncccMessageHexString = @"ISO0260000700120B238C4812EA1800A00000000000000020000300000009664530113105531966453105531011301139876902060649381739374377578880747103=130910110480240000001701139664539134900013996187        NCCC FOR IC TESTING   CHINMEN-MATZUTW TW0276619400169         00010000901019BK17BC11000        108& 0000400108! 0400020                   Y1! C000026         105               ! B400020 022500            0 134000000                6619400169      41                                ST                                        0000000000010010921 ";
            try
            {
                NcccIso8583 ncccRequestMessage = new NcccIso8583(Encoding.ASCII.GetBytes(ncccMessageHexString));
                byte[] ncccRequestRawData = ColaCommUtility.ContactBytesArray(ColaCommUtility.IntegerToBinary(Encoding.ASCII.GetBytes(ncccMessageHexString).Length), Encoding.ASCII.GetBytes(ncccMessageHexString));
                SetMessageText(RichTextBox1, $"Original Data[{ColaCommUtility.GetDisplayForRawData(ncccRequestRawData)}],Length[{Encoding.ASCII.GetBytes(ncccMessageHexString).Length}]", Brushes.Red);
                SetMessageText(RichTextBox1, $"Add 2 Bytes,Hex Data Length[{ncccRequestRawData[0]:X02}-{ncccRequestRawData[1]:X02}]", Brushes.Red);
                byte[] retnProcessBytes = NcccAuthFlowUtility.ProcessTransactionFlow(ncccRequestMessage, "OnlineSession", string.Empty);
                if (retnProcessBytes != null)
                {
                    SetMessageText(RichTextBox1, Encoding.ASCII.GetString(retnProcessBytes), Brushes.Red);
                }
                else
                {
                    SetMessageText(RichTextBox1, "Return Null Data", Brushes.Red);
                }
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox1, ex.Message, Brushes.Red);
            }
        }

        //include System.Web.Services
        private void BtnWebService_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                //CountryInfoService countryInfoService = new CountryInfoService();
                ////call System.Web.Services.Protocols.SoapDocumentMethodAttribute define method
                ////var returnData = countryInfoService.FullCountryInfoAllCountries();
                ////var returnData = countryInfoService.ListOfLanguagesByName();
                ////var returnData = countryInfoService.ListOfCurrenciesByName();
                //var returnData = countryInfoService.ListOfCurrenciesByCode();
                //foreach (var data in returnData)
                //{
                //    Type myType = data.GetType();
                //    IList<PropertyInfo> props = new List<PropertyInfo>(myType.GetProperties());
                //    foreach (PropertyInfo prop in props)
                //    {
                //        object propValue = prop.GetValue(data, null);
                //        SetMessageText(RichTextBox1, $"Name[{prop.Name}], Value[{propValue.ToString()}]", Brushes.Red);
                //    }
                //}
                //WebApi webApi = new WebApi();
                //webApi.Timeout = 1;
                //var returnData = webApi.QueryBill(string.Empty);
                //SetMessageText(RichTextBox1, returnData, Brushes.Red);
                //webApi.SoapVersion


                XElement contacts = new XElement("ENVELOPE",
                                        new XElement("MESSAGE",
                                            new XElement("TXN_DATETIME", "123456"),
                                            new XElement("API_TXN_NO", "123456"),
                                            new XElement("MTI", "123456"),
                                            new XElement("PCODE", "123456"),
                                            new XElement("SRC_ID", "123456"),
                                            new XElement("SESSION_ID", "123456"),
                                            new XElement("FEE_REF_ID", "123456"),
                                            new XElement("HC", 1),
                                            new XElement("TXN_INFO",
                                                new XElement("NOTICE_NO", "567889999"),
                                                new XElement("TXN_AMOUNT", 100)
                                            ),
                                            new XElement("PAY_INFO",
                                                new XElement("CC",
                                                    new XElement("CARD_NUMBER", "Mercer Island"),
                                                    new XElement("EXPIRE_DATE", "WA"),
                                                    new XElement("CVV2", "WA"),
                                                    new XElement("ACQ_BIN", "WA"),
                                                    new XElement("RRN", "WA"),
                                                    new XElement("TERMINAL_ID", "WA")
                                                )
                                            )),
                                        new XElement("MAC_INFO",
                                            new XElement("KEY_ID", "123456"),
                                            new XElement("DIV_DATA", "Mercer Island"),
                                            new XElement("ICV", "WA"),
                                            new XElement("MAC", "WA")
                                        )
                                    );
                SetMessageText(RichTextBox1, contacts.ToString(), Brushes.Red);
                var envelope = new ENVELOPE();
                var message = new MESSAGE();
                var macInfo = new MAC_INFO();
                var txnInfo = new TXN_INFO();
                var payInfo = new PAY_INFO();
                var cc = new CC();
                message.TXN_DATETIME = "123456";
                message.API_TXN_NO = "123456";
                message.MTI = "123456";
                message.PCODE = "123456";
                message.SRC_ID = "123456";
                message.SESSION_ID = "123456";
                message.FEE_REF_ID = "123456";
                message.HC = "1";
                txnInfo.NOTICE_NO = "123456";
                txnInfo.TXN_AMOUNT = "123456";

                cc.CARD_NUMBER = "123456";
                cc.EXPIRE_DATE = "123456";
                cc.CVV2 = "123456";
                cc.ACQ_BIN = "123456";
                cc.RRN = "123456";
                cc.TERMINAL_ID = "123456";

                macInfo.KEY_ID = "123456";
                macInfo.DIV_DATA = "123456";
                macInfo.ICV = "123456";
                macInfo.MAC = "123456";


                payInfo.CC = cc;
                message.TXN_INFO = txnInfo;
                message.PAY_INFO = payInfo;

                envelope.MESSAGE = message;
                envelope.MAC_INFO = macInfo;

                // removes version
                var xmlString = ColaCommUtility.ObjectToXMLString<ENVELOPE>(envelope, true);
                SetMessageText(RichTextBox1, xmlString, Brushes.Red);
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox1, ex.Message, Brushes.Red);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void BtnSelectFileLocation_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(TextBoxSelectLocation.Text.Trim()))
            {
                SetMessageText(RichTextBox1, $"Directory[{TextBoxSelectLocation.Text.Trim()}] No Exist", Brushes.Red);
                TextBoxSelectLocation.Focus();
                TextBoxSelectLocation.SelectionStart = 0;
                return;
            }
            Process.Start("explorer.exe", TextBoxSelectLocation.Text.Trim());
            //using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            //{
            //    System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            //
            //    if (result == System.Windows.Forms.DialogResult.OK)
            //    {
            //        TextBoxSelectLocation.Text = dialog.SelectedPath;
            //    }
            //}
        }

        private void WriteFile(string writeFileName, byte[] writeBytes)
        {
            File.WriteAllBytes(writeFileName, writeBytes);
        }

        private void BtnFunctionTest3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                GetISOBytes();
                if (_getISOBytes is null || _getISOBytes.Length == 0)
                {
                    SetMessageText(RichTextBox1, $"BtnFunctionTest3_Click Input Data Error", Brushes.Red);
                    return;
                }
                NcccIso8583 nccc = new NcccIso8583(_getISOBytes);
                SetMessageText(RichTextBox1, $"Header[{nccc.Header().Encode()}]", Brushes.Black);
                SetMessageText(RichTextBox1, $"BitMap[{HexStringConverter.FromByteArray(nccc.Bitmap())}]", Brushes.Black);
                SetMessageText(RichTextBox1, $"MTI [{nccc.Mti()}]", Brushes.Black);
                foreach (var pair in nccc.Core().OrderedFields())
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    if (value is byte[] bytes)
                    {
                        var tempString = HexStringConverter.FromByteArray(bytes);
                        SetMessageText(RichTextBox1, $"FIELD [{key}] VALUE [{tempString}]", Brushes.Black);
                    }
                    else
                    {
                        SetMessageText(RichTextBox1, $"FIELD [{key}] Field Position [{FIELDMethods.Val(key)}] VALUE [{value}]", Brushes.Black);
                    }
                }
                var fieldName = FIELD.F_35;
                if (nccc.Core().Field(fieldName) != null)
                {
                    string track2Data = (string)nccc.Core().Field(fieldName);
                    int startIndex = track2Data.IndexOfAny(new char[] { '=', 'D', '~', '^' });
                    var cardNumber = string.Empty;
                    if (startIndex > 0)
                        cardNumber = track2Data.Substring(0, startIndex);
                    else
                        cardNumber = track2Data;
                    if (Regex.Match(cardNumber, @"^4[0-9]{12}(?:[0-9]{3})?$").Success)
                    {
                        SetMessageText(RichTextBox1, "Visa", Brushes.Red);
                    }
                    else if (Regex.Match(cardNumber, @"^(?:5[1-5][0-9]{2}|222[1-9]|22[3-9][0-9]|2[3-6][0-9]{2}|27[01][0-9]|2720)[0-9]{12}$").Success)
                    {
                        SetMessageText(RichTextBox1, "MasterCard", Brushes.Red);
                    }
                    else if (Regex.Match(cardNumber, @"^(?:2131|1800|35\d{3})\d{11}$").Success)
                    {
                        SetMessageText(RichTextBox1, "MasterCard", Brushes.Red);
                    }
                    else
                    {
                        SetMessageText(RichTextBox1, "UnKnow", Brushes.Red);
                    }
                }
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox1, ex.Message, Brushes.Red);
            }

        }

        private void BtnCommandQRCode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //string inputData = @"0541EAI WLET          2020041611252068                    XTWA 4380450100061406 2609 164641307710000015 5812 TW  EMV QR TEST               Taipei City                                         MP 901 0 000000010.00    000000000000 00.00 0000000010.00 01.00000          20200416 112520 010703003607        011901493560876BAE7C0A2        02100930243689                                                                               0930243689       1071512@tsbtmail.com                                                                                E";
                //byte[] receiveBytes = Encoding.ASCII.GetBytes(inputData);
                //var checkMessage = EAICommUtility.CheckEAIXTWAMessageFormat(receiveBytes);
                //if (!checkMessage.Equals(SystemConstantData.EAISystemCodeAndData.FormatCheckSuccess))
                //{
                //    var sendBytes = EAICommUtility.ConvertEAIXTWAIncoming2Outgoing(receiveBytes, checkMessage);
                //    Logger.Info($"Send: [{Encoding.ASCII.GetString(sendBytes)}] at {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}. The server has send {sendBytes.Length} bytes.");
                //}
                string rspJsonFilePath = @"E:\FirstBank\Development\CyberCOLA\src\CyberCOLA\WpfFunctionTest1\Data\FCB9GQ_Rsp.json";
                string reqJsonFilePath = @"E:\FirstBank\Development\CyberCOLA\src\CyberCOLA\WpfFunctionTest1\Data\FCB9GQ_Req.json";
                string rspJsonString = File.ReadAllText(rspJsonFilePath);
                string reqJsonString = File.ReadAllText(reqJsonFilePath);
                SetMessageText(RichTextBox1, $"JSON Request[{reqJsonString}]", Brushes.Red);
                SetMessageText(RichTextBox1, $"JSON Response[{rspJsonString}]", Brushes.Red);
                var rspObject = JsonConvert.DeserializeObject<FCB9GQ.FCB9GQ_RSP>(rspJsonString);
                var reqObject = JsonConvert.DeserializeObject<FCB9GQ.FCB9GQ_REQ>(reqJsonString);

                //var rspConvertJsonString = JsonConvert.SerializeObject(rspObject, Formatting.Indented);
                //var reqConvertJsonString = JsonConvert.SerializeObject(reqObject, Formatting.Indented);
                //SetMessageText(RichTextBox1, $"Response Convert JSON String[{rspConvertJsonString}]", Brushes.Red);
                //SetMessageText(RichTextBox1, $"Request Convert JSON String[{reqConvertJsonString}]", Brushes.Red);
                ////
                //string[] returnStrings = new[] { "000", "111", "222", "333", "444", "555", "666" };
                //var fcb9gQResponseMessageJSON = new FCB9GQ.FCB9GQ_REQ();
                ////set up FCB9GQ_REQ
                //var reqJSONObject = new FCB9GQ.FCB9GQ_REQ();
                //reqJSONObject.TXN_ID = "FCB9GQREQ_QRTN";
                //reqJSONObject.FN_CODE = "1";
                //reqJSONObject.TERMINAL_ID = "1234";
                //reqJSONObject.USER_ID = "1234";
                //var reqBody = new FCB9GQ.FCB9GQ_REQ_BODY();
                //reqBody.CreditCardNo = "4234567890123456";
                //reqBody.MerPAN = "1234567890123456";
                //reqBody.TxnSeqNo = "123456789012";
                //reqBody.TxnAmtNTD = 12.22M;
                //reqBody.MCC = "123456789012";
                //reqBody.CurCode = "123";
                //reqBody.OriTxnAmt = 123.00M;
                //reqBody.CountryCode = "12";
                //reqBody.MerName = "1234567890123456789012345";
                //reqBody.MerCity = "123456789012345";
                //reqBody.WalletAppInd = "12";
                //reqBody.ReserveField = "123456789012345678901234567980123456798012345678";
                //reqBody.QRLen = "132";
                //reqBody.QRMsg = "123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345";
                //reqJSONObject.BODY = reqBody;
                ////set up FCB9GQ_RSP
                //var rspJSONObject = new FCB9GQ.FCB9GQ_RSP();
                //rspJSONObject.FMH = "";
                //rspJSONObject.FN_CODE = "";
                //rspJSONObject.RESPONSE_CODE = "T000";
                //rspJSONObject.RESPONSE_DESCRIPTION = "成功";
                //rspJSONObject.TERMINAL_ID = "1234";
                //rspJSONObject.USER_ID = "1234";
                //var rspBody = new FCB9GQ.FCB9GQ_RSP_BODY();
                //var nowDateTime = DateTime.Now.ToString("yyyyMMddHHmmss");
                //rspBody.TxnDate = nowDateTime.Substring(0, 8);
                //rspBody.TxnTime = nowDateTime.Substring(8);
                //rspBody.CreditCardNo = "4234567890123456";
                //rspBody.MerPAN = "1234567890123456";
                //rspBody.QRTxnSeqNo = "123456789012";
                //rspBody.TxnAmtNTD = 12.22M;
                //rspBody.AuthorizationCode = "123456";
                //rspJSONObject.BODY = rspBody;
                //var reqJSONString = JsonConvert.SerializeObject(reqJSONObject, Formatting.Indented);
                //var rspJSONString = JsonConvert.SerializeObject(rspJSONObject, Formatting.Indented);
                var reqJSONString = JsonConvert.SerializeObject(reqObject, Newtonsoft.Json.Formatting.Indented);
                var rspJSONString = JsonConvert.SerializeObject(rspObject, Newtonsoft.Json.Formatting.Indented);
                SetMessageText(RichTextBox1, $"Request JSON String[{reqJSONString}]", Brushes.Red);
                SetMessageText(RichTextBox1, $"Response JSON String[{rspJSONString}]", Brushes.Red);
                var nccc = EAICommUtility.ConvertFCB9GQMessage2NcccIso8583(reqObject);
                SetMessageText(RichTextBox1, $"Header[{nccc.Header().Encode()}]", Brushes.Black);
                SetMessageText(RichTextBox1, $"BitMap[{HexStringConverter.FromByteArray(nccc.Bitmap())}]", Brushes.Black);
                SetMessageText(RichTextBox1, $"MTI [{nccc.Mti()}]", Brushes.Black);
                foreach (var pair in nccc.Core().OrderedFields())
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    if (value is byte[] bytes)
                    {
                        var tempString = HexStringConverter.FromByteArray(bytes);
                        SetMessageText(RichTextBox1, $"FIELD [{key}] VALUE [{tempString}]", Brushes.Black);
                    }
                    else
                    {
                        SetMessageText(RichTextBox1, $"FIELD [{key}] Field Position [{FIELDMethods.Val(key)}] VALUE [{value}]", Brushes.Black);
                    }
                }
                var tokenGroup = (TokenGroup)nccc.Core().GetOthersValByKey(nccc.GetTokenFieldId().ToString());
                SetMessageText(RichTextBox1, $"tokenGroup 1,Token Id[{nccc.GetTokenFieldId().ToString()}]", Brushes.Red);
                if (tokenGroup == null) return;
                SetMessageText(RichTextBox1, $"tokenGroup 2", Brushes.Red);
                //tokenGroup.ShowTokens();
                foreach (KeyValuePair<string, Token> token in tokenGroup.Tokens)
                {
                    SetMessageText(RichTextBox1, $"[Token {token.Key}] = [{token.Value.Encode()}]", Brushes.Red);
                }
                SetMessageText(RichTextBox1, $"EAICommUtility.UpdateExpiredDate", Brushes.Red);
                string[] returnStrings = new string[] { "1212", "00", "A", "NCCC" };
                EAICommUtility.UpdateExpiredDate(ref nccc, reqObject, returnStrings);
                SetMessageText(RichTextBox1, $"Header[{nccc.Header().Encode()}]", Brushes.Black);
                SetMessageText(RichTextBox1, $"BitMap[{HexStringConverter.FromByteArray(nccc.Bitmap())}]", Brushes.Black);
                SetMessageText(RichTextBox1, $"MTI [{nccc.Mti()}]", Brushes.Black);
                foreach (var pair in nccc.Core().OrderedFields())
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    if (value is byte[] bytes)
                    {
                        var tempString = HexStringConverter.FromByteArray(bytes);
                        SetMessageText(RichTextBox1, $"FIELD [{key}] VALUE [{tempString}]", Brushes.Black);
                    }
                    else
                    {
                        SetMessageText(RichTextBox1, $"FIELD [{key}] Field Position [{FIELDMethods.Val(key)}] VALUE [{value}]", Brushes.Black);
                    }
                }
                tokenGroup = (TokenGroup)nccc.Core().GetOthersValByKey(nccc.GetTokenFieldId().ToString());
                if (tokenGroup == null) return;
                //tokenGroup.ShowTokens();
                foreach (KeyValuePair<string, Token> token in tokenGroup.Tokens)
                {
                    SetMessageText(RichTextBox1, $"[Token {token.Key}] = [{token.Value.Encode()}]", Brushes.Red);
                }
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox1, ex.Message, Brushes.Red);
            }
        }

        private async void BtnSMSWebAPI_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var jsonOption = new JsonSerializerOptions()
                {
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                var correlationId = Guid.NewGuid().ToString();
                var smsParameterModel = new SMSParameterModel();
                smsParameterModel.SystemId = SingletonOfSMSWebAPIInfo.GetSMSWebAPIInfo().DataInfo.SystemId;
                smsParameterModel.SourceId = SingletonOfSMSWebAPIInfo.GetSMSWebAPIInfo().DataInfo.SourceId;
                smsParameterModel.CellPhone = "123456789";
                smsParameterModel.Name = "王先生";
                smsParameterModel.SmsProvider = SingletonOfSMSWebAPIInfo.GetSMSWebAPIInfo().SMSWebAPI.SmsProvider;
                smsParameterModel.MessageBody = @"123456為您使用VISA SERVE 綁定第一銀行信用卡後四碼1234服務驗證碼，提醒您認證有效時間為02/03 10:58:00，逾期失效，謝謝";
                var jsonContent = new StringContent(System.Text.Json.JsonSerializer.Serialize(smsParameterModel, jsonOption), Encoding.UTF8, "application/json");
                //var json = System.Text.Json.JsonSerializer.Serialize(smsParameterModel);
                //StringContent jsonContent = new(json, Encoding.UTF8, "application/json");
                string myContent = await jsonContent.ReadAsStringAsync();
                SetMessageText(RichTextBox1, $"StringContent[{myContent}]", Brushes.Red);
                var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri(SingletonOfSMSWebAPIInfo.GetSMSWebAPIInfo().SMSWebAPI.Urls.Url[0]);
                httpClient.Timeout = new TimeSpan(0, 0, 0, Convert.ToInt32(SingletonOfSMSWebAPIInfo.GetSMSWebAPIInfo().SMSWebAPI.TimeoutMilliseconds) / 1000);
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("x-correlation-id", correlationId);
                using var response = await httpClient.PostAsync($"{SingletonOfSMSWebAPIInfo.GetSMSWebAPIInfo().SMSWebAPI.Urls.Url[0]}Messages", jsonContent);
                if (response.IsSuccessStatusCode) //range 200-299
                {
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    var sMSResponse = System.Text.Json.JsonSerializer.Deserialize<SMSResponseModel>(apiResponse, jsonOption);
                    SetMessageText(RichTextBox1, $"Return Message[{sMSResponse?.SMS_STATUS_STRING}]", Brushes.Red);
                }
                else
                {
                    SetMessageText(RichTextBox1, $"Call[SMS BackEnd] Fail :{(int)response.StatusCode}:{response.StatusCode}", Brushes.Red);
                }
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox1, $"Call[SMS BackEnd] Fail Exception:{ex.Message}", Brushes.Red);
            }
        }

        private void BtnCommandMQ_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var vdMessage = new VDMessage.RequestXML();
                var header = new VDMessage.RequestHeader();
                header.TxID = "OAOEARMK";
                header.SystemKey = "FCB9G";
                header.TxSeqNo = "20230220000021";
                header.MsgSeqNo = "00001";
                header.CltTimeStamp = "2023 - 02 - 20 09:25:00.000";
                header.CustID = "00000000";
                header.UserID = "0000";
                header.AcctNo = "00000000000";
                header.MsgDirection = "RQ";
                vdMessage.RequestHeader = header;
                var txRq = new VDMessage.TxRq();
                txRq.FuncCode = "UCT7";
                txRq.AcctNo = "帳戶號碼";
                txRq.CardNo = "金融卡卡序號";
                txRq.CreditNo = "信用卡號";
                txRq.AuthNo = "授權碼";
                txRq.TxAmt = "1000.00";
                vdMessage.TxRq = txRq;
                var xmlString = ColaCommUtility.ObjectToXMLString<VDMessage.RequestXML>(vdMessage, true);
                SetMessageText(RichTextBox1, xmlString, Brushes.Red);
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox1, $"Convert Fail Exception:{ex.Message}", Brushes.Red);
            }
        }
        private async void BtnCreateFISCRawData_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                Regex generalPattern = new Regex(@".*\[FiscSession\]\]\[\[.{7,}.*");
                string inputFilePath = TextBoxSelectFileName.Text.Trim();
                string outputDirPath = TextBoxSelectLocation.Text.Trim();
                var encode = Encoding.GetEncoding("big5");
                //check File exist
                if (!File.Exists(inputFilePath))
                {
                    SetMessageText(RichTextBox1, $"File[{inputFilePath}] Not Found", Brushes.Red);
                    TextBoxSelectFileName.Focus();
                }
                if (!Directory.Exists(outputDirPath))
                {
                    SetMessageText(RichTextBox1, $"Directory[{outputDirPath}] Not Found", Brushes.Red);
                    TextBoxSelectLocation.Focus();
                }
                string traceNo = TextBoxTraceNo.Text.Trim();
                if (CheckBoxTraceNo.IsChecked == true)
                {
                    if (string.IsNullOrEmpty(traceNo) || traceNo.Length != 6)
                    {
                        SetMessageText(RichTextBox1, $"TraceNo is Empty or Length Error[{traceNo}]", Brushes.Red);
                        TextBoxTraceNo.Focus();
                        return;
                    }
                }
                bool traceNoFlag = (bool)(CheckBoxTraceNo.IsChecked);
                var mtiLists = TextBoxMTI.Text.Trim();
                try
                {
                    _selectMTILists = new List<string>(mtiLists.Split(';'));
                }
                catch (Exception ex)
                {
                    SetMessageText(RichTextBox1, $"MTI Entry Data Error[{mtiLists}], Exception : {ex.Message}", Brushes.Red);
                    TextBoxMTI.Focus();
                    return;
                }
                SetMessageText(RichTextBox1, $"Start Process at {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
                await Task.Run(() =>
                {
                    ProcessLogFile(inputFilePath, outputDirPath, traceNo, traceNoFlag, mtiLists);
                });
                SetMessageText(RichTextBox1, $"End Process at {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}", Brushes.Red);
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox1, ex.Message, Brushes.Red);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private async void SendUIMessage(string message)
        {
            //This code will run on a non - UI thread
            //Send message to window
            await Task.Run(() =>
            {
                // This code will run on a non-UI thread
                // Send message to window
                Dispatcher.Invoke(() =>
                {
                    // This code will run on the UI thread
                    MainWindow? main = Application.Current.MainWindow as MainWindow;
                    main?.SetMessageText(RichTextBox1, $"{message}", Brushes.Black);
                });
            });
        }

        private void ProcessLogFile(string inputFilePath, string outputDirPath, string traceNo, bool traceNoFlag, string mtiLists)
        {
            Regex generalPattern = new Regex(@".*\[FiscSession\]\]\[\[.{7,}.*");
            var encode = Encoding.GetEncoding("big5");
            try
            {
                SendUIMessage($"InputFilePath[{inputFilePath}], OutputDirPath[{outputDirPath}], TraceNo[{traceNo}], TraceNoFlag[{traceNoFlag}], MtiLists[{mtiLists}]");
                long readCount = 0L;
                long skipCount = 0L;
                int matchCount = 0;
                using (var reader = new StreamReader(inputFilePath, encode))
                {
                    while (reader.ReadLine() is { } readString)
                    {
                        readCount++;
                        Match match = generalPattern.Match(readString);
                        if (!match.Success)
                        {
                            skipCount++;
                            continue;
                        }
                        Regex selectPattern = new Regex(@"\[(?<datetime>(.*?))\]\[(?<loggerlevel>(.*?))\]\[(?<thread>(.*?))\]\[(?<logger>(.*?))\]\]\[\[(?<message>(.*?))\]\]?");
                        match = selectPattern.Match(readString);
                        if (!match.Success)
                        {
                            skipCount++;
                            continue;
                        }
                        Logger.Info($"Read String [{readString}]");
                        string rawMessage = match.Groups["message"].Value;
                        Logger.Info($"Read Raw Data[{rawMessage}]");
                        if (rawMessage.Length < 5)
                        {
                            skipCount++;
                            continue;
                        }
                        byte[] rawMessageBytes = ColaCommUtility.HexString2HexBinary(rawMessage);
                        byte[] messageBytes = new byte[rawMessageBytes.Length - 2];
                        Buffer.BlockCopy(rawMessageBytes, 2, messageBytes, 0, messageBytes.Length);
                        FISC fisc = new FISC(messageBytes);
                        //fisc.ShowFields();
                        Logger.Info($"BitMap[{HexStringConverter.FromByteArray(fisc.Bitmap())}]");
                        Logger.Info($"MTI [{fisc.Mti()}]");
                        foreach (var pair in fisc.Core().OrderedFields())
                        {
                            var key = pair.Key;
                            var value = pair.Value;
                            if (value is byte[] bytes)
                            {
                                var tempString = HexStringConverter.FromByteArray(bytes);
                                Logger.Info($"FIELD [{key}] VALUE [{tempString}]");
                            }
                            else
                            {
                                Logger.Info($"FIELD [{key}] Field Position [{FIELDMethods.Val(key)}] VALUE [{value}]");
                            }
                        }
                        if (traceNoFlag)
                        {
                            if (!ObjectUtils.IsNull(fisc.RawVal(FIELD.F_11)))
                            {
                                string fiscTraceNo = fisc.Core().Field(FIELD.F_11).ToString();
                                if (fiscTraceNo.Equals(traceNo))
                                {
                                    string outputFilePath = $"{outputDirPath}\\TraceNo_{traceNo}_{++matchCount}.txt";
                                    using (FileStream fileStream = new FileStream(outputFilePath, FileMode.Create))
                                    {
                                        fileStream.Write(rawMessageBytes, 0, rawMessageBytes.Length);
                                    }
                                    Logger.Info($"Write TraceNo[{traceNo}] raw data to [{outputFilePath}]");
                                }
                            }
                            else
                            {
                                skipCount++;
                            }
                        }
                        else
                        {
                            if ((_selectMTILists.Count != 0) && (!_selectMTILists.Contains(fisc.Mti())))
                            {
                                skipCount++;
                                continue;
                            }
                            var outputFilePath = $"{outputDirPath}\\NoTraceNo_{fisc.Mti()}_{++matchCount}.txt";
                            using (FileStream fileStream = new FileStream(outputFilePath, FileMode.Create))
                            {
                                fileStream.Write(rawMessageBytes, 0, rawMessageBytes.Length);
                            }
                            Logger.Info($"Write TraceNo[{traceNo}] raw data to [{outputFilePath}]");
                        }
                    }
                }
                Logger.Info($"Read Count[{readCount}], Skip Count[{skipCount}], Match Count[{matchCount}]");
                SendUIMessage($"Read Count[{readCount}], Skip Count[{skipCount}], Match Count[{matchCount}]");
            }
            catch (Exception ex)
            {
                Logger.Info($"ProcessLog Exception:{ex.Message}");
            }

        }

        private void BtnIDVerify_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //string inputData = @"0541EAI WLET          2020041611252068                    XTWA 4380450100061406 2609 164641307710000015 5812 TW  EMV QR TEST               Taipei City                                         MP 901 0 000000010.00    000000000000 00.00 0000000010.00 01.00000          20200416 112520 010703003607        011901493560876BAE7C0A2        02100930243689                                                                               0930243689       1071512@tsbtmail.com                                                                                E";
                //byte[] receiveBytes = Encoding.ASCII.GetBytes(inputData);
                //var checkMessage = EAICommUtility.CheckEAIXTWAMessageFormat(receiveBytes);
                //if (!checkMessage.Equals(SystemConstantData.EAISystemCodeAndData.FormatCheckSuccess))
                //{
                //    var sendBytes = EAICommUtility.ConvertEAIXTWAIncoming2Outgoing(receiveBytes, checkMessage);
                //    Logger.Info($"Send: [{Encoding.ASCII.GetString(sendBytes)}] at {DateTime.Now:yyyy-MM-dd HH:mm:ss:fffff}. The server has send {sendBytes.Length} bytes.");
                //}
                string rspJsonFilePath = @"E:\FirstBank\Development\CyberCOLA\src\CyberCOLA\WpfFunctionTest1\Data\OASVRFY1_Rsp.json";
                string reqJsonFilePath = @"E:\FirstBank\Development\CyberCOLA\src\CyberCOLA\WpfFunctionTest1\Data\OASVRFY1_Req.json";
                string rspJsonString = File.ReadAllText(rspJsonFilePath);
                string reqJsonString = File.ReadAllText(reqJsonFilePath);
                SetMessageText(RichTextBox1, $"JSON Request[{reqJsonString}]", Brushes.Red);
                SetMessageText(RichTextBox1, $"JSON Response[{rspJsonString}]", Brushes.Red);
                var rspObject = JsonConvert.DeserializeObject<OASVRFY1.OASVRFY1_RSP>(rspJsonString);
                var reqObject = JsonConvert.DeserializeObject<OASVRFY1.OASVRFY1_REQ>(reqJsonString);

                //var rspConvertJsonString = JsonConvert.SerializeObject(rspObject, Formatting.Indented);
                //var reqConvertJsonString = JsonConvert.SerializeObject(reqObject, Formatting.Indented);
                //SetMessageText(RichTextBox1, $"Response Convert JSON String[{rspConvertJsonString}]", Brushes.Red);
                //SetMessageText(RichTextBox1, $"Request Convert JSON String[{reqConvertJsonString}]", Brushes.Red);
                ////
                //string[] returnStrings = new[] { "000", "111", "222", "333", "444", "555", "666" };
                //var fcb9gQResponseMessageJSON = new OASVRFY1.OASVRFY1_REQ();
                ////set up OASVRFY1_REQ
                //var reqJSONObject = new OASVRFY1.OASVRFY1_REQ();
                //reqJSONObject.TXN_ID = "OASVRFY1REQ_QRTN";
                //reqJSONObject.FN_CODE = "1";
                //reqJSONObject.TERMINAL_ID = "1234";
                //reqJSONObject.USER_ID = "1234";
                //var reqBody = new OASVRFY1.OASVRFY1_REQ_BODY();
                //reqBody.CreditCardNo = "4234567890123456";
                //reqBody.MerPAN = "1234567890123456";
                //reqBody.TxnSeqNo = "123456789012";
                //reqBody.TxnAmtNTD = 12.22M;
                //reqBody.MCC = "123456789012";
                //reqBody.CurCode = "123";
                //reqBody.OriTxnAmt = 123.00M;
                //reqBody.CountryCode = "12";
                //reqBody.MerName = "1234567890123456789012345";
                //reqBody.MerCity = "123456789012345";
                //reqBody.WalletAppInd = "12";
                //reqBody.ReserveField = "123456789012345678901234567980123456798012345678";
                //reqBody.QRLen = "132";
                //reqBody.QRMsg = "123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345";
                //reqJSONObject.BODY = reqBody;
                ////set up OASVRFY1_RSP
                //var rspJSONObject = new OASVRFY1.OASVRFY1_RSP();
                //rspJSONObject.FMH = "";
                //rspJSONObject.FN_CODE = "";
                //rspJSONObject.RESPONSE_CODE = "T000";
                //rspJSONObject.RESPONSE_DESCRIPTION = "成功";
                //rspJSONObject.TERMINAL_ID = "1234";
                //rspJSONObject.USER_ID = "1234";
                //var rspBody = new OASVRFY1.OASVRFY1_RSP_BODY();
                //var nowDateTime = DateTime.Now.ToString("yyyyMMddHHmmss");
                //rspBody.TxnDate = nowDateTime.Substring(0, 8);
                //rspBody.TxnTime = nowDateTime.Substring(8);
                //rspBody.CreditCardNo = "4234567890123456";
                //rspBody.MerPAN = "1234567890123456";
                //rspBody.QRTxnSeqNo = "123456789012";
                //rspBody.TxnAmtNTD = 12.22M;
                //rspBody.AuthorizationCode = "123456";
                //rspJSONObject.BODY = rspBody;
                //var reqJSONString = JsonConvert.SerializeObject(reqJSONObject, Formatting.Indented);
                //var rspJSONString = JsonConvert.SerializeObject(rspJSONObject, Formatting.Indented);
                var reqJSONString = JsonConvert.SerializeObject(reqObject, Newtonsoft.Json.Formatting.Indented);
                var rspJSONString = JsonConvert.SerializeObject(rspObject, Newtonsoft.Json.Formatting.Indented);
                SetMessageText(RichTextBox1, $"Request JSON String[{reqJSONString}]", Brushes.Red);
                SetMessageText(RichTextBox1, $"Response JSON String[{rspJSONString}]", Brushes.Red);
                var checkRequestMessageFormat = EAICommUtility.CheckOASVRFY1MessageFormat(reqObject);
                if (checkRequestMessageFormat != SystemConstantData.EAISystemCodeAndData.FormatCheckSuccess)
                {
                    SetMessageText(RichTextBox1, $"JASON Format Check Error", Brushes.Red);
                }
                var fiscMessage = EAICommUtility.ConvertOASVRFY1Message2FISC(reqObject);
                SetMessageText(RichTextBox1, $"BitMap[{HexStringConverter.FromByteArray(fiscMessage.Bitmap())}]", Brushes.Black);
                SetMessageText(RichTextBox1, $"MTI [{fiscMessage.Mti()}]", Brushes.Black);
                foreach (var pair in fiscMessage.Core().OrderedFields())
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    if (value is byte[] bytes)
                    {
                        var tempString = HexStringConverter.FromByteArray(bytes);
                        SetMessageText(RichTextBox1, $"FIELD [{key}] VALUE [{tempString}]", Brushes.Black);
                    }
                    else
                    {
                        SetMessageText(RichTextBox1, $"FIELD [{key}] Field Position [{FIELDMethods.Val(key)}] VALUE [{value}]", Brushes.Black);
                    }
                }
                FIELD fieldPos = FIELD.F_48;
                if (!ObjectUtils.IsNull(fiscMessage.RawVal(fieldPos)))
                {
                    var de48TagData = fiscMessage.RawVal(fieldPos).ToString();
                    var de48TagField = new DE48TagField(de48TagData);
                    foreach (var pair in de48TagField.GetSortedListDE48TagField())
                    {
                        var key = pair.Key;
                        var value = pair.Value;
                        if ((key.Equals("44")) || (key.Equals("72")))
                        {
                            Encoding enc = Encoding.GetEncoding("IBM037");
                            byte[] bytes = enc.GetBytes(value);
                            SetMessageText(RichTextBox1, $"Field48 [FIELD {key}] VALUE [{CommonUtils.HexBinary2HexString(bytes)}], ASCII[{Encoding.ASCII.GetString(bytes)}]", Brushes.Black);
                        }
                        else
                        {
                            SetMessageText(RichTextBox1, $"Field48 [FIELD {key}] VALUE [{value}]", Brushes.Black);
                        }
                    }
                }
                fieldPos = FIELD.F_58;
                if (!ObjectUtils.IsNull(fiscMessage.RawVal(fieldPos)))
                {
                    var de58TagData = fiscMessage.RawVal(fieldPos).ToString();
                    var de58TagField = new DE58TagField(de58TagData);
                    foreach (var pair in de58TagField.GetSortedListDE58TagField())
                    {
                        var key = pair.Key;
                        var value = pair.Value;
                        SetMessageText(RichTextBox1, $"Field58 [FIELD {key}] VALUE [{value}]", Brushes.Black);
                    }
                }
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox1, ex.Message, Brushes.Red);
            }
        }

        private void BtnGetCardType_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string runtimePath = AppDomain.CurrentDomain.BaseDirectory;
                string assemblyPath = $"{runtimePath}\\TM.dll";
                var assembly = Assembly.LoadFrom(assemblyPath);
                var typesCard = assembly.GetTypes()
                                        .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(CardType)))
                                        .ToList();
                foreach (var type in typesCard)
                {
                    var assemblyNmae = assembly.GetName().Name;
                    var instance = Activator.CreateInstance(type) as CardType;
                    if (instance == null) continue;
                    TypeMaps[type] = instance;
                    IdMaps[instance.Id()] = instance;
                }

                var typesToken = assembly.GetTypes()
                                         .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(Token)))
                                         .ToList();
                foreach (var type in typesToken)
                {
                    var instance = Activator.CreateInstance(type) as Token;
                    if (instance == null) continue;
                    TokenTypes[instance.Id] = type;
                }

                SetMessageText(RichTextBox1, $"TypesCard  Count [{typesCard.Count}]", Brushes.Black);
                SetMessageText(RichTextBox1, $"TypesToken Count [{typesToken.Count}]", Brushes.Black);
                var ncccMessageHexString = @"ISO0260000700100B238C48128E1801E000000000000000200003000000000210006170910020164831710010617061753110510606493817324147632200003503=25022011000035791680210270513996996        0101061977     NCCC TEST NAME        TAIPEI CITY  TW TW0270101061977                 901016ATSAATSA+000    019BK28000000000000000010105       388& 0000700388! 0400020             Y      1! C000026         105               ! C400012 00000040005 ! B200158 7FF90000808080008800D2BE53DAB8759DDB0000000021000000000000001C00002915890119061700021EE52D000706020A03A0000000000000000000000000000000000000000000000000000000! B300080 CF0081702651E020C8200000000000002200961E03000007A0000000031010202020202020202020! B400020 05151000      15100 100                                      41                                ST                          ";
                var nccc = new NcccIso8583(Encoding.ASCII.GetBytes(ncccMessageHexString));
                foreach (var pair in nccc.Core().OrderedFields())
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    if (value is byte[] bytes)
                    {
                        var tempString = HexStringConverter.FromByteArray(bytes);
                        SetMessageText(RichTextBox1, $"FIELD [{key}] VALUE [{tempString}]", Brushes.Black);
                    }
                    else
                    {
                        SetMessageText(RichTextBox1, $"FIELD [{key}] Field Position [{FIELDMethods.Val(key)}] VALUE [{value}]", Brushes.Black);
                    }
                }
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox1, ex.Message, Brushes.Red);
            }
        }

        private void Command6_Click(object sender, RoutedEventArgs e)
        {
            //- Ternary Operator(? :)
            //- Null Forgiving Operator(!.)
            //- Null Conditional Operator(?.)
            //- Null TeCoalescing Operator(??)
            //- Null Coalescing Assignment Operator(??=)

            //Ternary Operator
            int age = 19;
            string message = (age >= 18) ? "Adult" : "Minor";

            //Null Forgiving Operator
            string firstName = null; //Warning on right side
            string lastName = null!; //No Warning now on right side
            // The two values only illustrate the operator; discard them so the sample builds without CS0219.
            _ = firstName;
            _ = lastName;

            //Null Conditional Operator
            string address = null;
            int? length = address?.Length; //Output: null

            //Null Coalescing Operator
            int? a = null;
            int b = a ?? -1; // Output of b: -1

            //Null Coalescing Assignment Operator
            string fullName = null;
            if (fullName is null)
            {
                fullName = "No Name Found";
            }
            fullName ??= "No Name Found";
            //If and ??= both are doing same thing
        }

        private void BtnAESGCMEnyDey_Click(object sender, RoutedEventArgs e)
        {
            if (CheckBoxEnyDey.IsChecked == true)
            {
                var inputString = TextBoxDataInput.Text;
                var encryInputString = ColaCommUtility.EncryptAesGcm(inputString);
                SetMessageText(RichTextBox1, $"Input String[{inputString}] Encrypt String[{encryInputString}]", Brushes.Red);
            }
            else
            {
                var inputString = TextBoxDataInput.Text;
                var decryInputString = ColaCommUtility.DecryptAesGcm(inputString);
                SetMessageText(RichTextBox1, $"Input String[{inputString}] Decrypt String[{decryInputString}]", Brushes.Red);
            }
        }
        /// <summary>
        /// XMLs the string to object using JSON, Create JSON Property name must equal class name.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xmlString">The XML string.</param>
        /// <returns></returns>
        private T? XmlStringToObjectUsingJson<T>(string xmlString)
        {
            try
            {
                XElement xmlElement = XElement.Parse(xmlString);
                string jsonString = JsonConvert.SerializeXNode(xmlElement, Newtonsoft.Json.Formatting.None, omitRootObject: true);
                //var replaceString = jsonString.Replace("Header", "RequestHeader"); //replace JSON Property name
                if (string.IsNullOrEmpty(jsonString))
                {
                    return default(T);
                }
                T? classObject = JsonConvert.DeserializeObject<T>(jsonString);
                return classObject;
            }
            catch (Exception ex)
            {
                //Log.Fatal(ex, $"XmlStringToObjectUsingJson<T> Exception:{ex.Message}");
                SetMessageText(RichTextBox1, $"Input String[{xmlString}] Exception[{ex.Message}]", Brushes.Red);
                return default(T);
            }
        }

        private void BtnXml2Json2Class_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var vdMessage = new VDMessage.RequestXML();
                var header = new VDMessage.RequestHeader();
                header.TxID = "OAOEARMK";
                header.SystemKey = "FCB9G";
                header.TxSeqNo = "20230220000021";
                header.MsgSeqNo = "00001";
                header.CltTimeStamp = "2023 - 02 - 20 09:25:00.000";
                header.CustID = "00000000";
                header.UserID = "0000";
                header.AcctNo = "00000000000";
                header.MsgDirection = "RQ";
                vdMessage.RequestHeader = header;
                var txRq = new VDMessage.TxRq();
                txRq.FuncCode = "UCT7";
                txRq.AcctNo = "帳戶號碼";
                txRq.CardNo = "金融卡卡序號";
                txRq.CreditNo = "信用卡號";
                txRq.AuthNo = "授權碼";
                txRq.TxAmt = "1000.00";
                vdMessage.TxRq = txRq;
                var vdRequestString = ColaCommUtility.ObjectToXMLString<VDMessage.RequestXML>(vdMessage, true);
                SetMessageText(RichTextBox1, $"Object to XML String[{vdRequestString}]", Brushes.Black);
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox1, $"Convert Fail Exception:{ex.Message}", Brushes.Red);
            }
        }

        private void BtnGetServiceRouterLog_Click(object sender, RoutedEventArgs e)
        {
            var messageId = TextBoxDataInput.Text.Trim();
            GetServiceRouterLog(messageId);
        }

        private void GetServiceRouterLog(string messageIdString)
        {
            try
            {
                ServiceRouterLogInfoStruct serviceRouterLogInfo = new ServiceRouterLogInfoStruct();
                var retInt = CommonLib.DBUtils.StoreProcUtility.SelectServiceRouterLog(messageIdString, ref serviceRouterLogInfo);
                if (retInt == 0)
                {
                    SetMessageText(RichTextBox1, $"GetServiceRouterLog get data [{serviceRouterLogInfo.InterchangeData}]", Brushes.Red);
                }
                else
                {
                    SetMessageText(RichTextBox1, $"GetServiceRouterLog get data invalid[{retInt}]", Brushes.Red);
                }
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox1, ex.Message, Brushes.Red);
                Logger.Fatal(ex.Message);
                TextBlockMessage.Text = ex.Message;
            }

        }
    }
}

