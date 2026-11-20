namespace Com.Cybersoft4u.TM.ISO8583.V2003
{
    using Com.Cybersoft4u.CyberUtils.Utils;

    public abstract class DataElementSpec : ISO8583.DataElementSpec
    {
        protected Packer Packer;

        private readonly Format format;

        private readonly Representation representation;

        protected DataElementSpec(
            FIELD field,
            int maxLength,
            Format format,
            Representation representation,
            Packer packer)
            : base(field, maxLength)
        {
            this.format = format;
            this.representation = representation;
            this.Packer = packer;
        }

        public Format Format()
        {
            return this.format;
        }

        public Representation Representation()
        {
            return this.representation;
        }

        public abstract object Unpack(ByteBuffer record);

        public abstract ByteBuffer Pack(object data);
    }
}
