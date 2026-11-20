using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.CommonLib.Resource;
using Com.Cybersoft4u.CyberUtils.Utils;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using Com.Cybersoft4u.TM.ISO8583.TWIN;
using ExcelDataReader;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Com.Cybersoft4u.TM.FISC;
using Control = System.Windows.Controls.Control;
using Cursors = System.Windows.Input.Cursors;
using DataGrid = System.Windows.Controls.DataGrid;
using Logger = NLog.Logger;
using RichTextBox = System.Windows.Controls.RichTextBox;

namespace Com.Cybersoft4u.Test.Wpf.WpfISO8583
{
    /// <summary>
    /// Main window for ISO 8583 message testing application.
    /// Provides UI for creating, parsing, and testing ISO 8583 financial transaction messages.
    /// </summary>
    public partial class MainWindow : Window
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog
        private static readonly SystemParameterInfo SystemParameterInfo = SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo();
        private delegate void UpdateRichTextBox(Control richTextBox, string message, Brush brush);

        private static string[] _cardTrack2Array;
        private static string _initialDirectory;
        private static readonly Random StaticTraceRnd = new Random();
        private static readonly Random StaticAmtRnd = new Random();
        private static readonly Random StaticRefRnd = new Random();
        private static readonly Random StaticCardIndex = new Random();
        private byte[] _getISOBytes;
        private XDocument _xmlDocumentMessageType;
        private XDocument _xmlDocumentFieldType;
        private List<BICField> _bicFieldList = new List<BICField>();
        private BicFieldDefaultValue _bicFieldDefaultValue;
        private Dictionary<string, string> tokenIdFileDictionary = new Dictionary<string, string>();
        private string _tokenDefineFilePath;
        public MainWindow()
        {
            try
            {
                InitializeComponent();
                var dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
                dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
                dispatcherTimer.Start();
                InitialSystemParameter();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, ex.Message);
            }
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            // Updating the Label which displays the current second
            TextBlockTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            // Forcing the CommandManager to raise the RequerySuggested event
            CommandManager.InvalidateRequerySuggested();
        }

        private void InitialType(string type)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                string messageTypeConfigFilePath;
                string fieldDefaultValueFilePath;
                string fieldTypeConfigFilePath;
                string messageTypeInfo;
                var configurationLoaderRoot = new ConfigurationLoaderRoot();
                DataGridISO8583EntryScreen.Items.Clear();
                DataGridISO8583EntryScreen.Items.Refresh();
                switch (type.ToUpper().Trim())
                {
                    case "FISC":
                        fieldDefaultValueFilePath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "fisc-bic-field-default-value-info-xml-file-path").Trim();
                        fieldTypeConfigFilePath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "FiscFieldTypeConfig").Trim();
                        messageTypeConfigFilePath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "FiscMessageTypeConfig").Trim();
                        messageTypeInfo = "FiscMessageTypeInfo";
                        break;
                    case "NCCC":
                        fieldDefaultValueFilePath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "nccc-bic-field-default-value-info-xml-file-path").Trim();
                        fieldTypeConfigFilePath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "NcccFieldTypeConfig").Trim();
                        messageTypeConfigFilePath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "NcccMessageTypeConfig").Trim();
                        messageTypeInfo = "NcccMessageTypeInfo";
                        break;
                    case "TWIN":
                        fieldDefaultValueFilePath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "twin-bic-field-default-value-info-xml-file-path").Trim();
                        fieldTypeConfigFilePath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "TwinFieldTypeConfig").Trim();
                        messageTypeConfigFilePath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "TwinMessageTypeConfig").Trim();
                        messageTypeInfo = "TwinMessageTypeInfo";
                        break;
                    default:
                        SetMessageText(RichTextBoxLogMessage, $"InitialType Error, Select Type[{type.ToUpper().Trim()}] Error", Brushes.Red);
                        return;
                }
                if (string.IsNullOrEmpty(fieldDefaultValueFilePath))
                {
                    Log.Fatal($"Can not find parameter[{type.ToLower()}-bic-field-default-value-info-xml-file-path]");
                    return;
                }

                XmlSerializer serializer = new XmlSerializer(typeof(BicFieldDefaultValue));
                using (XmlReader reader = XmlReader.Create(fieldDefaultValueFilePath))
                {
                    _bicFieldDefaultValue = (BicFieldDefaultValue)serializer.Deserialize(reader);
                }

                if (string.IsNullOrEmpty(fieldTypeConfigFilePath))
                {
                    Log.Fatal($"Can not find parameter[{char.ToUpper(type[0]) + type.Substring(1)}FieldTypeConfig]");
                    return;
                }
                _xmlDocumentFieldType = XDocument.Load(fieldTypeConfigFilePath);

                _xmlDocumentMessageType = XDocument.Load(messageTypeConfigFilePath);
                IEnumerable<XElement> messageTypeElements = _xmlDocumentMessageType.Element(messageTypeInfo)?.Elements("MessageType");
                var requestMessage = messageTypeElements?.Where(x => Convert.ToInt16(x.Element("RequestMessage")?.Value.Substring(2, 1)) % 2 == 0)
                                                                         .Select(x => x.Element("RequestMessage")?.Value);

                _bicFieldList = _xmlDocumentFieldType.Element("ArrayOfBicField")?.Elements("BicField")
                              .OrderBy(x => $"{Convert.ToInt16(x.Element("BitNum")?.Value.Trim()):000}")
                              .Select(x => new BICField()
                              {
                                  BitNum = x.Element("BitNum")?.Value,
                                  FieldName = x.Element("FieldName")?.Value,
                                  FieldDataType = x.Element("FieldDataType")?.Value,
                                  Length = x.Element("Length")?.Value,
                                  FieldDataFormat = x.Element("FieldDataFormat")?.Value,
                                  Description = x.Element("Description")?.Value,
                              })
                              .ToList();

                ComboBoxMessageType.Items.Clear();
                if (requestMessage != null)
                {
                    foreach (var messageType in requestMessage)
                    {
                        ComboBoxMessageType.Items.Add(messageType);
                    }
                }
                ComboBoxMessageType.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, ex.Message);
                SetMessageText(RichTextBoxLogMessage, $"InitialType Exception:{ex.Message}", Brushes.Red);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void InitialSystemParameter()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                //initial application use variable
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                var configurationLoaderRoot = new ConfigurationLoaderRoot();
                string xmlFilePath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "CardXmlFilePath").Trim();
                _cardTrack2Array = GetCardArray(xmlFilePath);
                _initialDirectory = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "InitialDirectory").Trim();
                ComboBoxType.Items.Clear();
                ComboBoxType.Items.Add("FISC");
                ComboBoxType.Items.Add("NCCC");
                ComboBoxType.Items.Add("TWIN");
                ComboBoxType.Items.Refresh();
                ComboBoxType.SelectedIndex = 0;
                InitialType("FISC");
                ComboBoxProgramName.Items.Clear();
                var xmlFilePosition = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "ProgramLists-xml-file-path").Trim();
                var programLists = XDocument.Load(xmlFilePosition).Element("ProgramLists")?.Elements("Program")?
                                             .Select(p => $"{p.Element("Name")?.Value}-{p.Element("ExecPosition")?.Value}")
                                             .ToList();
                if (programLists != null)
                {
                    foreach (var program in programLists)
                    {
                        ComboBoxProgramName.Items.Add(program);
                    }
                }
                ComboBoxProgramName.SelectedIndex = 0;

                ComboBoxTokenId.SelectedIndex = 0;
                ComboBoxTokenId.Items.Clear();
                tokenIdFileDictionary.Clear();
                _tokenDefineFilePath = configurationLoaderRoot.Get(SystemConstantData.ConfigSectionName.GeneralConfigSectionName, "TokenDefineFilePath").Trim();
                var fileNameArrays = Directory.GetFiles(_tokenDefineFilePath);
                foreach (var fileName in fileNameArrays)
                {
                    var temp = Path.GetFileNameWithoutExtension(fileName);
                    if (temp.ToUpper().Contains("TOKEN"))
                    {
                        ComboBoxTokenId.Items.Add(temp.Replace("token", ""));
                        tokenIdFileDictionary[temp.Replace("token", "")] = fileName;
                    }
                }
                ComboBoxTokenId.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, ex.Message);
                SetMessageText(RichTextBoxLogMessage, $"InitialSystemParameter Exception:{ex.Message}", Brushes.Red);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void SetMessageText(Control richTextBox, string message, Brush brush)
        {
            if (!(richTextBox is RichTextBox rtb)) return;
            if (rtb.Dispatcher.CheckAccess())
            {
                TextRange rangeOfText1 = new TextRange(rtb.Document.ContentEnd, rtb.Document.ContentEnd)
                { Text = message + '\r' };
                rangeOfText1.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
                rangeOfText1.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);
                rtb.ScrollToEnd();
            } //if
            else
            {
                rtb.Dispatcher.BeginInvoke(new UpdateRichTextBox(SetMessageText), richTextBox, message, brush);
            }
        }

        private void BtnClosed_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private class BICField
        {
            public string BitNum { get; set; }
            public string FieldName { get; set; }
            public string FieldDataType { get; set; }
            public string Length { get; set; }
            public string FieldDataFormat { get; set; }
            public string Description { get; set; }
        }

        private class TokenFieldInfo
        {
            public string Level { get; set; }
            public string FieldId { get; set; }
            public string DataLength { get; set; }
            public string DataType { get; set; }
            public string DataFormat { get; set; }
            public string Value { get; set; }
            public string FieldName { get; set; }
            public string Description { get; set; }
            public string EntryData { get; set; }
        }

        private class ISOFieldInfo
        {
            public string BitNum { get; set; }
            public string FieldName { get; set; }
            public string FieldDataType { get; set; }
            public string Length { get; set; }
            public string FieldDataFormat { get; set; }
            public string Description { get; set; }
            public string Condition { get; set; }
            public string EntryData { get; set; }
        }

        private string[] GetCardArray(string cardXmlFilePath)
        {
            var doc = XDocument.Load(cardXmlFilePath);
            var services = doc.Descendants("CardInformation")
                              .Select(cardInformation => cardInformation.Element("Track2Data")?.Value);
            return services.ToArray();
        }

        private string SetDefaultValue(int bitNum)
        {
            NcccIso8583 ncccMessage = null;
            if (CheckBoxSelectFile.IsChecked != null && CheckBoxSelectFile.IsChecked.Value)
            {
                if (File.Exists(TextBoxTemplateFilePath.Text.Trim()))
                {
                    var readBytes = File.ReadAllBytes(TextBoxTemplateFilePath.Text.Trim());
                    ncccMessage = new NcccIso8583(readBytes);
                    foreach (var pair in ncccMessage.Core().OrderedFields())
                    {
                        var key = pair.Key;
                        var value = pair.Value;
                        if (value is byte[] bytes)
                        {
                            var tempString = HexStringConverter.FromByteArray(bytes);
                            SetMessageText(RichTextBoxLogMessage, $"FIELD [{key}] VALUE [{tempString}]", Brushes.Black);
                        }
                        else
                        {
                            SetMessageText(RichTextBoxLogMessage, $"FIELD [{key}] Field Position [{FIELDMethods.Val(key)}] VALUE [{value}]", Brushes.Black);
                        }
                    }
                }
            }
            string track2Data = _cardTrack2Array[StaticCardIndex.Next(0, _cardTrack2Array.Length - 1)];
            string defaultValue = String.Empty;
            try
            {
                switch (bitNum)
                {
                    case 2:
                        defaultValue = _cardTrack2Array[StaticCardIndex.Next(0, _cardTrack2Array.Length - 1)]
                            .Substring(0, 16);
                        break;
                    case 4:
                        int amtMount = StaticAmtRnd.Next(1, 100);
                        defaultValue = $"{amtMount:000000000000}";
                        break;
                    case 7:
                        defaultValue = $"{DateTime.Now.ToUniversalTime():MMddHHmmss}";
                        break;
                    case 11:
                        int traceNumber = StaticTraceRnd.Next(1, 999999);
                        defaultValue = $"{traceNumber:000000}";
                        break;
                    case 12:
                        defaultValue = $"{DateTime.Now:HHmmss}";
                        break;
                    case 13:
                        defaultValue = $"{DateTime.Now:MMdd}";
                        break;
                    case 15:
                        defaultValue = $"{DateTime.Now:MMdd}";
                        break;
                    case 17:
                        defaultValue = $"{DateTime.Now:MMdd}";
                        break;
                    case 35:
                        defaultValue = track2Data;
                        break;
                    case 37:
                        int refNumber = StaticRefRnd.Next(1, 999999);
                        defaultValue = $"{DateTime.Now:yyMMdd}{refNumber:000000}";
                        break;
                    case 61:
                        defaultValue = $"BK{SystemParameterInfo.BankInfo.BankID}PRO200000000000";
                        break;
                    default:
                        if (ncccMessage != null)
                        {
                            var field = FIELDMethods.Val(bitNum);
                            defaultValue = (string)ncccMessage.Core().Field(field);
                        }
                        else
                            defaultValue = _bicFieldDefaultValue.BicField[bitNum - 1].Value;
                        break;
                }

                return defaultValue;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, ex.Message);
                SetMessageText(RichTextBoxLogMessage, $"SetDefaultValue Exception:{ex.Message}", Brushes.Red);
                return defaultValue;
            }
        }
        private void SetFocusOnGrid(DataGrid grid, int index)
        {
            grid.ScrollIntoView(grid.Items.GetItemAt(index));
            grid.SelectionMode = DataGridSelectionMode.Single;
            grid.SelectionUnit = DataGridSelectionUnit.FullRow;
            grid.SelectedIndex = index;

            DataGridRow row = (DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(index);
            row.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }

        private void BtnCreateISOData_Click(object sender, RoutedEventArgs e)
        {
            BtnCreateISOData.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                var type = ComboBoxType.Text;
                switch (type.ToUpper().Trim())
                {
                    case "FISC":
                        CreateFiscISOData();
                        break;
                    case "NCCC":
                        CreateNcccISOData();
                        break;
                    case "TWIN":
                        CreateTwinISOData();
                        break;
                    default:
                        SetMessageText(RichTextBoxLogMessage, $"Create ISO Data Error, Select Type[{type.ToUpper().Trim()}] Error", Brushes.Red);
                        return;
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, ex.Message);
                SetMessageText(RichTextBoxLogMessage, $"BtnCreateISOData_Click Exception:{ex.Message}", Brushes.Red);
            }
            finally
            {
                BtnCreateISOData.IsEnabled = true;
                Mouse.OverrideCursor = null;
            }
        }
        private void BtnCleanMessage_Click(object sender, RoutedEventArgs e)
        {
            RichTextBoxLogMessage.Document.Blocks.Clear();
        }

        private void BtnStartProgram_Click(object sender, RoutedEventArgs e)
        {
            BtnStartProgram.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                var program = ComboBoxProgramName.Text.Trim().Split('-');
                if (program.Length != 2)
                {
                    Log.Fatal("Get Program Name Parameter Error");
                    SetMessageText(RichTextBoxLogMessage, "Get Program Name Parameter Error", Brushes.Red);
                    return;
                }

                var exePath = program[1].Trim();
                var progExecPosition = System.Reflection.Assembly.GetExecutingAssembly().Location;
                if (progExecPosition.Contains("Debug"))
                    exePath.Replace("Release", "Debug");
                if (string.IsNullOrEmpty(exePath))
                {
                    Log.Fatal("Program exec position invalid");
                    SetMessageText(RichTextBoxLogMessage, "Program exec position invalid", Brushes.Red);
                    return;
                }

                if (!File.Exists(exePath))
                {
                    Log.Fatal($"Can not find exe file[{exePath}]");
                    SetMessageText(RichTextBoxLogMessage, $"Can not find exe file[{exePath}]", Brushes.Red);
                    return;
                }
                Process.Start(exePath);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, ex.Message);
                SetMessageText(RichTextBoxLogMessage, $"BtnStartProgram_Click Exception:{ex.Message}", Brushes.Red);
            }
            finally
            {
                BtnStartProgram.IsEnabled = true;
                Mouse.OverrideCursor = null;
            }

        }

        private void BtnSelectedFilePosition_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

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
                TextBoxTemplateFilePath.Text = openFileDialog1.FileName;
            }
        }

        private void BtnReInitialFieldData_Click(object sender, RoutedEventArgs e)
        {
            BtnReInitialFieldData.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                var type = ComboBoxType.Text.ToUpper().Trim();
                InitialType(type);
                switch (type)
                {
                    case "FISC":
                        CreateFISCFieldInitialData();
                        break;
                    case "NCCC":
                        CreateNCCCFieldInitialData();
                        break;
                    case "TWIN":
                        CreateTWINFieldInitialData();
                        break;

                }
                SetMessageText(RichTextBoxLogMessage, $"Initial Data Field Completed", Brushes.Red);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, ex.Message);
                SetMessageText(RichTextBoxLogMessage, $"BtnReInitialFieldData_Click Exception:{ex.Message}", Brushes.Red);
            }
            finally
            {
                BtnReInitialFieldData.IsEnabled = true;
                Mouse.OverrideCursor = null;
            }
        }

        private void CreateFISCFieldInitialData()
        {
            DataGridISO8583EntryScreen.ItemsSource = null;
            string messageType = ComboBoxMessageType.SelectedItem as string;
            if (string.IsNullOrEmpty(messageType))
            {
                SetMessageText(RichTextBoxLogMessage, "Message Type Invalid", Brushes.Red);
                return;
            }
            GroupBoxMessage.Header = $"Message : {messageType}";
            switch (messageType.Substring(1, 1))
            {
                case "3":
                    TextBoxISOHeader.Text = $"ISO{08:00}{60:00}{000:000}{5:0}{1:0}";
                    break;
                case "8":
                    TextBoxISOHeader.Text = $"ISO{00:00}{60:00}{000:000}{6:0}{0:0}";
                    break;
                default:
                    TextBoxISOHeader.Text = $"ISO{00:00}{60:00}{000:000}{6:0}{0:0}";
                    break;
            }
            IEnumerable<XElement> messageTypeElements = _xmlDocumentMessageType.Descendants("MessageType");
            IEnumerable<XElement> requestMessage = messageTypeElements.Where(x => (x.Element("RequestMessage")!.Value.Equals(messageType)))
                                                    .Select(x => x);

            var fields = from lv1 in requestMessage.Elements("RequestField").Elements("Field")
                         select new
                         {
                             Number = lv1.Attribute("Number")?.Value,
                             DataCondition = lv1.Attribute("DataCondition")?.Value
                         };

            DataGridISO8583EntryScreen.Items.Clear();
            DataGridISO8583EntryScreen.Items.Refresh();
            var items = new List<ISOFieldInfo>();
            foreach (var field in fields)
            {
                int bicNum = Convert.ToInt16(field.Number.Trim());
                if (bicNum == 1) continue;
                string condition = field.DataCondition.Trim();
                var defaultValue = SetDefaultValue(bicNum);
                var isoFieldInfoData = new ISOFieldInfo
                {
                    BitNum = $"{bicNum,000}",
                    FieldName = _bicFieldList[bicNum - 1].FieldName,
                    FieldDataType = _bicFieldList[bicNum - 1].FieldDataType,
                    Length = _bicFieldList[bicNum - 1].Length,
                    FieldDataFormat = _bicFieldList[bicNum - 1].FieldDataFormat,
                    Description = _bicFieldList[bicNum - 1].Description,
                    Condition = condition,
                    EntryData = defaultValue
                };
                items.Add(isoFieldInfoData);
            }
            DataGridISO8583EntryScreen.ItemsSource = items;
        }

        private void CreateNCCCFieldInitialData()
        {
            DataGridISO8583EntryScreen.ItemsSource = null;
            string messageType = ComboBoxMessageType.SelectedItem as string;
            if (string.IsNullOrEmpty(messageType))
            {
                SetMessageText(RichTextBoxLogMessage, "Message Type Invalid", Brushes.Red);
                return;
            }
            GroupBoxMessage.Header = $"Message : {messageType}";
            switch (messageType.Substring(1, 1))
            {
                case "3":
                    TextBoxISOHeader.Text = $"ISO{08:00}{60:00}{000:000}{5:0}{1:0}";
                    break;
                case "8":
                    TextBoxISOHeader.Text = $"ISO{00:00}{60:00}{000:000}{6:0}{0:0}";
                    break;
                default:
                    TextBoxISOHeader.Text = $"ISO{00:00}{60:00}{000:000}{6:0}{0:0}";
                    break;
            }
            IEnumerable<XElement> messageTypeElements = _xmlDocumentMessageType.Descendants("MessageType");
            IEnumerable<XElement> requestMessage = messageTypeElements.Where(x => (x.Element("RequestMessage")!.Value.Equals(messageType)))
                                                    .Select(x => x);

            var fields = from lv1 in requestMessage.Elements("RequestField").Elements("Field")
                         select new
                         {
                             Number = lv1.Attribute("Number")?.Value,
                             DataCondition = lv1.Attribute("DataCondition")?.Value
                         };

            DataGridISO8583EntryScreen.Items.Clear();
            DataGridISO8583EntryScreen.Items.Refresh();
            var items = new List<ISOFieldInfo>();
            foreach (var field in fields)
            {
                int bicNum = Convert.ToInt16(field.Number.Trim());
                if (bicNum == 1) continue;
                string condition = field.DataCondition.Trim();
                var defaultValue = SetDefaultValue(bicNum);
                var isoFieldInfoData = new ISOFieldInfo
                {
                    BitNum = $"{bicNum,000}",
                    FieldName = _bicFieldList[bicNum - 1].FieldName,
                    FieldDataType = _bicFieldList[bicNum - 1].FieldDataType,
                    Length = _bicFieldList[bicNum - 1].Length,
                    FieldDataFormat = _bicFieldList[bicNum - 1].FieldDataFormat,
                    Description = _bicFieldList[bicNum - 1].Description,
                    Condition = condition,
                    EntryData = defaultValue
                };
                items.Add(isoFieldInfoData);
            }
            DataGridISO8583EntryScreen.ItemsSource = items;
        }

        private void CreateTWINFieldInitialData()
        {
            DataGridISO8583EntryScreen.ItemsSource = null;
            string messageType = ComboBoxMessageType.SelectedItem as string;
            if (string.IsNullOrEmpty(messageType))
            {
                SetMessageText(RichTextBoxLogMessage, "Message Type Invalid", Brushes.Red);
                return;
            }
            GroupBoxMessage.Header = $"Message : {messageType}";
            switch (messageType.Substring(1, 1))
            {
                case "3":
                    TextBoxISOHeader.Text = $"NCM{08:00}{01:00}{000:000}{0:0}{0:0}";
                    break;
                case "8":
                    TextBoxISOHeader.Text = $"NCM{00:00}{01:00}{000:000}{0:0}{0:0}";
                    break;
                default:
                    TextBoxISOHeader.Text = $"NCM{03:00}{01:00}{000:000}{0:0}{0:0}";
                    break;
            }
            IEnumerable<XElement> messageTypeElements = _xmlDocumentMessageType.Descendants("MessageType");
            IEnumerable<XElement> requestMessage = messageTypeElements.Where(x => (x.Element("RequestMessage")!.Value.Equals(messageType)))
                                                    .Select(x => x);

            var fields = from lv1 in requestMessage.Elements("RequestField").Elements("Field")
                         select new
                         {
                             Number = lv1.Attribute("Number")?.Value,
                             DataCondition = lv1.Attribute("DataCondition")?.Value
                         };

            DataGridISO8583EntryScreen.Items.Clear();
            DataGridISO8583EntryScreen.Items.Refresh();
            var items = new List<ISOFieldInfo>();
            foreach (var field in fields)
            {
                int bicNum = Convert.ToInt16(field.Number.Trim());
                if (bicNum == 1) continue;
                string condition = field.DataCondition.Trim();
                var defaultValue = SetDefaultValue(bicNum);
                var isoFieldInfoData = new ISOFieldInfo
                {
                    BitNum = $"{bicNum,000}",
                    FieldName = _bicFieldList[bicNum - 1].FieldName,
                    FieldDataType = _bicFieldList[bicNum - 1].FieldDataType,
                    Length = _bicFieldList[bicNum - 1].Length,
                    FieldDataFormat = _bicFieldList[bicNum - 1].FieldDataFormat,
                    Description = _bicFieldList[bicNum - 1].Description,
                    Condition = condition,
                    EntryData = defaultValue
                };
                items.Add(isoFieldInfoData);
            }
            DataGridISO8583EntryScreen.ItemsSource = items;
        }

        private void ComboBoxTokenId_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var tokenId = ComboBoxTokenId.SelectedValue.ToString().Trim();
            Label21.Content = "Expand Tree :";
            if (tokenIdFileDictionary.Count == 0) return;
            var tokenIdFileName = tokenIdFileDictionary[tokenId];
            if (tokenIdFileName == null) return;
            TextBoxTokenData.Text = tokenIdFileName;
            var extension = Path.GetExtension(tokenIdFileName).ToLower();
            using (var stream = new FileStream(tokenIdFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                //判斷格式套用讀取方法
                IExcelDataReader reader = null;
                if (extension == ".xls")
                {
                    SetMessageText(RichTextBoxTokenLogMessage, $"File {tokenIdFileName} => XLS Format", Brushes.Red);
                    reader = ExcelReaderFactory.CreateBinaryReader(stream, new ExcelReaderConfiguration()
                    {
                        FallbackEncoding = Encoding.GetEncoding("big5")
                    });
                }
                else if (extension == ".xlsx")
                {
                    SetMessageText(RichTextBoxTokenLogMessage, $"File {tokenIdFileName} => XLSX Format", Brushes.Red);
                    reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                }
                else if (extension == ".csv")
                {
                    SetMessageText(RichTextBoxTokenLogMessage, $"File {tokenIdFileName} => CSV Format", Brushes.Red);
                    reader = ExcelReaderFactory.CreateCsvReader(stream, new ExcelReaderConfiguration()
                    {
                        FallbackEncoding = Encoding.GetEncoding("big5")
                    });
                }
                else if (extension == ".txt")
                {
                    SetMessageText(RichTextBoxTokenLogMessage, $"File {tokenIdFileName} => Text(Tab Separated) Format",
                        Brushes.Red);
                    reader = ExcelReaderFactory.CreateCsvReader(stream, new ExcelReaderConfiguration()
                    {
                        FallbackEncoding = Encoding.GetEncoding("big5"),
                        AutodetectSeparators = new char[] { '\t' }
                    });
                }

                //沒有對應產生任何格式
                if (reader == null)
                {

                    SetMessageText(RichTextBoxTokenLogMessage, $"File {tokenIdFileName} => File Read Error", Brushes.Red);
                    return;
                }

                SetMessageText(RichTextBoxTokenLogMessage, $"File {tokenIdFileName} =>  Transform", Brushes.Red);
                using (reader)
                {

                    var ds = reader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        UseColumnDataType = false,
                        ConfigureDataTable = (tableReader) => new ExcelDataTableConfiguration()
                        {
                            //設定讀取資料時是否忽略標題
                            UseHeaderRow = false
                        }
                    });


                    //把 DataSet 顯示出來
                    var table = ds.Tables[0];
                    DataGridTokenEntryScreen.ItemsSource = null;
                    DataGridTokenEntryScreen.Items.Clear();
                    DataGridTokenEntryScreen.Items.Refresh();
                    var items = new List<TokenFieldInfo>();
                    for (int row = 0; row < table.Rows.Count; row++)
                    {
                        var tokenData = new TokenFieldInfo();
                        if (string.IsNullOrEmpty(table.Rows[row][0].ToString().ToString().Trim())) continue;
                        if (table.Rows[row][0].ToString().Trim().ToUpper().Equals("LEVEL")) continue;
                        tokenData.Level = table.Rows[row][0].ToString();
                        tokenData.FieldId = table.Rows[row][1].ToString();
                        tokenData.DataLength = table.Rows[row][2].ToString();
                        tokenData.DataType = table.Rows[row][3].ToString();
                        tokenData.Value = table.Rows[row][4].ToString();
                        tokenData.FieldName = table.Rows[row][5].ToString();
                        tokenData.Description = table.Rows[row][6].ToString();
                        tokenData.EntryData = string.Empty;
                        items.Add(tokenData);
                    }
                    DataGridTokenEntryScreen.ItemsSource = items;
                }
            }
            SetMessageText(RichTextBoxTokenLogMessage, $"File {tokenIdFileName} =>  End", Brushes.Red);
            var tempString = Label21.Content.ToString();
            Label21.Content = tempString.Replace(":", $"{tokenId} :");
        }

        private void BtnAddToken_Click(object sender, RoutedEventArgs e)
        {
            if (DataGridTokenEntryScreen?.ItemsSource == null) return;
            for (int index = 0; index < DataGridTokenEntryScreen.Items.Count; index++)
            {
                DataGridRow row = (DataGridRow)DataGridTokenEntryScreen.ItemContainerGenerator.ContainerFromIndex(index);
                foreach (DataGridColumn column in DataGridTokenEntryScreen.Columns)
                {
                    if (column.GetCellContent(row) is TextBlock)
                    {
                        TextBlock cellContent = column.GetCellContent(row) as TextBlock;
                        SetMessageText(RichTextBoxTokenLogMessage, $"Header[{column.Header}] {cellContent.GetType().ToString()} =>  {cellContent.Text}", Brushes.Red);
                    }
                }
            }
        }

        private void BtnExpandTree_Click(object sender, RoutedEventArgs e)
        {
            var tokenIdFileName = TextBoxTokenData.Text.Trim();
            List<TokenFieldInfo> itemsList = new List<TokenFieldInfo>();
            if (string.IsNullOrEmpty(tokenIdFileName))
            {
                SetMessageText(RichTextBoxTokenTreeLogMessage, $"Token File [{tokenIdFileName}] Invalid, No Process", Brushes.Red);
                return;
            }
            var extension = Path.GetExtension(tokenIdFileName).ToLower();
            using (var stream = new FileStream(tokenIdFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                //判斷格式套用讀取方法
                IExcelDataReader reader = null;
                if (extension == ".xls")
                {
                    SetMessageText(RichTextBoxTokenTreeLogMessage, $"File {tokenIdFileName} => XLS Format", Brushes.Red);
                    reader = ExcelReaderFactory.CreateBinaryReader(stream, new ExcelReaderConfiguration()
                    {
                        FallbackEncoding = Encoding.GetEncoding("big5")
                    });
                }
                else if (extension == ".xlsx")
                {
                    SetMessageText(RichTextBoxTokenTreeLogMessage, $"File {tokenIdFileName} => XLSX Format", Brushes.Red);
                    reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                }
                else if (extension == ".csv")
                {
                    SetMessageText(RichTextBoxTokenTreeLogMessage, $"File {tokenIdFileName} => CSV Format", Brushes.Red);
                    reader = ExcelReaderFactory.CreateCsvReader(stream, new ExcelReaderConfiguration()
                    {
                        FallbackEncoding = Encoding.GetEncoding("big5")
                    });
                }
                else if (extension == ".txt")
                {
                    SetMessageText(RichTextBoxTokenTreeLogMessage, $"File {tokenIdFileName} => Text(Tab Separated) Format", Brushes.Red);
                    reader = ExcelReaderFactory.CreateCsvReader(stream, new ExcelReaderConfiguration()
                    {
                        FallbackEncoding = Encoding.GetEncoding("big5"),
                        AutodetectSeparators = new char[] { '\t' }
                    });
                }

                //沒有對應產生任何格式
                if (reader == null)
                {

                    SetMessageText(RichTextBoxTokenTreeLogMessage, $"File {tokenIdFileName} => File Read Error", Brushes.Red);
                    return;
                }

                SetMessageText(RichTextBoxTokenTreeLogMessage, $"File {tokenIdFileName} =>  Transform", Brushes.Red);
                using (reader)
                {

                    var ds = reader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        UseColumnDataType = false,
                        ConfigureDataTable = (tableReader) => new ExcelDataTableConfiguration()
                        {
                            //設定讀取資料時是否忽略標題
                            UseHeaderRow = false
                        }
                    });


                    //把 DataSet 顯示出來
                    var table = ds.Tables[0];
                    for (int row = 0; row < table.Rows.Count; row++)
                    {
                        var tokenData = new TokenFieldInfo();
                        if (string.IsNullOrEmpty(table.Rows[row][0].ToString().ToString().Trim())) continue;
                        if (table.Rows[row][0].ToString().Trim().ToUpper().Equals("LEVEL")) continue;
                        tokenData.Level = table.Rows[row][0].ToString();
                        tokenData.FieldId = table.Rows[row][1].ToString();
                        tokenData.DataLength = table.Rows[row][2].ToString();
                        tokenData.DataType = table.Rows[row][3].ToString();
                        tokenData.Value = table.Rows[row][4].ToString();
                        tokenData.FieldName = table.Rows[row][5].ToString();
                        tokenData.Description = table.Rows[row][6].ToString();
                        tokenData.EntryData = string.Empty;
                        itemsList.Add(tokenData);
                    }
                }
            }
            SetMessageText(RichTextBoxTokenTreeLogMessage, $"File {tokenIdFileName} =>  End", Brushes.Red);

            if (!itemsList[0].Level.Equals("1"))
            {
                SetMessageText(RichTextBoxTokenTreeLogMessage, $"File {tokenIdFileName} Error", Brushes.Red);
                return;
            }

            var testLength = RecursiveGetElementLength(itemsList, 1, "1");
            SetMessageText(RichTextBoxTokenTreeLogMessage, $"RecursiveGetElementLength[{testLength}]", Brushes.Red);

            TreeViewItem root = new TreeViewItem()
            {
                Header = new StackPanel
                {
                    Orientation = System.Windows.Controls.Orientation.Horizontal,
                    Children =
                    {
                        new Border
                        {
                            Width = 12,
                            Height = 12,
                            Background = Brushes.Red,
                            BorderThickness = new Thickness(1.0),
                            BorderBrush = Brushes.Black
                        },

                        new System.Windows.Controls.Label
                        {
                           Content = $"{itemsList[0].FieldId}--{Path.GetFileNameWithoutExtension(tokenIdFileName)}",
                           Foreground = Brushes.Blue,
                        }
                    }
                }
            };

            //root.Header = $"{itemsList[0].FieldId}--{Path.GetFileNameWithoutExtension(tokenIdFileName)}"  ;
            root.Tag = itemsList[0].Level;
            TreeView1.Items.Add(root);
            var outerString = itemsList.Where(x => x.Level.Equals("2")).ToList();
            List<string> outerItemLists = new List<string>();
            foreach (var outerItem in outerString)
            {
                var innerDetail1 = new TreeViewItem();
                if (outerItem.DataType.ToUpper().Trim().Equals("CO"))
                    innerDetail1.Header = outerItem.FieldId;
                else
                    innerDetail1.Header = $"{outerItem.FieldId}--{outerItem.DataLength}";
                innerDetail1.Tag = outerItem.Level;
                root.Items.Add(innerDetail1);
                if (outerItem.DataType.ToUpper().Trim().Equals("CO"))
                {
                    string innerLevel1 = Convert.ToString(Convert.ToInt16(outerItem.Level.Trim()) + 1);
                    var inner1Strings = itemsList.Where(x => x.Level.Equals(innerLevel1)).ToList();
                    List<string> inner1ItemLists = new List<string>();
                    foreach (var inner1Item in inner1Strings)
                    {
                        var innerDetail2 = new TreeViewItem();
                        if (inner1Item.DataType.ToUpper().Trim().Equals("CO"))
                            innerDetail2.Header = inner1Item.FieldId;
                        else
                            innerDetail2.Header = $"{inner1Item.FieldId}--{inner1Item.DataLength}";
                        innerDetail2.Tag = inner1Item.Level;
                        //inner.ItemsSource = innerStrings;
                        innerDetail1.Items.Add(innerDetail2);
                        if (inner1Item.DataType.ToUpper().Trim().Equals("CO"))
                        {
                            string innerLevel2 = Convert.ToString(Convert.ToInt16(inner1Item.Level.Trim()) + 1);
                            var inner2Strings = itemsList.Where(x => x.Level.Equals(innerLevel2)).ToList();
                            List<string> inner2ItemLists = new List<string>();
                            foreach (var inner2Item in inner2Strings)
                            {
                                var inner2 = new TreeViewItem();
                                if (inner2Item.DataType.ToUpper().Trim().Equals("CO"))
                                    inner2.Header = inner2Item.FieldId;
                                else
                                    inner2.Header = $"{inner2Item.FieldId}--{inner2Item.DataLength}";

                                innerDetail2.Tag = inner2Item.Level;
                                //inner.ItemsSource = innerStrings;
                                innerDetail2.Items.Add(inner2);
                            }
                        }
                        else
                        {
                            inner1ItemLists.Add(inner1Item.FieldId);
                        }
                    }
                }
                else
                {
                    outerItemLists.Add(outerItem.FieldId);
                }
            }
            //if (itemLists.Count > 0) root.ItemsSource = itemLists;
            var itemArray = GetTreeViewItems(TreeView1);
            foreach (var item in itemArray)
            {
                SetMessageText(RichTextBoxTokenTreeLogMessage, $"Item[{item.Header}", Brushes.Red);
            }
        }
        private TreeViewItem FindNode(ItemCollection items, string tag)
        {
            TreeViewItem oResult = null;

            foreach (var oItem in items)
            {
                TreeViewItem oTreeViewItem = (TreeViewItem)oItem;

                if ((string)oTreeViewItem.Tag == tag) { oResult = oTreeViewItem; break; }

                if (oTreeViewItem.Items.Count > 0)
                {
                    oResult = FindNode(oTreeViewItem.Items, tag);

                    if (oResult != null) { break; }
                }
            }
            return oResult;
        }
        private TreeViewItem[] GetTreeViewItems(ItemsControl treeView)
        {
            List<TreeViewItem> returnItems = new List<TreeViewItem>();
            for (int x = 0; x < treeView.Items.Count; x++)
            {
                returnItems.AddRange(GetTreeViewItems((TreeViewItem)treeView.Items[x]));
            }
            return returnItems.ToArray();
        }
        private TreeViewItem[] GetTreeViewItems(TreeViewItem currentTreeViewItem)
        {
            List<TreeViewItem> returnItems = new List<TreeViewItem>();
            returnItems.Add(currentTreeViewItem);
            for (int x = 0; x < currentTreeViewItem.Items.Count; x++)
            {
                returnItems.AddRange(GetTreeViewItems((TreeViewItem)currentTreeViewItem.Items[x]));
            }
            return returnItems.ToArray();
        }
        private void GetISOBytes()
        {
            try
            {
                _getISOBytes = null;
                var fileName = TextBoxParserFilePath.Text.Trim();
                if (!File.Exists(fileName))
                {
                    SetMessageText(RichTextBoxTokenTreeLogMessage, $"File[{fileName}] Not Found", Brushes.Red);
                    TextBoxParserFilePath.Focus();
                    _getISOBytes = null;
                }
                var readBytes = File.ReadAllBytes(fileName);
                _getISOBytes = new byte[readBytes.Length];
                Buffer.BlockCopy(readBytes, 0, _getISOBytes, 0, readBytes.Length);
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxTokenTreeLogMessage, ex.Message, Brushes.Red);
                _getISOBytes = null;
            }
        }
        private void CreateCETokenTreeView(Token token)
        {
            var tokenCEData = (TM.ISO8583.Nccc.Tokens.TokenCE)token;
            var tokenHeader = token.Encode().Substring(0, 10);
            var tokenId = tokenHeader.Substring(2, 2);
            var tokenDataLength = Convert.ToInt16(tokenHeader.Substring(4, 5));
            var tokenData = token.Encode().Substring(10);
            TreeViewItem root = new TreeViewItem()
            {
                Header = new StackPanel
                {
                    Orientation = System.Windows.Controls.Orientation.Horizontal,
                    Children =
                    {
                        new Border
                        {
                            Width = 12,
                            Height = 12,
                            Background = Brushes.Red,
                            BorderThickness = new Thickness(1.0),
                            BorderBrush = Brushes.Black
                        },

                        new System.Windows.Controls.Label
                        {
                           Content = $"Token Data[CE][{tokenHeader}]",
                           Foreground = Brushes.Blue,
                        }
                    }
                }
            };
            root.Tag = "1";
            TreeView1.Items.Add(root);
            TreeViewItem treeViewItem = new TreeViewItem();
            treeViewItem.Header = $"AUTHN-IND-FLG[2] ---> [{tokenData.Substring(0, 2)}]";
            root.Items.Add(treeViewItem);
            treeViewItem = new TreeViewItem();
            treeViewItem.Header = $"AUTHN-IND-DATA[{tokenData.Length - 2}] ---> [{tokenData.Substring(2)}]";
            root.Items.Add(treeViewItem);
        }
        private void CreateQRTokenTreeView(Token token)
        {
            var tokenQRData = (TM.ISO8583.Nccc.Tokens.TokenQR)token;
            var tokenDictionary = tokenQRData.GetSubTokenData();

            TreeViewItem root = new TreeViewItem()
            {
                Header = new StackPanel
                {
                    Orientation = System.Windows.Controls.Orientation.Horizontal,
                    Children =
                    {
                        new Border
                        {
                            Width = 12,
                            Height = 12,
                            Background = Brushes.Red,
                            BorderThickness = new Thickness(1.0),
                            BorderBrush = Brushes.Black
                        },

                        new System.Windows.Controls.Label
                        {
                           Content = "Token Data[QR]",
                           Foreground = Brushes.Blue,
                        }
                    }
                }
            };
            root.Tag = "1";
            TreeView1.Items.Add(root);
            foreach (var data in tokenDictionary)
            {
                var tokenId = data.Key;
                var tokenData = data.Value;
                TreeViewItem tagTreeViewItem = new TreeViewItem();
                tagTreeViewItem.Header = $"TagId[{data.Key}]]";
                TreeViewItem tagIdTreeViewItem = new TreeViewItem();
                tagIdTreeViewItem.Header = $"TagId[{tokenData.Substring(0, 2)}]";
                TreeViewItem tagIdLengthTreeViewItem = new TreeViewItem();
                tagIdLengthTreeViewItem.Header = $"TagId[{tokenData.Substring(2, 3)}]";
                TreeViewItem tagIdDataTreeViewItem = new TreeViewItem();
                tagIdDataTreeViewItem.Header = $"TagId[{tokenData.Substring(5, tokenData.Length - 1)}]";
                tagTreeViewItem.Items.Add(tagIdTreeViewItem);
                tagTreeViewItem.Items.Add(tagIdLengthTreeViewItem);
                tagTreeViewItem.Items.Add(tagIdDataTreeViewItem);
                root.Items.Add(tagTreeViewItem);
            }
            //
            ////root.Header = $"{itemsList[0].FieldId}--{Path.GetFileNameWithoutExtension(tokenIdFileName)}"  ;
            //root.Tag = itemsList[0].Level;
            //TreeView1.Items.Add(root);
            //int startPosition  = 0;
            //int dataLength = 0;
            //for (var index = 1; index < itemsList.Count; index++)
            //{
            //    if (!itemsList[index].Level.Trim().Equals("2"))
            //        continue;
            //    TreeViewItem innerTreeViewItem;
            //    if (itemsList[index].DataType.ToUpper().Trim().Equals("CO"))
            //    {
            //        dataLength = RecursiveGetElementLength(itemsList, index + 1, "2");
            //        var subData = tokenIdDetailData.Substring(startPosition, dataLength);
            //        innerTreeViewItem = new TreeViewItem();
            ////        innerTreeViewItem.Header = $"{itemsList[index].FieldId}[{dataLength}] ---> [{subData}]";
            ////        innerTreeViewItem.Tag = itemsList[index].Level;
            //        startPosition += dataLength;
            //    }
            //    else
            //    {
            //        dataLength = Convert.ToInt16(itemsList[index].DataLength.Trim());
            //        var subData = tokenIdDetailData.Substring(startPosition, dataLength);
            //        innerTreeViewItem = new TreeViewItem();
            //        innerTreeViewItem.Header = $"{itemsList[index].FieldId}[{itemsList[index].DataLength}] ---> [{subData}]";
            //        innerTreeViewItem.Tag = itemsList[index].Level;
            //        startPosition += dataLength;
            //    }
            //    root.Items.Add(innerTreeViewItem);
            //}
        }
        private void CreateTokenTreeView(string tokenIdFileName, string tokenIdData)
        {
            var tokenIdHeader = tokenIdData.Substring(0, 10);
            var tokenIdDetailData = tokenIdData.Substring(10);
            List<TokenFieldInfo> itemsList = new List<TokenFieldInfo>();
            if (string.IsNullOrEmpty(tokenIdFileName))
            {
                SetMessageText(RichTextBoxTokenTreeLogMessage, $"File {tokenIdFileName} Invalid", Brushes.Red);
                return;
            }
            var extension = Path.GetExtension(tokenIdFileName).ToLower();
            using (var stream = new FileStream(tokenIdFileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                //判斷格式套用讀取方法
                IExcelDataReader reader = null;
                if (extension == ".xls")
                {
                    SetMessageText(RichTextBoxTokenTreeLogMessage, $"File {tokenIdFileName} => XLS Format", Brushes.Red);
                    reader = ExcelReaderFactory.CreateBinaryReader(stream, new ExcelReaderConfiguration()
                    {
                        FallbackEncoding = Encoding.GetEncoding("big5")
                    });
                }
                else if (extension == ".xlsx")
                {
                    SetMessageText(RichTextBoxTokenTreeLogMessage, $"File {tokenIdFileName} => XLSX Format", Brushes.Red);
                    reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                }
                else if (extension == ".csv")
                {
                    SetMessageText(RichTextBoxTokenTreeLogMessage, $"File {tokenIdFileName} => CSV Format", Brushes.Red);
                    reader = ExcelReaderFactory.CreateCsvReader(stream, new ExcelReaderConfiguration()
                    {
                        FallbackEncoding = Encoding.GetEncoding("big5")
                    });
                }
                else if (extension == ".txt")
                {
                    SetMessageText(RichTextBoxTokenTreeLogMessage, $"File {tokenIdFileName} => Text(Tab Separated) Format", Brushes.Red);
                    reader = ExcelReaderFactory.CreateCsvReader(stream, new ExcelReaderConfiguration()
                    {
                        FallbackEncoding = Encoding.GetEncoding("big5"),
                        AutodetectSeparators = new char[] { '\t' }
                    });
                }

                //沒有對應產生任何格式
                if (reader == null)
                {

                    SetMessageText(RichTextBoxTokenTreeLogMessage, $"File {tokenIdFileName} => File Read Error", Brushes.Red);
                    return;
                }

                SetMessageText(RichTextBoxTokenTreeLogMessage, $"File {tokenIdFileName} =>  Transform", Brushes.Red);
                using (reader)
                {

                    var ds = reader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        UseColumnDataType = false,
                        ConfigureDataTable = (tableReader) => new ExcelDataTableConfiguration()
                        {
                            //設定讀取資料時是否忽略標題
                            UseHeaderRow = false
                        }
                    });


                    //把 DataSet 顯示出來
                    var table = ds.Tables[0];
                    for (int row = 0; row < table.Rows.Count; row++)
                    {
                        var tokenData = new TokenFieldInfo();
                        if (string.IsNullOrEmpty(table.Rows[row][0].ToString().ToString().Trim())) continue;
                        if (table.Rows[row][0].ToString().Trim().ToUpper().Equals("LEVEL")) continue;
                        tokenData.Level = table.Rows[row][0].ToString();
                        tokenData.FieldId = table.Rows[row][1].ToString();
                        tokenData.DataLength = table.Rows[row][2].ToString();
                        tokenData.DataType = table.Rows[row][3].ToString();
                        tokenData.Value = table.Rows[row][4].ToString();
                        tokenData.FieldName = table.Rows[row][5].ToString();
                        tokenData.Description = table.Rows[row][6].ToString();
                        tokenData.EntryData = string.Empty;
                        itemsList.Add(tokenData);
                    }
                }
            }
            SetMessageText(RichTextBoxTokenTreeLogMessage, $"File {tokenIdFileName} =>  End", Brushes.Red);

            if (!itemsList[0].Level.Equals("1"))
            {
                SetMessageText(RichTextBoxTokenTreeLogMessage, $"File {tokenIdFileName} Error", Brushes.Red);
                return;
            }

            TreeViewItem root = new TreeViewItem()
            {
                Header = new StackPanel
                {
                    Orientation = System.Windows.Controls.Orientation.Horizontal,
                    Children =
                    {
                        new Border
                        {
                            Width = 12,
                            Height = 12,
                            Background = Brushes.Red,
                            BorderThickness = new Thickness(1.0),
                            BorderBrush = Brushes.Black
                        },

                        new System.Windows.Controls.Label
                        {
                           Content = $"{itemsList[0].FieldId}[{Path.GetFileNameWithoutExtension(tokenIdFileName.ToUpper().Trim().Replace("TOKEN",""))}][{tokenIdHeader}]",
                           Foreground = Brushes.Blue,
                        }
                    }
                }
            };

            //root.Header = $"{itemsList[0].FieldId}--{Path.GetFileNameWithoutExtension(tokenIdFileName)}"  ;
            root.Tag = itemsList[0].Level;
            TreeView1.Items.Add(root);
            int startPosition = 0;
            int dataLength = 0;
            for (var index = 1; index < itemsList.Count; index++)
            {
                if (!itemsList[index].Level.Trim().Equals("2"))
                    continue;
                TreeViewItem innerTreeViewItem;
                if (itemsList[index].DataType.ToUpper().Trim().Equals("CO"))
                {
                    dataLength = RecursiveGetElementLength(itemsList, index + 1, "2");
                    var subData = tokenIdDetailData.Substring(startPosition, dataLength);
                    innerTreeViewItem = new TreeViewItem();
                    innerTreeViewItem.Header = $"{itemsList[index].FieldId}[{dataLength}] ---> [{subData}]";
                    innerTreeViewItem.Tag = itemsList[index].Level;
                    startPosition += dataLength;
                }
                else
                {
                    dataLength = Convert.ToInt16(itemsList[index].DataLength.Trim());
                    var subData = tokenIdDetailData.Substring(startPosition, dataLength);
                    innerTreeViewItem = new TreeViewItem();
                    innerTreeViewItem.Header = $"{itemsList[index].FieldId}[{itemsList[index].DataLength}] ---> [{subData}]";
                    innerTreeViewItem.Tag = itemsList[index].Level;
                    startPosition += dataLength;
                }
                root.Items.Add(innerTreeViewItem);
            }
        }
        private int RecursiveGetElementLength(List<TokenFieldInfo> itemsList, int startIndex, string startLevel)
        {
            int sumOfDataElementLength = 0;
            string nextLevel = Convert.ToString(Convert.ToInt16(startLevel.Trim()) + 1);
            bool haveGroupLevel = false;
            for (var index = startIndex; index < itemsList.Count; index++)
            {
                //if (!ItemsList[index].Level.Equals(nextLevel)) break;
                if (itemsList[index].DataType.ToUpper().Trim().Equals("CO"))
                {
                    var groupLength = RecursiveGetElementLength(itemsList, index + 1, nextLevel);
                    sumOfDataElementLength += groupLength;
                    haveGroupLevel = true;
                }
                else
                {
                    if (itemsList[index].Level.Equals(nextLevel))
                    {
                        var length = Convert.ToInt16(itemsList[index].DataLength);
                        sumOfDataElementLength += length;
                    }
                }
            }

            if (!haveGroupLevel) return sumOfDataElementLength;
            return sumOfDataElementLength;
        }
        private void BtnParserData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                GetISOBytes();
                if (_getISOBytes is null || _getISOBytes.Length == 0)
                {
                    SetMessageText(RichTextBoxTokenTreeLogMessage, $"BtnParserData_Click Input Data Error", Brushes.Red);
                    return;
                }

                var nccc = new NcccIso8583(_getISOBytes);
                SetMessageText(RichTextBoxTokenTreeLogMessage, $"--- Begin Parsing NCCC Message ---", Brushes.Red);
                SetMessageText(RichTextBoxTokenTreeLogMessage, $"Header[{nccc.Header().Encode()}]", Brushes.Black);
                SetMessageText(RichTextBoxTokenTreeLogMessage, $"BitMap[{HexStringConverter.FromByteArray(nccc.Bitmap())}]", Brushes.Black);
                SetMessageText(RichTextBoxTokenTreeLogMessage, $"MTI [{nccc.Mti()}]", Brushes.Black);
                foreach (var pair in nccc.Core().OrderedFields())
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    if (value is byte[] bytes)
                    {
                        var tempString = HexStringConverter.FromByteArray(bytes);
                        SetMessageText(RichTextBoxTokenTreeLogMessage, $"FIELD [{key}] VALUE [{tempString}]", Brushes.Black);
                    }
                    else
                    {
                        SetMessageText(RichTextBoxTokenTreeLogMessage, $"FIELD [{key}] Field Position [{FIELDMethods.Val(key)}] VALUE [{value}]", Brushes.Black);
                    }
                }
                SetMessageText(RichTextBoxTokenTreeLogMessage, $"--- End Parsing NCCC Message ---", Brushes.Red);


                SetMessageText(RichTextBoxTokenTreeLogMessage, $"--- Begin Parsing NCCC Token Message ---", Brushes.Red);
                var field = nccc.GetTokenFieldId();
                var tokenGroup = (TokenGroup)nccc.Core().GetOthersValByKey(field.ToString());
                if (tokenGroup == null) return;
                tokenGroup.ShowTokens();
                foreach (var token in tokenGroup.Tokens)
                {
                    SetMessageText(RichTextBoxTokenTreeLogMessage, $"[Token{token.Key}] = [{token.Value.Encode()}]", Brushes.Red);
                    var tokenHeader = token.Value.Encode().Substring(0, 10);
                    var tokenId = tokenHeader.Substring(2, 2);
                    var tokenDataLength = Convert.ToInt16(tokenHeader.Substring(4, 5));
                    var tokenData = token.Value.Encode().Substring(10);
                    var tokenConfigFilePath = $"{_tokenDefineFilePath}\\token{tokenId}.xls";
                    SetMessageText(RichTextBoxTokenTreeLogMessage, $"Header[{tokenHeader}], ID[{tokenId}], DataLength[{tokenDataLength}], Data[{tokenData}], ConfigFilePath[{tokenConfigFilePath}]", Brushes.Red);
                }
                foreach (var token in tokenGroup.Tokens)
                {
                    if (token.Key.Equals("CE"))
                        CreateCETokenTreeView(token.Value);
                    else if (token.Key.Equals("QR"))
                        CreateQRTokenTreeView(token.Value);
                    else
                    {
                        if (!tokenIdFileDictionary.ContainsKey(token.Key))
                        {
                            SetMessageText(RichTextBoxTokenTreeLogMessage, $"No Token[{token.Key}] Configuration", Brushes.Red);
                            continue;
                        }
                        var tokenIdFilePath = tokenIdFileDictionary[token.Key];
                        var tokenData = token.Value.Encode();
                        CreateTokenTreeView(tokenIdFilePath, tokenData);
                    }
                }
                SetMessageText(RichTextBoxTokenTreeLogMessage, $"--- End Parsing NCCC Token Message ---", Brushes.Red);
            }
            catch (Exception ex)
            {
                SetMessageText(RichTextBoxTokenTreeLogMessage, ex.Message, Brushes.Red);
            }
        }
        private void BtnSelectParserFilePath_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

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
                TextBoxParserFilePath.Text = openFileDialog1.FileName;
            }
        }
        private void BtnCleanTokenTreeLogMessage_Click(object sender, RoutedEventArgs e)
        {
            RichTextBoxTokenTreeLogMessage.Document.Blocks.Clear();
        }

        private void BtnCleanTokenLog_Click(object sender, RoutedEventArgs e)
        {
            RichTextBoxTokenLogMessage.Document.Blocks.Clear();
        }

        private void BtnCleanTokenExpandTreeLog_Click(object sender, RoutedEventArgs e)
        {
            TreeView1.Items.Clear();
        }

        private void BtnCreateP57_Click(object sender, RoutedEventArgs e)
        {
            string initDirPath = @"E:\FirstBank\Development\CyberCOLA\src\CyberCOLA\WpfStressSimulator\Data\Nccc";
            string outPutDir = @"E:\FirstBank\Development\CyberCOLA\src\CyberCOLA\WpfStressSimulator\Data\Nccc";
            string[] fileArray = Directory.GetFiles(initDirPath, "0*.txt");
            Mouse.OverrideCursor = Cursors.Wait;
            foreach (string filePath in fileArray)
            {
                try
                {
                    var readBytes = File.ReadAllBytes(filePath);
                    if (readBytes == null)
                    {
                        SetMessageText(RichTextBoxLogMessage, $"Skip File Path [{filePath}]", Brushes.Red);
                        continue;
                    }
                    var nccc = new NcccIso8583(readBytes);
                    if (nccc == null)
                    {
                        SetMessageText(RichTextBoxLogMessage, $"Skip File Path [{filePath}] Create NcccIso8593 error", Brushes.Red);
                        continue;
                    }
                    var field = FIELD.F_63;
                    if (!ObjectUtils.IsNull(nccc.RawVal(field)))
                    {
                        nccc.Core().Field(FIELD.F_57, nccc.Core().Field(field));
                        var iso8583Bytes = nccc.Encode().Val();
                        string outPutFilePath = outPutDir + @"\" + Path.GetFileNameWithoutExtension(filePath) + "_P63_P57.txt";
                        File.WriteAllBytes(outPutFilePath, iso8583Bytes);
                        SetMessageText(RichTextBoxLogMessage, $"Write File Path [{outPutFilePath}], Length [{iso8583Bytes.Length}]", Brushes.Red);
                        nccc.Purge(FIELD.F_63);
                        iso8583Bytes = nccc.Encode().Val();
                        outPutFilePath = outPutDir + @"\" + Path.GetFileNameWithoutExtension(filePath) + "_P57.txt";
                        File.WriteAllBytes(outPutFilePath, iso8583Bytes);
                        SetMessageText(RichTextBoxLogMessage, $"Write File Path [{outPutFilePath}], Length [{iso8583Bytes.Length}]", Brushes.Red);
                    }
                }
                catch (Exception ex)
                {
                    SetMessageText(RichTextBoxLogMessage, ex.Message, Brushes.Red);
                }
            }
            Mouse.OverrideCursor = null;
        }

        private void CreateFiscISOData()
        {
            BtnCreateISOData.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                var fiscMessage = new FISC();
                fiscMessage.Mti(ComboBoxMessageType.Text);
                for (int index = 0; index < DataGridISO8583EntryScreen?.Items.Count; index++)
                {
                    if (!(DataGridISO8583EntryScreen.Items[index] is ISOFieldInfo))
                    {
                        SetMessageText(RichTextBoxLogMessage, $"Skip Index[{index}] invalid data !!!", Brushes.Red);
                        continue;
                    }
                    ISOFieldInfo item = (ISOFieldInfo)DataGridISO8583EntryScreen.Items[index];
                    if (item == null || string.IsNullOrEmpty(item.EntryData))
                    {
                        SetMessageText(RichTextBoxLogMessage, $"Skip Field[{item.BitNum}] no entry data !!!", Brushes.Red);
                        continue;
                    }
                    SetMessageText(RichTextBoxLogMessage, $"Process Index[{index}]", Brushes.Red);
                    if (item.Condition.Equals("M"))
                    {
                        if (string.IsNullOrEmpty(item.EntryData.Trim()))
                        {
                            SetFocusOnGrid(DataGridISO8583EntryScreen, index);
                            SetMessageText(RichTextBoxLogMessage, $"Field[{item.BitNum}] must be enter data !!!", Brushes.Red);
                            return;
                        }
                    }
                    if (item.FieldDataFormat.ToUpper().Equals("FIXED"))
                    {
                        var defineDataLength = Convert.ToInt16(item.Length.Trim());
                        if (defineDataLength != item.EntryData.Length)
                        {
                            SetFocusOnGrid(DataGridISO8583EntryScreen, index);
                            SetMessageText(RichTextBoxLogMessage, $"Field[{item.BitNum}] entry data length[{item.EntryData.Length}] not equal to define length[{defineDataLength}] !!!", Brushes.Red);
                            return;
                        }
                    }
                    if (item.FieldDataType.ToUpper().Equals("N"))
                    {
                        var isNumeric = item.EntryData.All(char.IsDigit);
                        if (!isNumeric)
                        {
                            SetFocusOnGrid(DataGridISO8583EntryScreen, index);
                            SetMessageText(RichTextBoxLogMessage, $"Field[{item.BitNum}] entry data[{item.EntryData}] is not numeric !!!", Brushes.Red);
                            return;
                        }
                    }
                    var field = FIELDMethods.Val(Convert.ToInt32(item.BitNum.Trim()));
                    fiscMessage.Core().Field(field, item.EntryData);
                }
                var iso8583Bytes = fiscMessage.Encode().Val();
                SetMessageText(RichTextBoxLogMessage, $" --- ISO8583 Data ---", Brushes.Red);
                SetMessageText(RichTextBoxLogMessage, $"Create FISC　ISO8583 Data[{Encoding.ASCII.GetString(iso8583Bytes)}]", Brushes.Black);
                SetMessageText(RichTextBoxLogMessage, $" --- ISO8583 Data ---", Brushes.Red);
                SetMessageText(RichTextBoxLogMessage, $" --- Begin Parsing FISC ISO8583 Data ---", Brushes.Red);
                var fiscRequestMessage = new FISC(iso8583Bytes);
                foreach (var pair in fiscRequestMessage.Core().OrderedFields())
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    if (value is byte[] bytes)
                    {
                        var tempString = HexStringConverter.FromByteArray(bytes);
                        SetMessageText(RichTextBoxLogMessage, $"FIELD [{key}] VALUE [{tempString}]", Brushes.Black);
                    }
                    else
                    {
                        SetMessageText(RichTextBoxLogMessage, $"FIELD [{key}] Field Position [{FIELDMethods.Val(key)}] VALUE [{value}]", Brushes.Black);
                    }
                }
                SetMessageText(RichTextBoxLogMessage, $" --- End Parsing FISC ISO8583 Data ---", Brushes.Red);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, ex.Message);
                SetMessageText(RichTextBoxLogMessage, $"BtnCreateISOData_Click Exception:{ex.Message}", Brushes.Red);
            }
            finally
            {
                BtnCreateISOData.IsEnabled = true;
                Mouse.OverrideCursor = null;
            }
        }

        private void CreateNcccISOData()
        {
            BtnCreateISOData.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                var ncccMessage = new NcccIso8583();
                if (TextBoxISOHeader.Text.Length != 12)
                {
                    TextBoxISOHeader.Focus();
                    SetMessageText(RichTextBoxLogMessage, $"ISO Header[{TextBoxISOHeader.Text}] entry length error !!!", Brushes.Red);
                    return;
                }
                ncccMessage.Header().Decode(TextBoxISOHeader.Text);
                ncccMessage.Mti(ComboBoxMessageType.Text);
                for (int index = 0; index < DataGridISO8583EntryScreen?.Items.Count; index++)
                {
                    if (!(DataGridISO8583EntryScreen.Items[index] is ISOFieldInfo))
                    {
                        SetMessageText(RichTextBoxLogMessage, $"Skip Index[{index}] invalid data !!!", Brushes.Red);
                        continue;
                    }
                    ISOFieldInfo item = (ISOFieldInfo)DataGridISO8583EntryScreen.Items[index];
                    if (item == null || string.IsNullOrEmpty(item.EntryData))
                    {
                        SetMessageText(RichTextBoxLogMessage, $"Skip Field[{item.BitNum}] no entry data !!!", Brushes.Red);
                        continue;
                    }
                    SetMessageText(RichTextBoxLogMessage, $"Process Index[{index}]", Brushes.Red);
                    if (item.Condition.Equals("M"))
                    {
                        if (string.IsNullOrEmpty(item.EntryData.Trim()))
                        {
                            SetFocusOnGrid(DataGridISO8583EntryScreen, index);
                            SetMessageText(RichTextBoxLogMessage, $"Field[{item.BitNum}] must be enter data !!!", Brushes.Red);
                            return;
                        }
                    }
                    if (item.FieldDataFormat.ToUpper().Equals("FIXED"))
                    {
                        var defineDataLength = Convert.ToInt16(item.Length.Trim());
                        if (defineDataLength != item.EntryData.Length)
                        {
                            SetFocusOnGrid(DataGridISO8583EntryScreen, index);
                            SetMessageText(RichTextBoxLogMessage, $"Field[{item.BitNum}] entry data length[{item.EntryData.Length}] not equal to define length[{defineDataLength}] !!!", Brushes.Red);
                            return;
                        }
                    }
                    if (item.FieldDataType.ToUpper().Equals("N"))
                    {
                        var isNumeric = item.EntryData.All(char.IsDigit);
                        if (!isNumeric)
                        {
                            SetFocusOnGrid(DataGridISO8583EntryScreen, index);
                            SetMessageText(RichTextBoxLogMessage, $"Field[{item.BitNum}] entry data[{item.EntryData}] is not numeric !!!", Brushes.Red);
                            return;
                        }
                    }
                    var field = FIELDMethods.Val(Convert.ToInt32(item.BitNum.Trim()));
                    ncccMessage.Core().Field(field, item.EntryData);
                }
                var iso8583Bytes = ncccMessage.Encode().Val();
                SetMessageText(RichTextBoxLogMessage, $" --- ISO8583 Data ---", Brushes.Red);
                SetMessageText(RichTextBoxLogMessage, $"Create NCCC ISO8583 Data[{Encoding.ASCII.GetString(iso8583Bytes)}]", Brushes.Black);
                SetMessageText(RichTextBoxLogMessage, $" --- ISO8583 Data ---", Brushes.Red);
                SetMessageText(RichTextBoxLogMessage, $" --- Begin Parsing ISO8583 Data ---", Brushes.Red);
                NcccIso8583 ncccRequestMessage = new NcccIso8583(iso8583Bytes);
                foreach (var pair in ncccRequestMessage.Core().OrderedFields())
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    if (value is byte[] bytes)
                    {
                        var tempString = HexStringConverter.FromByteArray(bytes);
                        SetMessageText(RichTextBoxLogMessage, $"FIELD [{key}] VALUE [{tempString}]", Brushes.Black);
                    }
                    else
                    {
                        SetMessageText(RichTextBoxLogMessage, $"FIELD [{key}] Field Position [{FIELDMethods.Val(key)}] VALUE [{value}]", Brushes.Black);
                    }
                }
                SetMessageText(RichTextBoxLogMessage, $" --- End Parsing ISO8583 Data ---", Brushes.Red);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, ex.Message);
                SetMessageText(RichTextBoxLogMessage, $"BtnCreateISOData_Click Exception:{ex.Message}", Brushes.Red);
            }
            finally
            {
                BtnCreateISOData.IsEnabled = true;
                Mouse.OverrideCursor = null;
            }
        }

        private void CreateTwinISOData()
        {
            BtnCreateISOData.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                var twinMessage = new TWINIso8583();
                if (TextBoxISOHeader.Text.Length != 12)
                {
                    TextBoxISOHeader.Focus();
                    SetMessageText(RichTextBoxLogMessage, $"ISO Header[{TextBoxISOHeader.Text}] entry length error !!!", Brushes.Red);
                    return;
                }
                twinMessage.Header().Decode(TextBoxISOHeader.Text);
                twinMessage.Mti(ComboBoxMessageType.Text);
                for (int index = 0; index < DataGridISO8583EntryScreen?.Items.Count; index++)
                {
                    if (!(DataGridISO8583EntryScreen.Items[index] is ISOFieldInfo))
                    {
                        SetMessageText(RichTextBoxLogMessage, $"Skip Index[{index}] invalid data !!!", Brushes.Red);
                        continue;
                    }
                    ISOFieldInfo item = (ISOFieldInfo)DataGridISO8583EntryScreen.Items[index];
                    if (item == null || string.IsNullOrEmpty(item.EntryData))
                    {
                        SetMessageText(RichTextBoxLogMessage, $"Skip Field[{item.BitNum}] no entry data !!!", Brushes.Red);
                        continue;
                    }
                    SetMessageText(RichTextBoxLogMessage, $"Process Index[{index}]", Brushes.Red);
                    if (item.Condition.Equals("M"))
                    {
                        if (string.IsNullOrEmpty(item.EntryData.Trim()))
                        {
                            SetFocusOnGrid(DataGridISO8583EntryScreen, index);
                            SetMessageText(RichTextBoxLogMessage, $"Field[{item.BitNum}] must be enter data !!!", Brushes.Red);
                            return;
                        }
                    }
                    if (item.FieldDataFormat.ToUpper().Equals("FIXED"))
                    {
                        var defineDataLength = Convert.ToInt16(item.Length.Trim());
                        if (defineDataLength != item.EntryData.Length)
                        {
                            SetFocusOnGrid(DataGridISO8583EntryScreen, index);
                            SetMessageText(RichTextBoxLogMessage, $"Field[{item.BitNum}] entry data length[{item.EntryData.Length}] not equal to define length[{defineDataLength}] !!!", Brushes.Red);
                            return;
                        }
                    }
                    if (item.FieldDataType.ToUpper().Equals("N"))
                    {
                        var isNumeric = item.EntryData.All(char.IsDigit);
                        if (!isNumeric)
                        {
                            SetFocusOnGrid(DataGridISO8583EntryScreen, index);
                            SetMessageText(RichTextBoxLogMessage, $"Field[{item.BitNum}] entry data[{item.EntryData}] is not numeric !!!", Brushes.Red);
                            return;
                        }
                    }
                    var field = FIELDMethods.Val(Convert.ToInt32(item.BitNum.Trim()));
                    twinMessage.Core().Field(field, item.EntryData);
                }
                var iso8583Bytes = twinMessage.Encode().Val();
                SetMessageText(RichTextBoxLogMessage, $" --- ISO8583 Data ---", Brushes.Red);
                SetMessageText(RichTextBoxLogMessage, $"Create TWIN ISO8583 Data[{Encoding.ASCII.GetString(iso8583Bytes)}]", Brushes.Black);
                SetMessageText(RichTextBoxLogMessage, $" --- ISO8583 Data ---", Brushes.Red);
                SetMessageText(RichTextBoxLogMessage, $" --- Begin Parsing TWIN ISO8583 Data ---", Brushes.Red);
                var twinRequestMessage = new TWINIso8583(iso8583Bytes);
                foreach (var pair in twinRequestMessage.Core().OrderedFields())
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    if (value is byte[] bytes)
                    {
                        var tempString = HexStringConverter.FromByteArray(bytes);
                        SetMessageText(RichTextBoxLogMessage, $"FIELD [{key}] VALUE [{tempString}]", Brushes.Black);
                    }
                    else
                    {
                        SetMessageText(RichTextBoxLogMessage, $"FIELD [{key}] Field Position [{FIELDMethods.Val(key)}] VALUE [{value}]", Brushes.Black);
                    }
                }
                SetMessageText(RichTextBoxLogMessage, $" --- End Parsing TWIN ISO8583 Data ---", Brushes.Red);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, ex.Message);
                SetMessageText(RichTextBoxLogMessage, $"BtnCreateISOData_Click Exception:{ex.Message}", Brushes.Red);
            }
            finally
            {
                BtnCreateISOData.IsEnabled = true;
                Mouse.OverrideCursor = null;
            }
        }

    }
}
