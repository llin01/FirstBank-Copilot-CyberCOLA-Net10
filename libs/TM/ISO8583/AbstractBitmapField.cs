// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="AbstractBitmapField.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the AbstractBitmapField type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.ISO8583
{
    #region DEPENDENCY

    using System.Collections.Generic;
    using System.Linq;

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.Utils;

    #endregion

    public abstract class AbstractBitmapField : ICodecable
    {
        private readonly Dictionary<FIELD, object> fields = new Dictionary<FIELD, object>();

        private Logger logger;

        protected AbstractBitmapField(byte bitmapLength)
        {
            this.BitmapLength = bitmapLength;
        }

        protected AbstractBitmapField(byte bitmapLength, byte[] fieldBytes)
        {
            this.BitmapLength = bitmapLength;
            this.Decode(fieldBytes);
        }

        public byte BitmapLength { get; private set; }

        protected Logger Logger
        {
            get
            {
                return this.logger ?? (this.logger = LoggerAdapter.GetLogger(this.GetType().FullName));
            }
        }

        public abstract Bytes Encode();

        public abstract ICodecable Decode(byte[] message);

        public abstract DataElementSpec Spec(FIELD field);

        public object Field(FIELD field)
        {
            object v;
            this.fields.TryGetValue(field, out v);
            return v;
        }

        public AbstractBitmapField Field(FIELD field, object value)
        {
            this.fields[field] = value;
            return this;
        }

        public virtual AbstractBitmapField Purge(FIELD field)
        {
            this.fields.Remove(field);
            return this;
        }

        public AbstractBitmapField PurgeFields()
        {
            this.fields.Clear();
            return this;
        }

        public IEnumerable<KeyValuePair<FIELD, object>> OrderedFields()
        {
            return this.fields.OrderBy(p => p.Key);
        }
    }
}
