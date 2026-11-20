namespace Com.Cybersoft4u.TM.ISO8583
{
    /// <summary>
    /// Base class for ISO8583 data element specifications.
    /// </summary>
    public abstract class DataElementSpec
    {
        /// <summary>The field identifier.</summary>
        protected FIELD field;

        /// <summary>The maximum length of the field.</summary>
        protected int maxLength;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataElementSpec"/> class.
        /// </summary>
        /// <param name="field">The field identifier.</param>
        /// <param name="maxLength">The maximum length of the field.</param>
        protected DataElementSpec(FIELD field, int maxLength)
        {
            this.field = field;
            this.maxLength = maxLength;
        }

        /// <summary>
        /// Gets the field identifier.
        /// </summary>
        /// <returns>The field identifier.</returns>
        public FIELD Field()
        {
            return this.field;
        }

        /// <summary>
        /// Gets the maximum length of the field.
        /// </summary>
        /// <returns>The maximum length.</returns>
        public int MaxLength()
        {
            return this.maxLength;
        }
    }
}
