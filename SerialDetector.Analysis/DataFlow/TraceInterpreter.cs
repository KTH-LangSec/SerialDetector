using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SerialDetector.Analysis.DataFlow.Symbolic;

namespace SerialDetector.Analysis.DataFlow
{
    internal sealed class TraceInterpreter : IInterpreter
    {
        private readonly MethodDef method;

        public TraceInterpreter(MethodDef method)
        {
            this.method = method;
        }
        
        public IEnumerable<IEffect> EnumerateEffects()
        {
            for (var index = 0; index < method.Body.Instructions.Count; index++)
            {
                var instruction = method.Body.Instructions[index];
                switch (instruction.OpCode.Code)
                {
                    case Code.Call:
                        yield return BuildMethodCallEffect(instruction, CallKind.Concrete);
                        break;
                    case Code.Callvirt:
                        yield return BuildMethodCallEffect(instruction, CallKind.Virtual);
                        break;
                    case Code.Newobj:
                        yield return BuildCtorCallEffect(instruction);
                        break;
                }
            }
        }
        
        private MethodCallEffect BuildMethodCallEffect(Instruction instruction, CallKind callKind)
        {
            var methodOperand = (IMethod) instruction.Operand;
            var parameterCount = methodOperand.GetParameterCount();
            var symbolicParameters = new SymbolicSlot[parameterCount];
            for (int i = parameterCount - 1; i >= 0; i--)
            {
                symbolicParameters[i] = new SymbolicSlot(new SymbolicReference());
            }
                    
            var signature = methodOperand.MethodSig;
            SymbolicSlot symbolicRetSlot = null;
            if (!IsSystemVoid(signature.RetType))
            {
                symbolicRetSlot = new SymbolicSlot(
                    new SymbolicReference(
                        new MethodReturnSource(methodOperand)));
            }

            return new MethodCallEffect(
                methodOperand,
                symbolicParameters,
                symbolicRetSlot,
                callKind);
        }
        
        private MethodCallEffect BuildCtorCallEffect(Instruction instruction)
        {
            var methodOperand = (IMethod) instruction.Operand;
            var signature = methodOperand.MethodSig;
            var parameterCount = methodOperand.GetParameterCount();
            var symbolicParameters = new SymbolicSlot[parameterCount];
            var lastParameter = signature.ImplicitThis ? 1 : 0;
            for (int i = parameterCount - 1; i >= lastParameter; i--)
            {
                symbolicParameters[i] = new SymbolicSlot(new SymbolicReference());
            }

            var thisReturnValue = 
                new SymbolicSlot(
                    new SymbolicReference(
                        new MethodReturnSource(methodOperand)));
            if (signature.ImplicitThis)
            {
                symbolicParameters[0] = thisReturnValue;
            }
                    
            return new MethodCallEffect(
                methodOperand,
                symbolicParameters,
                thisReturnValue,
                CallKind.Concrete);
        }
        
        private static bool IsSystemVoid(TypeSig type) => 
            type.RemovePinnedAndModifiers().GetElementType() == ElementType.Void;
    }
}