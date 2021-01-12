using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using SerialDetector.Analysis.DataFlow.Context;

namespace SerialDetector.Analysis.DataFlow
{
    public class DataFlowAnalysisResult
    {
        private static bool IsHighPriorityPattern((MethodUniqueSignature, ImmutableStack<string>) methodCall) =>
            IsHighPriorityPattern(methodCall.Item1.ToString());
        
        private static bool IsHighPriorityPattern(string signature)
        {
            return signature.StartsWith("System.RuntimeMethodHandle::InvokeMethod(") ||
                   //signature.StartsWith("System.Collections.IList::Add(System.Object)") ||
                   signature.Contains("::SerializationInvoke(") ||          // external method in BinaryFormatter that triggers DataSet gadget
                   (signature.Contains("::Invoke(") &&                      // delegate call
                        !signature.StartsWith("System.CtorDelegate::Invoke(System.Object)") // exclude default ctor call in System.RuntimeType::CreateInstanceDefaultCtor
                   ) ||
                   (signature.Contains(".ctor(") &&                         // delegate
                        !signature.StartsWith("System.String::.ctor(") &&   
                        !signature.StartsWith("System.Decimal::.ctor(") && 
                        !signature.StartsWith("System.Decimal::.ctor(")
                   );    
        }

        
        private readonly IPatternCache patternCache;

        private class SimpleNode : ICallGraphNode
        {
            public SimpleNode(MethodUniqueSignature signature)
            {
                MethodSignature = signature;
            }

            public string PriorColor { get; set; }
            public AssemblyInfo AssemblyInfo => null;
            public string AssemblyName => String.Empty;
            public MethodUniqueSignature MethodSignature { get; }
            public bool IsPublic => false;
            public MethodDef MethodDef => null;
            public CallInfo Info => null;
            public List<ICallGraphNode> InNodes { get; } = new List<ICallGraphNode>();
            public List<ICallGraphNode> OutNodes { get; } = new List<ICallGraphNode>();
            public ICallGraphNode CreateShadowCopy() => null;
            public void ResetShadowCopy()
            {
            }

            public ICallGraphNode ShadowCopy { get; } = null;
        }

        private readonly Dictionary<ulong, TaintedSourceInfo> taintedSourceInfos  = 
            new Dictionary<ulong, TaintedSourceInfo>();
        
        internal DataFlowAnalysisResult(IPatternCache patternCache)
        {
            this.patternCache = patternCache;
        }

        internal (ulong, ImmutableStack<string>) AddTaintedMethodCall(MethodUniqueSignature method, 
            ImmutableStack<string> callStack)
        {
            Console.WriteLine($"{method?.ToString() ?? "UNKNOWN"}: mark as tainted!");
            if (method == null)
            {
                var message = "ERROR! AddTaintedMethodCall: method == null";
                Debug.Fail(message);
                Console.WriteLine(message);
                return (0, ImmutableStack<string>.Empty);
            }

            var taintedSourceId = patternCache.GetNewTaintedIndex();
            if (taintedSourceInfos.ContainsKey(taintedSourceId) || 
                patternCache.Patterns.ContainsKey(taintedSourceId))
            {
                var message = $"We must add an tainted object only one time {method}";
                Debug.Fail(message);
                Console.WriteLine(message);
                return (0, ImmutableStack<string>.Empty);
            }

            var newInfo = new TaintedSourceInfo(taintedSourceId, method, callStack);
            taintedSourceInfos[newInfo.Id] = newInfo;
            patternCache.Patterns[newInfo.Id] = newInfo;
            return (newInfo.Id, newInfo.BackwardCallStack);
        }

