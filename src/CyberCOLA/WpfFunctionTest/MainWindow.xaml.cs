
using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.NcccMessage;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.Interchange;
using Com.Cybersoft4u.ServiceProvider.EntryPoints.ClientMonitor;
using Com.Cybersoft4u.TM.FISC;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens;
using Com.Cybersoft4u.TM.ISO8583.SVCS;
using Com.Cybersoft4u.Utils;
using Com.Cybersoft4u.Utils.NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using Com.Cybersoft4u.CommonLib.TwinMessage;
using Com.Cybersoft4u.TM.FISC.DE48;
using Com.Cybersoft4u.TM.FISC.DE58;
using Com.Cybersoft4u.TM.ISO8583.Category.CardType;
using Com.Cybersoft4u.TM.ISO8583.ICASH;
using Com.Cybersoft4u.TM.ISO8583.IPASS;
using Com.Cybersoft4u.TM.ISO8583.TWIN;
using Application = System.Windows.Application;
using Cursors = System.Windows.Input.Cursors;
using MessageType = Com.Cybersoft4u.CommonLib.NcccMessage.MessageType;
//using MessageType = Com.Cybersoft4u.CommonLib.NcccMessage.MessageType;
using RichTextBox = System.Windows.Controls.RichTextBox;
using VISA = Com.Cybersoft4u.TM.VISA.VISA;

namespace Com.Cybersoft4u.Test.Wpf.WpfFunctionTest
{
    /// <summary>
    /// MainWindow.xaml的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly SingletonOfSMSServerInfo SingletonOfSMSServerInfo = SingletonOfSMSServerInfo.Instance;
        private static readonly SystemParameterInfo SystemParameterInfo = SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo();
        private delegate void UpdateRichTextBox(System.Windows.Controls.Control richTextBox, string message, Brush brush);
        private readonly Logger Logger = null;
        private ConcurrentDictionary<string, Com.Cybersoft4u.CommonLib.NcccMessage.MessageType> ncccMessageTypeInfo = null;
        private string _initialDirectory;
        private MethodInfo[] methodsInfo;
        private byte[] _getISOBytes;
        private static readonly Dictionary<Type, CardType> TypeMaps = new Dictionary<Type, CardType>();

        private static readonly Dictionary<string, CardType> IdMaps = new Dictionary<string, CardType>();

