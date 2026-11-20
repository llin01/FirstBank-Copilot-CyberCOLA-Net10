// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="RelationID.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the RelationId type.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Threading;

namespace Com.Cybersoft4u.ServiceProvider.Utils
{
    #region DEPENDENCY



    #endregion

    public class RelationId
    {
        private static long counter;

        public static string Gen()
        {
            return Convert.ToString(Interlocked.Increment(ref counter));
        }
    }
}
