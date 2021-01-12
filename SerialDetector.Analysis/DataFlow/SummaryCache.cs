using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using SerialDetector.Analysis.DataFlow.Context;

namespace SerialDetector.Analysis.DataFlow
{
    public abstract class SummaryCache
    {
        private class RecursionCallInfo
        {
            public RecursionCallInfo()
            {
                Counter = 0;
                Summary = Summary.Empty;
            }
            
            public int Counter;
            public Summary Summary;
        }
        
        private readonly Dictionary<string, HashSet<TypeDef>> createdTypes = 
            new Dictionary<string, HashSet<TypeDef>>(50000);
        
        private readonly Dictionary<string, Summary> cache = 
            new Dictionary<string, Summary>(10000);
        
        private readonly Dictionary<string, HashSet<string>> recursionLoops =
            new Dictionary<string, HashSet<string>>(10000);
        
        private readonly Dictionary<string, RecursionCallInfo> recursionCalls = 
            new Dictionary<string, RecursionCallInfo>(10000);

        protected DataFlowAnalysisResult result;
        
        protected ImmutableStack<string> CallStack { get; private set; } = ImmutableStack<string>.Empty;
        private int callStackCounter;
        
        private protected abstract Summary Analyze(MethodCall methodCall);
        
        private protected bool IsCallStackEmpty => CallStack.IsEmpty;

        private protected void RegisterType(TypeDef type)
        {
            result.Stat.CreatedTypesCount++;
            if (!RegisterType(type.ScopeType.ToString(), type))
            {
                return;
            }
            
            var types = new List<TypeDef> {type};
            var typeDef = type;
            while (true)
            {
                // Get interfaces of typeDef
                var interfaces = new Queue<ITypeDefOrRef>(typeDef.Interfaces.Select(x => x.Interface));
                while (interfaces.Count > 0)
                {
                    var i = interfaces.Dequeue();
                    RegisterTypes(i.ScopeType.ToString(), types);

                    var resolvedInterface = i.ResolveTypeDef();
                    if (resolvedInterface != null && resolvedInterface.HasInterfaces)
                    {
                        foreach (var n in resolvedInterface.Interfaces)
                        {
                            interfaces.Enqueue(n.Interface);
                        }
                    }
                }

                var typeRef = typeDef.BaseType;
                if (typeRef == null)
                    break;
                
                RegisterTypes(typeRef.ScopeType.ToString(), types);
                typeDef = typeRef.ResolveTypeDef();
                if (typeDef == null)
                    break;

                if (RegisterType(typeDef.ScopeType.ToString(), typeDef))
                {
                    types.Add(typeDef);
                }
            }
        }

        private void RegisterTypes(string baseTypeName, List<TypeDef> types)
        {
            if (!createdTypes.TryGetValue(baseTypeName, out var list))
            {
                list = new HashSet<TypeDef>(TypeEqualityComparer.Instance);
                createdTypes.Add(baseTypeName, list);
            }

            for (var i = 0; i < types.Count; i++)
            {
                var type = types[i];
                list.Add(type);
            }
            
            // invalidate summaries !!!
        }
        
        private bool RegisterType(string baseTypeName, TypeDef type)
        {
            bool added;
            if (createdTypes.TryGetValue(baseTypeName, out var list))
            {
                added = list.Add(type);
            }
            else
            {
                list = new HashSet<TypeDef>(TypeEqualityComparer.Instance) {type};
                createdTypes.Add(baseTypeName, list);
                added = true;
            }

            // invalidate summaries !!!
            return added;
        }
        
        private IEnumerable<string> GetImplementedTypes(TypeDef type)
        {
            ITypeDefOrRef typeRef = type;
            while (true)
            {
                var typeDef = typeRef.ResolveTypeDef();
                if (typeDef == null)
                    break;
                
                // Get interfaces of typeDef
                var interfaces = new Queue<ITypeDefOrRef>(typeDef.Interfaces.Select(x => x.Interface));
                while (interfaces.Count > 0)
                {
                    var i = interfaces.Dequeue();
                    yield return i.ScopeType.ToString();

                    var resolvedInterface = i.ResolveTypeDef();
                    if (resolvedInterface != null && resolvedInterface.HasInterfaces)
                    {
                        foreach (var n in resolvedInterface.Interfaces)
                        {
                            interfaces.Enqueue(n.Interface);
                        }
                    }
                }

                typeRef = typeDef.BaseType;
                if (typeRef == null)
                    break;
                
                yield return typeRef.ScopeType.ToString();    // TODO: PERF don't convert to string
            }
        }

        private static readonly HashSet<TypeDef> Empty = new HashSet<TypeDef>(0); 
        private protected HashSet<TypeDef> GetRegisteredTypes(string baseTypeName)
        {
            Debug.Assert(Empty.Count == 0);
            if (createdTypes.TryGetValue(baseTypeName, out var list))
            {
                return list;
            }

            return Empty;
        }

        private protected void RegisterVirtualCall()
        {
            
        }

        private protected void ResetSummaryCache()
        {
            Debug.Assert(recursionLoops.Count == 0);
            Debug.Assert(recursionCalls.Count == 0);    // or clear it as well
            
            cache.Clear();
        }

