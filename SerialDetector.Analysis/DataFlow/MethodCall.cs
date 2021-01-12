using System.Collections.Generic;
using dnlib.DotNet;
using SerialDetector.Analysis.DataFlow.Symbolic;

namespace SerialDetector.Analysis.DataFlow
{
    internal class MethodCall : IMethodCall
    {
        public MethodCall(MethodUniqueSignature signature, MethodDef methodDefinition, 
            SymbolicSlot[] parameters, CallKind callKind)
        {
            Signature = signature;
            Definition = methodDefinition;
            Parameters = parameters;
            CallKind = callKind;
        }

        public CallKind CallKind { get; }
        public MethodUniqueSignature Signature { get; }
        public MethodDef Definition { get; }
        public SymbolicSlot[] Parameters { get; }
        public List<SymbolicSlot> OutputSlots { get; protected set; }
        
        public override string ToString() => Signature.ToString();

        /*        
        public string ToFormula()
        {
            var formula = new StringBuilder();
            formula.Append(Definition.DeclaringType2?.FullName);
            formula.Append("::");
            formula.Append(Definition.Name);
            //formula.Append(Definition.CreateMethodUniqueSignature());
            formula.Append("(");
            foreach (var parameter in Parameters)
            {
                formula.Append(parameter.IsTainted() ? "T, " : "N, ");
            }

            formula.Length -= 2;
            formula.Append(")");
            if (IsVirtual)
            {
                formula.Append(":virt");
            }
            
            return formula.ToString();
        }
*/
    }
}