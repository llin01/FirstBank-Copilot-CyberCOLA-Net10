namespace Com.Cybersoft4u.TM.ISO8583.V2003
{
    using Com.Cybersoft4u.CyberUtils.Utils;

    #region DEPENDENCY



    #endregion

    public abstract class CoreV2003 : Core
    {
        internal Iso8583 CurrentIso8583;

        public virtual T CurrentShell<T>() where T : Iso8583
        {
            return (T)this.CurrentIso8583;
        }

        public ByteBuffer Encode()
        {
            if (this.CurrentIso8583 is CoreCodecable)
            {
                return ((CoreCodecable)this.CurrentIso8583).CoreEncode(this);
            }

            return new ByteBuffer(this.CurrentIso8583.Core(this).Encode().Val());
        }

        public abstract string Identity();
    }
}
