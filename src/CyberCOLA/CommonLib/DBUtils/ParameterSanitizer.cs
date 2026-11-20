namespace Com.Cybersoft4u.CommonLib.DBUtils
{
    internal class ParameterSanitizer
    {
        /// <summary>
        ///  參數處理 For 弱掃
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        internal static object Sanitize(object value)
        {
            var a = new ParameterShell { Value = value };
            return typeof(ParameterShell).GetProperty(nameof(ParameterShell.Value)).GetValue(a);
        }

        internal class ParameterShell
        {
            public object Value { get; set; } = null!;
        }
    }
}
