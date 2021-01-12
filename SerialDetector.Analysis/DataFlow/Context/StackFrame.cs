using System;
using System.Collections.Generic;
using SerialDetector.Analysis.DataFlow.Symbolic;

namespace SerialDetector.Analysis.DataFlow.Context
{
    internal sealed class StackFrame
    {
        private readonly List<SymbolicSlot> slots = new List<SymbolicSlot>();

        public StackFrame()
        {
        }

        private StackFrame(List<SymbolicSlot> slots)
        {
            this.slots = slots;
        }

        public void Push(SymbolicSlot value)
        {
            slots.Add(value);
        }

        public SymbolicSlot Pop()
        {
            var lastIndex = slots.Count - 1;
            var value = slots[lastIndex];
            slots.RemoveAt(lastIndex);
            return value;
        }

        public SymbolicSlot Peek()
        {
            var lastIndex = slots.Count - 1;
            return slots[lastIndex];
        }

        public int Count => slots.Count;

        public (StackFrame, StackFrame) Fork()
        {
            var newSlots = new List<SymbolicSlot>(slots.Count);
            for (int i = 0; i < slots.Count; i++)
            {
                newSlots.Add(slots[i]);
            }
            return (new StackFrame(newSlots), this);
        }

        public static StackFrame Merge(StackFrame firstFrame, StackFrame secondFrame)
        {
            if (firstFrame.slots.Count != secondFrame.slots.Count)
            {
                throw new Exception("Not support merging StackFrames with different slot count");
            }
            
            var newSlots = new List<SymbolicSlot>(firstFrame.slots.Count);
            for (int i = 0; i < firstFrame.slots.Count; i++)
            {
                newSlots.Add(SymbolicSlot.Merge(firstFrame.slots[i], secondFrame.slots[i]));
            }
            
            return new StackFrame(newSlots);
        }
    }
}