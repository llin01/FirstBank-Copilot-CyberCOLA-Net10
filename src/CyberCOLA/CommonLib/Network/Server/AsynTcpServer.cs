using NLog;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Com.Cybersoft4u.CommonLib.Network.Server
{
    public class AsynTcpServer
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog

        private Socket? _asyncServer;

        public bool IsRunning { get; set; }
        public int PortNumber { get; set; }
        public int SessionNumber { get; set; }
        private readonly CancellationToken _cancellationToken;

        public AsynTcpServer(CancellationToken cancellationToken)
        {
            IsRunning = false;
            _cancellationToken = cancellationToken;
        }

        public void StartServer()
        {
            _asyncServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var iep = new IPEndPoint(IPAddress.Any, PortNumber);
            _asyncServer.Bind(iep);
            _asyncServer.Listen(SessionNumber);
            _asyncServer.BeginAccept(AcceptConn, _asyncServer);
            IsRunning = true;
            Log.Info($"HSMServer Listener PortNumber[{PortNumber}] Waiting Connect ...");
        }

        private void AcceptConn(IAsyncResult pAsyncRes)
        {
            try
            {
                if (_asyncServer == null) return;
                Socket Client = ((Socket)pAsyncRes.AsyncState!).EndAccept(pAsyncRes);
                Log.Info("Connected From : {0} At {1:yyyy-MM-dd HH:mm:ss:fff}", Client.RemoteEndPoint, DateTime.Now);
                // One buffer per connection: a shared one let concurrent requests overwrite each other before they
                // were read, so a reply could be computed from another connection's card data.
                var buffer = new byte[1024];
                Client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, CallBackReceiveData, (Client, buffer));
                _asyncServer.BeginAccept(AcceptConn, _asyncServer);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Exception occurred ({ex.Message}). Closing connection.");
            }
        }

        private void CallBackSendData(IAsyncResult pAsyncRes)
        {
            var client = (Socket)pAsyncRes.AsyncState!;
            int lBytesSend = client.EndSend(pAsyncRes);
            Log.Info("Send Data Length[{0}]", lBytesSend);
            client.Close();
        }

        private void CallBackReceiveData(IAsyncResult pAsyncRes)
        {
            var (client, dataBuffer) = ((Socket, byte[]))pAsyncRes.AsyncState!;

            try
            {
                int bytesRead;
                byte[] message;
                if ((bytesRead = client.EndReceive(pAsyncRes)) <= 2)
                {
                    Log.Info("Listen PortNumber[{0}] Receive Data Error, Data too short.", PortNumber);
                    message = Encoding.ASCII.GetBytes("9901");
                    client.BeginSend(message, 0, message.Length, SocketFlags.None, CallBackSendData, client);
                    Log.Info("Send Data[{0}],Length[{1}]", Encoding.ASCII.GetString(message), message.Length);
                    return;
                }
                // The vendor HSM client (SocketClient.dll) sends one binary byte holding the function number before
                // the fields. Read it before Trim(): Trim() drops 0x09-0x0D as white space, and for the other values
                // the length was one too long (a Generate CVV (01) request read as 3D CAVV Generate (09)).
                string rawString = Encoding.ASCII.GetString(dataBuffer, 0, bytesRead);
                bool binaryHeader = rawString.Length > 1 && rawString[0] >= '\x01' && rawString[0] <= '\x1F';
                string dataBufferString = (binaryHeader ? rawString.Substring(1) : rawString).Trim();
                Log.Info("Listen PortNumber[{0}] Receive Data Length[{1}]", PortNumber, dataBufferString.Length);

                string funcID;
                if (binaryHeader)
                {
                    funcID = ((int)rawString[0]).ToString("D2");
                }
                else
                {
                    funcID = IdentifyFuncID(dataBufferString);
                }
                string responseStr = ProcessHSMCommand(funcID, dataBufferString);

                message = Encoding.ASCII.GetBytes(responseStr);
                // The same client reads the reply's first byte as the binary return code: the ASCII "00" read as
                // 0x30, i.e. return code 30 ("HSM communication error"). Send the 2-digit code as that one byte.
                if (binaryHeader && responseStr.Length >= 2 &&
                    byte.TryParse(responseStr.Substring(0, 2), System.Globalization.NumberStyles.HexNumber, null, out var returnCode))
                {
                    message = new[] { returnCode }.Concat(message.Skip(2)).ToArray();
                }
                client.BeginSend(message, 0, message.Length, SocketFlags.None, CallBackSendData, client);
                Log.Info("Send Data[{0}],Length[{1}]", Encoding.ASCII.GetString(message), message.Length);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, $"Exception occurred ({ex.Message}). Closing connection.");
                client.Close();
            }
        }

        private string IdentifyFuncID(string data)
        {
            string clean = data.Trim();
            int len = clean.Length;

            if (len == 34) return "01"; // Generate CVV
            if (len == 39) return "02"; // Verify CVV
            if (len == 46) return "03"; // Encrypt Bank's PIN
            if (len == 28) return "04"; // Clear PIN Generate
            if (len == 35) return "09"; // 3D CAVV Generate
            if (len == 38) return "10"; // 3D CAVV Verify
            if (len == 60) return "12"; // Change Receiving 3DES
            if (len == 18) return "13"; // New Key Replace
            if (len == 16) return "15"; // Generate RSA Key Pair
            if (len == 36) return "17"; // Encrypt Pin
            if (len == 59) return "19"; // 3DS-Decode For ACS OTP

            // FuncID 07: ARQC Verify. Spec: 8+3+2+2+n(cdolData)+16+18+2 = 51+cdolLen
            if (len >= 51)
            {
                string cdolLenStr = clean.Substring(13, 2);
                if (int.TryParse(cdolLenStr, out int cdolLen))
                {
                    if (len == 51 + cdolLen) return "07";
                }
            }

            // FuncID 05 (Generate PVV) vs 08 (ARPC Generate) both length 57.
            if (len == 57)
            {
                string segment13_16 = clean.Substring(13, 16);
                bool isHex16 = segment13_16.Length == 16 && segment13_16.All(c => (c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f'));
                if (isHex16)
                    return "08"; // ARPC Generate
                return "05";     // Generate PVV
            }

            // FuncID 11/14/20 all length 41.
            if (len == 41)
            {
                string wkBizId = clean.Substring(0, 8);
                if (wkBizId.Contains("NCCC") || wkBizId.Contains("FISC"))
                    return "20";

                string possibleCard = clean.Substring(12, 16);
                bool isCard = possibleCard.All(c => c >= '0' && c <= '9');
                if (isCard)
                    return "14";

                return "11";
            }

            // FuncID 18: Encrypt Data Extend. Spec: 3+2+1+16+3+n = 25+dataLen
            if (len >= 25)
            {
                string dataLenStr = clean.Substring(22, 3);
                if (int.TryParse(dataLenStr, out int dataLen))
                {
                    if (len == 25 + dataLen) return "18";
                }
            }

            // FuncID 21: Change Receiving TR31. Spec: 8+8+3+3+3+n+4+2 = 31+keyLen
            if (len >= 31)
            {
                string keyLenStr = clean.Substring(22, 3);
                if (int.TryParse(keyLenStr, out int keyLen))
                {
                    if (len == 31 + keyLen) return "21";
                }
            }

            // FuncID 22: AES Encrypt Decrypt. Spec: 2+2+2+32+3+n = 41+dataLen
            if (len >= 41)
            {
                string dataLenStr = clean.Substring(38, 3);
                if (int.TryParse(dataLenStr, out int dataLen))
                {
                    if (len == 41 + dataLen) return "22";
                }
            }

            // FuncID 23: Generate MAC. Spec: 8+2+16+3+n = 29+dataLen
            if (len >= 29)
            {
                string dataLenStr = clean.Substring(26, 3);
                if (int.TryParse(dataLenStr, out int dataLen))
                {
                    if (len == 29 + dataLen) return "23";
                }
            }

            // FuncID 16: Data Decrypt Encrypt (ESVC/ICASH).
            if (len >= 23)
            {
                string method = clean.Substring(13, 2);
                if (method == "01")
                {
                    string fnLenStr = clean.Substring(18, 3);
                    if (int.TryParse(fnLenStr, out int fnLen))
                    {
                        if (len == 31 + fnLen) return "16";
                    }
                }
                else if (method == "02")
                {
                    string keyLenStr = clean.Substring(18, 3);
                    if (int.TryParse(keyLenStr, out int keyLen))
                    {
                        if (len >= 29 + keyLen)
                        {
                            string fnLenStr = clean.Substring(27 + keyLen, 3);
                            if (int.TryParse(fnLenStr, out int fnLen))
                            {
                                if (len == 40 + keyLen + fnLen) return "16";
                            }
                        }
                    }
                }
            }

            return "UNKNOWN";
        }

        private string ProcessHSMCommand(string funcID, string data)
        {
            if (funcID == "UNKNOWN")
            {
                Log.Warn($"[HSM Sim] Drop unsupported or invalid message length ({data.Length})");
                return "03" + "00008020";
            }

            try
            {
                string rtnCode = "00";
                string rsnCode = "00000000";

                switch (funcID)
                {
                    case "01": // Generate CVV
                        if (data.Length < 34)
                        {
                            Log.Warn($"[HSM Sim] Generate CVV (01) -> Length too short ({data.Length} < 34)");
                            return "03" + "00008020" + "00000";
                        }
                        else
                        {
                            string bizId = data.Substring(0, 8);
                            string bankId = data.Substring(8, 3);
                            string cardNo = data.Substring(11, 16);
                            string expiryDate = data.Substring(27, 4);
                            string serviceCode = data.Substring(31, 3);

                            if (!IsDigitsOnly(cardNo) || !IsDigitsOnly(expiryDate))
                            {
                                Log.Warn($"[HSM Sim] Generate CVV (01) -> Invalid format");
                                return "03" + "00008020" + "00000";
                            }

                            Log.Info($"[HSM Sim] Generate CVV (01) -> bizId:{bizId}, bankId:{bankId}, cardNo:{MaskSensitive(cardNo)}, expiryDate:{expiryDate}, serviceCode:{serviceCode}");
                            int hash = StableHash(cardNo + expiryDate);
                            // The vendor client reads the CVV as the 3 characters after the 8-digit reason code.
                            string cvv = (hash % 1000).ToString("D3");
                            return rtnCode + rsnCode + cvv;
                        }

                    case "02": // Verify CVV
                        if (data.Length < 39)
                        {
                            Log.Warn($"[HSM Sim] Verify CVV (02) -> Length too short ({data.Length} < 39)");
                            return "03" + "00008020";
                        }
                        else
                        {
                            string bizId = data.Substring(0, 8);
                            string bankId = data.Substring(8, 3);
                            string cardNo = data.Substring(11, 16);
                            string expiryDate = data.Substring(27, 4);
                            string serviceCode = data.Substring(31, 3);
                            string cvv = data.Substring(34, 5);

                            Log.Info($"[HSM Sim] Verify CVV (02) -> bizId:{bizId}, bankId:{bankId}, cardNo:{MaskSensitive(cardNo)}, cvv:{MaskSensitive(cvv)}");
                            return rtnCode + rsnCode;
                        }

                    case "03": // Encrypt Bank's PIN
                        if (data.Length < 46)
                        {
                            Log.Warn($"[HSM Sim] Encrypt Bank's PIN (03) -> Length too short ({data.Length} < 46)");
                            return "03" + "00008020" + new string('0', 16) + "0000" + "0000";
                        }
                        else
                        {
                            string bizId = data.Substring(0, 8);
                            string bankId = data.Substring(8, 3);
                            string cardNo = data.Substring(11, 16);
                            string pvkIndex = data.Substring(27, 1);
                            string ppkBizId = data.Substring(28, 8);
                            string ppkKeyId = data.Substring(36, 3);
                            string pinBlockFormat = data.Substring(39, 1);
                            string clearPIN = data.Substring(40, 6);

                            if (!IsDigitsOnly(cardNo))
                            {
                                Log.Warn($"[HSM Sim] Encrypt Bank's PIN (03) -> Invalid format");
                                return "03" + "00008020" + new string('0', 16) + "0000" + "0000";
                            }

                            Log.Info($"[HSM Sim] Encrypt Bank's PIN (03) -> bizId:{bizId}, bankId:{bankId}, cardNo:{MaskSensitive(cardNo)}, clearPIN:{MaskSensitive(clearPIN)}");
                            int hash = StableHash(cardNo + clearPIN);
                            string encPINBlock = hash.ToString("X16").PadLeft(16, '0');
                            string pvv = (hash % 10000).ToString("D4");
                            // The vendor client reads a 6-character PIN offset here (its Func03 out parameter).
                            string pinOffset = ((hash / 10) % 1000000).ToString("D6");
                            return rtnCode + rsnCode + encPINBlock + pvv + pinOffset;
                        }

                    case "04": // Clear PIN Generate
                        if (data.Length < 28)
                        {
                            Log.Warn($"[HSM Sim] Clear PIN Generate (04) -> Length too short ({data.Length} < 28)");
                            return "03" + "00008020" + "000000";
                        }
                        else
                        {
                            string bizId = data.Substring(0, 8);
                            string bankId = data.Substring(8, 3);
                            string cardNo = data.Substring(11, 16);
                            string pvkIndex = data.Substring(27, 1);

                            Log.Info($"[HSM Sim] Clear PIN Generate (04) -> bizId:{bizId}, bankId:{bankId}, cardNo:{MaskSensitive(cardNo)}");
                            int hash = StableHash(cardNo);
                            string clearPin = (hash % 1000000).ToString("D6");
                            return rtnCode + rsnCode + clearPin;
                        }

                    case "06": // ARQC Generate (CVN18)
                        if (data.Length < 35)
                        {
                            Log.Warn($"[HSM Sim] ARQC Generate (06) -> Length too short ({data.Length} < 35)");
                            return "03" + "00008020" + new string('0', 16);
                        }
                        else
                        {
                            string bizId = data.Substring(0, 8);
                            string bankId = data.Substring(8, 3);
                            string keyIndex = data.Substring(11, 2);
                            if (!int.TryParse(data.Substring(13, 2), out int cdolLen))
                            {
                                Log.Warn($"[HSM Sim] ARQC Generate (06) -> Invalid cdolDataLength");
                                return "03" + "00008020" + new string('0', 16);
                            }
                            if (data.Length < 35 + cdolLen)
                            {
                                Log.Warn($"[HSM Sim] ARQC Generate (06) -> Data length insufficient ({data.Length} < {35 + cdolLen})");
                                return "03" + "00008020" + new string('0', 16);
                            }
                            string cdolData = data.Substring(15, cdolLen);
                            string panData = data.Substring(15 + cdolLen, 18);
                            string cryptogramVersionNumber = data.Substring(15 + cdolLen + 18, 2);

                            Log.Info($"[HSM Sim] ARQC Generate (06) -> bizId:{bizId}, keyIndex:{keyIndex}");
                            int hash = StableHash(cdolData + panData);
                            string arqcData = hash.ToString("X16").PadLeft(16, '0');
                            return rtnCode + rsnCode + arqcData;
                        }

                    case "05": // Generate PVV
                        if (data.Length < 57)
                        {
                            Log.Warn($"[HSM Sim] Generate PVV (05) -> Length too short ({data.Length} < 57)");
                            return "03" + "00008020" + "0000" + "0000" + "0000";
                        }
                        else
                        {
                            string pvkBizId = data.Substring(0, 8);
                            string bankId = data.Substring(8, 3);
                            string pvkIndex = data.Substring(11, 1);
                            string cardNo = data.Substring(12, 16);
                            string ppkBizId = data.Substring(28, 8);
                            string pinBlockFormat = data.Substring(36, 1);
                            string encPINBlock = data.Substring(37, 16);
                            string pinOffset = data.Substring(53, 4);

                            Log.Info($"[HSM Sim] Generate PVV (05) -> pvkBizId:{pvkBizId}, bankId:{bankId}, pinOffset:{pinOffset}, encPINBlock:{MaskSensitive(encPINBlock)}");
                            int hash = StableHash(encPINBlock);
                            string pvv = (hash % 10000).ToString("D4");
                            string pvvOrigin = pvv;
                            return rtnCode + rsnCode + pvv + pvvOrigin + pinOffset;
                        }

                    case "07": // ARQC Verify
                        if (data.Length < 15)
                        {
                            Log.Warn($"[HSM Sim] ARQC Verify (07) -> Length too short ({data.Length} < 15)");
                            return "03" + "00008020";
                        }
                        else
                        {
                            string bizId = data.Substring(0, 8);
                            string bankId = data.Substring(8, 3);
                            string keyIndex = data.Substring(11, 2);
                            if (!int.TryParse(data.Substring(13, 2), out int cdolLen))
                            {
                                Log.Warn($"[HSM Sim] ARQC Verify (07) -> Invalid cdolDataLength");
                                return "03" + "00008020";
                            }

                            if (data.Length < 15 + cdolLen + 16 + 18 + 2)
                            {
                                Log.Warn($"[HSM Sim] ARQC Verify (07) -> Data length insufficient ({data.Length} < {15 + cdolLen + 16 + 18 + 2})");
                                return "03" + "00008020";
                            }

                            string cdolData = data.Substring(15, cdolLen);
                            string arqcData = data.Substring(15 + cdolLen, 16);
                            string panData = data.Substring(15 + cdolLen + 16, 18);
                            string cryptogramVersionNumber = data.Substring(15 + cdolLen + 16 + 18, 2);

                            Log.Info($"[HSM Sim] ARQC Verify (07) -> bizId:{bizId}, keyIndex:{keyIndex}, arqcData:{MaskSensitive(arqcData)}, panData:{MaskSensitive(panData)}");

                            if (arqcData == "9999999999999999")
                            {
                                Log.Warn($"[HSM Sim] ARQC Verify (07) -> Simulating verify failure");
                                return "00" + "00009001";
                            }
                            return rtnCode + rsnCode;
                        }

                    case "08": // ARPC Generate
                        if (data.Length < 57)
                        {
                            Log.Warn($"[HSM Sim] ARPC Generate (08) -> Length too short ({data.Length} < 57)");
                            return "03" + "00008020" + new string('0', 16);
                        }
                        else
                        {
                            string bizId = data.Substring(0, 8);
                            string bankId = data.Substring(8, 3);
                            string keyIndex = data.Substring(11, 2);
                            string arqcData = data.Substring(13, 16);
                            string arc = data.Substring(29, 4);
                            string atc = data.Substring(33, 4);
                            string panData = data.Substring(37, 18);
                            string cryptogramVersionNumber = data.Substring(55, 2);

                            Log.Info($"[HSM Sim] ARPC Generate (08) -> bizId:{bizId}, arqcData:{MaskSensitive(arqcData)}, arc:{arc}, atc:{atc}");
                            int hash = StableHash(arqcData + arc + atc);
                            string arpcData = hash.ToString("X16").PadLeft(16, '0');
                            return rtnCode + rsnCode + arpcData;
                        }

                    case "09": // 3D CAVV Generate
                        if (data.Length < 35)
                        {
                            Log.Warn($"[HSM Sim] 3D CAVV Generate (09) -> Length too short ({data.Length} < 35)");
                            return "03" + "00008020" + "000";
                        }
                        else
                        {
                            string bizId = data.Substring(0, 8);
                            string bankId = data.Substring(8, 3);
                            string keyIndex = data.Substring(11, 1);
                            string cardNo = data.Substring(12, 16);
                            string expiryDate = data.Substring(28, 4);
                            string serviceCode = data.Substring(32, 3);

                            Log.Info($"[HSM Sim] 3D CAVV Generate (09) -> bizId:{bizId}, cardNo:{MaskSensitive(cardNo)}, expiryDate:{expiryDate}");
                            int hash = StableHash(cardNo + expiryDate);
                            string cavv = (hash % 1000).ToString("D3");
                            return rtnCode + rsnCode + cavv;
                        }

                    case "10": // 3D CAVV Verify
                        if (data.Length < 38)
                        {
                            Log.Warn($"[HSM Sim] 3D CAVV Verify (10) -> Length too short ({data.Length} < 38)");
                            return "03" + "00008020";
                        }
                        else
                        {
                            string bizId = data.Substring(0, 8);
                            string bankId = data.Substring(8, 3);
                            string keyIndex = data.Substring(11, 1);
                            string cardNo = data.Substring(12, 16);
                            string expiryDate = data.Substring(28, 4);
                            string serviceCode = data.Substring(32, 3);
                            string cvv = data.Substring(35, 3);

                            Log.Info($"[HSM Sim] 3D CAVV Verify (10) -> bizId:{bizId}, cardNo:{MaskSensitive(cardNo)}, cvv:{MaskSensitive(cvv)}");
                            return rtnCode + rsnCode;
                        }

                    case "11": // Change Sending 3DES IWK/AWK
                        if (data.Length < 41)
                        {
                            Log.Warn($"[HSM Sim] Change Sending 3DES (11) -> Length too short ({data.Length} < 41)");
                            return "03" + "00008020" + "0000" + new string('0', 32);
                        }
                        else
                        {
                            string wkBizId = data.Substring(0, 8);
                            string zcmkBizId = data.Substring(8, 8);
                            string bankId = data.Substring(16, 3);
                            string wkKeyid = data.Substring(19, 3);

                            Log.Info($"[HSM Sim] Change Sending 3DES (11) -> wkBizId:{wkBizId}, zcmkBizId:{zcmkBizId}, bankId:{bankId}");
                            string keySync = "1234";
                            string encKey = "A1B2C3D4E5F67890A1B2C3D4E5F67890";
                            return rtnCode + rsnCode + keySync + encKey;
                        }

                    case "12": // Change Receiving 3DES IWK/AWK
                        if (data.Length < 60)
                        {
                            Log.Warn($"[HSM Sim] Change Receiving 3DES (12) -> Length too short ({data.Length} < 60)");
                            return "03" + "00008020";
                        }
                        else
                        {
                            string wkBizId = data.Substring(0, 8);
                            string zcmkBizId = data.Substring(8, 8);
                            string bankId = data.Substring(16, 3);
                            string wkKeyId = data.Substring(19, 3);
                            string encKey = data.Substring(22, 32);
                            string keySync = data.Substring(54, 4);

                            Log.Info($"[HSM Sim] Change Receiving 3DES (12) -> wkBizId:{wkBizId}, zcmkBizId:{zcmkBizId}, keySync:{keySync}");
                            return rtnCode + rsnCode;
                        }

                    case "13": // New Key Replace Old Key
                        if (data.Length < 18)
                        {
                            Log.Warn($"[HSM Sim] New Key Replace Old Key (13) -> Length too short ({data.Length} < 18)");
                            return "03" + "00008020";
                        }
                        else
                        {
                            string wkBizId = data.Substring(0, 8);
                            string bankId = data.Substring(8, 3);
                            string wkKeyid = data.Substring(11, 3);
                            string replaceMac = data.Substring(14, 1);
                            string replaceOPINENC = data.Substring(15, 1);
                            string replaceIPINENC = data.Substring(16, 1);
                            string replaceTR31 = data.Substring(17, 1);

                            Log.Info($"[HSM Sim] New Key Replace Old Key (13) -> wkBizId:{wkBizId}, wkKeyid:{wkKeyid}, replaceMac:{replaceMac}");
                            return rtnCode + rsnCode;
                        }

                    case "14": // Generate CVV1/CVV2/CVV3/PVV
                        if (data.Length < 41)
                        {
                            Log.Warn($"[HSM Sim] Generate CVV1/2/3/PVV (14) -> Length too short ({data.Length} < 41)");
                            return "03" + "00008020" + "0000" + "000000" + "000" + "000" + "000";
                        }
                        else
                        {
                            string bizId = data.Substring(0, 8);
                            string bankId = data.Substring(8, 3);
                            string pvkIndex = data.Substring(11, 1);
                            string cardNo = data.Substring(12, 16);
                            string expiryDate = data.Substring(28, 4);

                            Log.Info($"[HSM Sim] Generate CVV1/2/3/PVV (14) -> bizId:{bizId}, cardNo:{MaskSensitive(cardNo)}");
                            int hash = StableHash(cardNo + expiryDate);
                            string pvvVal = (hash % 10000).ToString("D4");
                            string clearPin = ((hash / 10) % 1000000).ToString("D6");
                            string cvv1 = (hash % 1000).ToString("D3");
                            string cvv2 = ((hash / 2) % 1000).ToString("D3");
                            string cvv3 = ((hash / 3) % 1000).ToString("D3");
                            return rtnCode + rsnCode + pvvVal + clearPin + cvv1 + cvv2 + cvv3;
                        }

                    case "15": // Generate RSA Key Pair
                        if (data.Length < 16)
                        {
                            Log.Warn($"[HSM Sim] Generate RSA Key Pair (15) -> Length too short ({data.Length} < 16)");
                            return "03" + "00008020" + "";
                        }
                        else
                        {
                            string bizId = data.Substring(0, 8);
                            string bankId = data.Substring(8, 3);
                            string keyVersion = data.Substring(11, 2);
                            string method = data.Substring(13, 2);
                            string keyLength = data.Substring(15, 1);

                            Log.Info($"[HSM Sim] Generate RSA Key Pair (15) -> bizId:{bizId}, keyVersion:{keyVersion}, keyLength:{keyLength}");
                            int modulusBytes = keyLength == "1" ? 256 : 128;
                            string publicKey = new string('A', modulusBytes);
                            return rtnCode + rsnCode + publicKey;
                        }

                    case "16": // Data Decrypt Encrypt
                        if (data.Length < 18)
                        {
                            Log.Warn($"[HSM Sim] Data Decrypt Encrypt (16) -> Length too short ({data.Length} < 18)");
                            return "03" + "00008020" + "0000000000" + "000" + "";
                        }
                        else
                        {
                            string bizId = data.Substring(0, 8);
                            string bankId = data.Substring(8, 3);
                            string keyVersion = data.Substring(11, 2);
                            string method = data.Substring(13, 2);
                            string dataPadding = data.Substring(15, 1);
                            string sessionKeyPadding = data.Substring(16, 1);
                            string fileStatus = data.Substring(17, 1);

                            Log.Info($"[HSM Sim] Data Decrypt Encrypt (16) -> bizId:{bizId}, method:{method}, fileStatus:{fileStatus}");

                            if (method == "02")
                            {
                                if (data.Length < 30) return "03" + "00008020" + "0000000000" + "000" + "";
                                if (!int.TryParse(data.Substring(18, 3), out int keyLen))
                                {
                                    return "03" + "00008020";
                                }
                                if (data.Length < 30 + keyLen) return "03" + "00008020";
                                string encSessionKey = data.Substring(21, keyLen);
                                string checkVal = data.Substring(21 + keyLen, 6);
                                if (!int.TryParse(data.Substring(27 + keyLen, 3), out int fnLen))
                                {
                                    return "03" + "00008020";
                                }
                                if (data.Length < 30 + keyLen + fnLen + 10) return "03" + "00008020";
                                string fileName = data.Substring(30 + keyLen, fnLen);
                                string fileSize = data.Substring(30 + keyLen + fnLen, 10);

                                return rtnCode + rsnCode + fileSize + fnLen.ToString("D3") + fileName;
                            }
                            else
                            {
                                if (!int.TryParse(data.Substring(18, 3), out int fnLen))
                                {
                                    return "03" + "00008020";
                                }
                                if (data.Length < 21 + fnLen + 10) return "03" + "00008020";
                                string fileName = data.Substring(21, fnLen);
                                string fileSize = data.Substring(21 + fnLen, 10);

                                string encSessionKey = new string('F', 256);
                                string checkVal = "123456";
                                return rtnCode + rsnCode + fileSize + fnLen.ToString("D3") + fileName + "256" + encSessionKey + checkVal;
                            }
                        }

                    case "17": // Encrypt Pin
                        if (data.Length < 36)
                        {
                            Log.Warn($"[HSM Sim] Encrypt Pin (17) -> Length too short ({data.Length} < 36)");
                            return "03" + "00008020" + new string('0', 16);
                        }
                        else
                        {
                            string bankId = data.Substring(0, 3);
                            string keyIndex = data.Substring(3, 2);
                            string pinBlockFormat = data.Substring(5, 1);
                            string pinLength = data.Substring(6, 2);
                            string clearPIN = data.Substring(8, 12);
                            string cardNo = data.Substring(20, 16);

                            Log.Info($"[HSM Sim] Encrypt Pin (17) -> bankId:{bankId}, pinLength:{pinLength}, cardNo:{MaskSensitive(cardNo)}, clearPIN:{MaskSensitive(clearPIN)}");
                            int hash = StableHash(cardNo + clearPIN);
                            string encryptedPin = hash.ToString("X16").PadLeft(16, '0');
                            return rtnCode + rsnCode + encryptedPin;
                        }

                    case "18": // Encrypt Data Extend
                        if (data.Length < 25)
                        {
                            Log.Warn($"[HSM Sim] Encrypt Data Extend (18) -> Length too short ({data.Length} < 25)");
                            return "03" + "00008020" + "";
                        }
                        else
                        {
                            string bankId = data.Substring(0, 3);
                            string keyIndex = data.Substring(3, 2);
                            string mode = data.Substring(5, 1);
                            string keySyncData = data.Substring(6, 16);
                            if (!int.TryParse(data.Substring(22, 3), out int dataLen))
                            {
                                return "03" + "00008020";
                            }
                            if (data.Length < 25 + dataLen)
                            {
                                return "03" + "00008020";
                            }
                            string dataPayload = data.Substring(25, dataLen);

                            Log.Info($"[HSM Sim] Encrypt Data Extend (18) -> bankId:{bankId}, dataLength:{dataLen}");
                            char[] charArray = dataPayload.ToCharArray();
                            Array.Reverse(charArray);
                            string encryptedData = new string(charArray);
                            return rtnCode + rsnCode + encryptedData;
                        }

                    case "19": // 3DS-Decode For ACS OTP
                        if (data.Length < 59)
                        {
                            Log.Warn($"[HSM Sim] 3DS-Decode (19) -> Length too short ({data.Length} < 59)");
                            return "03" + "00008020" + new string('0', 19) + new string('0', 10);
                        }
                        else
                        {
                            string bankId = data.Substring(0, 3);
                            string cardNo = data.Substring(3, 32);
                            string otp = data.Substring(35, 24);

                            Log.Info($"[HSM Sim] 3DS-Decode (19) -> bankId:{bankId}, cardNo:{MaskSensitive(cardNo)}, otp:{MaskSensitive(otp)}");
                            string decryptedCardNo = "4000123456789010".PadRight(19, ' ');
                            string decryptedOtp = "123456".PadRight(10, ' ');
                            return rtnCode + rsnCode + decryptedCardNo + decryptedOtp;
                        }

                    case "20": // Change Sending 3DES IWK/AWK for TR31
                        if (data.Length < 41)
                        {
                            Log.Warn($"[HSM Sim] Change Sending TR31 (20) -> Length too short ({data.Length} < 41)");
                            return "03" + "00008020" + "0000" + "000" + new string('0', 32);
                        }
                        else
                        {
                            string wkBizId = data.Substring(0, 8);
                            string zcmkBizId = data.Substring(8, 8);
                            string bankId = data.Substring(16, 3);
                            string wkKeyid = data.Substring(19, 3);

                            Log.Info($"[HSM Sim] Change Sending TR31 (20) -> wkBizId:{wkBizId}, bankId:{bankId}");
                            string keySync = "1234";
                            string keyLength = "032";
                            string encKey = "A1B2C3D4E5F67890A1B2C3D4E5F67890";
                            return rtnCode + rsnCode + keySync + keyLength + encKey;
                        }

                    case "21": // Change Receiving 3DES IWK/AWK for TR31
                        if (data.Length < 25)
                        {
                            Log.Warn($"[HSM Sim] Change Receiving TR31 (21) -> Length too short ({data.Length} < 25)");
                            return "03" + "00008020";
                        }
                        else
                        {
                            string wkBizId = data.Substring(0, 8);
                            string zcmkBizId = data.Substring(8, 8);
                            string bankId = data.Substring(16, 3);
                            string wkKeyId = data.Substring(19, 3);
                            if (!int.TryParse(data.Substring(22, 3), out int keyLen))
                            {
                                return "03" + "00008020";
                            }
                            if (data.Length < 25 + keyLen + 4 + 2)
                            {
                                return "03" + "00008020";
                            }
                            string encKey = data.Substring(25, keyLen);
                            string keySync = data.Substring(25 + keyLen, 4);

                            Log.Info($"[HSM Sim] Change Receiving TR31 (21) -> wkBizId:{wkBizId}, keySync:{keySync}");
                            return rtnCode + rsnCode;
                        }

                    case "22": // AES Encrypt Decrypt
                        if (data.Length < 41)
                        {
                            Log.Warn($"[HSM Sim] AES Encrypt Decrypt (22) -> Length too short ({data.Length} < 41)");
                            return "03" + "00008020" + "";
                        }
                        else
                        {
                            string keyIndex = data.Substring(0, 2);
                            string method = data.Substring(2, 2);
                            string mode = data.Substring(4, 2);
                            string iv = data.Substring(6, 32);
                            if (!int.TryParse(data.Substring(38, 3), out int dataLen))
                            {
                                return "03" + "00008020";
                            }
                            if (data.Length < 41 + dataLen)
                            {
                                return "03" + "00008020";
                            }
                            string content = data.Substring(41, dataLen);

                            Log.Info($"[HSM Sim] AES Encrypt Decrypt (22) -> method:{method}, dataLength:{dataLen}");
                            char[] charArray = content.ToCharArray();
                            Array.Reverse(charArray);
                            string result = new string(charArray);
                            return rtnCode + rsnCode + result;
                        }

                    case "23": // Generate MAC
                        if (data.Length < 29)
                        {
                            Log.Warn($"[HSM Sim] Generate MAC (23) -> Length too short ({data.Length} < 29)");
                            return "03" + "00008020" + new string('0', 16);
                        }
                        else
                        {
                            string wkBizId = data.Substring(0, 8);
                            string method = data.Substring(8, 2);
                            string iv = data.Substring(10, 16);
                            if (!int.TryParse(data.Substring(26, 3), out int dataLen))
                            {
                                return "03" + "00008020" + new string('0', 16);
                            }
                            if (data.Length < 29 + dataLen)
                            {
                                return "03" + "00008020" + new string('0', 16);
                            }
                            string macData = data.Substring(29, dataLen);

                            Log.Info($"[HSM Sim] Generate MAC (23) -> wkBizId:{wkBizId}, dataLength:{dataLen}");
                            int hash = StableHash(macData);
                            string macResult = hash.ToString("X16").PadLeft(16, '0');
                            return rtnCode + rsnCode + macResult;
                        }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[HSM Sim] Parse incoming command exception: {ex.Message}");
            }

            return "03" + "00002999";
        }

        private bool IsDigitsOnly(string str)
        {
            foreach (char c in str)
            {
                if (c < '0' || c > '9')
                    return false;
            }
            return true;
        }

        // string.GetHashCode() is randomized per process, so a card's simulated CVV, PVV or cryptogram changed on every
        // simulator restart (and Math.Abs threw on int.MinValue). A digest gives the same value every run.
        private static int StableHash(string value)
        {
            var digest = System.Security.Cryptography.SHA256.HashData(Encoding.ASCII.GetBytes(value));
            return BitConverter.ToInt32(digest, 0) & int.MaxValue;
        }

        private static string MaskSensitive(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;
            if (value.Length <= 6)
                return new string('*', value.Length);
            return new string('*', value.Length - 4) + value.Substring(value.Length - 4);
        }

        public void StopServer()
        {
            try
            {
                if (IsRunning)
                {
                    _asyncServer?.Shutdown(SocketShutdown.Both);
                    _asyncServer?.Close();
                }
            }
            catch (SocketException soketex)
            {
                Log.Fatal(soketex, soketex.Message);
                _asyncServer?.Close();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, ex.Message);
            }
            finally
            {
                IsRunning = false;
            }
        }
    }
}
