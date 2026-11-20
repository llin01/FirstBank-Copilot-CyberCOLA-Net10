// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="VISAField.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the VISAField type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.VISA
{
    #region DEPENDENCY

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583;

    #endregion

    public class VISAField : DataElementSpec
    {
        private readonly Format format;

        private readonly Packer packer;

        private readonly Representation representation;

        public VISAField(FIELD field, int maxLength, Format format, Representation representation, Packer packer)
            : base(field, maxLength)
        {
            this.format = format;
            this.representation = representation;
            this.packer = packer;
        }

        public Packer Packer()
        {
            return this.packer;
        }

        public Format Format()
        {
            return this.format;
        }

        public Representation Representation()
        {
            return this.representation;
        }

        public virtual object Unpack(ByteBuffer record)
        {
            return this.packer.Unpack(this, record);
        }

        public virtual ByteBuffer Pack(object data)
        {
            return this.packer.Pack(data, this);
        }
    }
}
