namespace Com.Cybersoft4u.TM.ISO8583.ActionCode.Network
{
    public class ChannelAlreadyExisted : ActionCode
    {
        public override string V1987String()
        {
            return Default.V1987String();
        }

        public override string V2003String()
        {
            return "8800";
        }
    }
}
