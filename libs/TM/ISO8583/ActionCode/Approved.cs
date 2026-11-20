namespace Com.Cybersoft4u.TM.ISO8583.ActionCode
{
    public class Approved : ActionCode
    {
        public override string V1987String()
        {
            return "00";
        }

        public override string V2003String()
        {
            return "0000";
        }
    }
}
