namespace Com.Cybersoft4u.TM.ISO8583.ICASH
{
    #region DEPENDENCY

    using System.Collections.Generic;
    using Com.Cybersoft4u.CyberUtils.Exception;
    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.V1987;

    using DataElementSpec = ISO8583.DataElementSpec;

    #endregion

    public class ICASHChip : Iso8583V1987<ICASHChipDefaultCore>
    {
        private static readonly IDictionary<FIELD, DefaultField> DataElementSpecs =
            new Dictionary<FIELD, DefaultField>();

        private static readonly Packer Packers = new DefaultPacker();

        static ICASHChip()
        {
            //// Bit Map Extended
            DataElementSpecs[FIELD.F_1] = new DefaultField(FIELD.F_1, 16, DEType.AN, Packers);

            //// Trans Date
            DataElementSpecs[FIELD.F_2] = new DefaultField(FIELD.F_2, 8, DEType.N, Packers);

            //// Store No
            DataElementSpecs[FIELD.F_3] = new DefaultField(FIELD.F_3, 8, DEType.N, Packers);

            //// Reg ID
            DataElementSpecs[FIELD.F_4] = new DefaultField(FIELD.F_4, 3, DEType.N, Packers);

            //// SN
            DataElementSpecs[FIELD.F_5] = new DefaultField(FIELD.F_5, 6, DEType.N, Packers);

            //// ACT Date
            DataElementSpecs[FIELD.F_6] = new DefaultField(FIELD.F_6, 8, DEType.N, Packers);

            //// Trans Type
            DataElementSpecs[FIELD.F_7] = new DefaultField(FIELD.F_7, 2, DEType.N, Packers);

            //// Tx Date Time
            DataElementSpecs[FIELD.F_8] = new DefaultField(FIELD.F_8, 14, DEType.N, Packers);

            //// Response Code
            DataElementSpecs[FIELD.F_9] = new DefaultField(FIELD.F_9, 8, DEType.N, Packers);

            //// Icash Card No
            DataElementSpecs[FIELD.F_10] = new DefaultField(FIELD.F_10, 16, DEType.N, Packers);

            //// Amount
            DataElementSpecs[FIELD.F_11] = new DefaultField(FIELD.F_11, 8, DEType.N, Packers);

            //// TSAN_OSN
            DataElementSpecs[FIELD.F_12] = new DefaultField(FIELD.F_12, 14, DEType.N, Packers);

            //// TSAM_TSN
            DataElementSpecs[FIELD.F_13] = new DefaultField(FIELD.F_13, 6, DEType.N, Packers);

            //// ICC_TSN
            DataElementSpecs[FIELD.F_14] = new DefaultField(FIELD.F_14, 8, DEType.N, Packers);

            //// ICC_LSN
            DataElementSpecs[FIELD.F_15] = new DefaultField(FIELD.F_15, 8, DEType.N, Packers);

            //// ICC_LA
            DataElementSpecs[FIELD.F_16] = new DefaultField(FIELD.F_16, 8, DEType.N, Packers);

            //// ICC_DSN
            DataElementSpecs[FIELD.F_17] = new DefaultField(FIELD.F_17, 8, DEType.N, Packers);

            //// ICC_DA
            DataElementSpecs[FIELD.F_18] = new DefaultField(FIELD.F_18, 8, DEType.N, Packers);

            //// LSAM_ID
            DataElementSpecs[FIELD.F_19] = new DefaultField(FIELD.F_19, 14, DEType.N, Packers);

            //// LSAM_CSN
            DataElementSpecs[FIELD.F_20] = new DefaultField(FIELD.F_20, 8, DEType.N, Packers);

            //// LSAM_CA
            DataElementSpecs[FIELD.F_21] = new DefaultField(FIELD.F_21, 8, DEType.N, Packers);

            //// LSAM_DSN
            DataElementSpecs[FIELD.F_22] = new DefaultField(FIELD.F_22, 8, DEType.N, Packers);

            //// LSAM_DA
            DataElementSpecs[FIELD.F_23] = new DefaultField(FIELD.F_23, 8, DEType.N, Packers);

            //// LSAM_DV
            DataElementSpecs[FIELD.F_24] = new DefaultField(FIELD.F_24, 8, DEType.N, Packers);

            //// PSAM_ID
            DataElementSpecs[FIELD.F_25] = new DefaultField(FIELD.F_25, 14, DEType.N, Packers);

            //// PSAM_CSN
            DataElementSpecs[FIELD.F_26] = new DefaultField(FIELD.F_26, 8, DEType.N, Packers);

            //// PSAM_CA
            DataElementSpecs[FIELD.F_27] = new DefaultField(FIELD.F_27, 8, DEType.N, Packers);

            //// PSAM_SBN
            DataElementSpecs[FIELD.F_28] = new DefaultField(FIELD.F_28, 8, DEType.N, Packers);

            //// PSAM_NRS
            DataElementSpecs[FIELD.F_29] = new DefaultField(FIELD.F_29, 8, DEType.N, Packers);

            //// PSAM_SV
            DataElementSpecs[FIELD.F_30] = new DefaultField(FIELD.F_30, 8, DEType.N, Packers);

            //// Card Expiration Date
            DataElementSpecs[FIELD.F_31] = new DefaultField(FIELD.F_31, 8, DEType.N, Packers);

            //// MAC
            DataElementSpecs[FIELD.F_32] = new DefaultField(FIELD.F_32, 8, DEType.AN, Packers);

            //// HSM_CODE
            DataElementSpecs[FIELD.F_33] = new DefaultField(FIELD.F_33, 4, DEType.N, Packers);

            //// MSRW_Terminal_SN
            DataElementSpecs[FIELD.F_34] = new DefaultField(FIELD.F_34, 19, DEType.AN, Packers);

            //// NECM_ID
            DataElementSpecs[FIELD.F_35] = new DefaultField(FIELD.F_35, 20, DEType.AN, Packers);

            //// ICC_BL
            DataElementSpecs[FIELD.F_36] = new DefaultField(FIELD.F_36, 8, DEType.N, Packers);
        }

