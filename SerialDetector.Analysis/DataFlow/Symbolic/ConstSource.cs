namespace SerialDetector.Analysis.DataFlow.Symbolic
{
    internal sealed class ConstSource : TaintedValue, ISourceValue
    {
        public ConstSource(object value)
        {
            Value = value;
        }
        
        public object Value { get; }

        public override string ToString() => $"Const[{Value}]";
    }
}