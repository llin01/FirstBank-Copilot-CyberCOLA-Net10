namespace Com.Cybersoft4u.TM.ISO8583.Nccc
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
    /// NCCC (National Credit Card Center) ISO 8583 message implementation for payment authorization and processing.
    /// </summary>
    public class NcccIso8583 : Iso8583V1987<DefaultCore>
    {
        protected NcccHeader header = new NcccHeader();

        private const int HeaderLength = 12;

        private static readonly IDictionary<FIELD, DefaultField> DataElementSpecs =
            new Dictionary<FIELD, DefaultField>();

        private static readonly Packer Packers = new DefaultPacker();

        static NcccIso8583()
        {
            //// Bit Map Extended
            DataElementSpecs[FIELD.F_1] = new DefaultField(FIELD.F_1, 16, DEType.AN, Packers);

            //// Primary account number (PAN)
            DataElementSpecs[FIELD.F_2] = new DefaultField(FIELD.F_2, 19, DEType.LL, Packers);

            //// Processing code
            DataElementSpecs[FIELD.F_3] = new DefaultField(FIELD.F_3, 6, DEType.N, Packers);

            //// Amount transaction
            DataElementSpecs[FIELD.F_4] = new DefaultField(FIELD.F_4, 12, DEType.N, Packers);

            //// Amount settlement
            DataElementSpecs[FIELD.F_5] = new DefaultField(FIELD.F_5, 12, DEType.N, Packers);

            //// Cardholder Billing Amount
            DataElementSpecs[FIELD.F_6] = new DefaultField(FIELD.F_6, 12, DEType.N, Packers);

            //// Date and time transmission
            DataElementSpecs[FIELD.F_7] = new DefaultField(FIELD.F_7, 10, DEType.N, Packers);

            //// Cardholder Billing Fee Amount
            DataElementSpecs[FIELD.F_8] = new DefaultField(FIELD.F_8, 8, DEType.N, Packers);

            //// Settlement Conversion Rate
            DataElementSpecs[FIELD.F_9] = new DefaultField(FIELD.F_9, 8, DEType.N, Packers);

            //// Billing Conversion Rate
            DataElementSpecs[FIELD.F_10] = new DefaultField(FIELD.F_10, 8, DEType.N, Packers);

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

            //// Date Conversion
            DataElementSpecs[FIELD.F_16] = new DefaultField(FIELD.F_16, 4, DEType.N, Packers);

            //// Date capture
            DataElementSpecs[FIELD.F_17] = new DefaultField(FIELD.F_17, 4, DEType.N, Packers);

            //// Message error indicator
            DataElementSpecs[FIELD.F_18] = new DefaultField(FIELD.F_18, 4, DEType.N, Packers);

            //// Acquiring Institution Country Code
            DataElementSpecs[FIELD.F_19] = new DefaultField(FIELD.F_19, 3, DEType.N, Packers);

            //// Country Code Primary Account Number Extended
            DataElementSpecs[FIELD.F_20] = new DefaultField(FIELD.F_20, 3, DEType.N, Packers);

            //// Forwarding Institution Country Code
            DataElementSpecs[FIELD.F_21] = new DefaultField(FIELD.F_21, 3, DEType.N, Packers);

            //// Point of service data code
            DataElementSpecs[FIELD.F_22] = new DefaultField(FIELD.F_22, 3, DEType.N, Packers);

            //// Card Sequence Number
            DataElementSpecs[FIELD.F_23] = new DefaultField(FIELD.F_23, 3, DEType.N, Packers);

            //// Network International Identifier
            DataElementSpecs[FIELD.F_24] = new DefaultField(FIELD.F_24, 3, DEType.N, Packers);

            //// Point Of Service Condition Code
            DataElementSpecs[FIELD.F_25] = new DefaultField(FIELD.F_25, 2, DEType.N, Packers);

            //// Point Of Service Condition Code
            DataElementSpecs[FIELD.F_26] = new DefaultField(FIELD.F_26, 2, DEType.N, Packers);

            //// Authorization Identification Response Length
            DataElementSpecs[FIELD.F_27] = new DefaultField(FIELD.F_27, 1, DEType.N, Packers);

            //// Transaction Fee Amount
            DataElementSpecs[FIELD.F_28] = new DefaultField(FIELD.F_28, 9, DEType.N, Packers);

            //// Settlement Fee Amount
            DataElementSpecs[FIELD.F_29] = new DefaultField(FIELD.F_29, 8, DEType.N, Packers);

            //// Transaction Processing Fee Amount
            DataElementSpecs[FIELD.F_30] = new DefaultField(FIELD.F_30, 8, DEType.N, Packers);

            //// Settlement Processing Fee Amount
            DataElementSpecs[FIELD.F_31] = new DefaultField(FIELD.F_31, 8, DEType.N, Packers);

            //// Acquiring institution identification code
            DataElementSpecs[FIELD.F_32] = new DefaultField(FIELD.F_32, 13, DEType.LL, Packers);

            //// Forwarding Institution Identification Code
            DataElementSpecs[FIELD.F_33] = new DefaultField(FIELD.F_33, 15, DEType.LL, Packers);

            //// Extended Primary Account Number
            DataElementSpecs[FIELD.F_34] = new DefaultField(FIELD.F_34, 28, DEType.AN, Packers);

            //// Track 2 data
            DataElementSpecs[FIELD.F_35] = new FIELD_35(37);

            //// Track 3 Data
            DataElementSpecs[FIELD.F_36] = new DefaultField(FIELD.F_36, 107, DEType.LLL, Packers);

            //// Retrieval reference number
            DataElementSpecs[FIELD.F_37] = new DefaultField(FIELD.F_37, 12, DEType.AN, Packers);

            //// Approval code
            DataElementSpecs[FIELD.F_38] = new DefaultField(FIELD.F_38, 6, DEType.AN, Packers);

            //// Response code
            DataElementSpecs[FIELD.F_39] = new DefaultField(FIELD.F_39, 2, DEType.AN, Packers);

            //// Service Restriction Code
            DataElementSpecs[FIELD.F_40] = new DefaultField(FIELD.F_40, 3, DEType.AN, Packers);

            //// Card acceptor terminal identification
            DataElementSpecs[FIELD.F_41] = new DefaultField(FIELD.F_41, 16, DEType.ANS, Packers);

            //// Card acceptor identification code
            DataElementSpecs[FIELD.F_42] = new DefaultField(FIELD.F_42, 15, DEType.ANS, Packers);

            //// Card acceptor name/location
            DataElementSpecs[FIELD.F_43] = new DefaultField(FIELD.F_43, 40, DEType.ANS, Packers);

            //// BASE24-ATM Additional Response Data
            DataElementSpecs[FIELD.F_44] = new DefaultField(FIELD.F_44, 27, DEType.LL, Packers);

            //// Track 1 Data
            DataElementSpecs[FIELD.F_45] = new DefaultField(FIELD.F_45, 78, DEType.LL, Packers);

            //// Additional Data - ISO
            DataElementSpecs[FIELD.F_46] = new DefaultField(FIELD.F_46, 999, DEType.LLL, Packers);

            //// Additional Data - National
            DataElementSpecs[FIELD.F_47] = new DefaultField(FIELD.F_47, 999, DEType.LLL, Packers);

            //// BASE24 Additional Data
            DataElementSpecs[FIELD.F_48] = new DefaultField(FIELD.F_48, 999, DEType.LLL, Packers);

            //// Verification data
            DataElementSpecs[FIELD.F_49] = new DefaultField(FIELD.F_49, 3, DEType.N, Packers);

            //// Settlement Currency Code
            DataElementSpecs[FIELD.F_50] = new DefaultField(FIELD.F_50, 3, DEType.N, Packers);

            //// Cardholder Billing Currency Code
            DataElementSpecs[FIELD.F_51] = new DefaultField(FIELD.F_51, 3, DEType.N, Packers);

            //// Personal identification number (PIN) data
            DataElementSpecs[FIELD.F_52] = new DefaultField(FIELD.F_52, 16, DEType.AN, Packers);

            //// Security related control information
            DataElementSpecs[FIELD.F_53] = new DefaultField(FIELD.F_53, 16, DEType.N, Packers);

            //// Additional Amounts
            DataElementSpecs[FIELD.F_54] = new DefaultField(FIELD.F_54, 15, DEType.LLL, Packers);

            //// Reserved ISO
            DataElementSpecs[FIELD.F_55] = new DefaultField(FIELD.F_55, 999, DEType.LLL, Packers);

            //// Reserved ISO
            DataElementSpecs[FIELD.F_56] = new DefaultField(FIELD.F_56, 999, DEType.LLL, Packers);

            //// BASE24 POS Additional Data
            DataElementSpecs[FIELD.F_57] = new DefaultField(FIELD.F_57, 2500, DEType.LLLL, Packers);

            //// Reserved ISO
            DataElementSpecs[FIELD.F_58] = new DefaultField(FIELD.F_58, 999, DEType.LLL, Packers);

            //// Reserved ISO
            DataElementSpecs[FIELD.F_59] = new DefaultField(FIELD.F_59, 999, DEType.LLL, Packers);

            //// BASE24-POS Terminal Data
            DataElementSpecs[FIELD.F_60] = new DefaultField(FIELD.F_60, 61, DEType.LLL, Packers);

            //// BASE24-POS Card Issuer-Category-Response Code
            DataElementSpecs[FIELD.F_61] = new DefaultField(FIELD.F_61, 22, DEType.LLL, Packers);

            //// BASE24 Postal Code
            DataElementSpecs[FIELD.F_62] = new DefaultField(FIELD.F_62, 13, DEType.LLL, Packers);

            //// BASE24 POS Additional Data
            DataElementSpecs[FIELD.F_63] = new DefaultField(FIELD.F_63, 999, DEType.LLL, Packers);

            //// Message Authentication Code Field
            DataElementSpecs[FIELD.F_64] = new DefaultField(FIELD.F_64, 16, DEType.AN, Packers);

            //// Extended Bit Map
            DataElementSpecs[FIELD.F_65] = new DefaultField(FIELD.F_65, 16, DEType.AN, Packers);

            //// Settlement code
            DataElementSpecs[FIELD.F_66] = new DefaultField(FIELD.F_66, 1, DEType.N, Packers);

            //// Extended Payment Code
            DataElementSpecs[FIELD.F_67] = new DefaultField(FIELD.F_67, 2, DEType.N, Packers);

            //// Receiving Institution Country Code
            DataElementSpecs[FIELD.F_68] = new DefaultField(FIELD.F_68, 3, DEType.N, Packers);

            //// Settlement Institution Country Code
            DataElementSpecs[FIELD.F_69] = new DefaultField(FIELD.F_69, 3, DEType.N, Packers);

            //// Network management Information code
            DataElementSpecs[FIELD.F_70] = new DefaultField(FIELD.F_70, 3, DEType.N, Packers);

            //// Message Number
            DataElementSpecs[FIELD.F_71] = new DefaultField(FIELD.F_71, 4, DEType.N, Packers);

            //// Message Number Last
            DataElementSpecs[FIELD.F_72] = new DefaultField(FIELD.F_72, 4, DEType.N, Packers);

            //// Date, action
            DataElementSpecs[FIELD.F_73] = new DefaultField(FIELD.F_73, 6, DEType.AN, Packers);

            //// Credits, number
            DataElementSpecs[FIELD.F_74] = new DefaultField(FIELD.F_74, 10, DEType.N, Packers);

            //// Credits, reversal number
            DataElementSpecs[FIELD.F_75] = new DefaultField(FIELD.F_75, 10, DEType.N, Packers);

            //// Debits, number
            DataElementSpecs[FIELD.F_76] = new DefaultField(FIELD.F_76, 10, DEType.N, Packers);

            //// Debits, reversal number
            DataElementSpecs[FIELD.F_77] = new DefaultField(FIELD.F_77, 10, DEType.N, Packers);

            //// Transfer, number
            DataElementSpecs[FIELD.F_78] = new DefaultField(FIELD.F_78, 10, DEType.N, Packers);

            //// Transfer, reversal number
            DataElementSpecs[FIELD.F_79] = new DefaultField(FIELD.F_79, 10, DEType.N, Packers);

            //// Inquiries, number
            DataElementSpecs[FIELD.F_80] = new DefaultField(FIELD.F_80, 10, DEType.N, Packers);

            //// Authorizations, number
            DataElementSpecs[FIELD.F_81] = new DefaultField(FIELD.F_81, 10, DEType.N, Packers);

            //// Processing Fee Amount Credits
            DataElementSpecs[FIELD.F_82] = new DefaultField(FIELD.F_82, 12, DEType.N, Packers);

            //// Transaction Fee Amount Credits
            DataElementSpecs[FIELD.F_83] = new DefaultField(FIELD.F_83, 12, DEType.N, Packers);

            //// Processing Fee Amount Debits
            DataElementSpecs[FIELD.F_84] = new DefaultField(FIELD.F_84, 12, DEType.N, Packers);

            //// Transaction Fee Amount Debits
            DataElementSpecs[FIELD.F_85] = new DefaultField(FIELD.F_85, 12, DEType.N, Packers);

            //// Credits, amount
            DataElementSpecs[FIELD.F_86] = new DefaultField(FIELD.F_86, 16, DEType.N, Packers);

            //// Credits, reversal amount
            DataElementSpecs[FIELD.F_87] = new DefaultField(FIELD.F_87, 16, DEType.N, Packers);

            //// Debits, amount
            DataElementSpecs[FIELD.F_88] = new DefaultField(FIELD.F_88, 16, DEType.N, Packers);

            //// Debits, reversal amount
            DataElementSpecs[FIELD.F_89] = new DefaultField(FIELD.F_89, 16, DEType.N, Packers);

            //// Original data elements
            DataElementSpecs[FIELD.F_90] = new DefaultField(FIELD.F_90, 42, DEType.N, Packers);

            //// File update code
            DataElementSpecs[FIELD.F_91] = new DefaultField(FIELD.F_91, 1, DEType.AN, Packers);

            //// File Security Code
            DataElementSpecs[FIELD.F_92] = new DefaultField(FIELD.F_92, 2, DEType.AN, Packers);

            //// Response Indicator
            DataElementSpecs[FIELD.F_93] = new DefaultField(FIELD.F_93, 5, DEType.AN, Packers);

            //// Service Indicator
            DataElementSpecs[FIELD.F_94] = new DefaultField(FIELD.F_94, 7, DEType.AN, Packers);

            //// Replacement amounts
            DataElementSpecs[FIELD.F_95] = new DefaultField(FIELD.F_95, 42, DEType.AN, Packers);

            //// Message Security Code
            DataElementSpecs[FIELD.F_96] = new DefaultField(FIELD.F_96, 16, DEType.AN, Packers);

            //// Amount, net settlement
            DataElementSpecs[FIELD.F_97] = new DefaultField(FIELD.F_97, 17, DEType.AN, Packers);

            //// Payee
            DataElementSpecs[FIELD.F_98] = new DefaultField(FIELD.F_98, 25, DEType.ANS, Packers);

            //// Settlement institution identification code
            DataElementSpecs[FIELD.F_99] = new DefaultField(FIELD.F_99, 13, DEType.LL, Packers);

            //// Receiving Institution Identification Code
            DataElementSpecs[FIELD.F_100] = new DefaultField(FIELD.F_100, 13, DEType.LL, Packers);

            //// File name
            DataElementSpecs[FIELD.F_101] = new DefaultField(FIELD.F_101, 21, DEType.LL, Packers);

            //// Account Identification 1
            DataElementSpecs[FIELD.F_102] = new DefaultField(FIELD.F_102, 30, DEType.LL, Packers);

            //// Account Identification 2
            DataElementSpecs[FIELD.F_103] = new DefaultField(FIELD.F_103, 30, DEType.LL, Packers);

            //// TR31 PKBLOCK
            DataElementSpecs[FIELD.F_110] = new DefaultField(FIELD.F_110, 103, DEType.LLL, Packers);

            //// For Visa and Master Use
            DataElementSpecs[FIELD.F_113] = new DefaultField(FIELD.F_113, 999, DEType.LLL, Packers);

            //// For Visa and Master Use
            DataElementSpecs[FIELD.F_115] = new DefaultField(FIELD.F_115, 999, DEType.LLL, Packers);

            //// Reserved for private use
            DataElementSpecs[FIELD.F_120] = new DefaultField(FIELD.F_120, 999, DEType.LLL, Packers);

            //// From Host Maintenance Information
            DataElementSpecs[FIELD.F_122] = new DefaultField(FIELD.F_122, 78, DEType.LLL, Packers);

            //// Cryptographic Service Message
            DataElementSpecs[FIELD.F_123] = new DefaultField(FIELD.F_123, 153, DEType.LLL, Packers);

            //// Token Message
            DataElementSpecs[FIELD.F_124] = new DefaultField(FIELD.F_124, 1002, DEType.LLL, Packers);

            //// ATM Additional Data
            DataElementSpecs[FIELD.F_126] = new DefaultField(FIELD.F_126, 800, DEType.LLL, Packers);

            //// POS User Data
            DataElementSpecs[FIELD.F_127] = new DefaultField(FIELD.F_127, 103, DEType.LLL, Packers);
        }

        public NcccIso8583()
        {
        }

        public NcccIso8583(byte[] message)
            : base(message)
        {
        }

        public virtual NcccHeader Header()
        {
            return this.header;
        }

        public virtual ICodecable Header(NcccHeader header)
        {
            this.header = header;
            return this;
        }

        public new DefaultCore Core()
        {
            return (DefaultCore)base.Core();
        }

        public new NcccIso8583 Core(Core core)
        {
            return (NcccIso8583)base.Core(core);
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
            this.header = new NcccHeader(record.IndexSubstring(0, HeaderLength));
            base.Decode(Bytes.From(record.Substring(HeaderLength)).Val());
            this.FillVariables();
            return this;
        }

        public virtual string Track2Encode()
        {
            var separator = "=";
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
                        .Append(this.Core().ServiceCode.GetWithDefault(DEFAULT_PARAM.ServiceCode))
                        .Append(this.Core().AeEffectiveDate)
                        .Append(this.Core().AeDiscretionaryData)
                        .Append(this.Core().AeZero)
                        .Append(this.Core().AeLanguage);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(this.Core().ServiceCode))
                {
                    return stringBuilder.ToString();
                }

                stringBuilder.Append(this.Core().ServiceCode);
                if (this.Core().CardNo.StartsWith("9000") && this.Core().ServiceCode.Equals("701"))
                {
                    // UCARD
                    stringBuilder.Append(this.Core().Pvki).Append(this.Core().Pvv).Append(this.Core().Cvv);
                }
                else
                {
                    // VISA, MASTER, JCB
                    stringBuilder.Append(this.Core().Pvki == null ? string.Empty : this.Core().Pvki)
                                 .Append(this.Core().Pvv == null ? string.Empty : this.Core().Pvv)
                                 .Append(this.Core().Cvv == null ? string.Empty : this.Core().Cvv);
                }
            }

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
        public FIELD GetTokenFieldId()
        {
            if (this.Core().Fields().ContainsKey(FIELD.F_63))
                return FIELD.F_63;
            else if (this.Core().Fields().ContainsKey(FIELD.F_57))
                return FIELD.F_57;
            else
                return FIELD.F_UKN;
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
                case FIELD.F_25:
                    this.Core().PointOfServiceConditionMode = val;
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
                    if (!this.Mti().StartsWith("08") && !this.Mti().StartsWith("03") && val.Length == 27
                        && this.Core().MerchantId == null)
                    {
                        this.Core().MerchantId = val.IndexSubstring(0, 19).Trim();
                    }
                    break;
                case FIELD.F_49:
                    this.Core().TransactionCurrencyCode = val;
                    break;
                case FIELD.F_52:
                    this.Core().Pinblock = val;
                    break;
                case FIELD.F_53:
                    this.F53Decode(val);
                    break;
                case FIELD.F_57:
                    this.F57TokenDecode(val);
                    break;
                case FIELD.F_60:
                    this.Core().NcccBankCode = val.IndexSubstring(5, 7);
                    break;
                case FIELD.F_62:
                    this.Core().PostCode = val.Substring(3).Trim();
                    break;
                case FIELD.F_63:
                    this.F63TokenDecode(val);
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
                case FIELD.F_99:
                    this.Core().SettlementInstitutionIdentificationCode = val;
                    break;
                case FIELD.F_101:
                    this.F101FileNameDecode(val);
                    break;
                case FIELD.F_110:
                    this.F110TR31MessageDecode(val);
                    break;
                case FIELD.F_120:
                    this.F120Decode(val);
                    break;
                case FIELD.F_123:
                    this.F123CryptographicServiceMessageDecode(val);
                    break;
                case FIELD.F_124:
                    this.F124Decode(val);
                    break;
                case FIELD.F_126:
                    this.F126TokenDecode(val);
                    break;
                case FIELD.F_127:
                    this.F127PosUserDataDecode(val);
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
            if (this.Core().CardNo.StartsWith("1800"))
            {
                this.Core().CardType = CardType.Default;
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
            else if (this.Core().CardNo.StartsWith("9"))
            {
                this.Core().CardType = CardType.Get<UCard>();
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

        public string F57TokenEncode()
        {
            var tokenGroup = this.Core().GetOthersValByKey("F_57") as TokenGroup;

            if (!ObjectUtils.IsNull(tokenGroup))
            {
                return tokenGroup.Encode();
            }
            else
            {
                return null;
            }
        }

        public void F57TokenDecode(string val)
        {
            this.Core().SetOthersValByKey("F_57", new TokenGroup(val));
        }

        public string F63TokenEncode()
        {
            var tokenGroup = this.Core().GetOthersValByKey("F_63") as TokenGroup;

            if (!ObjectUtils.IsNull(tokenGroup))
            {
                return tokenGroup.Encode();
            }
            else
            {
                return null;
            }
        }

        public void F63TokenDecode(string val)
        {
            this.Core().SetOthersValByKey("F_63", new TokenGroup(val));
        }

        public virtual string F90OriginalDataElementsEncode()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(this.Core().OriginMti)
                .Append(this.Core().OriginRetrievalReferenceNumber)
                .Append(this.Core().OriginTransactionDate)
                .Append(this.Core().OriginTransactionTime)
                .Append(this.Core().OriginCaptureDate)
                .Append(string.Empty.PadRight(10, ' '));
            return stringBuilder.ToString();
        }

        public virtual void F90OriginalDataElementsDecode(string val)
        {
            this.Core().OriginMti = val.IndexSubstring(0, 4);
            this.Core().OriginRetrievalReferenceNumber = val.IndexSubstring(4, 16);
            this.Core().OriginTransactionDate = val.IndexSubstring(16, 20);
            this.Core().OriginTransactionTime = val.IndexSubstring(20, 28);
            this.Core().OriginCaptureDate = val.IndexSubstring(28, 32);
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

        public void F110TR31MessageDecode(string val)
        {
            this.Core().SetOthersValByKey("F_110", val);
        }

        public string F110TR31MessageEncode()
        {
            return (string)this.Core().GetOthersValByKey("F_110");
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

        public void F124Decode(string val)
        {
            this.Core().SetOthersValByKey("F_124", val);
        }

        public string F124Encode()
        {
            return (string)this.Core().GetOthersValByKey("F_124");
        }

        public string F126TokenEncode()
        {
            var tokenGroup = this.Core().GetOthersValByKey("F_126") as TokenGroup;

            if (!ObjectUtils.IsNull(tokenGroup))
            {
                return tokenGroup.Encode();
            }
            else
            {
                return null;
            }
        }

        public void F126TokenDecode(string val)
        {
            this.Core().SetOthersValByKey("F_126", new TokenGroup(val));
        }

        public void F127PosUserDataDecode(string val)
        {
            this.Core().SetOthersValByKey("F_127", val);
        }

        public string F127PosUserDataEncode()
        {
            return (string)this.Core().GetOthersValByKey("F_127");
        }
    }
}
