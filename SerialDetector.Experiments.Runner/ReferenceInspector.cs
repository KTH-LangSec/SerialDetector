using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet;

namespace SerialDetector.Experiments.Runner
{
    //TODO: add delegate support
    internal class ReferenceInspector
    {
        readonly List<ModuleDefMD> assemblies = new List<ModuleDefMD>();

        public ReferenceInspector(string directoryPath)
        {
            foreach (var file in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
            {
                //foreach (var file in Directory.EnumerateFiles(directoryPath)) {
                try
                {
                    assemblies.Add(ModuleDefMD.Load(file));
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error load '{Path.GetFileName(file)}': {e.Message}");
                }
            }
        }

        public void FindAssemblyReference(string name)
        {
            FindAssemblyReference(name, module =>
            {
                Console.WriteLine($"{module.Name}");

                foreach (var typeRef in module.GetTypeRefs())
                {
                    if (typeRef.DefinitionAssembly.Name == name)
                    {
                        //Console.WriteLine($"    {typeRef.FullName}");
                        FindMethodCalls(module, name, typeRef.FullName);
                    }
                }
            });
        }

        public void FindMethodCalls(Type type)
        {
            var assemblyRefName = type.Assembly.GetName().Name; // try to use FullName
            FindMethodCalls(assemblyRefName, type.FullName);
        }

        public void FindMethodCalls(string assemblyName, string typeFullName)
        {
            Console.WriteLine();
            Console.WriteLine($"Looking for '{typeFullName}' type usages...");
            Console.WriteLine();

            FindAssemblyReference(assemblyName, module =>
            {
                //Console.WriteLine($"{module.Name}");
                FindMethodCalls(module, assemblyName, typeFullName);
            });
        }

        private void FindMethodCalls(ModuleDefMD module, string assemblyName, string typeFullName)
        {
            var memberInfos = new List<(string, MDToken)>();
            FindMemberReference(module, assemblyName, typeFullName,
                member => memberInfos.Add(member));

            var map = new Dictionary<string, HashSet<string>>();
            FindMethodCalls(module, memberInfos, (fullName, method) =>
            {
                if (map.TryGetValue(fullName, out var list))
                {
                    list.Add(method.FullName);
                }
                else
                {
                    map.Add(fullName, new HashSet<string> {method.FullName});
                }
            });

            foreach (var pair in map)
            {
                Console.WriteLine($"    {pair.Key}");
                foreach (var methodName in pair.Value)
                {
                    Console.WriteLine($"        {methodName}");
                }
            }
        }

        private void FindAssemblyReference(string name, Action<ModuleDefMD> action)
        {
            //Console.WriteLine($"Looking for '{name}' assembly ref");
            foreach (var module in assemblies)
            {
                if (module.Assembly.Name == name || module.GetAssemblyRef(name) != null)
                {
                    action(module);
                }
            }
        }

        private void FindTypeReference(ModuleDefMD module,
            string assemblyRefName, string typeFullName, Action<TypeRef> action)
        {
            foreach (var typeRef in module.GetTypeRefs())
            {
                if (typeRef.DefinitionAssembly.Name == assemblyRefName &&
                    typeRef.FullName == typeFullName)
                {
                    action(typeRef);
                }
            }
        }

        private void FindMemberReference(ModuleDefMD module,
            string assemblyName, string typeFullName, Action<(string, MDToken)> action)
        {
            if (module.Assembly.Name == assemblyName)
            {
                foreach (var typeDef in module.GetTypes())
                {
                    if (typeDef.FullName == typeFullName)
                    {
                        foreach (var methodDef in typeDef.Methods)
                        {
                            action((methodDef.FullName, methodDef.MDToken));
                        }
                    }
                }
            }

            foreach (var memberRef in module.GetMemberRefs())
            {
                var typeRef = memberRef.DeclaringType;
                if (typeRef.DefinitionAssembly.Name == assemblyName &&
                    typeRef.FullName == typeFullName)
                {
                    action((memberRef.FullName, memberRef.MDToken));
                }
            }
        }

        private void FindMethodCalls(ModuleDefMD module, List<(string, MDToken)> memberInfos,
            Action<string, MethodDef> action)
        {
            foreach (var typeDef in module.GetTypes())
            {
                if (!typeDef.HasMethods) continue;

                foreach (var methodDef in typeDef.Methods)
                {
                    if (!methodDef.HasBody) continue;
                    if (!methodDef.Body.HasInstructions) continue;

                    foreach (var instruction in methodDef.Body.Instructions)
                    {
                        if ( //(instruction.OpCode.Code == Code.Call || instruction.OpCode.Code == Code.Callvirt) &&
                            instruction.Operand is IMethodDefOrRef memberRefOperand)
                        {
                            foreach (var member in memberInfos.Where(info => info.Item2 == memberRefOperand.MDToken))
                            {
                                // TODO: pass IMethodDefOrRef instead of string
                                action(member.Item1, methodDef);
                            }
                        }
                    }
                }
            }
        }

        public void FindMethodCallsByName(string name)
        {
            foreach (var module in assemblies)
            {
                foreach (var typeDef in module.GetTypes())
                {
                    if (!typeDef.HasMethods) continue;

                    foreach (var methodDef in typeDef.Methods)
                    {
                        if (!methodDef.HasBody) continue;
                        if (!methodDef.Body.HasInstructions) continue;

                        var show = true;
                        foreach (var instruction in methodDef.Body.Instructions)
                        {
                            if (instruction.Operand is IMethodDefOrRef memberRefOperand &&
                                memberRefOperand.Name == name)
                            {
                                if (show)
                                {
                                    Console.WriteLine($"    {methodDef.FullName}");
                                    show = false;
                                }

                                Console.WriteLine($"        {memberRefOperand.FullName}");
                            }
                        }
                    }
                }
            }
        }

        public IEnumerable<IMethodDefOrRef> FindMagicMethodCalls()
            => assemblies.FindSensitiveSinkCalls();

/*
		public void FindString() {
			foreach (var module in assemblies) {
				module.StringsStream.
			}
		}
*/
    }
}