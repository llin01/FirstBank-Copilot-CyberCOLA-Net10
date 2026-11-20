namespace Com.Cybersoft4u.TM.ISO8583.IPASS
{
    #region DEPENDENCY

    using System.Collections.Generic;
    using System.Text;

    using Com.Cybersoft4u.CyberUtils.Exception;
    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.V1987;

    using DataElementSpec = ISO8583.DataElementSpec;

    #endregion

    public class IPASS : Iso8583V1987<IPASSDefaultCore>
    {
        protected IPASSHeader header = new IPASSHeader();

        private const int HeaderLength = 8;

        private static readonly IDictionary<FIELD, DefaultField> DataElementSpecs =
            new Dictionary<FIELD, DefaultField>();

        private static readonly Packer Packers = new DefaultPacker();

        static IPASS()
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

            //// Date effective, Transmission date
            DataElementSpecs[FIELD.F_13] = new DefaultField(FIELD.F_13, 4, DEType.N, Packers);

            //// Date Expiration
            DataElementSpecs[FIELD.F_14] = new DefaultField(FIELD.F_14, 4, DEType.N, Packers);

            //// Track 3 data
            DataElementSpecs[FIELD.F_36] = new DefaultField(FIELD.F_36, 48, DEType.LLL, Packers);

            //// Action code
            DataElementSpecs[FIELD.F_39] = new DefaultField(FIELD.F_39, 2, DEType.AN, Packers);

            //// Card acceptor terminal identification
            DataElementSpecs[FIELD.F_41] = new DefaultField(FIELD.F_41, 8, DEType.ANS, Packers);

            //// Track I data
            DataElementSpecs[FIELD.F_45] = new DefaultField(FIELD.F_45, 16, DEType.LL, Packers);

            //// System ID
            DataElementSpecs[FIELD.F_63] = new DefaultField(FIELD.F_63, 2, DEType.ANS, Packers);

            //// Network management Information code
            DataElementSpecs[FIELD.F_70] = new DefaultField(FIELD.F_70, 3, DEType.N, Packers);

            //// Date, action
            DataElementSpecs[FIELD.F_73] = new DefaultField(FIELD.F_73, 6, DEType.AN, Packers);

            //// File update code
            DataElementSpecs[FIELD.F_91] = new DefaultField(FIELD.F_91, 1, DEType.AN, Packers);

        }

        public IPASS()
        {
        }

        public IPASS(byte[] message)
            : base(message)
        {
            if (message != null)
            {
                this.Decode(message);
            }
        }

        public virtual IPASSHeader Header()
        {
            return this.header;
        }

        public virtual ICodecable Header(IPASSHeader header)
        {
            this.header = header;
            return this;
        }

        public new IPASSDefaultCore Core()
        {
            return (IPASSDefaultCore)base.Core();
        }

        public new IPASS Core(Core core)
        {
            return (IPASS)base.Core(core);
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
            this.header = new IPASSHeader(record.IndexSubstring(0, HeaderLength));
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
                case FIELD.F_36:
                    this.Core().Track3Data = val;
                    break;
                case FIELD.F_39:
                    this.Core().ResponseCode = val;
                    break;
                case FIELD.F_41:
                    this.Core().TerminalId = val;
                    break;
                case FIELD.F_45:
                    this.Core().Track1Data = val;
                    break;
                case FIELD.F_63:
                    this.Core().SystemId = val;
                    break;
                case FIELD.F_70:
                    this.Core().NetworkManagementInformationCode = val;
                    break;
                case FIELD.F_73:
                    this.Core().ActionDate = val;
                    break;
                case FIELD.F_91:
                    this.Core().FileUpdateCode = val;
                    break;
            }
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
