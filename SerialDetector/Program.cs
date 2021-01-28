using System;
using System.Diagnostics;
using System.IO;
using CommandLine;
using SerialDetector.Analysis;
using SerialDetector.CommandLine;
using SerialDetector.KnowledgeBase;
using static System.String;

namespace SerialDetector
{
    static class Program
    {
        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<AnalyzeOptions, PayloadOptions>(args)
                .MapResult(
                    (AnalyzeOptions options) => RunAnalysis(options),
                    (PayloadOptions options) => GeneratePayload(options),
                    errs => 1);
        }

        private static int RunAnalysis(AnalyzeOptions options)
        {
            var timer = Stopwatch.StartNew();
            var indexDb = new IndexDb(options.Directory);
            indexDb.Build();
            indexDb.ShowStatistic();
            
            var callGraphBuilder = new CallGraphBuilder(indexDb);
            CreateCleanDirectory(options.Output);
            foreach (var group in Loader.GetTemplateGroups())
            {
                var callGraph = callGraphBuilder.CreateGraph(group.Templates);
                callGraphBuilder.ShowStatistic();
                if (callGraph.IsEmpty) continue;

                callGraph.RemoveDuplicatePaths();
                
                var groupDirectory = Path.Combine(options.Output, group.Name);
                Directory.CreateDirectory(groupDirectory);
                callGraph.Dump(Path.Combine(groupDirectory, "full.png"));
                callGraph.DumpSeparateUsages(Path.Combine(groupDirectory, "usages"));
                
                callGraph.RemoveSameClasses();
                //callGraph.RemoveMiddleNodes();
                callGraph.Dump(Path.Combine(groupDirectory, "classes.png"));
                //callGraph.RemoveNonPublicEntryNodes();
                //callGraph.Dump(Path.Combine(groupDirectory, "public.png"));
            }
            
            timer.Stop();
            Console.WriteLine($"{timer.ElapsedMilliseconds}");
            Console.WriteLine($"{timer}");
            
            return 0;
        }

        private static void CreateCleanDirectory(string name)
        {
            if (Directory.Exists(name))
            {
                Directory.Delete(name, true);
            }

            Directory.CreateDirectory(name);
        }

        private static int GeneratePayload(PayloadOptions options)
        {
            Payload payload;
            if (!IsNullOrWhiteSpace(options.Name))
            {
                // generate a payload by name
                if (!IsNullOrWhiteSpace(options.Gadget) || !IsNullOrWhiteSpace(options.Formatter))
                {
                    Console.Error.WriteLine("Either a payload name OR a gadget and a formatter MUST be specified.");
                    return -1;
                }
                
                payload = Payload.FromFile(options.Name, options.Command);
            }
            else
            {
                // generate a payload by a gadget-formatter pair
                if (IsNullOrWhiteSpace(options.Gadget) || IsNullOrWhiteSpace(options.Formatter))
                {
                    Console.Error.WriteLine("Either a payload name OR a gadget and a formatter MUST be specified.");
                    return -1;
                }

                var gadget = Create<IGadget>(options.Gadget);
                if (gadget == null)
                {
                    Console.Error.WriteLine($"Not found the gadget {options.Gadget}");
                    return -1;
                }

                var formatter = Create<IFormatter>(options.Formatter);
                if (formatter == null)
                {
                    Console.Error.WriteLine($"Not found the formatter {options.Formatter}");
                    return -1;
                }
                
                payload = formatter.GeneratePayload(gadget.Build(options.Command));
            }

            using var data = payload.ToStream();
            using var output = Console.OpenStandardOutput();
            
            var buffer = new byte[4 * 1024];
            int count = 1;
            while (count > 0)
            {
                count = data.Read(buffer, 0, buffer.Length);
                output.Write(buffer, 0, count);
            }
            
            output.Flush();
            return 0;
        }

        private static T Create<T>(string name)
            where T : class
        {
            var type = typeof(T).Assembly.GetType(
                $"SerialDetector.KnowledgeBase.{(typeof(T).Name == "IGadget" ? "Gadgets" : "Formatters")}.{name}", 
                throwOnError: false, 
                ignoreCase: true);
            if (type == null)
            {
                return null;
            }

            return Activator.CreateInstance(type, nonPublic: true) as T;
        }
    }
}
