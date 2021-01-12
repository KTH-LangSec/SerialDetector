using System.Collections.Generic;

namespace SerialDetector.Analysis.DataFlow
{
    internal interface IPatternCache
    {
        ulong GetNewTaintedIndex();
        
        Dictionary<ulong, TaintedSourceInfo> Patterns { get; } 
    }
}