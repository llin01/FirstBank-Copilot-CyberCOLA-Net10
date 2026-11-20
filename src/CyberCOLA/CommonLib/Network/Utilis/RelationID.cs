// --------------------------------------------------------------------------------------------------------------------
// <copyright company="Cybersoft" file="RelationID.cs">
//   Author: Yeti
// </copyright>
// <summary>
//   Defines the RelationId type.
// </summary>
//
// --------------------------------------------------------------------------------------------------------------------

namespace Com.Cybersoft4u.CommonLib.Network.Utilis
{
    #region DEPENDENCY

    using System;
    using System.Threading;

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
