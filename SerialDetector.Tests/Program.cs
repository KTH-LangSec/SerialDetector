using System;
using System.Diagnostics;
using System.Threading;
using SerialDetector.Analysis.DataFlow;

namespace SerialDetector.Tests
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            var test = new SymbolicEngineTests();
            
            var workingThread = new Thread(() =>
            {
                var result = test.DotNetFrameworkMethodTest();
                Console.WriteLine("Analysis is completed");
                Dump(result?.Stat);
            });
            var timer = Stopwatch.StartNew();
            workingThread.Start();
                
            Console.WriteLine();
            Console.WriteLine("Press q and <Enter> to exit...");
            while (Console.ReadLine() != "q")
            {
            }

            if (workingThread.IsAlive)
            {
                workingThread.Abort();
                workingThread.Join();
                timer.Stop();
                Console.WriteLine($"Analysis is aborted ({timer.ElapsedMilliseconds} ms)");
                Dump(test.Stat);
            }

            return 1;
        }

        private static void Dump(DataFlowAnalysisStatistic stat)
        {
            if (stat == null)
                return;
            
            var output = @"D:\tmp\.net-refs\res";
            stat.DumpConsole();
            stat.DumpCsv(output, "dfa_stat.csv");
        }
    }
}