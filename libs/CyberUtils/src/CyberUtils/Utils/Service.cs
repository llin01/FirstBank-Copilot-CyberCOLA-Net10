namespace Com.Cybersoft4u.CyberUtils.Utils
{
    using System.Collections.Generic;

    public interface Service
    {
        void Start(params object[] pars);

        void Stop(params object[] pars);

        void Restart(params object[] pars);

        void Reload(params object[] pars);

        Dictionary<object, object> Status(params object[] pars);
    }
}
