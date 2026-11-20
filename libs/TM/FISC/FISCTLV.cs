
namespace Com.Cybersoft4u.TM.FISC
{
    #region DEPENDENCY

    using System.Collections.Generic;
    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.TLV;
    using Com.Cybersoft4u.Utils;

    #endregion

    public class FISCTLV
    {
        public Dictionary<string, Tag> Tags = new Dictionary<string, Tag>();

        public ByteBuffer Encode()
        {
            var data = new ByteBuffer();
            foreach (var tag in this.Tags)
            {
                data.WriteByteBuffer(tag.Value.Encode());
            }

            return data;
        }

        public FISCTLV Decode(byte[] data)
        {
            var buffer = new ByteBuffer(data);
            while (buffer.ReadRemainLength() > 0)
            {
                var tag = new Tag().Decode(buffer);
                this.Tags[tag.Id] = tag;
            }

            return this;
        }

        public string GetTagHex(string tagId, int padRightLen)
        {
            if (!this.Tags.ContainsKey(tagId))
            {
                return string.Empty.PadRight(padRightLen, '0');
            }

            return HexStringConverter.FromByteArray(this.Tags[tagId].Value).PadRight(padRightLen, '0');
        }

        public void ShowTags()
        {
            foreach (var tag in this.Tags)
            {
                //Console.WriteLine("[{0}] = {1}", tag.Key, HexStringConverter.FromByteArray(tag.Value.Value));
                LoggerAdapter.GetLogger(this.GetType().FullName).Info($"[{tag.Key}] = {HexStringConverter.FromByteArray(tag.Value.Value)}");
            }
        }
    }
}
