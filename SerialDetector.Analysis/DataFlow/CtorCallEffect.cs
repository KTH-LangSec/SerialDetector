using dnlib.DotNet;
using SerialDetector.Analysis.DataFlow.Symbolic;

namespace SerialDetector.Analysis.DataFlow
{
    internal sealed class CtorCallEffect : MethodCallEffect
    {
        public CtorCallEffect(IMethod method, 
            SymbolicSlot[] symbolicParameters, 
            SymbolicSlot symbolicRetSlot) 
            :base(method, symbolicParameters, symbolicRetSlot, CallKind.Concrete)
        {
        }
    }
}