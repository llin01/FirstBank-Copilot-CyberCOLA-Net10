using BenchmarkDotNet.Attributes;
using System.Text;

namespace Com.Cybersoft4u.CyberCOLABenchmarks
{
    // Enable memory diagnostics to track GC allocations
    [MemoryDiagnoser]
    public class ParserBenchmark
    {
        private byte[] _rawFiscMessage = Array.Empty<byte>();

        [GlobalSetup]
        public void Setup()
        {
            // Simulate a 200-byte FISC/ISO8583 message string
            // Field 39 (Response Code) is located at offset 120 with length 2
            string sampleMsg = new string('A', 120) + "00" + new string('B', 78);
            _rawFiscMessage = Encoding.ASCII.GetBytes(sampleMsg);
        }

        // 1. Baseline: Traditional substring slicing via string instantiation
        [Benchmark(Baseline = true)]
        public string LegacyStringParser()
        {
            string rawText = Encoding.ASCII.GetString(_rawFiscMessage);
            return rawText.Substring(120, 2); 
        }

        // 2. Modern: Zero-allocation slice comparison using ReadOnlySpan and UTF-8 literals
        [Benchmark]
        public bool ModernSpanParser()
        {
            ReadOnlySpan<byte> messageSpan = _rawFiscMessage;
            ReadOnlySpan<byte> field39 = messageSpan.Slice(120, 2);
            return field39.SequenceEqual("00"u8);
        }
    }
}
