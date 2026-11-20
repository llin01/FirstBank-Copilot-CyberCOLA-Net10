// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="DefaultPacker.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the DefaultPacker type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.VISA
{
    #region DEPENDENCY

    using System;
    using System.Text;

    using Com.Cybersoft4u.CyberUtils.Exception;
    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.Exception;

    #endregion

    public class DefaultPacker : Packer
    {
        public static readonly DefaultPacker Default = new DefaultPacker();

        public static readonly Encoding EBCDIC = Encoding.GetEncoding("IBM037");

        public static readonly Encoding EBCDICKatakana = Encoding.GetEncoding("IBM290");

        public ByteBuffer Pack(object data, VISAField spec)
        {
            //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (spec == null)
            {
                throw new ArgumentNullException("spec");
            }

            var rtn = new ByteBuffer();
            var dataLength = GetDataLength(data, spec);

            // Write Length
            switch (spec.Format())
            {
                case Format.Variable1:
                case Format.Variable2:
                    switch (spec.Representation())
                    {
                        case Representation.AN:
                        case Representation.ANS:
                        case Representation.B:
                        case Representation.N:
                        case Representation.BCD:
                            rtn.WriteByte((byte)dataLength);
                            break;
                        case Representation.NBCD:
                            rtn.WriteByte((byte)((dataLength / 2) + (dataLength % 2)));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    break;
            }

            try
            {
                // Write Data
                switch (spec.Representation())
                {
                    case Representation.AN:
                    case Representation.ANS:
                    case Representation.N:
                        rtn.WriteBytes(FIELD.F_118.Equals(spec.Field()) ? EBCDICKatakana.GetBytes((string)data) : EBCDIC.GetBytes((string)data));
                        break;
                    case Representation.NBCD:
                    case Representation.BCD:
                        var writeData = (string)data;
                        if (writeData.Length % 2 == 1)
                        {
                            writeData = "0" + writeData;
                        }

                        rtn.WriteBytes(HexStringConverter.ToByteArray(writeData));
                        break;
                    case Representation.B:
                        rtn.WriteBytes((byte[])data);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception)
            {
                throw new FieldEncodeException(
                    spec.Field(),
                    string.Format("Field[{0}] Can't convert to byte", spec.Field()));
            }

            return rtn;
        }

        public object Unpack(VISAField spec, ByteBuffer record)
        {
            //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            if (spec == null)
            {
                throw new InvalidValueException("spec");
            }

            if (record == null)
            {
                throw new InvalidValueException("record");
            }

            var length = 0;
            var data = new byte[] { };

            try
            {
                length = GetDeDataLength(spec, record);
                var byteLength = (length / 2) + (length % 2);
                data = record.ReadBytes(byteLength);
                switch (spec.Representation())
                {
                    case Representation.AN:
                    case Representation.ANS:
                    case Representation.N:
                        return FIELD.F_118.Equals(spec.Field()) ? EBCDICKatakana.GetString(data) : EBCDIC.GetString(data);
                    case Representation.NBCD:
                    case Representation.BCD:
                        return HexStringConverter.FromByteArray(data).Substring(length % 2, length);
                    case Representation.B:
                        return data;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception)
            {
                const string Format = "Field[{0}] decode failed.\n SPEC:\n  Representation: {1}\n  Format: {2}\n  MaxLength: {3}\n Content:\n  Data:{4}\n  Length:{5}";

                throw new FieldDecodeException(
                    spec.Field(),
                    string.Format(
                        Format,
                        FIELDMethods.Val(spec.Field()),
                        spec.Representation(),
                        spec.Format(),
                        spec.MaxLength(),
                        data,
                        length));
            }
        }

        private static int GetDataLength(object data, VISAField spec)
        {
            switch (spec.Representation())
            {
                case Representation.AN:
                case Representation.ANS:
                    return ((string)data).Length;
                case Representation.N:
                    return ((string)data).Length;
                case Representation.NBCD:
                case Representation.BCD:
                    return ((string)data).Length;
                case Representation.B:
                    return ((byte[])data).Length;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static int GetDeDataLength(VISAField spec, ByteBuffer record)
        {
            switch (spec.Format())
            {
                case Format.Fix:
                    return spec.MaxLength() * 2;
                case Format.Variable1:
                case Format.Variable2:
                    if (spec.Representation().Equals(Representation.B)
                        || spec.Representation().Equals(Representation.AN)
                        || spec.Representation().Equals(Representation.ANS)
                        || spec.Representation().Equals(Representation.N))
                    {
                        return record.ReadByte() * 2;
                    }

                    if (spec.Representation().Equals(Representation.NBCD))
                    {
                        return record.ReadByte() * 2;
                    }
                    return record.ReadByte();
                default:
                    return spec.MaxLength() * 2;
            }
        }
    }
}
