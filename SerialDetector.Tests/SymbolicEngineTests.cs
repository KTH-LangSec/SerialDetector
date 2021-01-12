using SerialDetector.Analysis;
using SerialDetector.Analysis.DataFlow;
using SerialDetector.Tests.Model.MethodBody;
using dnlib.DotNet;
using NUnit.Framework;

namespace SerialDetector.Tests
{
    internal sealed class SymbolicEngineTests : SymbolicEngineBase
    {
        public SymbolicEngineTests()
        {
            InputTaintedMode = false;
        }
        
        /*
        [SetUp]
        public void Setup()
        {
        }
        */

        [Test]
        public void BarTest()
        {
            CheckResult(ExecuteByTaintedStringBuilderCtor(GetJumpsMethod(nameof(Jumps.Bar))), 
                new[]
                {
                    "2:CALL SerialDetector.Tests.Model.MethodBody.Jumps::SideEffect(System.Object,System.String)"
//                    "2:CALL SerialDetector.Tests.Model.MethodBody.Jumps::SideEffect(T, N)",
//                    "FIELDS Arg0.f2 == T:MethodReturn[System.Void System.Text.StringBuilder::.ctor()]",
//                    "FIELDS Arg0.f1 == T:MethodReturn[System.Void System.Text.StringBuilder::.ctor()]"
                });
        }
        
        [Test]
        public void SimpleAliasTest()
        {
            CheckResult(ExecuteByTaintedStringBuilderCtor(GetJumpsMethod(nameof(Jumps.EntryPoint))), 
                new[]
                {
                    "3:CALL SerialDetector.Tests.Model.MethodBody.Jumps::SideEffect(System.Object,System.String)"
                });
        }
        
        [Test]
        public void ReturnTaintedCaseTest()
        {
            CheckResult(ExecuteByTaintedStringBuilderCtor(GetJumpsMethod(nameof(Jumps.ReturnTaintedCase))),
                new[]
                {
                    "2:CALL SerialDetector.Tests.Model.MethodBody.Jumps::SideEffect(System.Object,System.String)",
                    "2:CALL SerialDetector.Tests.Model.MethodBody.Jumps::SideEffect(System.Object,System.String)",
                    "2:CALL SerialDetector.Tests.Model.MethodBody.Jumps::SideEffect(System.Object,System.String)"
//                    "2:CALL SerialDetector.Tests.Model.MethodBody.Jumps::SideEffect(T, N)",
//                    "2:CALL SerialDetector.Tests.Model.MethodBody.Jumps::SideEffect(T, N)",
//                    "RETURN T:MethodReturn[System.Void System.Text.StringBuilder::.ctor()]",
//                    "FIELDS Arg0.f2 == T:MethodReturn[System.Void System.Text.StringBuilder::.ctor()]"
                });
        }

        [Test]
        public void AccessPathThroughCallByStaticTest()
        {
            CheckResult(ExecuteByTaintedStringBuilderCtor(GetJumpsMethod(nameof(Jumps.AccessPathThroughCallByStatic))),
                new[]
                {
                    "3:CALL SerialDetector.Tests.Model.MethodBody.Jumps::SideEffect(System.Object,System.String)"
//                    "2:CALL SerialDetector.Tests.Model.MethodBody.Jumps::SideEffect(T, N)",
//                    "STATIC SerialDetector.Tests.Model.MethodBody.Jumps::myClassStatic == Arg0|myClass.cl",
//                    "FIELDS Arg0|myClass.cl.cl == Arg0|myClass.cl.cl.cl",
//                    "FIELDS Arg0|myClass.cl.cl == Arg2|cl",
//                    "FIELDS Arg0|myClass.cl.cl == Arg2|cl.cl",
//                    "FIELDS Arg0|myClass.cl.cl.cl == Arg2|cl",
//                    "FIELDS Arg0|myClass.cl.cl.cl.cl == Arg2|cl",
//                    "FIELDS Arg2|cl.obj == T:MethodReturn[System.Void System.Text.StringBuilder::.ctor()]"
                });
        }

        [Test]
        public void VirtualMethodCallTest()
        {
            CheckResult(ExecuteByTaintedStringBuilderCtor(GetJumpsMethod(nameof(Jumps.VirtualMethodCall))),
                new[]
                {
                    "4:CALL SerialDetector.Tests.Model.MethodBody.Jumps::SideEffect(System.Object,System.String)"
//                    "2:CALL SerialDetector.Tests.Model.MethodBody.Jumps::SideEffect(T, N)"
                });
        }

