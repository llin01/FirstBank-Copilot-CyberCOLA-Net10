namespace Com.Cybersoft4u.CyberUtils.Utils
{
    using System;
    using System.Reflection;

    public static class AssemblyUtils
    {
        /// <summary>
        /// Get All Assemblies Loaded.
        /// </summary>
        /// <returns></returns>
        public static Assembly[] GetAllAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies();
        }

        /// <summary>
        /// Get Loaded <code>Assembly</code> which has <code>type</code>.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Assembly GetAssembly(Type type)
        {
            return Assembly.GetAssembly(type);
        }
    }
}
