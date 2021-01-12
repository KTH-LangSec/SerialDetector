using System;
using System.Collections.Immutable;
using System.Diagnostics;
using dnlib.DotNet;
using SerialDetector.Analysis.DataFlow.Symbolic;
using static SerialDetector.Analysis.DataFlow.Symbolic.SymbolicReference;

namespace SerialDetector.Analysis.DataFlow.Context
{
    internal sealed class ExecutionContext
    {
        private readonly MethodDef method;
        private readonly bool enableStaticFields;
        //private static uint fakeSymbolicValueCounter = 0;

        public static SymbolicSlot FakeSymbolicValue =>
            new SymbolicSlot(new SymbolicReference(new ConstSource("FAKE")));//$"FAKE {fakeSymbolicValueCounter++}")));
        
        // fix null to avoid store any values
        private static uint symbolicNullCounter = 0;
        public static SymbolicSlot SymbolicNull =>
            new SymbolicSlot(new SymbolicReference(new ConstSource($"NULL {symbolicNullCounter++}")));

        private readonly SymbolicReference staticEntity;

        private SymbolicSlot returnValue;

        public ExecutionContext(ImmutableStack<string> callStack, MethodUniqueSignature signature, MethodDef method, 
            bool enableStaticFields, bool markInputArguments)
        {
            this.method = method;
            this.enableStaticFields = enableStaticFields;
            CallStack = callStack;
            Signature = signature;
            
            staticEntity = new SymbolicReference();
            Static = new SymbolicSlot(staticEntity);

            var countArguments = method.GetParameterCount();
            Arguments = new ArgumentContext(signature, countArguments, markInputArguments);
            InstructionCount = method.HasBody ? (double)method.Body.Instructions.Count : 0;
        }
        
        public double MethodCallCount { get; set; }
        public double InstructionCount { get; private set; }

        public ImmutableStack<string> CallStack { get; }
        public MethodUniqueSignature Signature { get; }

        public SymbolicSlot Static { get; }
        
        public ArgumentContext Arguments { get; }
        
        public VariableContext Variables { get; } = new VariableContext();
        
        public StackFrame Frame { get; set; } = new StackFrame();

        // for return, continue, break under the if-statement
        public bool SkipMode { get; set; }

        public void AddReturnValue(SymbolicSlot value)
        {
            if (returnValue == null)
                returnValue = value;
            else
                returnValue = SymbolicSlot.Merge(returnValue, value);
        }
        
        //public void Dump(string path) => new Summary(method, staticEntity, Arguments.Entities, returnValue).Dump(path);
        
        public Summary ToSummary()
        {
            // TODO: OPTIMIZATION if slot is not materialized, we can ignore it and just remove
            var arguments = Arguments.Entities;
            if (method.Parameters.Count != arguments.Length)
            {
                throw new ArgumentException("method.Parameters.Count != arguments.Length");
            }

            //return new Summary(method, staticEntity, arguments, returnValue?.MergeEntities());
            
            // Optimizing
            var context = new SymbolicReference.VisitingContext();
            
            // Add roots to the context to avoid removing it
            context.Visit(staticEntity);
            for (var i = 0; i < arguments.Length; i++)
            {
                var argument = arguments[i];
                context.Visit(argument);
            }

            SymbolicReference returnEntity = null;
            if (returnValue != null)
            {
                returnEntity = returnValue.MergeEntities();
                context.Visit(returnEntity);
            }

            // Remove empty entities
            staticEntity.RemoveEmptyEntities(context);
            for (var i = 0; i < arguments.Length; i++)
            {
                var argument = arguments[i];
                argument.RemoveEmptyEntities(context);
            }

            returnEntity?.RemoveEmptyEntities(context);

            // Remove non-reachable possible input/tainted entities
            //OptimizePossibleTaintedEntities(context, arguments);
            
            // Create summary
            return new Summary(Signature, staticEntity, arguments, returnEntity, 
                MethodCallCount, InstructionCount);
        }
        
        public void Apply(Summary summary, SymbolicSlot[] parameters, DataFlowAnalysisResult result,
            ReturnValueApplyingMode returnValueApplying, string indent = "")
        {
            //return;
            if (summary.IsEmpty)
                return;

            MethodCallCount += summary.MethodCallCount;
            InstructionCount += summary.InstructionCount;
            
            Debug.Assert(summary.Arguments.Length == parameters.Length);
            if (summary.Arguments.Length != parameters.Length)
            {
                Console.WriteLine("Skip applying summary!");
                return;
            }
            
            //Console.WriteLine($"{indent}{summary.Signature} APPLYING...");
            result.Stat.StartSummaryApplying(summary.Signature.ToString());
            
            // Match
            var context = new SymbolicReference.ApplyingContext(CallStack);
            Static.Match(context, summary.Static);
            for (int i = 0; i < parameters.Length; i++)
            {
                parameters[i].Match(context, summary.Arguments[i]);
            }

            // Apply
            if (enableStaticFields)
            {
                staticEntity.Apply(context, summary.Static, result);
            }
            
            for (int i = 0; i < parameters.Length; i++)
            {
                parameters[i].Apply(context, summary.Arguments[i], result);
            }

            if (summary.ReturnValue != null)
            {
                var matchedReturnValue = new SymbolicSlot(summary.ReturnValue.GetMatchedValue(context, result));
                switch (returnValueApplying)
                {
                    case ReturnValueApplyingMode.Replace:
                        Frame.Pop();
                        Frame.Push(matchedReturnValue);
                        break;
                    case ReturnValueApplyingMode.Merge:
                        var returnedSlot = Frame.Pop();
                        Frame.Push(SymbolicSlot.Merge(returnedSlot, matchedReturnValue));
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }

            //Console.WriteLine($"Apply({summary.OutScopeMap.Count}): {timer.ElapsedMilliseconds} ms");
/*
            Debug.Assert(Static.entities.Length == 1);
            foreach (var f in Static.entities[0].fields)
            {
                if (f.Value.source is ArgumentSource arg)
                {
                    Debug.Assert(arg.signature == Signature);
                }
            }
*/

            result.Stat.StopSummaryApplying(method.CreateMethodUniqueSignature().ToString(), summary);
            result.Stat.AppliedSummaryCount++;
        }
        
        public override string ToString() => Signature.ToString();

        private void OptimizePossibleTaintedEntities(SymbolicReference.VisitingContext markedEntities, SymbolicReference[] arguments)
        {
            throw new NotImplementedException("Must be re-implemented by VisitingContext changes!");
            var context = new SymbolicReference.VisitingContext();
            staticEntity.OptimizePossibleTaintedEntities(context, markedEntities);
            for (var i = 0; i < arguments.Length; i++)
            {
                var argument = arguments[i];
                argument.OptimizePossibleTaintedEntities(context, markedEntities);
            }

            returnValue?.OptimizePossibleTaintedEntities(context, markedEntities);
        }
    }
}