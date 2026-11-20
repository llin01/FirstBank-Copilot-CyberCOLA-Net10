namespace Com.Cybersoft4u.TM.ISO8583.ActionCode.Error9000
{
    public class SystemError : ActionCode
    {
        public override string V1987String()
        {
            return "96";
        }

        public override string V2003String()
        {
            return "9121";
        }
    }
}
