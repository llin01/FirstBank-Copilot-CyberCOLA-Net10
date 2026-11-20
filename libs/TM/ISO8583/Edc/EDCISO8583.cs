namespace Com.Cybersoft4u.TM.ISO8583.Edc
{
    #region DEPENDENCY

    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.RegularExpressions;
    using Com.Cybersoft4u.CyberUtils.Exception;
    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.Category.CardType;
    using Com.Cybersoft4u.TM.ISO8583.Edc.Field;
    using Com.Cybersoft4u.TM.ISO8583.Exception;
    using Com.Cybersoft4u.TM.ISO8583.V1987;

    using DataElementSpec = ISO8583.DataElementSpec;

    #endregion

    public class EdcIso8583 : Iso8583V1987<DefaultCore>
    {
        private static readonly IDictionary<FIELD, DefaultField> DataElementSpecs =
            new Dictionary<FIELD, DefaultField>();

        private static readonly Packer Packers = new EdcPacker();

        private static readonly int HeaderLength = 10;

        static EdcIso8583()
        {
            //// Precessing code
            DataElementSpecs[FIELD.F_3] = new DefaultField(FIELD.F_3, 6, DEType.N, Packers);

            //// Amount transaction
            DataElementSpecs[FIELD.F_4] = new DefaultField(FIELD.F_4, 12, DEType.N, Packers);

            //// Systems trace audit number
            DataElementSpecs[FIELD.F_11] = new DefaultField(FIELD.F_11, 6, DEType.N, Packers);

            //// Date and time local transaction
            DataElementSpecs[FIELD.F_12] = new DefaultField(FIELD.F_12, 6, DEType.N, Packers);

            //// Date effective
            DataElementSpecs[FIELD.F_13] = new DefaultField(FIELD.F_13, 4, DEType.N, Packers);

            //// Point of service data code
            DataElementSpecs[FIELD.F_22] = new EdcFIELD_22(4);

            //// Network International Identifier
            DataElementSpecs[FIELD.F_24] = new DefaultField(FIELD.F_24, 4, DEType.N, Packers);

            //// Message reason code
            DataElementSpecs[FIELD.F_25] = new DefaultField(FIELD.F_25, 2, DEType.N, Packers);

            //// Acquiring institution identification code
            DataElementSpecs[FIELD.F_32] = new EdcFIELD_32(22);

            //// Track 2 data
            DataElementSpecs[FIELD.F_35] = new EdcFIELD_35(38);

            //// Retrieval reference number
            DataElementSpecs[FIELD.F_37] = new DefaultField(FIELD.F_37, 24, DEType.AN, Packers);

            //// Approval code
            DataElementSpecs[FIELD.F_38] = new DefaultField(FIELD.F_38, 12, DEType.AN, Packers);

            //// Action code
            DataElementSpecs[FIELD.F_39] = new DefaultField(FIELD.F_39, 4, DEType.AN, Packers);

            //// Card acceptor terminal identification
            DataElementSpecs[FIELD.F_41] = new DefaultField(FIELD.F_41, 16, DEType.AN, Packers);

            //// Card acceptor identification code
            DataElementSpecs[FIELD.F_42] = new DefaultField(FIELD.F_42, 30, DEType.AN, Packers);

            //// Track 1 data
            DataElementSpecs[FIELD.F_45] = new DefaultField(FIELD.F_45, 152, DEType.LL, Packers);

            //// Additional data private
            DataElementSpecs[FIELD.F_48] = new DefaultField(FIELD.F_48, 1998, DEType.LLL, Packers);

            //// Personal identification number (PIN) data
            DataElementSpecs[FIELD.F_52] = new DefaultField(FIELD.F_52, 16, DEType.B, Packers);

            //// Amounts additional
            DataElementSpecs[FIELD.F_54] = new DefaultField(FIELD.F_54, 24, DEType.LLL, Packers);

            //// Integrated circuit card (ICC) system related data
            DataElementSpecs[FIELD.F_55] = new EdcFIELD_55(1998);

            //// Authorisation life cycle code
            DataElementSpecs[FIELD.F_57] = new DefaultField(FIELD.F_57, 1998, DEType.LLL, Packers);

            //// Transport data
            DataElementSpecs[FIELD.F_59] = new EdcFIELD_59(1998);

            //// Reserved for national use
            DataElementSpecs[FIELD.F_60] = new DefaultField(FIELD.F_60, 1998, DEType.LLL, Packers);

            //// Reserved for private use
            DataElementSpecs[FIELD.F_62] = new DefaultField(FIELD.F_62, 1998, DEType.LLL, Packers);

            //// Reserved for private use
            DataElementSpecs[FIELD.F_63] = new DefaultField(FIELD.F_63, 1998, DEType.LLL, Packers);

            //// Message authentication code (MAC) val
            DataElementSpecs[FIELD.F_64] = new DefaultField(FIELD.F_64, 16, DEType.B, Packers);
        }

        public EdcIso8583()
        {
            this.Header = new EdcHeader();
        }

        public EdcIso8583(byte[] message)
            : base(message)
        {
            this.Header = new EdcHeader();
        }

        public EdcHeader Header { get; protected set; }

        public new DefaultCore Core()
        {
            return (DefaultCore)base.Core();
        }

        public new EdcIso8583 Core(Core core)
        {
            return (EdcIso8583)base.Core(core);
        }

        public override DataElementSpec Spec(FIELD field)
        {
            DefaultField spec;
            DataElementSpecs.TryGetValue(field, out spec);
            return spec;
        }

        public override Bytes Encode()
        {
            var data = Bytes.From(this.Header.Encode()).Append(base.Encode()).ToString();
            return Bytes.FromHexString(data);
        }

        public override ICodecable Decode(byte[] message)
        {
            var record = HexStringConverter.FromByteArray(message);
            this.Header = new EdcHeader(record.IndexSubstring(0, HeaderLength));
            base.Decode(Bytes.From(record.Substring(HeaderLength)).Val());
            this.FillVariables();
            return this;
        }

        protected string Track2Encode()
        {
            var separator = "D";
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(this.Core().CardNo).Append(separator).Append(this.Core().ExpireDate);
            if (this.Core().CardType.Equals(CardType.Default))
            {
                if (this.Core().AeZero == null)
                {
                    // AE ANSI X4.16
                    stringBuilder.Append(this.Core().AeEffectiveDate).Append(this.Core().AeDiscretionaryData);
                }
                else
                {
                    // AE ISO7813
                    stringBuilder.Append(this.Core().AeInterchangeDesignator)
                        .Append(this.Core().ServiceCode)
                        .Append(this.Core().AeEffectiveDate)
                        .Append(this.Core().AeDiscretionaryData)
                        .Append(this.Core().AeZero)
                        .Append(this.Core().AeLanguage);
                }
            }
            else
            {
                stringBuilder.Append(this.Core().ServiceCode);
                if (this.Core().CardNo.StartsWith("9000") && this.Core().ServiceCode.Equals("701"))
                {
                    // UCARD
                    stringBuilder.Append(this.Core().Pvki).Append(this.Core().Pvv).Append(this.Core().Cvv);
                }
                else
                {
                    // VISA, MASTER, JCB
                    stringBuilder.Append(this.Core().Pvki ?? string.Empty)
                        .Append(this.Core().Pvv ?? string.Empty)
                        .Append(this.Core().Cvv ?? string.Empty);
                }
            }

            return stringBuilder.ToString();
        }

        protected string F48Encode()
        {
            return (string)this.Core().GetOthersValByKey("F_48");
        }

        protected void F48Decode(string val)
        {
            this.Core().SetOthersValByKey("F_48", val);
        }

        protected string F55EmvChipEncode()
        {
            return (string)this.Core().GetOthersValByKey("F_55");
        }

        protected void F55EmvChipDecode(string val)
        {
            this.Core().SetOthersValByKey("F_55", val);
        }

        protected string F57TsamEncode()
        {
            return (string)this.Core().GetOthersValByKey("F_57");
        }

        protected void F57TsamDecode(string val)
        {
            this.Core().SetOthersValByKey("F_57", val);
        }

        protected string F59TableEncode()
        {
            return (string)this.Core().GetOthersValByKey("F_59");
        }

        protected void F59TableDecode(string val)
        {
            this.Core().SetOthersValByKey("F_59", val);
        }

        private void FillVariables()
        {
            foreach (var pair in this.Core().Fields())
            {
                try
                {
                    this.Logger.Debug(string.Format("FILL FIELD {0} with VALUE {1}", pair.Key, pair.Value));
                    this.MatchAndFill(pair.Key, (string)pair.Value);
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
                case FIELD.F_3:
                    this.Core().ProcessingCode = val;
                    break;
                case FIELD.F_4:
                    this.Core().TransactionAmount = val;
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
                case FIELD.F_22:
                    this.Core().PointOfServiceEntryMode = val;
                    break;
                case FIELD.F_24:
                    this.Core().NetworkInternationalIdentifier = val;
                    break;
                case FIELD.F_25:
                    this.Core().PointOfServiceConditionMode = val;
                    break;
                case FIELD.F_32:
                    this.Core().AcquiringInstitutionIdentificationCode = val;
                    break;
                case FIELD.F_35:
                    this.Track2Decode(val);
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
                case FIELD.F_48:
                    this.F48Decode(val);
                    break;
                case FIELD.F_52:
                    this.Core().Pinblock = val;
                    break;
                case FIELD.F_54:
                    this.Core().AdditionalAmount = val;
                    break;
                case FIELD.F_55:
                    this.F55EmvChipDecode(val);
                    break;
                case FIELD.F_57:
                    this.F57TsamDecode(val);
                    break;
                case FIELD.F_59:
                    this.F59TableDecode(val);
                    break;
                case FIELD.F_60:
                    this.Core().SetOthersValByKey("F_60", val);
                    break;
                case FIELD.F_62:
                    this.Core().SetOthersValByKey("F_62", val);
                    break;
                case FIELD.F_63:
                    this.Core().SetOthersValByKey("F_63", val);
                    break;
                case FIELD.F_64:
                    this.Core().MessageAuthenticationCode = val;
                    break;
            }
        }

        private void Track2Decode(string val)
        {
            if (string.IsNullOrEmpty(val))
            {
                return;
            }

            char[] separator = { 'D' };
            var track2Split = val.Split(separator);

            this.Core().CardNo = track2Split[0];
            if (track2Split.Length == 1)
                val = string.Empty;
            else
                val = track2Split[1];

            var finalVal = val;
            if (this.Core().CardNo.Length == 16)
            {
                try
                {
                    this.Core().ExpireDate = finalVal.IndexSubstring(0, 4);
                    this.Core().ServiceCode = finalVal.GetIndexSubstring(4, 7);
                    this.Core().Pvki = finalVal.GetIndexSubstring(7, 8);
                    this.Core().Pvv = finalVal.GetIndexSubstring(8, 12);
                    this.Core().Cvv = finalVal.IndexSubstring(12, 15);
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            if (this.Core().CardNo.Length == 15)
            {
                if (val.Length == 13)
                {
                    // AE ANSI X4.16
                    try
                    {
                        this.Core().ExpireDate = finalVal.IndexSubstring(0, 4);
                        this.Core().AeEffectiveDate = finalVal.IndexSubstring(4, 8);
                        this.Core().AeDiscretionaryData = finalVal.IndexSubstring(8, 13);
                    }
                    catch (Exception e)
                    {
                        throw new DecodeFailException(e);
                    }
                }
                else
                {
                    // AE ISO7813
                    try
                    {
                        this.Core().ExpireDate = finalVal.IndexSubstring(0, 4);
                        this.Core().AeInterchangeDesignator = finalVal.IndexSubstring(4, 5);
                        this.Core().ServiceCode = finalVal.IndexSubstring(5, 7);
                        this.Core().AeEffectiveDate = finalVal.IndexSubstring(7, 11);
                        this.Core().AeDiscretionaryData = finalVal.IndexSubstring(11, 16);
                        this.Core().AeZero = finalVal.IndexSubstring(16, 19);
                        this.Core().AeLanguage = finalVal.IndexSubstring(19, 21);
                    }
                    catch (Exception e)
                    {
                        throw new DecodeFailException(e);
                    }
                }
            }
        }

        private void SetCardType()
        {
            if (this.Core().CardNo.StartsWith("1800"))
            {
                this.Core().CardType = CardType.Get<DefaultCardType>();
            }
            else if (this.Core().CardNo.Length < 16)
            {
            }
            else if (Regex.Match(this.Core().CardNo, @"^(?:2131|1800|35\d{3})\d{11}$").Success)
            {
                this.Core().CardType = CardType.Get<JCB>();
            }
            else if (Regex.Match(this.Core().CardNo, @"^4[0-9]{12}(?:[0-9]{3})?$").Success)
            {
                this.Core().CardType = CardType.Get<VISA>();
            }
            else if (Regex.Match(this.Core().CardNo, @"^(?:5[1-5][0-9]{2}|222[1-9]|22[3-9][0-9]|2[3-6][0-9]{2}|27[01][0-9]|2720)[0-9]{12}$").Success)
            {
                this.Core().CardType = CardType.Get<MasterCard>();
            }
            else if (this.Core().CardNo.StartsWith("4000") && this.Core().ServiceCode == "701")
            {
                this.Core().CardType = CardType.Get<UCard>();
            }
        }

        private void TokenDecode(string val)
        {
            //// TODO
        }
    }
}
