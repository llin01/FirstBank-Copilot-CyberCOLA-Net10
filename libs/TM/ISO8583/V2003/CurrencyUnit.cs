namespace Com.Cybersoft4u.TM.ISO8583.V2003
{
    using Com.Cybersoft4u.CyberUtils.Utils;

    #region DEPENDENCY



    #endregion

    public class CurrencyUnit : Stringable
    {
        public const string DefaultCurrencyCode = "901";

        protected string currencyCode;

        protected string minorUnit;

        protected string value;

        public CurrencyUnit(string value = null)
        {
            if (value == null)
            {
                return;
            }

            this.CurrencyCode = value.Substring(0, 3);
            this.MinorUnit = value.Substring(3, 1);
            this.Value = value.Substring(4, 12);
        }

        public virtual string CurrencyCode
        {
            get
            {
                return this.currencyCode;
            }

            set
            {
                ObjectUtils.CheckLength(value, 3, "CurrencyCode");
                NumberUtils.CheckDigits(value);
                this.currencyCode = value;
            }
        }

        public virtual string MinorUnit
        {
            get
            {
                return this.minorUnit;
            }

            set
            {
                ObjectUtils.CheckLength(value, 1, "MinorUnit");
                NumberUtils.CheckDigits(value);
                this.minorUnit = value;
            }
        }

        public virtual string Value
        {
            get
            {
                return this.value;
            }

            set
            {
                ObjectUtils.CheckLength(value, 12, "Value");
                NumberUtils.CheckDigits(value);
                this.value = value;
            }
        }

        public override string ToString()
        {
            return this.CurrencyCode + this.MinorUnit + this.Value;
        }
    }
}