        public (ulong, ImmutableStack<string>) UpdateTaintedMethod(ulong taintedSourceId,
            ImmutableStack<string> forwardCallStack,
            ImmutableStack<string> backwardCallStack)
        {
            if (!patternCache.Patterns.TryGetValue(taintedSourceId, out var info))
            {
                var message = $"The results must contain info for {taintedSourceId} before updating.";
                Debug.Fail(message);
                Console.WriteLine(message);
                return (0, ImmutableStack<string>.Empty);
            }

            /*
            if (info.BackwardCallStack == backwardCallStack)
            {
                info.PushBackwardCallStack(forwardCallStack.Peek());
                return (taintedSourceId, info.BackwardCallStack);
            }
            */

            /*
            // this is a new tainted value
            Debug.Assert(!info.BackwardCallStack.SequenceEqual(backwardCallStack));
            */
            var newId = patternCache.GetNewTaintedIndex();
            Console.WriteLine($"{taintedSourceId} -> {newId}");
            var newInfo = new TaintedSourceInfo(
                newId, 
                info.Method, 
                forwardCallStack.Pop(),
                backwardCallStack.Push(forwardCallStack.Peek()),
                info.AttackTriggerCalls);
                
            Debug.Assert(!taintedSourceInfos.ContainsKey(newInfo.Id));
            taintedSourceInfos.Remove(taintedSourceId);
            taintedSourceInfos[newInfo.Id] = newInfo;
            patternCache.Patterns[newInfo.Id] = newInfo;
            return (newInfo.Id, newInfo.BackwardCallStack);
        }
            
        internal Summary Summary { get; set; }

        internal void AddAttackTriggerCall(ulong taintedSourceId, 
            (MethodUniqueSignature, ImmutableStack<string>) methodCall)
        {
            if (!taintedSourceInfos.TryGetValue(taintedSourceId, out var info))
            {
                var message = $"Taint source {taintedSourceId} doesn't exist for adding the external call {methodCall.Item1}.";
                Debug.Fail(message);
                Console.WriteLine(message);
                return;
            }

            HasPattern = true;
            info.AddAttackTriggerCall(methodCall);
        }
        
        public bool HasPattern { get; private set; }

        public int TaintedObjectCount => taintedSourceInfos.Count;
        public int PatternCount => taintedSourceInfos.Values.Count(info => !info.AttackTriggerCalls.IsEmpty);

        public int HighPriorityPatternCount =>
            taintedSourceInfos.Values.Count(info => info.AttackTriggerCalls.Any(IsHighPriorityPattern));

        public int ExternalCallCount => taintedSourceInfos.Values.Sum(info => info.AttackTriggerCallsCount);
        
        public DataFlowAnalysisStatistic Stat { get; } = new DataFlowAnalysisStatistic();

        public void DumpAllStat(string directory, string name, 
            List<(string, string[], List<ulong>)> uniquePatternGroups)
        {
            var path = Path.Combine(directory, name);
            using (var writer = new StreamWriter(path))
            {
                writer.WriteLine(Stat.Dump());
                writer.WriteLine();
                writer.WriteLine("DUMP2");
                writer.WriteLine(Stat.Dump2(
                    uniquePatternGroups.Count, 
                    uniquePatternGroups.Count(x => x.Item2.Any(IsHighPriorityPattern))));
            }
        }
        
