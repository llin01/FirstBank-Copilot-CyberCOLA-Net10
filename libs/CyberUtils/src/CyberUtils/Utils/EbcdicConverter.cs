namespace Com.Cybersoft4u.CyberUtils.Utils
{
    #region DEPENDENCY

    using System.Text;

    #endregion

    public class EbcdicConverter
    {
        public static string GetString(string ebcdicHexString)
        {
            //start program must be register
            //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var ebcdicByteArray = HexStringConverter.ToByteArray(ebcdicHexString);
            return Encoding.GetEncoding("IBM037").GetString(ebcdicByteArray);
        }

        public static string FromString(string str)
        {
            //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var ebcdicByteArray = Encoding.GetEncoding("IBM037").GetBytes(str);
            return HexStringConverter.FromByteArray(ebcdicByteArray);
        }
    }
}
