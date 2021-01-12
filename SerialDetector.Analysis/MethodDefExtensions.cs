using System;
using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

// ReSharper disable once CheckNamespace
namespace SerialDetector.Analysis
{
    internal static class MethodDefExtensions
    {
        public static HashSet<MethodDef> FindOverrides(this MethodDef methodDef)
        {
            var result = new HashSet<MethodDef>();
            if (!methodDef.IsVirtual)
            {
                return result;
            }
            
            if (methodDef.HasOverrides)
            {
                foreach (var baseMethod in methodDef.Overrides)
                {
                    var baseMethodDef = baseMethod.MethodDeclaration.ResolveMethodDef();
                    if (baseMethodDef == null)
                    {
                        //Console.WriteLine($"Error resolving method {baseMethod.MethodDeclaration.FullName}");
                        continue;
                    }

                    if (baseMethodDef.IsVirtual)
                    {
                        result.Add(baseMethodDef);
                    }
                }

                return result;
            }

            var typeDef = methodDef.DeclaringType;
            CollectOverrides(typeDef.Interfaces, methodDef.Name, methodDef.MethodSig, result);
            
            var baseType = typeDef.BaseType;
            while (baseType != null)
            {
                var baseTypeDef = baseType.ResolveTypeDef();
                if (baseTypeDef == null)
                {
                    //Console.WriteLine($"Error resolving base type {baseType.FullName}");
                    break;
                }

                var baseMethod = baseTypeDef.FindMethod(methodDef.Name, methodDef.MethodSig);
                if (baseMethod != null && baseMethod.IsVirtual)
                {
                    result.Add(baseMethod);
                }

                CollectOverrides(baseTypeDef.Interfaces, methodDef.Name, methodDef.MethodSig, result);
                baseType = baseTypeDef.BaseType;
            }

            return result;
        }

        private static void CollectOverrides(IList<InterfaceImpl> interfaces, UTF8String name, MethodSig sig, 
            HashSet<MethodDef> overrides)
        {
            var queue = new Queue<InterfaceImpl>(interfaces);
            while (queue.Count > 0)
            {
                var baseInterface = queue.Dequeue();
                if (baseInterface.Interface == null) continue;
                
                var baseInterfaceDef = baseInterface.Interface.ResolveTypeDef();
                if (baseInterfaceDef == null)
                {
                    //Console.WriteLine($"Error resolving interface {baseInterface.Interface.FullName}");
                    continue;
                }

                foreach (var newInterface in baseInterfaceDef.Interfaces)
                {
                    queue.Enqueue(newInterface);
                }

                var baseMethod = baseInterfaceDef.FindMethod(name, sig);
                if (baseMethod != null && baseMethod.IsVirtual)
                {
                    overrides.Add(baseMethod);
                }
            }
        }
        
        public static bool IsPublicGlobalVisibility(this MethodDef method)
        {
            if (method.IsPrivate ||
                method.IsFamilyAndAssembly ||
                method.IsAssembly)
            {
                return false;
            }

            return IsPublicGlobalVisibility(method.DeclaringType);
        }

        public static bool IsPublicGlobalVisibility(this TypeDef type)
        {
            if (!type.IsNested)
            {
                return type.IsPublic;
            }

            if (type.IsNestedPrivate ||
                type.IsNestedFamilyAndAssembly ||
                type.IsNestedAssembly)
            {
                return false;
            }
            
            // this nested class is public, let's check parent classes
            return IsPublicGlobalVisibility(type.DeclaringType);
        }

        public static int GetParameterCount(this IMethod method)
        {
            var signature = method.MethodSig;
            var parameterCount = signature.Params.Count;
                    
            if (signature.ImplicitThis)
                parameterCount++;

            return parameterCount;
        }