        public List<(string, string[], List<ulong>)> Dump(string directory, string name)
        {
            var internalDir = Path.Combine(directory, name);
            if (Directory.Exists(internalDir))
            {
                Directory.Delete(internalDir, true);
            }

            Directory.CreateDirectory(internalDir);

            var uniquePatternGroups = new List<(string, string[], List<ulong>)>(); // sensitive sink, attack triggers -> patterns ids
            var allHighPriorityAttackTriggers = new HashSet<string>();
            foreach (var info in taintedSourceInfos.Values)
            {
                if (info.AttackTriggerCallsCount == 0)
                    continue;

                var allAttackTriggers = new List<string>(info.AttackTriggerCallsCount);
                var uniqueAttackTriggers = new HashSet<string>(info.AttackTriggerCallsCount);
                var highPriorityAttackTriggers = new HashSet<string>();
                int index = 0;
                foreach (var method in info.AttackTriggerCalls)
                {
                    var signature = method.Item1.ToString();
                    allAttackTriggers.Add($"{index++}: {signature}");
                    uniqueAttackTriggers.Add(signature);
                    if (IsHighPriorityPattern(signature))
                    {
                        highPriorityAttackTriggers.Add(signature);
                        allHighPriorityAttackTriggers.Add(signature);
                    }
                }

                var prefix = highPriorityAttackTriggers.Count > 0 ? "!" : ""; 
                var dirName = Path.Combine(internalDir, $"{prefix}{info.Id}");
                Directory.CreateDirectory(dirName);
                using (var file = new StreamWriter(Path.Combine(dirName, "_attack_triggers.txt")))
                {
                    foreach (var method in allAttackTriggers)
                    {
                        file.WriteLine(method);
                    }
                }
                
                using (var file = new StreamWriter(Path.Combine(dirName, "_attack_triggers_unique.txt")))
                {
                    foreach (var method in uniqueAttackTriggers)
                    {
                        file.WriteLine(method);    
                    }
                }

                if (highPriorityAttackTriggers.Count > 0)
                {
                    using (var file = new StreamWriter(Path.Combine(dirName, "_attack_triggers_high.txt")))
                    {
                        foreach (var method in highPriorityAttackTriggers)
                        {
                            file.WriteLine(method);
                        }
                    }
                }

                var match = false;
                foreach (var (ss, methods, patternIds) in uniquePatternGroups)
                {
                    if (ss != info.Method.ToString())
                        continue;
                    
                    if (methods.Length != uniqueAttackTriggers.Count)
                        continue;

                    var matchMethods = true;
                    var i = 0;
                    foreach (var attackTrigger in uniqueAttackTriggers)
                    {
                        if (methods[i++] != attackTrigger)
                        {
                            matchMethods = false;
                            break;
                        }
                    }

                    if (matchMethods)
                    {
                        patternIds.Add(info.Id);
                        match = true;
                        break;
                    }
                }

                if (!match)
                {
                    uniquePatternGroups.Add((
                        info.Method.ToString(), 
                        uniqueAttackTriggers.ToArray(), 
                        new List<ulong> {info.Id}));
                }

                /*
                //info.CallGraph.Dump(Path.Combine(internalDir, $"{info.Id}.png"));
                var graph = CreateCallGraph(info);
                graph.Dump(Path.Combine(internalDir, $"{info.Id}.png"));
                graph.RemoveNonPublicOneToOneNodes();
                graph.Dump(Path.Combine(internalDir, $"{info.Id}_min.png"));
                */
            }
            
            using (var file = new StreamWriter(Path.Combine(directory, name + ".txt")))
            {
                file.WriteLine($"Tainted Objects: {TaintedObjectCount}");
                file.WriteLine($"Patterns: {PatternCount} ({HighPriorityPatternCount} high priority)");
                file.WriteLine($"Unique Patterns: {uniquePatternGroups.Count} ({uniquePatternGroups.Count(x => x.Item2.Any(IsHighPriorityPattern))} high priority)");
                file.WriteLine($"Attack Trigger Methods: {ExternalCallCount}");
                file.WriteLine();
                file.WriteLine("Unique Pattern Groups:");
                uniquePatternGroups.Sort((x, y) => String.Compare(x.Item1, y.Item1, StringComparison.Ordinal));
                foreach (var (ss, _, ids) in uniquePatternGroups)
                {
                    file.WriteLine($"{ss}: " + String.Join(", ", ids));
                }
                
                file.WriteLine();
                foreach (var info in taintedSourceInfos.Values)
                {
                    var priority = info.AttackTriggerCalls.Any(IsHighPriorityPattern) ? "HIGH PRIORITY!" : "";
                    file.WriteLine($"#{info.Id} {info.Method} ({info.AttackTriggerCallsCount} attack triggers) {priority}");
                    foreach (var call in info.BackwardCallStack.Reverse())
                    {
                        file.WriteLine(call);
                    }

                    file.WriteLine("---");
                    foreach (var call in info.ForwardCallStack)
                    {
                        file.WriteLine(call);
                    }

                    file.WriteLine();
                }
            }

            using (var file = new StreamWriter(Path.Combine(directory, name + "_attack_triggers_high.txt")))
            {
                foreach (var method in allHighPriorityAttackTriggers)
                {
                    file.WriteLine(method);
                }
            }
            
            //callGraph.RemoveNonPublicOneToOneNodes();
            //callGraph.Dump(Path.Combine(directory, name + ".png"));
            
            /*
            if (taintedSourceInfos.Count > 0)
            {
                var mainCallGraph = CreateMainCallGraph();
                mainCallGraph.Dump(Path.Combine(internalDir, "main.png"));
                mainCallGraph.RemoveNonPublicOneToOneNodes();
                mainCallGraph.RemoveDuplicatePaths();
                mainCallGraph.Dump(Path.Combine(internalDir, "main_min.png"));
            }        
           */

            return uniquePatternGroups;
        }

