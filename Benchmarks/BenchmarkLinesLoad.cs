using BenchmarkDotNet.Attributes;
using UtilitiesLibrary;
using System.Threading;

namespace UtilitiesLibraryBenchmark
{
    [MemoryDiagnoser]
    public class BenchmarkLinesLoad
    {
        [Benchmark]
        public void LoadFile()
        {
            using (var lines = new Lines(null))
            {
                var loadCompleted = new ManualResetEvent(false);

                lines.LoadCompleted += (s, e) => loadCompleted.Set();
                lines.Load(@"d:\test\test.eml");

                loadCompleted.WaitOne();
            }
        }
    }
}