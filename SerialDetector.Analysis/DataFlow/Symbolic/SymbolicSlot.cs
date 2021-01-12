using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using static SerialDetector.Analysis.DataFlow.Symbolic.SymbolicReference;

namespace SerialDetector.Analysis.DataFlow.Symbolic
{
    internal sealed class SymbolicSlot
    {
        private SymbolicReference[] entities;
        private SymbolicSlot[] values;
        private string[] fields;

        public SymbolicSlot(SymbolicReference entity)
        {
            entities = new [] {entity};
        }
        
        private SymbolicSlot()
        {
        }
        
        private SymbolicSlot(SymbolicReference[] entities, SymbolicSlot[] values, string[] fields)
        {
            Reset(entities, values, fields);
        }

        public static SymbolicSlot Merge(SymbolicSlot first, SymbolicSlot second)
        {
            if (first == second)
                return first;
            
            var result = new SymbolicSlot();
            Merge(first, second, result);
            return result;
        }

        public bool IsConstAfterSimplification()
        {
            if (entities != null && entities.Length > 0)
            {
                var constantsCount = 0;
                for (int i = 0; i < entities.Length; i++)
                {
                    if (entities[i].IsConst)
                        constantsCount++;
                }

                if (constantsCount == entities.Length)
                {
                    if (entities.Length == 1)
                        return true;
                    
                    entities = new[] {entities[0]};
                    return true;
                }

                var newEntities = new SymbolicReference[entities.Length - constantsCount];
                var newIndex = 0;
                for (int i = 0; i < entities.Length; i++)
                {
                    if (!entities[i].IsConst)
                    {
                        newEntities[newIndex++] = entities[i]; 
                    }
                }

                entities = newEntities;
            }

            return false;
        }
        
        /*
        public bool IsSimpleConst()
        {
            if (entities != null)
            {
                // all or none must be const
                var anyConst = false;
                var allConst = true;
                foreach (var entity in entities)
                {
                    if (entity.IsConst)
                        anyConst = true;
                    else
                        allConst = false;
                }

                if (anyConst)
                {
                    if (!allConst)
                        throw new Exception("All most be const if any entity is const");

                    return true;
                }
            }

            return false;
        }
        */


        public bool ContainsNotConst()
        {
            // TODO: PERF optimize to avoid fields materializing
            MaterializeFields();
            for (int i = 0; i < entities.Length; i++)
            {
                if (!entities[i].IsConst)
                    return true;
            }

            return false;
        }

        // store by address
        public void Store(SymbolicSlot value)
        {
            if (IsConstAfterSimplification() || value.IsConstAfterSimplification())
                return;
            
            // store like a field because we should create a snapshot in the moment 
            // and avoid cycled references in the "values" field
            MaterializeFields();
            value.MaterializeFields();

            if (IsConstAfterSimplification() || value.IsConstAfterSimplification())
                return;

            var mergedEntity = SymbolicReference.Merge(entities, value.entities);
            entities = new[] {mergedEntity};
            value.entities = new[] {mergedEntity};
        }

        public SymbolicSlot LoadField(string name)
        {
            string[] newFields;
            if (fields == null)
            {
                newFields = new[] {name};
            }
            else
            {
                Debug.Assert(fields.Length > 0);
                newFields = new string[fields.Length + 1];
                Array.Copy(fields, newFields, fields.Length);
                newFields[newFields.Length - 1] = name;
            }
            
            return new SymbolicSlot(entities, values, newFields);
        }

        public void StoreField(string name, SymbolicSlot value)
        {
            if (IsConstAfterSimplification() || value.IsConstAfterSimplification())
                return;
            
            MaterializeFields();
            value.MaterializeFields();
            
            if (IsConstAfterSimplification() || value.IsConstAfterSimplification())
                return;

            var targetEntity = SymbolicReference.Merge(value.entities); 
            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                entity.StoreField(name, targetEntity);
            }
        }

        public void AddSinkMethod(MethodUniqueSignature method, IEnumerable<string> callStack,
            List<SymbolicSlot> outputSlots)
        {
            return;
            MaterializeFields();
            for (int i = 0; i < outputSlots.Count; i++)
            {
                var outputSlot = outputSlots[i];
                if (outputSlot == this)
                    continue;

                outputSlot.MaterializeFields();
                for (int j = 0; j < entities.Length; j++)
                {
                    var entity = entities[j];
                    entity.AddSinkMethod(method, callStack, outputSlot.entities);
                }
            }
        }

