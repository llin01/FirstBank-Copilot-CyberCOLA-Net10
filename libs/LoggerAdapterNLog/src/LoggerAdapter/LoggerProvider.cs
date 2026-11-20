// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="LoggerProvider.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the LoggerProvider type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Com.Cybersoft4u.Utils
{
    #region DEPENDENCY

    using System;

    #endregion

    public abstract class LoggerProvider
    {
        public abstract LoggerProvider ConfigureFilePath(string path);

        public abstract Logger CreateLogger(string name);

        public abstract Logger CreateLogger(Type type);
    }
}
