namespace Com.Cybersoft4u.TM.ISO8583.SVCS
{
    #region DEPENDENCY

    using System.Collections.Generic;
    using System.Text;

    using Com.Cybersoft4u.CyberUtils.Exception;
    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.V1987;

    using DataElementSpec = ISO8583.DataElementSpec;

    #endregion

    public class SVCS : Iso8583V1987<SVCSDefaultCore>
    {
        protected SVCSHeader header = new SVCSHeader();

        private const int HeaderLength = 8;

        private static readonly IDictionary<FIELD, DefaultField> DataElementSpecs =
            new Dictionary<FIELD, DefaultField>();

        private static readonly Packer Packers = new DefaultPacker();

        static SVCS()
        {
            //// Bit Map Extended
            DataElementSpecs[FIELD.F_1] = new DefaultField(FIELD.F_1, 16, DEType.AN, Packers);

            //// Primary account number (PAN)
            DataElementSpecs[FIELD.F_2] = new DefaultField(FIELD.F_2, 19, DEType.LL, Packers);

            //// Processing code
            DataElementSpecs[FIELD.F_3] = new DefaultField(FIELD.F_3, 6, DEType.N, Packers);

            //// Amount transaction
            DataElementSpecs[FIELD.F_4] = new DefaultField(FIELD.F_4, 12, DEType.N, Packers);

            //// Date and time transmission
            DataElementSpecs[FIELD.F_7] = new DefaultField(FIELD.F_7, 10, DEType.N, Packers);

            //// Systems trace audit number
            DataElementSpecs[FIELD.F_11] = new DefaultField(FIELD.F_11, 6, DEType.N, Packers);

            //// Date and time local transaction
            DataElementSpecs[FIELD.F_12] = new DefaultField(FIELD.F_12, 6, DEType.N, Packers);

            //// Date effective
            DataElementSpecs[FIELD.F_13] = new DefaultField(FIELD.F_13, 4, DEType.N, Packers);

            //// Date Expiration
            DataElementSpecs[FIELD.F_14] = new DefaultField(FIELD.F_14, 4, DEType.N, Packers);

            //// Date settlement
            DataElementSpecs[FIELD.F_15] = new DefaultField(FIELD.F_15, 4, DEType.N, Packers);

            //// Acquiring institution identification code
            DataElementSpecs[FIELD.F_32] = new DefaultField(FIELD.F_32, 13, DEType.LL, Packers);

            //// Retrieval reference number
            DataElementSpecs[FIELD.F_37] = new DefaultField(FIELD.F_37, 12, DEType.AN, Packers);

            //// Response code
            DataElementSpecs[FIELD.F_39] = new DefaultField(FIELD.F_39, 2, DEType.AN, Packers);

            //// Card acceptor terminal identification
            DataElementSpecs[FIELD.F_41] = new DefaultField(FIELD.F_41, 8, DEType.ANS, Packers);

            //// Card acceptor identification code
            DataElementSpecs[FIELD.F_42] = new DefaultField(FIELD.F_42, 15, DEType.ANS, Packers);

            //// Reserved ISO
            DataElementSpecs[FIELD.F_58] = new DefaultField(FIELD.F_58, 999, DEType.LLL, Packers);

            //// Network management Information code
            DataElementSpecs[FIELD.F_70] = new DefaultField(FIELD.F_70, 3, DEType.N, Packers);

            //// Date, action
            DataElementSpecs[FIELD.F_73] = new DefaultField(FIELD.F_73, 6, DEType.AN, Packers);

            //// Original data elements
            DataElementSpecs[FIELD.F_90] = new DefaultField(FIELD.F_90, 42, DEType.N, Packers);

            //// File update code
            DataElementSpecs[FIELD.F_91] = new DefaultField(FIELD.F_91, 1, DEType.AN, Packers);

            //// File Security Code
            DataElementSpecs[FIELD.F_92] = new DefaultField(FIELD.F_92, 2, DEType.AN, Packers);

        }

        public SVCS()
        {
        }

        public SVCS(byte[] message)
            : base(message)
        {
            if (message != null)
            {
                this.Decode(message);
            }
        }

        public virtual SVCSHeader Header()
        {
            return this.header;
        }

        public virtual ICodecable Header(SVCSHeader header)
        {
            this.header = header;
            return this;
        }