        [Test]
        public void MergeArguments2Test()
        {
            CheckResult(ExecuteByTaintedStringBuilderCtor(GetJumpsMethod(nameof(Jumps.MergeArguments2))),
                new[]
                {
                    "2:CALL SerialDetector.Tests.Model.MethodBody.Jumps::SideEffect(System.Object,System.String)"
                });
        }
        
        [Test]
        public void MergeEntitiesBugTest()
        {
            CheckResult(ExecuteByTaintedStringBuilderCtor(GetJumpsMethod(nameof(Jumps.MergeEntitiesBug))),
                new[]
                {
                    "2:CALL SerialDetector.Tests.Model.MethodBody.Jumps::SideEffect(System.Object,System.String)",
                    "2:CALL SerialDetector.Tests.Model.MethodBody.Jumps::SideEffect(System.Object,System.String)"
                });
        }
        
        [Test]
        public void AccessPathThroughCallTest()
        {
            CheckResult(ExecuteByTaintedStringBuilderCtor(GetJumpsMethod(nameof(Jumps.AccessPathThroughCall))),
                new[]
                {
                    "2:CALL SerialDetector.Tests.Model.MethodBody.Jumps::SideEffect(System.Object,System.String)"
//                    "2:CALL SerialDetector.Tests.Model.MethodBody.Jumps::SideEffect(T, N)",
//                    "FIELDS Arg1|cl.cl == Arg2",
//                    "FIELDS Arg2|cl.obj == T:MethodReturn[System.Void System.Text.StringBuilder::.ctor()]"
                });
        }
        
        [Test]
        public void AccessPathRecursionTest()
        {
            CheckResult(ExecuteByTaintedStringBuilderCtor(GetJumpsMethod(nameof(Jumps.AccessPathRecursion))),
                new[]
                {
                    "2:CALL SerialDetector.Tests.Model.MethodBody.Jumps::SideEffect(System.Object,System.String)"
//                    "2:CALL SerialDetector.Tests.Model.MethodBody.Jumps::SideEffect(T, N)",
//                    "FIELDS Arg0|myClass.cl == Arg2|cl",
//                    "FIELDS Arg2|cl.obj == T:MethodReturn[System.Void System.Text.StringBuilder::.ctor()]",
//                    "FIELDS Arg2|cl.cl.cl == Arg2|cl"
                });
        }

        [Test]
        public void SecondSideEffectCallShouldBeTaintedTest()
        {
            CheckResult(ExecuteByTaintedStringBuilderCtor(GetJumpsMethod(nameof(Jumps.SecondSideEffectCallShouldBeTainted))),
                new[]
                {
                    "2:CALL SerialDetector.Tests.Model.MethodBody.Jumps::SideEffect(System.Object,System.String)",
                    "2:CALL SerialDetector.Tests.Model.MethodBody.Jumps::SideEffect(System.Object,System.String)"
//                    "2:CALL SerialDetector.Tests.Model.MethodBody.Jumps::SideEffect(T, N)",
//                    "2:CALL SerialDetector.Tests.Model.MethodBody.Jumps::SideEffect(T, N)"
                });
        }

        [Test]
        public void RecursiveObjectGraphTest()
        {
            CheckResult(ExecuteByTaintedStringBuilderCtor(GetJumpsMethod(nameof(Jumps.RecursiveObjectGraph))),
                new[]
                {
                    "2:CALL SerialDetector.Tests.Model.MethodBody.Jumps::SideEffect(System.Object,System.String)"
//                    "2:CALL SerialDetector.Tests.Model.MethodBody.Jumps::SideEffect(T, N)",
//                    "FIELDS Arg0|myClass.cl == Arg0|myClass",
//                    "FIELDS Arg0|myClass.obj == T:MethodReturn[System.Void System.Text.StringBuilder::.ctor()]"
                });
        }
        
        [Test]
        public void AssignToOutParameterTest()
        {
            CheckResult(ExecuteByTaintedStringBuilderCtor(GetJumpsMethod(nameof(Jumps.AssignToOutParameter))),
                new[]
                {
                    "3:CALL SerialDetector.Tests.Model.MethodBody.Jumps::SideEffect(System.Object,System.String)"
                });
        }
        
        [Test]
        public void ReturnFromOutParameterTest()
        {
            CheckResult(ExecuteByTaintedStringBuilderCtor(GetJumpsMethod(nameof(Jumps.ReturnFromOutParameter))),
                new[]
                {
                    "3:CALL SerialDetector.Tests.Model.MethodBody.Jumps::SideEffect(System.Object,System.String)"
                });
        }
        
