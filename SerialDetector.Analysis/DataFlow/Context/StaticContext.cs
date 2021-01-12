using SerialDetector.Analysis.DataFlow.Symbolic;

namespace SerialDetector.Analysis.DataFlow.Context
{
    internal sealed class StaticContext : SlotsContext<string>
    {
        public override SymbolicSlot Load(string id, string name = null)
        {
            if (!Slots.TryGetValue(id, out var slot))
            {
                slot = new SymbolicSlot(new SymbolicReference(new StaticFieldSource(id)));
                Slots.Add(id, slot);
                return slot;
            }

            return slot;
        }
    }
}