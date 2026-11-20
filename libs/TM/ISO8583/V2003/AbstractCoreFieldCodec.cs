namespace Com.Cybersoft4u.TM.ISO8583.V2003
{
    using System;

    public class AbstractCoreFieldCodec : CoreFieldCodec
    {
        public virtual void Encode(Core core, object data)
        {
        }

        public virtual object Decode(Core core)
        {
            throw new NotImplementedException();
        }
    }
}
