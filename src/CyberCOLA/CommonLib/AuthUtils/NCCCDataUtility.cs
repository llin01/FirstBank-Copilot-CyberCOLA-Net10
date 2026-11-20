using Com.Cybersoft4u.CommonLib.Resource;
using NLog;

namespace Com.Cybersoft4u.CommonLib.AuthUtils
{
    public static class NCCCDataUtility
    {
        #region NLog
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        #endregion //NLog
        private static readonly SystemParameterInfo SystemParameterInfo = SingletonOfSystemParameterInfo.Instance.GetSystemParameterInfo();
    }
}
