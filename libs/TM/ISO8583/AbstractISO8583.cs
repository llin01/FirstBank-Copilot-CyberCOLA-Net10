namespace Com.Cybersoft4u.TM.ISO8583
{
    #region DEPENDENCY

    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.CyberUtils.Utils.Category;
    using Com.Cybersoft4u.Utils;

    #endregion

    public abstract class AbstractIso8583<T> : Iso8583 where T : Core
    {
        private volatile Core Iso8583Core;

        private Logger logger;

        protected AbstractIso8583()
        {
            this.Iso8583Core = (Core)Activator.CreateInstance(typeof(T));
            this.ConstructProcess();
        }

        protected Logger Logger
        {
            get
            {
                return this.logger ?? (this.logger = LoggerAdapter.GetLogger(this.GetType().FullName));
            }
        }

        public abstract Bytes Encode();

        public abstract ICodecable Decode(byte[] message);

        public virtual ICodecable Decode(ReadOnlySpan<byte> message) => this.Decode(message.ToArray());

        public virtual Core Core()
        {
            return this.Iso8583Core;
        }

        public TP Core<TP>() where TP : Core
        {
            return (TP)this.Core();
        }

        public virtual Iso8583 Core(Core core)
        {
            this.Iso8583Core = ObjectUtils.CheckNotNull(core);
            return this;
        }

        public string Mti()
        {
            return this.Core().MessageTypeIndicator;
        }

        public Iso8583 Mti(string mti)
        {
            this.Core().MessageTypeIndicator = mti;
            return this;
        }

        public virtual byte[] Bitmap()
        {
            return this.Bitmap(this.Core());
        }

        public virtual Iso8583 Prepare(Core core)
        {
            this.Core(core);
            this.Prepare();
            return this;
        }

        public virtual TP Prepare<TP>(Core core) where TP : AbstractIso8583<T>
        {
            this.Core(core);
            this.Prepare();
            return (TP)this;
        }

        public abstract Iso8583 Prepare();

        public abstract DataElementSpec Spec(FIELD field);

        public virtual string Val(FIELD field)
        {
            return this.RawVal(field) as string;
        }

        public virtual Iso8583 Val(FIELD field, string value)
        {
            return this.RawVal(field, value);
        }

        public object RawVal(FIELD field)
        {
            object v;
            this.Core().Fields().TryGetValue(field, out v);
            return v;
        }

        public Iso8583 RawVal(FIELD field, object value)
        {
            this.Core().Field(field, value);
            return this;
        }

        public virtual Iso8583 Purge(FIELD field)
        {
            this.Core().PurgeField(field);
            return this;
        }

        public Iso8583 PurgeFields()
        {
            this.Core().PurgeFields();
            return this;
        }

        public IReadOnlyDictionary<Key, object> Extends()
        {
            return this.Iso8583Core.Extends();
        }

        public R Extend<K, R>() where K : Key
        {
            return this.Iso8583Core.Extend<K, R>();
        }

        public object Extend<K>() where K : Key
        {
            return this.Iso8583Core.Extend<K>();
        }

        public void Extend<K>(object val) where K : Key
        {
            this.Iso8583Core.Extend<K>(val);
        }

        public void PurgeExtend<K>() where K : Key
        {
            this.Iso8583Core.PurgeExtend<K>();
        }

        public void PurgeExtends()
        {
            this.Iso8583Core.PurgeExtends();
        }

        public Iso8583 Clear()
        {
            this.Iso8583Core = null;
            return this;
        }

        public abstract string Identity();

        protected static void Mark(IList<byte> bytes, int index)
        {
            var indexShift = index - 1;
            bytes[indexShift / 8] |= (byte)(1 << (7 - (indexShift % 8)));
        }

        protected abstract void ConstructProcess();

        protected byte[] Bitmap(Core core)
        {
            byte[] data = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

            foreach (var pair in core.OrderedFields().Where(pair => FIELDMethods.Val(pair.Key) < 65 && pair.Value != null))
            {
                Mark(data, FIELDMethods.Val(pair.Key));
            }

            return data;
        }

        public void ShowFields()
        {
            //Console.WriteLine(string.Format("MTI {0}", this.Mti()));
            this.logger.Info($"MTI {this.Mti()}");
            foreach (var pair in this.Core().Fields())
            {
                //Console.WriteLine(string.Format("FIELD {0} VALUE [{1}]", pair.Key, pair.Value is byte[] ? HexStringConverter.FromByteArray((byte[])pair.Value) : pair.Value));
                this.logger.Info($"FIELD {pair.Key} VALUE [{(pair.Value is byte[]? HexStringConverter.FromByteArray((byte[])pair.Value) : pair.Value)}]");
            }
        }
    }
}
