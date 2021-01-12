using System.Collections.Generic;
using dnlib.DotNet;
using SerialDetector.Analysis.DataFlow.Symbolic;

namespace SerialDetector.Analysis.DataFlow
{
    internal class MethodCallEffect : MethodCall, IEffect
    {
        public MethodCallEffect(
            IMethod method, 
            SymbolicSlot[] symbolicParameters, SymbolicSlot symbolicRetSlot,
            CallKind callKind)
            : base(method.CreateMethodUniqueSignature(), method.ResolveMethodDef(), symbolicParameters, callKind)
        {
            OutputSlots = new List<SymbolicSlot>(method.MethodSig.Params.Count + 1); 
            if (symbolicRetSlot != null)
                OutputSlots.Add(symbolicRetSlot);

            foreach (var index in method.EnumerateOutputParameterIndexes())
            {
                OutputSlots.Add(symbolicParameters[index]);
            }
        }
    }
}