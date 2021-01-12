using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using dnlib.DotNet;
using SerialDetector.Analysis.DataFlow.Context;
using SerialDetector.Analysis.DataFlow.Symbolic;
using ExecutionContext = SerialDetector.Analysis.DataFlow.Context.ExecutionContext;

namespace SerialDetector.Analysis.DataFlow
{
    public sealed class SymbolicEngine : SummaryCache
    {
        private sealed class PatternCache : IPatternCache
        {
            private ulong taintedIndex = 2;

            public ulong GetNewTaintedIndex() => taintedIndex++; 

            public Dictionary<ulong, TaintedSourceInfo> Patterns { get; } = new Dictionary<ulong, TaintedSourceInfo>();
        }
        
        private readonly ExternalMethodList list = new ExternalMethodList();
        
        private readonly IndexDb indexDb;
        private readonly MethodUniqueSignature taintedCall;
        private readonly uint virtualCallsLimit;
        private readonly bool enableStaticFields;
        private readonly bool inputTaintedMode = false;

        private readonly PatternCache patternCache = new PatternCache();
        private string entryPointAssemblyName;
        private bool firstExecutionContextCreating;

        public SymbolicEngine(IndexDb indexDb, 
            MethodUniqueSignature taintedCall, 
            uint virtualCallsLimit,
            bool enableStaticFields,
            bool inputTaintedMode)
        {
            this.indexDb = indexDb;
            this.taintedCall = taintedCall;
            this.virtualCallsLimit = virtualCallsLimit;
            this.enableStaticFields = enableStaticFields;
            this.inputTaintedMode = inputTaintedMode;
            firstExecutionContextCreating = inputTaintedMode;
        }

        public DataFlowAnalysisStatistic CurrentStat => result.Stat;

        public DataFlowAnalysisResult ExecuteForward(MethodDef method, List<TypeDef> requiredArgumentTypes = null)
        {
            try
            {
                return ExecuteForwardInternal(method, requiredArgumentTypes);
            }
            catch (ThreadAbortException)
            {
                throw;
            }
            catch (Exception e)
            {
                var message = e.ToString();
                var index = -1;
                var lines = 20;
                while (lines > 0 && index + 1 < message.Length &&
                       (index = message.IndexOf(Environment.NewLine, index + 1)) > -1)
                {
                    lines--;
                }

                if (index > -1)
                {
                    message = message.Substring(0, index);
                }
                
                Console.WriteLine(message);
                return null;
            }
        }
        
        public DataFlowAnalysisResult ExecuteForwardInternal(MethodDef method, List<TypeDef> requiredArgumentTypes)
        {
            if (!IsCallStackEmpty)
            {
                throw new Exception("The current call stack must be empty");
            }

            if (result != null)
            {
                throw new Exception("The current result must be null");
            }

            entryPointAssemblyName = method.DeclaringType.DefinitionAssembly.Name;
            firstExecutionContextCreating = inputTaintedMode;
            result = new DataFlowAnalysisResult(patternCache);

            if (requiredArgumentTypes != null)
            {
                foreach (var type in requiredArgumentTypes)
                {
                    RegisterType(type);
                }
            }

            var summary = GetOrCreateSummary(new MethodCall(
                method.CreateMethodUniqueSignature(),
                method,
                null,
                CallKind.Concrete));
            result.Stat.StoreMemorySize();
            result.Summary = summary;
            var r = result;
            result = null;
            entryPointAssemblyName = null;
            CheckRecursionCache();
            ResetSummaryCache();
            return r;
        }

