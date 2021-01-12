using SerialDetector.Analysis.DataFlow.Symbolic;

namespace SerialDetector.Analysis.DataFlow.Context
{
    internal class ArgumentContext
    {
        private readonly int count;
        private readonly SymbolicReference root;
        private readonly SymbolicSlot[] slots;

        public ArgumentContext(MethodUniqueSignature signature, int count, bool markInput)
        {
            this.count = count;
            root = new SymbolicReference();
            slots = new SymbolicSlot[count];
            for (int id = 0; id < count; id++)
            {
                var entity = new SymbolicReference(new ArgumentSource(id, signature));
                if (markInput)
                {
                    entity.MarkInput();
                }
                
                root.StoreField(SymbolicReference.ArgumentPrefix + id, entity);
                slots[id] = new SymbolicSlot(entity);
            }
        }
        
        public void Store(int id, SymbolicSlot newSlot)
        {
            if (newSlot.IsConstAfterSimplification())
                return;
            
            slots[id] = SymbolicSlot.Merge(slots[id], newSlot);
        }

        public SymbolicSlot Load(int id, string name) =>
            slots[id];

        public SymbolicSlot[] Slots => slots;
        public SymbolicReference[] Entities
        {
            get
            {
                var entities = new SymbolicReference[count];
                for (int id = 0; id < count; id++)
                {
                    entities[id] = root.LoadField(SymbolicReference.ArgumentPrefix + id);

                }
                
                return entities;
            }
        }
    }
}