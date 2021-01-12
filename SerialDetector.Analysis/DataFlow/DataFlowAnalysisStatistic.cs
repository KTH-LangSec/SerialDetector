using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.VisualBasic.Devices;
using SerialDetector.Analysis.DataFlow.Context;

namespace SerialDetector.Analysis.DataFlow
{
    public sealed class DataFlowAnalysisStatistic
    {
        private class Info
        {
            public string CallerMethodName;
            public string MethodName;
            public long NodeCount;
            public ulong FreeMemoryKb;
            public long SummaryApplyingTicks;
            public long SummaryApplyingMilliseconds;
        }
        
        private readonly List<Info> stat = new List<Info>(100000);
        
        private readonly Dictionary<string, Stopwatch> analysingTimers = new Dictionary<string, Stopwatch>();

        private string applyingSummaryMethod; 
        private readonly Stopwatch applyingTimer = new Stopwatch();
        
        private readonly Stopwatch mainTimer = Stopwatch.StartNew();
        
        internal void StartMethodAnalyzing(string method)
        {
            return;
            if (analysingTimers.ContainsKey(method))
                return;
            
            analysingTimers[method] = Stopwatch.StartNew();
        }

        internal void StopMethodAnalyzing(string method)
        {
            return;
            var timer = analysingTimers[method];
            timer.Stop();
            Console.WriteLine($"Analyzing {method}: {timer.ElapsedTicks} ({timer.ElapsedMilliseconds} ms)");

            analysingTimers.Remove(method);
        }

        internal void StartSummaryApplying(string method)
        {
            applyingSummaryMethod = method;
            applyingTimer.Restart();
        }

        internal void StopSummaryApplying(string callerMethod, Summary summary)
        {
            var method = summary.Signature.ToString();
            if (method != applyingSummaryMethod)
            {
                Console.WriteLine("ERROR! Several summary can not be applied in parallel.");
                applyingSummaryMethod = null;
                return;
            }
            
            applyingTimer.Stop();

            var computerInfo = new ComputerInfo();
            stat.Add(new Info
            {
                CallerMethodName = callerMethod,
                MethodName = method,
                NodeCount = summary.GetNodeCount(),
                FreeMemoryKb = computerInfo.AvailablePhysicalMemory / 1024,
                SummaryApplyingTicks = applyingTimer.ElapsedTicks,
                SummaryApplyingMilliseconds = applyingTimer.ElapsedMilliseconds
            });
            
            applyingTimer.Reset();
            applyingSummaryMethod = null;
        }
        
        public ulong InstructionCount { get; set; }

        public ulong AppliedSummaryCount { get; set; }
        
        public ulong MethodCallCount { get; set; }
        public ulong EmulatedMethodCallCount { get; set; }
        
        public ulong IgnoredTargetMethodCalls { get; set; }
        public ulong IgnoredNotImplementedMethodCalls { get; set; }
        public ulong IgnoredRecursionMethodCalls { get; set; }
        public ulong IgnoredVirtualMethodCallsByLimit { get; set; }
        public HashSet<string> IgnoredVirtualMethodsByLimit { get; } = new HashSet<string>(400);
        
        public HashSet<string> AnalyzedDifferentConcreteMethods { get; } = new HashSet<string>(20000);
        public HashSet<string> AnalyzedDifferentVirtualMethods { get; } = new HashSet<string>(20000);
        
        public ulong AnalyzedConcreteMethodCount { get; set; }
        public ulong AnalyzedVirtualMethodCount { get; set; }

        public ulong CreatedTypesCount { get; set; }
        public ulong DifferentCreatedTypesCount { get; set; }
        
        public long VirtualMemoryBytes { get; private set; }
        public long WorkingSetBytes { get; private set; }
        
        public void StoreMemorySize()
        {
            // GC.Collect();
            // GC.WaitForPendingFinalizers();
            // GC.Collect();
            
            var process = Process.GetCurrentProcess(); 
            VirtualMemoryBytes = process.VirtualMemorySize64;
            WorkingSetBytes = process.WorkingSet64;
        }