        private protected Summary GetOrCreateSummary(MethodCall methodCall)
        {
            var signature = CreateMethodCallSignature(methodCall);
            if (cache.TryGetValue(signature, out var summary))
            {
                DebugLog($"{Indent()}{methodCall.Signature} applying from {(summary.IsEmpty ? "EMPTY " : "")}summary...");
                return summary;
            }

            if (CallStack.Contains(signature))
            {
                DebugLog($"{Indent()}{methodCall.Signature} recursion!");
                if (!recursionLoops.TryGetValue(signature, out var currentRecursionCalls))
                {
                    currentRecursionCalls = new HashSet<string>();
                    recursionLoops.Add(signature, currentRecursionCalls);
                }
                
                foreach (var call in CallStack)
                {
                    if (call == signature)
                        break;

                    if (!currentRecursionCalls.Add(call))
                        break;

                    if (!recursionCalls.TryGetValue(call, out var callInfo))
                    {
                        callInfo = new RecursionCallInfo();
                        recursionCalls.Add(call, callInfo);
                    }
                    
                    callInfo.Counter++;
                }

                result.Stat.IgnoredRecursionMethodCalls++;
                return Summary.Empty;
            }
            
            if (recursionCalls.TryGetValue(signature, out var info))
            {
                DebugLog($"{Indent()}{methodCall.Signature} applying from {(info.Summary.IsEmpty ? "EMPTY " : "")}temparary summary...");
                return info.Summary;
            }

            callStackCounter++;
            CallStack = CallStack.Push(signature);
            
            //var time = DateTime.Now;
            // Console.WriteLine($"{Indent()}{CallStack.Peek()} ANALYZING... ({methodCall.Definition.Module})");
            //Console.WriteLine($"{Indent()}[{time.Minute:00}:{time.Second:00}]{CallStack.Peek()} ANALYZING... ({methodCall.Definition.Module})");
            //Console.WriteLine($"{Indent()}{CallStack.Peek()} ANALYZING... ({methodCall.Definition.Module})");
            //var timer = Stopwatch.StartNew();

            // if (CallStack.Peek() ==
            //     "C:YamlDotNet.Serialization.TypeInspectors.ReadablePropertiesTypeInspector/ReflectionPropertyDescriptor::Write(System.Object,System.Object)"
            //     // "C:System.Reflection.RuntimePropertyInfo::SetValue(System.Object,System.Object,System.Reflection.BindingFlags,System.Reflection.Binder,System.Object[],System.Globalization.CultureInfo)"
            // )
            // {
            //     int aaa = 777;
            // }

            summary = Analyze(methodCall);

            // timer.Stop();
            // time = DateTime.Now;
            // Console.WriteLine($"{Indent()}[{time.Minute:00}:{time.Second:00}]{CallStack.Peek()} ANALYZED ({timer.ElapsedMilliseconds} ms)");
            // Console.WriteLine($"{Indent()}{CallStack.Peek()} ANALYZED ({timer.ElapsedMilliseconds} ms)");
            // Console.WriteLine($"{Indent()}{CallStack.Peek()} ANALYZED)");
            
            CallStack = CallStack.Pop();
            callStackCounter--;

            if (recursionLoops.TryGetValue(signature, out var currentRecursionCalls2))
            {
                DebugLog($"{Indent()} {methodCall.Signature} adding to summary...");
                foreach (var call in currentRecursionCalls2)
                {
                    Debug.Assert(call != signature);
                    Debug.Assert(recursionCalls.ContainsKey(call));
                    Debug.Assert(recursionCalls[call].Counter > 0);
                    if (--recursionCalls[call].Counter == 0)
                    {
                        recursionCalls.Remove(call);
                    }
                }

                recursionLoops.Remove(signature);
                if (recursionCalls.TryGetValue(signature, out var info2))
                {
                    Debug.Assert(info2.Counter > 0);
                    info2.Summary = summary;
                }
                else
                {
                    cache.Add(signature, summary);
                }
            }
            else if (recursionCalls.ContainsKey(signature))
            {
                recursionCalls[signature].Summary = summary;
            }
            else
            {
                cache.Add(signature, summary);
            }
            
            return summary;
        }
        
        protected static void DebugLog(string message)
        {
#if DEBUG
            Console.WriteLine(message); 
#endif
        }
        
        protected void CheckRecursionCache()
        {
            if (recursionLoops.Count != 0)
            {
                Debug.Fail($"ERROR! recursionLoops is not empty ({recursionLoops.Count})");
                Console.WriteLine($"ERROR! recursionLoops is not empty ({recursionLoops.Count})");
            }

            if (recursionCalls.Count != 0)
            {
                Debug.Fail($"ERROR! recursionCalls is not empty ({recursionCalls.Count})");
                Console.WriteLine($"ERROR! recursionCalls is not empty ({recursionCalls.Count})");
            }
        }

        protected string Indent() =>
            new string(' ', callStackCounter * 2); //"";

        private static string CreateMethodCallSignature(MethodCall methodCall) =>
            (methodCall.CallKind == CallKind.Virtual ? "V:" : "C:") + methodCall.Signature;
    }
}