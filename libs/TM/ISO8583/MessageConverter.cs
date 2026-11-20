// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="MessageConverter.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the MessageConverter type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------
using Com.Cybersoft4u.Utils;

namespace Com.Cybersoft4u.TM.ISO8583
{
    public abstract class MessageConverter<TOrigin, TTarget>
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

        public abstract TTarget ToTarget(TOrigin origin);

        public abstract TOrigin ToOrigin(TTarget target);
    }
}
