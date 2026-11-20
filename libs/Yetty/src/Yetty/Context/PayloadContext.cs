namespace Com.Cybersoft4u.Yetty.Context
{
    #region DEPENDENCY

    using System.Collections.Generic;

    #endregion

    public class PayloadContext
    {
        private readonly Dictionary<object, object> attributes;

        public PayloadContext()
        {
            this.attributes = new Dictionary<object, object>();
        }

        public PayloadContext(object val) : this()
        {
            this.Val = val;
        }

        public object Val { get; set; }

        public object Attr(object key)
        {
            return this.attributes[key];
        }

        public PayloadContext Attr(object key, object value)
        {
            this.attributes[key] = value;
            return this;
        }

        public PayloadContext RemoveAttr(object key)
        {
            this.attributes.Remove(key);
            return this;
        }

        public bool Has(object key)
        {
            return this.attributes.ContainsKey(key);
        }
    }
}
