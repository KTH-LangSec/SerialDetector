namespace SerialDetector.Analysis.DataFlow.Symbolic
{
    internal class NewArraySource : TaintedValue, ISourceValue
    {
        private readonly string type;

        public NewArraySource(string type)
        {
            this.type = type;
        }

        public override string ToString() => $"Array[{type}]";
    }
}