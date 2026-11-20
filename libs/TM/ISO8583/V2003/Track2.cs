namespace Com.Cybersoft4u.TM.ISO8583.V2003
{
    #region DEPENDENCY

    using System.Text;
    using System.Text.RegularExpressions;
    using Com.Cybersoft4u.CyberUtils.Exception;
    using Com.Cybersoft4u.CyberUtils.Utils;
    using Com.Cybersoft4u.TM.ISO8583.Category.CardType;

    #endregion

    public class Track2
    {
        public const char DefaultSeparator = '=';

        private CardType cardType = CardType.Default;

        private string cardNo;

        static Track2()
        {
            if (Default.Val("AE_SERVICE_CODE") == null)
            {
                Default.Val("AE_SERVICE_CODE", "101");
            }
        }

        public Track2(char separator)
        {
            this.Separator = separator;
        }

        public Track2(string data, char separator)
            : this(separator)
        {
            this.Decode(data);
        }

        public string ExpireDate { get; set; }

        public string AeEffectiveDate { get; set; }

        public string AeDiscretionaryData { get; set; }

        public string AeInterchangeDesignator { get; set; }

        public string AeZero { get; set; }

        public string AeLanguage { get; set; }

        public string ServiceCode { get; set; }

        public string Pvki { get; set; }

        public string Pvv { get; set; }

        public string Cvv { get; set; }

        public string CardNo
        {
            get
            {
                return this.cardNo;
            }

            set
            {
                this.cardNo = value;
                this.ConvertCardNoAndSetCardType();
            }
        }

        public char Separator { get; set; }

        public CardType CardType
        {
            get
            {
                return this.cardType;
            }

            set
            {
                this.cardType = value;
            }
        }

        public override string ToString()
        {
            return this.Encode();
        }

        public string Encode()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(this.CardNo).Append(this.Separator).Append(this.ExpireDate);
            if (this.CardType.Equals(CardType.Default))
            {
                if (this.AeZero == null)
                {
                    // AE ANSI X4.16
                    stringBuilder.Append(this.AeEffectiveDate).Append(this.AeDiscretionaryData);
                }
                else
                {
                    // AE ISO7813
                    stringBuilder.Append(this.AeInterchangeDesignator).Append(
                        this.ServiceCode.GetWithDefault(Default.Val("AE_SERVICE_CODE"))).Append(this.AeEffectiveDate).
                        Append(this.AeDiscretionaryData).Append(this.AeZero).Append(this.AeLanguage);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(this.ServiceCode))
                {
                    return stringBuilder.ToString();
                }

                stringBuilder.Append(this.ServiceCode);
                if (this.CardNo.StartsWith("9000") && this.ServiceCode.Equals("701"))
                {
                    // UCARD
                    stringBuilder.Append(this.Pvki).Append(this.Pvv).Append(this.Cvv);
                }
                else
                {
                    // VISA, MASTER, JCB
                    stringBuilder.Append(
                        string.IsNullOrEmpty(this.Pvki) && string.IsNullOrEmpty(this.Pvv)
                            ? "00000"
                            : this.Pvki + this.Pvv);
                    if (!string.IsNullOrEmpty(this.Cvv))
                    {
                        stringBuilder.Append(this.Cvv);
                    }
                }
            }

            return stringBuilder.ToString();
        }

        public void Decode(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            var dataSplit = data.Split(this.Separator);
            if (dataSplit.Length == 1)
            {
                throw new InvalidValueException("Track2 did not contain separator");
            }

            this.CardNo = dataSplit[0];
            var val = dataSplit[1];
            if (this.CardNo.Length == 16)
            {
                Run.IgnoreException(() => this.ExpireDate = val.IndexSubstring(0, 4));
                Run.IgnoreException(() => this.ServiceCode = val.GetIndexSubstring(4, 7));
                Run.IgnoreException(() => this.Pvki = val.GetIndexSubstring(7, 8, "0"));
                Run.IgnoreException(() => this.Pvv = val.GetIndexSubstring(8, 12, "0000"));
                Run.IgnoreException(() => this.Cvv = val.IndexSubstring(12, 15));

                if (this.CardNo.StartsWith("4000") && this.ServiceCode == "701")
                {
                    this.CardNo = "9000" + this.CardNo.GetIndexSubstring(4, 15) + "0";
                }

                this.ConvertCardNoAndSetCardType();
            }

            if (this.CardNo.Length != 15)
            {
                return;
            }

            if (val.Length == 13)
            {
                // AE ANSI X4.16
                Run.IgnoreException(() => this.ExpireDate = val.IndexSubstring(0, 4));
                Run.IgnoreException(() => this.AeEffectiveDate = val.IndexSubstring(4, 8));
                Run.IgnoreException(() => this.AeDiscretionaryData = val.IndexSubstring(8, 13));
            }
            else
            {
                // AE ISO7813
                Run.IgnoreException(() => this.ExpireDate = val.IndexSubstring(0, 4));
                Run.IgnoreException(() => this.AeInterchangeDesignator = val.IndexSubstring(4, 5));
                Run.IgnoreException(() => this.ServiceCode = val.IndexSubstring(5, 7));
                Run.IgnoreException(() => this.AeEffectiveDate = val.IndexSubstring(7, 11));
                Run.IgnoreException(() => this.AeDiscretionaryData = val.IndexSubstring(11, 16));
                Run.IgnoreException(() => this.AeZero = val.IndexSubstring(16, 19));
                Run.IgnoreException(() => this.AeLanguage = val.IndexSubstring(19, 21));
            }
        }

        private void ConvertCardNoAndSetCardType()
        {
            //switch (this.CardNo[0])
            //{
            //    case '3':
            //        this.CardType = CardType.Get<JCB>();
            //        break;
            //    case '4':
            //        if ("701".Equals(this.ServiceCode) && this.CardNo.StartsWith("4000"))
            //        {
            //            this.cardType = CardType.Get<UCard>();
            //            this.CardNo = "9000" + this.CardNo.Substring(4);
            //            break;
            //        }
            //
            //        this.CardType = CardType.Get<VISA>();
            //        break;
            //    case '5':
            //        this.CardType = CardType.Get<MasterCard>();
            //        break;
            //    case '9':
            //        this.CardType = CardType.Get<UCard>();
            //        break;
            //}
            if (Regex.Match(this.CardNo, @"^(?:2131|1800|35\d{3})\d{11}$").Success)
            {
                this.CardType = CardType.Get<JCB>();
            }
            else if (Regex.Match(this.CardNo, @"^4[0-9]{12}(?:[0-9]{3})?$").Success)
            {
                if ("701".Equals(this.ServiceCode) && this.CardNo.StartsWith("4000"))
                {
                    this.cardType = CardType.Get<UCard>();
                    this.CardNo = "9000" + this.CardNo.Substring(4);
                }
                else
                {
                    this.CardType = CardType.Get<VISA>();
                }
            }
            else if (Regex.Match(this.CardNo, @"^(?:5[1-5][0-9]{2}|222[1-9]|22[3-9][0-9]|2[3-6][0-9]{2}|27[01][0-9]|2720)[0-9]{12}$").Success)
            {
                this.CardType = CardType.Get<MasterCard>();
            }
            else if (this.CardNo.StartsWith("9"))
            {
                this.CardType = CardType.Get<UCard>();
            }
        }
    }
}
