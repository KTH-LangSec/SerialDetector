namespace SerialDetector.Analysis.DataFlow.Symbolic
{
    internal interface ITaintedEntity
    {
        void MarkTaint();
        bool IsTainted();
    }
}