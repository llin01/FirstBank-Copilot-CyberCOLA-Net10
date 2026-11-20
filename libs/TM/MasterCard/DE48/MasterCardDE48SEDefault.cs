namespace Com.Cybersoft4u.TM.MasterCard.DE48
{
    #region DEPENDENCY

    #endregion

    public class MasterCardDE48SEDefault : MasterCardDE48SE
    {
        public MasterCardDE48SEDefault()
            : base("DF")
        {
        }

        public override string ValueEncode()
        {
            return this.Value;
        }

        public override MasterCardDE48SE ValueDecode()
        {
            return this;
        }
    }
}