        private protected override Summary Analyze(MethodCall methodCall)
        {
            Debug.Assert(methodCall.CallKind == CallKind.Concrete || methodCall.Parameters != null);
            if (methodCall.CallKind == CallKind.Concrete)
            {
                Debug.Assert(methodCall.Definition.HasBody);
                result.Stat.AnalyzedConcreteMethodCount++;
                result.Stat.AnalyzedDifferentConcreteMethods.Add(methodCall.Signature.ToString());
                result.Stat.InstructionCount += (ulong)methodCall.Definition.Body.Instructions.Count; 
                
                var context = new ExecutionContext(
                    CallStack,
                    methodCall.Signature, 
                    methodCall.Definition,
                    enableStaticFields,
                    firstExecutionContextCreating);
                if (firstExecutionContextCreating)
                {
                    firstExecutionContextCreating = false;
                }

                result.Stat.StartMethodAnalyzing(methodCall.Signature.ToString());
                BuildExecutionContext(context, methodCall);
                result.Stat.StopMethodAnalyzing(methodCall.Signature.ToString());
                return context.ToSummary();
            }
            else
            {
                // emulate virtual call
                // TODO: optimize virtual call w/o implMethods?
                result.Stat.AnalyzedVirtualMethodCount++;
                result.Stat.AnalyzedDifferentVirtualMethods.Add(methodCall.Signature.ToString());
                var context = new ExecutionContext(
                    CallStack,
                    methodCall.Signature, 
                    methodCall.Definition,
                    enableStaticFields,
                    firstExecutionContextCreating);
                if (firstExecutionContextCreating)
                {
                    firstExecutionContextCreating = false;
                }

                if (methodCall.Definition.HasReturnType)
                {
                    context.Frame.Push(
                        new SymbolicSlot(
                            new SymbolicReference(
                                new MethodReturnSource(methodCall.Definition))));
                }

                var mode = ReturnValueApplyingMode.Replace;
                
                // execute this one and all overriden methods that have body
                if (methodCall.Definition.HasBody)
                {
                    var summary = GetOrCreateSummary(new MethodCall(
                        methodCall.Signature,
                        methodCall.Definition,
                        null,
                        CallKind.Concrete));

                    result.Stat.EmulatedMethodCallCount++;
                    context.Apply(summary, context.Arguments.Slots, result, mode, Indent());
                    mode = ReturnValueApplyingMode.Merge;
                }

                var types = GetRegisteredTypes(methodCall.Definition.DeclaringType.ScopeType.ToString());
                var implementations = types.Count == 0
                    ? indexDb.GetImplementations(methodCall.Signature, entryPointAssemblyName)
                    : indexDb.GetImplementations(methodCall.Signature, types);
                
                foreach (var methodDef in implementations)
                {
                    var summary = GetOrCreateSummary(new MethodCall(
                        methodDef.CreateMethodUniqueSignature(),
                        methodDef,
                        null,
                        CallKind.Concrete));

                    result.Stat.EmulatedMethodCallCount++;
                    context.Apply(summary, context.Arguments.Slots, result, mode, Indent());
                    mode = ReturnValueApplyingMode.Merge;
                }

                if (mode == ReturnValueApplyingMode.Replace)
                {
                    Debug.Fail("We must call Analyze() only if we have at least one implementation!");
                    return Summary.Empty;
                }
                
                // emulate ret opcode
                if (methodCall.Definition.HasReturnType)
                {
                    var value = context.Frame.Pop();
                    if (!Interpreter.IsSimple(methodCall.Definition.ReturnType) &&
                        !value.IsConstAfterSimplification())
                    {
                        context.AddReturnValue(value);    
                    }
                }

                return context.ToSummary();
            }
        }

        private void BuildExecutionContext(ExecutionContext executionContext, MethodCall method)
        {
            var methodDef = method.Definition;
            var interpreter = new Interpreter(executionContext, methodDef);
            //var interpreter = new TraceInterpreter(method);
            foreach (var effect in interpreter.EnumerateEffects())
            {
                switch (effect)
                {
                    case CtorCallEffect ctorCall:
                        if (ctorCall.Definition != null)
                        {
                            // can be null for array creating instructions, e.g., JavaScriptSerializer
                            // System.Web.Configuration.HealthMonitoringSectionHelper::.ctor()
                            // IL_00A5: newobj System.Void System.Collections.ArrayList[0...,0...]::.ctor(System.Int32,System.Int32)
                            Debug.Assert(!ctorCall.Definition.DeclaringType.IsAbstract);
                            Debug.Assert(!ctorCall.Definition.DeclaringType.IsInterface);
                            RegisterType(ctorCall.Definition.DeclaringType);
                        }
                        
                        Handle(ctorCall, executionContext);
                        break;    
                    case MethodCallEffect methodCall:
                        // if (method.Definition.Name == "ParseDictionary")
                        // {
                        //     int tt = 777;
                        // }
                        
                        if (methodCall.CallKind == CallKind.Virtual)
                        {
                            //RegisterVirtualCall(methodCall.Signature, method.Signature);
                        }
                        
                        Handle(methodCall, executionContext);
                        break;
                }
            }
        }