        [Test]
        public void ReturnFromRefParameterTest()
        {
            CheckResult(ExecuteByTaintedStringBuilderCtor(GetJumpsMethod(nameof(Jumps.ReturnFromRefParameter))),
                new[]
                {
                    "2:CALL SerialDetector.Tests.Model.MethodBody.Jumps::SideEffect(System.Object,System.String)"
                });
        }

        [Test]
        public void NotReturnFromParameterTest()
        {
            CheckResult(ExecuteByTaintedStringBuilderCtor(GetJumpsMethod(nameof(Jumps.NotReturnFromParameter))),
                new string[]
                {
                });
        }

        [Test]
        public void RecursiveMergingExecutionContextTest()
        {
            CheckResult(ExecuteByTaintedStringBuilderCtor(GetJumpsMethod(nameof(Jumps.RecursiveMergingExecutionContext))),
                new string[0]);
        }

        [Test]
        public void RecursiveSummaryApplyingTest()
        {
            CheckResult(ExecuteByTaintedStringBuilderCtor(GetJumpsMethod(nameof(Jumps.RecursiveSummaryApplying))),
                new string[0]);
        }
        
        [Test]
        public void ForeachBodyTest()
        {
            CheckResult(ExecuteByTaintedStringBuilderCtor(GetJumpsMethod(nameof(Jumps.ForeachBody))),
                new[]
                {
                    "2:CALL SerialDetector.Tests.Model.MethodBody.Jumps::SideEffect(System.Object,System.String)"
//                    "2:CALL SerialDetector.Tests.Model.MethodBody.Jumps::SideEffect(T, N)",
//                    "FIELDS Arg0.f2 == T:MethodReturn[System.Void System.Text.StringBuilder::.ctor()]"
                });
        }
        
        [Test]
        public void RecursionMaterializingFieldTest()
        {
            CheckResult(ExecuteByTaintedStringBuilderCtor(GetJumpsMethod(nameof(Jumps.RecursionMaterializingField))),
                new[]
                {
                    "3:CALL SerialDetector.Tests.Model.MethodBody.Jumps::SideEffect(System.Object,System.String)"
                });
        }

        [Test]
        public void FewSSCallsTest()
        {
            CheckResult(ExecuteByTaintedStringBuilderCtor(GetJumpsMethod(nameof(Jumps.FewSSCalls))),
                new[]
                {
                    "3:CALL SerialDetector.Tests.Model.MethodBody.Jumps::SideEffect(System.Object,System.String)"
                });
        }

        //[Test]
        //[Ignore("for debugging")]
        public DataFlowAnalysisResult DotNetFrameworkMethodTest()
        {
            // revert the commit "Fix a limit to summary size." and repro too much summary with aliases to recursion objects like 
            // (Arg7|m_objects, $array, Arg7.m_objects.$array.m_next.m_next.m_next.m_next.m_next.m_next)
            //System.Text.StringBuilder a;
            //a.Remove(); //(System.Int32,System.Int32,System.Text.StringBuilder&,System.Int32&)
            var result = CheckResult(
                //Execute(GetMethodFW(typeof(System.Xml.Serialization.XmlSerializer).Assembly.GetType("System.Xml.DtdParser"), "GetToken")), 
                Execute(GetMethodFW(typeof(System.Xml.Serialization.XmlSerializer).Assembly.GetType("System.Xml.DtdParser"), "ParseInDocumentDtd"),
                    new MethodUniqueSignature("System.RuntimeTypeHandle::Allocate(System.RuntimeType)")), 
                //Execute(GetMethodFW("System.Globalization.CalendricalCalculationsHelper", "SumLongSequenceOfPeriodicTerms")), 
                //Execute(GetMethodFW(typeof(ServicePointManager), "FindServicePoint")), 
                null);

            return result;
        }

        [Test]
        [Ignore("for debugging")]
        public void UninitializedValueSourceTest()
        {
            //var method = GetMethodFW("System.TimeZoneInfo", "get_Local");
            //var method = GetMethodFW("System.Security.Permissions.FileIOAccess", ".ctor");
            //var method = GetMethodFW("System.Globalization.CultureData", "GetCultureData");
            var method = GetMethodFW("System.Runtime.Serialization.ObjectHolder", "UpdateData");
            //var method = GetMethodFW(typeof(System.Uri), "ParseRemaining");
            
            CheckResult(ExecuteByTaintedStringBuilderCtor(method),
                new[]
                {
                    ""
                });
        }

        private DataFlowAnalysisResult ExecuteByTaintedStringBuilderCtor(MethodDef method) =>
            Execute(method, new MethodUniqueSignature("System.Text.StringBuilder::.ctor()"));
        
        private MethodDef GetJumpsMethod(string name) =>
            GetMethod(typeof(Jumps), name);
    }
}