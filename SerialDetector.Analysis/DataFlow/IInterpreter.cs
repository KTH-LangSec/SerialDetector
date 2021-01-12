using System.Collections.Generic;

namespace SerialDetector.Analysis.DataFlow
{
    public interface IInterpreter
    {
        IEnumerable<IEffect> EnumerateEffects();
    }
}