// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="MCOLA.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the MCOLA type.
//   This is mainframe COLA type.
// </summary>
//
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.MCOLA
{
    #region DEPENDENCY

    using System.Collections.Generic;
    using System.Linq;

    using Com.Cybersoft4u.CyberUtils.Exception;
    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;
    using Com.Cybersoft4u.TM.ISO8583.Nccc;
    using Com.Cybersoft4u.TM.ISO8583.V1987;

    using DataElementSpec = Com.Cybersoft4u.TM.ISO8583.DataElementSpec;

    #endregion

    public class MCOLA : NcccIso8583, ITransaction
    {
        private static readonly IDictionary<FIELD, DefaultField> DataElementSpecs =
            new Dictionary<FIELD, DefaultField>();

        private static readonly Packer Packers = new DefaultPacker();

        static MCOLA()
        {
            DataElementSpecs[FIELD.F_5] = new DefaultField(FIELD.F_5, 12, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_6] = new DefaultField(FIELD.F_6, 12, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_8] = new DefaultField(FIELD.F_8, 8, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_9] = new DefaultField(FIELD.F_9, 8, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_10] = new DefaultField(FIELD.F_10, 8, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_14] = new DefaultField(FIELD.F_14, 4, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_16] = new DefaultField(FIELD.F_16, 4, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_19] = new DefaultField(FIELD.F_19, 3, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_20] = new DefaultField(FIELD.F_20, 3, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_21] = new DefaultField(FIELD.F_21, 3, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_23] = new DefaultField(FIELD.F_23, 3, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_24] = new DefaultField(FIELD.F_24, 3, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_26] = new DefaultField(FIELD.F_26, 2, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_27] = new DefaultField(FIELD.F_27, 1, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_29] = new DefaultField(FIELD.F_29, 8, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_30] = new DefaultField(FIELD.F_30, 8, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_31] = new DefaultField(FIELD.F_31, 8, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_33] = new DefaultField(FIELD.F_33, 13, DEType.LL, Packers);
            DataElementSpecs[FIELD.F_34] = new DefaultField(FIELD.F_34, 28, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_40] = new DefaultField(FIELD.F_40, 3, DEType.LLL, Packers);
            DataElementSpecs[FIELD.F_44] = new DefaultField(FIELD.F_44, 27, DEType.LL, Packers);
            DataElementSpecs[FIELD.F_45] = new DefaultField(FIELD.F_45, 78, DEType.LL, Packers);
            DataElementSpecs[FIELD.F_46] = new DefaultField(FIELD.F_46, 1, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_47] = new DefaultField(FIELD.F_47, 1, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_50] = new DefaultField(FIELD.F_50, 3, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_51] = new DefaultField(FIELD.F_51, 3, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_54] = new DefaultField(FIELD.F_54, 15, DEType.LLL, Packers);
            DataElementSpecs[FIELD.F_55] = new DefaultField(FIELD.F_55, 1, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_56] = new DefaultField(FIELD.F_56, 1, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_57] = new DefaultField(FIELD.F_57, 1, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_58] = new DefaultField(FIELD.F_58, 1, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_59] = new DefaultField(FIELD.F_59, 1, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_64] = new DefaultField(FIELD.F_64, 16, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_65] = new DefaultField(FIELD.F_65, 16, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_67] = new DefaultField(FIELD.F_67, 2, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_68] = new DefaultField(FIELD.F_68, 3, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_69] = new DefaultField(FIELD.F_69, 3, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_71] = new DefaultField(FIELD.F_71, 4, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_72] = new DefaultField(FIELD.F_72, 4, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_82] = new DefaultField(FIELD.F_82, 12, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_83] = new DefaultField(FIELD.F_83, 12, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_84] = new DefaultField(FIELD.F_84, 12, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_85] = new DefaultField(FIELD.F_85, 12, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_92] = new DefaultField(FIELD.F_92, 2, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_93] = new DefaultField(FIELD.F_93, 5, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_94] = new DefaultField(FIELD.F_94, 7, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_96] = new DefaultField(FIELD.F_96, 16, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_98] = new DefaultField(FIELD.F_98, 25, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_100] = new DefaultField(FIELD.F_100, 13, DEType.LL, Packers);
            DataElementSpecs[FIELD.F_102] = new DefaultField(FIELD.F_102, 28, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_103] = new DefaultField(FIELD.F_103, 28, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_104] = new DefaultField(FIELD.F_104, 1, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_105] = new DefaultField(FIELD.F_105, 1, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_106] = new DefaultField(FIELD.F_106, 1, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_107] = new DefaultField(FIELD.F_107, 1, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_108] = new DefaultField(FIELD.F_108, 1, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_109] = new DefaultField(FIELD.F_109, 1, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_110] = new DefaultField(FIELD.F_110, 1, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_111] = new DefaultField(FIELD.F_111, 1, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_112] = new DefaultField(FIELD.F_112, 1, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_113] = new DefaultField(FIELD.F_113, 999, DEType.LLL, Packers);
            DataElementSpecs[FIELD.F_114] = new DefaultField(FIELD.F_114, 1, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_115] = new DefaultField(FIELD.F_115, 27, DEType.LLL, Packers);
            DataElementSpecs[FIELD.F_116] = new DefaultField(FIELD.F_116, 1, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_117] = new DefaultField(FIELD.F_117, 1, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_118] = new DefaultField(FIELD.F_118, 1, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_119] = new DefaultField(FIELD.F_119, 1, DEType.AN, Packers);
            DataElementSpecs[FIELD.F_121] = new DefaultField(FIELD.F_121, 63, DEType.LLL, Packers);
            DataElementSpecs[FIELD.F_124] = new DefaultField(FIELD.F_124, 1002, DEType.LLL, Packers);
            DataElementSpecs[FIELD.F_125] = new DefaultField(FIELD.F_125, 15, DEType.LLL, Packers);
            DataElementSpecs[FIELD.F_127] = new DefaultField(FIELD.F_127, 999, DEType.LLL, Packers);
            DataElementSpecs[FIELD.F_128] = new DefaultField(FIELD.F_128, 16, DEType.AN, Packers);
        }

        public MCOLA()
            : base(null)
        {
            this.ConstructProcess();
        }

        public MCOLA(byte[] message)
            : base(message)
        {
        }

        protected internal delegate object SetTask();

        public string Dst
        {
            get
            {
                return this.Extend<TPDUDstKey, string>();
            }

            set
            {
                this.Extend<TPDUDstKey>(value);
            }
        }

        public string Src
        {
            get
            {
                return this.Extend<TPDUSrcKey, string>();
            }

            set
            {
                this.Extend<TPDUSrcKey>(value);
            }
        }

        public override NcccHeader Header()
        {
            return this.Extend<HeaderKey, NcccHeader>();
        }

        public override ICodecable Header(NcccHeader header)
        {
            this.Extend<HeaderKey>(header);
            return this;
        }

        public override DataElementSpec Spec(FIELD field)
        {
            DefaultField spec;
            return DataElementSpecs.TryGetValue(field, out spec) ? spec : base.Spec(field);
        }

        public new MCOLA Core(Core core)
        {
            var mcola = (MCOLA)base.Core(core);
            mcola.ConstructProcess();
            return mcola;
        }

        public override Bytes Encode()
        {
            ListSpecialField();
            return
                new Bytes(
                    new ByteBuffer().WriteBytes(this.Dst.GetAsciiBytes()).WriteBytes(this.Src.GetAsciiBytes()).WriteBytes(base.Encode().Val()).Val());
        }

        public override ICodecable Decode(byte[] message)
        {
            var data = new ByteBuffer(message);
            this.Dst = data.ReadBytes(4).GetUTF8String();
            this.Src = data.ReadBytes(4).GetUTF8String();
            base.Decode(data.Val());
            ListSpecialField();
            return this;
        }

        //public virtual Bytes PureEncode()
        //{
        //    return
        //        new Bytes(
        //            new ByteBuffer().WriteBytes(base.Encode().Val()).Val());
        //}

        //public virtual ICodecable PureDecode(byte[] message)
        //{
        //    var data = new ByteBuffer(message);
        //    base.Decode(data.Val());
        //    return this;
        //}

        public string Name()
        {
            return this.GetType().FullName;
        }

        public string F5Encode()
        {
            return this.Core().GetOthersValByKey("F_5") as string;
        }

        public void F5Decode(string val)
        {
            this.Core().SetOthersValByKey("F_5", val);
        }

        public string F6Encode()
        {
            return this.Core().GetOthersValByKey("F_6") as string;
        }

        public void F6Decode(string val)
        {
            this.Core().SetOthersValByKey("F_6", val);
        }

        public string F9Encode()
        {
            return this.Core().GetOthersValByKey("F_9") as string;
        }

        public void F9Decode(string val)
        {
            this.Core().SetOthersValByKey("F_9", val);
        }

        public string F10Encode()
        {
            return this.Core().GetOthersValByKey("F_10") as string;
        }

        public void F10Decode(string val)
        {
            this.Core().SetOthersValByKey("F_10", val);
        }

        public string F16Encode()
        {
            return this.Core().GetOthersValByKey("F_16") as string;
        }

        public void F16Decode(string val)
        {
            this.Core().SetOthersValByKey("F_16", val);
        }

        public string F20Encode()
        {
            return this.Core().GetOthersValByKey("F_20") as string;
        }

        public void F20Decode(string val)
        {
            this.Core().SetOthersValByKey("F_20", val);
        }

        public string F23Encode()
        {
            return this.Core().GetOthersValByKey("F_23") as string;
        }

        public void F23Decode(string val)
        {
            this.Core().SetOthersValByKey("F_23", val);
        }

        public string F33Encode()
        {
            return this.Core().GetOthersValByKey("F_33") as string;
        }

        public void F33Decode(string val)
        {
            this.Core().SetOthersValByKey("F_33", val);
        }

        public override string Track2Encode()
        {
            try
            {
                var track2 = this.Core().GetOthersValByKey("F_35") as string;
                return new string(track2.Where(c => !char.IsControl(c)).ToArray());
            }
            catch
            {
                return this.Core().GetOthersValByKey("F_35") as string;
            }
        }

        public override void Track2Decode(string val)
        {
            this.Core().SetOthersValByKey("F_35", val);
        }

        protected override void SetCardType()
        {
        }

        public string F45Encode()
        {
            return this.Core().GetOthersValByKey("F_45") as string;
        }

        public void F45Decode(string val)
        {
            this.Core().SetOthersValByKey("F_45", val);
        }

        public string F48Encode()
        {
            return this.Core().GetOthersValByKey("F_48") as string;
        }

        public void F48Decode(string val)
        {
            this.Core().SetOthersValByKey("F_48", val);
        }

        public string F50Encode()
        {
            return this.Core().GetOthersValByKey("F_50") as string;
        }

        public void F50Decode(string val)
        {
            this.Core().SetOthersValByKey("F_50", val);
        }

        public string F51Encode()
        {
            return this.Core().GetOthersValByKey("F_51") as string;
        }

        public void F51Decode(string val)
        {
            this.Core().SetOthersValByKey("F_51", val);
        }

        public string F54Encode()
        {
            return this.Core().GetOthersValByKey("F_54") as string;
        }

        public void F54Decode(string val)
        {
            this.Core().SetOthersValByKey("F_54", val);
        }

        public override string F90OriginalDataElementsEncode()
        {
            return this.Core().GetOthersValByKey("F_90") as string;
        }

        public override void F90OriginalDataElementsDecode(string val)
        {
            this.Core().SetOthersValByKey("F_90", val);
        }

        public string F94Encode()
        {
            return this.Core().GetOthersValByKey("F_94") as string;
        }

        public void F94Decode(string val)
        {
            this.Core().SetOthersValByKey("F_94", val);
        }

        public string F96Encode()
        {
            return this.Core().GetOthersValByKey("F_96") as string;
        }

        public void F96Decode(string val)
        {
            this.Core().SetOthersValByKey("F_96", val);
        }

        public new string F124Encode()
        {
            return this.Core().GetOthersValByKey("F_124") as string;
        }

        public new void F124Decode(string val)
        {
            this.Core().SetOthersValByKey("F_124", val);
        }

        protected internal MCOLA SetFieldWhenNull(FIELD field, SetTask value)
        {
            if (ObjectUtils.IsNull(this.RawVal(field)))
            {
                this.RawVal(field, value());
            }

            return this;
        }

        protected internal MCOLA CheckNonNullFields(FIELD[] fields)
        {
            fields.ToList().ForEach(
                field =>
                {
                    if (ObjectUtils.IsNull(this.RawVal(field)))
                    {
                        throw new NoSuchElementException(string.Format("FIELD[{0}] can't be null.", field));
                    }
                });

            return this;
        }

        protected override void ConstructProcess()
        {
            base.ConstructProcess();
            if (ObjectUtils.IsNull(this.Src))
            {
                this.Src = "IAGW";
            }

            if (ObjectUtils.IsNull(this.Dst))
            {
                this.Dst = "COLA";
            }

            if (ObjectUtils.IsNull(this.Header()))
            {
                this.Header(new NcccHeader());
            }
        }

        protected override void MatchAndFill(FIELD field, string val)
        {
            base.MatchAndFill(field, val);

            switch (field)
            {
                case FIELD.F_5:
                    this.F5Decode(val);
                    break;
                case FIELD.F_6:
                    this.F6Decode(val);
                    break;
                case FIELD.F_9:
                    this.F9Decode(val);
                    break;
                case FIELD.F_10:
                    this.F10Decode(val);
                    break;
                case FIELD.F_16:
                    this.F16Decode(val);
                    break;
                case FIELD.F_20:
                    this.F20Decode(val);
                    break;
                case FIELD.F_23:
                    this.F23Decode(val);
                    break;
                case FIELD.F_33:
                    this.F33Decode(val);
                    break;
                case FIELD.F_45:
                    this.F45Decode(val);
                    break;
                case FIELD.F_48:
                    this.F48Decode(val);
                    break;
                case FIELD.F_50:
                    this.F50Decode(val);
                    break;
                case FIELD.F_51:
                    this.F51Decode(val);
                    break;
                case FIELD.F_54:
                    this.F54Decode(val);
                    break;
                case FIELD.F_94:
                    this.F94Decode(val);
                    break;
                case FIELD.F_96:
                    this.F96Decode(val);
                    break;
            }
        }

        private void ListSpecialField()
        {
            var reqTokenGroup = (TokenGroup)this.Core().GetOthersValByKey("F_63");

            if (ObjectUtils.IsNull(reqTokenGroup))
            {
                var reqTokenGroupStr = this.Core().GetFieldsByKey(FIELD.F_63);
                if (!ObjectUtils.IsNull(reqTokenGroupStr))
                {
                    this.F63TokenDecode((string)reqTokenGroupStr);
                    reqTokenGroup = (TokenGroup)this.Core().GetOthersValByKey("F_63");
                }
            }

            ShowFieldsNcccIso8583Token(reqTokenGroup);
        }

        public void ShowFieldsNcccIso8583Token(TokenGroup TokenGrp)
        {
            if (!ObjectUtils.IsNull(TokenGrp))
            {
                foreach (var token in TokenGrp.Tokens)
                {
                    this.Logger.Debug(() => string.Format("[Token{0}] = [{1}]", token.Key, token.Value.Encode()));
                }
            }
        }

    }
}
