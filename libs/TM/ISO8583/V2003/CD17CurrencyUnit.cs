namespace Com.Cybersoft4u.TM.ISO8583.V2003
{
    using Com.Cybersoft4u.CyberUtils.Exception;
    using Com.Cybersoft4u.CyberUtils.Utils;

    public class CD17CurrencyUnit : CD13CurrencyUnit
    {

        public CD17CurrencyUnit(string value = null)
        {
            if (value == null)
            {
                return;
            }

            this.CD = value.Substring(0, 1);
            this.CurrencyCode = value.Substring(1, 3);
            this.MinorUnit = value.Substring(4, 1);
            this.Value = value.Substring(5, 12);
        }

        public override string CurrencyCode
        {
            get
            {
                return base.CurrencyCode;
            }

            set
            {
                ObjectUtils.CheckLength(value, 3, "CurrencyCode");
                NumberUtils.CheckDigits(value);
                this.currencyCode = value;
            }
        }

        public override string MinorUnit
        {
            get
            {
                return base.MinorUnit;
            }

            set
            {
                ObjectUtils.CheckLength(value, 1, "MinorUnit");
                NumberUtils.CheckDigits(value);
                this.minorUnit = value;
            }
        }

        public override string Value
        {
            get
            {
                return base.Value;
            }

            set
            {
                ObjectUtils.CheckLength(value, 12, "Value");
                NumberUtils.CheckDigits(value);
                this.value = value;
            }
        }

        /// <summary>
        /// C for credit, D for debit
        /// </summary>
        public override string CD
        {
            get
            {
                return base.CD;
            }

            set
            {
                var tmp = value.ToLower();
                ObjectUtils.CheckLength(tmp, 1, "CD");
                if (tmp != "c" && tmp != "d")
                {
                    throw new InvalidValueException("CD");
                }

                this.cd = tmp;
            }
        }
    }
}