        public MainWindow()
        {
            try
            {
                LoggerAdapter.ReBuild(new NLoggerProvider(), "NLog.config");
                Logger = LoggerAdapter.GetLogger("LoggerHandler[WpfFunctionTest]");
                InitializeComponent();
                var dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                dispatcherTimer.Tick += DispatcherTimer_Tick;
                dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
                dispatcherTimer.Start();
                InitialSystemParameter();
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex);
            }
        }
        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            // Updating the Label which displays the current second
            TextBlockTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Forcing the CommandManager to raise the RequerySuggested event
            CommandManager.InvalidateRequerySuggested();
        }

        private void InitialSystemParameter()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Mouse.OverrideCursor = Cursors.Wait;
            LoggerAdapter.ReBuild(new NLoggerProvider(), "NLog.config");
            Logger.Info("MainWindow Start");
            try
            {
                ncccMessageTypeInfo = SingletonOfNcccMessageTypeMap.Instance.GetMessageTypeMap();
                foreach (var message in ncccMessageTypeInfo)
                {
                    string key = (String)message.Key;
                    MessageType messageType = (MessageType)message.Value;
                    //SetMessageText(RichTextBox1, $"MessageType{key}", Brushes.Red);
                    //messageType?.ResponseField.Field.ForEach(var => SetMessageText(RichTextBox1, $"{FIELDMethods.Val(Convert.ToInt32(var.Number)).ToString()}", Brushes.Red));
                }

                var configurationLoaderRoot = new ConfigurationLoaderRoot();
                _initialDirectory = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "InitialDirectory".Trim());
                TextBoxSelectFileName.Text = _initialDirectory;
                TextBoxSelectLocation.Text = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
                ComboBoxHSMFunction.SelectedIndex = 0;
                ComboBoxHSMFunction.Items.Clear();
                string hsmDllFilePath = $"{AppDomain.CurrentDomain.BaseDirectory}CommonLib.dll";
                SetMessageText(RichTextBox2, $"{hsmDllFilePath}", Brushes.Red);
                string typeName = @"Com.Cybersoft4u.CommonLib.FirstHSMUtils.FirstHSMUtility";
                Assembly assembly = Assembly.LoadFrom(hsmDllFilePath);
                Type[] typesInAssembly = assembly.GetExportedTypes();
                System.Type type = assembly.GetType(typeName);
                if (type != null)
                {
                    methodsInfo = type.GetMethods((BindingFlags.Public | BindingFlags.Static));
                    foreach (var methodInfo in methodsInfo)
                    {
                        if (methodInfo.Name.Contains("CheckFlow")) continue;
                        ComboBoxHSMFunction.Items.Add(methodInfo.Name);
                    }
                }
                else
                {
                    SetMessageText(RichTextBox2, "Library No Include HSM Function", Brushes.Red);
                }
                ComboBoxETicketFunction.Items.Clear();
                ComboBoxETicketFunction.Items.Add("ICash");
                ComboBoxETicketFunction.Items.Add("IPass");
                ComboBoxETicketFunction.Items.Add("SVCS");
                ComboBoxETicketFunction.SelectedIndex = 0;
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
                        int readDataLength = Convert.ToInt32(Encoding.ASCII.GetString(ColaCommUtility.GetSubBytes(fileBytes, offset, lengthDataLength)));

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

            openFileDialog1.InitialDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? @"E:";
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

        private void ComboBoxHSMFunction_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            //CreateCallHSMFunctionParameterField();
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
                var dataString = @"ISO0260000700200323AC48128E0808826000000000001230012281459243791892259241228013012281234567890120100506464105211234567890123456=20101234567890121234            12345678901234512345678901234567890121234567890123   121230052& 0000200052! S800030 T4234567890123456   2010      018BK77PRO20000000000";
                //var nccc2 = new NcccIso8583(byteArray);
                var nccc1 = new NcccIso8583(Encoding.Default.GetBytes(dataString));
                GetISOBytes();
                if (_getISOBytes is null || _getISOBytes.Length == 0)
                {
                    SetMessageText(RichTextBox1, $"BtnFunctionTest3_Click Input Data Error", Brushes.Red);
                    return;
                }

                VISA visa = new VISA(_getISOBytes);
                foreach (var pair in visa.Core().OrderedFields())
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

                return;
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

        private void BtnNccc_Click(object sender, RoutedEventArgs e)
        {
            //var messageHexString = @"ISO00600006005008222000000004000007F8780A000000001130901070000830113901000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000C000000000000000006456312";
            //var messageHexString = @"ISO0260000100200B238C48128E1800E000000000000000200003000000010000001270928000000191728000127012898769020006493817374038790000000053=1309101104802400000000000100108313996187        6619400169     NCCC FOR IC TESTING   CHINMEN-MATZUTW TW0276619400169         00010000901019BK17    000        010105       262& 0000500262! 0400020                   Y ! C000026         105       3       ! B400020 022500            0 ! C500144 6619400169      11                                ST                                      I                             06                      134000000                                                                                                            0000000000010010831 ";
            //var messageHexString = @"ISO006000060080082200000000008000400000000000120042002321901041500010000000000001620067B5300086CSM(MCL/KSM RCV/BC28 ORG/NCCC KD/65786D830508C10E885783DD70B61BBD CTP/00000000000ACB )";
            //var messageHexString = @"ISO02600007008008220000000000800040000000000012008041948450092500001000000000000162006157000086CSM(MCL/KSM RCV/BC39 ORG/NCCC KD/1C0BCB9A5EA62D474F00149CDB1A0524 CTP/0000000000028A )";
            //var messageHexString = @"ISO0260000100220B238C4812EE1800E000000020000000222003000000030000009070530212313431330200907090853119020006493817215588650006639109=16070000010030176918100013995009        6601000105     NCCC FOR IC TESTING   CHINMEN-MATZUTW TW0276601000105         00010000901019BK53UAT200000000000010105       262& 0000500262! 0400020                   Y ! B400020 022500            0 ! C000026         105       3   F   ! C500144 6601000105      62                                ST                                      E00                           030000103000001030000090000000000000000000000000000000000000000000134000000                                                                                                            0000000000010030171 ";
            //string hexString = @"49-53-4F-30-30-36-30-30-30-30-36-30-30-31-31-30-33-32-33-41-30-30-30-31-32-45-38-30-38-30-30-32-30-30-30-30-33-30-30-30-30-30-30-30-30-30-30-30-31-39-30-36-32-32-31-35-35-30-31-37-31-38-33-35-31-39-31-35-35-30-31-37-30-36-32-32-30-32-31-31-30-36-34-39-33-38-31-37-33-37-34-33-37-37-35-37-38-38-38-30-34-38-32-31-30-37-3D-31-33-30-39-31-30-31-31-30-34-38-30-32-34-30-30-30-30-30-30-31-37-30-36-32-32-31-38-33-35-31-39-34-30-38-30-34-33-30-30-31-33-39-39-36-31-38-37-20-20-20-20-20-20-20-20-39-30-31-30-37-38-26-20-30-30-30-30-33-30-30-30-37-38-21-20-30-34-30-30-30-32-30-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-59-20-21-20-43-30-30-30-30-32-36-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20";
            //string hexString = @"49-53-4F-30-30-36-30-30-30-30-36-30-30-38-30-30-38-32-32-30-30-30-30-30-30-30-30-30-30-30-30-30-30-34-30-30-30-30-30-30-30-30-30-30-30-30-30-30-30-39-32-38-30-35-35-35-32-31-30-32-31-33-33-39-33-30-31-03";
            //byte[] messageBytes = ColaCommUtility.HexString2HexBinary(hexString.Replace("-",""));
            try
            {
                byte[] messageBytes;
                if (CheckBoxParsingData.IsChecked == true)
                {
                    var parsingDataString = TextBoxParsingData.Text;
                    messageBytes = new byte[parsingDataString.Length];
                    Buffer.BlockCopy(Encoding.ASCII.GetBytes(parsingDataString), 0, messageBytes, 0, messageBytes.Length);
                }
                else
                {
                    GetISOBytes();
                    if (_getISOBytes is null || _getISOBytes.Length == 0)
                    {
                        SetMessageText(RichTextBox1, $"BtnNccc_Click Input Data Error", Brushes.Red);
                        return;
                    }
                    int dataLength;
                    if (CheckBoxInclude2ByteLength.IsChecked == true)
                        dataLength = 2;
                    else
                        dataLength = 0;
                    messageBytes = new byte[_getISOBytes.Length - dataLength];
                    Buffer.BlockCopy(_getISOBytes, dataLength, messageBytes, 0, messageBytes.Length);
                }
                NcccIso8583 nccc = new NcccIso8583(messageBytes);
                //nccc.ShowFields();
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
                return;


                //var nccc = new NcccIso8583(messageBytes);
                //var nccc = NcccTransCreator.SignOffRequest();
                var dataBytes = nccc.Encode().Val();
                byte[] sendBytes = new byte[dataBytes.Length + 2];
                sendBytes[0] = (byte)(dataBytes.Length >> 8);
                sendBytes[1] = (byte)dataBytes.Length;
                Buffer.BlockCopy(dataBytes, 0, sendBytes, 2, dataBytes.Length);
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
                return;
                //var fieldData = FIELD.F_35;
                //if (!ObjectUtils.IsNull(nccc.RawVal(fieldData)))
                //    new BicTrack2((string)nccc.Core().Field(fieldData));
                //var tokenGroup = (TokenGroup)nccc.Core().GetOthersValByKey("F_63");
                //if (tokenGroup == null) return;
                //foreach (var t in tokenGroup.Tokens)
                //{
                //    SetMessageText(RichTextBox1, $" TokenID[{t.Key}] Value[{t.Value.Encode()}]", Brushes.Black);
                //    switch (t.Key)
                //    {
                //        case NcccTransactionConst.PosAdditionalData.TokenB2.Id:
                //            var valueTokenB2 = (TokenB2)t.Value;
                //            SetMessageText(RichTextBox1, $" TokenB2 Value [{valueTokenB2.DataEncode()}]", Brushes.Black);
                //            break;
                //        case NcccTransactionConst.PosAdditionalData.TokenB3.Id:
                //            var valueTokenB3 = (TokenB3)t.Value;
                //            SetMessageText(RichTextBox1, $" TokenB3 Value [{valueTokenB3.DataEncode()}]", Brushes.Black);
                //            break;
                //        case NcccTransactionConst.PosAdditionalData.TokenB4.Id:
                //            var valueTokenB4 = (TokenB4)t.Value;
                //            SetMessageText(RichTextBox1, $" TokenB4 Value [{valueTokenB4.DataEncode()}]", Brushes.Black);
                //            break;
                //        case NcccTransactionConst.PosAdditionalData.TokenB5.Id:
                //            var valueTokenB5 = (TokenB5)t.Value;
                //            SetMessageText(RichTextBox1, $" TokenB5 Value [{valueTokenB5.DataEncode()}]", Brushes.Black);
                //            break;
                //        case NcccTransactionConst.PosAdditionalData.TokenB6.Id:
                //            var valueTokenB6 = (TokenB6)t.Value;
                //            SetMessageText(RichTextBox1, $" TokenB6 Value [{valueTokenB6.DataEncode()}]", Brushes.Black);
                //            break;
                //        case NcccTransactionConst.PosAdditionalData.Token04.Id:
                //            var valueToken04 = (Token04)t.Value;
                //            SetMessageText(RichTextBox1, $" Token04 Value [{valueToken04.DataEncode()}]", Brushes.Black);
                //            break;
                //        case NcccTransactionConst.PosAdditionalData.TokenC0.Id:
                //            var valueTokenC0 = (TokenC0)t.Value;
                //            SetMessageText(RichTextBox1, $" TokenC0 Value [{valueTokenC0.DataEncode()}]", Brushes.Black);
                //            break;
                //        case NcccTransactionConst.PosAdditionalData.TokenC5.Id:
                //            dynamic valueTokenC5;
                //            var c5 = t.Value as TokenRoyaltyC5;
                //            if (c5 != null)
                //            {
                //                valueTokenC5 = c5;
                //            }
                //            else
                //            {
                //                valueTokenC5 = (TokenInstallmentC5)t.Value;
                //                SetMessageText(RichTextBox1, $" TokenC5 Value [{((TokenInstallmentC5)valueTokenC5).Encode()}]", Brushes.Black);
                //            }
                //            break;
                //        case NcccTransactionConst.PosAdditionalData.TokenCE.Id:
                //            var valueTokenCE = (TokenCE)t.Value;
                //            SetMessageText(RichTextBox1, $" TokenCE Value [{valueTokenCE.DataEncode()}]", Brushes.Black);
                //            break;
                //        case NcccTransactionConst.PosAdditionalData.TokenU7.Id:
                //            var valueTokenU7 = (TokenU7)t.Value;
                //            SetMessageText(RichTextBox1, $" TokenU7 Value [{valueTokenU7.DataEncode()}]", Brushes.Black);
                //            break;
                //    }
                //    var fieldPos = FIELD.F_63;
                //    if (!ObjectUtils.IsNull(nccc.RawVal(fieldPos)))
                //    {
                //        SetMessageText(RichTextBox1, $" FIELD {fieldPos} Value [{nccc.RawVal(fieldPos).ToString()}]",
                //            Brushes.Black);
                //    }
                //}
                //var authStruct = new BicAuthInfoStruct(nccc);
                //SetMessageText(RichTextBox1, $"Pvv : {authStruct.isoP35Pvv}]",
                //            Brushes.Black);

                var NcccMessageTypeMapInfo = SingletonOfNcccMessageTypeMap.Instance.GetMessageTypeMap();
                Regex findPattern = new Regex("[cCmM]");
                NcccIso8583 responseNcccMessage = null;
                MessageType ncccMessageType = null;
                bool findFlag = NcccMessageTypeMapInfo.TryGetValue("0800", out ncccMessageType);
                responseNcccMessage = new NcccIso8583();
                var test = nccc.Header().Encode();
                responseNcccMessage.Header().Decode(test);
                responseNcccMessage.Mti(ncccMessageType.ResponseMessage);
                foreach (var message in ncccMessageType.ResponseField.Field)
                {
                    int fieldPos = Convert.ToInt32(message.Number);
                    if (fieldPos == 1) continue;
                    char respCondition = Convert.ToChar(message.DataCondition);
                    if (findPattern.IsMatch(Convert.ToString(respCondition)))
                    {
                        var field = FIELDMethods.Val(Convert.ToInt32(message.Number));
                        if (!ObjectUtils.IsNull(nccc.RawVal(field)))
                        {
                            responseNcccMessage.Core().Field(field, nccc.Core().Field(field));
                        }
                        else
                        {
                            switch (fieldPos)
                            {
                                case 15:
                                    //responseNcccMessage.Core().Field(field,ColaCommUtility.GetSettlementDate(SystemConstantData.CommonData.PasystfNCCCInfo));
                                    responseNcccMessage.Core().Field(field, "0123");
                                    break;
                                case 39:
                                    responseNcccMessage.Core().Field(field, "00");
                                    break;
                            }
                        }
                    }
                }
                var returnBytes = responseNcccMessage.Encode().Val();
                SetMessageText(RichTextBox1, Encoding.ASCII.GetString(returnBytes), Brushes.Red);
                return;
                nccc.Core().Field(FIELD.F_2, "1234123412341234");
                byte[] testBytes = nccc.Encode().Val();
                SetMessageText(RichTextBox1, Encoding.ASCII.GetString(testBytes), Brushes.Red);
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox1, ex.Message, Brushes.Red);
            }
        }

        private void BtnNcccToken_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                byte[] messageBytes;
                if (CheckBoxParsingData.IsChecked == true)
                {
                    var parsingDataString = TextBoxParsingData.Text;
                    messageBytes = new byte[parsingDataString.Length];
                    Buffer.BlockCopy(Encoding.ASCII.GetBytes(parsingDataString), 0, messageBytes, 0, messageBytes.Length);
                }
                else
                {
                    GetISOBytes();
                    if (_getISOBytes is null || _getISOBytes.Length == 0)
                    {
                        SetMessageText(RichTextBox1, $"BtnNcccToken_Click Input Data Error", Brushes.Red);
                        return;
                    }
                    int dataLength;
                    if (CheckBoxInclude2ByteLength.IsChecked == true)
                        dataLength = 2;
                    else
                        dataLength = 0;
                    messageBytes = new byte[_getISOBytes.Length - dataLength];
                    Buffer.BlockCopy(_getISOBytes, dataLength, messageBytes, 0, messageBytes.Length);
                }
                var nccc = new NcccIso8583(messageBytes);
                var tokenGroup = (TokenGroup)nccc.Core().GetOthersValByKey(nccc.GetTokenFieldId().ToString());
                if (tokenGroup == null) return;
                //tokenGroup.ShowTokens();
                foreach (KeyValuePair<string, Token> token in tokenGroup.Tokens)
                {
                    SetMessageText(RichTextBox1, $"[Token {token.Key}] = [{token.Value.Encode()}]", Brushes.Red);
                }
                var tokenB2 = (TokenB2)tokenGroup.GetToken(NcccTransactionConst.PosAdditionalData.TokenB2.Id);
                if (tokenB2 == null)
                {
                    SetMessageText(RichTextBox1, "No Token B2 Data", Brushes.Red);
                }
                else
                {
                    const int tokenHeaderLength = 10;
                    string tokenB2HeaderData = tokenB2.Encode();
                    string tokenB2Header = tokenB2HeaderData.Substring(0, tokenHeaderLength);
                    string tokenB2Data = tokenB2HeaderData.Substring(tokenHeaderLength, tokenB2HeaderData.Length - tokenHeaderLength);
                    SetMessageText(RichTextBox1, $"Header : {tokenB2Header}", Brushes.Red);
                    SetMessageText(RichTextBox1, $"Data   : {tokenB2Data}", Brushes.Red);
                }
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox1, ex.Message, Brushes.Red);
            }

        }

        private void BtnNcccAuthFlow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                byte[] messageBytes;
                byte[] ncccRequestRawData;
                if (CheckBoxParsingData.IsChecked == true)
                {
                    var parsingDataString = TextBoxParsingData.Text;
                    messageBytes = new byte[parsingDataString.Length];
                    Buffer.BlockCopy(Encoding.ASCII.GetBytes(parsingDataString), 0, messageBytes, 0, messageBytes.Length);
                    ncccRequestRawData = ColaCommUtility.ContactBytesArray(
                                         ColaCommUtility.IntegerToBinary(messageBytes.Length),
                                         messageBytes);
                }
                else
                {
                    GetISOBytes();
                    if (_getISOBytes is null || _getISOBytes.Length == 0)
                    {
                        SetMessageText(RichTextBox1, $"BtnNcccAuthFlow_Click Input Data Error", Brushes.Red);
                        return;
                    }
                    int dataLength;
                    if (CheckBoxInclude2ByteLength.IsChecked == true)
                    {
                        dataLength = 2;
                        messageBytes = new byte[_getISOBytes.Length - dataLength];
                        Buffer.BlockCopy(_getISOBytes, dataLength, messageBytes, 0, messageBytes.Length);
                        ncccRequestRawData = new byte[messageBytes.Length];
                        Buffer.BlockCopy(messageBytes, 0, ncccRequestRawData, 0, ncccRequestRawData.Length);
                    }
                    else
                    {
                        dataLength = 0;
                        messageBytes = new byte[_getISOBytes.Length - dataLength];
                        Buffer.BlockCopy(_getISOBytes, dataLength, messageBytes, 0, messageBytes.Length);
                        ncccRequestRawData = ColaCommUtility.ContactBytesArray(
                                             ColaCommUtility.IntegerToBinary(messageBytes.Length),
                                             messageBytes);
                    }
                }

                NcccIso8583 ncccRequestMessage = new NcccIso8583(messageBytes);
                SetMessageText(RichTextBox1, "--- Request Message ---", Brushes.Black);
                SetMessageText(RichTextBox1, $"Header[{ncccRequestMessage.Header().Encode()}]", Brushes.Black);
                SetMessageText(RichTextBox1, $"BitMap[{HexStringConverter.FromByteArray(ncccRequestMessage.Bitmap())}]", Brushes.Black);
                SetMessageText(RichTextBox1, $"MTI [{ncccRequestMessage.Mti()}]", Brushes.Black);
                foreach (var pair in ncccRequestMessage.Core().OrderedFields())
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
                SetMessageText(RichTextBox1, $"Original Data[{ColaCommUtility.GetDisplayForRawData(ncccRequestRawData)}],Length[{ncccRequestRawData.Length - 2}]", Brushes.Red);
                SetMessageText(RichTextBox1, $"Add 2 Bytes,Hex Data Length[{ncccRequestRawData[0]:X02}-{ncccRequestRawData[1]:X02}]", Brushes.Red);
                byte[] retnProcessBytes = NcccAuthFlowUtility.ProcessTransactionFlow(ncccRequestMessage, "OnlineSession", string.Empty);
                if (retnProcessBytes != null)
                    SetMessageText(RichTextBox1, System.Text.Encoding.ASCII.GetString(retnProcessBytes), Brushes.Red);
                else
                {
                    SetMessageText(RichTextBox1, "Return Null Data", Brushes.Red);
                    return;
                }
                byte[] ncccResponseBytes = new byte[retnProcessBytes.Length - 2];
                Array.Copy(retnProcessBytes, 2, ncccResponseBytes, 0, retnProcessBytes.Length - 2);
                var ncccResponseMessage = new NcccIso8583(ncccResponseBytes);
                SetMessageText(RichTextBox1, "--- Response Message ---", Brushes.Black);
                SetMessageText(RichTextBox1, $"Return NcccIsoData(No Binary Length) [{Encoding.ASCII.GetString(ncccResponseBytes)}]", Brushes.Red);
                SetMessageText(RichTextBox1, $"Header[{ncccResponseMessage.Header().Encode()}]", Brushes.Black);
                SetMessageText(RichTextBox1, $"BitMap[{HexStringConverter.FromByteArray(ncccResponseMessage.Bitmap())}]", Brushes.Black);
                SetMessageText(RichTextBox1, $"MTI [{ncccResponseMessage.Mti()}]", Brushes.Black);
                foreach (var pair in ncccResponseMessage.Core().OrderedFields())
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

        private void BtnFisc_Click(object sender, RoutedEventArgs e)
        {
            //var messageHexString = @"ISO00600006005008222000000004000007F8780A000000001130901070000830113901000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000C000000000000000006456312";
            //var messageHexString = @"ISO0260000100200B238C48128E1800E000000000000000200003000000010000001270928000000191728000127012898769020006493817374038790000000053=1309101104802400000000000100108313996187        6619400169     NCCC FOR IC TESTING   CHINMEN-MATZUTW TW0276619400169         00010000901019BK17    000        010105       262& 0000500262! 0400020                   Y ! C000026         105       3       ! B400020 022500            0 ! C500144 6619400169      11                                ST                                      I                             06                      134000000                                                                                                            0000000000010010831 ";
            //var messageHexString = @"ISO006000060080082200000000008000400000000000120042002321901041500010000000000001620067B5300086CSM(MCL/KSM RCV/BC28 ORG/NCCC KD/65786D830508C10E885783DD70B61BBD CTP/00000000000ACB )";
            //var messageHexString = @"ISO02600007008008220000000000800040000000000012008041948450092500001000000000000162006157000086CSM(MCL/KSM RCV/BC39 ORG/NCCC KD/1C0BCB9A5EA62D474F00149CDB1A0524 CTP/0000000000028A )";
            //var messageHexString = @"ISO0260000100220B238C4812EE1800E000000020000000222003000000030000009070530212313431330200907090853119020006493817215588650006639109=16070000010030176918100013995009        6601000105     NCCC FOR IC TESTING   CHINMEN-MATZUTW TW0276601000105         00010000901019BK53UAT200000000000010105       262& 0000500262! 0400020                   Y ! B400020 022500            0 ! C000026         105       3   F   ! C500144 6601000105      62                                ST                                      E00                           030000103000001030000090000000000000000000000000000000000000000000134000000                                                                                                            0000000000010030171 ";
            //string hexString = @"49-53-4F-30-30-36-30-30-30-30-36-30-30-31-31-30-33-32-33-41-30-30-30-31-32-45-38-30-38-30-30-32-30-30-30-30-33-30-30-30-30-30-30-30-30-30-30-30-31-39-30-36-32-32-31-35-35-30-31-37-31-38-33-35-31-39-31-35-35-30-31-37-30-36-32-32-30-32-31-31-30-36-34-39-33-38-31-37-33-37-34-33-37-37-35-37-38-38-38-30-34-38-32-31-30-37-3D-31-33-30-39-31-30-31-31-30-34-38-30-32-34-30-30-30-30-30-30-31-37-30-36-32-32-31-38-33-35-31-39-34-30-38-30-34-33-30-30-31-33-39-39-36-31-38-37-20-20-20-20-20-20-20-20-39-30-31-30-37-38-26-20-30-30-30-30-33-30-30-30-37-38-21-20-30-34-30-30-30-32-30-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-59-20-21-20-43-30-30-30-30-32-36-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20";
            //string hexString = @"49-53-4F-30-30-36-30-30-30-30-36-30-30-38-30-30-38-32-32-30-30-30-30-30-30-30-30-30-30-30-30-30-30-34-30-30-30-30-30-30-30-30-30-30-30-30-30-30-30-39-32-38-30-35-35-35-32-31-30-32-31-33-33-39-33-30-31-03";
            //byte[] messageBytes = ColaCommUtility.HexString2HexBinary(hexString.Replace("-",""));
            string ebcdicString = @"F0F1F0F0F67E640188E1A0480000000000000010F1F6F4F6F9F3F7F0F2F7F0F0F0F0F2F7F1F5F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F9F7F0F0F0F0F0F0F0F0F0F0F0F0F0F2F0F7F0F3F3F6F2F1F6F1F0F0F0F0F0F0F0F0F6F5F4F5F1F1F3F6F2F1F0F2F0F7F2F1F0F8F0F2F0F7F5F9F6F9F8F4F0F0F1F2F0F6F0F1F5F6F1F1F0F6F0F2F2F0F2F0F2F4F0F2F0F7F6F9F0F9F1F7F1F2F3F4F5F6F7F8C3C1D9C440C1C3C3D7E340C9C4C340D6A388859940D481999285A38995874040404040404040E5858781A2404040404040404040D5E540F0F5F7E3F2F3F0F2F0F1F2F6F0F3F2F1F6F3F3F2F6F0F1F0F1C8F0F6F1F1F0F1F2F3F4F5F6F7F8F9F0F0F8F0F2F0F3F8F2F0F2F5F2F9F2F0F3F7F9F2F8F4F0F8F4F0F0F7F3F7F3F0F4D4E3C1D9F6F0F1F5D4C3C3F0F1F1F0C4C1F0F2F2F74040F3F1F4F2F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F14BF0F0F0F0F0F0F0F0F0F0F0F0F2F94BF9F6F0F0F0F0F0F0F1F1F9F9F3F5F1F0F3F0F9F0F0F1F9F5E3C1F1F2F3F4F5F6F7F8F9F0F1F2F3F4F1F1F2F3F4F5F6F7F8F9F0C1C2C3C4C5C6C7C8C9D1F1F2F3F4F5F6F7F8F9F0C1C2C3C4C5C6C7C8C9D14040404040404040F1F2F3F4F5C1C2C3C4C54040F1F2F3F4F5F6F7F8F9F0F1F2F3F4F5F6C1C2C3C4C5C6C7C8C9D1D2D3D4D5D6D74040404040404040404040404040404040404040404040404040404040404040D3C1E2E3D5C1D4C561C6C9D9E2E3D5C1D4C5404040404040404040F1F9F9F5F4F0F0F1F2F3F4F5F6F1C1F2C2F3C3F4C4F1F1F2F3F4C3";
            //string ebcdicString = @"F0F4F2F0F23C040108C080480000004200000000F1F6F4F6F8F8F2F8F8F7F1F0F8F0F9F2F9F6F0F0F0F0F0F0F0F0F0F0F0F0F0F4F6F0F0F0F0F1F2F4F0F1F5F1F1F3F5F3F3F1F6F4F0F9F5F1F1F3F0F1F2F4F2F6F1F1F8F1F2F0F6F4F5F5F7F4F2F4F0F2F4F3F5F4F7F3F1F9F4F0F0F0F0F0F0F0F0F9F8F1F0F0F0F4F0F5F1F0F0F3F1F7F9F0F1F0F1F3F3F3F0F3F0F0F6F6F8F0F2F0F4F0F2F6F9F9F9F5F1F9F9F9F9F6F6F0F0F1F5F8F1F1F440404040404040F0F1F0F0F5F3F3F1F6F4F0F1F2F4F0F1F5F1F1F3F0F0F0F0F0F4F9F0F7F0F6F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0";
            byte[] messageBytes = ColaCommUtility.HexString2HexBinary(ebcdicString);
            try
            {
                //byte[] messageBytes;
                //if (CheckBoxParsingData.IsChecked == true)
                //{
                //    var parsingDataString = TextBoxParsingData.Text;
                //    if (CheckBoxFiscData.IsChecked == true)
                //    {
                //        messageBytes = ColaCommUtility.HexString2HexBinary(parsingDataString);
                //    }
                //    else
                //    {
                //        messageBytes = new byte[parsingDataString.Length];
                //        Buffer.BlockCopy(Encoding.ASCII.GetBytes(parsingDataString), 0, messageBytes, 0, messageBytes.Length);
                //    }
                //}
                //else
                //{
                //    GetISOBytes();
                //    if (_getISOBytes is null || _getISOBytes.Length == 0)
                //    {
                //        SetMessageText(RichTextBox1, $"BtnFisc_Click Input Data Error", Brushes.Red);
                //        return;
                //    }
                //    int dataLength;
                //    if (CheckBoxInclude2ByteLength.IsChecked == true)
                //        dataLength = 2;
                //    else
                //        dataLength = 0;
                //    messageBytes = new byte[_getISOBytes.Length - dataLength];
                //    Buffer.BlockCopy(_getISOBytes, dataLength, messageBytes, 0, messageBytes.Length);
                //}
                FISC fisc = new FISC(messageBytes);
                //fisc.ShowFields();
                SetMessageText(RichTextBox1, $"BitMap[{HexStringConverter.FromByteArray(fisc.Bitmap())}]", Brushes.Black);
                SetMessageText(RichTextBox1, $"MTI [{fisc.Mti()}]", Brushes.Black);
                foreach (var pair in fisc.Core().OrderedFields())
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
                FiscAuthInfoStruct authInfoData = new FiscAuthInfoStruct(fisc);
                string[] authReturnString = new string[] { "123456", "00" };
                var tagIdData = ColaCommUtility.CreateFisc0100P48Data(fisc,
                                                                      authInfoData,
                                                                      authReturnString);
                return;

                var dataBytes = fisc.Encode().Val();
                byte[] sendBytes = new byte[dataBytes.Length + 2];
                sendBytes[0] = (byte)(dataBytes.Length >> 8);
                sendBytes[1] = (byte)dataBytes.Length;
                Buffer.BlockCopy(dataBytes, 0, sendBytes, 2, dataBytes.Length);
                SetMessageText(RichTextBox1, $"BitMap[{HexStringConverter.FromByteArray(fisc.Bitmap())}]", Brushes.Black);
                SetMessageText(RichTextBox1, $"MTI [{fisc.Mti()}]", Brushes.Black);
                foreach (var pair in fisc.Core().OrderedFields())
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
                return;

                var FiscMessageTypeMapInfo = SingletonOfFiscMessageTypeMap.Instance.GetMessageTypeMap();
                Regex findPattern = new Regex("[cCmM]");
                FISC responseFiscMessage = null;
                Com.Cybersoft4u.CommonLib.FiscMessage.MessageType fiscMessageType = null;
                bool findFlag = FiscMessageTypeMapInfo.TryGetValue("0800", out fiscMessageType);
                responseFiscMessage = new FISC();
                responseFiscMessage.Mti(fiscMessageType.ResponseMessage);
                foreach (var message in fiscMessageType.ResponseField.Field)
                {
                    int fieldPos = Convert.ToInt32(message.Number);
                    if (fieldPos == 1) continue;
                    char respCondition = Convert.ToChar(message.DataCondition);
                    if (findPattern.IsMatch(Convert.ToString(respCondition)))
                    {
                        var field = FIELDMethods.Val(Convert.ToInt32(message.Number));
                        if (!ObjectUtils.IsNull(fisc.RawVal(field)))
                        {
                            responseFiscMessage.Core().Field(field, fisc.Core().Field(field));
                        }
                        else
                        {
                            switch (fieldPos)
                            {
                                case 15:
                                    //responseNcccMessage.Core().Field(field,ColaCommUtility.GetSettlementDate(SystemConstantData.CommonData.PasystfNCCCInfo));
                                    responseFiscMessage.Core().Field(field, "0123");
                                    break;
                                case 39:
                                    responseFiscMessage.Core().Field(field, "00");
                                    break;
                            }
                        }
                    }
                }
                var returnBytes = responseFiscMessage.Encode().Val();
                SetMessageText(RichTextBox1, Encoding.ASCII.GetString(returnBytes), Brushes.Red);
                return;
                fisc.Core().Field(FIELD.F_2, "1234123412341234");
                byte[] testBytes = fisc.Encode().Val();
                SetMessageText(RichTextBox1, Encoding.ASCII.GetString(testBytes), Brushes.Red);
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox1, ex.Message, Brushes.Red);
            }
        }
        private void BtnFiscToken_Click(object sender, RoutedEventArgs e)
        {
            //var messageHexString = @"f0f4f2f0feeb14010a21e0160000004000000000f1f6f5f2f4f0f9f5f0f0f0f0f0f0f0f5f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f0f1f5f0f0f0f0f0f0f0f0f0f0f1f5f0f0f0f0f0f0f0f0f0f4f7f1f3f2f0f6f0f1f1f4f4f0f0f0f6f1f0f0f0f0f0f0f5f3f1f4f2f1f0f0f7f2f2f2f0f5f0f9f2f9f0f9f2f9f0f9f2f8f1f5f8f9f0f0f0f6f0f0f9f6f7f4f3f3f9f0f5f1f5f8f0f0f0f2f8f2c6c9d9e2e340e3c8c9d9c440c2c1d5d240404040404040c2c1d9c3c5d3d6d5c14040404040c5e2d7f0f2f5d9f2f0f0f1d7f6f3f1f5d4d7d3d2d9d3c2d8e6f0f9f2f94040f8f4f0f8f4f0f9f0f1f0f0f3f4f0f2f0f1f5d4c3c2d5f6f2f0f0f6f0f0f0f0f0f0f0f0f9d4d7d3d2d9d3c2d8e6f0f1f0f0f7f2f2f2f0f5f0f6f0f1f1f4f4f0f0f0f0f0f0f0f0f0f0f9f6f7f4f0f0f0f0f0f0f0f0f0f0f0";
            //var messageHexString = @"F0F1F0F0767C640188E1A008F1F6F4F6F7F8F9F2F5F5F0F5F4F7F9F0F5F6F0F0F0F0F0F0F0F0F0F0F0F0F0F1F0F0F0F0F0F0F0F0F0F0F0F1F0F0F0F0F0F6F0F6F0F5F3F0F3F0F6F1F0F0F0F0F0F0F9F6F4F5F4F7F1F3F3F0F3F0F0F6F0F6F2F6F0F8F5F4F9F9F1F5F8F8F1F2F0F6F4F0F4F7F7F7F0F6F0F0F3F7F4F1F3F1F5F7F4F8F6F3F0F0F6F9F9F0F0F1F0F0F0F1F0F0F7F0F4F2F6F6F2F4F0F9F0F0F1E3C5E2E3404040404040404040404040404040404040404040E3818997858940C389A3A84040E3E6F0F4F3E3F4F4F2F032303233303630363133323935352E3437352D31F9F2F0F3F7F7F7F4F2F0F7F0F1F0F3F2F1F0F9F0F1F9F0F1F0F2F6F9F9F9F5F1F9F9F9F9F6F6F0F0F1F5F8F1F1F040404040404040";
            //var messageBytes = HexStringConverter.ToByteArray(messageHexString);
            GetISOBytes();
            if (_getISOBytes is null || _getISOBytes.Length == 0)
            {
                SetMessageText(RichTextBox1, $"BtnMasterCard_Click_Click Input Data Error", Brushes.Red);
                return;
            }
            int dataLength;
            if (CheckBoxInclude2ByteLength.IsChecked == true)
                dataLength = 2;
            else
                dataLength = 0;
            var messageBytes = new byte[_getISOBytes.Length - dataLength];
            Buffer.BlockCopy(_getISOBytes, dataLength, messageBytes, 0, messageBytes.Length);
            try
            {
                var fiscMessage = new FISC(messageBytes);
                foreach (var pair in fiscMessage.Core().Fields())
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    SetMessageText(RichTextBox1, $"[FIELD {key}] VALUE [{value}]", Brushes.Black);
                }

                FIELD fieldPos = FIELD.F_48;
                if (!ObjectUtils.IsNull(fiscMessage.RawVal(fieldPos)))
                {
                    SetMessageText(RichTextBox1, $"[FIELD {FIELDMethods.Val(fieldPos)}] Tag Data", Brushes.Black);
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
                var returnBytes = fiscMessage.Encode().Val();
                SetMessageText(RichTextBox1, $"Original Bytes Data[{CommonUtils.HexBinary2HexString(returnBytes)}]", Brushes.Black);
                return;

                fieldPos = FIELD.F_55;
                if (!ObjectUtils.IsNull(fiscMessage.RawVal(fieldPos)))
                {
                    SetMessageText(RichTextBox1, $"[FIELD {FIELDMethods.Val(fieldPos)}] Tag Data", Brushes.Black);
                    var emvData = fiscMessage.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags;
                    foreach (var pair in emvData)
                    {
                        var key = pair.Key;
                        var tag = pair.Value;
                        SetMessageText(RichTextBox1, $"Field55 [FIELD {key}] VALUE [{CommonUtils.HexBinary2HexString(tag.Value)}]", Brushes.Black);
                    }

                }

                fieldPos = FIELD.F_58;
                if (!ObjectUtils.IsNull(fiscMessage.RawVal(fieldPos)))
                {
                    SetMessageText(RichTextBox1, $"[FIELD {FIELDMethods.Val(fieldPos)}] Tag Data", Brushes.Black);
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

        private void BtnFiscAuthFlow_Click(object sender, RoutedEventArgs e)
        {
            string ebcdicString = @"F0F1F0F0F67E640188E1A0480000000000000010F1F6F4F6F9F3F7F0F2F7F0F0F0F0F2F7F1F5F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F9F7F0F0F0F0F0F0F0F0F0F0F0F0F0F2F0F7F0F3F3F6F2F1F6F1F0F0F0F0F0F0F0F0F6F5F4F5F1F1F3F6F2F1F0F2F0F7F2F1F0F8F0F2F0F7F5F9F6F9F8F4F0F0F1F2F0F6F0F1F5F6F1F1F0F6F0F2F2F0F2F0F2F4F0F2F0F7F6F9F0F9F1F7F1F2F3F4F5F6F7F8C3C1D9C440C1C3C3D7E340C9C4C340D6A388859940D481999285A38995874040404040404040E5858781A2404040404040404040D5E540F0F5F7E3F2F3F0F2F0F1F2F6F0F3F2F1F6F3F3F2F6F0F1F0F1C8F0F6F1F1F0F1F2F3F4F5F6F7F8F9F0F0F8F0F2F0F3F8F2F0F2F5F2F9F2F0F3F7F9F2F8F4F0F8F4F0F0F7F3F7F3F0F4D4E3C1D9F6F0F1F5D4C3C3F0F1F1F0C4C1F0F2F2F74040F3F1F4F2F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F0F14BF0F0F0F0F0F0F0F0F0F0F0F0F2F94BF9F6F0F0F0F0F0F0F1F1F9F9F3F5F1F0F3F0F9F0F0F1F9F5E3C1F1F2F3F4F5F6F7F8F9F0F1F2F3F4F1F1F2F3F4F5F6F7F8F9F0C1C2C3C4C5C6C7C8C9D1F1F2F3F4F5F6F7F8F9F0C1C2C3C4C5C6C7C8C9D14040404040404040F1F2F3F4F5C1C2C3C4C54040F1F2F3F4F5F6F7F8F9F0F1F2F3F4F5F6C1C2C3C4C5C6C7C8C9D1D2D3D4D5D6D74040404040404040404040404040404040404040404040404040404040404040D3C1E2E3D5C1D4C561C6C9D9E2E3D5C1D4C5404040404040404040F1F9F9F5F4F0F0F1F2F3F4F5F6F1C1F2C2F3C3F4C4F1F1F2F3F4C3";
            byte[] messageBytes = ColaCommUtility.HexString2HexBinary(ebcdicString);
            //GetISOBytes();
            //if (_getISOBytes is null || _getISOBytes.Length == 0)
            //{
            //    SetMessageText(RichTextBox1, $"BtnMasterCard_Click_Click Input Data Error", Brushes.Red);
            //    return;
            //}
            //int dataLength;
            //if (CheckBoxInclude2ByteLength.IsChecked == true)
            //    dataLength = 2;
            //else
            //    dataLength = 0;
            //var messageBytes = new byte[_getISOBytes.Length - dataLength];
            //Buffer.BlockCopy(_getISOBytes, dataLength, messageBytes, 0, messageBytes.Length);
            try
            {
                string tempString;
                var fiscRequestMessage = new FISC(messageBytes);
                SetMessageText(RichTextBox1, "--- Request Message ---", Brushes.Black);
                SetMessageText(RichTextBox1, $"MTI [{fiscRequestMessage.Mti()}]", Brushes.Black);
                foreach (var pair in fiscRequestMessage.Core().OrderedFields())
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    if (value is byte[] bytes)
                    {
                        tempString = HexStringConverter.FromByteArray(bytes);
                        SetMessageText(RichTextBox1, $"[FIELD {key}] VALUE [{tempString}]", Brushes.Black);
                    }
                    else
                    {
                        SetMessageText(RichTextBox1, $"[FIELD {key}] VALUE [{value}]", Brushes.Black);
                    }
                }
                // get token data
                FIELD fieldPos = FIELD.F_48;
                if (!ObjectUtils.IsNull(fiscRequestMessage.RawVal(fieldPos)))
                {
                    SetMessageText(RichTextBox1, $"[FIELD {FIELDMethods.Val(fieldPos)}] Tag Data", Brushes.Black);
                    var de48TagData = fiscRequestMessage.RawVal(fieldPos).ToString();
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
                fieldPos = FIELD.F_55;
                if (!ObjectUtils.IsNull(fiscRequestMessage.RawVal(fieldPos)))
                {
                    SetMessageText(RichTextBox1, $"[FIELD {FIELDMethods.Val(fieldPos)}] Tag Data", Brushes.Black);
                    var emvData = fiscRequestMessage.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags;
                    foreach (var pair in emvData)
                    {
                        var key = pair.Key;
                        var tag = pair.Value;
                        SetMessageText(RichTextBox1, $"Field55 [FIELD {key}] VALUE [{CommonUtils.HexBinary2HexString(tag.Value)}]", Brushes.Black);
                    }

                }
                fieldPos = FIELD.F_58;
                if (!ObjectUtils.IsNull(fiscRequestMessage.RawVal(fieldPos)))
                {
                    SetMessageText(RichTextBox1, $"[FIELD {FIELDMethods.Val(fieldPos)}] Tag Data", Brushes.Black);
                    var de58TagData = fiscRequestMessage.RawVal(fieldPos).ToString();
                    var de58TagField = new DE58TagField(de58TagData);
                    foreach (var pair in de58TagField.GetSortedListDE58TagField())
                    {
                        var key = pair.Key;
                        var value = pair.Value;
                        SetMessageText(RichTextBox1, $"Field58 [FIELD {key}] VALUE [{value}]", Brushes.Black);
                    }
                }
                // process transaction flow
                byte[] retnProcessBytes = FiscAuthFlowUtility.ProcessTransactionFlow(fiscRequestMessage, "OnlineSession", string.Empty);
                if (retnProcessBytes != null)
                    SetMessageText(RichTextBox1, Convert.ToHexString(retnProcessBytes), Brushes.Red);
                else
                {
                    SetMessageText(RichTextBox1, "Return Null Data", Brushes.Red);
                    return;
                }
                byte[] fiscResponseBytes = new byte[retnProcessBytes.Length - 2];
                Array.Copy(retnProcessBytes, 2, fiscResponseBytes, 0, retnProcessBytes.Length - 2);
                var fiscResponseMessage = new FISC(fiscResponseBytes);
                SetMessageText(RichTextBox1, "--- Response Message ---", Brushes.Black);
                SetMessageText(RichTextBox1, $"MTI [{fiscResponseMessage.Mti()}]", Brushes.Black);
                foreach (var pair in fiscResponseMessage.Core().OrderedFields())
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    if (value is byte[] bytes)
                    {
                        tempString = HexStringConverter.FromByteArray(bytes);
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

        private void BtnETicket_Click(object sender, RoutedEventArgs e)
        {
            //var messageHexString = @"ISO00600006005008222000000004000007F8780A000000001130901070000830113901000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000C000000000000000006456312";
            //var messageHexString = @"ISO0260000100200B238C48128E1800E000000000000000200003000000010000001270928000000191728000127012898769020006493817374038790000000053=1309101104802400000000000100108313996187        6619400169     NCCC FOR IC TESTING   CHINMEN-MATZUTW TW0276619400169         00010000901019BK17    000        010105       262& 0000500262! 0400020                   Y ! C000026         105       3       ! B400020 022500            0 ! C500144 6619400169      11                                ST                                      I                             06                      134000000                                                                                                            0000000000010010831 ";
            //var messageHexString = @"ISO006000060080082200000000008000400000000000120042002321901041500010000000000001620067B5300086CSM(MCL/KSM RCV/BC28 ORG/NCCC KD/65786D830508C10E885783DD70B61BBD CTP/00000000000ACB )";
            //var messageHexString = @"ISO02600007008008220000000000800040000000000012008041948450092500001000000000000162006157000086CSM(MCL/KSM RCV/BC39 ORG/NCCC KD/1C0BCB9A5EA62D474F00149CDB1A0524 CTP/0000000000028A )";
            //var messageHexString = @"ISO0260000100220B238C4812EE1800E000000020000000222003000000030000009070530212313431330200907090853119020006493817215588650006639109=16070000010030176918100013995009        6601000105     NCCC FOR IC TESTING   CHINMEN-MATZUTW TW0276601000105         00010000901019BK53UAT200000000000010105       262& 0000500262! 0400020                   Y ! B400020 022500            0 ! C000026         105       3   F   ! C500144 6601000105      62                                ST                                      E00                           030000103000001030000090000000000000000000000000000000000000000000134000000                                                                                                            0000000000010030171 ";
            //string hexString = @"49-53-4F-30-30-36-30-30-30-30-36-30-30-31-31-30-33-32-33-41-30-30-30-31-32-45-38-30-38-30-30-32-30-30-30-30-33-30-30-30-30-30-30-30-30-30-30-30-31-39-30-36-32-32-31-35-35-30-31-37-31-38-33-35-31-39-31-35-35-30-31-37-30-36-32-32-30-32-31-31-30-36-34-39-33-38-31-37-33-37-34-33-37-37-35-37-38-38-38-30-34-38-32-31-30-37-3D-31-33-30-39-31-30-31-31-30-34-38-30-32-34-30-30-30-30-30-30-31-37-30-36-32-32-31-38-33-35-31-39-34-30-38-30-34-33-30-30-31-33-39-39-36-31-38-37-20-20-20-20-20-20-20-20-39-30-31-30-37-38-26-20-30-30-30-30-33-30-30-30-37-38-21-20-30-34-30-30-30-32-30-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-59-20-21-20-43-30-30-30-30-32-36-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20";
            //string hexString = @"49-53-4F-30-30-36-30-30-30-30-36-30-30-38-30-30-38-32-32-30-30-30-30-30-30-30-30-30-30-30-30-30-30-34-30-30-30-30-30-30-30-30-30-30-30-30-30-30-30-39-32-38-30-35-35-35-32-31-30-32-31-33-33-39-33-30-31-03";
            //byte[] messageBytes = ColaCommUtility.HexString2HexBinary(hexString.Replace("-",""));
            try
            {
                byte[] messageBytes;
                if (CheckBoxParsingData.IsChecked == true)
                {
                    var parsingDataString = TextBoxParsingData.Text;
                    messageBytes = new byte[parsingDataString.Length];
                    Buffer.BlockCopy(Encoding.ASCII.GetBytes(parsingDataString), 0, messageBytes, 0, messageBytes.Length);
                }
                else
                {
                    GetISOBytes();
                    if (_getISOBytes is null || _getISOBytes.Length == 0)
                    {
                        SetMessageText(RichTextBox1, $"BtnNcccFileAuthFlow_Click Input Data Error", Brushes.Red);
                        return;
                    }
                    int dataLength;
                    if (CheckBoxInclude2ByteLength.IsChecked == true)
                        dataLength = 2;
                    else
                        dataLength = 0;
                    messageBytes = new byte[_getISOBytes.Length - dataLength];
                    Buffer.BlockCopy(_getISOBytes, dataLength, messageBytes, 0, messageBytes.Length);
                }

                dynamic eTicket;
                switch (ComboBoxETicketFunction.Text.ToUpper().Trim())
                {
                    case "SVCS":
                        eTicket = new SVCS(messageBytes);
                        break;
                    case "IPASS":
                        eTicket = new IPASS(messageBytes);
                        break;
                    case "ICASH":
                        eTicket = new ICASH(messageBytes);
                        break;
                    default:
                        SetMessageText(RichTextBox1, $"Select E-Ticket Data Error[{ComboBoxETicketFunction.Text.ToUpper().Trim()}]", Brushes.Red);
                        return;

                }
                SetMessageText(RichTextBox1, $"Header[{eTicket.Header().Encode()}]", Brushes.Black);
                SetMessageText(RichTextBox1, $"BitMap[{HexStringConverter.FromByteArray(eTicket.Bitmap())}]", Brushes.Black);
                SetMessageText(RichTextBox1, $"MTI [{eTicket.Mti()}]", Brushes.Black);
                foreach (var pair in eTicket.Core().OrderedFields())
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
                if (eTicket is SVCS)
                {
                    var fieldNumber = FIELD.F_58;
                    if (!ObjectUtils.IsNull(eTicket.RawVal(fieldNumber)))
                    {
                        var fieldData = (SVCSChip)eTicket.Core().GetOthersValByKey(fieldNumber.ToString());
                        foreach (var pair in fieldData.Core().OrderedFields())
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
                }

                if (eTicket is ICASH)
                {
                    var fieldNumber = FIELD.F_61;
                    if (!ObjectUtils.IsNull(eTicket.RawVal(fieldNumber)))
                    {
                        var fieldData = (ICASHChip)eTicket.Core().GetOthersValByKey(fieldNumber.ToString());
                        foreach (var pair in fieldData.Core().OrderedFields())
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
                }
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox1, ex.Message, Brushes.Red);
            }

        }

        private void BtnETicketAuthFlow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                byte[] messageBytes;
                byte[] eTicketRequestRawData;
                if (CheckBoxParsingData.IsChecked == true)
                {
                    var parsingDataString = TextBoxParsingData.Text;
                    messageBytes = new byte[parsingDataString.Length];
                    Buffer.BlockCopy(Encoding.ASCII.GetBytes(parsingDataString), 0, messageBytes, 0, messageBytes.Length);
                    eTicketRequestRawData = ColaCommUtility.ContactBytesArray(
                                            ColaCommUtility.IntegerToBinary(messageBytes.Length),
                                            messageBytes);
                }
                else
                {
                    GetISOBytes();
                    if (_getISOBytes is null || _getISOBytes.Length == 0)
                    {
                        SetMessageText(RichTextBox1, $"BtnETicketAuthFlow_Click Input Data Error", Brushes.Red);
                        return;
                    }
                    int dataLength;
                    if (CheckBoxInclude2ByteLength.IsChecked == true)
                    {
                        dataLength = 2;
                        messageBytes = new byte[_getISOBytes.Length - dataLength];
                        Buffer.BlockCopy(_getISOBytes, dataLength, messageBytes, 0, messageBytes.Length);
                        eTicketRequestRawData = new byte[messageBytes.Length];
                        Buffer.BlockCopy(messageBytes, 0, eTicketRequestRawData, 0, eTicketRequestRawData.Length);
                    }
                    else
                    {
                        dataLength = 0;
                        messageBytes = new byte[_getISOBytes.Length - dataLength];
                        Buffer.BlockCopy(_getISOBytes, dataLength, messageBytes, 0, messageBytes.Length);
                        eTicketRequestRawData = ColaCommUtility.ContactBytesArray(
                                             ColaCommUtility.IntegerToBinary(messageBytes.Length),
                                             messageBytes);
                    }
                }
                dynamic eTicket;
                switch (ComboBoxETicketFunction.Text.ToUpper().Trim())
                {
                    case "SVCS":
                        SetMessageText(RichTextBox1, $"Request ETicket is SVCS", Brushes.Black);
                        eTicket = new SVCS(messageBytes);
                        break;
                    case "IPASS":
                        SetMessageText(RichTextBox1, $"Request ETicket is IPASS", Brushes.Black);
                        eTicket = new IPASS(messageBytes);
                        break;
                    case "ICASH":
                        SetMessageText(RichTextBox1, $"Request ETicket is ICASH", Brushes.Black);
                        eTicket = new ICASH(messageBytes);
                        break;
                    default:
                        SetMessageText(RichTextBox1, $"Select E-Ticket Data Error[{ComboBoxETicketFunction.Text.ToUpper().Trim()}]", Brushes.Red);
                        return;

                }
                SetMessageText(RichTextBox1, $"Header[{eTicket.Header().Encode()}]", Brushes.Black);
                SetMessageText(RichTextBox1, $"BitMap[{HexStringConverter.FromByteArray(eTicket.Bitmap())}]", Brushes.Black);
                SetMessageText(RichTextBox1, $"MTI [{eTicket.Mti()}]", Brushes.Black);
                foreach (var pair in eTicket.Core().OrderedFields())
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
                if (eTicket is SVCS)
                {
                    var fieldNumber = FIELD.F_58;
                    if (!ObjectUtils.IsNull(eTicket.RawVal(fieldNumber)))
                    {
                        var fieldData = (SVCSChip)eTicket.Core().GetOthersValByKey(fieldNumber.ToString());
                        foreach (var pair in fieldData.Core().OrderedFields())
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
                }

                if (eTicket is ICASH)
                {
                    var fieldNumber = FIELD.F_61;
                    if (!ObjectUtils.IsNull(eTicket.RawVal(fieldNumber)))
                    {
                        var fieldData = (ICASHChip)eTicket.Core().GetOthersValByKey(fieldNumber.ToString());
                        foreach (var pair in fieldData.Core().OrderedFields())
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
                }
                byte[] retnProcessBytes = null;
                switch (ComboBoxETicketFunction.Text.ToUpper().Trim())
                {
                    case "SVCS":
                        retnProcessBytes = SvcsAuthFlowUtility.ProcessTransactionFlow(eTicket, "OnlineSession", string.Empty);
                        break;
                    case "IPASS":
                        retnProcessBytes = IpassAuthFlowUtility.ProcessTransactionFlow(eTicket, "OnlineSession", string.Empty);
                        break;
                    case "ICASH":
                        retnProcessBytes = IcashAuthFlowUtility.ProcessTransactionFlow(eTicket, "OnlineSession", string.Empty);
                        break;
                }
                if (retnProcessBytes != null)
                {
                    SetMessageText(RichTextBox1, Encoding.ASCII.GetString(retnProcessBytes), Brushes.Red);
                }
                else
                {
                    SetMessageText(RichTextBox1, "Return Null Data", Brushes.Red);
                }
                byte[] eTicketResponseBytes = new byte[retnProcessBytes.Length - 2];
                Array.Copy(retnProcessBytes, 2, eTicketResponseBytes, 0, retnProcessBytes.Length - 2);
                switch (ComboBoxETicketFunction.Text.ToUpper().Trim())
                {
                    case "SVCS":
                        SetMessageText(RichTextBox1, $"Response ETicket is SVCS", Brushes.Black);
                        eTicket = new SVCS(eTicketResponseBytes);
                        break;
                    case "IPASS":
                        SetMessageText(RichTextBox1, $"Response ETicket is IPASS", Brushes.Black);
                        eTicket = new IPASS(eTicketResponseBytes);
                        break;
                    case "ICASH":
                        SetMessageText(RichTextBox1, $"Response  ETicket is ICASH", Brushes.Black);
                        eTicket = new ICASH(eTicketResponseBytes);
                        break;
                }
                SetMessageText(RichTextBox1, $"Header[{eTicket.Header().Encode()}]", Brushes.Black);
                SetMessageText(RichTextBox1, $"BitMap[{HexStringConverter.FromByteArray(eTicket.Bitmap())}]", Brushes.Black);
                SetMessageText(RichTextBox1, $"MTI [{eTicket.Mti()}]", Brushes.Black);
                foreach (var pair in eTicket.Core().OrderedFields())
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
                if (eTicket is SVCS)
                {
                    var fieldNumber = FIELD.F_58;
                    if (!ObjectUtils.IsNull(eTicket.RawVal(fieldNumber)))
                    {
                        var fieldData = (SVCSChip)eTicket.Core().GetOthersValByKey(fieldNumber.ToString());
                        foreach (var pair in fieldData.Core().OrderedFields())
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
                }

                if (eTicket is ICASH)
                {
                    var fieldNumber = FIELD.F_61;
                    if (!ObjectUtils.IsNull(eTicket.RawVal(fieldNumber)))
                    {
                        var fieldData = (ICASHChip)eTicket.Core().GetOthersValByKey(fieldNumber.ToString());
                        foreach (var pair in fieldData.Core().OrderedFields())
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
                }
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox1, ex.Message, Brushes.Red);
            }
        }

        private void BtnFunctionTest2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                byte[] messageBytes;
                byte[] svcsRequestRawData;
                if (CheckBoxParsingData.IsChecked == true)
                {
                    var parsingDataString = TextBoxParsingData.Text;
                    messageBytes = new byte[parsingDataString.Length];
                    Buffer.BlockCopy(Encoding.ASCII.GetBytes(parsingDataString), 0, messageBytes, 0, messageBytes.Length);
                    svcsRequestRawData = ColaCommUtility.ContactBytesArray(
                                             ColaCommUtility.IntegerToBinary(messageBytes.Length),
                                             messageBytes);
                }
                else
                {
                    GetISOBytes();
                    if (_getISOBytes is null || _getISOBytes.Length == 0)
                    {
                        SetMessageText(RichTextBox1, $"BtnSvcsAuthFlow_Click Input Data Error", Brushes.Red);
                        return;
                    }
                    int dataLength;
                    if (CheckBoxInclude2ByteLength.IsChecked == true)
                    {
                        dataLength = 2;
                        messageBytes = new byte[_getISOBytes.Length - dataLength];
                        Buffer.BlockCopy(_getISOBytes, dataLength, messageBytes, 0, messageBytes.Length);
                        svcsRequestRawData = new byte[messageBytes.Length];
                        Buffer.BlockCopy(messageBytes, 0, svcsRequestRawData, 0, svcsRequestRawData.Length);
                    }
                    else
                    {
                        dataLength = 0;
                        messageBytes = new byte[_getISOBytes.Length - dataLength];
                        Buffer.BlockCopy(_getISOBytes, dataLength, messageBytes, 0, messageBytes.Length);
                        svcsRequestRawData = ColaCommUtility.ContactBytesArray(
                                             ColaCommUtility.IntegerToBinary(messageBytes.Length),
                                             messageBytes);
                    }
                }
                var svcsRequestMessage = new SVCS(messageBytes);
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

                var fieldOtherPos = FIELD.F_58;
                if (!ObjectUtils.IsNull(svcsRequestMessage.RawVal(fieldOtherPos)))
                {
                    var svcsChip = svcsRequestMessage.F58TokenEncode();

                    foreach (var pair in svcsChip.Core().OrderedFields())
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
                    SetMessageText(RichTextBox1, $"FIELD [{fieldOtherPos}] [{Encoding.ASCII.GetString(svcsChip.Encode().Val()).Substring(4)}]", Brushes.Black);
                    SetMessageText(RichTextBox1, $"Original Data[{ColaCommUtility.GetDisplayForRawData(svcsRequestRawData)}],Length[{svcsRequestRawData.Length - 2}]", Brushes.Red);
                    SetMessageText(RichTextBox1, $"Add 2 Bytes,Hex Data Length[{svcsRequestRawData[0]:X02}-{svcsRequestRawData[1]:X02}]", Brushes.Red);
                    byte[] retnProcessBytes = SvcsAuthFlowUtility.ProcessTransactionFlow(svcsRequestMessage, "OnlineSession", string.Empty);
                    if (retnProcessBytes != null)
                    {
                        SetMessageText(RichTextBox1, Encoding.ASCII.GetString(retnProcessBytes), Brushes.Red);
                    }
                    else
                    {
                        SetMessageText(RichTextBox1, "Return Null Data", Brushes.Red);
                    }
                    byte[] svcsResponseBytes = new byte[retnProcessBytes.Length - 2];
                    Array.Copy(retnProcessBytes, 2, svcsResponseBytes, 0, retnProcessBytes.Length - 2);
                    SVCS svcsResponseMessage = new SVCS(svcsResponseBytes);
                    SetMessageText(RichTextBox1, "--- Response Message ---", Brushes.Black);
                    SetMessageText(RichTextBox1, $"Return SvcsIsoData(No Binary Length) [{Encoding.ASCII.GetString(svcsResponseBytes)}]", Brushes.Red);
                    SetMessageText(RichTextBox1, $"Header[{svcsResponseMessage.Header().Encode()}]", Brushes.Black);
                    SetMessageText(RichTextBox1, $"BitMap[{HexStringConverter.FromByteArray(svcsResponseMessage.Bitmap())}]", Brushes.Black);
                    SetMessageText(RichTextBox1, $"MTI [{svcsResponseMessage.Mti()}]", Brushes.Black);
                    foreach (var pair in svcsResponseMessage.Core().OrderedFields())
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
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox1, ex.Message, Brushes.Red);
            }
        }

        private void ComboBoxETicketFunction_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void BtnTwin_Click(object sender, RoutedEventArgs e)
        {
            //var messageHexString = @"ISO00600006005008222000000004000007F8780A000000001130901070000830113901000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000C000000000000000006456312";
            //var messageHexString = @"ISO0260000100200B238C48128E1800E000000000000000200003000000010000001270928000000191728000127012898769020006493817374038790000000053=1309101104802400000000000100108313996187        6619400169     NCCC FOR IC TESTING   CHINMEN-MATZUTW TW0276619400169         00010000901019BK17    000        010105       262& 0000500262! 0400020                   Y ! C000026         105       3       ! B400020 022500            0 ! C500144 6619400169      11                                ST                                      I                             06                      134000000                                                                                                            0000000000010010831 ";
            //var messageHexString = @"ISO006000060080082200000000008000400000000000120042002321901041500010000000000001620067B5300086CSM(MCL/KSM RCV/BC28 ORG/NCCC KD/65786D830508C10E885783DD70B61BBD CTP/00000000000ACB )";
            //var messageHexString = @"ISO02600007008008220000000000800040000000000012008041948450092500001000000000000162006157000086CSM(MCL/KSM RCV/BC39 ORG/NCCC KD/1C0BCB9A5EA62D474F00149CDB1A0524 CTP/0000000000028A )";
            //var messageHexString = @"ISO0260000100220B238C4812EE1800E000000020000000222003000000030000009070530212313431330200907090853119020006493817215588650006639109=16070000010030176918100013995009        6601000105     NCCC FOR IC TESTING   CHINMEN-MATZUTW TW0276601000105         00010000901019BK53UAT200000000000010105       262& 0000500262! 0400020                   Y ! B400020 022500            0 ! C000026         105       3   F   ! C500144 6601000105      62                                ST                                      E00                           030000103000001030000090000000000000000000000000000000000000000000134000000                                                                                                            0000000000010030171 ";
            //string hexString = @"49-53-4F-30-30-36-30-30-30-30-36-30-30-31-31-30-33-32-33-41-30-30-30-31-32-45-38-30-38-30-30-32-30-30-30-30-33-30-30-30-30-30-30-30-30-30-30-30-31-39-30-36-32-32-31-35-35-30-31-37-31-38-33-35-31-39-31-35-35-30-31-37-30-36-32-32-30-32-31-31-30-36-34-39-33-38-31-37-33-37-34-33-37-37-35-37-38-38-38-30-34-38-32-31-30-37-3D-31-33-30-39-31-30-31-31-30-34-38-30-32-34-30-30-30-30-30-30-31-37-30-36-32-32-31-38-33-35-31-39-34-30-38-30-34-33-30-30-31-33-39-39-36-31-38-37-20-20-20-20-20-20-20-20-39-30-31-30-37-38-26-20-30-30-30-30-33-30-30-30-37-38-21-20-30-34-30-30-30-32-30-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-59-20-21-20-43-30-30-30-30-32-36-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20-20";
            //string hexString = @"49-53-4F-30-30-36-30-30-30-30-36-30-30-38-30-30-38-32-32-30-30-30-30-30-30-30-30-30-30-30-30-30-30-34-30-30-30-30-30-30-30-30-30-30-30-30-30-30-30-39-32-38-30-35-35-35-32-31-30-32-31-33-33-39-33-30-31-03";
            //byte[] messageBytes = ColaCommUtility.HexString2HexBinary(hexString.Replace("-",""));
            try
            {
                //AppDomain currentDomain = AppDomain.CurrentDomain;
                //Assembly[] assems = currentDomain.GetAssemblies();
                //foreach (Assembly assem in assems)
                //{
                //    SetMessageText(RichTextBox1, $"{assem.ToString()}", Brushes.Red);
                //    var types = assem.GetTypes();
                //    foreach (Type type in types)
                //    {
                //        if (type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(CardType)))
                //        {
                //           SetMessageText(RichTextBox1, $"{type.FullName}", Brushes.Red);
                //           var instance = (CardType)Activator.CreateInstance(type);
                //           TypeMaps[type] = instance;
                //           IdMaps[instance.Id()] = instance;
                //        }
                //    }
                //}
                //
                //return;
                //AssemblyUtils.GetAllAssemblies()
                //.ToList()
                //.ForEach(
                //   assembly =>
                //       {
                //           assembly.GetTypes()
                //               .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(CardType)))
                //               .ToList()
                //               .ForEach(
                //                   type =>
                //                       {
                //                           var instance = (CardType)Activator.CreateInstance(type);
                //                           TypeMaps[type] = instance;
                //                           IdMaps[instance.Id()] = instance;
                //                       });
                //       });
                //var assembly1 = AppDomain.CurrentDomain.GetAssemblies().ToList().Select(assembly => assembly.GetTypes());
                //foreach (var type1 in assembly1)
                //{
                //    foreach (var type2 in type1)
                //    {
                //        if (type2.IsClass && !type2.IsAbstract && type2.IsSubclassOf(typeof(CardType)))
                //        {
                //            var instance = (CardType)Activator.CreateInstance(type2);
                //            TypeMaps[type2] = instance;
                //            IdMaps[instance.Id()] = instance;
                //            SetMessageText(RichTextBox1, $"type[{type2.FullName}]", Brushes.Black);
                //        }
                //    }
                //}
                //
                //var testData = SingletonOfCommonProperty.Instance.GetColaPasystfInfoStruct();
                //return;
                byte[] messageBytes;
                if (CheckBoxParsingData.IsChecked == true)
                {
                    var parsingDataString = TextBoxParsingData.Text;
                    messageBytes = new byte[parsingDataString.Length];
                    Buffer.BlockCopy(Encoding.ASCII.GetBytes(parsingDataString), 0, messageBytes, 0, messageBytes.Length);
                }
                else
                {
                    GetISOBytes();
                    if (_getISOBytes is null || _getISOBytes.Length == 0)
                    {
                        SetMessageText(RichTextBox1, $"BtnNccc_Click Input Data Error", Brushes.Red);
                        return;
                    }
                    int dataLength;
                    if (CheckBoxInclude2ByteLength.IsChecked == true)
                        dataLength = 2;
                    else
                        dataLength = 0;
                    messageBytes = new byte[_getISOBytes.Length - dataLength];
                    Buffer.BlockCopy(_getISOBytes, dataLength, messageBytes, 0, messageBytes.Length);
                }

                //var messageString = @"NCM0301000000100B63E65912CE1B8080400006200000000000030000000000082000000000100091905553537701313553509192012091953111580520010000000010006005151375520031501098508=2301201169727040000022091930850212345699981109000008126600001xxxxxxxxxxxxxxxxxxxxxxTAIPEI       TW TW02100000812660000100000090190101234567890123450123456789012345019BK48PRO2000000000003010123456789012345678901234567890123456789001000000000000000000000000000000000000000000";
                //messageBytes = Encoding.ASCII.GetBytes(messageString);
                TWINIso8583 twin = new TWINIso8583(messageBytes);
                //nccc.ShowFields();
                SetMessageText(RichTextBox1, $"Header[{twin.Header().Encode()}]", Brushes.Black);
                SetMessageText(RichTextBox1, $"BitMap[{HexStringConverter.FromByteArray(twin.Bitmap())}]", Brushes.Black);
                SetMessageText(RichTextBox1, $"MTI [{twin.Mti()}]", Brushes.Black);
                foreach (var pair in twin.Core().OrderedFields())
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
                var fieldData = FIELD.F_55;
                if (!ObjectUtils.IsNull(twin.RawVal(fieldData)))
                {
                    var field55 = (TWINFIELD_55)twin.Core().GetOthersValByKey("F_55");
                    foreach (var keyData in field55.GetTLV())
                    {
                        SetMessageText(RichTextBox1, $"Key[{keyData.Key}]--Value[{keyData.Value}]", Brushes.Black);
                    }
                    SetMessageText(RichTextBox1, $"{fieldData.ToString()}--Data[{field55.Encode()}]", Brushes.Black);
                }
                fieldData = FIELD.F_63;
                if (!ObjectUtils.IsNull(twin.RawVal(fieldData)))
                {
                    var field63 = (TWINTLV)twin.Core().GetOthersValByKey("F_63");
                    foreach (var keyData in field63.GetTLV())
                    {
                        SetMessageText(RichTextBox1, $"Key[{keyData.Key}]--Value[{keyData.Value}]", Brushes.Black);
                    }
                    SetMessageText(RichTextBox1, $"{fieldData.ToString()}--Data[{field63.Encode()}]", Brushes.Black);
                }
                return;


                //var nccc = new NcccIso8583(messageBytes);
                //var nccc = NcccTransCreator.SignOffRequest();
                var dataBytes = twin.Encode().Val();
                byte[] sendBytes = new byte[dataBytes.Length + 2];
                sendBytes[0] = (byte)(dataBytes.Length >> 8);
                sendBytes[1] = (byte)dataBytes.Length;
                Buffer.BlockCopy(dataBytes, 0, sendBytes, 2, dataBytes.Length);
                SetMessageText(RichTextBox1, $"Header[{twin.Header().Encode()}]", Brushes.Black);
                SetMessageText(RichTextBox1, $"BitMap[{HexStringConverter.FromByteArray(twin.Bitmap())}]", Brushes.Black);
                SetMessageText(RichTextBox1, $"MTI [{twin.Mti()}]", Brushes.Black);
                foreach (var pair in twin.Core().OrderedFields())
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
                return;
                //var fieldData = FIELD.F_35;
                //if (!ObjectUtils.IsNull(nccc.RawVal(fieldData)))
                //    new BicTrack2((string)nccc.Core().Field(fieldData));
                //var tokenGroup = (TokenGroup)nccc.Core().GetOthersValByKey("F_63");
                //if (tokenGroup == null) return;
                //foreach (var t in tokenGroup.Tokens)
                //{
                //    SetMessageText(RichTextBox1, $" TokenID[{t.Key}] Value[{t.Value.Encode()}]", Brushes.Black);
                //    switch (t.Key)
                //    {
                //        case NcccTransactionConst.PosAdditionalData.TokenB2.Id:
                //            var valueTokenB2 = (TokenB2)t.Value;
                //            SetMessageText(RichTextBox1, $" TokenB2 Value [{valueTokenB2.DataEncode()}]", Brushes.Black);
                //            break;
                //        case NcccTransactionConst.PosAdditionalData.TokenB3.Id:
                //            var valueTokenB3 = (TokenB3)t.Value;
                //            SetMessageText(RichTextBox1, $" TokenB3 Value [{valueTokenB3.DataEncode()}]", Brushes.Black);
                //            break;
                //        case NcccTransactionConst.PosAdditionalData.TokenB4.Id:
                //            var valueTokenB4 = (TokenB4)t.Value;
                //            SetMessageText(RichTextBox1, $" TokenB4 Value [{valueTokenB4.DataEncode()}]", Brushes.Black);
                //            break;
                //        case NcccTransactionConst.PosAdditionalData.TokenB5.Id:
                //            var valueTokenB5 = (TokenB5)t.Value;
                //            SetMessageText(RichTextBox1, $" TokenB5 Value [{valueTokenB5.DataEncode()}]", Brushes.Black);
                //            break;
                //        case NcccTransactionConst.PosAdditionalData.TokenB6.Id:
                //            var valueTokenB6 = (TokenB6)t.Value;
                //            SetMessageText(RichTextBox1, $" TokenB6 Value [{valueTokenB6.DataEncode()}]", Brushes.Black);
                //            break;
                //        case NcccTransactionConst.PosAdditionalData.Token04.Id:
                //            var valueToken04 = (Token04)t.Value;
                //            SetMessageText(RichTextBox1, $" Token04 Value [{valueToken04.DataEncode()}]", Brushes.Black);
                //            break;
                //        case NcccTransactionConst.PosAdditionalData.TokenC0.Id:
                //            var valueTokenC0 = (TokenC0)t.Value;
                //            SetMessageText(RichTextBox1, $" TokenC0 Value [{valueTokenC0.DataEncode()}]", Brushes.Black);
                //            break;
                //        case NcccTransactionConst.PosAdditionalData.TokenC5.Id:
                //            dynamic valueTokenC5;
                //            var c5 = t.Value as TokenRoyaltyC5;
                //            if (c5 != null)
                //            {
                //                valueTokenC5 = c5;
                //            }
                //            else
                //            {
                //                valueTokenC5 = (TokenInstallmentC5)t.Value;
                //                SetMessageText(RichTextBox1, $" TokenC5 Value [{((TokenInstallmentC5)valueTokenC5).Encode()}]", Brushes.Black);
                //            }
                //            break;
                //        case NcccTransactionConst.PosAdditionalData.TokenCE.Id:
                //            var valueTokenCE = (TokenCE)t.Value;
                //            SetMessageText(RichTextBox1, $" TokenCE Value [{valueTokenCE.DataEncode()}]", Brushes.Black);
                //            break;
                //        case NcccTransactionConst.PosAdditionalData.TokenU7.Id:
                //            var valueTokenU7 = (TokenU7)t.Value;
                //            SetMessageText(RichTextBox1, $" TokenU7 Value [{valueTokenU7.DataEncode()}]", Brushes.Black);
                //            break;
                //    }
                //    var fieldPos = FIELD.F_63;
                //    if (!ObjectUtils.IsNull(nccc.RawVal(fieldPos)))
                //    {
                //        SetMessageText(RichTextBox1, $" FIELD {fieldPos} Value [{nccc.RawVal(fieldPos).ToString()}]",
                //            Brushes.Black);
                //    }
                //}
                //var authStruct = new BicAuthInfoStruct(nccc);
                //SetMessageText(RichTextBox1, $"Pvv : {authStruct.isoP35Pvv}]",
                //            Brushes.Black);

                var NcccMessageTypeMapInfo = SingletonOfNcccMessageTypeMap.Instance.GetMessageTypeMap();
                Regex findPattern = new Regex("[cCmM]");
                NcccIso8583 responseNcccMessage = null;
                MessageType ncccMessageType = null;
                bool findFlag = NcccMessageTypeMapInfo.TryGetValue("0800", out ncccMessageType);
                responseNcccMessage = new NcccIso8583();
                var test = twin.Header().Encode();
                responseNcccMessage.Header().Decode(test);
                responseNcccMessage.Mti(ncccMessageType.ResponseMessage);
                foreach (var message in ncccMessageType.ResponseField.Field)
                {
                    int fieldPos = Convert.ToInt32(message.Number);
                    if (fieldPos == 1) continue;
                    char respCondition = Convert.ToChar(message.DataCondition);
                    if (findPattern.IsMatch(Convert.ToString(respCondition)))
                    {
                        var field = FIELDMethods.Val(Convert.ToInt32(message.Number));
                        if (!ObjectUtils.IsNull(twin.RawVal(field)))
                        {
                            responseNcccMessage.Core().Field(field, twin.Core().Field(field));
                        }
                        else
                        {
                            switch (fieldPos)
                            {
                                case 15:
                                    //responseNcccMessage.Core().Field(field,ColaCommUtility.GetSettlementDate(SystemConstantData.CommonData.PasystfNCCCInfo));
                                    responseNcccMessage.Core().Field(field, "0123");
                                    break;
                                case 39:
                                    responseNcccMessage.Core().Field(field, "00");
                                    break;
                            }
                        }
                    }
                }
                var returnBytes = responseNcccMessage.Encode().Val();
                SetMessageText(RichTextBox1, Encoding.ASCII.GetString(returnBytes), Brushes.Red);
                return;
                twin.Core().Field(FIELD.F_2, "1234123412341234");
                byte[] testBytes = twin.Encode().Val();
                SetMessageText(RichTextBox1, Encoding.ASCII.GetString(testBytes), Brushes.Red);
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBox1, ex.Message, Brushes.Red);
            }
        }

        private void BtnTwinToken_Click(object sender, RoutedEventArgs e)
        {
            var twinMessage = new TwinMessage0800(TwinTransactionConst.NetworkManagementInformationCode.SignOn).GetTwinMessage();
            SetMessageText(RichTextBox1, $"Header[{twinMessage.Header().Encode()}]", Brushes.Black);
            SetMessageText(RichTextBox1, $"BitMap[{HexStringConverter.FromByteArray(twinMessage.Bitmap())}]", Brushes.Black);
            SetMessageText(RichTextBox1, $"MTI [{twinMessage.Mti()}]", Brushes.Black);
            foreach (var pair in twinMessage.Core().OrderedFields())
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


            //var messageString = @"NCM0301000000100B63E65912CE1B8080400006200000000000030000000000082000000000100091905553537701313553509192012091953111580520010000000010006005151375520031501098508=2301201169727040000022091930850212345699981109000008126600001xxxxxxxxxxxxxxxxxxxxxxTAIPEI       TW TW02100000812660000100000090190101234567890123450123456789012345019BK48PRO2000000000003010123456789012345678901234567890123456789001000000000000000000000000000000000000000000";
            //var messageBytes = Encoding.ASCII.GetBytes(messageString);
            //
            //var twin = new TWINIso8583(messageBytes);
            //var twinRequestMessage = new Com.Cybersoft4u.TM.ISO8583.TWIN.Transaction.TwinSaleRequest().Core(twin.Core());
            //SetMessageText(RichTextBox1, $"Header[{twinRequestMessage.Header().Encode()}]", Brushes.Black);
            //SetMessageText(RichTextBox1, $"BitMap[{HexStringConverter.FromByteArray(twinRequestMessage.Bitmap())}]", Brushes.Black);
            //SetMessageText(RichTextBox1, $"MTI [{twinRequestMessage.Mti()}]", Brushes.Black);
            //foreach (var pair in twinRequestMessage.Core().OrderedFields())
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
            //var twinResponseMessage  = new Com.Cybersoft4u.TM.ISO8583.TWIN.Transaction.TwinSignOnResponse().Core(twinRequestMessage.Core());
            //SetMessageText(RichTextBox1, $"Header[{twinResponseMessage.Header().Encode()}]", Brushes.Black);
            //SetMessageText(RichTextBox1, $"BitMap[{HexStringConverter.FromByteArray(twinResponseMessage.Bitmap())}]", Brushes.Black);
            //SetMessageText(RichTextBox1, $"MTI [{twinResponseMessage.Mti()}]", Brushes.Black);
            //foreach (var pair in twinResponseMessage.Core().OrderedFields())
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

        }

        private void BtnTwinAuthFlow_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}

