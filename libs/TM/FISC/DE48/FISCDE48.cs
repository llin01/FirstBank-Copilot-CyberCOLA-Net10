using Com.Cybersoft4u.Utils;

namespace Com.Cybersoft4u.TM.FISC.DE48
{
    #region DEPENDENCY

    using System;

    using System.Collections.Generic;

    using System.Text;

    #endregion

    public class FISCDE48
    {
        public Dictionary<string, FISCDE48SE> Tags = new Dictionary<string, FISCDE48SE>();

        public String Encode()
        {
            var stringBuilder = new StringBuilder();
            foreach (var tag in this.Tags)
            {
                stringBuilder.Append(tag.Value.Encode());
            }

            return stringBuilder.ToString();
        }

        public FISCDE48 Decode(string data)
        {
            while (data.Length > 0)
            {
                var tag = FISCDE48SE.Generate(data);
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
