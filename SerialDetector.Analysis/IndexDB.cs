using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace SerialDetector.Analysis
{
    public class IndexDb
    {
        private struct Statistic
        {
            public ulong AssemblyCount;
            public double TypeCount;
            public double MethodCount;

            public double MethodCallCount;
            public double InstructionCount;

            public ulong SpecifiedVirtualCalls;

            public TimeSpan AssemblyLoading;
            public TimeSpan IndexBuilding;
        }
        
        private readonly List<CallInfo> empty = new List<CallInfo>();
        private Statistic stat = new Statistic();
        private readonly ModuleContext defaultContext;
        
        //<method call> -> <assembly of the method call> -> <method definition w/ (1)>
        private Dictionary<MethodUniqueSignature, Dictionary<AssemblyInfo, List<CallInfo>>> callers;

        private Dictionary<MethodUniqueSignature, HashSet<MethodDef>> implementations;

        public IndexDb(string path, bool useGAC = false)
        {
            AssemblyResolver asmResolver = new AssemblyResolver();
            defaultContext = new ModuleContext(asmResolver);

            // All resolved assemblies will also get this same modCtx
            asmResolver.DefaultModuleContext = defaultContext;

            // Enable the TypeDef cache for all assemblies that are loaded
            // by the assembly resolver. Only enable it if all auto-loaded
            // assemblies are read-only.
            asmResolver.EnableTypeDefCache = true;
            asmResolver.UseGAC = useGAC;

            if (path != null)
            {
                var timer = Stopwatch.StartNew();
                if (File.Exists(path))
                {
                    LoadAssembly(path);
                }
                else
                {
                    //foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                    foreach (var file in Directory.EnumerateFiles(path))
                    {
                        LoadAssembly(file);
                    }
                }

                stat.AssemblyLoading = timer.Elapsed;
            }
        }

        public readonly List<ModuleDefMD> Assemblies = new List<ModuleDefMD>();
        public Dictionary<string, HashSet<string>> AssemblyReferences { get; private set; }
 
        public List<TypeDef> Build(Type[] convertingTypes = null)
        {
            callers = new Dictionary<MethodUniqueSignature, Dictionary<AssemblyInfo, List<CallInfo>>>();
            implementations = new Dictionary<MethodUniqueSignature, HashSet<MethodDef>>();
            AssemblyReferences = new Dictionary<string, HashSet<string>>();
            
            HashSet<string> typeNames;
            if (convertingTypes != null)
            {
                typeNames = new HashSet<string>(convertingTypes.Length);
                foreach (var type in convertingTypes)
                {
                    typeNames.Add(type.FullName);
                }
            }
            else
            {
                typeNames = new HashSet<string>()
                {
                    "YamlDotNet.Serialization.TypeInspectors.ReadablePropertiesTypeInspector/ReflectionPropertyDescriptor"
                };
            }
            
            var timer = Stopwatch.StartNew();
            var result = BuildIndexes(Assemblies, typeNames);
            stat.IndexBuilding = timer.Elapsed;
            stat.AssemblyCount = (ulong) Assemblies.Count;

            if (convertingTypes != null && result.Count != convertingTypes.Length)
            {
                throw new Exception("Error converting types during indexing assemblies");
            }
            
            return result;
        }
        
        public int GetImplementationsCount(MethodUniqueSignature signature, HashSet<TypeDef> types)
        {
            if (!implementations.TryGetValue(signature, out var methods))
                return 0;

            if (types != null)
            {
                var count = 0;
                foreach (var method in methods)
                {
                    if (types.Contains(method.DeclaringType))
                    {
                        count++;
                    }
                }

                return count;
            }
            
            Debug.Assert(methods.Count > 0);
            return methods.Count;
        }
        
        public int GetImplementationsCount(MethodUniqueSignature signature, 
            string entryPointAssemblyName = null)
        {
            if (!implementations.TryGetValue(signature, out var methods))
                return 0;

            if (entryPointAssemblyName != null)
            {
                var references = AssemblyReferences[entryPointAssemblyName];
                var count = 0;
                foreach (var method in methods)
                {
                    var assemblyName = method.DeclaringType.DefinitionAssembly.Name; 
                    if (entryPointAssemblyName == assemblyName ||
                        references.Contains(assemblyName))
                    {
                        count++;
                    }
                }

                return count;
            }
            
            Debug.Assert(methods.Count > 0);
            return methods.Count;
        }

        public IReadOnlyCollection<MethodDef> GetImplementations(MethodUniqueSignature signature, 
            string entryPointAssemblyName = null)
        {
            if (!implementations.TryGetValue(signature, out var methods)) 
                return Array.Empty<MethodDef>();

            if (entryPointAssemblyName != null)
            {
                var references = AssemblyReferences[entryPointAssemblyName];
                var filteredMethods = new List<MethodDef>(methods.Count);
                foreach (var method in methods)
                {
                    var assemblyName = method.DeclaringType.DefinitionAssembly.Name; 
                    if (entryPointAssemblyName == assemblyName ||
                        references.Contains(assemblyName))
                    {
                        filteredMethods.Add(method);
                    }
                }

                return filteredMethods;
            }
            
            Debug.Assert(methods.Count > 0);
            return methods;
        }

        public IReadOnlyCollection<MethodDef> GetImplementations(MethodUniqueSignature signature, 
            HashSet<TypeDef> types)
        {
            if (!implementations.TryGetValue(signature, out var methods)) 
                return Array.Empty<MethodDef>();

            if (types != null)
            {
                var filteredMethods = new List<MethodDef>(methods.Count);
                foreach (var method in methods)
                {
                    if (types.Contains(method.DeclaringType))
                    {
                        filteredMethods.Add(method);
                    }
                }

                return filteredMethods;
            }
            
            Debug.Assert(methods.Count > 0);
            return methods;
        }

        // TODO: use IReadOnlyCollection<CallInfo> 
        public List<CallInfo> GetCalls(MethodUniqueSignature methodSignature)
        {
            if (callers.TryGetValue(methodSignature, out var callInfoMap))
            {
                if (callInfoMap.Count == 1)
                {
                    return callInfoMap.Values.First();
                }

                var result = new List<CallInfo>();
                foreach (var list in callInfoMap.Values)
                {
                    result.AddRange(list);
                }
                
                return result;
            }
            
            return empty;
        }

        public List<CallInfo> GetCalls(MethodUniqueSignature methodSignature, AssemblyInfo assemblyInfo)
        {
            if (assemblyInfo == null ||
                assemblyInfo.Name == (UTF8String) null ||
                assemblyInfo.Name == UTF8String.Empty ||
                assemblyInfo.Version == null ||
                assemblyInfo.Version == AssemblyInfo.EmptyVersion)
            {
                return GetCalls(methodSignature);
            }
            
            if (callers.TryGetValue(methodSignature, out var callInfoMap))
            {
                return callInfoMap[assemblyInfo];
            }

            return empty;
        }

        public List<CallInfo> GetCalls(TemplateInfo template)
        {
            if (callers.TryGetValue(template.Method, out var callInfoMap))
            {
                if (callInfoMap.Count == 1)
                {
                    var assemblyInfo = callInfoMap.Keys.First();
                    return template.RequiredOlderVersion == null ||
                           template.RequiredOlderVersion == AssemblyInfo.EmptyVersion ||
                           assemblyInfo.Version < template.RequiredOlderVersion
                                ? callInfoMap.Values.First() 
                                : empty;
                }
                
                var result = new List<CallInfo>();
                foreach (var pair in callInfoMap)
                {
                    if (template.RequiredOlderVersion == null ||
                        template.RequiredOlderVersion == AssemblyInfo.EmptyVersion ||
                        pair.Key.Version < template.RequiredOlderVersion)
                    {
                        result.AddRange(pair.Value);
                    }
                }

                return result;
            }

            return empty;
        }

        public void ShowStatistic()
        {
            Console.WriteLine($"Assemblies: {stat.AssemblyCount}");
            Console.WriteLine($"----------");
            Console.WriteLine($"loading {stat.AssemblyLoading}");
            Console.WriteLine();
            Console.WriteLine($"Types: {stat.TypeCount}");
            Console.WriteLine($"Methods: {stat.MethodCount}");
            Console.WriteLine($"Method Calls: {stat.MethodCallCount}");
            Console.WriteLine($"Instructions: {stat.InstructionCount}");
            Console.WriteLine($"SpecifiedVirtualCalls: {stat.SpecifiedVirtualCalls}");
            Console.WriteLine($"----------");
            Console.WriteLine($"indexing {stat.IndexBuilding}");
            Console.WriteLine();
        }

        public void LoadAssembly(string fileName)
        {
            try
            {
                var module = ModuleDefMD.Load(fileName);
                if (module.HasNativeEntryPoint)
                {
                    Console.WriteLine($"Skip load '{Path.GetFileName(fileName)}' with native entry point");
                    SkippedModules.Add(module.FullName);
                    return;
                }

                module.Context = defaultContext;
                Assemblies.Add(module);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error load '{Path.GetFileName(fileName)}': {e.Message}");
            }
        }
        
        public HashSet<string> SkippedModules { get; } = new HashSet<string>();

        private List<TypeDef> BuildIndexes(List<ModuleDefMD> modules, HashSet<string> convertingTypes)
        {
            var result = new List<TypeDef>();
            var references = new Dictionary<string, List<string>>(modules.Count);
            foreach (var module in modules)
            {
                references.Add(
                    module.Assembly.Name, 
                    module.GetAssemblyRefs().Select(x => x.Name.ToString()).ToList());
                /*
                var currentAssemblyName = module.Assembly.Name.ToString();
                var foundRefs = false;
                foreach (var assemblyRef in module.GetAssemblyRefs())
                {
                    var name = assemblyRef.Name;
                    if (references.TryGetValue(name, out var list))
                    {
                        list.Add(currentAssemblyName);
                    }
                    else
                    {
                        references.Add(name, new HashSet<string>{currentAssemblyName});
                    }

                    foundRefs = true;
                }

                if (!foundRefs)
                {
                    rootReferences.Add(currentAssemblyName);
                }
                */
                
                foreach (var typeDef in module.GetTypes())
                {
                    if (convertingTypes.Contains(typeDef.FullName/*typeDef.ReflectionFullName*/))
                    {
                        result.Add(typeDef);
                    }
                    
                    stat.TypeCount++;
                    if (!typeDef.HasMethods) continue;

                    foreach (var methodDef in typeDef.Methods)
                    {
                        stat.MethodCount++;
                        if (!methodDef.HasBody) continue;
                        if (!methodDef.Body.HasInstructions) continue;

                        stat.InstructionCount += methodDef.Body.Instructions.Count; 
                        var cachedOverrides = methodDef.FindOverrides();
                        AddImplementations(methodDef, cachedOverrides);
                        
                        CallInfo cacheInfo = null;
                        for (var index = 0; index < methodDef.Body.Instructions.Count; index++)
                        {
                            var instruction = methodDef.Body.Instructions[index];
                            if (instruction.OpCode == OpCodes.Ldsfld ||
                                instruction.OpCode == OpCodes.Ldfld ||
                                instruction.OpCode == OpCodes.Ldsflda ||
                                instruction.OpCode == OpCodes.Ldflda ||
                                instruction.OpCode == OpCodes.Stsfld ||
                                instruction.OpCode == OpCodes.Stfld)
                            {
                                continue;
                            }
                            
                            // TODO: check opcodes:
                            //    + call: 802655
                            //    + callvirt: 918700
                            //    + newobj: 255686
                            //    - ldsfld: 27607
                            //    - ldfld: 57948
                            //    - stfld: 23493
                            //    ? ldftn: 41261
                            //    - ldflda: 4556
                            //    ? ldvirtftn: 1453
                            //    - stsfld: 2023
                            //    ? ldtoken: 622
                            //    - ldsflda: 24
                            if (instruction.Operand is IMethod methodOperand)
                            {
                                if (methodOperand.DeclaringType.DefinitionAssembly == null)
                                {
                                    // TODO: it is possible for an array of generic type, just skip it for now 
                                    // FSharp.Core.dll
                                    // T[0...,0...] Microsoft.FSharp.Core.ExtraTopLevelOperators::array2D$cont@115<?,T>(?[],System.Int32,Microsoft.FSharp.Core.Unit)
                                    continue;
                                }
                                
                                stat.MethodCallCount++;
                                
                                CallInfo newInfo;
                                if (cacheInfo == null)
                                {
                                    newInfo = new CallInfo(index, methodDef, cachedOverrides);
                                    cacheInfo = newInfo;
                                }
                                else
                                {
                                    newInfo = cacheInfo.Copy(index);
                                }

                                // TODO PERF add cache for AssemblyInfo
                                var assemblyInfo = new AssemblyInfo(
                                    methodOperand.DeclaringType.DefinitionAssembly.Name,
                                    methodOperand.DeclaringType.DefinitionAssembly.Version);

                                MethodUniqueSignature key = null;
                                if (instruction.OpCode.Code == Code.Callvirt)
                                {
                                    if (TryGetConcreteSignature(methodDef, index, out key))
                                    {
                                        stat.SpecifiedVirtualCalls++;
                                        newInfo.Opcode = OpCodes.Call;
                                    }
                                }
                                
                                if (key == null)
                                {
                                    key = methodOperand.CreateMethodUniqueSignature();    
                                }
                                
                                if (callers.TryGetValue(key, out var callInfoMap))
                                {
                                    if (callInfoMap.TryGetValue(assemblyInfo, out var list))
                                    {
                                        list.Add(newInfo);
                                    }
                                    else
                                    {
                                        callInfoMap.Add(assemblyInfo, new List<CallInfo> {newInfo});
                                    }
                                }
                                else
                                {
                                    callers.Add(key, new Dictionary<AssemblyInfo, List<CallInfo>>()
                                    {
                                        {assemblyInfo, new List<CallInfo> {newInfo}}
                                    });
                                }
                            }
                        }
                    }
                }
            }

            foreach (var refAssembly in references.Keys)
            {
                CacheReferences(refAssembly, references);
            }

            return result;
        }

        private static HashSet<string> Empty = new HashSet<string>(0);
        private HashSet<string> CacheReferences(string assembly, Dictionary<string, List<string>> references)
        {
            if (AssemblyReferences.TryGetValue(assembly, out var result))
            {
                return result;
            }
            
            AssemblyReferences.Add(assembly, Empty);
            result = new HashSet<string>();
            if (references.TryGetValue(assembly, out var list))
            {
                foreach (var ref1 in list)
                {
                    result.Add(ref1);
                    foreach (var ref2 in CacheReferences(ref1, references))
                    {
                        result.Add(ref2);
                    }
                }
            }

            AssemblyReferences[assembly] = result;
            return result;
        }

        private bool TryGetConcreteSignature(MethodDef methodDef, int index, out MethodUniqueSignature signature)
        {
            signature = null;
            
            var instructions = methodDef.Body.Instructions;
            Debug.Assert(instructions[index].OpCode.Code == Code.Callvirt);
            var method = (IMethod) instructions[index].Operand;
            
            if (method.DeclaringType.ContainsGenericParameter ||    // because we can lose info about <T> when 'this' arg is returned from another call      
                method.DeclaringType.FullName == "System.Array")  // because type.ResolveTypeDef() returns type of an element (e.g., w/o [])
            {
                return false;
            }

            Debug.Assert(method.MethodSig.ImplicitThis);
            var pushedStackSlots = method.GetParameterCount();
            
            // try to rollback to an instruction that pushes 'this'
            while (index > 0 && pushedStackSlots > 0)
            {
                var instruction = instructions[--index];
                if (instruction.OpCode.FlowControl != FlowControl.Call &&
                    instruction.OpCode.FlowControl != FlowControl.Meta &&
                    instruction.OpCode.FlowControl != FlowControl.Next)
                {
                    // not supported instruction
                    //Console.WriteLine($"Not supported FlowControl in TryGetConcreteSignature(): {instruction}");
                    return false;
                }
                
                instruction.CalculateStackUsage(out var pushes, out var pops);
                pushedStackSlots -= pushes;
                if (pushedStackSlots == 0)
                {
                    // check instruction
                    if (instruction.TryGetPushedType(methodDef, out var type))
                    {
                        while (type != null &&
                               type != method.DeclaringType &&
                               type.FullName != method.DeclaringType.FullName)
                        {
                            var typeDef = type.ResolveTypeDef();
                            if (typeDef == null)
                            {
                                return false;
                            }
                            
                            var overriddenMethod = typeDef.FindMethod(method.Name, method.MethodSig);
                            if (overriddenMethod != null)
                            {
                                signature = overriddenMethod.CreateMethodUniqueSignature();
                                return true;
                            }

                            type = typeDef.BaseType;
                        }
                    }

                    return false;
                }

                pushedStackSlots += pops;
            }

            return false;
        }

        private void AddImplementations(MethodDef method, ICollection<MethodDef> cachedOverrides = null)
        {
            foreach (var baseMethod in cachedOverrides ?? method.FindOverrides())
            {
                var baseSignature = baseMethod.CreateMethodUniqueSignature();
                if (!implementations.TryGetValue(baseSignature, out var implList))
                {
                    implList = new HashSet<MethodDef>();
                    implementations.Add(baseSignature, implList);
                }

                var added = implList.Add(method);
                //Debug.Assert(added);  //it happens when 2 same libraries loaded from different directories
                                        //e.g., I copied few .NET FW libs to separate folder
                                        //to analyze entry points from these libs only 
            }
        }
    }
}