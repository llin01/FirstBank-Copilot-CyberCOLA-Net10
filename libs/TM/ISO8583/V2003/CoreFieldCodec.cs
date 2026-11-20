namespace Com.Cybersoft4u.TM.ISO8583.V2003
{
    public interface CoreFieldCodec
    {
        void Encode(Core core, object data);

        object Decode(Core core);
    }
}
