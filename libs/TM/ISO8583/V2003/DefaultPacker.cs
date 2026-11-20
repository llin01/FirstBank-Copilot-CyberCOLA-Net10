namespace Com.Cybersoft4u.TM.ISO8583.V2003
{
    #region DEPENDENCY

    using System;
    using System.Text;

    using Com.Cybersoft4u.CyberUtils.Exception;
    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.Exception;

    #endregion

    public class DefaultPacker : Packer
    {
        public ByteBuffer Pack(object data, DataElementSpec spec)
        {
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
                case Format.LLVAR:
                    rtn.WriteBytes(HexStringConverter.ToByteArray(Convert.ToString(dataLength).PadLeft(2, '0')));
                    break;
                case Format.LLLVAR:
                case Format.LLLLVAR:
                    rtn.WriteBytes(HexStringConverter.ToByteArray(Convert.ToString(dataLength).PadLeft(4, '0')));
                    break;
            }

            // Check Data Length is small than spec
            if (dataLength > spec.MaxLength())
            {
                throw new InvalidValueException(
                    string.Format(
                        "FIELD [{0}] length is larger[{1}] than spec[{2}].",
                        spec.Field(),
                        dataLength,
                        spec.MaxLength()));
            }

            try
            {
                // Write Data
                switch (spec.Representation())
                {
                    case Representation.A:
                    case Representation.AN:
                    case Representation.ANP:
                    case Representation.ANS:
                    case Representation.AS:
                    case Representation.N:
                    case Representation.NS:
                    case Representation.P:
                    case Representation.S:
                    case Representation.XN:
                    case Representation.Z:
                        return rtn.WriteBytes(Encoding.UTF8.GetBytes(data.ToString()));
                    case Representation.ANB:
                    case Representation.ANSB:
                    case Representation.B:
                        return rtn.WriteBytes((byte[])data);
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
        }

        public object Unpack(DataElementSpec spec, ByteBuffer record)
        {
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
                length = GetByteLength(spec, record);
                data = record.ReadBytes(length);
                switch (spec.Representation())
                {
                    case Representation.A:
                    case Representation.AN:
                    case Representation.ANP:
                    case Representation.ANS:
                    case Representation.AS:
                    case Representation.N:
                    case Representation.NS:
                    case Representation.P:
                    case Representation.S:
                    case Representation.XN:
                    case Representation.Z:
                        return Encoding.UTF8.GetString(data);
                    case Representation.ANB:
                    case Representation.ANSB:
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
                    string.Format(Format, FIELDMethods.Val(spec.Field()), spec.Representation(), spec.Format(), spec.MaxLength(), data, length));
            }
        }

        private static int GetDataLength(object data, DataElementSpec spec)
        {
            switch (spec.Representation())
            {
                case Representation.A:
                case Representation.AN:
                case Representation.ANP:
                case Representation.ANS:
                case Representation.AS:
                case Representation.N:
                case Representation.NS:
                case Representation.P:
                case Representation.S:
                case Representation.XN:
                case Representation.Z:
                    return data.ToString().Length;
                case Representation.ANB:
                case Representation.ANSB:
                case Representation.B:
                    return ((byte[])data).Length;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static int GetByteLength(DataElementSpec spec, ByteBuffer record)
        {
            switch (spec.Format())
            {
                case Format.LLVAR:
                    return Convert.ToInt16(HexStringConverter.FromByteArray(record.ReadBytes(1)));
                case Format.LLLVAR:
                case Format.LLLLVAR:
                    return Convert.ToInt32(HexStringConverter.FromByteArray(record.ReadBytes(2)));
                default:
                    return spec.MaxLength();
            }
        }
    }
}
