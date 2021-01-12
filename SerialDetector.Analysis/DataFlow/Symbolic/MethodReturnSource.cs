using dnlib.DotNet;

namespace SerialDetector.Analysis.DataFlow.Symbolic
{
    internal sealed class MethodReturnSource : TaintedValue, ISourceValue
    {
        private readonly IMethod method;

        public MethodReturnSource(IMethod method)
        {
            this.method = method;
        }
        
        public override string ToString() => $"{(IsTainted() ? "T:" : "")}MethodReturn[{method.FullName}]";
    }
}