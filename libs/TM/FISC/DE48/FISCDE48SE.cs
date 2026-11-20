namespace Com.Cybersoft4u.TM.FISC.DE48
{
    #region DEPENDENCY

    using System;

    using System.Collections.Generic;

    using System.Linq;

    using System.Linq.Expressions;

    using System.Text;

    using Com.Cybersoft4u.CyberUtils.Utils;

    #endregion

    public abstract class FISCDE48SE
    {
        public string Id;

        public string Value;

        private static readonly Dictionary<string, Type> SETypes = new Dictionary<string, Type>();
        internal protected readonly Dictionary<string, FISCDE48SESF> Subfields = new Dictionary<string, FISCDE48SESF>();

        static FISCDE48SE()
        {
            Init();
        }

        public FISCDE48SE()
        {
        }

        public FISCDE48SE(string id)
        {
            this.Id = id;
        }

        public static FISCDE48SE Generate(string data)
        {
            var id = data.Substring(0, 2);
            Type type;
            if (!SETypes.TryGetValue(id, out type))
            {
                return new FISCDE48SEDefault().Decode(data);
            }

            var se = Expression.Lambda<Func<FISCDE48SE>>(Expression.New(type)).Compile()();
            se.Decode(data);
            return se;
        }

        public static void Init()
        {
            AssemblyUtils.GetAllAssemblies()
                .ToList()
                .ForEach(
                    assembly =>
                    {
                        assembly.GetTypes()
                            .Where(type => type.IsClass && !type.IsAbstract && type.IsSubclassOf(typeof(FISCDE48SE)))
                            .ToList()
                            .ForEach(type => SETypes[((FISCDE48SE)Activator.CreateInstance(type)).Id] = type);
                    });
        }

        public string Encode()
        {
            this.Value = this.ValueEncode();

            var stringBuilder = new StringBuilder();

            stringBuilder.Append(this.Id);
            stringBuilder.Append(this.LengthOfValue().ToString().PadLeft(2, '0'));
            stringBuilder.Append(this.Value);

            return stringBuilder.ToString();
        }

        public FISCDE48SE Decode(string data)
        {
            this.Id = data.GetIndexSubstring(0, 2);
            var length = int.Parse(data.GetIndexSubstring(2, 4));
            this.Value = data.Substring(4, length);

            return this.ValueDecode();
        }

        public virtual string ValueEncode()
        {
            var stringBuilder = new StringBuilder();

            foreach (var pair in this.Subfields)
            {
                var sf = pair.Value;
                stringBuilder.Append(sf.Encode());
            }

            return stringBuilder.ToString();
        }

        public virtual FISCDE48SE ValueDecode()
        {
            var val = this.Value;

            while (val.Length >= 2)
            {
                var sfId = val.IndexSubstring(0, 2);
                var sf = this.Subfields[sfId];
                sf.Decode(val);
                val = val.Substring(sf.LengthOfSESF());
            }

            return this;
        }


        protected string SubfieldLLEncode(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "00";
            }
            else
            {
                var length = value.Length.ToString().PadLeft(2, '0');
                return length + value;
            }
        }

        protected string SubfieldLLDecode(string data, int index, out int nextIndex)
        {
            var length = int.Parse(data.Substring(index, 2));
            nextIndex = index + 2 + length;

            return data.Substring(index + 2).IndexSubstring(0, length);
        }

        public int LengthOfValue()
        {
            return this.Value.Length;
        }

        public int LengthOfSE()
        {
            return this.LengthOfValue() + 4;
        }
    }
}
