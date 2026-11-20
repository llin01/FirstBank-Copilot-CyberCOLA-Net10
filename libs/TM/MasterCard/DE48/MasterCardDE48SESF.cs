namespace Com.Cybersoft4u.TM.MasterCard.DE48
{
    #region DEPENDENCY

    using System.Text;

    using Com.Cybersoft4u.CyberUtils.Utils;

    using Com.Cybersoft4u.TM.ISO8583.V1987;

    #endregion

    public class MasterCardDE48SESF
    {
        public readonly string Id;

        public string Value;

        public int MaxLength;

        public readonly DEType Type;

        public MasterCardDE48SESF(string id, int maxLength, DEType type)
        {
            this.Id = id;
            this.MaxLength = maxLength;
            this.Type = type;
        }

        public string Encode()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append(this.Id);

            switch (this.Type)
            {
                case DEType.LL:
                    stringBuilder.Append(this.LengthOfValue().ToString().PadLeft(2, '0'));
                    break;
                case DEType.LLL:
                    stringBuilder.Append(this.LengthOfValue().ToString().PadLeft(3, '0'));
                    break;
            }

            stringBuilder.Append(this.Value);

            return stringBuilder.ToString();
        }

        public MasterCardDE48SESF Decode(string data)
        {
            var length = 0;

            switch (this.Type)
            {
                case DEType.LL:
                    length = int.Parse(data.GetIndexSubstring(2, 4));
                    this.Value = data.Substring(4, length);
                    break;
                case DEType.LLL:
                    length = int.Parse(data.GetIndexSubstring(2, 5));
                    this.Value = data.Substring(5, length);
                    break;
                default:
                    this.Value = data.IndexSubstring(0, MaxLength);
                    break;
            }

            return this;
        }

        public int LengthOfValue()
        {
            if (!string.IsNullOrEmpty(this.Value))
            {
                return this.Value.Length;
            }
            else
            {
                return 0;
            }
        }

        public int LengthOfSESF()
        {
            var idLen = 0;
            var lengthLen = 0;

            if (!string.IsNullOrEmpty(this.Id))
            {
                idLen = this.Id.Length;
            }

            switch (this.Type)
            {
                case DEType.LL:
                    lengthLen = 2;
                    break;
                case DEType.LLL:
                    lengthLen = 3;
                    break;
            }

            return idLen + lengthLen + this.LengthOfValue();
        }
    }
}
