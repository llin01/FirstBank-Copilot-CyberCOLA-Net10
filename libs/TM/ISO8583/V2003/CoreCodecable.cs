namespace Com.Cybersoft4u.TM.ISO8583.V2003
{
    using Com.Cybersoft4u.CyberUtils.Utils;

    #region DEPENDENCY



    #endregion

    public interface CoreCodecable
    {
        ByteBuffer CoreEncode(Core core);

        Core CoreDecode(byte[] message);

        void CorePrepare(Core core);
    }
}
