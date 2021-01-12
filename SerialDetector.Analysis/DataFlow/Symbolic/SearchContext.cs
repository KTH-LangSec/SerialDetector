using System.Collections.Generic;

namespace SerialDetector.Analysis.DataFlow.Symbolic
{
    internal sealed class SearchContext
    {
        public HashSet<SymbolicReference> CheckedEntities { get; } =
            new HashSet<SymbolicReference>();
    }
}