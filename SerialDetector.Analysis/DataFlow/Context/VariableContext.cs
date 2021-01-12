using SerialDetector.Analysis.DataFlow.Symbolic;

namespace SerialDetector.Analysis.DataFlow.Context
{
    internal sealed class VariableContext : SlotsContext<int>
    {
        public override SymbolicSlot Load(int id, string name = null)
        {
            if (!Slots.TryGetValue(id, out var slot))
            {
                slot = new SymbolicSlot(new SymbolicReference(new UninitializedValueSource($"Var {id}")));
                Slots.Add(id, slot);
            }

            return slot;
        }
    }
}