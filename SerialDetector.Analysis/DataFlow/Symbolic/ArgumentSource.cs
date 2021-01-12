namespace SerialDetector.Analysis.DataFlow.Symbolic
{
    internal sealed class ArgumentSource : TaintedValue, ISourceValue
    {
        private readonly MethodUniqueSignature signature;

        public ArgumentSource(int index, MethodUniqueSignature signature)
        {
            this.signature = signature;
            Index = index;
        }
        
        public int Index { get; }

        public override string ToString() => $"Arg{Index}";
    }
}