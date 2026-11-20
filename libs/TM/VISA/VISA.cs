// --------------------------------------------------------------------------------------------------------------------
// <copyright file="VISA.cs" company="Cybersoft">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the VISA type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.VISA
{
    #region DEPENDENCY

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.Category.Transaction;
    using Com.Cybersoft4u.TM.ISO8583.Exception;
    using Com.Cybersoft4u.TM.ISO8583.TLV;

    #endregion

    public class VISA : AbstractIso8583<VISACore>, ITransaction
    {
        private static readonly IDictionary<FIELD, VISAField> DataElementSpecs = new Dictionary<FIELD, VISAField>();

        private static readonly Packer Packers = new DefaultPacker();

        static VISA()
        {
            DataElementSpecs.Add(FIELD.F_2, new VISAField(FIELD.F_2, 11, Format.Variable1, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_3, new VISAField(FIELD.F_3, 3, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_4, new VISAField(FIELD.F_4, 6, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_5, new VISAField(FIELD.F_5, 6, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_6, new VISAField(FIELD.F_6, 6, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_7, new VISAField(FIELD.F_7, 5, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_8, new VISAField(FIELD.F_8, 4, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_9, new VISAField(FIELD.F_9, 4, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_10, new VISAField(FIELD.F_10, 4, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_11, new VISAField(FIELD.F_11, 3, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_12, new VISAField(FIELD.F_12, 3, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_13, new VISAField(FIELD.F_13, 2, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_14, new VISAField(FIELD.F_14, 2, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_15, new VISAField(FIELD.F_15, 2, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_16, new VISAField(FIELD.F_16, 2, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_17, new VISAField(FIELD.F_17, 2, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_18, new VISAField(FIELD.F_18, 2, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_19, new VISAField(FIELD.F_19, 2, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_20, new VISAField(FIELD.F_20, 2, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_21, new VISAField(FIELD.F_21, 2, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_22, new VISAField(FIELD.F_22, 2, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_23, new VISAField(FIELD.F_23, 2, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_24, new VISAField(FIELD.F_24, 2, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_25, new VISAField(FIELD.F_25, 1, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_26, new VISAField(FIELD.F_26, 1, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_27, new VISAField(FIELD.F_27, 1, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_28, new VISAField(FIELD.F_28, 9, Format.Fix, Representation.AN, Packers));
            DataElementSpecs.Add(FIELD.F_29, new VISAField(FIELD.F_29, 9, Format.Fix, Representation.AN, Packers));
            DataElementSpecs.Add(FIELD.F_30, new VISAField(FIELD.F_30, 9, Format.Fix, Representation.AN, Packers));
            DataElementSpecs.Add(FIELD.F_31, new VISAField(FIELD.F_31, 9, Format.Fix, Representation.AN, Packers));
            DataElementSpecs.Add(
                FIELD.F_32,
                new VISAField(FIELD.F_32, 7, Format.Variable1, Representation.BCD, Packers));
            DataElementSpecs.Add(
                FIELD.F_33,
                new VISAField(FIELD.F_33, 7, Format.Variable1, Representation.BCD, Packers));
            DataElementSpecs.Add(
                FIELD.F_34,
                new VISAField(FIELD.F_34, 29, Format.Variable1, Representation.ANS, Packers));
            DataElementSpecs.Add(
                FIELD.F_35,
                new VISAField(FIELD.F_35, 20, Format.Variable1, Representation.BCD, Packers));
            DataElementSpecs.Add(
                FIELD.F_36,
                new VISAField(FIELD.F_36, 53, Format.Variable1, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_37, new VISAField(FIELD.F_37, 12, Format.Fix, Representation.AN, Packers));
            DataElementSpecs.Add(FIELD.F_38, new VISAField(FIELD.F_38, 6, Format.Fix, Representation.AN, Packers));
            DataElementSpecs.Add(FIELD.F_39, new VISAField(FIELD.F_39, 2, Format.Fix, Representation.AN, Packers));
            DataElementSpecs.Add(FIELD.F_40, new VISAField(FIELD.F_40, 3, Format.Fix, Representation.ANS, Packers));
            DataElementSpecs.Add(FIELD.F_41, new VISAField(FIELD.F_41, 8, Format.Fix, Representation.ANS, Packers));
            DataElementSpecs.Add(FIELD.F_42, new VISAField(FIELD.F_42, 15, Format.Fix, Representation.ANS, Packers));
            DataElementSpecs.Add(FIELD.F_43, new VISAField(FIELD.F_43, 40, Format.Fix, Representation.ANS, Packers));
            DataElementSpecs.Add(
                FIELD.F_44,
                new VISAField(FIELD.F_44, 26, Format.Variable1, Representation.ANS, Packers));
            DataElementSpecs.Add(
                FIELD.F_45,
                new VISAField(FIELD.F_45, 77, Format.Variable1, Representation.ANS, Packers));
            DataElementSpecs.Add(
                FIELD.F_46,
                new VISAField(FIELD.F_46, 256, Format.Variable1, Representation.ANS, Packers));
            DataElementSpecs.Add(
                FIELD.F_47,
                new VISAField(FIELD.F_47, 256, Format.Variable1, Representation.ANS, Packers));
            DataElementSpecs.Add(
                FIELD.F_48,
                new VISAField(FIELD.F_48, 256, Format.Variable1, Representation.B, Packers));
            DataElementSpecs.Add(FIELD.F_49, new VISAField(FIELD.F_49, 2, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_50, new VISAField(FIELD.F_50, 2, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_51, new VISAField(FIELD.F_51, 2, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_52, new VISAField(FIELD.F_52, 8, Format.Fix, Representation.B, Packers));
            DataElementSpecs.Add(FIELD.F_53, new VISAField(FIELD.F_53, 8, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(
                FIELD.F_54,
                new VISAField(FIELD.F_54, 121, Format.Variable1, Representation.ANS, Packers));
            DataElementSpecs.Add(
                FIELD.F_55,
                new VISAField(FIELD.F_55, 256, Format.Variable1, Representation.B, Packers));
            DataElementSpecs.Add(
                FIELD.F_56,
                new VISAField(FIELD.F_56, 256, Format.Variable1, Representation.B, Packers));
            DataElementSpecs.Add(
                FIELD.F_57,
                new VISAField(FIELD.F_57, 256, Format.Variable1, Representation.ANS, Packers));
            DataElementSpecs.Add(
                FIELD.F_58,
                new VISAField(FIELD.F_58, 256, Format.Variable1, Representation.ANS, Packers));
            DataElementSpecs.Add(
                FIELD.F_59,
                new VISAField(FIELD.F_59, 15, Format.Variable1, Representation.ANS, Packers));
            DataElementSpecs.Add(
                FIELD.F_60,
                new VISAField(FIELD.F_60, 7, Format.Variable1, Representation.NBCD, Packers));
            DataElementSpecs.Add(
                FIELD.F_61,
                new VISAField(FIELD.F_61, 19, Format.Variable1, Representation.NBCD, Packers));
            DataElementSpecs.Add(
                FIELD.F_62,
                new VISAField(FIELD.F_62, 256, Format.Variable1, Representation.B, Packers));
            DataElementSpecs.Add(
                FIELD.F_63,
                new VISAField(FIELD.F_63, 256, Format.Variable1, Representation.B, Packers));
            DataElementSpecs.Add(FIELD.F_64, new VISAField(FIELD.F_64, 2, Format.Fix, Representation.B, Packers));
            DataElementSpecs.Add(FIELD.F_66, new VISAField(FIELD.F_66, 1, Format.Fix, Representation.N, Packers));
            DataElementSpecs.Add(FIELD.F_67, new VISAField(FIELD.F_67, 1, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_68, new VISAField(FIELD.F_68, 2, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_69, new VISAField(FIELD.F_69, 3, Format.Fix, Representation.AN, Packers));
            DataElementSpecs.Add(FIELD.F_70, new VISAField(FIELD.F_70, 2, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_71, new VISAField(FIELD.F_71, 2, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_72, new VISAField(FIELD.F_72, 2, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_73, new VISAField(FIELD.F_73, 3, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_74, new VISAField(FIELD.F_74, 5, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_75, new VISAField(FIELD.F_75, 5, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_76, new VISAField(FIELD.F_76, 5, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_77, new VISAField(FIELD.F_77, 5, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_78, new VISAField(FIELD.F_78, 5, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_79, new VISAField(FIELD.F_79, 5, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_80, new VISAField(FIELD.F_80, 5, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_81, new VISAField(FIELD.F_81, 5, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_82, new VISAField(FIELD.F_82, 6, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_83, new VISAField(FIELD.F_83, 6, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_84, new VISAField(FIELD.F_84, 6, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_85, new VISAField(FIELD.F_85, 6, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_86, new VISAField(FIELD.F_86, 8, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_87, new VISAField(FIELD.F_87, 8, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_88, new VISAField(FIELD.F_88, 8, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_89, new VISAField(FIELD.F_89, 8, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_90, new VISAField(FIELD.F_90, 21, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_91, new VISAField(FIELD.F_91, 1, Format.Fix, Representation.AN, Packers));
            DataElementSpecs.Add(FIELD.F_92, new VISAField(FIELD.F_92, 2, Format.Fix, Representation.AN, Packers));
            DataElementSpecs.Add(FIELD.F_93, new VISAField(FIELD.F_93, 5, Format.Fix, Representation.AN, Packers));
            DataElementSpecs.Add(FIELD.F_94, new VISAField(FIELD.F_94, 7, Format.Fix, Representation.AN, Packers));
            DataElementSpecs.Add(FIELD.F_95, new VISAField(FIELD.F_95, 42, Format.Fix, Representation.AN, Packers));
            DataElementSpecs.Add(FIELD.F_96, new VISAField(FIELD.F_96, 8, Format.Fix, Representation.B, Packers));
            DataElementSpecs.Add(FIELD.F_97, new VISAField(FIELD.F_97, 17, Format.Fix, Representation.AN, Packers));
            DataElementSpecs.Add(FIELD.F_98, new VISAField(FIELD.F_98, 25, Format.Fix, Representation.AN, Packers));
            DataElementSpecs.Add(
                FIELD.F_99,
                new VISAField(FIELD.F_99, 7, Format.Variable1, Representation.NBCD, Packers));
            DataElementSpecs.Add(
                FIELD.F_100,
                new VISAField(FIELD.F_100, 7, Format.Variable1, Representation.NBCD, Packers));
            DataElementSpecs.Add(
                FIELD.F_101,
                new VISAField(FIELD.F_101, 18, Format.Variable1, Representation.ANS, Packers));
            DataElementSpecs.Add(
                FIELD.F_102,
                new VISAField(FIELD.F_102, 29, Format.Variable1, Representation.ANS, Packers));
            DataElementSpecs.Add(
                FIELD.F_103,
                new VISAField(FIELD.F_103, 29, Format.Variable1, Representation.ANS, Packers));
            DataElementSpecs.Add(
                FIELD.F_104,
                new VISAField(FIELD.F_104, 256, Format.Variable1, Representation.B, Packers));
            DataElementSpecs.Add(
                FIELD.F_105,
                new VISAField(FIELD.F_105, 256, Format.Variable1, Representation.ANS, Packers));
            DataElementSpecs.Add(
                FIELD.F_106,
                new VISAField(FIELD.F_106, 256, Format.Variable1, Representation.ANS, Packers));
            DataElementSpecs.Add(
                FIELD.F_107,
                new VISAField(FIELD.F_107, 256, Format.Variable1, Representation.ANS, Packers));
            DataElementSpecs.Add(
                FIELD.F_108,
                new VISAField(FIELD.F_108, 256, Format.Variable1, Representation.ANS, Packers));
            DataElementSpecs.Add(
                FIELD.F_109,
                new VISAField(FIELD.F_109, 256, Format.Variable1, Representation.ANS, Packers));
            DataElementSpecs.Add(
                FIELD.F_110,
                new VISAField(FIELD.F_110, 256, Format.Variable1, Representation.ANS, Packers));
            DataElementSpecs.Add(
                FIELD.F_111,
                new VISAField(FIELD.F_111, 256, Format.Variable1, Representation.ANS, Packers));
            DataElementSpecs.Add(
                FIELD.F_112,
                new VISAField(FIELD.F_112, 256, Format.Variable1, Representation.ANS, Packers));
            DataElementSpecs.Add(
                FIELD.F_113,
                new VISAField(FIELD.F_113, 256, Format.Variable1, Representation.ANS, Packers));
            DataElementSpecs.Add(
                FIELD.F_114,
                new VISAField(FIELD.F_114, 256, Format.Variable1, Representation.ANS, Packers));
            DataElementSpecs.Add(
                FIELD.F_115,
                new VISAField(FIELD.F_115, 25, Format.Variable1, Representation.ANS, Packers));
            DataElementSpecs.Add(
                FIELD.F_116,
                new VISAField(FIELD.F_116, 256, Format.Variable1, Representation.B, Packers));
            DataElementSpecs.Add(
                FIELD.F_117,
                new VISAField(FIELD.F_117, 256, Format.Variable1, Representation.ANS, Packers));
            DataElementSpecs.Add(
                FIELD.F_118,
                new VISAField(FIELD.F_118, 256, Format.Variable1, Representation.ANS, Packers));
            DataElementSpecs.Add(
                FIELD.F_121,
                new VISAField(FIELD.F_121, 12, Format.Variable1, Representation.AN, Packers));
            DataElementSpecs.Add(
             FIELD.F_120,
             new VISAField(FIELD.F_120, 256, Format.Variable1, Representation.B, Packers));
            DataElementSpecs.Add(
                FIELD.F_123,
                new VISAField(FIELD.F_123, 256, Format.Variable1, Representation.B, Packers));
            DataElementSpecs.Add(
                FIELD.F_125,
                new VISAField(FIELD.F_125, 256, Format.Variable1, Representation.B, Packers));
            DataElementSpecs.Add(
                FIELD.F_126,
                new VISAField(FIELD.F_126, 256, Format.Variable1, Representation.B, Packers));
            DataElementSpecs.Add(
                FIELD.F_127,
                new VISAField(FIELD.F_127, 256, Format.Variable2, Representation.B, Packers));
            DataElementSpecs.Add(FIELD.F_130, new VISAField(FIELD.F_130, 3, Format.Fix, Representation.B, Packers));
            DataElementSpecs.Add(FIELD.F_131, new VISAField(FIELD.F_131, 5, Format.Fix, Representation.B, Packers));
            DataElementSpecs.Add(FIELD.F_132, new VISAField(FIELD.F_132, 4, Format.Fix, Representation.B, Packers));
            DataElementSpecs.Add(FIELD.F_133, new VISAField(FIELD.F_133, 8, Format.Fix, Representation.ANS, Packers));
            DataElementSpecs.Add(
                FIELD.F_134,
                new VISAField(FIELD.F_134, 256, Format.Variable1, Representation.B, Packers));
            DataElementSpecs.Add(
                FIELD.F_135,
                new VISAField(FIELD.F_135, 16, Format.Variable1, Representation.B, Packers));
            DataElementSpecs.Add(FIELD.F_136, new VISAField(FIELD.F_136, 8, Format.Fix, Representation.B, Packers));
            DataElementSpecs.Add(FIELD.F_137, new VISAField(FIELD.F_137, 2, Format.Fix, Representation.B, Packers));
            DataElementSpecs.Add(FIELD.F_138, new VISAField(FIELD.F_138, 2, Format.Fix, Representation.B, Packers));
            DataElementSpecs.Add(FIELD.F_139, new VISAField(FIELD.F_139, 10, Format.Fix, Representation.B, Packers));
            DataElementSpecs.Add(
                FIELD.F_140,
                new VISAField(FIELD.F_140, 256, Format.Variable1, Representation.B, Packers));
            DataElementSpecs.Add(
                FIELD.F_142,
                new VISAField(FIELD.F_142, 256, Format.Variable1, Representation.B, Packers));
            DataElementSpecs.Add(
                FIELD.F_143,
                new VISAField(FIELD.F_143, 21, Format.Variable1, Representation.B, Packers));
            DataElementSpecs.Add(FIELD.F_144, new VISAField(FIELD.F_144, 1, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_145, new VISAField(FIELD.F_145, 2, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_146, new VISAField(FIELD.F_146, 3, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_147, new VISAField(FIELD.F_147, 6, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_148, new VISAField(FIELD.F_148, 2, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_149, new VISAField(FIELD.F_149, 6, Format.Fix, Representation.BCD, Packers));
            DataElementSpecs.Add(FIELD.F_152, new VISAField(FIELD.F_152, 8, Format.Fix, Representation.B, Packers));
        }

        public VISA()
        {
            this.Header(new VISAHeader());
        }

        public VISA(byte[] message)
        {
            this.Decode(message);
        }

        public VISAHeader Header(VISAHeader header = null)
        {
            return header == null ? this.Core<VISACore>().Header() : this.Core<VISACore>().Header(header);
        }

        public VISAHeader RejectHeader(VISAHeader header = null)
        {
            return header == null ? this.Core<VISACore>().RejectHeader() : this.Core<VISACore>().RejectHeader(header);
        }

        public void RemoveRejectHeader()
        {
            this.Core<VISACore>().RemoveRejectHeader();
        }

        public override byte[] Bitmap()
        {
            byte[] data = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            if (this.HasSecondBitmap())
            {
                data[0] |= 0x80;
            }

            var core = this.Core<VISACore>();

            foreach (var pair in
                core.OrderedFields().Where(pair => FIELDMethods.Val(pair.Key) < 65 && pair.Value != null))
            {
                Mark(data, FIELDMethods.Val(pair.Key));
            }

            return data;
        }

        public bool HasSecondBitmap()
        {
            return this.Core<VISACore>().HasSecondBitmap();
        }

        public byte[] SecondBitmap()
        {
            return this.Core<VISACore>().SecondBitmap();
        }

        public bool HasThirdBitmap()
        {
            return this.Core<VISACore>().HasThirdBitmap();
        }

        public byte[] ThirdBitmap()
        {
            return this.Core<VISACore>().ThirdBitmap();
        }

        public override Bytes Encode()
        {
            var core = this.Core<VISACore>();
            var content = new ByteBuffer();

            this.Logger.Debug(() => "Encoding...");
            content.WriteBytes(this.Mti().GetHexBytes());
            this.Logger.Debug(() => string.Format("MTI         :{0}", this.Mti()));
            content.WriteBytes(this.Bitmap());
            this.Logger.Debug(() => string.Format("Bitmap 1    :{0}", HexStringConverter.FromByteArray(this.Bitmap())));
            if (this.HasSecondBitmap())
            {
                content.WriteBytes(this.SecondBitmap());
                this.Logger.Debug(() => string.Format("Bitmap 2    :{0}", HexStringConverter.FromByteArray(this.SecondBitmap())));
                if (this.HasThirdBitmap())
                {
                    content.WriteBytes(this.ThirdBitmap());
                    this.Logger.Debug(() => string.Format("Bitmap 3    :{0}", HexStringConverter.FromByteArray(this.ThirdBitmap())));
                }
            }

            foreach (var pair in core.OrderedFields().Where(pair => pair.Value != null))
            {
                var key = pair.Key;
                var value = core.Field(key);
                var fieldData = ((VISAField)this.Spec(key)).Pack(value);
                content.WriteByteBuffer(fieldData);
                this.Logger.Debug(
                    () => string.Format(
                        "{0}:{1}",
                        string.Format("FIELD[{0}]", key).PadRight(12, ' '),
                        value is byte[]? HexStringConverter.FromByteArray((byte[])value) : value));
            }

            var header = new ByteBuffer();
            if (this.Header() != null)
            {
                var tmpLength = (ushort)this.Header().Encode().Val().Length;
                tmpLength += (ushort)content.Val().Length;
                this.Header().TotalLength = tmpLength;
                header.WriteBytes(this.Header().Encode().Val());
            }

            var reject = new ByteBuffer();
            if (this.RejectHeader() != null)
            {
                var tmpLength = (ushort)this.RejectHeader().Encode().Val().Length;
                this.RejectHeader().TotalLength = (ushort)(tmpLength + this.Header().TotalLength);
                reject.WriteBytes(this.RejectHeader().Encode().Val());
            }

            var rtn = new ByteBuffer();
            rtn.WriteByteBuffer(reject).WriteByteBuffer(header).WriteByteBuffer(content);

            this.Logger.Debug(() => "Encoded.");

            ListSpecialField();

            return new Bytes(rtn.Val());
        }

        public override ICodecable Decode(byte[] message)
        {
            try
            {
                this.Logger.Debug(() => string.Format("Decoding... Hex String: {0}", HexStringConverter.FromByteArray(message)));
                var data = new ByteBuffer(message);
                data.MarkReadIndex();
                short length = data.ReadByte();
                data.ResetReadIndex();
                var header = new VISAHeader(data.ReadBytes(length));
                if (header.HeaderLength > 22)
                {
                    this.RejectHeader(header);
                    data.MarkReadIndex();
                    length = data.ReadByte();
                    data.ResetReadIndex();
                    header = new VISAHeader(data.ReadBytes(length));
                    this.Header(header);
                }
                else
                {
                    this.Header(header);
                }

                this.Mti(HexStringConverter.FromByteArray(data.ReadBytes(2)));
                this.Logger.Debug(() => string.Format("MTI         :{0}", this.Mti()));
                var bitmap = this.DecodeBitmap(data);
                this.Logger.Debug(() => string.Format("Bitmap 1    :{0}", HexStringConverter.FromByteArray(bitmap)));
                byte[] secondBitmap = null;
                byte[] thirdBitmap = null;
                if ((bitmap[0] & 0x80) == 0x80)
                {
                    secondBitmap = this.DecodeBitmap(data);
                    this.Logger.Debug(() => string.Format("Bitmap 2    :{0}", HexStringConverter.FromByteArray(secondBitmap)));
                    if ((secondBitmap[0] & 0x80) == 0x80)
                    {
                        thirdBitmap = this.DecodeBitmap(data);
                        this.Logger.Debug(() => string.Format("Bitmap 3    :{0}", HexStringConverter.FromByteArray(thirdBitmap)));
                    }
                }

                this.UnpackDataElement(bitmap, this.Core(), data, 0);

                if (secondBitmap != null && data.ReadRemainLength() > 0)
                {
                    this.UnpackDataElement(secondBitmap, this.Core(), data, 64);
                }

                if (thirdBitmap != null && data.ReadRemainLength() > 0)
                {
                    this.UnpackDataElement(thirdBitmap, this.Core(), data, 128);
                }

                this.Logger.Debug(() => "Decoded.");

                ListSpecialField();

            }
            catch (FieldDecodeException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new DecodeFailException(e);
            }

            return this;
        }

        public override Iso8583 Prepare()
        {
            return this;
        }

        public override string Identity()
        {
            return this.Mti();
        }

        public override DataElementSpec Spec(FIELD field)
        {
            VISAField spec;
            DataElementSpecs.TryGetValue(field, out spec);
            return spec;
        }

        public string Name()
        {
            return this.GetType().FullName;
        }

        protected override void ConstructProcess()
        {
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
                    if (currentField.Equals(FIELD.F_1) || currentField.Equals(FIELD.F_65))
                    {
                        continue;
                    }

                    var spec = (VISAField)this.Spec(currentField);
                    if (ObjectUtils.IsNull(spec))
                    {
                        continue;
                    }

                    core.Field(spec.Field(), spec.Unpack(record));
                    this.Logger.Debug(
                        () =>
                            {
                                var value = core.Field(currentField);
                                return string.Format(
                                    "{0}:{1}",
                                    string.Format("FIELD[{0}]", FIELDMethods.Val(currentField)).PadRight(12, ' '),
                                    value is byte[]? HexStringConverter.FromByteArray((byte[])value) : value);
                            });
                }
                catch (FieldDecodeException)
                {
                    throw;
                }
                catch (Exception)
                {
                    throw new FieldDecodeException(
                        currentField,
                        string.Format("Field[{0}] decode failed.", currentField));
                }
            }
        }

        private void ListSpecialField()
        {
            TLV TLVData = null;

            if (this.RawVal(FIELD.F_55) is byte[])
            {
                TLVData = new TLV().Decode((byte[])this.RawVal(FIELD.F_55));
                ShowFieldsVisaTLV(FIELD.F_55, TLVData);
            }

            if (this.RawVal(FIELD.F_56) is byte[])
            {
                TLVData = new TLV().Decode((byte[])this.RawVal(FIELD.F_56));
                ShowFieldsVisaTLV(FIELD.F_56, TLVData);
            }

            if (this.RawVal(FIELD.F_123) is byte[])
            {
                TLVData = new TLV().Decode((byte[])this.RawVal(FIELD.F_123));
                ShowFieldsVisaTLV(FIELD.F_123, TLVData);
            }

            if (this.RawVal(FIELD.F_125) is byte[])
            {
                TLVData = new TLV().Decode((byte[])this.RawVal(FIELD.F_125));
                ShowFieldsVisaTLV(FIELD.F_125, TLVData);
            }
        }

        private void ShowFieldsVisaTLV(FIELD field, Com.Cybersoft4u.TM.ISO8583.TLV.TLV TLVData)
        {
            if (!ObjectUtils.IsNull(TLVData))
            {
                try
                {
                    foreach (KeyValuePair<string, Dataset> dataset in TLVData.Datasets)
                    {
                        foreach (KeyValuePair<string, Tag> tag in dataset.Value.Tags)
                        {
                            this.Logger.Debug(() => string.Format("TlvFiller(): VISA Field {0} Dataset [{1}] Tag [{2}] Data [{3}]", field, dataset.Key, tag.Key, tag.Value.Value.GetHexString()));
                        }
                    }
                }
                catch (System.Exception)
                {
                    // Ignore
                }
            }
        }

    }
}
