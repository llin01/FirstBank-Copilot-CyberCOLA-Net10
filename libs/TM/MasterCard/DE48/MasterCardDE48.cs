using Com.Cybersoft4u.Utils;

namespace Com.Cybersoft4u.TM.MasterCard.DE48
{
    #region DEPENDENCY

    using System;

    using System.Collections.Generic;

    using System.Text;

    #endregion

    public class MasterCardDE48
    {
        public Dictionary<string, MasterCardDE48SE> Tags = new Dictionary<string, MasterCardDE48SE>();

        public String Encode()
        {
            var stringBuilder = new StringBuilder();
            foreach (var tag in this.Tags)
            {
                stringBuilder.Append(tag.Value.Encode());
            }

            return stringBuilder.ToString();
        }

        public MasterCardDE48 Decode(string data)
        {
            while (data.Length > 0)
            {
                var tag = MasterCardDE48SE.Generate(data);
                this.Tags[tag.Id] = tag;

                data = data.Substring(tag.LengthOfSE());
            }

            return this;
        }

        public void ShowTags()
        {
            foreach (var tag in this.Tags)
            {
                //Console.WriteLine(tag.Value.Encode());
                LoggerAdapter.GetLogger(this.GetType().FullName).Info($"[Tag{tag.Key}] = [{tag.Value.Encode()}]");
            }
        }
    }
}
