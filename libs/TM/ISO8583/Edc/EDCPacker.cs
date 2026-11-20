namespace Com.Cybersoft4u.TM.ISO8583.Edc
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.Exception;
    using Com.Cybersoft4u.TM.ISO8583.V1987;

    #endregion

    public class EdcPacker : Packer
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
            string rtn;
            if (!DEType.LL.Equals(spec.Type()) && !DEType.LLL.Equals(spec.Type()) && !DEType.LLLL.Equals(spec.Type()))
            {
                if (DEType.N.Equals(spec.Type()) || DEType.B.Equals(spec.Type()))
                {
                    rtn = data;
                }
                else
                {
                    rtn = Bytes.From(data).ToHexString();
                }
            }
            else
            {
                var dataLength = Bytes.From(data).Length();
                int lenLength;
                if (DEType.LL.Equals(spec.Type()))
                {
                    lenLength = 2;
                }
                else
                {
                    lenLength = 4;
                }

                rtn = dataLength.ToString().PadLeft(lenLength, '0') + Bytes.From(data).ToHexString();
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
                    var tmpData = record.IndexSubstring(0, spec.MaxLength());
                    if (DEType.N.Equals(spec.Type()) || DEType.B.Equals(spec.Type()))
                    {
                        iso8583.Val(spec.Field(), tmpData);
                    }
                    else
                    {
                        var bytes = Bytes.FromHexString(tmpData);
                        iso8583.Val(spec.Field(), bytes.ToString());
                    }

                    rtn = record.Substring(spec.MaxLength());
                }
                else
                {
                    int lenLength;
                    if (DEType.LL.Equals(spec.Type()))
                    {
                        lenLength = 2;
                    }
                    else
                    {
                        lenLength = 4;
                    }

                    var dataLength = int.Parse(record.IndexSubstring(0, lenLength)) * 2;
                    record = record.Substring(lenLength);
                    var bytes = Bytes.FromHexString(record.IndexSubstring(0, dataLength));
                    iso8583.Val(spec.Field(), bytes.ToString());
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
