using Com.Cybersoft4u.Utils;

namespace Com.Cybersoft4u.TM.ISO8583
{
    public abstract class MessageCorrector<TOrigin, TTarget>
        where TOrigin : Iso8583 where TTarget : Iso8583
    {
        private Logger logger;

        protected Logger Logger
        {
            get
            {
                return this.logger ??= LoggerAdapter.GetLogger(this.GetType().FullName);
            }
        }

        public abstract void CorrectResponse(TTarget targetResponse, TOrigin originRequest);
    }
}