        public ICASHChip()
        {
        }

        public ICASHChip(byte[] message)
            : base(message)
        {
        }

        public new ICASHChipDefaultCore Core()
        {
            return (ICASHChipDefaultCore)base.Core();
        }

        public new ICASH Core(Core core)
        {
            return (ICASH)base.Core(core);
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
                    this.Core().TransDate = val;
                    break;
                case FIELD.F_3:
                    this.Core().StoreNo = val;
                    break;
                case FIELD.F_4:
                    this.Core().RegID = val;
                    break;
                case FIELD.F_5:
                    this.Core().SN = val;
                    break;
                case FIELD.F_6:
                    this.Core().ACTDate = val;
                    break;
                case FIELD.F_7:
                    this.Core().TransType = val;
                    break;
                case FIELD.F_8:
                    this.Core().TxDateTime = val;
                    break;
                case FIELD.F_9:
                    this.Core().ResponseCode = val;
                    break;
                case FIELD.F_10:
                    this.Core().IcashCardNo = val;
                    break;
                case FIELD.F_11:
                    this.Core().Amount = val;
                    break;
                case FIELD.F_12:
                    this.Core().TSAM_OSN = val;
                    break;
                case FIELD.F_13:
                    this.Core().TSAM_TSN = val;
                    break;
                case FIELD.F_14:
                    this.Core().ICC_TSN = val;
                    break;
                case FIELD.F_15:
                    this.Core().ICC_LSN = val;
                    break;
                case FIELD.F_16:
                    this.Core().ICC_LA = val;
                    break;
                case FIELD.F_17:
                    this.Core().ICC_DSN = val;
                    break;
                case FIELD.F_18:
                    this.Core().ICC_DA = val;
                    break;
                case FIELD.F_19:
                    this.Core().LSAM_ID = val;
                    break;
                case FIELD.F_20:
                    this.Core().LSAM_CSN = val;
                    break;
                case FIELD.F_21:
                    this.Core().LSAM_CA = val;
                    break;
                case FIELD.F_22:
                    this.Core().LSAM_DSN = val;
                    break;
                case FIELD.F_23:
                    this.Core().LSAM_DA = val;
                    break;
                case FIELD.F_24:
                    this.Core().LSAM_DV = val;
                    break;
                case FIELD.F_25:
                    this.Core().PSAM_ID = val;
                    break;
                case FIELD.F_26:
                    this.Core().PSAM_CSN = val;
                    break;
                case FIELD.F_27:
                    this.Core().PSAM_CA = val;
                    break;
                case FIELD.F_28:
                    this.Core().PSAM_SBN = val;
                    break;
                case FIELD.F_29:
                    this.Core().PSAM_NRS = val;
                    break;
                case FIELD.F_30:
                    this.Core().PSAM_SV = val;
                    break;
                case FIELD.F_31:
                    this.Core().CardExpirationDate = val;
                    break;
                case FIELD.F_32:
                    this.Core().MAC = val;
                    break;
                case FIELD.F_33:
                    this.Core().HSM_CODE = val;
                    break;
                case FIELD.F_34:
                    this.Core().HSRW_Terminal_SN = val;
                    break;
                case FIELD.F_35:
                    this.Core().MECM_ID = val;
                    break;
                case FIELD.F_36:
                    this.Core().ICC_BL = val;
                    break;
            }
        }
    }
}