        public void AddTargetMethod(MethodUniqueSignature method, ImmutableStack<string> callStack,
            DataFlowAnalysisResult result)
        {
            if (IsConstAfterSimplification())
                return;
            
            MaterializeFields(assert: false);
            if (IsConstAfterSimplification())
                return;

            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                if (!entity.IsConst)
                {
                    entity.AddTargetMethod(method, callStack, result);
                }
            }
        }

        public void AddPossibleInputTransformMethod(MethodUniqueSignature method, IEnumerable<string> callStack,
            List<SymbolicSlot> outputSlots)
        {
            return;
            MaterializeFields();
            for (int i = 0; i < outputSlots.Count; i++)
            {
                var outputSlot = outputSlots[i];
                if (outputSlot == this)
                    continue;
                
                outputSlot.MaterializeFields();
                for (int j = 0; j < entities.Length; j++)
                {
                    var entity = entities[j];
                    entity.AddPossibleInputTransformMethod(method, callStack, outputSlot.entities);
                }
            }
        }
        
        private static void Merge(SymbolicSlot first, SymbolicSlot second, SymbolicSlot result)
        {
            if (first.fields == null && second.fields == null)
            {
                if (first.entities != null && second.entities != null)
                {
                    Debug.Assert(first.entities.Length > 0 && second.entities.Length > 0);
                    Debug.Assert(first.values == null && second.values == null);
                    
                    var newEntities = new SymbolicReference[first.entities.Length + second.entities.Length];
                    Array.Copy(first.entities, newEntities, first.entities.Length);
                    Array.Copy(second.entities, 0, newEntities, first.entities.Length, second.entities.Length);
                    result.Reset(newEntities, null, null);
                    return;
                }

                if (first.entities != null)
                {
                    Debug.Assert(first.entities.Length > 0 && second.entities == null);
                    Debug.Assert(first.values == null && second.values != null);
                    
                    var newValues = new SymbolicSlot[second.values.Length + 1];
                    Array.Copy(second.values, newValues, second.values.Length);
                    newValues[newValues.Length - 1] = new SymbolicSlot(first.entities, null, null);
                    result.Reset(null, newValues, null);
                    return;
                }
                
                if (second.entities != null)
                {
                    Debug.Assert(first.entities == null && second.entities.Length > 0);
                    Debug.Assert(first.values != null && second.values == null);
                    
                    var newValues = new SymbolicSlot[first.values.Length + 1];
                    Array.Copy(first.values, newValues, first.values.Length);
                    newValues[newValues.Length - 1] = new SymbolicSlot(second.entities, null, null);
                    result.Reset(null, newValues, null);
                    return;
                }

                {
                    Debug.Assert(first.entities == null && second.entities == null);
                    Debug.Assert(first.values != null && second.values != null);
                    Debug.Assert(first.values.Length > 0 && second.values.Length > 0);

                    var newValues = new SymbolicSlot[first.values.Length + second.values.Length];
                    Array.Copy(first.values, newValues, first.values.Length);
                    Array.Copy(second.values, 0, newValues, first.values.Length, second.values.Length);
                    result.Reset(null, newValues, null);
                    return;
                }
            }

            if (first.fields == null && first.values != null)
            {
                Debug.Assert(first.entities == null);
                Debug.Assert(first.values.Length > 0);

                var newValues = new SymbolicSlot[first.values.Length + 1];
                Array.Copy(first.values, newValues, first.values.Length);
                newValues[newValues.Length - 1] = second;
                result.Reset(null, newValues, null);
                return;
            }

            if (second.fields == null && second.values != null)
            {
                Debug.Assert(second.entities == null);
                Debug.Assert(second.values.Length > 0);
                
                var newValues = new SymbolicSlot[second.values.Length + 1];
                Array.Copy(second.values, newValues, second.values.Length);
                newValues[newValues.Length - 1] = first;
                result.Reset(null, newValues, null);
                return;
            }

            result.Reset(null, new [] {first, second}, null);
        }
        
        private void Reset(SymbolicReference[] newEntities, SymbolicSlot[] newValues, string[] newFields)
        {
            entities = newEntities;
            values = newValues;
            fields = newFields;
        }

        private void MaterializeFields(bool assert = true)
        {
            if (values != null || fields != null)
            {
                entities = MaterializeFields(this).ToArray();
                values = null;
                fields = null;
            }
            
            // check only if assert == true
            //Debug.Assert(!assert || entities.All(e => !e.IsConst));
        }

        private static List<SymbolicReference> MaterializeFields(SymbolicSlot value)
        {
            var newEntities = new List<SymbolicReference>();
            var queue = new Queue<(SymbolicSlot, string[][])>();
            queue.Enqueue((value, new string[0][]));
//            var context = new VisitingContext<SymbolicSlot>();
            while (queue.Count > 0)
            {
                var (slot, topFields) = queue.Dequeue();
                // if (!context.Visit(slot))
                //     continue;
                
                if (slot.values != null)
                {
                    Debug.Assert(slot.entities == null);

                    string[][] allFields;
                    if (slot.fields != null)
                    {
                        allFields = new string[topFields.Length + 1][];
                        Array.Copy(topFields, allFields, topFields.Length);
                        allFields[allFields.Length - 1] = slot.fields;
                    }
                    else
                    {
                        allFields = topFields;
                    }

                    for (int i = 0; i < slot.values.Length; i++)
                    {
                        queue.Enqueue((slot.values[i], allFields));                        
                    }
                }
                else if (slot.entities != null)
                {
                    Debug.Assert(slot.values == null);
                    for (int i = 0; i < slot.entities.Length; i++)
                    {
                        var entity = slot.entities[i];
                        if (slot.fields != null)
                        {
                            for (int f = 0; f < slot.fields.Length; f++)
                            {
                                var field = slot.fields[f];
                                entity = entity.LoadField(field);
                            }
                        }
                        
                        for (int j = topFields.Length - 1; j >= 0; j--)
                        {
                            var slotFields = topFields[j];
                            for (int f = 0; f < slotFields.Length; f++)
                            {
                                var field = slotFields[f];
                                entity = entity.LoadField(field);
                            }
                        }
                        
                        newEntities.Add(entity);
                    }
                }
                else
                {
                    Debug.Fail("SSlot must contain either values or entities");
                }
            }

            return newEntities;
        }

        public void Match(SymbolicReference.ApplyingContext context, SymbolicReference rootSummaryEntity)
        {
            if (IsConstAfterSimplification())
                return;
            
            MaterializeFields();
            if (IsConstAfterSimplification())
                return;

            //MergeEntities()
            rootSummaryEntity.Match(context, entities[0]);
        }
        
        public SymbolicReference MergeEntities()
        {
            MaterializeFields();
            if (IsConstAfterSimplification())
                return entities[0];

            if (entities.Length > 1)
            {
                var newEntity = SymbolicReference.Merge(entities);
                entities = new[] {newEntity};
            }

            return entities[0];
        }
        
        public void Apply(SymbolicReference.ApplyingContext context, SymbolicReference rootSummaryEntity, DataFlowAnalysisResult result)
        {
            if (IsConstAfterSimplification())
                return;
            
            MaterializeFields();
            if (IsConstAfterSimplification())
                return;
            
            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                entity.Apply(context, rootSummaryEntity, result);
            }
        }
        
        /*
        public void RemoveEmptyEntities(VisitingContext context)
        {
            MaterializeFields();
            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                entity.RemoveEmptyEntities(context);
            }
        }
        */
        
        public void OptimizePossibleTaintedEntities(SymbolicReference.VisitingContext context,
            SymbolicReference.VisitingContext markedEntities)
        {
            MaterializeFields();
            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                entity.OptimizePossibleTaintedEntities(context, markedEntities);
            }
        }

        public bool IsInput()
        {
            MaterializeFields();
            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                if (entity.IsInput())
                    return true;
            }
            
            return false;
        }
        
        public bool IsInputOrChildrenAreInput()
        {
            MaterializeFields();
            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                if (entity.IsInputOrChildrenAreInput())
                    return true;
            }
            
            return false;
        }

        public void MarkInput()
        {
            MaterializeFields();
            for (int i = 0; i < entities.Length; i++)
            {
                var entity = entities[i];
                entity.MarkInput();
            }
        }

        
        public void MarkTaint(ulong id, ImmutableStack<string> callStack)
        {
            // TODO: now we taint the return value of external sensitive sink only
            if (values != null || fields != null || entities == null || entities.Length != 1)
                throw new Exception("Not valid slot for tainted value");

            entities[0].MarkTaint(id, callStack);
        }
        
        public override string ToString()
        {
            if (values == null && entities == null)
            {
                return "EMPTY!!";
            }
            
            var builder = new StringBuilder();
            if (entities != null)
            {
                if (entities.Length > 1)
                    builder.Append("M:");

                builder.Append(entities[0]);
            }

            if (values != null)
            {
                if (values.Length > 1)
                    builder.Append("M:");

                builder.Append("V");
            }

            if (fields != null)
            {
                builder.Append("|");
                for (int i = 0; i < fields.Length; i++)
                {
                    builder.Append(fields[i]);
                    builder.Append(".");
                }

                builder.Length -= 1;
            }
            
            return builder.ToString();
        }
    }
}