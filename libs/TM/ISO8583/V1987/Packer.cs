namespace Com.Cybersoft4u.TM.ISO8583.V1987
{
    public interface Packer : IPacker
    {
        string Pack(string data, DataElementSpec spec);

        string Unpack(Iso8583 iso8583, DataElementSpec spec, string record);
    }
}
