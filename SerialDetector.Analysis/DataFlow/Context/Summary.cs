using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using dnlib.DotNet;
using SerialDetector.Analysis.DataFlow.Symbolic;

namespace SerialDetector.Analysis.DataFlow.Context
{
    internal sealed class Summary
    {
        public static Summary Empty { get; } = new Summary();

        private Summary()
        {
            IsEmpty = true;
        }

        private long? nodeCount;
        
        public Summary(MethodUniqueSignature signature, 
            SymbolicReference staticContext, 
            SymbolicReference[] arguments, 
            SymbolicReference returnValue,
            double methodCallCount,
            double instructionCount)
        {
            Signature = signature;
            Static = staticContext;
            Arguments = arguments;
            ReturnValue = returnValue;
            MethodCallCount = methodCallCount;
            InstructionCount = instructionCount;

            //if (method.FullName.Contains("System.Text.StringBuilder::.ctor(System.Int32,System.Int32,System.Text.StringBuilder)"))
            //if (method.FullName.Contains("UnsafeStringCopy"))
            //if (method.FullName.Contains("Return"))
            {
//                Dump();
            }
        }

        public bool IsEmpty { get; } = false;
        public MethodUniqueSignature Signature { get; }
        public SymbolicReference Static { get; }
        public SymbolicReference[] Arguments { get; }
        public SymbolicReference ReturnValue { get; }

        public double MethodCallCount { get; }
        public double InstructionCount { get; }

        private static int counter;

        public void Dump()
        {
            var methodName = Signature.ToString();
            
            Dump(@"C:\tmp\experiments\sum",
                $"{counter++}-{methodName.Substring(0, methodName.IndexOf('(')).Replace(':', '_').Replace('\\', '_').Replace('/', '_')}");
        }
        
        public void Dump(string directory, string name)
        {
            var dotFile = Path.Combine(directory, name + ".gv");
            var pngFile = Path.Combine(directory, name + ".png"); 

            var graphViz = new GraphVizSummary(this);
            graphViz.Save(dotFile);
            
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "dot",
                Arguments = "\"" + dotFile + "\" -Tpng -o \"" + pngFile + "\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            });

            process?.WaitForExit();
        }

        public long GetNodeCount()
        {
            if (IsEmpty)
                return 0;

            if (nodeCount.HasValue)
                return nodeCount.Value;
            
            var context = new SymbolicReference.VisitingContext();
            var processingNodes = new Queue<SymbolicReference>();
            if (ReturnValue != null)
            {
                processingNodes.Enqueue(ReturnValue);
            }

            for (var i = 0; i < Arguments.Length; i++)
            {
                var argument = Arguments[i];
                processingNodes.Enqueue(argument);
            }

            long count = 0;
            while (processingNodes.Count > 0)
            {
                var node = processingNodes.Dequeue();
                if (context.IsVisited(node))
                    continue;
                
                context.Visit(node);
                count++;
                
                foreach (var field in node.Fields)
                {
                    processingNodes.Enqueue(field.Value);
                }
            }

            nodeCount = count;
            return count;
        }
    }
}