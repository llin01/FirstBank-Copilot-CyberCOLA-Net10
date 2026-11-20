namespace Com.Cybersoft4u.TM.ISO8583
{
    #region DEPENDENCY

    using System.Collections.Generic;
    using System.Linq;

    using Com.Cybersoft4u.CyberUtils.Exception;
    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.CyberUtils.Utils.Category;

    #endregion

    public abstract class Core
    {
        protected IDictionary<FIELD, object> FieldStorage = new Dictionary<FIELD, object>();

        protected IDictionary<Key, object> ExtendStorage = new Dictionary<Key, object>();

        private string messageTypeIndicator;

        public string MessageTypeIndicator
        {
            get
            {
                return this.messageTypeIndicator;
            }

            set
            {
                if (value == null || value.Length != 4)
                {
                    throw new InvalidValueException("MessageTypeIndicator");
                }

                this.messageTypeIndicator = value;
            }
        }

        public IReadOnlyDictionary<FIELD, object> Fields()
        {
            return (IReadOnlyDictionary<FIELD, object>)this.FieldStorage;
        }

        public IEnumerable<KeyValuePair<FIELD, object>> OrderedFields()
        {
            return this.FieldStorage.OrderBy(p => p.Key);
        }

        public T Field<T>(FIELD field) where T : class
        {
            return this.Field(field) as T;
        }

        public object Field(FIELD field)
        {
            object rtn;
            return !this.FieldStorage.TryGetValue(field, out rtn) ? null : rtn;
        }

        public virtual void Field(FIELD field, object val)
        {
            if (ObjectUtils.IsNull(val))
            {
                return;
            }

            this.FieldStorage[field] = val;
            this.UpdateSecBitmap();
        }

        public virtual void PurgeField(FIELD field)
        {
            this.FieldStorage.Remove(field);
            this.UpdateSecBitmap();
        }

        public void PurgeFields()
        {
            this.FieldStorage.Clear();
        }

        public IReadOnlyDictionary<Key, object> Extends()
        {
            return (IReadOnlyDictionary<Key, object>)this.ExtendStorage;
        }

        /// <summary>
        /// Get Extend data by <code>K</code> Class, and return as <code>R</code> Type.
        /// </summary>
        /// <typeparam name="K">Key Class</typeparam>
        /// <typeparam name="R">Return Type</typeparam>
        /// <returns></returns>
        public R Extend<K, R>() where K : Key
        {
            return (R)this.Extend(Key.Get<K>());
        }

        public object Extend<K>() where K : Key
        {
            return this.Extend(Key.Get<K>());
        }

        public void Extend<K>(object val) where K : Key
        {
            this.Extend(Key.Get<K>(), val);
        }

        public object Extend(Key key)
        {
            object rtn;
            return !this.ExtendStorage.TryGetValue(key, out rtn) ? null : rtn;
        }

        public void Extend(Key key, object val)
        {
            if (val != null)
            {
                this.ExtendStorage[key] = val;
            }
        }

        public void PurgeExtend<K>() where K : Key
        {
            this.ExtendStorage.Remove(Key.Get<K>());
        }

        public void PurgeExtend(Key key)
        {
            this.ExtendStorage.Remove(key);
        }

        public void PurgeExtends()
        {
            this.ExtendStorage.Clear();
        }

        protected static void Mark(IList<byte> bytes, int index)
        {
            var indexShift = index - 1;
            bytes[indexShift / 8] |= (byte)(1 << (7 - (indexShift % 8)));
        }

        protected virtual void UpdateSecBitmap()
        {
            byte[] secMap = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

            foreach (
                var pair in
                    this.Fields().Where(
                        pair =>
                        FIELDMethods.Val(pair.Key) > 64 && FIELDMethods.Val(pair.Key) < 129
                        && pair.Value != null))
            {
                Mark(secMap, FIELDMethods.Val(pair.Key) - 64);
            }

            var hexArray = secMap.Select(b => b.ToString("X2")).ToArray();
            var hexMap = string.Concat(hexArray);
            if (!hexMap.Equals("0000000000000000"))
            {
                this.FieldStorage[FIELD.F_1] = hexMap;
            }
        }
    }
}
