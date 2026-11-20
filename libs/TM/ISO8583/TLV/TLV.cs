// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="TLV.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the TLV type.
//   this is VISA Version, Dataset ID > 70 should be bitmap format, but it doesn't.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583.TLV
{
    #region DEPENDENCY

    using System.Collections.Generic;

    using Com.Cybersoft4u.CyberUtils.Utils;

    #endregion

    public class TLV
    {
        public Dictionary<string, Dataset> Datasets = new Dictionary<string, Dataset>();

        public TLV()
        {
        }

        public TLV(byte[] data)
        {
            this.Decode(data);
        }

        public ByteBuffer Encode()
        {
            var data = new ByteBuffer();
            foreach (var dataset in this.Datasets)
            {
                data.WriteByteBuffer(dataset.Value.Encode());
            }

            return data;
        }

        public TLV Decode(byte[] data)
        {
            var buffer = new ByteBuffer(data);
            while (buffer.ReadRemainLength() > 0)
            {
                var dataset = new Dataset().Decode(buffer);
                this.Datasets[dataset.Id] = dataset;
            }

            return this;
        }
    }
}
