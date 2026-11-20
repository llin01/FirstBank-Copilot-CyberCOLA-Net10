namespace Com.Cybersoft4u.TM.ISO8583
{
    public interface Iso8583Header
    {
        string Encode();

        Iso8583Header Decode(string header);
    }
}
