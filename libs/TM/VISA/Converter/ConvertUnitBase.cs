// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="ConvertUnitBase.cs">
//   Author: Kaven
// </copyright>
// <summary>
//   Defines the ConvertUnitBase type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.TM.VISA.Converter
{
    #region DEPENDENCY

    using System;

    using Com.Cybersoft4u.Utils;

    #endregion

    public class ConvertUnitBase
    {
        public static Logger Logger(Type type)
        {
            //return LoggerAdapter.GetLogger(type);
            return LoggerAdapter.GetLogger(type.FullName);
        }

        public static void WriteLogCodecInfo(Type type, string msg)
        {
            Logger(type).Info(() => string.Format(msg));
        }

        public static void WriteLogCodecFail(Type type, string msg)
        {
            Logger(type).Debug(() => string.Format(msg));
        }

        public static void WriteLogException(Type type, string msg)
        {
            Logger(type).Error(() => string.Format(msg));
        }
    }
}
