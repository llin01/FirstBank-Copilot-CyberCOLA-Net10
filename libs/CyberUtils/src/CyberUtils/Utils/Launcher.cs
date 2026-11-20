namespace Com.Cybersoft4u.CyberUtils.Utils
{
    using System.Collections.Generic;

    using Com.Cybersoft4u.CyberUtils.Utils.Checker;

    public abstract class Launcher : Service
    {
        static Launcher()
        {
            var tmp = typeof(CheckAttribute);
        }

        public abstract void Start(params object[] pars);

        public abstract void Stop(params object[] pars);

        public abstract void Restart(params object[] pars);

        public abstract void Reload(params object[] pars);

        public abstract Dictionary<object, object> Status(params object[] pars);
    }
}
