#region << Version Note >>
/*
 * ==============================================================================
 *
 * Namespace/Filename: Com.Cybersoft4u.Test.Wpf.WpfFunctionTest/StaticRandom
 * CLR Version: 4.0.30319.42000
 * Description: 
 *
 * Version: 1.0
 * Created: 2020/10/28 上午 10:22:56
 * Compiler: Visual Studio 2019
 *
 * Author: CFLin
 * Company: CyberSoft Digital Services Corp.
 *
 *******************************************************************
 * CyberSoft Digital Services Corp. All rights reserved.
 *******************************************************************
 * ==============================================================================
*/
#endregion
using System;
using System.Threading;

namespace Com.Cybersoft4u.Test.Wpf.WpfFunctionTest
{
    public static class StaticRandom
    {
        static int seed = Environment.TickCount;

        static readonly ThreadLocal<Random> random =
            new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref seed)));

        public static int Rand()
        {
            return random.Value.Next(1, 899999);
        }
    }
}
