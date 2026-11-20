namespace Com.Cybersoft4u.TM.FISC.DE48
{
    #region DEPENDENCY

    #endregion

    public class FISCDE48SEDefault : FISCDE48SE
    {
        public FISCDE48SEDefault()
            : base("DF")
        {
        }

        public override string ValueEncode()
        {
            return this.Value;
        }

        public override FISCDE48SE ValueDecode()
        {
            return this;
        }
    }
}
