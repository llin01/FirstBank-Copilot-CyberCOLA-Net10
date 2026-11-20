namespace Com.Cybersoft4u.CyberUtils.Utils
{
    using System;

    public interface ICodecable
    {
        Bytes Encode();

        ICodecable Decode(byte[] message);

        ICodecable Decode(ReadOnlySpan<byte> message) => Decode(message.ToArray());
    }
}