        public new SVCSDefaultCore Core()
        {
            return (SVCSDefaultCore)base.Core();
        }

        public new SVCS Core(Core core)
        {
            return (SVCS)base.Core(core);
        }

        public override DataElementSpec Spec(FIELD field)
        {
            DefaultField spec;
            DataElementSpecs.TryGetValue(field, out spec);
            return spec;
        }

        public override Bytes Encode()
        {
            return Bytes.From(this.header.Encode().GetAsciiBytes()).Append(base.Encode());
        }

        public override ICodecable Decode(byte[] message)
        {
            var record = Bytes.From(message).ToString();
            this.header = new SVCSHeader(record.IndexSubstring(0, HeaderLength));
            this.Logger.Debug(() => $"Header    :{this.header.Encode()}");
            base.Decode(Bytes.From(record.Substring(HeaderLength)).Val());
            this.FillVariables();
            return this;
        }

        public void FillVariables()
        {
            foreach (var pair in this.Core().Fields())
            {
                try
                {
                    //// TODO
                    // Log.d(() -> String.format("FILL FIELD %s with VALUE %s", field, val));
                    // Console.WriteLine("FILL FIELD {0} with VALUE {1}", pair.Key, (string)pair.Value);
                    this.MatchAndFill(pair.Key, (string)pair.Value);
                }
                catch (System.Exception e)
                {
                    throw new ConvertException(e);
                }
            }
        }

        protected virtual void MatchAndFill(FIELD field, string val)
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
                    this.Core().ProcessingCode = val;
                    break;
                case FIELD.F_4:
                    this.Core().TransactionAmount = val;
                    break;
                case FIELD.F_7:
                    this.Core().TransmissionDatetime = val;
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
                    this.Core().ExpirationDate = val;
                    break;
                case FIELD.F_15:
                    this.Core().SettlementDate = val;
                    break;
                case FIELD.F_32:
                    this.Core().AcquiringInstitutionIdentificationCode = val;
                    break;
                case FIELD.F_37:
                    this.Core().RetrievalReferenceNumber = val;
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
                case FIELD.F_58:
                    this.F58TokenDecode(val);
                    this.Core().F58SVCSChipRelatedData = val;
                    break;
                case FIELD.F_70:
                    this.Core().NetworkManagementInformationCode = val;
                    break;
                case FIELD.F_73:
                    this.Core().ActionDate = val;
                    break;
                case FIELD.F_90:
                    this.F90OriginalDataElementsDecode(val);
                    break;
                case FIELD.F_91:
                    this.Core().FileUpdateCode = val;
                    break;
                case FIELD.F_92:
                    this.Core().FileSecurityCode = val;
                    break;
            }
        }

        public SVCSChip F58TokenEncode()
        {
            var svcsChip = this.Core().GetOthersValByKey("F_58") as SVCSChip;

            if (!ObjectUtils.IsNull(svcsChip))
            {
                return svcsChip;
            }
            else
            {
                return null;
            }
        }

        public void F58TokenDecode(string val)
        {
            this.Core().SetOthersValByKey("F_58", new SVCSChip(Encoding.ASCII.GetBytes("0000" + val)));
        }


        public virtual string F90OriginalDataElementsEncode()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(this.Core().OriginMti)
                         .Append(this.Core().OriginTransactionDateTime)
                         .Append(this.Core().OriginSTAN)
                         .Append(this.Core().OriginAcquiringID)
                         .Append(this.Core().OriginReferenceNumber)
                         .Append(new string(' ', 2));
            return stringBuilder.ToString();
        }

        public virtual void F90OriginalDataElementsDecode(string val)
        {
            this.Core().OriginMti = val.IndexSubstring(0, 4);
            this.Core().OriginTransactionDateTime = val.IndexSubstring(4, 14);
            this.Core().OriginSTAN = val.IndexSubstring(14, 20);
            this.Core().OriginAcquiringID = val.IndexSubstring(20, 28);
            this.Core().OriginReferenceNumber = val.IndexSubstring(28, 40);
        }

        public string F91FileUpdateCodeEncode()
        {
            return (string)this.Core().GetOthersValByKey("F_91");
        }

        public void F91FileUpdateCodeDecode(string val)
        {
            this.Core().SetOthersValByKey("F_91", val);
        }
    }
}
