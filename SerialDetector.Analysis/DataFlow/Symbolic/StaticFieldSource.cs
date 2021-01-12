namespace SerialDetector.Analysis.DataFlow.Symbolic
{
    internal sealed class StaticFieldSource : TaintedValue, ISourceValue
    {
        public StaticFieldSource(string name)
        {
            Name = name;
        }
        
        public string Name { get; }

        public override string ToString() => $"Static[{Name}]";
    }
}