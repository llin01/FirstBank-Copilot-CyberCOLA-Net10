// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="Dataset.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the Dataset type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.TLV
{
    #region DEPENDENCY

    using System.Collections.Generic;

    using Com.Cybersoft4u.CyberUtils.Utils;

    #endregion

    public class Dataset
    {
        public string Id;

        public Dictionary<string, Tag> Tags = new Dictionary<string, Tag>();

        public Dataset()
        {
        }

        public Dataset(byte[] data)
        {
            this.Decode(new ByteBuffer(data));
        }

        public ByteBuffer Encode()
        {
            var data = new ByteBuffer();
            foreach (var tag in this.Tags)
            {
                data.WriteByteBuffer(tag.Value.Encode());
            }

            var rtn = new ByteBuffer();
            rtn.WriteBytes(this.Id.GetHexBytes()).WriteUnsignedShort((ushort)data.Val().Length).WriteByteBuffer(data);
            return rtn;
        }

        public Dataset Decode(ByteBuffer data)
        {
            this.Id = data.ReadBytes(1).GetHexString();
            var length = data.ReadUnsignedShort();
            var tmpTags = new ByteBuffer(data.ReadBytes(length));
            while (tmpTags.ReadRemainLength() > 0)
            {
                var tag = new Tag().Decode(tmpTags);
                this.Tags[tag.Id] = tag;
            }

            return this;
        }
    }
}
