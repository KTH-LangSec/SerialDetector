namespace SerialDetector.Analysis.DataFlow.Symbolic
{
    internal sealed class UninitializedValueSource : TaintedValue, ISourceValue
    {
        public UninitializedValueSource(string errorMessage)
        {
            // TODO: need support ldloca for input propagation
            // System.Object System.RuntimeType::CreateInstanceSlow(System.Boolean,System.Boolean,System.Boolean,System.Threading.StackCrawlMark&)
            ErrorMessage = errorMessage;
        }
        
        public string ErrorMessage { get; }

        public override string ToString() => $"Uninitialized[{ErrorMessage}]";
    }
}