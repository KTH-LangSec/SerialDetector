using dnlib.DotNet;
using SerialDetector.Analysis.DataFlow.Symbolic;

namespace SerialDetector.Analysis.DataFlow
{
    internal enum CallKind
    {
        Concrete,
        Virtual
    }
    
    internal interface IMethodCall
    {
        CallKind CallKind { get; }
        MethodUniqueSignature Signature { get; }
        MethodDef Definition { get; }
        SymbolicSlot[] Parameters { get; }
    }
}