        private void Handle(MethodCallEffect methodCall, ExecutionContext executionContext)
        {
            executionContext.MethodCallCount++;
            result.Stat.MethodCallCount++;
            if (methodCall.Signature == taintedCall)
            {
                if (!inputTaintedMode || methodCall.Parameters.Any(slot => slot.IsInput()))
                {
                    // mark returned values as tainted
                    // TODO: ref counter for tainted obj
                    Debug.Assert(methodCall.OutputSlots.Count == 1, 
                        "for current tainted call this value always 1. Need to test other cases.");
                    for (var i = 0; i < methodCall.OutputSlots.Count; i++)
                    {
                        var (id, backwardCallStack) = result.AddTaintedMethodCall(methodCall.Signature, 
                            CallStack);
                        methodCall.OutputSlots[i].MarkTaint(id, backwardCallStack);
                    }
                }
                else if (inputTaintedMode)
                {
                    for (int i = 0; i < methodCall.Parameters.Length; i++)
                    {
                        var parameter = methodCall.Parameters[i];
                        parameter.AddSinkMethod(executionContext.Signature, 
                            CallStack, methodCall.OutputSlots);
                        
                    }
                }

                result.Stat.IgnoredTargetMethodCalls++;
                return;
            }

            var isAnalyzing = IsAnalyzingNeeded(methodCall);
            AddTargetMethodIfNeeded(methodCall, isAnalyzing);
            if (!isAnalyzing)
            {
                return;
            }

            var summary = GetOrCreateSummary(methodCall);
            executionContext.Apply(summary, methodCall.Parameters, result, ReturnValueApplyingMode.Replace, Indent());
        }

        private void AddTargetMethodIfNeeded(MethodCallEffect methodCall, bool isAnalyzing)
        {
            if (methodCall.Parameters.Length == 0)
            {
                return;
            }
            
            ///////////////////////////////////////////////////////
            // TODO: ADD support for Delegate Invoke() call
/*
            if ((methodCall.IsVirtual && methodCall.Definition.IsVirtual) || 
                (methodCall.Definition == null || !methodCall.Definition.HasBody
                    ||methodCall.Definition.Name == "SideEffect") &&
                     list.IsValid(methodCall.Signature))
            {
                for (int i = 0; i < methodCall.Parameters.Length; i++)
                {
                    var parameter = methodCall.Parameters[i];
                    parameter.AddTargetMethod(methodCall.Signature, result);
                }
            }
            
            return;
*/
            /////////////////////////////////////////////////////// 

            if (!isAnalyzing)
            {
                AddTargetMethodForAllParameters(methodCall);
            }
            else if (methodCall.Definition != null && 
                     methodCall.CallKind == CallKind.Virtual && methodCall.Definition.IsVirtual)
            {
                var implMethodsCount = indexDb.GetImplementationsCount(methodCall.Signature, entryPointAssemblyName);
                
                // add target methods only if we have several implementation
                if ((methodCall.Definition.HasBody && implMethodsCount > 0) ||
                    (!methodCall.Definition.HasBody && implMethodsCount > 1))
                {
                    methodCall.Parameters[0].AddTargetMethod(methodCall.Signature, CallStack, result);
                }
            }
            else if (methodCall.Definition == null || !methodCall.Definition.HasBody ||
                     methodCall.Definition.Name == "SideEffect")
            {
                AddTargetMethodForAllParameters(methodCall);
            }
            
            void AddTargetMethodForAllParameters(MethodCallEffect methodCallEffect)
            {
                if (list.IsValid(methodCallEffect.Signature))
                {
                    // TODO: need to exclude 'out' params, but leave 'ref'  
                    for (int i = 0; i < methodCallEffect.Parameters.Length; i++)
                    {
                        var parameter = methodCallEffect.Parameters[i];
                        parameter.AddTargetMethod(methodCallEffect.Signature, CallStack, result);
                    }
                }
            }
        }
        
