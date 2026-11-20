namespace Com.Cybersoft4u.TM.ISO8583.V1987
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.Exception;

    #endregion

    public class DefaultPacker : Packer
    {
        string Packer.Unpack(Iso8583 iso8583, DataElementSpec spec, string record)
        {
            return Unpack(iso8583, spec, record);
        }

        string Packer.Pack(string data, DataElementSpec spec)
        {
            return Pack(data, spec);
        }

        private static string Pack(string data, DataElementSpec spec)
        {
            string rtn = null;
            if (!DEType.LL.Equals(spec.Type()) && !DEType.LLL.Equals(spec.Type()) && !DEType.LLLL.Equals(spec.Type()))
            {
                rtn = data;
            }
            else
            {
                var dataLength = Bytes.From(data.GetAsciiBytes()).Length();
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

            return rtn;
        }

        private static string Unpack(Iso8583 iso8583, DataElementSpec spec, string record)
        {
            string rtn = null;
            try
            {
                if (!DEType.LL.Equals(spec.Type()) && !DEType.LLL.Equals(spec.Type()) && !DEType.LLLL.Equals(spec.Type()))
                {
                    iso8583.Val(spec.Field(), record.IndexSubstring(0, spec.MaxLength()));
                    rtn = record.Substring(spec.MaxLength());
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
                    var dataLength = int.Parse(record.IndexSubstring(0, lenLength));
                    record = record.Substring(lenLength);
                    iso8583.Val(spec.Field(), record.IndexSubstring(0, dataLength));
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
