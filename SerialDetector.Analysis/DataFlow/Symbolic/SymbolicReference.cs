using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace SerialDetector.Analysis.DataFlow.Symbolic
{
    internal sealed partial class SymbolicReference
    {
        public const string ArrayElement = "$array";
        public const string ArgumentPrefix = "$arg";
        public const string PossibleTaintedMark = "$tainted";
        public const string PossibleInputMark = "$input";
        
        // TODO: do SymbolicEntity private
        internal sealed class SymbolicEntity
        {
            // TODO: do SmartPointer private
            public class SmartPointer
            {
                private ulong revision;
                private SymbolicEntity entity;

                public void Match(ulong version, SymbolicEntity dest)
                {
                    revision = version;
                    entity = dest;
                }

                public bool TryGetMatched(ulong version, out SymbolicEntity dest)
                {
                    if (revision != version)
                    {
                        dest = null;
                        return false;
                    }

                    dest = entity;
                    return true;
                }

                public bool IsMatched(ulong version) => revision == version;

                public bool IsMatchedOrVisitedEver() => revision != 0 || entity != null;
                public bool IsMatchedEver() => entity != null;
            }

            private readonly ISourceValue source;
            private HashSet<(SymbolicEntity, string)> references = new HashSet<(SymbolicEntity, string)>();

            private readonly Dictionary<string, SymbolicEntity> fields = new Dictionary<string, SymbolicEntity>();

            //private readonly List<(MethodUniqueSignature, IEnumerable<string>)> methodCalls = new List<(MethodUniqueSignature, IEnumerable<string>)>();
            private readonly HashSet<(MethodUniqueSignature, ImmutableStack<string>)> methodCalls = new HashSet<(MethodUniqueSignature, ImmutableStack<string>)>();

            private HashSet<SymbolicEntity>
                possibleTaintedEntities; // = new HashSet<SymbolicEntity>();  // if this entity is input value

            private HashSet<SymbolicEntity>
                possibleInputEntities; // = new HashSet<SymbolicEntity>();    // if this entity is input value 

            private ulong taintedSourceId; // 0 - not tainted, 1 - input values, 2+ - tainted created obj
            private ImmutableStack<string> taintedCallStack = ImmutableStack<string>.Empty;

            public SymbolicEntity(SymbolicReference reference)
            {
                BackReference = reference;
            }

            public SymbolicEntity(ISourceValue source, SymbolicReference reference)
            {
                this.source = source;
                BackReference = reference;
            }

            public SymbolicReference BackReference { get; }
            
            public SmartPointer ApplyingReference { get; } = new SmartPointer();
            public SmartPointer MergingReference { get; } = new SmartPointer();

            public bool CheckRefAndFields()
            {
                foreach (var (entity, name) in references)
                {
                    if (!entity.fields.TryGetValue(name, out var existedEntity) || existedEntity != this)
                        return false;
                }

                foreach (var fieldPair in fields)
                {
                    var name = fieldPair.Key;
                    var entity = fieldPair.Value;
                    if (!entity.references.Contains((this, name)))
                        return false;
                }

                return true;
            }
            
            public static void TraverseMerging(MergingContext context, SymbolicEntity rootDest,
                SymbolicEntity rootSource)
            {
                if (rootDest == rootSource)
                    return;

                var stack = new Stack<(SymbolicEntity, SymbolicEntity)>();
                stack.Push((rootDest, rootSource));
                while (stack.Count > 0)
                {
                    var (dest, source) = stack.Pop();
                    Debug.Assert(dest.CheckRefAndFields());
                    Debug.Assert(source.CheckRefAndFields());
                    if (dest == source)
                    {
                        continue;
                    }
                    
                    dest = context.GetMatched(dest);
                    source = context.GetMatched(source);
                    Debug.Assert(dest.CheckRefAndFields());
                    Debug.Assert(source.CheckRefAndFields());
                    if (dest == source)
                    {
                        continue;
                    }

                    context.Match(source, dest);
                    foreach (var methodCall in source.methodCalls)
                    {
                        dest.methodCalls.Add(methodCall);
                    }

                    if (source.taintedSourceId > dest.taintedSourceId)
                    {
                        //Debug.Assert(dest.taintedSourceId == 0 || dest.taintedSourceId == source.taintedSourceId); 
                        // TODO: add array for tainted values
                        dest.taintedSourceId = source.taintedSourceId;
                        dest.taintedCallStack = source.taintedCallStack;
                    }

                    foreach (var (parentRef, parentRefName) in source.references)
                    {
                        Debug.Assert(!parentRef.ApplyingReference.IsMatchedEver());
                        if (parentRef == source)
                        {
                            if (dest.fields.TryGetValue(parentRefName, out var existedField))
                            {
                                stack.Push((dest, existedField));
                            }
                            else
                            {
                                Debug.Assert(!context.IsMatched(dest));
                                ReplaceReference(parentRefName, dest, dest, null);    
                            }
                            
                            // remove the field in the source
                            // to ignore it when we update source.fields
                            source.fields.Remove(parentRefName);
                        }
                        else
                        {
                            Debug.Assert(!context.IsMatched(parentRef));
                            Debug.Assert(!context.IsMatched(dest));
                            ReplaceReference(parentRefName, parentRef, dest, null);
                        }
                    }

                    foreach (var field in source.fields)
                    {
                        var fieldName = field.Key;
                        var fieldValue = field.Value;
                        Debug.Assert(!fieldValue.ApplyingReference.IsMatchedEver());
                        Debug.Assert(!context.IsMatched(fieldValue));
                        if (dest.fields.TryGetValue(fieldName, out var existedField))
                        {
                            Debug.Assert(!existedField.ApplyingReference.IsMatchedEver());
                            Debug.Assert(!context.IsMatched(existedField));
                            if (existedField != fieldValue)
                            {
                                stack.Push((existedField, fieldValue));
                            }
                            
                            fieldValue.references.Remove((source, fieldName));
                        }
                        else
                        {
                            Debug.Assert(!context.IsMatched(dest));
                            Debug.Assert(!context.IsMatched(fieldValue));
                            ReplaceReference(fieldName, dest, fieldValue, source);
                        }
                    }
                    
                    // ???
                    source.references.Clear();
                    source.fields.Clear();

                    if (source.possibleInputEntities != null)
                    {
                        foreach (var entity in source.possibleInputEntities)
                        {
                            ReplaceReference(PossibleInputMark, dest, entity, source);
                        }
                    }

                    if (source.possibleTaintedEntities != null)
                    {
                        foreach (var entity in source.possibleTaintedEntities)
                        {
                            ReplaceReference(PossibleTaintedMark, dest, entity, source);
                        }
                    }
                }
            }

            public bool IsConst => source is ConstSource;

            public SymbolicEntity LoadField(string name)
            {
                Debug.Assert(!IsConst);
                
                if (!fields.TryGetValue(name, out var field))
                {
                    field = new SymbolicReference().entity;
                    if (taintedSourceId == 1)
                    {
                        field.taintedSourceId = 1;
                    }

                    if (possibleInputEntities != null)
                    {
                        foreach (var entity in possibleInputEntities)
                        {
                            ReplaceReference(PossibleInputMark, field, entity, null);
                        }
                    }

                    ReplaceReference(name, this, field, null);
                }

                return field;
            }

            public void StoreField(string name, SymbolicEntity value)
            {
                if (fields.TryGetValue(name, out var field))
                {
                    TraverseMerging(new MergingContext(), field, value);
                }
                else
                {
                    ReplaceReference(name, this, value, null);
                }
            }

            public void AddSinkMethod(MethodUniqueSignature method, IEnumerable<string> callStack,
                SymbolicReference[] returnEntities)
            {
                if (possibleTaintedEntities == null)
                {
                    possibleTaintedEntities = new HashSet<SymbolicEntity>(returnEntities.Length);
                }

                for (int i = 0; i < returnEntities.Length; i++)
                {
                    ReplaceReference(PossibleTaintedMark, this, returnEntities[i].entity, null);
                }
            }

            public void AddTargetMethod(MethodUniqueSignature method, ImmutableStack<string> callStack,
                DataFlowAnalysisResult result)
            {
                if (taintedSourceId > 1)
                {
                    result.AddAttackTriggerCall(taintedSourceId, (method, callStack));
                }
                else
                {
                    methodCalls.Add((method, callStack));
                }
            }

            public void AddPossibleInputTransformMethod(MethodUniqueSignature method, IEnumerable<string> callStack,
                SymbolicReference[] returnEntities)
            {
                if (possibleInputEntities == null)
                {
                    possibleInputEntities = new HashSet<SymbolicEntity>(returnEntities.Length);
                }

                for (int i = 0; i < returnEntities.Length; i++)
                {
                    ReplaceReference(PossibleInputMark, this, returnEntities[i].entity, null);
                }
                
                // TODO: HACK/ simplification for array
                foreach (var field in fields)
                {
                    var name = field.Key;
                    var entity = field.Value;
                    if (name == ArrayElement)
                    {
                        // non recursion call to avoid SO
                        if (entity.possibleInputEntities == null)
                        {
                            entity.possibleInputEntities = new HashSet<SymbolicEntity>(returnEntities.Length);
                        }

                        for (int i = 0; i < returnEntities.Length; i++)
                        {
                            ReplaceReference(PossibleInputMark, entity, returnEntities[i].entity, null);
                        }
                    }
                }
            }

            public void MarkInput()
            {
                if (taintedSourceId == 0)
                {
                    taintedSourceId = 1;
/*                    
                    if (possibleInputEntities != null)
                    {
                        var entities = possibleInputEntities;
                        possibleInputEntities = null;
                        foreach (var entity in entities)
                        {
                            entity.MarkInput();
                        }
                    }
*/                    
                }
            }

            public void MarkTaint(ulong id, ImmutableStack<string> callStack)
            {
                taintedSourceId = id;
                taintedCallStack = callStack;
            }

            public bool IsInput()
            {
                if (taintedSourceId == 1)
                    return true;

                // return true if this entity is array and contains an input element  
                /*
                foreach (var field in fields)
                {
                    var name = field.Key;
                    var entity = field.Value;
                    if (name == ArrayElement && entity.taintedSourceId == 1)
                    {
                        return true;
                    }
                }
                */

                return false;
            }

            public bool IsInputOrChildrenAreInput()
            {
                if (taintedSourceId == 1)
                    return true;

                foreach (var field in fields)
                {
                    var entity = field.Value;
                    if (entity.IsInput())
                    {
                        return true;
                    }
                }

                return false;
            }

            public bool IsTainted() => taintedSourceId > 1;

            public ulong TaintedSourceId => taintedSourceId;

            // for dumping only
            public Dictionary<string, SymbolicEntity> Fields => fields;

            // for dumping only
            public HashSet<SymbolicEntity> PossibleInputEntities => possibleInputEntities;
            
            // for dumping only
            public HashSet<SymbolicEntity> PossibleTaintedEntities => possibleTaintedEntities;
            
            // for dumping only
            public bool HasTargetMethods => methodCalls.Count > 0;

            public void Apply(ApplyingContext context, SymbolicEntity rootEntity, DataFlowAnalysisResult result)
            {
                TraverseApplying(context, this, rootEntity, result);
            }

            public void RemoveEmptyEntities(VisitingContext context)
            {
                TraverseRemoving(context, this);
            }

            public void OptimizePossibleTaintedEntities(VisitingContext context,
                VisitingContext markedEntities)
            {
                TraverseRemovingPossibleTaintedEntities(context, this, markedEntities);
            }

            public SymbolicEntity GetMatchedValue(ApplyingContext context, DataFlowAnalysisResult result)
            {
                if (context.TryGetMatched(this, out var matched))
                {
                    return matched;
                }

                // go up
                SymbolicEntity sourceNode;
                var path = new Stack<string>();
                var visitedNotes = new HashSet<SymbolicEntity>();
                var processingNodes = new Stack<(SymbolicEntity, string)>();
                processingNodes.Push((this, String.Empty));

                do
                {
                    string field;
                    (sourceNode, field) = processingNodes.Pop();
                    path.Push(field);
                    if (context.IsMatched(sourceNode))
                    {
                        break;
                    }

                    var addedParent = false;
                    foreach (var (parentNode, parentField) in sourceNode.references)
                    {
                        // enumerate only fields
                        if (parentField == PossibleInputMark || parentField == PossibleTaintedMark)
                            continue;
                        
                        if (visitedNotes.Add(parentNode))
                        {
                            processingNodes.Push((parentNode, parentField));
                            addedParent = true;
                        }
                    }

                    if (!addedParent)
                    {
                        path.Pop();
                    }
                } while (processingNodes.Count > 0);

                if (context.TryGetMatched(sourceNode, out var destNode))
                {
                    // go back
                    while (path.Count > 1)
                    {
                        var field = path.Pop();
                        Debug.Assert(!context.ToMergingContext().IsMatched(destNode));
                        destNode = destNode.LoadField(field);
                        Debug.Assert(!context.ToMergingContext().IsMatched(destNode), "?");
                    }
                    
                    Debug.Assert(path.Count == 1 || path.Peek() == String.Empty);
                }
                else
                {
                    // the root node w/o matching to dest can be a ctor returned value (i.e. sourceNode.source is MethodReturnSource)
                    // TODO: check Assert:
/*
                Debug.Assert(sourceNode.source == null ||
                             sourceNode.source is MethodReturnSource ||
                             sourceNode.source is ConstSource cs && cs.Value is string v && (v.StartsWith("NULL") || v.StartsWith("FAKE")));
*/
                    destNode = new SymbolicReference(source).entity;
                }

                // found or created the destination node
                context.Match(this, destNode);

                TraverseApplying(context, destNode, this, result);
                return destNode;
            }

            public override string ToString()
            {
                var builder = new StringBuilder();
                if (taintedSourceId == 1)
                    builder.Append("I:");

                if (taintedSourceId > 1)
                    builder.Append("T:");

                if (source != null)
                    builder.Append(source);
                else
                    builder.Append("UnknownEntity");

                return builder.ToString();
            }

            private static void TraverseRemovingPossibleTaintedEntities(VisitingContext context,
                SymbolicEntity root,
                VisitingContext markedEntities)
            {
                var stack = new Stack<SymbolicEntity>();
                stack.Push(root);
                while (stack.Count > 0)
                {
                    var node = stack.Pop();
                    if (context.IsVisited(node))
                    {
                        // we already analysed the node
                        continue;
                    }

                    context.Visit(node);
                    if (node.possibleInputEntities != null)
                    {
                        // 1) remove self loop if it exists 
                        node.possibleInputEntities.Remove(node);
                        
                        // 2) add all outgoing nodes for analyzing
                        foreach (var outgoingNode in node.possibleInputEntities)
                        {
                            stack.Push(outgoingNode);
                        }
                    }

                    if (markedEntities.IsVisited(node))
                    {
                        // the node is reached by a field path
                        continue;
                    }

                    if (node.possibleTaintedEntities != null &&
                        node.possibleTaintedEntities.Count > 0)
                    {
                        // this value can affect some tainted value
                        continue;
                    }
                    
                    // copy all outgoing links of this node to each parent
                    // remove the node if it doesn't have any incoming PossibleTainted references AND
                    // doesn't have Target Method Calls
                    HashSet<(SymbolicEntity, string)> possibleTaintedReferences = null;
                    if (node.HasTargetMethods)
                    {
                        possibleTaintedReferences = new HashSet<(SymbolicEntity, string)>();
                    }

                    foreach (var (incomingNode, incomingNodeType) in node.references)
                    {
                        switch (incomingNodeType)
                        {
                            case PossibleTaintedMark:
                                if (possibleTaintedReferences == null)
                                {
                                    //remove this link
                                    incomingNode.possibleTaintedEntities.Remove(node);
                                }
                                else
                                {
                                    //store to a new reference collection
                                    possibleTaintedReferences.Add((incomingNode, incomingNodeType));
                                }
                                break;
                            case PossibleInputMark:
                                incomingNode.possibleInputEntities.Remove(node);
                                if (node.possibleInputEntities != null)
                                {
                                    foreach (var outgoingNode in node.possibleInputEntities)
                                    {
                                        ReplaceReference(PossibleInputMark, incomingNode, outgoingNode, node);
                                    }
                                }
                                break;
                            default:
                                // this is unreachable path (e.g., from a local var)
                                Debug.Assert(!markedEntities.IsVisited(incomingNode));
                                break;
                        }
                    }

                    if (possibleTaintedReferences == null)
                    {
                        node.references.Clear();    
                    }
                    else
                    {
                        node.references = possibleTaintedReferences;
                    }

                    /*
                    // we can remove possibleTaintedEntities that have a link to a node w/o method calls                       
                    node.possibleTaintedEntities?.RemoveWhere(entity =>
                        entity.references.Count == 0 &&
                        entity.possibleTaintedEntities == null);
                        */
                }
            }

            private static void TraverseRemoving(VisitingContext context, SymbolicEntity root)
            {
                var stack = new Stack<SymbolicEntity>(root.fields.Values);
                while (stack.Count > 0)
                {
                    var node = stack.Pop();
                    if (context.IsVisited(node))
                        continue;

                    context.Visit(node);
                    if (node.fields.Count > 0)
                    {
                        foreach (var field in node.fields)
                        {
                            stack.Push(field.Value);
                        }
                    }
                    else
                    {
                        if (node.taintedSourceId == 0 &&
                            node.methodCalls.Count == 0 &&
                            (node.possibleTaintedEntities == null || node.possibleTaintedEntities.Count == 0) &&
                            (node.possibleInputEntities == null || node.possibleInputEntities.Count == 0) &&
                            (node.references.Count == 1 || 
                             node.references.Count(tuple => 
                                 tuple.Item2 != PossibleInputMark && tuple.Item2 != PossibleTaintedMark) == 1))
                        {
                            RemoveEmptyParents(node);
                        }
                    }
                }
            }

            private static void RemoveEmptyParents(SymbolicEntity entity)
            {
                var queue = new Queue<SymbolicEntity>();
                queue.Enqueue(entity);
                while (queue.Count > 0)
                {
                    var node = queue.Dequeue();
                    if (node.fields.Count == 0 &&
                        node.taintedSourceId == 0 &&
                        node.methodCalls.Count == 0 &&
                        (node.possibleTaintedEntities == null || node.possibleTaintedEntities.Count == 0) &&
                        (node.possibleInputEntities == null || node.possibleInputEntities.Count == 0) &&
                        (node.references.Count == 1 ||
                         node.references.Count(tuple => 
                             tuple.Item2 != PossibleInputMark && tuple.Item2 != PossibleTaintedMark) == 1))
                    {
                        foreach (var (parentEntity, fieldName) in node.references)
                        {
                            Debug.Assert(fieldName != null);
                            switch (fieldName)
                            {
                                case PossibleInputMark:
                                    parentEntity.possibleInputEntities.Remove(node);
                                    break;
                                case PossibleTaintedMark:
                                    parentEntity.possibleTaintedEntities.Remove(node);
                                    break;
                                default:
                                    parentEntity.fields.Remove(fieldName);
                                    break;
                            }
                            queue.Enqueue(parentEntity);
                        }

                        node.references.Clear();
                    }
                }
            }

            private static void TraverseApplying(ApplyingContext context,
                SymbolicEntity rootDest, SymbolicEntity rootSource, DataFlowAnalysisResult result)
            {
                Debug.Assert(context.IsMatched(rootSource));
                var stack = new Stack<(SymbolicEntity, SymbolicEntity)>();
                stack.Push((rootDest, rootSource));
                while (stack.Count > 0)
                {
                    var (dest, source) = stack.Pop();
                    dest = context.ToMergingContext().GetMatched(dest);

                    ApplyPossibleInputEntities(context, stack, dest, source, result);
                    ApplyPossibleTaintedEntities(context, stack, dest, source, result);

                    if (dest.taintedSourceId > 1)
                    {
                        foreach (var methodCall in source.methodCalls)
                        {
                            result.AddAttackTriggerCall(dest.taintedSourceId, methodCall);
                        }
                    }
                    else
                    {
                        //dest.methodCalls.AddRange(source.methodCalls);
                        foreach (var methodCall in source.methodCalls)
                        {
                            dest.methodCalls.Add(methodCall);
                            //dest.methodCalls.Add((method, callStack.Push(context.Method.ToString())));
                        }
                    }

                    if (source.taintedSourceId > dest.taintedSourceId)
                    {
                        //Debug.Assert(dest.taintedSourceId == 0); 
                        // TODO: add array for tainted values
                        (dest.taintedSourceId, dest.taintedCallStack) = 
                            result.UpdateTaintedMethod(source.taintedSourceId, context.CallStack, source.taintedCallStack);
                    }

                    foreach (var sourceField in source.fields)
                    {
                        var sourceFieldName = sourceField.Key;
                        var sourceFieldValue = sourceField.Value;
                        if (context.TryGetMatched(sourceFieldValue, out var matchedDest))
                        {
                            if (dest.fields.TryGetValue(sourceFieldName, out var field))
                            {
                                TraverseMerging(context.ToMergingContext(), field, matchedDest);
                                dest = context.ToMergingContext().GetMatched(dest);
                            }
                            else
                            {
                                Debug.Assert(!context.ToMergingContext().IsMatched(dest));
                                Debug.Assert(!context.ToMergingContext().IsMatched(matchedDest));
                                ReplaceReference(sourceFieldName, dest, matchedDest, null);
                            }
                        }
                        else
                        {
                            Debug.Assert(!context.ToMergingContext().IsMatched(dest));
                            var destField = dest.LoadField(sourceFieldName);
                            Debug.Assert(!context.ToMergingContext().IsMatched(destField), "?");
                            context.Match(sourceFieldValue, destField);
                            stack.Push((destField, sourceFieldValue));
                        }
                    }
                }
            }
            
            private static void ApplyPossibleInputEntities(ApplyingContext context,
                Stack<(SymbolicEntity, SymbolicEntity)> stack,
                SymbolicEntity dest, 
                SymbolicEntity source, 
                DataFlowAnalysisResult result)
            {
                if (source.possibleInputEntities == null) 
                    return;
                
                foreach (var sourcePossibleInputEntity in source.possibleInputEntities)
                {
                    if (!context.TryGetMatched(sourcePossibleInputEntity, out var destPossibleInputEntity))
                    {
                        destPossibleInputEntity = new SymbolicReference().entity;
                        context.Match(sourcePossibleInputEntity, destPossibleInputEntity);
                        stack.Push((destPossibleInputEntity, sourcePossibleInputEntity));
                    }
                            
                    if (dest.taintedSourceId == 1)
                    {
                        destPossibleInputEntity.MarkInput();
                        destPossibleInputEntity.PropagateTaintedValues(result);
                    }
                    else if (dest.taintedSourceId == 0)
                    {
                        if (dest.possibleInputEntities == null)
                        {
                            dest.possibleInputEntities = new HashSet<SymbolicEntity>(
                                source.possibleInputEntities.Count);
                        }

                        ReplaceReference(PossibleInputMark, dest, destPossibleInputEntity, null);
                    }
                }
            }
            
            private static void ApplyPossibleTaintedEntities(ApplyingContext context, 
                Stack<(SymbolicEntity, SymbolicEntity)> stack,
                SymbolicEntity dest, 
                SymbolicEntity source, 
                DataFlowAnalysisResult result)
            {
                if (source.possibleTaintedEntities == null)
                    return;

                foreach (var sourcePossibleTaintedEntity in source.possibleTaintedEntities)
                {
                    if (!context.TryGetMatched(sourcePossibleTaintedEntity, out var destPossibleTaintedEntity))
                    {
                        destPossibleTaintedEntity = new SymbolicReference().entity;
                        context.Match(sourcePossibleTaintedEntity, destPossibleTaintedEntity);
                        stack.Push((destPossibleTaintedEntity, sourcePossibleTaintedEntity));
                    }

                    if (dest.taintedSourceId == 1)
                    {
                        var (id, backwardCallStack) = result.AddTaintedMethodCall(null, null);
                        destPossibleTaintedEntity.MarkTaint(id, backwardCallStack);
                        foreach (var methodCall in destPossibleTaintedEntity.methodCalls)
                        {
                            result.AddAttackTriggerCall(id, methodCall);
                        }
                        
                        destPossibleTaintedEntity.methodCalls.Clear();
                    }
                    else
                    {
                        if (dest.possibleTaintedEntities == null)
                        {
                            dest.possibleTaintedEntities = new HashSet<SymbolicEntity>(
                                source.possibleTaintedEntities.Count);
                        }

                        ReplaceReference(PossibleTaintedMark, dest, destPossibleTaintedEntity, null);
                    }
                }
            }

            private void PropagateTaintedValues(DataFlowAnalysisResult result)
            {
                Debug.Assert(taintedSourceId == 1);
                if (possibleTaintedEntities != null)
                {
                    var entities = possibleTaintedEntities;
                    possibleTaintedEntities = null;
                    foreach (var entity in entities)
                    {
                        var (id, backwardCallStack) = result.AddTaintedMethodCall(null, null);
                        entity.MarkTaint(id, backwardCallStack);
                        foreach (var methodCall in entity.methodCalls)
                        {
                            result.AddAttackTriggerCall(id, methodCall);
                        }
                        
                        entity.methodCalls.Clear();
                    }
                }
                
                if (possibleInputEntities != null)
                {
                    var entities = possibleInputEntities;
                    possibleInputEntities = null;
                    foreach (var entity in entities)
                    {
                        entity.MarkInput();
                        entity.PropagateTaintedValues(result);
                    }
                }
            }

            private static void ReplaceReference(string name, SymbolicEntity parent, SymbolicEntity child,
                SymbolicEntity oldParent)
            {
                Debug.Assert(name != null);
                if (oldParent != null)
                {
                    child.references.Remove((oldParent, name));
                }

                switch (name)
                {
                    case PossibleInputMark:
                        if (parent != child)
                        {
                            child.references.Add((parent, name));
                            if (parent.possibleInputEntities == null)
                                parent.possibleInputEntities = new HashSet<SymbolicEntity>();
                            
                            parent.possibleInputEntities.Add(child);
                        }
                        break;
                    case PossibleTaintedMark:
                        child.references.Add((parent, name));
                        if (parent.possibleTaintedEntities == null)
                            parent.possibleTaintedEntities = new HashSet<SymbolicEntity>();
                        
                        parent.possibleTaintedEntities.Add(child);
                        break;
                    default:
                        child.references.Add((parent, name));
                        parent.fields[name] = child;
                        if (parent.taintedSourceId == 1 && child.taintedSourceId == 0)
                        {
                            child.taintedSourceId = 1;
                        }

                        break;
                }
            }
        }

        public static SymbolicReference Merge(SymbolicReference[] entities1, SymbolicReference[] entities2 = null)
        {
            Debug.Assert(entities1 != null && entities1.Length > 0);

            var context = new MergingContext();
            var mergedEntity = entities1[0];
            for (int i = 1; i < entities1.Length; i++)
            {
                var entity = entities1[i];
                SymbolicEntity.TraverseMerging(context, 
                    mergedEntity.entity, 
                    entity.entity);

                entity.entity = mergedEntity.entity;
            }

            if (entities2 != null)
            {
                for (int i = 0; i < entities2.Length; i++)
                {
                    var entity = entities2[i];
                    SymbolicEntity.TraverseMerging(context, 
                        mergedEntity.entity, 
                        entity.entity);

                    entity.entity = mergedEntity.entity;
                }
            }

            return mergedEntity;
        }
        
        private SymbolicEntity entity;
        
        public SymbolicReference()
        {
            entity = new SymbolicEntity(this);
        }

        public SymbolicReference(ISourceValue source)
        {
            entity = new SymbolicEntity(source, this);
        }

        public void Match(ApplyingContext context, SymbolicReference dest)
        {
            context.Match(entity, dest.entity);
        }

        public bool IsConst => entity.IsConst;
        
        public SymbolicReference LoadField(string name) => 
            entity.LoadField(name).BackReference;

        public void StoreField(string name, SymbolicReference value) =>
            entity.StoreField(name, value.entity);

        public void AddSinkMethod(MethodUniqueSignature method, 
            IEnumerable<string> callStack, 
            SymbolicReference[] returnEntities) =>
            entity.AddSinkMethod(method, callStack, returnEntities);


        public void AddTargetMethod(MethodUniqueSignature method,
            ImmutableStack<string> callStack,
            DataFlowAnalysisResult result) =>
            entity.AddTargetMethod(method, callStack, result);

        public void AddPossibleInputTransformMethod(MethodUniqueSignature method,
            IEnumerable<string> callStack,
            SymbolicReference[] returnEntities) =>
            entity.AddPossibleInputTransformMethod(method, callStack, returnEntities);

        public void MarkInput() => entity.MarkInput();

        public void MarkTaint(ulong id, ImmutableStack<string> callStack) => entity.MarkTaint(id, callStack);

        public bool IsInput() => entity.IsInput();
        
        public bool IsInputOrChildrenAreInput() => entity.IsInputOrChildrenAreInput();

        public bool IsTainted() => entity.IsTainted();

        public ulong TaintedSourceId => entity.TaintedSourceId;

        // for dumping only
        public int FieldCount => entity.Fields.Count;
        
        // for dumping only
        public IEnumerable<KeyValuePair<string, SymbolicReference>> Fields =>
            entity.Fields.Select(pair =>
                new KeyValuePair<string, SymbolicReference>(pair.Key, pair.Value.BackReference));

        // for dumping only
        public IEnumerable<SymbolicReference> PossibleInputEntities => 
            entity.PossibleInputEntities?.Select(e => e.BackReference) ?? Enumerable.Empty<SymbolicReference>();
        
        // for dumping only
        public IEnumerable<SymbolicReference> PossibleTaintedEntities => 
            entity.PossibleTaintedEntities?.Select(e => e.BackReference) ?? Enumerable.Empty<SymbolicReference>();

        // for dumping only
        public bool HasTargetMethods => entity.HasTargetMethods;

        public void Apply(ApplyingContext context, SymbolicReference rootEntity, DataFlowAnalysisResult result)
            => entity.Apply(context, rootEntity.entity, result);

        public void RemoveEmptyEntities(VisitingContext context) =>
            entity.RemoveEmptyEntities(context);

        public void OptimizePossibleTaintedEntities(
            VisitingContext context,
            VisitingContext markedEntities) =>
            entity.OptimizePossibleTaintedEntities(context, markedEntities);

        public SymbolicReference GetMatchedValue(
            ApplyingContext context,
            DataFlowAnalysisResult result) =>
            entity.GetMatchedValue(context, result).BackReference;

        public override string ToString() => entity.ToString();
    }
}