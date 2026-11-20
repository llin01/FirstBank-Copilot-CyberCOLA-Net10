using BenchmarkDotNet.Running;

namespace Com.Cybersoft4u.CyberCOLABenchmarks
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<ParserBenchmark>();
        }
    }
}