        public string Dump2(int uniquePatternCount, int highUniquePatternCount)
        {
            mainTimer.Stop();
            return $"& {mainTimer.ElapsedMilliseconds / 1000.0:N1} & {(VirtualMemoryBytes / 1024 / 1024):N0} & {uniquePatternCount:N0} & {highUniquePatternCount:N0} & {AnalyzedDifferentConcreteMethods.Count:N0} & {AnalyzedConcreteMethodCount:N0} & {MethodCallCount:N0} & {AppliedSummaryCount:N0} & {InstructionCount:N0} \\\\";
        }

        public string Dump()
        {
            mainTimer.Stop();
            var builder = new StringBuilder();
            builder.AppendLine($"Time: {mainTimer.ElapsedMilliseconds} ms");
            builder.AppendLine($"Analyzed Different Methods: {AnalyzedDifferentConcreteMethods.Count + AnalyzedDifferentVirtualMethods.Count}");
            builder.AppendLine($"    Concrete: {AnalyzedDifferentConcreteMethods.Count}");
            builder.AppendLine($"    Virtual (emulated): {AnalyzedDifferentVirtualMethods.Count}");
            builder.AppendLine($"(Re)Analyzed Methods (Created Summaries): {AnalyzedConcreteMethodCount + AnalyzedVirtualMethodCount}");
            builder.AppendLine($"    Concrete: {AnalyzedConcreteMethodCount}");
            builder.AppendLine($"    Virtual (emulated): {AnalyzedVirtualMethodCount}");
            builder.AppendLine($"(Re)Analyzed Instructions: {InstructionCount}");
            builder.AppendLine($"Applied Summaries: {AppliedSummaryCount} times");
            builder.AppendLine($"Method Calls: {MethodCallCount + EmulatedMethodCallCount}");
            builder.AppendLine($"    Emulated Method Calls: {EmulatedMethodCallCount}");
            builder.AppendLine($"    Ignored Target Calls: {IgnoredTargetMethodCalls}");
            builder.AppendLine($"    Ignored Not Implemented: {IgnoredNotImplementedMethodCalls}");
            builder.AppendLine($"    Ignored Recursion Calls: {IgnoredRecursionMethodCalls}");
            builder.AppendLine($"    Ignored Virtual Methods by Limit: {IgnoredVirtualMethodCallsByLimit} ({IgnoredVirtualMethodsByLimit.Count} different methods)");
            builder.AppendLine($"Created Types: {CreatedTypesCount} ({DifferentCreatedTypesCount})");
            builder.AppendLine($"Virtual Memory: {VirtualMemoryBytes / 1024 / 1024} Mb ({VirtualMemoryBytes})");
            builder.AppendLine($"Working Set: {WorkingSetBytes / 1024 / 1024} Mb ({WorkingSetBytes})");
            return builder.ToString();
        }

        public void DumpConsole()
        {
            mainTimer.Stop();
            Console.WriteLine("=== STAT ===");
            Console.Write(Dump());
            Console.WriteLine();
        }

        public void DumpTxt(string directory, string name)
        {
            mainTimer.Stop();
            var path = Path.Combine(directory, name);
            using (var writer = new StreamWriter(path))
            {
                writer.WriteLine(Dump());
                // writer.WriteLine();
                // writer.WriteLine("DUMP2");
                // writer.WriteLine(Dump2());
            }
        }

        public void DumpCsv(string directory, string name)
        {
            mainTimer.Stop();
            var path = Path.Combine(directory, name);
            using (var writer = new StreamWriter(path))
            {
                // header
                writer.WriteLine("sep=;");
                writer.WriteLine("Index;Caller;Method;NodeCount;FreeMemoryKb;SummaryApplyingMilliseconds");
                
                // data
                for (int i = 0; i < stat.Count; i++)
                {
                    var row = stat[i];
                    writer.WriteLine($"{i};{row.CallerMethodName};{row.MethodName};{row.NodeCount};{row.FreeMemoryKb};{row.SummaryApplyingMilliseconds}");
                }
            }
        }
    }
}