        private bool IsAnalyzingNeeded(MethodCallEffect methodCall)
        {
            // probably we can cache 'empty' (with Input marks if needed) summary for non-analyzing cases
            // to improve performance (need to benchmark it)
            if (methodCall.Definition == null)
            {
                DebugLog($"{Indent()}{methodCall.Signature}: MethodDefinition == null");
                MarkOutputSlotsAsInput(methodCall);
                result.Stat.IgnoredNotImplementedMethodCalls++;
                return false;
            }

            if (methodCall.CallKind == CallKind.Concrete && !methodCall.Definition.HasBody)
            {
                DebugLog($"{Indent()}{methodCall.Signature}: no body");
                MarkOutputSlotsAsInput(methodCall);
                if (methodCall.Definition.HasReturnType)
                {
                    var returnType = methodCall.Definition.ReturnType.TryGetTypeDef();
                    if (returnType != null)
                    {
                        // register types from external methods and delegates
                        RegisterType(returnType);
                    }
                }
                
                result.Stat.IgnoredNotImplementedMethodCalls++;
                return false;
            }
            
            if (methodCall.CallKind == CallKind.Virtual)
            {
                int implMethodsCount;
                var registeredTypes = GetRegisteredTypes(methodCall.Definition.DeclaringType.ScopeType.ToString());
                if (registeredTypes.Count == 0)
                {
                    DebugLog($"{Indent()}{methodCall.Signature}: no created types");
                    //Console.WriteLine($"{methodCall.Signature}: no created types");
                    MarkOutputSlotsAsInput(methodCall);
                    result.Stat.IgnoredNotImplementedMethodCalls++;    // TODO: use another stat counter
                    return false;
                    implMethodsCount = indexDb.GetImplementationsCount(methodCall.Signature); //, entryPointAssemblyName);
                }
                else
                {
                    implMethodsCount = indexDb.GetImplementationsCount(methodCall.Signature, registeredTypes);                    
                }
                
                if (!methodCall.Definition.HasBody && implMethodsCount == 0)
                {
                    DebugLog($"{Indent()}{methodCall.Signature}: no any implementation");
                    //Console.WriteLine($"{Indent()}{methodCall.Signature}: no any implementation");
                    MarkOutputSlotsAsInput(methodCall);
                    result.Stat.IgnoredNotImplementedMethodCalls++;
                    return false;
                }
                
                if (implMethodsCount > virtualCallsLimit) //|| (registeredTypes.Count == 0 && implMethodsCount > 4))
                {
                    // TODO: need to propagate tainted value as well? 
                    //Console.WriteLine($"{methodCall.Signature}: too much implementations ({implMethodsCount})");
                    MarkOutputSlotsAsInput(methodCall);
                    result.Stat.IgnoredVirtualMethodCallsByLimit++;
                    result.Stat.IgnoredVirtualMethodsByLimit.Add(methodCall.Signature.ToString());
                    return false;
                }
            }

            if (indexDb.SkippedModules.Contains(methodCall.Definition.Module.FullName))
            {
                Console.WriteLine($"{methodCall.Signature}: in the skipped modules");
                MarkOutputSlotsAsInput(methodCall);
                result.Stat.IgnoredNotImplementedMethodCalls++;
                return false;
            }

            return true;
        }

        private void MarkOutputSlotsAsInput(MethodCall methodCall)
        {
            if (!inputTaintedMode) 
                return;
            
            if (methodCall.OutputSlots.Count == 0)
                return;
            
            if (methodCall.Parameters.Any(slot => slot.IsInputOrChildrenAreInput()))
            {
                for (int i = 0; i < methodCall.OutputSlots.Count; i++)
                {
                    var slot = methodCall.OutputSlots[i];
                    slot.MarkInput();
                }
            }
            else
            {
                for (int i = 0; i < methodCall.Parameters.Length; i++)
                {
                    var parameter = methodCall.Parameters[i];
                    parameter.AddPossibleInputTransformMethod(
                        methodCall.Signature,
                        CallStack,
                        methodCall.OutputSlots);
                }
            }
        }
    }
}