        public static IEnumerable<int> EnumerateOutputParameterIndexes(this IMethod method)
        {
            var signature = method.MethodSig;
            var offset = signature.ImplicitThis ? 1 : 0;
            for (var i = 0; i < signature.Params.Count; i++)
            {
                var param = signature.Params[i]; 
                if (param.IsByRef || param.IsPointer)
                    yield return offset + i;
            }
        }

        public static bool TryGetPushedType(this Instruction instruction, MethodDef method, out ITypeDefOrRef type)
        {
            switch (instruction.OpCode.StackBehaviourPush) 
            {
                case StackBehaviour.Push1:
                {
                    switch (instruction.OpCode.Code)
                    {
                        case Code.Ldarg_0:
                        case Code.Ldarg_1:
                        case Code.Ldarg_2:
                        case Code.Ldarg_3:
                        case Code.Ldarg_S:
                        case Code.Ldarg:
                            type = instruction.GetArgumentType(method.MethodSig, method.DeclaringType).ToTypeDefOrRef();
                            return true;
                        case Code.Ldloc_0:
                        case Code.Ldloc_1:
                        case Code.Ldloc_2:
                        case Code.Ldloc_3:
                        case Code.Ldloc_S:
                        case Code.Ldloc:
                            type = instruction.GetLocal(method.Body.Variables).Type.ToTypeDefOrRef();
                            return true;
                        case Code.Ldfld: 
                        case Code.Ldsfld:
                            type = ((IField) instruction.Operand).FieldSig.Type.ToTypeDefOrRef();
                            return true;
                        case Code.Ldelem:
                            LogUnsupportedInstruction();
                            type = null;
                            return false;
                        case Code.Ldobj:
                            LogUnsupportedInstruction();
                            type = null;
                            return false;
                        case Code.Add:   
                        case Code.Sub:   
                        case Code.Mul:   
                        case Code.Div:   
                        case Code.Div_Un:
                        case Code.Rem:   
                        case Code.Rem_Un:
                        case Code.And:   
                        case Code.Or:    
                        case Code.Xor:   
                        case Code.Shl:   
                        case Code.Shr:   
                        case Code.Shr_Un:
                        case Code.Neg:   
                        case Code.Not:   
                        case Code.Unbox_Any: 
                        case Code.Mkrefany:  
                        case Code.Add_Ovf:   
                        case Code.Add_Ovf_Un:
                        case Code.Mul_Ovf:   
                        case Code.Mul_Ovf_Un:
                        case Code.Sub_Ovf:   
                        case Code.Sub_Ovf_Un:
                            LogUnsupportedInstruction();
                            type = null;
                            return false;
                        default:
                            throw new Exception($"Not supported the instruction {instruction.OpCode.Code} in Push1");
                    }
                }
                case StackBehaviour.Pushi:
                {
                    switch (instruction.OpCode.Code)
                    {
                        case Code.Ldarga_S: 
                        case Code.Ldloca_S:
                        case Code.Ldflda:  
                        case Code.Ldsflda:
                        case Code.Ldelema:   

                        case Code.Ldc_I4_M1:
                        case Code.Ldc_I4_0: 
                        case Code.Ldc_I4_1: 
                        case Code.Ldc_I4_2: 
                        case Code.Ldc_I4_3: 
                        case Code.Ldc_I4_4: 
                        case Code.Ldc_I4_5: 
                        case Code.Ldc_I4_6: 
                        case Code.Ldc_I4_7: 
                        case Code.Ldc_I4_8: 
                        case Code.Ldc_I4_S:
                        case Code.Ldc_I4:   
                        case Code.Ldind_I1: 
                        case Code.Ldind_U1: 
                        case Code.Ldind_I2: 
                        case Code.Ldind_U2: 
                        case Code.Ldind_I4: 
                        case Code.Ldind_U4: 
                        case Code.Ldind_I:  
                        case Code.Conv_I1:  
                        case Code.Conv_I2:  
                        case Code.Conv_I4:  
                        case Code.Conv_U4:  
                        case Code.Isinst:
                        case Code.Unbox:
                        case Code.Conv_Ovf_I1_Un:
                        case Code.Conv_Ovf_I2_Un:
                        case Code.Conv_Ovf_I4_Un:
                        case Code.Conv_Ovf_U1_Un:
                        case Code.Conv_Ovf_U2_Un:
                        case Code.Conv_Ovf_U4_Un:
                        case Code.Conv_Ovf_I_Un:
                        case Code.Conv_Ovf_U_Un:
                        case Code.Ldlen:
                        case Code.Ldelem_I1: 
                        case Code.Ldelem_U1: 
                        case Code.Ldelem_I2: 
                        case Code.Ldelem_U2: 
                        case Code.Ldelem_I4: 
                        case Code.Ldelem_U4: 
                        case Code.Ldelem_I:  
                        case Code.Conv_Ovf_I1:
                        case Code.Conv_Ovf_U1:
                        case Code.Conv_Ovf_I2:
                        case Code.Conv_Ovf_U2:
                        case Code.Conv_Ovf_I4:
                        case Code.Conv_Ovf_U4:
                        case Code.Refanyval: 
                        case Code.Ldtoken:   
                        case Code.Conv_U2:   
                        case Code.Conv_U1:   
                        case Code.Conv_I:    
                        case Code.Conv_Ovf_I:
                        case Code.Conv_Ovf_U:
                        case Code.Conv_U:  
                        case Code.Arglist: 
                        case Code.Ceq:     
                        case Code.Cgt:     
                        case Code.Cgt_Un:  
                        case Code.Clt:     
                        case Code.Clt_Un:  
                        case Code.Ldftn:
                        case Code.Ldvirtftn: 
                        case Code.Ldarga:    
                        case Code.Ldloca:    
                        case Code.Localloc:  
                        case Code.Sizeof:    
                        case Code.Refanytype:
                            LogUnsupportedInstruction();
                            type = null;
                            return false;
                        default:
                            throw new Exception($"Not supported the instruction {instruction.OpCode.Code} in Pushi");
                    }
                }
                case StackBehaviour.Pushi8:
                case StackBehaviour.Pushr4:
                case StackBehaviour.Pushr8:
                    LogUnsupportedInstruction();
                    type = null;
                    return false;
                case StackBehaviour.Pushref:
                {
                    switch (instruction.OpCode.Code)
                    {
                        case Code.Castclass:
                            type = (ITypeDefOrRef) instruction.Operand;
                            return true;
                        case Code.Ldstr:
                        case Code.Newobj:     
                        case Code.Box:        
                        case Code.Newarr:     
                        case Code.Ldind_Ref:  
                            LogUnsupportedInstruction();
                            type = null;
                            return false;
                        case Code.Ldelem_Ref:
                        case Code.Ldnull:     
                            type = null;
                            return false;
                        default:
                            throw new Exception($"Not supported the instruction {instruction.OpCode.Code} in Pushref");
                    }
                }
                case StackBehaviour.Varpush: // only call, calli, callvirt which are handled elsewhere
                {
                    switch (instruction.OpCode.Code)
                    {
                        case Code.Call:
                        case Code.Callvirt:
                            type = ((IMethod) instruction.Operand).MethodSig.RetType.ToTypeDefOrRef();
                            return true;
                        case Code.Calli:
                            LogUnsupportedInstruction();
                            type = null;
                            return false;
                        default:
                            throw new Exception($"Not supported the instruction {instruction.OpCode.Code} in Varpush");
                    }
                }
                case StackBehaviour.Push1_push1:    // for dup need to check previous instructions
                    LogUnsupportedInstruction();
                    type = null;
                    return false;
                case StackBehaviour.Push0:
                default:
                    type = null;
                    return false;
            }

            void LogUnsupportedInstruction()
            {
                //Console.WriteLine($"Can not get type for {instruction} in {method.FullName}");
            }
        }
    }
}