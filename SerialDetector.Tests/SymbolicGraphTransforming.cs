using SerialDetector.Analysis;
using SerialDetector.Analysis.DataFlow;
using SerialDetector.Tests.Model.MethodBody;
using dnlib.DotNet;
using NUnit.Framework;

namespace SerialDetector.Tests
{
    internal sealed class SymbolicGraphTransforming : SymbolicEngineBase
    {
        public SymbolicGraphTransforming()
        {
            InputTaintedMode = false;
        }
        
        [Test]
        public void BFSApplyingMergingA()
        {
            CheckResult(ExecuteByTaintedStringBuilderCtor(GetMethod(nameof(GraphSamples.BFSApplyingMergingA))),
                new[]
                {
                    "3:CALL SerialDetector.Tests.Model.MethodBody.GraphSamples::SideEffect(System.Object)"
                });
        }

        
        private DataFlowAnalysisResult ExecuteByTaintedStringBuilderCtor(MethodDef method) =>
            Execute(method, new MethodUniqueSignature("System.Text.StringBuilder::.ctor()"));
        
        private MethodDef GetMethod(string name) =>
            GetMethod(typeof(GraphSamples), name);
    }
}