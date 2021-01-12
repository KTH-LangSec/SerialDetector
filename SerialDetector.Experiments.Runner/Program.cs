using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using CommandLine;
using dnlib.DotNet;
using SerialDetector.Analysis;
using SerialDetector.Analysis.DataFlow;
using SerialDetector.Experiments.Runner.CommandLine;

namespace SerialDetector.Experiments.Runner
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<AnalyzeOptions, AnalyzeDotNetOptions>(args)
                .MapResult(
                    (AnalyzeOptions options) => RunAnalysis(options),
                    (AnalyzeDotNetOptions options) => RunAnalysis(options),
                    errs => 1);
        }

        private static int RunAnalysis(AnalyzeOptions options)
        {
            return RunAnalysis(options.Directory, options.EntryPoint, options.Output);
        }

        private static int RunAnalysis(AnalyzeDotNetOptions options)
        {
            // get .NET FW directory
            Console.WriteLine(RuntimeEnvironment.GetSystemVersion());
            var dotNetInstallDirectory = RuntimeEnvironment.GetRuntimeDirectory();
            Console.WriteLine($"Detect .NET FW: {dotNetInstallDirectory}");
            
            // Copy .NET to Temp directory
            if (!Directory.Exists(options.TempDirectory))
                Directory.CreateDirectory(options.TempDirectory);
            
            CopyAssemblies(dotNetInstallDirectory, options.TempDirectory);
            CopyAssemblies(Path.Combine(dotNetInstallDirectory, "WPF"), options.TempDirectory);

            // Copy files from input directory to Temp
            if (!String.IsNullOrEmpty(options.Directory))
            {
                CopyAssemblies(options.Directory, options.TempDirectory);
            }

            return RunAnalysis(options.TempDirectory, options.EntryPoint, options.Output);
        }

        private static void CopyAssemblies(string from, string to)
        {
            foreach (var file in Directory.EnumerateFiles(from))
            {
                var fileName = Path.GetFileName(file);
                var fileExt = Path.GetExtension(fileName);
                if (fileExt != ".dll" && fileExt != ".exe")
                    continue;

                if (!File.Exists(Path.Combine(to, fileName)))
                    File.Copy(file, Path.Combine(to, fileName));
            }
        }
        
        
        private static int RunAnalysis(string input, string entryPoint, string output)
        {
            Console.WriteLine("Copying SerialDetector.Experiments...");
            File.Copy(
                typeof(Deserializers).Assembly.Location, 
                Path.Combine(input, Path.GetFileName(typeof(Deserializers).Assembly.Location)), 
                true);
            
            Console.WriteLine($"[{DateTime.Now:T}]");
            var indexDb = new IndexDb(input);
            
            var v = new Version();
            var methods = indexDb.Assemblies.FindSensitiveSinkCalls().ToList();
            Console.WriteLine($"[{DateTime.Now:T}] {methods.Count} methods!");

            var sensitiveSinks = methods
                .Select(method => method.CreateMethodUniqueSignature())
                .Distinct()
                .Select(name => new TemplateInfo(name, v))
                .ToList();
            
            CreateCleanDirectory(output);            
            File.WriteAllLines(
                Path.Combine(output, "sensitive-sinks.txt"),
                sensitiveSinks.Select(info => info.Method.ToString()));

            var setUp = GetEntryPointSetUp(entryPoint);
            sensitiveSinks = new List<TemplateInfo>
            {
                new TemplateInfo(new MethodUniqueSignature(setUp.SensitiveSink), v)
            };
            
            Console.WriteLine($"[{DateTime.Now:T}] {sensitiveSinks.Count} patterns!");
            foreach (var method in sensitiveSinks)
            {
                Console.WriteLine($"    {method.Method}");
            }
            
            Console.WriteLine();
            
            // TODO: remove convertedArgumentTypes
            var convertedArgumentTypes = indexDb.Build();
            indexDb.ShowStatistic();
            Console.WriteLine($"[{DateTime.Now:T}]");

            var callGraphBuilder = new CallGraphBuilder(indexDb);

            var i = 0;
            foreach (var sensitiveSink in sensitiveSinks)
            {
                var g = callGraphBuilder.CreateGraph(new List<TemplateInfo> {sensitiveSink});
                Console.WriteLine();
                Console.WriteLine($"[{DateTime.Now:T}] #{++i} {sensitiveSink.Method}");
                callGraphBuilder.ShowStatistic();

                if (g.IsEmpty)
                {
                    Console.WriteLine("CFA: Not found!");
                    continue;
                }
                
                StoreEntryPoints(Path.Combine(output, $"{i}_ep_all.txt"), g);
                Console.WriteLine($"Graph: nodes = {g.Nodes.Count}, entry nodes = {g.EntryNodes.Count}");
            
                g.RemoveDuplicatePaths();
                Console.WriteLine($"Graph: nodes = {g.Nodes.Count}, entry nodes = {g.EntryNodes.Count}");
            
                g.RemoveNonPublicEntryNodes();
                Console.WriteLine($"Graph: nodes = {g.Nodes.Count}, entry nodes = {g.EntryNodes.Count}");

                g.RemoveNonPublicMiddleNodes();
                Console.WriteLine($"Graph: nodes = {g.Nodes.Count}, entry nodes = {g.EntryNodes.Count}");
                //ShowEntryPoints(currentPatterns, g);
                StoreEntryPoints(Path.Combine(output, $"{i}_ep_public.txt"), g);
                if (g.Nodes.Count > 1 && g.Nodes.Count < 1000)
                {
                    g.Dump(Path.Combine(output, $"{i}_po.png"));
                }
                else
                {
                    Console.WriteLine($"{i}_ep_public graph contains {g.Nodes.Count} nodes!");
                }

                // DFA
                var symbolicEngine = new SymbolicEngine(indexDb, sensitiveSink.Method, 
                    setUp.VirtualCallsLimit, setUp.EnableStaticFields, false);
                var workingThread = new Thread(() =>
                {
                    RunDataFlowAnalysis(indexDb, g, entryPoint, convertedArgumentTypes, symbolicEngine, output);
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
                    symbolicEngine.CurrentStat?.DumpConsole();
                    symbolicEngine.CurrentStat?.DumpTxt(output, "dfa_stat.txt");
                    //symbolicEngine.CurrentStat?.DumpCsv(output, "dfa_stat.csv");
                    Console.WriteLine($"Analysis is aborted ({timer.ElapsedMilliseconds} ms)");
                }
            }

            return 0;
        }

        private static SetUpAttribute GetEntryPointSetUp(string entryPoint)
        {
            Console.WriteLine($"Analyzing {entryPoint}...");

            var className = "";
            var methodName = "";
            for (int i = 0; i < entryPoint.Length - 2; i++)
            {
                if (entryPoint[i] == ':' && entryPoint[i+1] == ':')
                {
                    className = entryPoint.Substring(0, i);
                    methodName = entryPoint.Substring(i + 2);
                    break;
                }
            }

            var classes = typeof(Deserializers).Assembly.DefinedTypes
                .Where(info => info.FullName == className || info.Name == className)
                .ToList();
            if (classes.Count == 0)
            {
                throw new Exception($"The class '{className}' is not found for {entryPoint}");
            }

            if (classes.Count > 1)
            {
                throw new Exception($"Many classes '{className}' are found for {entryPoint}");
            }

            var methods = classes[0].DeclaredMethods
                .Where(info => info.Name == methodName)
                .ToList();
            if (methods.Count == 0)
            {
                throw new Exception($"The method {methodName} is for found for {entryPoint}");
            }

            if (methods.Count > 1)
            {
                throw new Exception($"Many methods '{methodName}' are found for {entryPoint}");
            }

            var attributes = methods[0].GetCustomAttributes(typeof(SetUpAttribute), true);
            if (attributes.Length != 1)
            {
                throw new Exception($"The attribute '{typeof(SetUpAttribute).Name}' is for found for {entryPoint}");
            }

            return (SetUpAttribute) attributes[0];
        }

        private static void RunDataFlowAnalysis(IndexDb indexDb, CallGraph graph,
            string entryPoint, List<TypeDef> requiredArgumentTypes,
            SymbolicEngine symbolicEngine, string output)
        {
            try
            {
                if (!entryPoint.EndsWith(")"))
                    entryPoint += "()";
                
                var entryIndex = 0;
                foreach (var entry in /*graph.EntryNodes.Values) //*/graph.Nodes.Values)
                {
                    if (entry.MethodSignature.ToString() != entryPoint &&
                        entry.MethodSignature.ToString() != $"{typeof(Deserializers).Namespace}.{entryPoint}")
                        continue;

                    Console.WriteLine($"{entry.MethodSignature} analyzing...");
                    var references = indexDb.AssemblyReferences[entry.AssemblyName];
                    Console.WriteLine($"    Assembly: {entry.AssemblyName}, References: {references.Count}");
                    var timer = Stopwatch.StartNew(); 
                    var result = symbolicEngine.ExecuteForward(entry.MethodDef, requiredArgumentTypes);
                    if (result == null)
                    {
                        Console.WriteLine("FATAL ERROR: DFA result is empty.");
                        break;
                    }
                    
                    timer.Stop();
                    Console.WriteLine($"{entry.MethodSignature}: {timer.ElapsedMilliseconds} ms");
                    if (result.HasPattern)
                    {
                    }

                    Console.WriteLine($"DFA: {entry.MethodSignature} {result.ExternalCallCount} calls of {result.PatternCount} tainted object");
                    result.Stat.DumpConsole();
                    Console.WriteLine($"All method calls/instructions: {result.Summary.MethodCallCount} / {result.Summary.InstructionCount}");
                    Console.WriteLine("============");
                    //result.Stat.DumpTxt(output, $"dfa_stat_{entryIndex++}_{entry.MethodDef.Name}.txt");
                    //result.Stat.DumpCsv(output, $"dfa_stat_{entryIndex}_{entry.MethodDef.Name}.csv");
                    var p = result.Dump(output, $"patterns_{entryIndex}_{entry.MethodDef.Name}");
                    result.DumpAllStat(output, $"dfa_stat_{entryIndex}_{entry.MethodDef.Name}.txt", p);
                    Console.WriteLine();
                    entryIndex++;
                    break;
                }
                
                Console.WriteLine("Analysis is competed!");
            }
            catch (ThreadAbortException)
            {
                Thread.ResetAbort();
            }
        }

        private static void StoreEntryPoints(string filePath, CallGraph graph)
        {
            if (graph.EntryNodes.Count == 0)
            {
                return;
            }

            File.WriteAllLines(filePath,
                graph.EntryNodes.Values.Select(node => $"{node.MethodSignature}"));
        }

        private static void ShowEntryPoints(List<TemplateInfo> templates, CallGraph graph)
        {
            if (graph.EntryNodes.Count == 0)
            {
                Console.WriteLine("NO ONE ENTRY POINT");
            }
            else
            {
                Console.WriteLine("ENTRY POINTS");
                foreach (var node in graph.EntryNodes.Values)
                {
                    Console.WriteLine($"{node.MethodSignature}");
                }
            }

            if (templates != null)
            {
                foreach (var template in templates)
                {
                    Console.WriteLine($"    {template.Method}");
                }
            }
            
            Console.WriteLine("== == == == == == == == == == ==");
            Console.WriteLine();
        }
        
        private static void CreateCleanDirectory(string name)
        {
            if (Directory.Exists(name))
            {
                Directory.Delete(name, true);
            }

            Directory.CreateDirectory(name);
        }
    }
}