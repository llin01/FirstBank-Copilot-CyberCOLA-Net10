namespace Com.Cybersoft4u.TM.MasterCard.DE56
{
    #region DEPENDENCY

    #endregion

    public class MasterCardDE56SEDefault : MasterCardDE56SE
    {
        public MasterCardDE56SEDefault()
            : base("DF")
        {
        }

        public override string ValueEncode()
        {
            return this.Value;
        }

        public override MasterCardDE56SE ValueDecode()
        {
            return this;
        }
    }
}
