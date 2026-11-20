namespace Com.Cybersoft4u.TM.ISO8583.V2003
{
    using Com.Cybersoft4u.CyberUtils.Utils;

    public interface Packer : IPacker
    {
        ByteBuffer Pack(object data, DataElementSpec spec);

        object Unpack(DataElementSpec spec, ByteBuffer record);

    }
}
