namespace Com.Cybersoft4u.TM.ISO8583.V1987
{
    #region DEPENDENCY

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.Exception;

    #endregion

    public class Iso8583V1987<T> : AbstractIso8583<T>
        where T : Core
    {
        private static readonly IDictionary<FIELD, DefaultField> DataElementSpecs =
            new Dictionary<FIELD, DefaultField>();

        private static readonly Packer Packers = new DefaultPacker();

        static Iso8583V1987()
        {
            //// Bit Map Extended
            DataElementSpecs.Add(FIELD.F_1, new DefaultField(FIELD.F_1, 16, DEType.B, Packers));

            //// Primary account number (PAN)
            DataElementSpecs.Add(FIELD.F_2, new DefaultField(FIELD.F_2, 19, DEType.LL, Packers));

            //// Processing code
            DataElementSpecs.Add(FIELD.F_3, new DefaultField(FIELD.F_3, 6, DEType.N, Packers));

            //// Amount transaction
            DataElementSpecs.Add(FIELD.F_4, new DefaultField(FIELD.F_4, 12, DEType.N, Packers));

            //// Amount reconciliation
            DataElementSpecs.Add(FIELD.F_5, new DefaultField(FIELD.F_5, 12, DEType.N, Packers));

            //// Amount cardholder billing
            DataElementSpecs.Add(FIELD.F_6, new DefaultField(FIELD.F_6, 12, DEType.N, Packers));

            //// Date and time transmission
            DataElementSpecs.Add(FIELD.F_7, new DefaultField(FIELD.F_7, 10, DEType.N, Packers));

            //// Amount cardholder billing fee
            DataElementSpecs.Add(FIELD.F_8, new DefaultField(FIELD.F_8, 8, DEType.N, Packers));

            //// Conversion rate reconciliation
            DataElementSpecs.Add(FIELD.F_9, new DefaultField(FIELD.F_9, 8, DEType.N, Packers));

            //// Conversion rate cardholder billing
            DataElementSpecs.Add(FIELD.F_10, new DefaultField(FIELD.F_10, 8, DEType.N, Packers));

            //// Systems trace audit number
            DataElementSpecs.Add(FIELD.F_11, new DefaultField(FIELD.F_11, 6, DEType.N, Packers));

            //// Date and time local transaction
            DataElementSpecs.Add(FIELD.F_12, new DefaultField(FIELD.F_12, 6, DEType.N, Packers));

            //// Date effective
            DataElementSpecs.Add(FIELD.F_13, new DefaultField(FIELD.F_13, 4, DEType.N, Packers));

            //// Date expiration
            DataElementSpecs.Add(FIELD.F_14, new DefaultField(FIELD.F_14, 4, DEType.N, Packers));

            //// Date settlement
            DataElementSpecs.Add(FIELD.F_15, new DefaultField(FIELD.F_15, 4, DEType.N, Packers));

            //// Date conversion
            DataElementSpecs.Add(FIELD.F_16, new DefaultField(FIELD.F_16, 4, DEType.N, Packers));

            //// Date capture
            DataElementSpecs.Add(FIELD.F_17, new DefaultField(FIELD.F_17, 4, DEType.N, Packers));

            //// Message error indicator
            DataElementSpecs.Add(FIELD.F_18, new DefaultField(FIELD.F_18, 4, DEType.N, Packers));

            //// Country code acquiring institution
            DataElementSpecs.Add(FIELD.F_19, new DefaultField(FIELD.F_19, 3, DEType.N, Packers));

            //// Country code primary account number (PAN)
            DataElementSpecs.Add(FIELD.F_20, new DefaultField(FIELD.F_20, 3, DEType.N, Packers));

            //// Transaction life cycle identification data
            DataElementSpecs.Add(FIELD.F_21, new DefaultField(FIELD.F_21, 3, DEType.ANS, Packers));

            //// Point of service data code
            DataElementSpecs.Add(FIELD.F_22, new DefaultField(FIELD.F_22, 3, DEType.N, Packers));

            //// Card sequence number
            DataElementSpecs.Add(FIELD.F_23, new DefaultField(FIELD.F_23, 3, DEType.N, Packers));

            //// Function code
            DataElementSpecs.Add(FIELD.F_24, new DefaultField(FIELD.F_24, 3, DEType.N, Packers));

            //// Message reason code
            DataElementSpecs.Add(FIELD.F_25, new DefaultField(FIELD.F_25, 2, DEType.N, Packers));

            //// Merchant category code
            DataElementSpecs.Add(FIELD.F_26, new DefaultField(FIELD.F_26, 2, DEType.N, Packers));

            //// Point of service capability
            DataElementSpecs.Add(FIELD.F_27, new DefaultField(FIELD.F_27, 1, DEType.N, Packers));

            //// Date reconciliation
            DataElementSpecs.Add(FIELD.F_28, new DefaultField(FIELD.F_28, 8, DEType.N, Packers));

            //// Reconciliation indicator
            DataElementSpecs.Add(FIELD.F_29, new DefaultField(FIELD.F_29, 8, DEType.N, Packers));

            //// Amounts original
            DataElementSpecs.Add(FIELD.F_30, new DefaultField(FIELD.F_30, 8, DEType.N, Packers));

            //// Acquirer reference number
            DataElementSpecs.Add(FIELD.F_31, new DefaultField(FIELD.F_31, 8, DEType.N, Packers));

            //// Acquiring institution identification code
            DataElementSpecs.Add(FIELD.F_32, new DefaultField(FIELD.F_32, 11, DEType.LL, Packers));

            //// Forwarding institution identification code
            DataElementSpecs.Add(FIELD.F_33, new DefaultField(FIELD.F_33, 11, DEType.LL, Packers));

            //// Electronic commerce data
            DataElementSpecs.Add(FIELD.F_34, new DefaultField(FIELD.F_34, 28, DEType.LL, Packers));

            //// Track 2 data
            DataElementSpecs.Add(FIELD.F_35, new FIELD_35(37));

            //// Track 3 data
            DataElementSpecs.Add(FIELD.F_36, new DefaultField(FIELD.F_36, 104, DEType.LLL, Packers));

            //// Retrieval reference number
            DataElementSpecs.Add(FIELD.F_37, new DefaultField(FIELD.F_37, 12, DEType.AN, Packers));

            //// Approval code
            DataElementSpecs.Add(FIELD.F_38, new DefaultField(FIELD.F_38, 6, DEType.AN, Packers));

            //// Action code
            DataElementSpecs.Add(FIELD.F_39, new DefaultField(FIELD.F_39, 2, DEType.AN, Packers));

            //// Service code
            DataElementSpecs.Add(FIELD.F_40, new DefaultField(FIELD.F_40, 3, DEType.AN, Packers));

            //// Card acceptor terminal identification
            DataElementSpecs.Add(FIELD.F_41, new DefaultField(FIELD.F_41, 8, DEType.ANS, Packers));

            //// Card acceptor identification code
            DataElementSpecs.Add(FIELD.F_42, new DefaultField(FIELD.F_42, 15, DEType.ANS, Packers));

            //// Card acceptor name/location
            DataElementSpecs.Add(FIELD.F_43, new DefaultField(FIELD.F_43, 40, DEType.ANS, Packers));

            //// Additional response data
            DataElementSpecs.Add(FIELD.F_44, new DefaultField(FIELD.F_44, 25, DEType.LL, Packers));

            //// Track 1 data
            DataElementSpecs.Add(FIELD.F_45, new DefaultField(FIELD.F_45, 76, DEType.LL, Packers));

            //// Amounts fees
            DataElementSpecs.Add(FIELD.F_46, new DefaultField(FIELD.F_46, 999, DEType.LLL, Packers));

            //// Additional data national
            DataElementSpecs.Add(FIELD.F_47, new DefaultField(FIELD.F_47, 999, DEType.LLL, Packers));

            //// Additional data private
            DataElementSpecs.Add(FIELD.F_48, new DefaultField(FIELD.F_48, 999, DEType.LLL, Packers));

            //// Verification data
            DataElementSpecs.Add(FIELD.F_49, new DefaultField(FIELD.F_49, 3, DEType.A, Packers));

            //// Currency code, settlement
            DataElementSpecs.Add(FIELD.F_50, new DefaultField(FIELD.F_50, 3, DEType.AN, Packers));

            //// Currency code, cardholder billing
            DataElementSpecs.Add(FIELD.F_51, new DefaultField(FIELD.F_51, 3, DEType.A, Packers));

            //// Personal identification number (PIN) data
            DataElementSpecs.Add(FIELD.F_52, new DefaultField(FIELD.F_52, 16, DEType.B, Packers));

            //// Security related control information
            DataElementSpecs.Add(FIELD.F_53, new DefaultField(FIELD.F_53, 18, DEType.LL, Packers));

            //// Amounts additional
            DataElementSpecs.Add(FIELD.F_54, new DefaultField(FIELD.F_54, 250, DEType.LLL, Packers));

            //// Integrated circuit card (ICC) system related data
            DataElementSpecs.Add(FIELD.F_55, new DefaultField(FIELD.F_55, 999, DEType.LLL, Packers));

            //// Original data elements
            DataElementSpecs.Add(FIELD.F_56, new DefaultField(FIELD.F_56, 999, DEType.LLL, Packers));

            //// Authorization life cycle code
            DataElementSpecs.Add(FIELD.F_57, new DefaultField(FIELD.F_57, 999, DEType.LLL, Packers));

            //// Authorizing agent institution identification code
            DataElementSpecs.Add(FIELD.F_58, new DefaultField(FIELD.F_58, 999, DEType.LLL, Packers));

            //// Transport data
            DataElementSpecs.Add(FIELD.F_59, new DefaultField(FIELD.F_59, 999, DEType.LLL, Packers));

            //// Reserved for national use
            DataElementSpecs.Add(FIELD.F_60, new DefaultField(FIELD.F_60, 7, DEType.LL, Packers));

            //// Reserved for national use
            DataElementSpecs.Add(FIELD.F_61, new DefaultField(FIELD.F_61, 999, DEType.LLL, Packers));

            //// Reserved for private use
            DataElementSpecs.Add(FIELD.F_62, new DefaultField(FIELD.F_62, 999, DEType.LLL, Packers));

            //// Reserved for private use
            DataElementSpecs.Add(FIELD.F_63, new DefaultField(FIELD.F_63, 999, DEType.LLL, Packers));

            //// Message authentication code (MAC) val
            DataElementSpecs.Add(FIELD.F_64, new DefaultField(FIELD.F_64, 16, DEType.B, Packers));

            //// Bitmap tertiary
            DataElementSpecs.Add(FIELD.F_65, new DefaultField(FIELD.F_65, 16, DEType.B, Packers));

            //// Settlement code
            DataElementSpecs.Add(FIELD.F_66, new DefaultField(FIELD.F_66, 1, DEType.N, Packers));

            //// Extended payment data
            DataElementSpecs.Add(FIELD.F_67, new DefaultField(FIELD.F_67, 2, DEType.N, Packers));

            //// Receiving institution country code
            DataElementSpecs.Add(FIELD.F_68, new DefaultField(FIELD.F_68, 3, DEType.N, Packers));

            //// Settlement institution county code
            DataElementSpecs.Add(FIELD.F_69, new DefaultField(FIELD.F_69, 3, DEType.N, Packers));

            //// Network management Information code
            DataElementSpecs.Add(FIELD.F_70, new DefaultField(FIELD.F_70, 3, DEType.N, Packers));

            //// Message number
            DataElementSpecs.Add(FIELD.F_71, new DefaultField(FIELD.F_71, 4, DEType.N, Packers));

            //// Data, record
            DataElementSpecs.Add(FIELD.F_72, new DefaultField(FIELD.F_72, 999, DEType.LLL, Packers));

            //// Date, action
            DataElementSpecs.Add(FIELD.F_73, new DefaultField(FIELD.F_73, 6, DEType.N, Packers));

            //// Credits, number
            DataElementSpecs.Add(FIELD.F_74, new DefaultField(FIELD.F_74, 10, DEType.N, Packers));

            //// Credits, reversal number
            DataElementSpecs.Add(FIELD.F_75, new DefaultField(FIELD.F_75, 10, DEType.N, Packers));

            //// Debits, number
            DataElementSpecs.Add(FIELD.F_76, new DefaultField(FIELD.F_76, 10, DEType.N, Packers));

            //// Debits, reversal number
            DataElementSpecs.Add(FIELD.F_77, new DefaultField(FIELD.F_77, 10, DEType.N, Packers));

            //// Transfer, number
            DataElementSpecs.Add(FIELD.F_78, new DefaultField(FIELD.F_78, 10, DEType.N, Packers));

            //// Transfer, reversal number
            DataElementSpecs.Add(FIELD.F_79, new DefaultField(FIELD.F_79, 10, DEType.N, Packers));

            //// Inquiries, number
            DataElementSpecs.Add(FIELD.F_80, new DefaultField(FIELD.F_80, 10, DEType.N, Packers));

            //// Authorizations, number
            DataElementSpecs.Add(FIELD.F_81, new DefaultField(FIELD.F_81, 10, DEType.N, Packers));

            //// Credits, processing fee amount
            DataElementSpecs.Add(FIELD.F_82, new DefaultField(FIELD.F_82, 12, DEType.N, Packers));

            //// Credits, transaction fee amount
            DataElementSpecs.Add(FIELD.F_83, new DefaultField(FIELD.F_83, 12, DEType.N, Packers));

            //// Debits, processing fee amount
            DataElementSpecs.Add(FIELD.F_84, new DefaultField(FIELD.F_84, 12, DEType.N, Packers));

            //// Debits, transaction fee amount
            DataElementSpecs.Add(FIELD.F_85, new DefaultField(FIELD.F_85, 12, DEType.N, Packers));

            //// Credits, amount
            DataElementSpecs.Add(FIELD.F_86, new DefaultField(FIELD.F_86, 15, DEType.N, Packers));

            //// Credits, reversal amount
            DataElementSpecs.Add(FIELD.F_87, new DefaultField(FIELD.F_87, 15, DEType.N, Packers));

            //// Debits, amount
            DataElementSpecs.Add(FIELD.F_88, new DefaultField(FIELD.F_88, 15, DEType.N, Packers));

            //// Debits, reversal amount
            DataElementSpecs.Add(FIELD.F_89, new DefaultField(FIELD.F_89, 15, DEType.N, Packers));

            //// Original data elements
            DataElementSpecs.Add(FIELD.F_90, new DefaultField(FIELD.F_90, 42, DEType.N, Packers));

            //// File update code
            DataElementSpecs.Add(FIELD.F_91, new DefaultField(FIELD.F_91, 1, DEType.AN, Packers));

            //// File security code
            DataElementSpecs.Add(FIELD.F_92, new DefaultField(FIELD.F_92, 2, DEType.N, Packers));

            //// Response indicator
            DataElementSpecs.Add(FIELD.F_93, new DefaultField(FIELD.F_93, 5, DEType.N, Packers));

            //// Service indicator
            DataElementSpecs.Add(FIELD.F_94, new DefaultField(FIELD.F_94, 7, DEType.AN, Packers));

            //// Replacement amounts
            DataElementSpecs.Add(FIELD.F_95, new DefaultField(FIELD.F_95, 42, DEType.AN, Packers));

            //// Message security code
            DataElementSpecs.Add(FIELD.F_96, new DefaultField(FIELD.F_96, 8, DEType.AN, Packers));

            //// Amount, net settlement
            DataElementSpecs.Add(FIELD.F_97, new DefaultField(FIELD.F_97, 16, DEType.N, Packers));

            //// Payee
            DataElementSpecs.Add(FIELD.F_98, new DefaultField(FIELD.F_98, 25, DEType.ANS, Packers));

            //// Settlement institution identification code
            DataElementSpecs.Add(FIELD.F_99, new DefaultField(FIELD.F_99, 11, DEType.LL, Packers));

            //// Receiving institution identification code
            DataElementSpecs.Add(FIELD.F_100, new DefaultField(FIELD.F_100, 11, DEType.LL, Packers));

            //// File name
            DataElementSpecs.Add(FIELD.F_101, new DefaultField(FIELD.F_101, 17, DEType.ANS, Packers));

            //// Account identification 1
            DataElementSpecs.Add(FIELD.F_102, new DefaultField(FIELD.F_102, 28, DEType.LL, Packers));

            //// Account identification 2
            DataElementSpecs.Add(FIELD.F_103, new DefaultField(FIELD.F_103, 28, DEType.LL, Packers));

            //// Transaction description
            DataElementSpecs.Add(FIELD.F_104, new DefaultField(FIELD.F_104, 100, DEType.LLL, Packers));

            //// Reserved for ISO use
            DataElementSpecs.Add(FIELD.F_105, new DefaultField(FIELD.F_105, 999, DEType.LLL, Packers));

            //// Reserved for ISO use
            DataElementSpecs.Add(FIELD.F_106, new DefaultField(FIELD.F_106, 999, DEType.LLL, Packers));

            //// Reserved for ISO use
            DataElementSpecs.Add(FIELD.F_107, new DefaultField(FIELD.F_107, 999, DEType.LLL, Packers));

            //// Reserved for ISO use
            DataElementSpecs.Add(FIELD.F_108, new DefaultField(FIELD.F_108, 999, DEType.LLL, Packers));

            //// Reserved for ISO use
            DataElementSpecs.Add(FIELD.F_109, new DefaultField(FIELD.F_109, 999, DEType.LLL, Packers));

            //// Reserved for ISO use
            DataElementSpecs.Add(FIELD.F_110, new DefaultField(FIELD.F_110, 999, DEType.LLL, Packers));

            //// Reserved for private use
            DataElementSpecs.Add(FIELD.F_111, new DefaultField(FIELD.F_111, 999, DEType.LLL, Packers));

            //// Reserved for private use
            DataElementSpecs.Add(FIELD.F_112, new DefaultField(FIELD.F_112, 999, DEType.LLL, Packers));

            //// Reserved for private use
            DataElementSpecs.Add(FIELD.F_113, new DefaultField(FIELD.F_113, 11, DEType.LL, Packers));

            //// Reserved for national use
            DataElementSpecs.Add(FIELD.F_114, new DefaultField(FIELD.F_114, 999, DEType.LLL, Packers));

            //// Reserved for national use
            DataElementSpecs.Add(FIELD.F_115, new DefaultField(FIELD.F_115, 999, DEType.LLL, Packers));

            //// Reserved for national use
            DataElementSpecs.Add(FIELD.F_116, new DefaultField(FIELD.F_116, 999, DEType.LLL, Packers));

            //// Reserved for national use
            DataElementSpecs.Add(FIELD.F_117, new DefaultField(FIELD.F_117, 999, DEType.LLL, Packers));

            //// Reserved for national use
            DataElementSpecs.Add(FIELD.F_118, new DefaultField(FIELD.F_118, 999, DEType.LLL, Packers));

            //// Reserved for national use
            DataElementSpecs.Add(FIELD.F_119, new DefaultField(FIELD.F_119, 999, DEType.LLL, Packers));

            //// Reserved for private use
            DataElementSpecs.Add(FIELD.F_120, new DefaultField(FIELD.F_120, 999, DEType.LLL, Packers));

            //// Reserved for private use
            DataElementSpecs.Add(FIELD.F_121, new DefaultField(FIELD.F_121, 999, DEType.LLL, Packers));

            //// Reserved for private use
            DataElementSpecs.Add(FIELD.F_122, new DefaultField(FIELD.F_122, 999, DEType.LLL, Packers));

            //// Reserved for private use
            DataElementSpecs.Add(FIELD.F_123, new DefaultField(FIELD.F_123, 999, DEType.LLL, Packers));

            //// Reserved for private use
            DataElementSpecs.Add(FIELD.F_124, new DefaultField(FIELD.F_124, 999, DEType.LLL, Packers));

            //// Reserved for private use
            DataElementSpecs.Add(FIELD.F_125, new DefaultField(FIELD.F_125, 999, DEType.LLL, Packers));

            //// Reserved for private use
            DataElementSpecs.Add(FIELD.F_126, new DefaultField(FIELD.F_126, 999, DEType.LLL, Packers));

            //// Reserved for private use
            DataElementSpecs.Add(FIELD.F_127, new DefaultField(FIELD.F_127, 999, DEType.LLL, Packers));

            //// Message authentication code (MAC) val
            DataElementSpecs.Add(FIELD.F_128, new DefaultField(FIELD.F_128, 16, DEType.B, Packers));
        }

        public Iso8583V1987(byte[] message = null)
        {
            if (message != null)
            {
                this.Decode(message);
            }

            this.ConstructProcess();
        }

        public override ISO8583.DataElementSpec Spec(FIELD field)
        {
            DefaultField spec;
            DataElementSpecs.TryGetValue(field, out spec);
            return spec;
        }

        public override Iso8583 Prepare()
        {
            return this;
        }

        public override Bytes Encode()
        {
            this.Logger.Debug("Encoding...");
            this.Logger.Debug(string.Format("MTI         :{0}", this.Mti()));
            var rtn = new StringBuilder();
            this.Logger.Debug(() => $"Bitmap 1    :{HexStringConverter.FromByteArray(this.Bitmap())}");
            rtn.Append(this.Core().MessageTypeIndicator).Append(Bytes.From(this.Bitmap()).ToHexString());

            foreach (var pair in this.Core().OrderedFields().Where(pair => pair.Value != null))
            {
                var fieldData = ((DataElementSpec)this.Spec(pair.Key)).Pack(this);
                rtn.Append(fieldData);
                this.Logger.Debug(() => $"{$"FIELD[{pair.Key}]".PadRight(12, ' ')}:{fieldData}");
            }

            this.Logger.Debug(() => "Encoded.");
            var dataOut = Bytes.From(rtn.ToString().GetAsciiBytes());
            return dataOut;
        }

        public override ICodecable Decode(byte[] message)
        {
            try
            {
                this.Logger.Debug(() => string.Format("Decoding... Hex String: {0}", HexStringConverter.FromByteArray(message)));
                // Console.WriteLine("Decoding... Hex String: {0}", HexStringConverter.FromByteArray(message));
                var record = Bytes.From(message).ToString();
                this.Mti(record.IndexSubstring(0, 4));
                this.Logger.Debug(() => $"MTI         :{this.Mti()}");
                // Console.WriteLine("MTI         :{0}", this.Mti());
                var bitmap = HexStringConverter.ToByteArray(record.IndexSubstring(4, 20));
                this.Logger.Debug(() => $"Bitmap 1    :{HexStringConverter.FromByteArray(bitmap)}");
                // Console.WriteLine("Bitmap 1    :{0}", HexStringConverter.FromByteArray(bitmap));
                this.UnpackDataElement(bitmap, record.Substring(20), 0);
            }
            catch (FieldDecodeException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new DecodeFailException(e);
            }

            this.Logger.Debug(() => "Decoded.");
            return this;
        }

        public override string Identity()
        {
            var tmp = this.Core() as DefaultCore;
            return tmp == null ? this.Core().MessageTypeIndicator : tmp.MessageTypeIndicator + tmp.ProcessingCode;
        }

        protected override void ConstructProcess()
        {
        }

        protected void UnpackDataElement(byte[] bitmap, string record, int elementOffset)
        {
            if (record.Equals("\u0003")) return;   //remove dummy data(C.F)
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
                    var spec = (DataElementSpec)this.Spec(currentField);
                    record = spec.Unpack(this, record);
                    this.Logger.Debug(() => string.Format("{0}:{1}", string.Format("FIELD[{0}]", FIELDMethods.Val(currentField)).PadRight(12, ' '), this.Val(currentField)));
                }
                catch (FieldDecodeException)
                {
                    throw;
                }
                catch (Exception)
                {
                    throw new FieldDecodeException(currentField, string.Format("Field[{0}] decode failed.", currentField));
                }
            }

            if (!string.IsNullOrEmpty(this.Val(FIELD.F_1)) && !string.IsNullOrEmpty(record))
            {
                this.UnpackDataElement(HexStringConverter.ToByteArray(this.Val(FIELD.F_1)), record, 64);
            }
        }
    }
}
