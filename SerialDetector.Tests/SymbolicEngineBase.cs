using System;
using System.Linq;
using System.Text;
using SerialDetector.Analysis;
using SerialDetector.Analysis.DataFlow;
using dnlib.DotNet;
using NUnit.Framework;

namespace SerialDetector.Tests
{
    internal class SymbolicEngineBase : SelfModelBase
    {
        private readonly IndexDb index;
        private SymbolicEngine engine;

        public SymbolicEngineBase()
            :base("MethodBody")
        {
            // Load all assemblies from single directory!!!
            // copy .NET FW assemblies to GetType().Assembly.Location
            index = new IndexDb(GetType().Assembly.Location);
            index.LoadAssembly(new Uri(typeof(StringBuilder).Assembly.CodeBase).LocalPath);
            index.Build();
            index.ShowStatistic();
        }

        public DataFlowAnalysisStatistic Stat => engine?.CurrentStat;
        
        protected bool InputTaintedMode { get; set; }

        protected DataFlowAnalysisResult CheckResult(DataFlowAnalysisResult result, string[] model)
        {
            if (result == null)
            {
                Assert.Fail("The result is null");
                return null;
            }
            
            Console.WriteLine();
            var text = result.ToText().ToList();
            foreach (var line in text)
            {
                Console.WriteLine(line);
            }

            if (model == null || model.Length == 0 || (model.Length == 1 && String.IsNullOrEmpty(model[0])))
            {
                Assert.That(text, Is.EqualTo(Enumerable.Empty<string>()));
            }
            else
            {
                Assert.That(text, Is.EqualTo(model));    
            }

            return result;
        }
        
        protected DataFlowAnalysisResult Execute(MethodDef method, 
            MethodUniqueSignature taintedSignature = null)
        {
            engine = new SymbolicEngine(index, taintedSignature, 20, true, InputTaintedMode);
            
            var result = engine.ExecuteForward(method);
            result?.Summary.Dump(@"C:\tmp\experiments\tests", method.Name);
            result.Stat.DumpConsole();
            Console.WriteLine($"All method calls/instructions: {result.Summary.MethodCallCount:N0} / {result.Summary.InstructionCount:N0}");
            return result;
        }
    }
}