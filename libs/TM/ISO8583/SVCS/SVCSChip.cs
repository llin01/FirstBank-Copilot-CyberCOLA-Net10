namespace Com.Cybersoft4u.TM.ISO8583.SVCS
{
    #region DEPENDENCY

    using System.Collections.Generic;
    using Com.Cybersoft4u.CyberUtils.Exception;
    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.V1987;

    using DataElementSpec = ISO8583.DataElementSpec;

    #endregion

    public class SVCSChip : Iso8583V1987<SVCSChipDefaultCore>
    {
        private static readonly IDictionary<FIELD, DefaultField> DataElementSpecs =
            new Dictionary<FIELD, DefaultField>();

        private static readonly Packer Packers = new DefaultPacker();

        static SVCSChip()
        {
            //// Bit Map Extended
            DataElementSpecs[FIELD.F_1] = new DefaultField(FIELD.F_1, 16, DEType.AN, Packers);

            //// TX Code
            DataElementSpecs[FIELD.F_2] = new DefaultField(FIELD.F_2, 4, DEType.N, Packers);

            //// Batch Number
            DataElementSpecs[FIELD.F_3] = new DefaultField(FIELD.F_3, 6, DEType.N, Packers);

            //// Reload Function
            DataElementSpecs[FIELD.F_4] = new DefaultField(FIELD.F_4, 4, DEType.N, Packers);

            //// Host TX DateTime
            DataElementSpecs[FIELD.F_5] = new DefaultField(FIELD.F_5, 14, DEType.N, Packers);

            //// Terminal TX DateTime
            DataElementSpecs[FIELD.F_6] = new DefaultField(FIELD.F_6, 14, DEType.N, Packers);

            //// Terminal Trace Number
            DataElementSpecs[FIELD.F_7] = new DefaultField(FIELD.F_7, 6, DEType.N, Packers);

            //// Card Holder is Birthday
            DataElementSpecs[FIELD.F_10] = new DefaultField(FIELD.F_10, 8, DEType.N, Packers);

            //// ICC SNum
            DataElementSpecs[FIELD.F_11] = new DefaultField(FIELD.F_11, 8, DEType.N, Packers);

            //// ICC TP
            DataElementSpecs[FIELD.F_12] = new DefaultField(FIELD.F_12, 16, DEType.AN, Packers);

            //// LSN
            DataElementSpecs[FIELD.F_13] = new DefaultField(FIELD.F_13, 8, DEType.N, Packers);

            //// LRN
            DataElementSpecs[FIELD.F_14] = new DefaultField(FIELD.F_14, 16, DEType.AN, Packers);

            //// DSN
            DataElementSpecs[FIELD.F_15] = new DefaultField(FIELD.F_15, 8, DEType.N, Packers);

            //// SAM ID
            DataElementSpecs[FIELD.F_16] = new DefaultField(FIELD.F_16, 8, DEType.N, Packers);

            //// SAM SN
            DataElementSpecs[FIELD.F_17] = new DefaultField(FIELD.F_17, 8, DEType.N, Packers);

            //// SAM CRN
            DataElementSpecs[FIELD.F_18] = new DefaultField(FIELD.F_18, 16, DEType.AN, Packers);

            //// EasyCash Balance Before Transaction
            DataElementSpecs[FIELD.F_21] = new DefaultField(FIELD.F_21, 8, DEType.N, Packers);

            //// Loyalty1 Balance
            DataElementSpecs[FIELD.F_22] = new DefaultField(FIELD.F_22, 8, DEType.N, Packers);

            //// Loyalty2 Balance
            DataElementSpecs[FIELD.F_23] = new DefaultField(FIELD.F_23, 8, DEType.N, Packers);

            //// EasyCash Value Add
            DataElementSpecs[FIELD.F_24] = new DefaultField(FIELD.F_24, 8, DEType.N, Packers);

            //// EasyCash Deduction
            DataElementSpecs[FIELD.F_25] = new DefaultField(FIELD.F_25, 8, DEType.N, Packers);

            //// EasyCash Transaction Amount
            DataElementSpecs[FIELD.F_31] = new DefaultField(FIELD.F_31, 8, DEType.N, Packers);

            //// Loyalty1Transaction Amount
            DataElementSpecs[FIELD.F_32] = new DefaultField(FIELD.F_32, 8, DEType.N, Packers);

            //// Loyalty2 Transaction Amount
            DataElementSpecs[FIELD.F_33] = new DefaultField(FIELD.F_33, 8, DEType.N, Packers);

            //// LTAC Loading Command
            DataElementSpecs[FIELD.F_41] = new DefaultField(FIELD.F_41, 16, DEType.AN, Packers);

            //// DTAC
            DataElementSpecs[FIELD.F_42] = new DefaultField(FIELD.F_42, 16, DEType.AN, Packers);

            //// PTAC
            DataElementSpecs[FIELD.F_43] = new DefaultField(FIELD.F_43, 16, DEType.AN, Packers);

            //// TC Credit Loyalty1
            DataElementSpecs[FIELD.F_44] = new DefaultField(FIELD.F_44, 16, DEType.AN, Packers);

            //// TC Credit Loyalty2
            DataElementSpecs[FIELD.F_45] = new DefaultField(FIELD.F_45, 16, DEType.AN, Packers);

            //// TC Update Last Online Date
            DataElementSpecs[FIELD.F_46] = new DefaultField(FIELD.F_46, 16, DEType.AN, Packers);

            //// TC Block Card
            DataElementSpecs[FIELD.F_47] = new DefaultField(FIELD.F_47, 16, DEType.AN, Packers);

            //// Source Transaction Terminal TraceNo
            DataElementSpecs[FIELD.F_51] = new DefaultField(FIELD.F_51, 6, DEType.N, Packers);

            //// Source Transaction TX DateTime
            DataElementSpecs[FIELD.F_52] = new DefaultField(FIELD.F_52, 14, DEType.N, Packers);

            //// Source Transaction SNum
            DataElementSpecs[FIELD.F_53] = new DefaultField(FIELD.F_53, 8, DEType.N, Packers);


            //// Source Transaction EasyCash Balance Before Transaction
            DataElementSpecs[FIELD.F_54] = new DefaultField(FIELD.F_54, 8, DEType.N, Packers);
        }

        public SVCSChip()
        {
        }

        public SVCSChip(byte[] message)
            : base(message)
        {
        }

        public new SVCSChipDefaultCore Core()
        {
            return (SVCSChipDefaultCore)base.Core();
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

        public override ICodecable Decode(byte[] message)
        {
            base.Decode(message);
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
                    this.Core().TXCode = val;
                    break;
                case FIELD.F_3:
                    this.Core().BatchNumber = val;
                    break;
                case FIELD.F_4:
                    this.Core().ReloadFunction = val;
                    break;
                case FIELD.F_5:
                    this.Core().HostTXDateTime = val;
                    break;
                case FIELD.F_6:
                    this.Core().TerminalTXDateTime = val;
                    break;
                case FIELD.F_7:
                    this.Core().TerminalTraceNo = val;
                    break;
                case FIELD.F_10:
                    this.Core().CardHolderBirthday = val;
                    break;
                case FIELD.F_11:
                    this.Core().ICCSNum = val;
                    break;
                case FIELD.F_12:
                    this.Core().ICCTP = val;
                    break;
                case FIELD.F_13:
                    this.Core().LSN = val;
                    break;
                case FIELD.F_14:
                    this.Core().LRN = val;
                    break;
                case FIELD.F_15:
                    this.Core().DSN = val;
                    break;
                case FIELD.F_16:
                    this.Core().SAMID = val;
                    break;
                case FIELD.F_17:
                    this.Core().SAMSN = val;
                    break;
                case FIELD.F_18:
                    this.Core().SAMCRN = val;
                    break;
                case FIELD.F_21:
                    this.Core().EasyCashBalanceBeforeTransaction = val;
                    break;
                case FIELD.F_22:
                    this.Core().Loyalty1Balance = val;
                    break;
                case FIELD.F_23:
                    this.Core().Loyalty2Balance = val;
                    break;
                case FIELD.F_24:
                    this.Core().EasyCashValueAdd = val;
                    break;
                case FIELD.F_25:
                    this.Core().EasyCashDeduction = val;
                    break;
                case FIELD.F_31:
                    this.Core().EasyCashTransactionAmount = val;
                    break;
                case FIELD.F_32:
                    this.Core().Loyalty1TransactionAmount = val;
                    break;
                case FIELD.F_33:
                    this.Core().Loyalty2TransactionAmount = val;
                    break;
                case FIELD.F_41:
                    this.Core().LTACLoadingCommand = val;
                    break;
                case FIELD.F_42:
                    this.Core().DTAC = val;
                    break;
                case FIELD.F_43:
                    this.Core().PTAC = val;
                    break;
                case FIELD.F_44:
                    this.Core().TCCreditLoyalty1 = val;
                    break;
                case FIELD.F_45:
                    this.Core().TCCreditLoyalty2 = val;
                    break;
                case FIELD.F_46:
                    this.Core().TCUpdateLastOnlineDate = val;
                    break;
                case FIELD.F_47:
                    this.Core().TCBlockCard = val;
                    break;
                case FIELD.F_51:
                    this.Core().SourceTransactionTerminalTraceNo = val;
                    break;
                case FIELD.F_52:
                    this.Core().SourceTransactionTXDateTime = val;
                    break;
                case FIELD.F_53:
                    this.Core().SourceTransactionSNum = val;
                    break;
                case FIELD.F_54:
                    this.Core().SourceTransactionEasyCashBalanceBeforeTransaction = val;
                    break;
            }
        }
    }
}
