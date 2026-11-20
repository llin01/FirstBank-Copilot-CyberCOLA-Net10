namespace Com.Cybersoft4u.TM.ISO8583.TWIN
{
    #region DEPENDENCY

    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.RegularExpressions;
    using Com.Cybersoft4u.CyberUtils.Exception;
    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.Category.CardType;
    using Com.Cybersoft4u.TM.ISO8583.Exception;
    using Com.Cybersoft4u.TM.ISO8583.V1987;

    using DataElementSpec = ISO8583.DataElementSpec;

    #endregion

    /// <summary>
    /// TWIN (Taiwan Interbank Network) ISO 8583 message implementation for domestic interbank transactions.
    /// </summary>
    public class TWINIso8583 : Iso8583V1987<DefaultCore>
    {
        protected TWINHeader header = new TWINHeader();

        private const int HeaderLength = 12;

        private static readonly IDictionary<FIELD, DefaultField> DataElementSpecs =
            new Dictionary<FIELD, DefaultField>();

        private static readonly Packer Packers = new DefaultPacker();

        static TWINIso8583()
        {
            //// Bit Map Extended
            DataElementSpecs[FIELD.F_1] = new DefaultField(FIELD.F_1, 16, DEType.AN, Packers);

            //// Primary account number (PAN)
            DataElementSpecs[FIELD.F_2] = new DefaultField(FIELD.F_2, 19, DEType.LL, Packers);

            //// Processing code
            DataElementSpecs[FIELD.F_3] = new DefaultField(FIELD.F_3, 6, DEType.N, Packers);

            //// Amount transaction
            DataElementSpecs[FIELD.F_4] = new DefaultField(FIELD.F_4, 12, DEType.N, Packers);

            //// Cardholder Billing Amount
            DataElementSpecs[FIELD.F_6] = new DefaultField(FIELD.F_6, 12, DEType.N, Packers);

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

            //// Date capture
            DataElementSpecs[FIELD.F_17] = new DefaultField(FIELD.F_17, 4, DEType.N, Packers);

            //// Message error indicator
            DataElementSpecs[FIELD.F_18] = new DefaultField(FIELD.F_18, 4, DEType.N, Packers);

            //// Acquiring Institution Country Code
            DataElementSpecs[FIELD.F_19] = new DefaultField(FIELD.F_19, 3, DEType.N, Packers);

            //// Point of service data code
            DataElementSpecs[FIELD.F_22] = new DefaultField(FIELD.F_22, 3, DEType.N, Packers);

            //// Card Sequence Number
            DataElementSpecs[FIELD.F_23] = new DefaultField(FIELD.F_23, 3, DEType.N, Packers);

            //// Network International Identifier
            DataElementSpecs[FIELD.F_24] = new DefaultField(FIELD.F_24, 3, DEType.N, Packers);

            //// Message Reason Code
            DataElementSpecs[FIELD.F_25] = new DefaultField(FIELD.F_25, 2, DEType.N, Packers);

            //// Transaction Fee Amount
            DataElementSpecs[FIELD.F_28] = new DefaultField(FIELD.F_28, 9, DEType.N, Packers);

            //// Acquiring institution identification code
            DataElementSpecs[FIELD.F_32] = new DefaultField(FIELD.F_32, 13, DEType.LL, Packers);

            //// Track 2 data
            DataElementSpecs[FIELD.F_35] = new FIELD_35(37);

            //// Retrieval reference number
            DataElementSpecs[FIELD.F_37] = new DefaultField(FIELD.F_37, 12, DEType.AN, Packers);

            //// Approval code
            DataElementSpecs[FIELD.F_38] = new DefaultField(FIELD.F_38, 6, DEType.AN, Packers);

            //// Action code
            DataElementSpecs[FIELD.F_39] = new DefaultField(FIELD.F_39, 2, DEType.AN, Packers);

            //// Card acceptor terminal identification
            DataElementSpecs[FIELD.F_41] = new DefaultField(FIELD.F_41, 8, DEType.ANS, Packers);

            //// Card acceptor identification code
            DataElementSpecs[FIELD.F_42] = new DefaultField(FIELD.F_42, 15, DEType.ANS, Packers);

            //// Card acceptor name/location
            DataElementSpecs[FIELD.F_43] = new DefaultField(FIELD.F_43, 40, DEType.ANS, Packers);

            //// Additional Response Data
            DataElementSpecs[FIELD.F_44] = new DefaultField(FIELD.F_44, 27, DEType.LL, Packers);

            //// Network Information
            DataElementSpecs[FIELD.F_48] = new DefaultField(FIELD.F_48, 999, DEType.LLL, Packers);

            //// Transaction Amount Currency Code
            DataElementSpecs[FIELD.F_49] = new DefaultField(FIELD.F_49, 3, DEType.N, Packers);

            //// Cardholder Billing Currency Code
            DataElementSpecs[FIELD.F_51] = new DefaultField(FIELD.F_51, 3, DEType.N, Packers);

            //// Personal identification number (PIN) data
            DataElementSpecs[FIELD.F_52] = new DefaultField(FIELD.F_52, 16, DEType.AN, Packers);

            //// Security related control information
            DataElementSpecs[FIELD.F_53] = new DefaultField(FIELD.F_53, 16, DEType.N, Packers);

            //// Chip Card Related Data
            DataElementSpecs[FIELD.F_55] = new DefaultField(FIELD.F_55, 999, DEType.LLL, Packers);

            //// POS Data
            DataElementSpecs[FIELD.F_61] = new DefaultField(FIELD.F_61, 22, DEType.LLL, Packers);

            //// POS Additional Data
            DataElementSpecs[FIELD.F_63] = new DefaultField(FIELD.F_63, 999, DEType.LLL, Packers);

            //// Network management Information code
            DataElementSpecs[FIELD.F_70] = new DefaultField(FIELD.F_70, 3, DEType.N, Packers);

            //// Original data elements
            DataElementSpecs[FIELD.F_90] = new DefaultField(FIELD.F_90, 42, DEType.N, Packers);

            //// File update code
            DataElementSpecs[FIELD.F_91] = new DefaultField(FIELD.F_91, 1, DEType.AN, Packers);

            //// Replacement amounts
            DataElementSpecs[FIELD.F_95] = new DefaultField(FIELD.F_95, 42, DEType.AN, Packers);

            //// File name
            DataElementSpecs[FIELD.F_101] = new DefaultField(FIELD.F_101, 21, DEType.LL, Packers);

            //// From Host Maintenance Data
            DataElementSpecs[FIELD.F_120] = new DefaultField(FIELD.F_120, 999, DEType.LLL, Packers);

            //// Key Related Data
            DataElementSpecs[FIELD.F_123] = new DefaultField(FIELD.F_123, 043, DEType.LLL, Packers);

        }

        public TWINIso8583()
        {
        }

        public TWINIso8583(byte[] message)
            : base(message)
        {
        }

        public virtual TWINHeader Header()
        {
            return this.header;
        }

        public virtual ICodecable Header(TWINHeader header)
        {
            this.header = header;
            return this;
        }

        public new DefaultCore Core()
        {
            return (DefaultCore)base.Core();
        }

        public new TWINIso8583 Core(Core core)
        {
            return (TWINIso8583)base.Core(core);
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
            this.header = new TWINHeader(record.IndexSubstring(0, HeaderLength));
            base.Decode(Bytes.From(record.Substring(HeaderLength)).Val());
            this.FillVariables();
            return this;
        }

        public virtual string Track2Encode()
        {
            var separator = "=";
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(this.Core().CardNo).Append(separator).Append(this.Core().ExpireDate);
            if (string.IsNullOrEmpty(this.Core().ServiceCode))
            {
                return stringBuilder.ToString();
            }

            stringBuilder.Append(this.Core().ServiceCode);
            stringBuilder.Append(this.Core().Pvki == null ? string.Empty : this.Core().Pvki)
                         .Append(this.Core().Pvv == null ? string.Empty : this.Core().Pvv)
                         .Append(this.Core().Cvv == null ? string.Empty : this.Core().Cvv);
            return stringBuilder.ToString();
        }

        public string CardAcceptorEncode()
        {
            var stringBuilder = new StringBuilder();

            if (!string.IsNullOrEmpty(this.Core().BankName))
            {
                stringBuilder.Append(this.Core().BankName.PadRight(22, ' '));
            }
            else
            {
                stringBuilder.Append(string.Empty.PadRight(22, ' '));
            }

            if (!string.IsNullOrEmpty(this.Core().City))
            {
                stringBuilder.Append(this.Core().City.PadRight(13, ' '));
            }
            else
            {
                stringBuilder.Append(string.Empty.PadRight(13, ' '));
            }

            if (!string.IsNullOrEmpty(this.Core().State))
            {
                stringBuilder.Append(this.Core().State.PadRight(3, ' '));
            }
            else
            {
                stringBuilder.Append(string.Empty.PadRight(3, ' '));
            }

            if (!string.IsNullOrEmpty(this.Core().Country))
            {
                stringBuilder.Append(this.Core().Country.PadRight(2, ' '));
            }
            else
            {
                stringBuilder.Append(string.Empty.PadRight(2, ' '));
            }

            return stringBuilder.ToString();
        }

        public string PinBlockEncode()
        {
            // TODO
            return null;
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
                    //this.Logger.Debug($"FILL FIELD {pair.Key} with VALUE {(string) pair.Value}");
                    this.MatchAndFill(pair.Key, (string)pair.Value);
                }
                catch (Exception e)
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
                case FIELD.F_6:
                    this.Core().CardholderBillingAmount = val;
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
                    this.Core().ExpireDate = val;
                    break;
                case FIELD.F_15:
                    this.Core().SettlementDate = val;
                    break;
                case FIELD.F_17:
                    this.Core().CaptureDate = val;
                    break;
                case FIELD.F_18:
                    this.Core().MerchantCategoryCode = val;
                    break;
                case FIELD.F_22:
                    this.Core().PointOfServiceEntryMode = val;
                    break;
                case FIELD.F_23:
                    this.Core().CardSequenceNumber = val;
                    break;
                case FIELD.F_24:
                    this.Core().FunctionCode = val;
                    break;
                case FIELD.F_25:
                    this.Core().MessageReasonCode = val;
                    break;
                case FIELD.F_28:
                    this.Core().TransactionFeeAmount = val;
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
                case FIELD.F_43:
                    this.Core().BankName = val.IndexSubstring(0, 22);
                    this.Core().City = val.IndexSubstring(22, 35);
                    this.Core().State = val.IndexSubstring(35, 38);
                    this.Core().Country = val.Substring(38);
                    break;
                case FIELD.F_44:
                    this.F44Decode(val);
                    break;
                case FIELD.F_48:
                    if (val.Length == 21)
                    {
                        this.Core().NetworkInfoSubAcquirerID = val.IndexSubstring(0, 6);
                        this.Core().NetworkInfoNetworkReferenceID = val.Substring(6);
                    }
                    break;
                case FIELD.F_49:
                    this.Core().TransactionCurrencyCode = val;
                    break;
                case FIELD.F_51:
                    this.Core().CardHolderBillingAmountCurrencyCode = val;
                    break;
                case FIELD.F_52:
                    this.Core().Pinblock = val;
                    break;
                case FIELD.F_53:
                    this.F53Decode(val);
                    break;
                case FIELD.F_55:
                    this.F55Decode(val);
                    break;
                case FIELD.F_61:
                    this.F61Decode(val);
                    break;
                case FIELD.F_63:
                    this.F63TLVDecode(val);
                    break;
                case FIELD.F_70:
                    this.Core().NetworkManagementInformationCode = val;
                    break;
                case FIELD.F_90:
                    this.F90OriginalDataElementsDecode(val);
                    break;
                case FIELD.F_91:
                    this.F91FileUpdateCodeDecode(val);
                    break;
                case FIELD.F_95:
                    this.Core().ReplacementAmount = val;
                    break;
                case FIELD.F_120:
                    this.F120Decode(val);
                    break;
                case FIELD.F_123:
                    this.F123CryptographicServiceMessageDecode(val);
                    break;
            }
        }

        public virtual void Track2Decode(string val)
        {
            if (string.IsNullOrEmpty(val))
            {
                return;
            }

            char[] separator = { '=', 'D', '~', '^' };
            var track2_split = val.Split(separator);

            this.Core().CardNo = track2_split[0];
            if (track2_split.Length == 1)
                val = string.Empty;
            else
                val = track2_split[1];

            var finalVal = val;
            if (this.Core().CardNo.Length == 16)
            {
                try
                {
                    this.Core().ExpireDate = finalVal.IndexSubstring(0, 4);
                    this.Core().ServiceCode = finalVal.GetIndexSubstring(4, 7);
                    this.Core().Pvki = finalVal.GetIndexSubstring(7, 8, "0");
                    this.Core().Pvv = finalVal.GetIndexSubstring(8, 12, "0000");
                    this.Core().Cvv = finalVal.IndexSubstring(12, 15);
                }
                catch (Exception)
                {
                    // ignored
                }

                if (this.Core().CardNo.StartsWith("4000") && this.Core().ServiceCode == "701")
                {
                    this.Core().CardNo = "9000" + this.Core().CardNo.GetIndexSubstring(4, 15) + "0";
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

        protected virtual void SetCardType()
        {
            if (this.Core().CardNo.Length < 16)
            {
            }
            else if (this.Core().CardNo.StartsWith("456789"))
            {
                this.Core().CardType = CardType.Get<TWIN>();
            }
        }

        public string F44Encode()
        {
            return (string)this.Core().GetOthersValByKey("F_44");
        }

        public void F44Decode(string val)
        {
            this.Core().SetOthersValByKey("F_44", val);
        }

        public string F53Encode()
        {
            return (string)this.Core().GetOthersValByKey("F_53");
        }

        public void F53Decode(string val)
        {
            this.Core().SetOthersValByKey("F_53", val);
        }

        public string F55Encode()
        {
            var tlv = this.Core().GetOthersValByKey("F_55") as TWINFIELD_55;

            if (!ObjectUtils.IsNull(tlv))
            {
                if (tlv != null) return tlv.Encode();
            }
            return null;
        }

        public void F55Decode(string val)
        {
            this.Core().SetOthersValByKey("F_55", new TWINFIELD_55().Decode(val));
        }
        public string F61Encode()
        {
            return (string)this.Core().GetOthersValByKey("F_61");
        }

        public void F61Decode(string val)
        {
            this.Core().SetOthersValByKey("F_61", val);
        }

        public string F63TLVEncode()
        {
            var tlv = this.Core().GetOthersValByKey("F_63") as TWINTLV;

            if (!ObjectUtils.IsNull(tlv))
            {
                if (tlv != null) return tlv.Encode();
            }
            return null;
        }

        public void F63TLVDecode(string val)
        {
            this.Core().SetOthersValByKey("F_63", new TWINTLV().Decode(val));
        }

        public virtual string F90OriginalDataElementsEncode()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(this.Core().OriginMti)
                .Append(this.Core().SystemTraceAuditNumber)
                .Append(this.Core().OriginTransactionTime)
                .Append(this.Core().OriginTransactionDate)
                .Append(string.Empty.PadRight(22, ' '));
            return stringBuilder.ToString();
        }

        public virtual void F90OriginalDataElementsDecode(string val)
        {
            this.Core().OriginMti = val.IndexSubstring(0, 4);
            this.Core().SystemTraceAuditNumber = val.IndexSubstring(4, 10);
            this.Core().OriginTransactionTime = val.IndexSubstring(10, 16);
            this.Core().OriginTransactionDate = val.IndexSubstring(16, 20);
        }

        public string F91FileUpdateCodeEncode()
        {
            return (string)this.Core().GetOthersValByKey("F_91");
        }

        public void F91FileUpdateCodeDecode(string val)
        {
            this.Core().SetOthersValByKey("F_91", val);
        }

        public string F101FileNameEncode()
        {
            return (string)this.Core().GetOthersValByKey("F_101");
        }

        public void F101FileNameDecode(string val)
        {
            this.Core().SetOthersValByKey("F_101", val);
        }

        public string F120Encode()
        {
            return (string)this.Core().GetOthersValByKey("F_120");
        }

        public void F120Decode(string val)
        {
            this.Core().SetOthersValByKey("F_120", val);
        }

        public void F122Decode(string val)
        {
            this.Core().SetOthersValByKey("F_122", val);
        }

        public string F122Encode()
        {
            return (string)this.Core().GetOthersValByKey("F_122");
        }

        public void F123CryptographicServiceMessageDecode(string val)
        {
            this.Core().SetOthersValByKey("F_123", val);
        }

        public string F123CryptographicServiceMessageEncode()
        {
            return (string)this.Core().GetOthersValByKey("F_123");
        }
    }
}
