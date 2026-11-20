namespace Com.Cybersoft4u.TM.ISO8583.ActionCode
{
    public class DoNotHonor : ActionCode
    {
        public override string V1987String()
        {
            return "05";
        }

        public override string V2003String()
        {
            return "1000";
        }
    }
}
