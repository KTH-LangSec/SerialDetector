using System.Collections;
using System.Collections.Generic;
using SerialDetector.Analysis.DataFlow.Symbolic;

namespace SerialDetector.Analysis.DataFlow.Context
{
    internal abstract class SlotsContext<T> : IEnumerable<(T, SymbolicSlot)>
    {
        protected readonly Dictionary<T, SymbolicSlot> Slots = new Dictionary<T, SymbolicSlot>();

        public abstract SymbolicSlot Load(T id, string name = null);
        
        public void Store(T id, SymbolicSlot newSlot)
        {
            if (newSlot.IsConstAfterSimplification())
               return;
            
            if (Slots.TryGetValue(id, out var existedSlot))
            {
                Slots[id] = SymbolicSlot.Merge(existedSlot, newSlot);
                return;
            }
            
            Slots.Add(id, newSlot);            
        }
        
        public IEnumerator<(T, SymbolicSlot)> GetEnumerator()
        {
            foreach (var pair in Slots)
            {
                yield return (pair.Key, pair.Value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}