using SerialDetector.Analysis;
using SerialDetector.Analysis.DataFlow;
using SerialDetector.Tests.Model.MethodBody;
using dnlib.DotNet;
using NUnit.Framework;

namespace SerialDetector.Tests
{
    [Ignore("The experimental implementation of input tainting is commented now.")]
    internal sealed class InputTaintingTests : SymbolicEngineBase
    {
        public InputTaintingTests()
        {
            InputTaintedMode = true;
        }
        
        [Test]
        public void InputTaintedSuccessTest()
        {
            CheckResult(ExecuteByTaintedObject(GetMethod(nameof(TaintedSamples.InputTaintedSuccess))), 
                new[]
                {
                    "2:CALL SerialDetector.Tests.Model.MethodBody.TaintedSamples::SideEffect(System.Object)"
                });
        }
        
        [Test]
        public void InputTaintedByArraySuccessTest()
        {
            CheckResult(ExecuteByTaintedObject(GetMethod(nameof(TaintedSamples.InputTaintedByArraySuccess))), 
                new[]
                {
                    "2:CALL SerialDetector.Tests.Model.MethodBody.TaintedSamples::SideEffect(System.Object)"
                });
        }
        
        [Test]
        public void InputTaintedFieldSuccessTest()
        {
            CheckResult(ExecuteByTaintedObject(GetMethod(nameof(TaintedSamples.InputTaintedFieldSuccess))), 
                new[]
                {
                    "2:CALL SerialDetector.Tests.Model.MethodBody.TaintedSamples::SideEffect(System.Object)"
                });
        }
        
        [Test]
        public void InputTaintedFieldByCallSuccessTest()
        {
            CheckResult(ExecuteByTaintedObject(GetMethod(nameof(TaintedSamples.InputTaintedFieldByCallSuccess))), 
                new[]
                {
                    "2:CALL SerialDetector.Tests.Model.MethodBody.TaintedSamples::SideEffect(System.Object)"
                });
        }
        
        [Test]
        public void InputTaintedByExternalCallSuccessTest()
        {
            CheckResult(ExecuteByTaintedObject(GetMethod(nameof(TaintedSamples.InputTaintedByExternalCallSuccess))), 
                new[]
                {
                    "2:CALL SerialDetector.Tests.Model.MethodBody.TaintedSamples::SideEffect(System.Object)"
                });
        }
        
        [Test]
        public void InputTaintedByExternal2CallsSuccessTest()
        {
            CheckResult(ExecuteByTaintedObject(GetMethod(nameof(TaintedSamples.InputTaintedByExternal2CallsSuccess))), 
                new[]
                {
                    "2:CALL SerialDetector.Tests.Model.MethodBody.TaintedSamples::SideEffect(System.Object)"
                });
        }
        
        [Test]
        public void InputTaintedByCallSuccessTest()
        {
            CheckResult(ExecuteByTaintedObject(GetMethod(nameof(TaintedSamples.InputTaintedByCallSuccess))), 
                new[]
                {
                    "2:CALL SerialDetector.Tests.Model.MethodBody.TaintedSamples::SideEffect(System.Object)"
                });
        }
        
        [Test]
        public void InputTaintedBy2CallsSuccessTest()
        {
            CheckResult(ExecuteByTaintedObject(GetMethod(nameof(TaintedSamples.InputTaintedBy2CallsSuccess))), 
                new[]
                {
                    "2:CALL SerialDetector.Tests.Model.MethodBody.TaintedSamples::SideEffect(System.Object)"
                });
        }
        
        [Test]
        public void InputTaintedByOutParamSuccessTest()
        {
            CheckResult(ExecuteByTaintedObject(GetMethod(nameof(TaintedSamples.InputTaintedByOutParamSuccess))), 
                new[]
                {
                    "2:CALL SerialDetector.Tests.Model.MethodBody.TaintedSamples::SideEffect(System.Object)"
                });
        }
        
        [Test]
        public void InputTaintedByOutParamAndCallSuccessTest()
        {
            CheckResult(ExecuteByTaintedObject(GetMethod(nameof(TaintedSamples.InputTaintedByOutParamAndCallSuccess))), 
                new[]
                {
                    "2:CALL SerialDetector.Tests.Model.MethodBody.TaintedSamples::SideEffect(System.Object)"
                });
        }
        
