// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="FISC.cs">
// </copyright>
// <summary>
//   Defines the FISCd type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.TM.FISC
{
    #region DEPENDENCY

    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using Com.Cybersoft4u.CyberUtils.Exception;
    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.Category.CardType;
    using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;
    using Com.Cybersoft4u.TM.ISO8583.Exception;
    using Com.Cybersoft4u.TM.ISO8583.V1987;

    using DataElementSpec = Com.Cybersoft4u.TM.ISO8583.DataElementSpec;

    #endregion

    /// <summary>
    /// FISC (Financial Information Service Center) ISO 8583 message implementation for Taiwan domestic payment processing.
    /// </summary>
    public class FISC : AbstractIso8583<DefaultCore>, ITransaction
    {
        private static readonly IDictionary<FIELD, FISCField> DataElementSpecs =
            new Dictionary<FIELD, FISCField>();

        private static readonly Packer Packers = new FISCPacker();

        static FISC()
        {
            //// Bit Map, Secondary
            DataElementSpecs.Add(FIELD.F_1, new FISCField(FIELD.F_1, 8, DEType.B, Packers));

            //// Primary Account Number (PAN)
            DataElementSpecs.Add(FIELD.F_2, new FISCField(FIELD.F_2, 19, DEType.LL, Packers));

            //// Processing Code
            DataElementSpecs.Add(FIELD.F_3, new FISCField(FIELD.F_3, 6, DEType.N, Packers));

            //// Amount, Transaction
            DataElementSpecs.Add(FIELD.F_4, new FISCField(FIELD.F_4, 12, DEType.N, Packers));

            //// Amount, Settlement
            DataElementSpecs.Add(FIELD.F_5, new FISCField(FIELD.F_5, 12, DEType.N, Packers));

            //// Amount, Cardholder Billing
            DataElementSpecs.Add(FIELD.F_6, new FISCField(FIELD.F_6, 12, DEType.N, Packers));

            //// Transmission Date and Time
            DataElementSpecs.Add(FIELD.F_7, new FISCField(FIELD.F_7, 10, DEType.N, Packers));

            //// Amount, Cardholder Billing Fee
            DataElementSpecs.Add(FIELD.F_8, new FISCField(FIELD.F_8, 8, DEType.N, Packers));

            //// Conversion Rate, Settlement
            DataElementSpecs.Add(FIELD.F_9, new FISCField(FIELD.F_9, 8, DEType.N, Packers));

            //// Conversion Rate, Cardholder Billing
            DataElementSpecs.Add(FIELD.F_10, new FISCField(FIELD.F_10, 8, DEType.N, Packers));

            //// System Trace Audit Number (STAN)
            DataElementSpecs.Add(FIELD.F_11, new FISCField(FIELD.F_11, 6, DEType.N, Packers));

            //// Time, Local Transaction
            DataElementSpecs.Add(FIELD.F_12, new FISCField(FIELD.F_12, 6, DEType.N, Packers));

            //// Date, Local Transaction
            DataElementSpecs.Add(FIELD.F_13, new FISCField(FIELD.F_13, 4, DEType.N, Packers));

            //// Date, Expiration
            DataElementSpecs.Add(FIELD.F_14, new FISCField(FIELD.F_14, 4, DEType.N, Packers));

            //// Date, Settlement
            DataElementSpecs.Add(FIELD.F_15, new FISCField(FIELD.F_15, 4, DEType.N, Packers));

            //// Date, Conversion
            DataElementSpecs.Add(FIELD.F_16, new FISCField(FIELD.F_16, 4, DEType.N, Packers));

            //// Date, Capture.
            DataElementSpecs.Add(FIELD.F_17, new FISCField(FIELD.F_17, 4, DEType.N, Packers));

            //// Merchant Type
            DataElementSpecs.Add(FIELD.F_18, new FISCField(FIELD.F_18, 4, DEType.N, Packers));

            //// Acquiring Institution Country Code
            DataElementSpecs.Add(FIELD.F_19, new FISCField(FIELD.F_19, 3, DEType.N, Packers));

            //// Primary Account Number (PAN) Country Code
            DataElementSpecs.Add(FIELD.F_20, new FISCField(FIELD.F_20, 3, DEType.N, Packers));

            //// Forwarding Institution Country Code
            DataElementSpecs.Add(FIELD.F_21, new FISCField(FIELD.F_21, 3, DEType.N, Packers));

            //// Point-of-Service (POS) Entry Mode
            DataElementSpecs.Add(FIELD.F_22, new FISCField(FIELD.F_22, 3, DEType.N, Packers));

            //// Card Sequence Number
            DataElementSpecs.Add(FIELD.F_23, new FISCField(FIELD.F_23, 3, DEType.N, Packers));

            //// Network International ID
            DataElementSpecs.Add(FIELD.F_24, new FISCField(FIELD.F_24, 3, DEType.N, Packers));

            //// Point-of-Service (POS) Condition Code
            DataElementSpecs.Add(FIELD.F_25, new FISCField(FIELD.F_25, 2, DEType.N, Packers));

            //// Point-of-Service (POS) Personal ID Number (PIN) Capture Code
            DataElementSpecs.Add(FIELD.F_26, new FISCField(FIELD.F_26, 2, DEType.N, Packers));

            //// Authorization ID Response Length
            DataElementSpecs.Add(FIELD.F_27, new FISCField(FIELD.F_27, 1, DEType.N, Packers));

            //// Amount, Transaction Fee
            DataElementSpecs.Add(FIELD.F_28, new FISCField(FIELD.F_28, 9, DEType.AN, Packers));

            //// Amount, Settlement Fee
            DataElementSpecs.Add(FIELD.F_29, new FISCField(FIELD.F_29, 9, DEType.AN, Packers));

            //// Amount, Transaction Processing Fee
            DataElementSpecs.Add(FIELD.F_30, new FISCField(FIELD.F_30, 9, DEType.AN, Packers));

            //// Amount, Settlement Processing Fee
            DataElementSpecs.Add(FIELD.F_31, new FISCField(FIELD.F_31, 9, DEType.AN, Packers));

            //// Acquiring Institution ID Code
            DataElementSpecs.Add(FIELD.F_32, new FISCField(FIELD.F_32, 6, DEType.LL, Packers));

            //// Forwarding Institution ID Code
            DataElementSpecs.Add(FIELD.F_33, new FISCField(FIELD.F_33, 6, DEType.LL, Packers));

            //// Primary Account Number (PAN), Extended
            DataElementSpecs.Add(FIELD.F_34, new FISCField(FIELD.F_34, 28, DEType.LL, Packers));

            //// Track 2 Data
            DataElementSpecs.Add(FIELD.F_35, new FISCField(FIELD.F_35, 37, DEType.LL, Packers));

            //// Track 3 Data
            DataElementSpecs.Add(FIELD.F_36, new FISCField(FIELD.F_36, 104, DEType.LLL, Packers));

            //// Retrieval Reference Number
            DataElementSpecs.Add(FIELD.F_37, new FISCField(FIELD.F_37, 12, DEType.AN, Packers));

            //// Authorization ID Response
            DataElementSpecs.Add(FIELD.F_38, new FISCField(FIELD.F_38, 6, DEType.ANS, Packers));

            //// Response Code
            DataElementSpecs.Add(FIELD.F_39, new FISCField(FIELD.F_39, 2, DEType.AN, Packers));

            //// Service Restriction Code
            DataElementSpecs.Add(FIELD.F_40, new FISCField(FIELD.F_40, 3, DEType.AN, Packers));

            //// Card Acceptor Terminal ID
            DataElementSpecs.Add(FIELD.F_41, new FISCField(FIELD.F_41, 8, DEType.ANS, Packers));

            //// Card Acceptor ID Code
            DataElementSpecs.Add(FIELD.F_42, new FISCField(FIELD.F_42, 15, DEType.ANS, Packers));

            //// Card Acceptor Name/Location
            DataElementSpecs.Add(FIELD.F_43, new FISCField(FIELD.F_43, 40, DEType.ANS, Packers));

            //// Additional Response Data
            DataElementSpecs.Add(FIELD.F_44, new FISCField(FIELD.F_44, 25, DEType.LL, Packers));

            //// Track 1 Data
            DataElementSpecs.Add(FIELD.F_45, new FISCField(FIELD.F_45, 76, DEType.LL, Packers));

            //// Expanded Additional Amounts
            DataElementSpecs.Add(FIELD.F_46, new FISCField(FIELD.F_46, 999, DEType.LLL, Packers));

            //// Additional Data
            DataElementSpecs.Add(FIELD.F_47, new FISCField(FIELD.F_47, 999, DEType.LLL, Packers));

            //// Additional Data
            DataElementSpecs.Add(FIELD.F_48, new FISCField(FIELD.F_48, 999, DEType.LLL, Packers));

            //// Currency Code, Transaction
            DataElementSpecs.Add(FIELD.F_49, new FISCField(FIELD.F_49, 3, DEType.N, Packers));

            //// Currency Code, Settlement
            DataElementSpecs.Add(FIELD.F_50, new FISCField(FIELD.F_50, 3, DEType.N, Packers));

            //// Currency Code, Cardholder Billing
            DataElementSpecs.Add(FIELD.F_51, new FISCField(FIELD.F_51, 3, DEType.N, Packers));

            //// Personal ID Number (PIN) Data
            DataElementSpecs.Add(FIELD.F_52, new FISCField(FIELD.F_52, 8, DEType.B, Packers));

            //// Security-Related Control Information
            DataElementSpecs.Add(FIELD.F_53, new FISCField(FIELD.F_53, 16, DEType.N, Packers));

            //// Additional Amounts
            DataElementSpecs.Add(FIELD.F_54, new FISCField(FIELD.F_54, 120, DEType.LLL, Packers));

            //// Integrated Circuit Card (ICC) System-Related Data
            DataElementSpecs.Add(FIELD.F_55, new FIELD_55(255));

            //// Payment Account Data
            DataElementSpecs.Add(FIELD.F_56, new FISCField(FIELD.F_56, 37, DEType.LLL, Packers));

            //// Reserved for National Use
            DataElementSpecs.Add(FIELD.F_57, new FISCField(FIELD.F_57, 999, DEType.LLL, Packers));

            //// Reserved for National Use
            DataElementSpecs.Add(FIELD.F_58, new FISCField(FIELD.F_58, 999, DEType.LLL, Packers));

            //// Reserved for National Use
            DataElementSpecs.Add(FIELD.F_59, new FISCField(FIELD.F_59, 999, DEType.LLL, Packers));

            //// Advice Reason Code
            DataElementSpecs.Add(FIELD.F_60, new FISCField(FIELD.F_60, 60, DEType.LLL, Packers));

            //// Point-of-Service (POS) Data
            DataElementSpecs.Add(FIELD.F_61, new FISCField(FIELD.F_61, 26, DEType.LLL, Packers));

            //// Intermediate Network Facility (INF) Data
            DataElementSpecs.Add(FIELD.F_62, new FISCField(FIELD.F_62, 100, DEType.LLL, Packers));

            //// Network Data
            DataElementSpecs.Add(FIELD.F_63, new FISCField(FIELD.F_63, 50, DEType.LLL, Packers));

            //// Message Authentication Code
            DataElementSpecs.Add(FIELD.F_64, new FISCField(FIELD.F_64, 8, DEType.B, Packers));

            //// Bit Map, Extended
            DataElementSpecs.Add(FIELD.F_65, new FISCField(FIELD.F_65, 8, DEType.B, Packers));

            //// Settlement Code
            DataElementSpecs.Add(FIELD.F_66, new FISCField(FIELD.F_66, 1, DEType.N, Packers));

            //// Extended Payment Code
            DataElementSpecs.Add(FIELD.F_67, new FISCField(FIELD.F_67, 2, DEType.N, Packers));

            //// Receiving Institution Country Code
            DataElementSpecs.Add(FIELD.F_68, new FISCField(FIELD.F_68, 3, DEType.N, Packers));

            //// Settlement Institution Country Code
            DataElementSpecs.Add(FIELD.F_69, new FISCField(FIELD.F_69, 3, DEType.N, Packers));

            //// Network Management Information Code
            DataElementSpecs.Add(FIELD.F_70, new FISCField(FIELD.F_70, 3, DEType.N, Packers));

            //// Message Number
            DataElementSpecs.Add(FIELD.F_71, new FISCField(FIELD.F_71, 4, DEType.N, Packers));

            //// Message Number Last
            DataElementSpecs.Add(FIELD.F_72, new FISCField(FIELD.F_72, 4, DEType.N, Packers));

            //// Date, Action
            DataElementSpecs.Add(FIELD.F_73, new FISCField(FIELD.F_73, 6, DEType.N, Packers));

            //// Credits, Number
            DataElementSpecs.Add(FIELD.F_74, new FISCField(FIELD.F_74, 10, DEType.N, Packers));

            //// Credits, Reversal Number
            DataElementSpecs.Add(FIELD.F_75, new FISCField(FIELD.F_75, 10, DEType.N, Packers));

            //// Debits, Number
            DataElementSpecs.Add(FIELD.F_76, new FISCField(FIELD.F_76, 10, DEType.N, Packers));

            //// Debits, Reversal Number
            DataElementSpecs.Add(FIELD.F_77, new FISCField(FIELD.F_77, 10, DEType.N, Packers));

            //// Transfers, Number
            DataElementSpecs.Add(FIELD.F_78, new FISCField(FIELD.F_78, 10, DEType.N, Packers));

            //// Transfers, Reversal Number
            DataElementSpecs.Add(FIELD.F_79, new FISCField(FIELD.F_79, 10, DEType.N, Packers));

            //// Inquiries, Number
            DataElementSpecs.Add(FIELD.F_80, new FISCField(FIELD.F_80, 10, DEType.N, Packers));

            //// Authorizations, Number
            DataElementSpecs.Add(FIELD.F_81, new FISCField(FIELD.F_81, 10, DEType.N, Packers));

            //// Credits, Processing Fee Amount
            DataElementSpecs.Add(FIELD.F_82, new FISCField(FIELD.F_82, 12, DEType.N, Packers));

            //// Credits, Transaction Fee Amount
            DataElementSpecs.Add(FIELD.F_83, new FISCField(FIELD.F_83, 12, DEType.N, Packers));

            //// Debits, Processing Fee Amount
            DataElementSpecs.Add(FIELD.F_84, new FISCField(FIELD.F_84, 12, DEType.N, Packers));

            //// Debits, Transaction Fee Amount
            DataElementSpecs.Add(FIELD.F_85, new FISCField(FIELD.F_85, 12, DEType.N, Packers));

            //// Credits, Amount
            DataElementSpecs.Add(FIELD.F_86, new FISCField(FIELD.F_86, 16, DEType.N, Packers));

            //// Credits, Reversal Amount
            DataElementSpecs.Add(FIELD.F_87, new FISCField(FIELD.F_87, 16, DEType.N, Packers));

            //// Debits, Amount
            DataElementSpecs.Add(FIELD.F_88, new FISCField(FIELD.F_88, 16, DEType.N, Packers));

            //// Debits, Reversal Amount
            DataElementSpecs.Add(FIELD.F_89, new FISCField(FIELD.F_89, 16, DEType.N, Packers));

            //// Original Data Elements
            DataElementSpecs.Add(FIELD.F_90, new FISCField(FIELD.F_90, 42, DEType.N, Packers));

            //// Issuer File Update Code
            DataElementSpecs.Add(FIELD.F_91, new FISCField(FIELD.F_91, 1, DEType.AN, Packers));

            //// File Security Code
            DataElementSpecs.Add(FIELD.F_92, new FISCField(FIELD.F_92, 2, DEType.AN, Packers));

            //// Response Indicator
            DataElementSpecs.Add(FIELD.F_93, new FISCField(FIELD.F_93, 5, DEType.AN, Packers));

            //// Service Indicator
            DataElementSpecs.Add(FIELD.F_94, new FISCField(FIELD.F_94, 7, DEType.AN, Packers));

            //// Replacement Amounts
            DataElementSpecs.Add(FIELD.F_95, new FISCField(FIELD.F_95, 42, DEType.N, Packers));

            //// Message Security Code
            DataElementSpecs.Add(FIELD.F_96, new FISCField(FIELD.F_96, 8, DEType.B, Packers));

            //// Amount, Net Settlement
            DataElementSpecs.Add(FIELD.F_97, new FISCField(FIELD.F_97, 17, DEType.AN, Packers));

            //// Payee
            DataElementSpecs.Add(FIELD.F_98, new FISCField(FIELD.F_98, 25, DEType.ANS, Packers));

            //// Settlement Instituition ID Code
            DataElementSpecs.Add(FIELD.F_99, new FISCField(FIELD.F_99, 11, DEType.LL, Packers));

            //// Receiving Institution ID Code
            DataElementSpecs.Add(FIELD.F_100, new FISCField(FIELD.F_100, 11, DEType.LL, Packers));

            //// File Name
            DataElementSpecs.Add(FIELD.F_101, new FISCField(FIELD.F_101, 17, DEType.LL, Packers));

            //// Account ID 1
            DataElementSpecs.Add(FIELD.F_102, new FISCField(FIELD.F_102, 28, DEType.LL, Packers));

            //// Account ID 2
            DataElementSpecs.Add(FIELD.F_103, new FISCField(FIELD.F_103, 28, DEType.LL, Packers));

            //// Transaction Description
            DataElementSpecs.Add(FIELD.F_104, new FISCField(FIELD.F_104, 100, DEType.LLL, Packers));

            //// Reserved for MasterCard Use
            DataElementSpecs.Add(FIELD.F_105, new FISCField(FIELD.F_105, 999, DEType.LLL, Packers));

            //// Reserved for MasterCard Use
            DataElementSpecs.Add(FIELD.F_106, new FISCField(FIELD.F_106, 999, DEType.LLL, Packers));

            //// Reserved for MasterCard Use
            DataElementSpecs.Add(FIELD.F_107, new FISCField(FIELD.F_107, 999, DEType.LLL, Packers));

            //// MoneySend Reference Data
            DataElementSpecs.Add(FIELD.F_108, new FISCField(FIELD.F_108, 999, DEType.LLL, Packers));

            //// Reserved for ISO Use
            DataElementSpecs.Add(FIELD.F_109, new FISCField(FIELD.F_109, 999, DEType.LLL, Packers));

            //// Reserved for ISO Use
            DataElementSpecs.Add(FIELD.F_110, new FISCField(FIELD.F_110, 999, DEType.LLL, Packers));

            //// Reserved for ISO Use
            DataElementSpecs.Add(FIELD.F_111, new FISCField(FIELD.F_111, 999, DEType.LLL, Packers));

            //// Additional Data (National Use)
            DataElementSpecs.Add(FIELD.F_112, new FISCField(FIELD.F_112, 100, DEType.LLL, Packers));

            //// Reserved for National Use
            DataElementSpecs.Add(FIELD.F_113, new FISCField(FIELD.F_113, 999, DEType.LLL, Packers));

            //// Reserved for National Use
            DataElementSpecs.Add(FIELD.F_114, new FISCField(FIELD.F_114, 999, DEType.LLL, Packers));

            //// Reserved for National Use
            DataElementSpecs.Add(FIELD.F_115, new FISCField(FIELD.F_115, 999, DEType.LLL, Packers));

            //// Reserved For National Use
            DataElementSpecs.Add(FIELD.F_116, new FISCField(FIELD.F_116, 999, DEType.LLL, Packers));

            //// Reserved for National Use
            DataElementSpecs.Add(FIELD.F_117, new FISCField(FIELD.F_117, 999, DEType.LLL, Packers));

            //// Reserved for National Use
            DataElementSpecs.Add(FIELD.F_118, new FISCField(FIELD.F_118, 999, DEType.LLL, Packers));

            //// Reserved for National Use
            DataElementSpecs.Add(FIELD.F_119, new FISCField(FIELD.F_119, 999, DEType.LLL, Packers));

            //// Record Data
            DataElementSpecs.Add(FIELD.F_120, new FISCField(FIELD.F_120, 999, DEType.LLL, Packers));

            //// Authorizing Agent ID Code
            DataElementSpecs.Add(FIELD.F_121, new FISCField(FIELD.F_121, 6, DEType.LLL, Packers));

            //// Additional Record Data
            DataElementSpecs.Add(FIELD.F_122, new FISCField(FIELD.F_122, 999, DEType.LLL, Packers));

            //// Receipt Free Text
            DataElementSpecs.Add(FIELD.F_123, new FISCField(FIELD.F_123, 512, DEType.LLL, Packers));

            //// Member-defined Data
            DataElementSpecs.Add(FIELD.F_124, new FISCField(FIELD.F_124, 999, DEType.LLB, Packers));

            //// New PIN Data
            DataElementSpecs.Add(FIELD.F_125, new FISCField(FIELD.F_125, 8, DEType.B, Packers));

            //// Private Data
            DataElementSpecs.Add(FIELD.F_126, new FISCField(FIELD.F_126, 100, DEType.LLL, Packers));

            //// Private Data
            DataElementSpecs.Add(FIELD.F_127, new FISCField(FIELD.F_127, 999, DEType.LLB, Packers));

            //// Message Authentication Code
            DataElementSpecs.Add(FIELD.F_128, new FISCField(FIELD.F_128, 8, DEType.B, Packers));
        }

        public FISC()
        {
            if (this.ExtendedCore() == null)
            {
                this.ExtendedCore(new FISCExtendedCore());
            }

            this.ConstructProcess();
        }

        public FISC(byte[] message)
            : this()
        {
            if (message != null)
            {
                this.Decode(message);
            }
        }

        public string Name()
        {
            return this.GetType().FullName;
        }

        public override string Identity()
        {
            var tmp = this.Core();
            return tmp == null ? this.Core().MessageTypeIndicator : tmp.MessageTypeIndicator + tmp.ProcessingCode;
        }

        public new DefaultCore Core()
        {
            return (DefaultCore)base.Core();
        }

        public new FISC Core(Core core)
        {
            base.Core(core);
            if (this.ExtendedCore() == null)
            {
                this.ExtendedCore(new FISCExtendedCore());
            }

            return this;
        }

        public FISCExtendedCore ExtendedCore()
        {
            return this.Extend<FISCExtendCoreKey, FISCExtendedCore>();
        }

        public FISC ExtendedCore(FISCExtendedCore core)
        {
            this.Extend<FISCExtendCoreKey>(ObjectUtils.CheckNotNull(core));
            return this;
        }

        public override DataElementSpec Spec(FIELD field)
        {
            FISCField spec;
            DataElementSpecs.TryGetValue(field, out spec);
            return spec;
        }

        public override Iso8583 Prepare()
        {
            return this;
        }

        public override Bytes Encode()
        {
            this.Logger.Debug(() => "Encoding...");
            this.Logger.Debug(() => $"MTI         :{this.Mti()}");

            var rtn = new StringBuilder();
            rtn.Append(EbcdicConverter.FromString(this.Mti()));
            rtn.Append(HexStringConverter.FromByteArray(this.Bitmap()));
            this.Logger.Debug(() => $"Bitmap 1    :{HexStringConverter.FromByteArray(this.Bitmap())}");

            foreach (var pair in this.Core().OrderedFields().Where(pair => pair.Value != null))
            {
                var key = pair.Key;
                var fieldData = ((ISO8583.V1987.DataElementSpec)this.Spec(pair.Key)).Pack(this);
                rtn.Append(fieldData);
                this.Logger.Debug(
                    () => $"{$"FIELD[{key}]".PadRight(12, ' ')}:{pair.Value}");
            }

            var dataOut = Bytes.From(HexStringConverter.ToByteArray(rtn.ToString()));
            this.Logger.Debug(() => "Encoded.");

            ListSpecialField();

            return dataOut;
        }

        public override ICodecable Decode(byte[] message)
        {
            try
            {
                this.Logger.Debug(() => $"Decoding... Hex String: {HexStringConverter.FromByteArray(message)}");
                var record = HexStringConverter.FromByteArray(message);
                this.Mti(EbcdicConverter.GetString(record.IndexSubstring(0, 8)));
                this.Logger.Debug(() => $"MTI         :{this.Mti()}");
                var bitmap = HexStringConverter.ToByteArray(record.IndexSubstring(8, 24));
                this.Logger.Debug(() => $"Bitmap 1    :{HexStringConverter.FromByteArray(bitmap)}");
                this.UnpackDataElement(bitmap, record.Substring(24), 0);
                this.FillVariables();

                this.Logger.Debug(() => "Decoded.");

                ListSpecialField();

            }
            catch (FieldDecodeException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new DecodeFailException(e);
            }

            return this;
        }

        public string F3ProcessingCodeEncode()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(this.ExtendedCore().ProcessingCode.CardholderTransactionTypeCode);
            stringBuilder.Append(this.ExtendedCore().ProcessingCode.CardholderFromAccountTypeCode);
            stringBuilder.Append(this.ExtendedCore().ProcessingCode.CardholderToAccountTypeCode);

            var rtn = stringBuilder.ToString();
            if (rtn.Length > 0)
            {
                return rtn;
            }

            return null;
        }

        public void F3ProcessingCodeDecode(string val)
        {
            this.ExtendedCore().ProcessingCode = new ProcessingCode();

            if (string.IsNullOrEmpty(val))
            {
                return;
            }

            var finalVal = val;
            try
            {
                this.ExtendedCore().ProcessingCode.CardholderTransactionTypeCode = finalVal.IndexSubstring(0, 2);
                this.ExtendedCore().ProcessingCode.CardholderFromAccountTypeCode = finalVal.IndexSubstring(2, 4);
                this.ExtendedCore().ProcessingCode.CardholderToAccountTypeCode = finalVal.IndexSubstring(4, 6);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public string F5Encode()
        {
            return this.Core().GetOthersValByKey("F_5") as string;
        }

        public void F5Decode(string val)
        {
            this.Core().SetOthersValByKey("F_5", val);
        }

        public string F9Encode()
        {
            return this.Core().GetOthersValByKey("F_9") as string;
        }

        public void F9Decode(string val)
        {
            this.Core().SetOthersValByKey("F_9", val);
        }

        public string F20Encode()
        {
            return this.Core().GetOthersValByKey("F_20") as string;
        }

        public void F20Decode(string val)
        {
            this.Core().SetOthersValByKey("F_20", val);
        }

        public string F22PointOfServiceEntryModeEncode()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(this.ExtendedCore().PointOfServiceEntryMode.PosTerminalPanEntryMode);
            stringBuilder.Append(this.ExtendedCore().PointOfServiceEntryMode.PosTerminalPinEntryMode);

            var rtn = stringBuilder.ToString();
            if (rtn.Length > 0)
            {
                return rtn;
            }

            return null;
        }

        public void F22PointOfServiceEntryModeDecode(string val)
        {
            this.ExtendedCore().PointOfServiceEntryMode = new PointOfServiceEntryMode();

            if (string.IsNullOrEmpty(val))
            {
                return;
            }

            var finalVal = val;
            try
            {
                this.ExtendedCore().PointOfServiceEntryMode.PosTerminalPanEntryMode = finalVal.IndexSubstring(0, 2);
                this.ExtendedCore().PointOfServiceEntryMode.PosTerminalPinEntryMode = finalVal.IndexSubstring(2, 3);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public string F28Encode()
        {
            return this.Core().GetOthersValByKey("F_28") as string;
        }

        public void F28Decode(string val)
        {
            this.Core().SetOthersValByKey("F_28", val);
        }

        public string F35Encode()
        {
            return this.Core().GetOthersValByKey("F_35") as string;
        }

        public void F35Decode(string val)
        {
            this.Core().SetOthersValByKey("F_35", val);
        }

        public string F43CardAcceptorNameLocationEncode()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(this.ExtendedCore().CardAcceptorNameLocation.CardAcceptorName);
            stringBuilder.Append(" ");
            stringBuilder.Append(this.ExtendedCore().CardAcceptorNameLocation.CardAcceptorCity);
            stringBuilder.Append(" ");
            stringBuilder.Append(this.ExtendedCore().CardAcceptorNameLocation.CardAcceptorStateCountryCode);

            var rtn = stringBuilder.ToString();
            if (rtn.Length > 0)
            {
                return rtn;
            }

            return null;
        }

        public void F43CardAcceptorNameLocationDecode(string val)
        {
            this.ExtendedCore().CardAcceptorNameLocation = new CardAcceptorNameLocation();

            this.ExtendedCore().CardAcceptorNameLocation.CardAcceptorName = val.IndexSubstring(0, 22);
            this.ExtendedCore().CardAcceptorNameLocation.CardAcceptorCity = val.IndexSubstring(23, 36);
            this.ExtendedCore().CardAcceptorNameLocation.CardAcceptorStateCountryCode = val.IndexSubstring(37, 40);
        }

        public string F44Encode()
        {
            return this.Core().GetOthersValByKey("F_44") as string;
        }

        public void F44Decode(string val)
        {
            this.Core().SetOthersValByKey("F_44", val);
        }

        public string F45Encode()
        {
            return this.Core().GetOthersValByKey("F_45") as string;
        }

        public void F45Decode(string val)
        {
            this.Core().SetOthersValByKey("F_45", val);
        }

        public string F48AdditionalDataPrivateUseEncode()
        {
            var stringBuilder = new StringBuilder();

            if (this.Mti().StartsWith("08"))
            {
                stringBuilder.Append(this.ExtendedCore().AdditionalDataPrivateUse.Subelement.Encode());
            }
            else
            {
                //if (!((this is TM.MasterCard.Transaction.AlternativeNetworkRoutingRequest) && (string.IsNullOrEmpty(this.ExtendedCore().AdditionalDataPrivateUse.Subelement.Encode()))))
                //{
                stringBuilder.Append(this.ExtendedCore().AdditionalDataPrivateUse.TransactionCategoryCode);
                stringBuilder.Append(this.ExtendedCore().AdditionalDataPrivateUse.Subelement.Encode());
                //}
            }

            var rtn = stringBuilder.ToString();
            if (rtn.Length > 0)
            {
                return rtn;
            }

            return null;
        }

        public void F48AdditionalDataPrivateUseDecode(string val)
        {
            this.ExtendedCore().AdditionalDataPrivateUse = new AdditionalDataPrivateUse();

            if (string.IsNullOrEmpty(val))
            {
                return;
            }

            var finalVal = val;
            try
            {
                if (this.Mti().StartsWith("08"))
                {
                    this.ExtendedCore().AdditionalDataPrivateUse.Subelement.Decode(finalVal);
                }
                else
                {
                    this.ExtendedCore().AdditionalDataPrivateUse.TransactionCategoryCode = finalVal.IndexSubstring(0, 1);
                    this.ExtendedCore().AdditionalDataPrivateUse.Subelement.Decode(finalVal.Substring(1));
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public string F56PaymentAccountDataEncode()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(this.ExtendedCore().AdditionalDataPrivateUse.Subelement.Encode());
            var rtn = stringBuilder.ToString();
            if (rtn.Length > 0)
            {
                return rtn;
            }

            return null;
        }

        //public void F56PaymentAccountDataDecode(string val)
        //{
        //    this.ExtendedCore().PaymentAccountData = new PaymentAccountData();
        //
        //    if (string.IsNullOrEmpty(val))
        //    {
        //        return;
        //    }
        //
        //    var finalVal = val;
        //    try
        //    {
        //        this.ExtendedCore().PaymentAccountData.Subelement.Decode(finalVal);
        //    }
        //    catch (Exception)
        //    {
        //        // ignored
        //    }
        //}

        public string F50Encode()
        {
            return this.Core().GetOthersValByKey("F_50") as string;
        }

        public void F50Decode(string val)
        {
            this.Core().SetOthersValByKey("F_50", val);
        }

        public string F53Encode()
        {
            return this.Core().GetOthersValByKey("F_53") as string;
        }

        public void F53Decode(string val)
        {
            this.Core().SetOthersValByKey("F_53", val);
        }

        public string F54Encode()
        {
            return this.Core().GetOthersValByKey("F_54") as string;
        }

        public void F54Decode(string val)
        {
            this.Core().SetOthersValByKey("F_54", val);
        }

        public string F55EmvChipEncode()
        {
            var valByteBuf = this.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Encode();
            var valByteArray = valByteBuf.Array();

            if (valByteArray.Length > 0)
            {
                return HexStringConverter.FromByteArray(valByteArray);
            }

            return null;
        }

        public void F55EmvChipDecode(string val)
        {
            var valByteArray = HexStringConverter.ToByteArray(val);
            this.ExtendedCore().IntegratedCircuitCardSystemRelatedData = new FISCTLV().Decode(valByteArray);
        }
        public void F58Decode(string val)
        {
            this.ExtendedCore().FiscAdditionalDataPrivateUse = new Dictionary<string, string>();
            try
            {
                int startPosition = 0;
                int totalLength = val.Length;
                while (startPosition < totalLength)
                {
                    var offset = 2;
                    var subtoken = val.Substring(startPosition, offset);
                    startPosition += offset;

                    offset = 2;
                    var subtokenLength = Convert.ToInt16(val.Substring(startPosition, offset));
                    startPosition += offset;

                    offset = subtokenLength;
                    var subtokenData = val.Substring(startPosition, offset);
                    startPosition += offset;
                    this.ExtendedCore().FiscAdditionalDataPrivateUse.Add(subtoken, subtokenData);
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }
        public string F58Encode()
        {
            try
            {
                if (this.ExtendedCore().FiscAdditionalDataPrivateUse == null ||
                    this.ExtendedCore().FiscAdditionalDataPrivateUse.Count == 0)
                    return String.Empty;
                var stringBuilder = new StringBuilder();

                foreach (var pair in this.ExtendedCore().FiscAdditionalDataPrivateUse)
                    stringBuilder.Append($"{pair.Key}{pair.Value.Length:00}{pair.Value}");

                return stringBuilder.ToString();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public string F60AdviceReasonCodeEncode()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(this.ExtendedCore().AdviceReasonCode.AdviceReasonCodeSubfield);
            stringBuilder.Append(this.ExtendedCore().AdviceReasonCode.AdviceDetailCode);
            stringBuilder.Append(this.ExtendedCore().AdviceReasonCode.AdviceDetailText);

            var rtn = stringBuilder.ToString();
            if (rtn.Length > 0)
            {
                return rtn;
            }

            return null;
        }

        public void F60AdviceReasonCodeDecode(string val)
        {
            this.ExtendedCore().AdviceReasonCode = new AdviceReasonCode();

            if (string.IsNullOrEmpty(val))
            {
                return;
            }

            var finalVal = val;
            try
            {
                this.ExtendedCore().AdviceReasonCode.AdviceReasonCodeSubfield = finalVal.IndexSubstring(0, 3);
                this.ExtendedCore().AdviceReasonCode.AdviceDetailCode = finalVal.IndexSubstring(3, 7);
                this.ExtendedCore().AdviceReasonCode.AdviceDetailText = finalVal.Substring(7);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public string F61PointOfServiceDataEncode()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(this.ExtendedCore().PointOfServiceData.PosTerminalAttendance);
            stringBuilder.Append(this.ExtendedCore().PointOfServiceData.PosTerminalOperator);
            stringBuilder.Append(this.ExtendedCore().PointOfServiceData.PosTerminalLocation);
            stringBuilder.Append(this.ExtendedCore().PointOfServiceData.PosCustomerPresence);
            stringBuilder.Append(this.ExtendedCore().PointOfServiceData.PosCardPresence);
            stringBuilder.Append(this.ExtendedCore().PointOfServiceData.PosCardRetention);
            stringBuilder.Append(this.ExtendedCore().PointOfServiceData.PosTransactionStatus);
            stringBuilder.Append(this.ExtendedCore().PointOfServiceData.PosTransactionSecurity);
            stringBuilder.Append(this.ExtendedCore().PointOfServiceData.PosTransactionRouting);
            stringBuilder.Append(this.ExtendedCore().PointOfServiceData.PosTerminalType);
            stringBuilder.Append(this.ExtendedCore().PointOfServiceData.PosEntryCapability);
            stringBuilder.Append(this.ExtendedCore().PointOfServiceData.PosOtherData);

            var rtn = stringBuilder.ToString();
            if (rtn.Length > 0)
            {
                return rtn;
            }

            return null;
        }

        public void F61PointOfServiceDataDecode(string val)
        {
            this.ExtendedCore().PointOfServiceData = new PointOfServiceData();

            if (string.IsNullOrEmpty(val))
            {
                return;
            }

            var finalVal = val;
            try
            {
                this.ExtendedCore().PointOfServiceData.PosTerminalAttendance = finalVal.IndexSubstring(0, 1);
                this.ExtendedCore().PointOfServiceData.PosTerminalOperator = finalVal.IndexSubstring(1, 2);
                this.ExtendedCore().PointOfServiceData.PosTerminalLocation = finalVal.IndexSubstring(2, 3);
                this.ExtendedCore().PointOfServiceData.PosCustomerPresence = finalVal.IndexSubstring(3, 4);
                this.ExtendedCore().PointOfServiceData.PosCardPresence = finalVal.IndexSubstring(4, 5);

                this.ExtendedCore().PointOfServiceData.PosCardRetention = finalVal.IndexSubstring(5, 6);
                this.ExtendedCore().PointOfServiceData.PosTransactionStatus = finalVal.IndexSubstring(6, 7);
                this.ExtendedCore().PointOfServiceData.PosTransactionSecurity = finalVal.IndexSubstring(7, 8);
                this.ExtendedCore().PointOfServiceData.PosTransactionRouting = finalVal.IndexSubstring(8, 9);
                this.ExtendedCore().PointOfServiceData.PosTerminalType = finalVal.IndexSubstring(9, 10);

                this.ExtendedCore().PointOfServiceData.PosEntryCapability = finalVal.IndexSubstring(10, 11);
                this.ExtendedCore().PointOfServiceData.PosOtherData = finalVal.IndexSubstring(11, finalVal.Length - 1);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public string F62Encode()
        {
            return this.Core().GetOthersValByKey("F_62") as string;
        }

        public void F62Decode(string val)
        {
            this.Core().SetOthersValByKey("F_62", val);
        }

        public string F63NetworkDataEncode()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(this.ExtendedCore().NetworkData.FinancialNetworkCode);
            stringBuilder.Append(this.ExtendedCore().NetworkData.BanknetReferenceNumber);

            var rtn = stringBuilder.ToString();
            if (rtn.Length > 0)
            {
                return rtn;
            }

            return null;
        }

        public void F63NetworkDataDecode(string val)
        {
            this.ExtendedCore().NetworkData = new NetworkData();

            if (string.IsNullOrEmpty(val))
            {
                return;
            }

            var finalVal = val;
            try
            {
                this.ExtendedCore().NetworkData.FinancialNetworkCode = finalVal.IndexSubstring(0, 3);
                this.ExtendedCore().NetworkData.BanknetReferenceNumber = finalVal.Substring(3);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public string F90OriginalDataElementsEncode()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(this.ExtendedCore().OriginalDataElements.OriginalMessageTypeIdentifier);
            stringBuilder.Append(this.ExtendedCore().OriginalDataElements.OriginalSystemsTraceAuditNumber);
            stringBuilder.Append(this.ExtendedCore().OriginalDataElements.OriginalTransmissionDateAndTime);
            stringBuilder.Append(this.ExtendedCore().OriginalDataElements.OriginalAcquiringInstitutionIdCode);
            stringBuilder.Append(this.ExtendedCore().OriginalDataElements.OriginalForwardingInstitutionIdCode);

            var rtn = stringBuilder.ToString();
            if (rtn.Length > 0)
            {
                return rtn;
            }

            return null;
        }

        public string F94ServiceIndicatorEncode()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(this.ExtendedCore().ServiceIndicator.ReservedForFutureUse);
            stringBuilder.Append(this.ExtendedCore().ServiceIndicator.AcquirerIssuerIndicator);
            stringBuilder.Append(this.ExtendedCore().ServiceIndicator.AddressDataIndicator);
            stringBuilder.Append(this.ExtendedCore().ServiceIndicator.Filler);

            var rtn = stringBuilder.ToString();
            if (rtn.Length > 0)
            {
                return rtn;
            }

            return null;
        }

        public void F94ServiceIndicatorDecode(string val)
        {
            this.ExtendedCore().ServiceIndicator = new ServiceIndicator();

            if (string.IsNullOrEmpty(val))
            {
                return;
            }

            var finalVal = val;
            try
            {
                this.ExtendedCore().ServiceIndicator.ReservedForFutureUse = finalVal.IndexSubstring(0, 1);
                this.ExtendedCore().ServiceIndicator.AcquirerIssuerIndicator = finalVal.IndexSubstring(1, 2);
                this.ExtendedCore().ServiceIndicator.AddressDataIndicator = finalVal.IndexSubstring(2, 3);
                this.ExtendedCore().ServiceIndicator.Filler = finalVal.Substring(3);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public string F120Encode()
        {
            return this.Core().GetOthersValByKey("F_120") as string;
        }

        public void F120Decode(string val)
        {
            this.Core().SetOthersValByKey("F_120", val);
        }
        public string F124Encode()
        {
            return this.Core().GetOthersValByKey("F_124") as string;
        }

        public void F124Decode(string val)
        {
            this.Core().SetOthersValByKey("F_124", val);
        }
        public string F127Encode()
        {
            return this.Core().GetOthersValByKey("F_127") as string;
        }

        public void F127Decode(string val)
        {
            this.Core().SetOthersValByKey("F_127", val);
        }

        public bool IsActivationCodeNotification()
        {
            return !string.IsNullOrEmpty(this.ExtendedCore().AdviceReasonCode.AdviceReasonCodeSubfield)
                    && this.ExtendedCore()
                           .AdviceReasonCode.AdviceReasonCodeSubfield.Equals(FISCTransactionConst.AdviceReasonCode.AdviceReasonCodeSubfield.McDigitalEnableServiceAdviceIssuer)
                    && !string.IsNullOrEmpty(this.ExtendedCore().AdviceReasonCode.AdviceDetailCode)
                    && this.ExtendedCore()
                           .AdviceReasonCode.AdviceDetailCode.Equals(FISCTransactionConst.AdviceReasonCode.AdviceDetailCode.ActivationCodeNotification);
        }

        protected override void ConstructProcess()
        {
            if (this.ExtendedCore() == null)
            {
                this.ExtendedCore(new FISCExtendedCore());
            }
        }

        protected void UnpackDataElement(byte[] bitmap, string record, int elementOffset)
        {
            const int DefaultBitmapLength = 64;
            var bitSetIndex = 64;
            var arrangedBitMap = new Bytes();
            for (var i = bitmap.Length - 1; i > -1; i--)
            {
                arrangedBitMap.Append(bitmap[i]);
            }

            var bitmapSet = new BitSet(arrangedBitMap);
            while (true)
            {
                bitSetIndex = bitmapSet.PreviousSetBit(bitSetIndex - 1);
                if (bitSetIndex < 0)
                {
                    break;
                }

                var currentField = FIELD.F_UKN;
                try
                {
                    currentField = FIELDMethods.Val((DefaultBitmapLength - bitSetIndex + elementOffset).ToString());
                    var spec = (ISO8583.V1987.DataElementSpec)this.Spec(currentField);
                    record = spec.Unpack(this, record);
                    this.Logger.Debug(() => $"{$"FIELD[{FIELDMethods.Val(currentField)}]".PadRight(12, ' ')}:{this.Val(currentField)}");
                }
                catch (FieldDecodeException)
                {
                    throw;
                }
                catch (Exception)
                {
                    throw new FieldDecodeException(currentField, $"Field[{currentField}] decode failed.");
                }
            }

            if (!string.IsNullOrEmpty(this.Val(FIELD.F_1)) && !string.IsNullOrEmpty(record))
            {
                this.UnpackDataElement(HexStringConverter.ToByteArray(this.Val(FIELD.F_1)), record, 64);
            }
        }

        protected StringBuilder DataFieldCombine(byte[] bitmap, string record, int elementOffset)
        {
            StringBuilder DataStringBuilder = new StringBuilder();
            const int DefaultBitmapLength = 64;
            var bitSetIndex = 64;
            var arrangedBitMap = new Bytes();
            for (var i = bitmap.Length - 1; i > -1; i--)
            {
                arrangedBitMap.Append(bitmap[i]);
            }

            var bitmapSet = new BitSet(arrangedBitMap);

            var currentField = FIELD.F_64;
            var currentFieldReg = FIELD.F_UKN;
            if (elementOffset == 128)
            {
                currentFieldReg = FIELD.F_128;
            }
            if (elementOffset == 64)
            {
                currentFieldReg = FIELD.F_64;
            }

            while (true)
            {
                bitSetIndex = bitmapSet.PreviousSetBit(bitSetIndex - 1);
                if (bitSetIndex < 0)
                {
                    break;
                }

                currentField = FIELD.F_UKN;
                try
                {
                    currentField = FIELDMethods.Val((DefaultBitmapLength - bitSetIndex + elementOffset).ToString());

                    DataStringBuilder.Append(DataFieldCombineFiller(currentFieldReg, Convert.ToInt16(currentField - currentFieldReg - 1)));

                    currentFieldReg = currentField;

                    var spec = (ISO8583.V1987.DataElementSpec)this.Spec(currentField);
                    record = spec.Unpack(this, record);
                    DataStringBuilder.Append("|");
                    DataStringBuilder.Append(this.Val(currentField));
                }
                catch (FieldDecodeException)
                {
                    throw;
                }
                catch (Exception)
                {
                    throw new FieldDecodeException(currentField, $"Field[{currentField}] decode failed.");
                }
            }

            currentField = FIELD.F_64;
            if (elementOffset == 64)
            {
                currentField = FIELD.F_128;
            }
            if (elementOffset == 128)
            {
                currentField = FIELD.F_192;
            }

            DataStringBuilder.Append(DataFieldCombineFiller(currentFieldReg, Convert.ToInt16(currentField - currentFieldReg)));

            if (!string.IsNullOrEmpty(this.Val(FIELD.F_1)) && !string.IsNullOrEmpty(record))
            {
                DataStringBuilder.Append(this.DataFieldCombine(HexStringConverter.ToByteArray(this.Val(FIELD.F_1)), record, 64));
            }

            return DataStringBuilder;
        }

        protected StringBuilder DataFieldCombineFiller(FIELD currentFieldReg, int FillerCount)
        {
            StringBuilder DataStringFillerBuilder = new StringBuilder();
            var FieldSkip = Convert.ToUInt16(currentFieldReg) + 1;
            for (var i = FillerCount; i > 0; i--)
            {
                DataStringFillerBuilder.Append("|");
                //DataStringFillerBuilder.Append($"FDMY{(FieldSkip++).ToString("000")}");
            }

            return DataStringFillerBuilder;
        }

        protected void F90OriginalDataElementsDecode(string val)
        {
            this.ExtendedCore().OriginalDataElements = new OriginalDataElements();

            if (string.IsNullOrEmpty(val))
            {
                return;
            }

            var finalVal = val;
            try
            {
                this.ExtendedCore().OriginalDataElements.OriginalMessageTypeIdentifier = finalVal.IndexSubstring(0, 4);
                this.ExtendedCore().OriginalDataElements.OriginalSystemsTraceAuditNumber = finalVal.IndexSubstring(
                    4,
                    10);
                this.ExtendedCore().OriginalDataElements.OriginalTransmissionDateAndTime = finalVal.IndexSubstring(
                    10,
                    20);
                this.ExtendedCore().OriginalDataElements.OriginalAcquiringInstitutionIdCode = finalVal.IndexSubstring(
                    20,
                    31);
                this.ExtendedCore().OriginalDataElements.OriginalForwardingInstitutionIdCode =
                    finalVal.IndexSubstring(31, 42);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void FillVariables()
        {
            foreach (var pair in this.Core().Fields())
            {
                try
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    this.Logger.Debug(() => $"FILL FIELD {key} with VALUE {value}");
                    this.MatchAndFill(pair.Key, pair.Value as string);
                }
                catch (Exception e)
                {
                    throw new ConvertException(e);
                }
            }
        }

        private void MatchAndFill(FIELD field, string val)
        {
            switch (field)
            {
                case FIELD.F_1:
                    this.Core().NullSecondBitmap = val.Equals("0000000000000000");
                    break;
                case FIELD.F_2:
                    this.Core().Pan = val;
                    break;
                case FIELD.F_3:
                    this.F3ProcessingCodeDecode(val);
                    break;
                case FIELD.F_4:
                    this.Core().TransactionAmount = val;
                    break;
                case FIELD.F_5:
                    this.F5Decode(val);
                    break;
                case FIELD.F_6:
                    this.ExtendedCore().CardholderBillingAmount = val;
                    break;
                case FIELD.F_7:
                    this.Core().TransmissionDatetime = val;
                    break;
                case FIELD.F_9:
                    this.F9Decode(val);
                    break;
                case FIELD.F_10:
                    this.ExtendedCore().CardholderBillingConversionRate = val;
                    break;
                case FIELD.F_11:
                    this.Core().SystemTraceAuditNumber = val;
                    break;
                case FIELD.F_12:
                    this.Core().LocalTransactionTime = val;
                    break;
                case FIELD.F_13:
                    this.Core().LocalTransactionDate = val;
                    break;
                case FIELD.F_14:
                    this.Core().ExpireDate = val;
                    break;
                case FIELD.F_15:
                    this.Core().SettlementDate = val;
                    break;
                case FIELD.F_16:
                    this.ExtendedCore().ConversionDate = val;
                    break;
                case FIELD.F_18:
                    this.Core().MerchantCategoryCode = val;
                    break;
                case FIELD.F_20:
                    this.F20Decode(val);
                    break;
                case FIELD.F_22:
                    this.F22PointOfServiceEntryModeDecode(val);
                    break;
                case FIELD.F_23:
                    this.ExtendedCore().CardSequenceNumber = val;
                    break;
                case FIELD.F_25:
                    this.ExtendedCore().PointOfServiceConditionCode = val;
                    break;
                case FIELD.F_28:
                    this.F28Decode(val);
                    break;
                case FIELD.F_32:
                    this.Core().AcquiringInstitutionIdentificationCode = val;
                    break;
                case FIELD.F_33:
                    this.ExtendedCore().ForwardingInstitutionIdCode = val;
                    break;
                case FIELD.F_35:
                    this.F35Decode(val);
                    this.SetCardType();
                    break;
                case FIELD.F_37:
                    this.Core().RetrievalReferenceNumber = val;
                    break;
                case FIELD.F_38:
                    this.Core().ApproveCode = val;
                    break;
                case FIELD.F_39:
                    this.Core().ResponseCode = val;
                    break;
                case FIELD.F_41:
                    this.Core().TerminalId = val.Trim();
                    break;
                case FIELD.F_42:
                    this.Core().MerchantId = val.Trim();
                    break;
                case FIELD.F_43:
                    this.F43CardAcceptorNameLocationDecode(val);
                    break;
                case FIELD.F_44:
                    this.F44Decode(val);
                    break;
                case FIELD.F_45:
                    this.F45Decode(val);
                    break;
                case FIELD.F_48:
                    this.F48AdditionalDataPrivateUseDecode(val);
                    break;
                case FIELD.F_49:
                    this.Core().TransactionCurrencyCode = val;
                    break;
                case FIELD.F_50:
                    this.F50Decode(val);
                    break;
                case FIELD.F_51:
                    this.ExtendedCore().CardholderBillingCurrencyCode = val;
                    break;
                case FIELD.F_52:
                    this.Core().Pinblock = val;
                    break;
                case FIELD.F_53:
                    this.F53Decode(val);
                    break;
                case FIELD.F_54:
                    this.F54Decode(val);
                    break;
                case FIELD.F_55:
                    this.F55EmvChipDecode(val);
                    break;
                //case FIELD.F_56:
                //    this.F56PaymentAccountDataDecode(val);
                //    break;
                case FIELD.F_58:
                    this.F58Decode(val);
                    break;
                case FIELD.F_60:
                    this.F60AdviceReasonCodeDecode(val);
                    break;
                case FIELD.F_61:
                    this.F61PointOfServiceDataDecode(val);
                    break;
                case FIELD.F_62:
                    this.F62Decode(val);
                    break;
                case FIELD.F_63:
                    this.F63NetworkDataDecode(val);
                    break;
                case FIELD.F_70:
                    this.Core().NetworkManagementInformationCode = val;
                    break;
                case FIELD.F_90:
                    this.F90OriginalDataElementsDecode(val);
                    break;
                case FIELD.F_91:
                    this.ExtendedCore().IssuerFileUpdateCode = val;
                    break;
                case FIELD.F_93:
                    this.ExtendedCore().ResponseIndicator = val;
                    break;
                case FIELD.F_94:
                    this.F94ServiceIndicatorDecode(val);
                    break;
                case FIELD.F_95:
                    this.Core().ReplacementAmount = val;
                    break;
                case FIELD.F_96:
                    this.ExtendedCore().MessageSecurityCode = val;
                    break;
                case FIELD.F_100:
                    this.ExtendedCore().ReceivingInstitutionIdCode = val;
                    break;
                case FIELD.F_101:
                    this.ExtendedCore().FileName = val;
                    break;
                case FIELD.F_120:
                    this.F120Decode(val);
                    break;
                case FIELD.F_121:
                    this.ExtendedCore().AuthorizingAgentIdCode = val;
                    break;
                //case FIELD.F_124:
                //    this.F124MemberDefinedDataDecode(val);
                //    break;
                case FIELD.F_127:
                    this.F127Decode(val);
                    break;
            }
        }

        private void SetCardType()
        {
            string track2Data = this.F35Encode();
            int startIndex = track2Data.IndexOfAny(new char[] { '=', 'D', '~', '^' });
            string cardNumber;
            if (startIndex > 0)
                cardNumber = track2Data.Substring(0, startIndex);
            else
                cardNumber = track2Data;
            if (Regex.Match(cardNumber, @"^(?:2131|1800|35\d{3})\d{11}$").Success)
            {
                this.Core().CardType = CardType.Get<ISO8583.Category.CardType.JCB>();
            }
            else if (Regex.Match(cardNumber, @"^4[0-9]{12}(?:[0-9]{3})?$").Success)
            {
                this.Core().CardType = CardType.Get<ISO8583.Category.CardType.VISA>();
            }
            else if (Regex.Match(cardNumber, @"^(?:5[1-5][0-9]{2}|222[1-9]|22[3-9][0-9]|2[3-6][0-9]{2}|27[01][0-9]|2720)[0-9]{12}$").Success)
            {
                this.Core().CardType = CardType.Get<ISO8583.Category.CardType.MasterCard>();
            }
        }


        private void ListSpecialField()
        {
            foreach (var tag in this.ExtendedCore().AdditionalDataPrivateUse.Subelement.Tags)
            {
                //this.Logger.Debug(() => $"Field 48 [{tag.Key}] = {tag.Value.Encode()}");
                if ((tag.Key.Equals("44")) || (tag.Key.Equals("72")))
                {
                    Encoding enc = Encoding.GetEncoding("IBM037");
                    byte[] bytes = enc.GetBytes(tag.Value.Encode());
                    this.Logger.Debug(() => $"Field 48 [{tag.Key}] = Hex String[{CommonUtils.HexBinary2HexString(bytes)}]");
                }
                else
                {
                    this.Logger.Debug(() => $"Field 48 [{tag.Key}] = {tag.Value.Encode()}");
                }
            }

            foreach (var tag in this.ExtendedCore().IntegratedCircuitCardSystemRelatedData.Tags)
            {
                this.Logger.Debug(() => $"Field 55 [{tag.Key}] = {HexStringConverter.FromByteArray(tag.Value.Value)}");
            }

            //foreach (var tag in this.ExtendedCore().PaymentAccountData.Subelement.Tags)
            //{
            //  this.Logger.Debug(() => $"Field 56 [{tag.Key}] = {tag.Value.Encode()}");
            //}
        }

    }
}
