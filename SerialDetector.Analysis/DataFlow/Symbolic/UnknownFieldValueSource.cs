using System.Collections.Generic;

namespace SerialDetector.Analysis.DataFlow.Symbolic
{
    internal sealed class UnknownFieldValueSource : TaintedValue, ISourceValue
    {
        public UnknownFieldValueSource(ISourceValue source, string name)
        {
            Source = source;
            Name = name;
        }
        
        public ISourceValue Source { get; }
        public string Name { get; }

        public IEnumerable<ISourceValue> EnumerateFields()
        {
/*
            var current = this;
            while (true)
            {
                yield return current;
                if (!(current.Source is UnknownFieldValueSource unknownFieldValueSource))
                {
                    break;
                }

                current = unknownFieldValueSource;
            }
*/
            
            ISourceValue current = this;
            while (current != null)
            {
                yield return current;
                current = current is UnknownFieldValueSource unknownFieldValueSource
                    ? unknownFieldValueSource.Source
                    : null;
            }
        }

        public override string ToString() => $"{Source}.{Name}";
    }
}