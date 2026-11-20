using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Com.Cybersoft4u.CommonLib.AuthUtils;
using Com.Cybersoft4u.TM.FISC;
using Com.Cybersoft4u.TM.FISC.DE58;
using Com.Cybersoft4u.TM.ISO8583;
using Com.Cybersoft4u.TM.ISO8583.Nccc;
using Com.Cybersoft4u.TM.ISO8583.Nccc.Tokens;

namespace Com.Cybersoft4u.Simulator.Console.ConsoleStressSimulator
{
    public enum TransactionType
    {
        NCCC = 1,
        FISC = 2,
        VISA = 3,
        MasterCard = 4
    }

    public class GeneratorResult
    {
        public ConcurrentQueue<byte[]> Queue { get; set; } = new ConcurrentQueue<byte[]>();
        public string SourceDescription { get; set; } = string.Empty;
    }

    public class CardPools
    {
        public List<string> VisaList { get; set; } = new List<string>();
        public List<string> MasterCardList { get; set; } = new List<string>();
        public List<string> JcbList { get; set; } = new List<string>();
    }

    public class TestDataGenerator
    {
        /// <summary>
        /// Reference WpfStressSimulator CardXmlFilePath.xml parsing:
        /// Loads Track2 card data from CardXmlFilePath.xml in Config directory
        /// </summary>
        public static CardPools LoadCardPoolsFromXml(TransactionType type)
        {
            var pools = new CardPools();
            try
            {
                // Each network has its own pool; always loading Nccc's sent NCCC cards to the FISC Auth.
                string configRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config");
                string targetXml = Path.Combine(ResolveTemplateDirectory(configRoot, type.ToString()), "CardXmlFilePath.xml");

                if (!File.Exists(targetXml))
                {
                    targetXml = Path.Combine(configRoot, "Nccc", "CardXmlFilePath.xml");
                }

                if (!File.Exists(targetXml))
                {
                    targetXml = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "Fisc", "CardXmlFilePath.xml");
                }

                if (File.Exists(targetXml))
                {
                    var xmlDoc = new XmlDocument();
                    xmlDoc.Load(targetXml);

                    XmlNodeList? track2Nodes = xmlDoc.SelectNodes("//Track2Data");
                    if (track2Nodes != null)
                    {
                        foreach (XmlNode node in track2Nodes)
                        {
                            string track2 = node.InnerText.Trim();
                            if (string.IsNullOrEmpty(track2)) continue;

                            string pan = track2.Split('=', 'D', '~', '^')[0];
                            if (pan.StartsWith("4"))
                                pools.VisaList.Add(track2);
                            else if (pan.StartsWith("5"))
                                pools.MasterCardList.Add(track2);
                            else if (pan.StartsWith("3"))
                                pools.JcbList.Add(track2);
                            else
                                pools.VisaList.Add(track2);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[CardLoader] Warning: Failed to load CardXmlFilePath.xml: {ex.Message}");
            }

            return pools;
        }

        public static GeneratorResult GenerateTestData(TransactionType type, int count, string? configDirectory = null,
                                                       bool skipOwnCardTemplates = false, bool keepChipTemplates = false)
        {
            var result = new GeneratorResult();
            var queue = new ConcurrentQueue<byte[]>();

            // ISO8583 payload templates live under Data\<Type>; Config\<Type> only holds CardXmlFilePath.xml
            // (the card pool), which must never be sent as a message.
            string targetDir = configDirectory ?? ResolveTemplateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data"), type.ToString());

            // Sub-folders hold templates too (Data\Fisc\Data, VISA_TX, MASTERCARD_TX: refunds, reversals);
            // only the top level used to be read. Sorted so runs are repeatable.
            string[] templateFiles = Directory.Exists(targetDir)
                ? Directory.GetFiles(targetDir, "*", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal).ToArray()
                : Array.Empty<string>();

            // With card replacement on, some templates only work with their own card and always fail:
            // - FISC identity verification (DE58 tag 68 = 01/03): the Auth checks the template's ID, birth date and
            //   phone against the new card's holder (6P, reason 63);
            // - chip (FISC F55 tag 9F36, NCCC token B2): the cryptogram belongs to the template's card (ARQC, check
            //   code 102) and the fixed ATC must exceed the card's stored ATC, so at most the first use per card
            //   passes (check code 232).
            // - NCCC rebate refund (F3 20xxxx with a rebate C5 token): Cola_RedeemProcess must match the original
            //   rebate purchase, which the template's card never made (RC 05, check code 252, reason 04).
            // Leave those out so the run measures the other paths; the remaining templates still fill `count`.
            // keepChipTemplates sends the chip ones anyway, to exercise the chip path (expect 102/232 declines).
            // The Auth calls the HSM's ARQC check (Func07) only when SystemParameterInfo's HSMCheckFlags set
            // IsARQCCheck=Y for the network; with N, chip templates never reach the HSM.
            int skippedIdentity = 0, skippedChip = 0, skippedRebateRefund = 0;
            if (skipOwnCardTemplates && (type == TransactionType.FISC || type == TransactionType.NCCC) && templateFiles.Length > 0)
            {
                var kinds = templateFiles.ToDictionary(f => f, f => type == TransactionType.FISC
                    ? ClassifyFiscTemplate(File.ReadAllBytes(f))
                    : ClassifyNcccTemplate(File.ReadAllBytes(f)));
                string[] kept = templateFiles
                    .Where(f => kinds[f] == TemplateKind.Other || (keepChipTemplates && kinds[f] == TemplateKind.Chip))
                    .ToArray();
                if (kept.Length > 0)
                {
                    skippedIdentity = kinds.Values.Count(k => k == TemplateKind.IdentityVerification);
                    skippedChip = keepChipTemplates ? 0 : kinds.Values.Count(k => k == TemplateKind.Chip);
                    skippedRebateRefund = kinds.Values.Count(k => k == TemplateKind.RebateRefund);
                    templateFiles = kept;
                }
            }

            if (templateFiles.Length > 0)
            {
                result.SourceDescription = $"Template Directory ({templateFiles.Length} file(s) in {Path.GetFileName(targetDir)})"
                                           + (skippedIdentity + skippedChip + skippedRebateRefund > 0
                                               ? $", skipped {skippedIdentity} identity-verification, {skippedChip} chip and {skippedRebateRefund} rebate-refund template(s)"
                                               : string.Empty);

                int fileIndex = 0;
                for (int i = 1; i <= count; i++)
                {
                    string file = templateFiles[fileIndex % templateFiles.Length];
                    byte[] rawFileBytes = File.ReadAllBytes(file);

                    // Some templates (e.g. Data\Fisc\*.dat) already carry the 2-byte length header; adding a
                    // second one shifted every field by two bytes and the GW rejected the whole message.
                    byte[] fullMessage = HasLengthHeader(rawFileBytes)
                        ? rawFileBytes
                        : ColaCommUtility.AddBinaryLength2BytesArray(rawFileBytes);
                    queue.Enqueue(fullMessage);

                    fileIndex++;
                }
            }
            else
            {
                result.SourceDescription = $"Dynamic Mock Generator ({type})";
                for (int i = 1; i <= count; i++)
                {
                    byte[] messageBytes = CreateMockMessage(type, i);
                    queue.Enqueue(messageBytes);
                }
            }

            result.Queue = queue;
            return result;
        }

        private enum TemplateKind { Other, IdentityVerification, Chip, RebateRefund }

        // Identity verification: the trigger of FISC_0100AuthProcess's NCCC identity check (isoP58T68 IN ('01','03')).
        // Chip: carries the ATC (F55 tag 9F36) that Cola_CheckICStatus compares with the card's stored ATC.
        // A template that cannot be decoded counts as Other and is kept, as before.
        private static TemplateKind ClassifyFiscTemplate(byte[] rawFileBytes)
        {
            try
            {
                byte[] payload = HasLengthHeader(rawFileBytes) ? rawFileBytes.Skip(2).ToArray() : rawFileBytes;
                var fisc = new FISC(payload);
                object? de58 = fisc.RawVal(FIELD.F_58);
                if (de58 != null)
                {
                    var tags = new DE58TagField(de58.ToString()!);
                    if (tags.FindDE58TagIdExist("68"))
                    {
                        string platform = tags.GetDE58TagDataByTokenId("68");
                        if (platform == "01" || platform == "03") return TemplateKind.IdentityVerification;
                    }
                }
                if (fisc.RawVal(FIELD.F_55) != null &&
                    fisc.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags.ContainsKey("9F36"))
                {
                    return TemplateKind.Chip;
                }
                return TemplateKind.Other;
            }
            catch (Exception)
            {
                return TemplateKind.Other;
            }
        }

        // NCCC chip data travels in token B2 (ATC and ARQC), read the same way as FirstHSMUtility does.
        // A template that cannot be decoded counts as Other and is kept.
        private static TemplateKind ClassifyNcccTemplate(byte[] rawFileBytes)
        {
            try
            {
                byte[] payload = HasLengthHeader(rawFileBytes) ? rawFileBytes.Skip(2).ToArray() : rawFileBytes;
                var nccc = new NcccIso8583(payload);
                var field = nccc.GetTokenFieldId();
                if (nccc.RawVal(field) == null) return TemplateKind.Other;
                var tokenGroup = nccc.Core().GetOthersValByKey(field.ToString()) as TokenGroup;
                if (tokenGroup?.GetToken(NcccTransactionConst.PosAdditionalData.TokenB2.Id) is TokenB2) return TemplateKind.Chip;
                // A C5 token is either instalment or rebate data; only the rebate one sends a refund to Cola_RedeemProcess.
                object? c5 = tokenGroup?.GetToken(NcccTransactionConst.PosAdditionalData.TokenC5.Id);
                bool rebateC5 = c5 != null && c5 is not TokenNCCCInstallmentC5 && c5 is not TokenC5Installment;
                return rebateC5 && (nccc.Val(FIELD.F_3) ?? string.Empty).StartsWith("20")
                    ? TemplateKind.RebateRefund
                    : TemplateKind.Other;
            }
            catch (Exception)
            {
                return TemplateKind.Other;
            }
        }

        // Same rule as StressEngine.Strip2ByteHeader: the first two bytes (big-endian) equal the remaining length.
        private static bool HasLengthHeader(byte[] bytes)
        {
            return bytes.Length > 2 && ((bytes[0] << 8) | bytes[1]) == bytes.Length - 2;
        }

        // Folder names are mixed case (e.g. "Nccc" for TransactionType.NCCC); match case-insensitively
        // so the lookup also works on case-sensitive file systems (cross-platform publish).
        private static string ResolveTemplateDirectory(string dataRoot, string typeName)
        {
            if (Directory.Exists(dataRoot))
            {
                foreach (string dir in Directory.GetDirectories(dataRoot))
                {
                    if (string.Equals(Path.GetFileName(dir), typeName, StringComparison.OrdinalIgnoreCase))
                    {
                        return dir;
                    }
                }
            }
            return Path.Combine(dataRoot, typeName);
        }

        public static ConcurrentQueue<byte[]> CloneQueue(ConcurrentQueue<byte[]> templateQueue)
        {
            var newQueue = new ConcurrentQueue<byte[]>();
            foreach (var item in templateQueue)
            {
                byte[] copy = new byte[item.Length];
                Array.Copy(item, copy, item.Length);
                newQueue.Enqueue(copy);
            }
            return newQueue;
        }

        private static byte[] CreateMockMessage(TransactionType type, int sequenceNumber)
        {
            string seqStr = sequenceNumber.ToString("D6");
            string payloadStr;

            switch (type)
            {
                case TransactionType.NCCC:
                    payloadStr = $"0200F23A000108A0000016400800001234560000000001000729153000{seqStr}001001";
                    break;
                case TransactionType.FISC:
                    payloadStr = $"0200F23A000108A0000016400800006543210000000005000729153000{seqStr}002002";
                    break;
                case TransactionType.VISA:
                    payloadStr = $"0100F23A000108A00000164008000040000000000010000729153000{seqStr}003003";
                    break;
                case TransactionType.MasterCard:
                    payloadStr = $"0100F23A000108A00000164008000051000000000010000729153000{seqStr}004004";
                    break;
                default:
                    payloadStr = $"02000000000000000000{seqStr}";
                    break;
            }

            byte[] body = Encoding.ASCII.GetBytes(payloadStr);
            return ColaCommUtility.AddBinaryLength2BytesArray(body);
        }
    }
}
