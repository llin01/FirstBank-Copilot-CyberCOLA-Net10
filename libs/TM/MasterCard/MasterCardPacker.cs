namespace Com.Cybersoft4u.TM.MasterCard
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;
    using Com.Cybersoft4u.TM.ISO8583.V1987;
    using Com.Cybersoft4u.TM.ISO8583.Exception;

    using DataElementSpec = ISO8583.V1987.DataElementSpec;

    #endregion

    public class MasterCardPacker : Packer
    {
        string Packer.Unpack(Iso8583 iso8583, DataElementSpec spec, string record)
        {
            return Unpack(iso8583, spec, record);
        }

        string Packer.Pack(string data, DataElementSpec spec)
        {
            return Pack(data, spec);
        }

        public static string Pack(string data, DataElementSpec spec)
        {
            string rtn = null;
            if (!DEType.LL.Equals(spec.Type()) && !DEType.LLL.Equals(spec.Type()) && !DEType.LLLL.Equals(spec.Type()))
            {
                rtn = data;
            }
            else
            {
                var dataLength = Bytes.From(data).Length();
                int lenLength;
                if (DEType.LL.Equals(spec.Type()))
                {
                    lenLength = 2;
                }
                else if (DEType.LLL.Equals(spec.Type()))
                {
                    lenLength = 3;
                }
                else
                {
                    lenLength = 4;
                }
                rtn = dataLength.ToString().PadLeft(lenLength, '0') + data;
            }

            if (DEType.B.Equals(spec.Type()))
            {
                return rtn;
            }

            return EbcdicConverter.FromString(rtn);
        }

        private static string Unpack(Iso8583 iso8583, DataElementSpec spec, string record)
        {
            string rtn = null;
            try
            {
                if (!DEType.LL.Equals(spec.Type()) && !DEType.LLL.Equals(spec.Type()) && !DEType.LLLL.Equals(spec.Type()))
                {
                    var dataLength = spec.MaxLength() * 2;
                    var dataString = string.Empty;
                    if (DEType.B.Equals(spec.Type()))
                    {
                        dataString = record.IndexSubstring(0, dataLength);
                    }
                    else
                    {
                        dataString = EbcdicConverter.GetString(record.IndexSubstring(0, dataLength));
                    }

                    iso8583.Val(spec.Field(), dataString);
                    rtn = record.Substring(dataLength);
                }
                else
                {
                    int lenLength;
                    if (DEType.LL.Equals(spec.Type()))
                    {
                        lenLength = 2;
                    }
                    else if (DEType.LLL.Equals(spec.Type()))
                    {
                        lenLength = 3;
                    }
                    else
                    {
                        lenLength = 4;
                    }

                    lenLength = lenLength * 2;

                    var dataLengthString = EbcdicConverter.GetString(record.IndexSubstring(0, lenLength));
                    var dataLength = int.Parse(dataLengthString) * 2;
                    record = record.Substring(lenLength);

                    var dataString = EbcdicConverter.GetString(record.IndexSubstring(0, dataLength));
                    iso8583.Val(spec.Field(), dataString);
                    rtn = record.Substring(dataLength);
                }
            }
            catch (Exception)
            {
                throw new FieldDecodeException(
                    spec.Field(),
                    string.Format("Field[{0}] decode failed.", FIELDMethods.Val(spec.Field())));
            }

            return rtn;
        }
    }
}
