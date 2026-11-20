namespace Com.Cybersoft4u.TM.ISO8583.Category.CardType
{
    public class VISA : CardType
    {
        public override string Id()
        {
            return "04";
        }

        public override string Name()
        {
            return "VISA";
        }
    }
}