        public override string ToString()
        {
            var builder = new StringBuilder();
            foreach (var line in ToText())
            {
                builder.AppendLine(line);
            }

            return builder.ToString();
        }

        public IEnumerable<string> ToText()
        {
            foreach (var info in taintedSourceInfos.Values)
            {
                foreach (var method in info.AttackTriggerCalls)
                {
                    yield return $"{info.Id}:CALL {method.Item1}";
                }
            }

/*                
            foreach (var returnValue in Summary.ReturnValues)
            {
                yield return $"RETURN {returnValue}";
            }

            foreach (var (name, slot) in Summary.StaticScopeMap)
            {
                yield return $"STATIC {name} == {slot}";
            }

            foreach (var (target, field, value) in Summary.OutScopeMap)
            {
                yield return $"FIELDS {target}.{field} == {value}";
            }
*/            
        }

        private CallGraph CreateMainCallGraph()
        {
            var graph = new CallGraph();
            foreach (var info in taintedSourceInfos.Values)
            {
                AddMethodCall(graph, 
                    info.Method,
                    Convert(info.BackwardCallStack)
                        .Reverse()
                        .Concat(Convert(info.ForwardCallStack)),
                    true);
            }

            return graph;
        }
        
        private static CallGraph CreateCallGraph(TaintedSourceInfo info)
        {
            var graph = new CallGraph();
            AddMethodCall(graph, 
                info.Method, 
                Convert(info.BackwardCallStack)
                    .Reverse()
                    .Concat(Convert(info.ForwardCallStack)),
                true);
            
            /*
            foreach (var (method, callStack) in info.ExternalCalls)
            {
                AddMethodCall(graph, method, Convert(callStack), false);
            }
            */

            return graph;
        }
        
        private static IEnumerable<MethodUniqueSignature> Convert(IEnumerable<string> callStack) =>
            callStack
                .Where(call => !call.StartsWith("V:"))
                .Select(call => new MethodUniqueSignature(call[1] == ':'
                    ? call.Substring(2, call.Length - 2)
                    : call));

        private static void AddMethodCall(CallGraph graph, MethodUniqueSignature method, 
            IEnumerable<MethodUniqueSignature> callStack, bool taintedCall)
        {
            if (!graph.Nodes.TryGetValue(method, out var node))
            {
                var newNode = new SimpleNode(method);
                if (!taintedCall)
                {
                    newNode.PriorColor = "dodgerblue";
                }
                graph.Nodes.Add(method, newNode);
                node = newNode;
            }

            foreach (var callSignature in callStack)
            {
                if (!graph.Nodes.TryGetValue(callSignature, out var parentNode))
                {
                    parentNode = new SimpleNode(callSignature);
                    graph.Nodes.Add(callSignature, parentNode);
                }

                if (!node.InNodes.Contains(parentNode))
                {
                    parentNode.OutNodes.Add(node);
                    node.InNodes.Add(parentNode);
                }
                
                node = parentNode;
            }

            if (!graph.EntryNodes.ContainsKey(node.MethodSignature))
            {
                graph.EntryNodes.Add(node.MethodSignature, node);
            }
        }
    }
}