// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="VISACore.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the VISACore type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.VISA
{
    #region DEPENDENCY

    using System.Linq;

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.CyberUtils.Utils.Category;
    using Com.Cybersoft4u.TM.ISO8583;

    #endregion

    public class VISACore : Core
    {
        public VISAHeader Header(VISAHeader header = null)
        {
            if (header == null)
            {
                return this.Extend<VISAHeaderKey, VISAHeader>();
            }

            this.Extend<VISAHeaderKey>(header);
            return header;
        }

        public VISAHeader RejectHeader(VISAHeader header = null)
        {
            if (header == null)
            {
                return this.Extend<VISARejectHeaderKey, VISAHeader>();
            }

            this.Extend<VISARejectHeaderKey>(header);
            return header;
        }

        public void RemoveRejectHeader()
        {
            this.PurgeExtend(Key.Get<VISARejectHeaderKey>());
        }

        public bool HasSecondBitmap()
        {
            return
                this.OrderedFields()
                    .Any(
                        pair =>
                        FIELDMethods.Val(pair.Key) > 64 && FIELDMethods.Val(pair.Key) < 193 && pair.Value != null);
        }

        public byte[] SecondBitmap()
        {
            byte[] data = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            if (this.HasThirdBitmap())
            {
                data[0] |= 0x80;
            }

            foreach (var pair in
                this.OrderedFields()
                    .Where(
                        pair =>
                        FIELDMethods.Val(pair.Key) > 64 && FIELDMethods.Val(pair.Key) < 129 && pair.Value != null))
            {
                Mark(data, FIELDMethods.Val(pair.Key) - 64);
            }

            return data;
        }

        public bool HasThirdBitmap()
        {
            return
                this.OrderedFields()
                    .Any(
                        pair =>
                        FIELDMethods.Val(pair.Key) > 128 && FIELDMethods.Val(pair.Key) < 193 && pair.Value != null);
        }

        public byte[] ThirdBitmap()
        {
            byte[] data = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

            foreach (var pair in
                this.OrderedFields()
                    .Where(
                        pair =>
                        FIELDMethods.Val(pair.Key) > 128 && FIELDMethods.Val(pair.Key) < 193 && pair.Value != null))
            {
                Mark(data, FIELDMethods.Val(pair.Key) - 128);
            }

            return data;
        }

        public override void Field(FIELD field, object val)
        {
            if (ObjectUtils.IsNull(val))
            {
                return;
            }

            this.FieldStorage[field] = val;
        }

        public override void PurgeField(FIELD field)
        {
            this.FieldStorage.Remove(field);
        }
    }
}