        [Test]
        public void InputTaintedByDictionaryOutParamSuccessTest()
        {
            CheckResult(ExecuteByTaintedObject(GetMethod(nameof(TaintedSamples.InputTaintedByDictionaryOutParamSuccess))), 
                new[]
                {
                    "2:CALL SerialDetector.Tests.Model.MethodBody.TaintedSamples::SideEffect(System.Object)"
                });
        }
        
        [Test]
        public void InputTaintedFailTest()
        {
            CheckResult(ExecuteByTaintedObject(GetMethod(nameof(TaintedSamples.InputTaintedFail))), 
                new[]
                {
                    ""
                });
        }
        
        [Test]
        public void InputTaintedFailByCallTest()
        {
            CheckResult(ExecuteByTaintedObject(GetMethod(nameof(TaintedSamples.InputTaintedByCallFail))), 
                new[]
                {
                    ""
                });
        }
        
        [Test]
        public void UnsafeArrayCopyTaintedSuccessTest()
        {
            CheckResult(ExecuteByTaintedObject(GetMethod(nameof(TaintedSamples.UnsafeArrayCopyTaintedSuccess))), 
                new[]
                {
                    "2:CALL SerialDetector.Tests.Model.MethodBody.TaintedSamples::SideEffect(System.Object)"
                });
        }
        
        [Test]
        public void UnsafeArrayCopyTaintedSuccessTest2()
        {
            CheckResult(ExecuteByTaintedObject(GetMethod(nameof(TaintedSamples.UnsafeArrayCopyTaintedSuccess2))), 
                new[]
                {
                    "2:CALL SerialDetector.Tests.Model.MethodBody.TaintedSamples::SideEffect(System.Object)"
                });
        }
        
        [Test]
        public void UnsafeStringCopyTaintedSuccessTest()
        {
            CheckResult(ExecuteByTaintedObject(GetMethod(nameof(TaintedSamples.UnsafeStringCopyTaintedSuccess))), 
                new[]
                {
                    "2:CALL SerialDetector.Tests.Model.MethodBody.TaintedSamples::SideEffect(System.Object)"
                });
        }
        
        [Test]
        public void UnsafeStringCopy2TaintedSuccessTest()
        {
            CheckResult(ExecuteByTaintedObject(GetMethod(nameof(TaintedSamples.UnsafeStringCopy2TaintedSuccess))), 
                new[]
                {
                    "2:CALL SerialDetector.Tests.Model.MethodBody.TaintedSamples::SideEffect(System.Object)"
                });
        }
        
        [Test]
        public void ToStringSuccessTest()
        {
            CheckResult(ExecuteByTaintedObject(GetMethod(nameof(TaintedSamples.ToStringSuccess))), 
                new[]
                {
                    "2:CALL SerialDetector.Tests.Model.MethodBody.TaintedSamples::SideEffect(System.Object)"
                });
        }
        
        [Test]
        public void StringBuilderTaintedSuccessTest()
        {
            CheckResult(ExecuteByTaintedObject(GetMethod(nameof(TaintedSamples.StringBuilderTaintedSuccess))), 
                new[]
                {
                    "2:CALL SerialDetector.Tests.Model.MethodBody.TaintedSamples::SideEffect(System.Object)"
                });
        }
        
        [Test]
        public void FileTextTaintedSuccessTest()
        {
            CheckResult(ExecuteByTaintedObject(GetMethod(nameof(TaintedSamples.FileTextTaintedSuccess))), 
                new[]
                {
                    "2:CALL SerialDetector.Tests.Model.MethodBody.TaintedSamples::SideEffect(System.Object)"
                });
        }
        
        private DataFlowAnalysisResult ExecuteByTaintedObject(MethodDef method) =>
            Execute(method, new MethodUniqueSignature("SerialDetector.Tests.Model.MethodBody.TaintedSamples::CreateTaintedObject(System.Object)"));
        
        private MethodDef GetMethod(string name) =>
            GetMethod(typeof(TaintedSamples), name);
    }
}