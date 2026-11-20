namespace Com.Cybersoft4u.TM.ISO8583.Category.CardType
{
    public class MasterCard : CardType
    {
        public override string Id()
        {
            return "05";
        }

        public override string Name()
        {
            return "MasterCard";
        }
    }
}
