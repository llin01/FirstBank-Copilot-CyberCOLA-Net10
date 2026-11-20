namespace Com.Cybersoft4u.TM.ISO8583.V2003
{
    public enum Format
    {
        /// <summary>
        /// Unspecified
        /// </summary>
        Unspecified,

        /// <summary>
        /// 00~99 Length
        /// </summary>
        LLVAR,

        /// <summary>
        /// 000~999 Length
        /// </summary>
        LLLVAR,

        /// <summary>
        /// 0000~9999 Length
        /// </summary>
        LLLLVAR,

        /// <summary>
        /// 0000~9999 years, 01~12 month, 01~31 day
        /// </summary>
        CCYYMMDD,

        /// <summary>
        /// two digits hours and two digits minutes
        /// </summary>
        HHMM,

        /// <summary>
        /// Daily, Weekly, Monthly
        /// </summary>
        DWM,

        /// <summary>
        /// Space
        /// </summary>
        Space,

        /// <summary>
        /// Kilo, Miles
        /// </summary>
        KM,

        /// <summary>
        /// Yes, No
        /// </summary>
        YN,

        MMDDhhmmss,

        MMDD,

        /// <summary>
        /// Last two digits of years, 01~12 month
        /// </summary>
        YYMM,

        CCYYMM,

        /// <summary>
        /// Credit, Debit
        /// </summary>
        CD,

        CCYYMMDDhhmmss,

        /// <summary>
        ///     A.M., P.M.
        /// </summary>
        AP
    }
}
