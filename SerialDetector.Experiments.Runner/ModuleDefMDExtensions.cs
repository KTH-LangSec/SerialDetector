using System.Collections.Generic;
using dnlib.DotNet;

namespace SerialDetector.Experiments.Runner
{
    internal static class ModuleDefMDExtensions
    {
        public static IEnumerable<IMethodDefOrRef> FindSensitiveSinkCalls(this List<ModuleDefMD> assemblies)
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
                            if (instruction.Operand is IMethodDefOrRef methodRefOperand &&
                                methodRefOperand.IsMethod)
                            {
                                if (methodRefOperand.MethodSig.RetType.FullName != "System.Object")
                                    continue;

                                var methodRefOperandResolved = methodRefOperand.ResolveMethodDef();
                                if (methodRefOperandResolved == null)
                                {
                                    // TODO: debug this warning
                                    //Console.WriteLine($"Error resolving {methodRefOperand.FullName}");
                                    continue;
                                }
                                
                                if (methodRefOperandResolved.IsAbstract)
                                   continue; 
                                    
                                if (methodRefOperandResolved.HasBody &&
                                   methodRefOperandResolved.Body.HasInstructions) 
                                    continue;

                                if (show)
                                {
                                    //Console.WriteLine($"    {methodDef.FullName}");
                                    show = false;
                                }

                                //Console.WriteLine($"        {methodRefOperand.FullName}");
                                yield return methodRefOperand;
                            }
                        }
                    }
                }
            }
        }
    }
}