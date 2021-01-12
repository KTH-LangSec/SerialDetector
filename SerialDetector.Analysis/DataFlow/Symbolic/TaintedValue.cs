namespace SerialDetector.Analysis.DataFlow.Symbolic
{
    internal abstract class TaintedValue : ITaintedEntity
    {
        protected bool isTainted;
        public void MarkTaint()
        {
            isTainted = true;
        }

        public bool IsTainted() => isTainted;
    }
}