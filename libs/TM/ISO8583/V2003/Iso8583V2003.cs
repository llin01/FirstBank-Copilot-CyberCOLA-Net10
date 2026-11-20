namespace Com.Cybersoft4u.TM.ISO8583.V2003
{
    #region DEPENDENCY

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Com.Cybersoft4u.CyberUtils.Exception;
    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.Exception;

    #endregion

    public class Iso8583V2003<T> : AbstractIso8583<T>, CoreCodecable
        where T : Core
    {
        private static readonly IDictionary<FIELD, FieldDef> DataElementSpecs = new Dictionary<FIELD, FieldDef>();

        private static readonly Packer Packers = new DefaultPacker();

        static Iso8583V2003()
        {
            DataElementSpecs.Add(FIELD.F_1, new FieldDef(FIELD.F_1, 8, Format.Unspecified, Representation.B, Packers));
            DataElementSpecs.Add(FIELD.F_2, new FieldDef(FIELD.F_2, 19, Format.LLVAR, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_3, new FieldDef(FIELD.F_3, 6, Format.Unspecified, Representation.AN, Packers));
            DataElementSpecs.Add(FIELD.F_4, new FieldDef(FIELD.F_4, 16, Format.Unspecified, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_5, new FieldDef(FIELD.F_5, 16, Format.Unspecified, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_6, new FieldDef(FIELD.F_6, 16, Format.Unspecified, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_7, new FieldDef(FIELD.F_7, 10, Format.MMDDhhmmss, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_8, new FieldDef(FIELD.F_8, 12, Format.Unspecified, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_9, new FieldDef(FIELD.F_9, 8, Format.Unspecified, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_10, new FieldDef(FIELD.F_10, 8, Format.Unspecified, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_11, new FieldDef(FIELD.F_11, 12, Format.Unspecified, Representation.N, Packers));
            DataElementSpecs.Add(
                FIELD.F_12,
                new FieldDef(FIELD.F_12, 14, Format.CCYYMMDDhhmmss, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_13, new FieldDef(FIELD.F_13, 6, Format.CCYYMM, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_14, new FieldDef(FIELD.F_14, 4, Format.YYMM, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_15, new FieldDef(FIELD.F_15, 8, Format.CCYYMMDD, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_16, new FieldDef(FIELD.F_16, 4, Format.MMDD, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_17, new FieldDef(FIELD.F_17, 4, Format.MMDD, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_18, new FieldDef(FIELD.F_18, 140, Format.LLLVAR, Representation.ANSB, Packers));
            DataElementSpecs.Add(FIELD.F_19, new FieldDef(FIELD.F_19, 3, Format.Unspecified, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_20, new FieldDef(FIELD.F_20, 3, Format.Unspecified, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_21, new FieldDef(FIELD.F_21, 22, Format.Unspecified, Representation.ANS, Packers));
            DataElementSpecs.Add(FIELD.F_22, new FieldDef(FIELD.F_22, 16, Format.Unspecified, Representation.B, Packers));
            DataElementSpecs.Add(FIELD.F_23, new FieldDef(FIELD.F_23, 3, Format.Unspecified, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_24, new FieldDef(FIELD.F_24, 3, Format.Unspecified, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_25, new FieldDef(FIELD.F_25, 4, Format.Unspecified, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_26, new FieldDef(FIELD.F_26, 4, Format.Unspecified, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_27, new FieldDef(FIELD.F_27, 27, Format.Unspecified, Representation.ANB, Packers));
            DataElementSpecs.Add(FIELD.F_28, new FieldDef(FIELD.F_28, 8, Format.CCYYMMDD, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_29, new FieldDef(FIELD.F_29, 3, Format.Unspecified, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_30, new FieldDef(FIELD.F_30, 32, Format.Unspecified, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_31, new FieldDef(FIELD.F_31, 23, Format.Unspecified, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_32, new FieldDef(FIELD.F_32, 11, Format.LLVAR, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_33, new FieldDef(FIELD.F_33, 11, Format.LLVAR, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_34, new FieldDef(FIELD.F_34, 9999, Format.LLLLVAR, Representation.B, Packers));
            DataElementSpecs.Add(FIELD.F_35, new FieldDef(FIELD.F_35, 37, Format.LLVAR, Representation.Z, Packers));
            DataElementSpecs.Add(FIELD.F_36, new FieldDef(FIELD.F_36, 104, Format.LLLVAR, Representation.Z, Packers));
            DataElementSpecs.Add(FIELD.F_37, new FieldDef(FIELD.F_37, 12, Format.Unspecified, Representation.ANP, Packers));
            DataElementSpecs.Add(FIELD.F_38, new FieldDef(FIELD.F_38, 6, Format.Unspecified, Representation.ANP, Packers));
            DataElementSpecs.Add(FIELD.F_39, new FieldDef(FIELD.F_39, 4, Format.Unspecified, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_40, new FieldDef(FIELD.F_40, 3, Format.Unspecified, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_41, new FieldDef(FIELD.F_41, 16, Format.Unspecified, Representation.ANS, Packers));
            DataElementSpecs.Add(FIELD.F_42, new FieldDef(FIELD.F_42, 35, Format.LLVAR, Representation.ANS, Packers));
            DataElementSpecs.Add(
                FIELD.F_43,
                new FieldDef(FIELD.F_43, 9999, Format.LLLLVAR, Representation.ANSB, Packers));
            DataElementSpecs.Add(
                FIELD.F_44,
                new FieldDef(FIELD.F_44, 9999, Format.LLLLVAR, Representation.ANSB, Packers));
            DataElementSpecs.Add(FIELD.F_45, new FieldDef(FIELD.F_45, 76, Format.LLVAR, Representation.ANS, Packers));
            DataElementSpecs.Add(FIELD.F_46, new FieldDef(FIELD.F_46, 216, Format.LLLVAR, Representation.ANS, Packers));
            DataElementSpecs.Add(FIELD.F_47, new FieldDef(FIELD.F_47, 999, Format.LLLVAR, Representation.ANS, Packers));
            DataElementSpecs.Add(FIELD.F_48, new FieldDef(FIELD.F_48, 999, Format.LLLVAR, Representation.ANS, Packers));
            DataElementSpecs.Add(
                FIELD.F_49,
                new FieldDef(FIELD.F_49, 9999, Format.LLLLVAR, Representation.ANS, Packers));
            DataElementSpecs.Add(FIELD.F_52, new FieldDef(FIELD.F_52, 8, Format.Unspecified, Representation.B, Packers));
            DataElementSpecs.Add(FIELD.F_53, new FieldDef(FIELD.F_53, 48, Format.LLVAR, Representation.B, Packers));
            DataElementSpecs.Add(FIELD.F_54, new FieldDef(FIELD.F_54, 250, Format.LLLVAR, Representation.ANS, Packers));
            DataElementSpecs.Add(FIELD.F_55, new FieldDef(FIELD.F_55, 9999, Format.Unspecified, Representation.B, Packers));
            DataElementSpecs.Add(FIELD.F_56, new FieldDef(FIELD.F_56, 41, Format.LLVAR, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_57, new FieldDef(FIELD.F_57, 3, Format.Unspecified, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_58, new FieldDef(FIELD.F_58, 11, Format.LLVAR, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_59, new FieldDef(FIELD.F_59, 999, Format.LLLVAR, Representation.ANS, Packers));
            DataElementSpecs.Add(FIELD.F_64, new FieldDef(FIELD.F_64, 4, Format.Unspecified, Representation.B, Packers));
            DataElementSpecs.Add(FIELD.F_66, new FieldDef(FIELD.F_66, 216, Format.LLLVAR, Representation.ANS, Packers));
            DataElementSpecs.Add(FIELD.F_67, new FieldDef(FIELD.F_67, 2, Format.Unspecified, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_68, new FieldDef(FIELD.F_68, 9, Format.Unspecified, Representation.AN, Packers));
            DataElementSpecs.Add(FIELD.F_69, new FieldDef(FIELD.F_69, 40, Format.Unspecified, Representation.ANS, Packers));
            DataElementSpecs.Add(FIELD.F_70, new FieldDef(FIELD.F_70, 18, Format.Unspecified, Representation.N, Packers));
            DataElementSpecs.Add(
                FIELD.F_72,
                new FieldDef(FIELD.F_72, 9999, Format.LLLLVAR, Representation.ANSB, Packers));
            DataElementSpecs.Add(FIELD.F_73, new FieldDef(FIELD.F_73, 8, Format.CCYYMMDD, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_74, new FieldDef(FIELD.F_74, 156, Format.Unspecified, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_75, new FieldDef(FIELD.F_75, 90, Format.Unspecified, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_93, new FieldDef(FIELD.F_93, 11, Format.LLVAR, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_94, new FieldDef(FIELD.F_94, 11, Format.LLVAR, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_95, new FieldDef(FIELD.F_95, 99, Format.LLVAR, Representation.ANS, Packers));
            DataElementSpecs.Add(FIELD.F_96, new FieldDef(FIELD.F_96, 999, Format.LLLVAR, Representation.B, Packers));
            DataElementSpecs.Add(FIELD.F_97, new FieldDef(FIELD.F_97, 21, Format.Unspecified, Representation.XN, Packers));
            DataElementSpecs.Add(FIELD.F_98, new FieldDef(FIELD.F_98, 25, Format.Unspecified, Representation.ANS, Packers));
            DataElementSpecs.Add(FIELD.F_99, new FieldDef(FIELD.F_99, 11, Format.LLVAR, Representation.AN, Packers));
            DataElementSpecs.Add(FIELD.F_100, new FieldDef(FIELD.F_100, 11, Format.LLVAR, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_101, new FieldDef(FIELD.F_101, 99, Format.LLVAR, Representation.ANS, Packers));
            DataElementSpecs.Add(FIELD.F_102, new FieldDef(FIELD.F_102, 28, Format.LLVAR, Representation.ANS, Packers));
            DataElementSpecs.Add(FIELD.F_103, new FieldDef(FIELD.F_103, 28, Format.LLVAR, Representation.ANS, Packers));
            DataElementSpecs.Add(
                FIELD.F_104,
                new FieldDef(FIELD.F_104, 9999, Format.Unspecified, Representation.ANSB, Packers));
            DataElementSpecs.Add(
                FIELD.F_109,
                new FieldDef(FIELD.F_109, 144, Format.LLLVAR, Representation.ANS, Packers));
            DataElementSpecs.Add(
                FIELD.F_110,
                new FieldDef(FIELD.F_110, 144, Format.LLLVAR, Representation.ANS, Packers));
            DataElementSpecs.Add(FIELD.F_128, new FieldDef(FIELD.F_128, 4, Format.Unspecified, Representation.B, Packers));
        }

        public virtual ByteBuffer CoreEncode(Core core)
        {
            UpdateCoreShell(core, this);

            this.Logger.Debug(() => "Core Decode", () => string.Format("MTI:{0}", this.Mti()));
            var rtn = new ByteBuffer();
            rtn.WriteBytes(core.MessageTypeIndicator.GetUTF8Bytes()).WriteBytes(this.Bitmap(core));
            foreach (var pair in core.OrderedFields().Where(pair => pair.Value != null))
            {
                var key = pair.Key;
                var fieldData = ((DataElementSpec)this.Spec(key)).Pack(core.Field(key));
                rtn.WriteByteBuffer(fieldData);
                this.Logger.Debug(() => string.Format("FIELD[{0}]:{1}", key, Bytes.From(fieldData.Val())));
            }

            return rtn;
        }

        public virtual Core CoreDecode(byte[] message)
        {
            Core core;
            try
            {
                core = (Core)Activator.CreateInstance(typeof(T));
                UpdateCoreShell(core, this);

                var buf = new ByteBuffer(message);
                core.MessageTypeIndicator = this.DecodeMti(buf);
                this.Logger.Debug(() => string.Format("MTI:{0}", this.Mti()));
                var bitmap = this.DecodeBitmap(buf);
                this.UnpackDataElement(bitmap, core, ByteBuffer.From(buf.Val(), 0, buf.ReadRemainLength()), 0);
            }
            catch (FieldDecodeException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new DecodeFailException(e);
            }

            return core;
        }

        public virtual void CorePrepare(Core core)
        {
            UpdateCoreShell(core, this);
        }

        /// <summary>
        /// Encode <code>ISO8583</code> object to byte data.
        /// Override <code>CoreEncode</code> instead of this
        /// </summary>
        /// <returns></returns>
        public sealed override Bytes Encode()
        {
            return new Bytes(this.CoreEncode(this.Core()).Val());
        }

        /// <summary>
        /// Decode <code>message</code> to ISO8583 object
        /// Override <code>CoreDecode</code> instead of this
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public sealed override ICodecable Decode(byte[] message)
        {
            if (message == null)
            {
                return this;
            }

            this.Core(this.CoreDecode(message));
            return this;
        }

        /// <summary>
        /// Prepare fields data from core variable.
        /// Override <code>CorePrepare</code> instead of this
        /// </summary>
        /// <returns></returns>
        public sealed override Iso8583 Prepare()
        {
            this.CorePrepare(this.Core());
            return this;
        }

        public override ISO8583.DataElementSpec Spec(FIELD field)
        {
            FieldDef spec;
            if (!DataElementSpecs.TryGetValue(field, out spec))
            {
                throw new InvalidValueException(string.Format("Field[{0}] is not found.", field));
            }

            return spec;
        }

        public override string Identity()
        {
            return this.Core() is CoreV2003 ? ((CoreV2003)this.Core()).Identity() : string.Empty;
        }

        protected override void ConstructProcess()
        {
        }

        protected virtual string DecodeMti(ByteBuffer buffer)
        {
            return Encoding.UTF8.GetString(buffer.ReadBytes(4));
        }

        protected virtual byte[] DecodeBitmap(ByteBuffer buffer)
        {
            return buffer.ReadBytes(8);
        }

        protected void UnpackDataElement(byte[] bitmap, Core core, ByteBuffer record, int elementOffset)
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
                    currentField = FIELDMethods.Val(DefaultBitmapLength - bitSetIndex + elementOffset);
                    var spec = (DataElementSpec)this.Spec(currentField);
                    core.Field(spec.Field(), spec.Unpack(record));
                    this.Logger.Debug(() =>
                        string.Format("FIELD[{0}]:{1}", FIELDMethods.Val(currentField), core.Field(currentField)));
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

            if (core.Field(FIELD.F_1) != null && record.ReadRemainLength() > 0)
            {
                this.UnpackDataElement((byte[])core.Field(FIELD.F_1), core, record, 64);
            }
        }

        private static void UpdateCoreShell(Core core, Iso8583 iso8583)
        {
            var csc = core as CoreV2003;

            if (csc != null)
            {
                csc.CurrentIso8583 = iso8583;
            }
        }
    }
}
