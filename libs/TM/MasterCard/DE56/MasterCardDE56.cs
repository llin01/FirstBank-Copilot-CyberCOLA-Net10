using Com.Cybersoft4u.Utils;

namespace Com.Cybersoft4u.TM.MasterCard.DE56
{
    #region DEPENDENCY

    using System;

    using System.Collections.Generic;

    using System.Text;

    #endregion

    public class MasterCardDE56
    {
        public Dictionary<string, MasterCardDE56SE> Tags = new Dictionary<string, MasterCardDE56SE>();

        public String Encode()
        {
            var stringBuilder = new StringBuilder();
            foreach (var tag in this.Tags)
            {
                stringBuilder.Append(tag.Value.Encode());
            }

            return stringBuilder.ToString();
        }

        public MasterCardDE56 Decode(string data)
        {
            while (data.Length > 0)
            {
                var tag = MasterCardDE56SE.Generate(data);
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
