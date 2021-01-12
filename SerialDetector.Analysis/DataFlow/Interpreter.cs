using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using SerialDetector.Analysis.DataFlow.Context;
using SerialDetector.Analysis.DataFlow.Symbolic;
using StackFrame = SerialDetector.Analysis.DataFlow.Context.StackFrame;

namespace SerialDetector.Analysis.DataFlow
{
    internal sealed class Interpreter : IInterpreter
    {
        private readonly MethodDef method;
        private readonly uint[] offsets;
        private readonly ExecutionContext context;
        private readonly Context.StackFrame[] frames;
        private readonly bool[] backwardInterpretedJumps;
        //private readonly SymbolicSlot returnValue;
        //private readonly List<IEffect> effects = new List<IEffect>();

        public Interpreter(MethodDef method)
            :this(new ExecutionContext(ImmutableStack<string>.Empty, method.CreateMethodUniqueSignature(), method, false, false), method)
        {
        }
        
        public Interpreter(ExecutionContext context, MethodDef method)
        {
            this.context = context;
            this.method = method;
            offsets = method.Body.Instructions.Select(i => i.Offset).ToArray();
            frames = new Context.StackFrame[method.Body.Instructions.Count];
            backwardInterpretedJumps = new bool[method.Body.Instructions.Count];
        }
        
        public IEnumerable<IEffect> EnumerateEffects()
        {
            //Console.WriteLine($"EnumerateEffects: {method.FullName}");
            method.Body.SimplifyMacros(method.Parameters);

            int index = 0;
            while (index < method.Body.Instructions.Count)
            {
                UpdateContext(index);
                if (context.SkipMode)
                {
                    index++;
                    continue;
                }
                
                var (newEffect, backwardJump) = Interpret(index);
                if (newEffect != null)
                    yield return newEffect;

                index = backwardJump != 0 ? backwardJump : index + 1;
            }
        }

        private void FakeUpdateFrame(Instruction instruction)
        {
            instruction.CalculateStackUsage(method.HasReturnType, out var pushes, out var pops);
            while (pops-- > 0)
                context.Frame.Pop();
            while (pushes-- > 0)
                context.Frame.Push(ExecutionContext.FakeSymbolicValue);
        }

        private void UpdateContext(int index)
        {
            var frame = frames[index];
            if (frame == null)
                return;

            Debug.Assert(frame != null);
            context.Frame = context.Frame != null 
                ? StackFrame.Merge(context.Frame, frame) 
                : frame;
            
            context.SkipMode = false;
            frames[index] = null;
        }
        
        private int AddCondition(Instruction jumpInstruction, int currentIndex, Context.StackFrame frame)
        {
            int jumpIndex = Array.BinarySearch(offsets, jumpInstruction.Offset);
            Debug.Assert(jumpIndex >= 0);

            var instructionFrame = frames[jumpIndex];
            if (instructionFrame != null)
            {
                frames[jumpIndex] = StackFrame.Merge(instructionFrame, frame);
            }
            else
            {
                frames[jumpIndex] = frame;
            }

            if (jumpIndex < currentIndex && !backwardInterpretedJumps[currentIndex])
            {
                backwardInterpretedJumps[currentIndex] = true;
                return jumpIndex;
            }

            return 0;
        }
        
        private static bool IsSystemVoid(TypeSig type) => 
            type.RemovePinnedAndModifiers().GetElementType() == ElementType.Void;

        private MethodCallEffect BuildMethodCallEffect(Instruction instruction, CallKind callKind)
        {
            var methodOperand = (IMethod) instruction.Operand;
            var parameterCount = methodOperand.GetParameterCount();
            var symbolicParameters = new SymbolicSlot[parameterCount];
            for (int i = parameterCount - 1; i >= 0; i--)
            {
                symbolicParameters[i] = context.Frame.Pop();
            }
                    
            var signature = methodOperand.MethodSig;
            SymbolicSlot symbolicRetSlot = null;
            if (!IsSystemVoid(signature.RetType))
            {
                symbolicRetSlot = new SymbolicSlot(
                    new SymbolicReference(
                        new MethodReturnSource(methodOperand)));
                context.Frame.Push(symbolicRetSlot);
            }

            return new MethodCallEffect(
                methodOperand,
                symbolicParameters,
                symbolicRetSlot,
                callKind);
        }
        
        private MethodCallEffect BuildCtorCallEffect(Instruction instruction)
        {
            var methodOperand = (IMethod) instruction.Operand;
            var signature = methodOperand.MethodSig;
            var parameterCount = methodOperand.GetParameterCount();
            var symbolicParameters = new SymbolicSlot[parameterCount];
            var lastParameter = signature.ImplicitThis ? 1 : 0;
            for (int i = parameterCount - 1; i >= lastParameter; i--)
            {
                symbolicParameters[i] = context.Frame.Pop();
            }

            var thisReturnValue = 
                new SymbolicSlot(
                    new SymbolicReference(
                        new MethodReturnSource(methodOperand)));
            if (signature.ImplicitThis)
            {
                symbolicParameters[0] = thisReturnValue;
            }
                    
            context.Frame.Push(thisReturnValue);
            return new CtorCallEffect(
                methodOperand,
                symbolicParameters,
                thisReturnValue);
        }
        
        private void EnableSkipMode()
        {
            context.Frame = null;
            context.SkipMode = true;
        }

        public static bool IsSimple(TypeSig type)
        {
            if (type.IsModifier)
                return IsSimple(type.Next);
            
            if (type.IsSingleOrMultiDimensionalArray)
                return IsSimple(type.Next);

            
            if (type.IsPrimitive)
                return true;

            var fullName = type.ReflectionFullName;
            if (fullName == "System.String" ||
                fullName == "System.DateTime")
                return true;

            return false;
            
            // note any type with modifier returns IsCorLibType == false
            //return type.IsCorLibType && !type.ReflectionFullName.Equals("System.Object");
        }

        private (IEffect, int) Interpret(int currentIndex)
        {
            var instruction = method.Body.Instructions[currentIndex];
            switch (instruction.OpCode.Code)
            {
                case Code.Add:
                case Code.Add_Ovf:
                case Code.Add_Ovf_Un:
                case Code.Sub:
                case Code.Sub_Ovf:
                case Code.Sub_Ovf_Un: // CHECK arithmetic operations
                {
                    FakeUpdateFrame(instruction);
                    break;
                    // implemented because they can be used for pointers 
                    var value2 = context.Frame.Pop();
                    var value1 = context.Frame.Pop();

                    var value1ContainsNotConst = value1.ContainsNotConst(); 
                    var value2ContainsNotConst = value2.ContainsNotConst();
                    if (value1ContainsNotConst && !value2ContainsNotConst)
                    {
                        context.Frame.Push(value1);    
                    }
                    else if (!value1ContainsNotConst && value2ContainsNotConst)
                    {
                        context.Frame.Push(value2);    
                    }
                    else if (value1ContainsNotConst && value2ContainsNotConst)
                    {
                        //context.Frame.Push(SymbolicSlot.Merge(value1, value2));    
                        //Console.WriteLine("Add/sub two not constants (lose tracing vars)!");
                        context.Frame.Push(value1);    
                    } 
                    else
                    {
                        context.Frame.Push(ExecutionContext.FakeSymbolicValue);
                    }
                    
                    break;
                }

                case Code.And:
                case Code.Arglist:    // CHECK
                    FakeUpdateFrame(instruction);
                    break;
                case Code.Beq:
                case Code.Bge:
                case Code.Bge_Un:
                case Code.Bgt:
                case Code.Bgt_Un:
                case Code.Ble:
                case Code.Ble_Un:
                case Code.Blt:
                case Code.Blt_Un:
                case Code.Bne_Un:
                {
                    FakeUpdateFrame(instruction);
                    var (firstFrame, secondFrame) = context.Frame.Fork();
                    var jumpIndex = AddCondition(
                        (Instruction) instruction.Operand,
                        currentIndex,
                        firstFrame);
                    context.Frame = secondFrame;
                    return (null, jumpIndex);
                }
                case Code.Box:    // CHECK ref
                    // now ignore any transformation like cast-op
                    break;
                case Code.Br:
                {
                    var jumpIndex = AddCondition(
                        (Instruction) instruction.Operand,
                        currentIndex,
                        context.Frame);
                    EnableSkipMode();
                    return (null, jumpIndex);
                }
                case Code.Break: // CHECK
                    break;
                case Code.Brfalse:
                {
                    // Transfers control to a target instruction if value is false, a null reference, or zero
                    FakeUpdateFrame(instruction);
                    var (firstFrame, secondFrame) = context.Frame.Fork();
                    var jumpIndex = AddCondition(
                        (Instruction) instruction.Operand,
                        currentIndex,
                        firstFrame);
                    context.Frame = secondFrame;
                    return (null, jumpIndex);
                }
                case Code.Brtrue:
                {
                    // Transfers control to a target instruction if value is true, not null, or non-zero
                    FakeUpdateFrame(instruction);
                    var (firstFrame, secondFrame) = context.Frame.Fork(); 
                    var jumpIndex = AddCondition(
                        (Instruction) instruction.Operand,
                        currentIndex,
                        firstFrame);
                    context.Frame = secondFrame;
                    return (null, jumpIndex);
                }

                case Code.Call:
                    // need to add order number for the effect
                    return (BuildMethodCallEffect(instruction, CallKind.Concrete), 0);
                case Code.Calli:
                    throw new Exception($"Not supported calli opcode {method.Module} {method} ({instruction})");
                    // https://docs.microsoft.com/en-us/dotnet/api/system.reflection.emit.opcodes.calli
                    break;
                case Code.Callvirt:
                {
                    var callKind = CallKind.Virtual;
                    var methodOperand = ((IMethod)instruction.Operand).ResolveMethodDef();
                    if (methodOperand != null && !methodOperand.IsVirtual)
                    {
                        Debug.Assert(!methodOperand.IsAbstract);
                        callKind = CallKind.Concrete;
                    }
                    
                    return (BuildMethodCallEffect(instruction, callKind), 0);
                }
                case Code.Castclass:    // CHECK implement later!
                    // don't change a frame stack to keep tainted symbolic value in the stack
                    break;
                case Code.Ceq:
                case Code.Cgt:
                case Code.Cgt_Un:
                    FakeUpdateFrame(instruction);
                    break;
                case Code.Ckfinite:
                    // Throws ArithmeticException if value is not a finite number
                    // don't change a frame stack to keep tainted symbolic value in the stack
                    break;
                case Code.Clt:
                case Code.Clt_Un:
                    FakeUpdateFrame(instruction);
                    break;
                case Code.Constrained:
                    // see Code.Callvirt
                    FakeUpdateFrame(instruction);
                    break;
                case Code.Conv_I:
                case Code.Conv_I1:
                case Code.Conv_I2:
                case Code.Conv_I4:
                case Code.Conv_I8:
                case Code.Conv_Ovf_I:
                case Code.Conv_Ovf_I_Un:
                case Code.Conv_Ovf_I1:
                case Code.Conv_Ovf_I1_Un:
                case Code.Conv_Ovf_I2:
                case Code.Conv_Ovf_I2_Un:
                case Code.Conv_Ovf_I4:
                case Code.Conv_Ovf_I4_Un:
                case Code.Conv_Ovf_I8:
                case Code.Conv_Ovf_I8_Un:
                case Code.Conv_Ovf_U:
                case Code.Conv_Ovf_U_Un:
                case Code.Conv_Ovf_U1:
                case Code.Conv_Ovf_U1_Un:
                case Code.Conv_Ovf_U2:
                case Code.Conv_Ovf_U2_Un:
                case Code.Conv_Ovf_U4:
                case Code.Conv_Ovf_U4_Un:
                case Code.Conv_Ovf_U8:
                case Code.Conv_Ovf_U8_Un:
                case Code.Conv_R_Un:
                case Code.Conv_R4:
                case Code.Conv_R8:
                case Code.Conv_U:
                case Code.Conv_U1:
                case Code.Conv_U2:
                case Code.Conv_U4:
                case Code.Conv_U8:
                    // pop value, convert to a certain type, and push onto the stack
                    // just ignore converting and information about types for now
                    break;
                case Code.Cpblk:    // CHECK add stat
                    // Copies a specified number bytes from a source address to a destination address
                    FakeUpdateFrame(instruction);
                    break;
                case Code.Cpobj:    // CHECK add stat
                    // Copies the value type located at the address of an object (type &, or native int) to the address of the destination object (type &, or native int).
                    FakeUpdateFrame(instruction);
                    break;
                case Code.Div:
                case Code.Div_Un:
                    FakeUpdateFrame(instruction);
                    break;
                case Code.Dup:    // CHECK for value types (need clone SymbolicObject?)
                    context.Frame.Push(context.Frame.Peek());
                    break;
                case Code.Endfilter:
                    // TODO:
                    //Transfers control from the filter clause of an exception back to the CLI exception handler.
                    break;
                case Code.Endfinally:
                    // TODO:
                    // Transfers control from the fault or finally clause of an exception block back to the CLI exception handler.
                    break;
                case Code.Initblk:
                    // Initializes a specified block of memory at a specific address to a given size and initial value.
                    FakeUpdateFrame(instruction);
                    break;
                case Code.Initobj:
                    // Initializes each field of the value type at a specified address to a null reference or a 0 of the appropriate primitive type
                    FakeUpdateFrame(instruction);
                    break;
                case Code.Isinst:    // CHECK cast
                    // Tests whether an object reference (type O) is an instance of a particular class.
                    FakeUpdateFrame(instruction);
                    break;
                case Code.Jmp:
                    // TODO: like a call with exit semantic
                    // Exits current method and jumps to specified method
                    break;
                case Code.Ldarga:    // CHECK ref
                case Code.Ldarg:
                    var argument = (Parameter) instruction.Operand;
                    if (IsSimple(argument.Type))
                    {
                        FakeUpdateFrame(instruction);
                        break;
                    }

                    context.Frame.Push(
                        context.Arguments.Load(
                            argument.Index, 
                            argument.Name));
                    break;
                case Code.Ldc_I4:
                case Code.Ldc_I8:
                case Code.Ldc_R4:
                case Code.Ldc_R8:
                    FakeUpdateFrame(instruction);
                    break;
                case Code.Ldelem:
                case Code.Ldelem_I:
                case Code.Ldelem_I1:
                case Code.Ldelem_I2:
                case Code.Ldelem_I4:
                case Code.Ldelem_I8:
                case Code.Ldelem_R4:
                case Code.Ldelem_R8:
                case Code.Ldelem_Ref:
                case Code.Ldelem_U1:
                case Code.Ldelem_U2:
                case Code.Ldelem_U4:
                case Code.Ldelema: // CHECK array
                {
                    var index = context.Frame.Pop();
                    var target = context.Frame.Pop();
                    if (target.IsConstAfterSimplification())
                    {
                        context.Frame.Push(ExecutionContext.FakeSymbolicValue);
                        break;
                    }
                    
                    context.Frame.Push(target.LoadField(SymbolicReference.ArrayElement));
                    break;
                }
                case Code.Ldflda:    // CHECK ref
                case Code.Ldfld:
                {
                    var operand = (IField) instruction.Operand;
                    if (IsSimple(operand.FieldSig.Type))
                    {
                        FakeUpdateFrame(instruction);
                        break;
                    }
                    
                    var target = context.Frame.Pop();
                    context.Frame.Push(
                        target.LoadField(((IFullName)instruction.Operand).Name));    //FieldDefMD
                    break;
                }
                case Code.Ldftn:
                    FakeUpdateFrame(instruction);
                    break;
                case Code.Ldind_I:
                case Code.Ldind_I1:
                case Code.Ldind_I2:
                case Code.Ldind_I4:
                case Code.Ldind_I8:
                case Code.Ldind_R4:
                case Code.Ldind_R8:
                case Code.Ldind_Ref:
                case Code.Ldind_U1:
                case Code.Ldind_U2:
                case Code.Ldind_U4:
                case Code.Ldobj:    // CHECK ref
                {
                    // do nothing because we have the same symbolic object for address and value
                    // see a stack semantic by https://docs.microsoft.com/en-us/dotnet/api/system.reflection.emit.opcodes.ldobj?view=netframework-4.8 
                    break;
                }
                case Code.Ldlen:        //CHECK array
                    FakeUpdateFrame(instruction);
                    break;
                case Code.Ldloca: // ref
                case Code.Ldloc:
                {
                    var operand = (Local) instruction.Operand;
                    if (IsSimple(operand.Type))
                    {
                        FakeUpdateFrame(instruction);
                        break;
                    }

                    context.Frame.Push(
                        context.Variables.Load(operand.Index));
                    break;
                }
                case Code.Ldnull:
                    context.Frame.Push(ExecutionContext.SymbolicNull);
                    break;
                case Code.Ldsflda: // ref
                case Code.Ldsfld:
                {
                    var operand = (IField) instruction.Operand;
                    if (IsSimple(operand.FieldSig.Type))
                    {
                        FakeUpdateFrame(instruction);
                        break;
                    }

                    context.Frame.Push(
                        context.Static.LoadField(
                            ((IFullName) instruction.Operand).FullName.Split(' ')[1])); //FieldDefMD
                    break;
                }
                case Code.Ldstr:
                    context.Frame.Push(
                        new SymbolicSlot(
                            new SymbolicReference(
                                new ConstSource((string) instruction.Operand)))); 
                    break;
                case Code.Ldtoken:    // reflection
                    FakeUpdateFrame(instruction);
                    break;
                case Code.Ldvirtftn:    // for calli
                    FakeUpdateFrame(instruction);
                    break;
                case Code.Leave: // CHECK try/catch
                {
                    // temporary solution: skipping catch-block as a code after br-opcode
                    var jumpIndex = AddCondition((Instruction) instruction.Operand,
                        currentIndex,
                        context.Frame);
                    // TODO: Debug.Assert(jumpIndex == 0);
                    EnableSkipMode();
                    break;
                }
                case Code.Localloc:
                    FakeUpdateFrame(instruction);
                    break;
                case Code.Mkrefany:    // ref?
                    FakeUpdateFrame(instruction);
                    break;
                case Code.Mul:
                case Code.Mul_Ovf:
                case Code.Mul_Ovf_Un:
                case Code.Neg:
                    // ignore any calculation of build-in types
                    FakeUpdateFrame(instruction);
                    break;
                case Code.Newarr:
                {
                    var count = context.Frame.Pop();
                    var arrayObject = 
                        new SymbolicSlot(
                            new SymbolicReference(
                                new NewArraySource(((IFullName)instruction.Operand).FullName)));
                    context.Frame.Push(arrayObject);
                    break;
                }
                case Code.Newobj:
                    return (BuildCtorCallEffect(instruction), 0);
                case Code.Nop:
                    break;
                case Code.Not:
                case Code.Or:
                    // logic operators
                    FakeUpdateFrame(instruction);
                    break;
                case Code.Pop:
                    context.Frame.Pop();
                    break;
                case Code.Readonly:    // CHECK ldelema
                    FakeUpdateFrame(instruction);
                    break;
                case Code.Refanytype:    // CHECK reflection??
                    FakeUpdateFrame(instruction);
                    break;
                case Code.Refanyval:    // CHECK reflection??
                    FakeUpdateFrame(instruction);
                    break;
                case Code.Rem:
                case Code.Rem_Un:
                    FakeUpdateFrame(instruction);
                    break;
                case Code.Ret:
                {
                    if (method.HasReturnType)
                    {
                        var value = context.Frame.Pop();
                        if (!value.IsConstAfterSimplification() &&
                            !IsSimple(method.ReturnType))
                        {
                            context.AddReturnValue(value);
                        }
                    }

                    EnableSkipMode();
                    break;
                }
                case Code.Rethrow:
                    FakeUpdateFrame(instruction);
                    EnableSkipMode();
                    break;
                case Code.Shl:
                case Code.Shr:
                case Code.Shr_Un:
                    FakeUpdateFrame(instruction);
                    break;
                case Code.Sizeof:
                    FakeUpdateFrame(instruction);
                    break;
                case Code.Starg:
                {
                    var operand = (Parameter) instruction.Operand;
                    if (IsSimple(operand.Type))
                    {
                        FakeUpdateFrame(instruction);
                        break;
                    }
                    
                    context.Arguments.Store(
                        operand.Index,
                        context.Frame.Pop());
                    break;
                }
                case Code.Stelem: // CHECK array
                case Code.Stelem_I:
                case Code.Stelem_I1:
                case Code.Stelem_I2:
                case Code.Stelem_I4:
                case Code.Stelem_I8:
                case Code.Stelem_R4:
                case Code.Stelem_R8:
                case Code.Stelem_Ref: // CHECK ref
                {
                    var value = context.Frame.Pop();
                    var index = context.Frame.Pop();
                    var target = context.Frame.Pop();
                    //if (value.IsSimpleConst)
                    //    break;
                    
                    target.StoreField(SymbolicReference.ArrayElement, value);
                    break;
                }
                case Code.Stfld:
                {
                    var value = context.Frame.Pop();
                    var target = context.Frame.Pop();
                    var operand = (IField) instruction.Operand;
                    if (IsSimple(operand.FieldSig.Type))
                        break;
                    
                    /*
                    // HACK DBNull
                    // the method System.Net.HttpWebRequest::GetResponse()
                    // executes too much time w/o this restriction
                    // IsTainted() runs much times 
                    if (value.ToString().EndsWith("Static[System.DBNull::Value]"))
                        break;
                    */
                    
                    target.StoreField(operand.Name, value);
                    break;
                }
                case Code.Stind_I:
                case Code.Stind_I1:
                case Code.Stind_I2:
                case Code.Stind_I4:
                case Code.Stind_I8:
                case Code.Stind_R4:
                case Code.Stind_R8:
                case Code.Stind_Ref:
                case Code.Stobj:     // CHECK ref
                {
                    var value = context.Frame.Pop();
                    var address = context.Frame.Pop();
                    address.Store(value);
                    break;
                }
                case Code.Stloc:
                {
                    var operand = (Local) instruction.Operand;
                    if (IsSimple(operand.Type))
                    {
                        FakeUpdateFrame(instruction);
                        break;
                    }

                    context.Variables.Store(
                        operand.Index,
                        context.Frame.Pop());
                    break;
                }
                case Code.Stsfld:
                {
                    var value = context.Frame.Pop();
                    var operand = (IField) instruction.Operand;
                    if (IsSimple(operand.FieldSig.Type))
                        break;

                    context.Static.StoreField(
                        operand.FullName.Split(' ')[1],
                        value);
                    break;
                }
                case Code.Switch:
                    FakeUpdateFrame(instruction);
                    var currentFrame = context.Frame;
                    var instructions = (Instruction[]) instruction.Operand;
                    for (var i = instructions.Length - 1; i >= 0; i--)
                    {
                        Context.StackFrame firstFrame;
                        (firstFrame, currentFrame) = currentFrame.Fork();
                        var jumpIndex = AddCondition(
                            instructions[i],
                            currentIndex,
                            firstFrame);
                        //TODO: Debug.Assert(jumpIndex == 0);
                    }
                    
                    context.Frame = currentFrame;
                    break;
                case Code.Tailcall:    // call
                    FakeUpdateFrame(instruction);
                    break;
                case Code.Throw:
                    FakeUpdateFrame(instruction);
                    EnableSkipMode();
                    break;
                case Code.Unaligned:    // unmanaged
                    FakeUpdateFrame(instruction);
                    break;
                case Code.Unbox:    // Check boxing
                case Code.Unbox_Any:
                    break;
                case Code.Volatile:    // CHECK ???
                    FakeUpdateFrame(instruction);
                    break;
                case Code.Xor:    // CHECK logic operations
                    FakeUpdateFrame(instruction);
                    break;
                
                case Code.Beq_S:
                case Code.Bge_S:
                case Code.Bge_Un_S:  
                case Code.Bgt_S:
                case Code.Bgt_Un_S:
                case Code.Ble_S:
                case Code.Ble_Un_S:
                case Code.Blt_S:
                case Code.Blt_Un_S:
                case Code.Bne_Un_S:
                case Code.Br_S:
                case Code.Brfalse_S:
                case Code.Brtrue_S:
                case Code.Ldarg_0:
                case Code.Ldarg_1:
                case Code.Ldarg_2:
                case Code.Ldarg_3:
                case Code.Ldarg_S:
                case Code.Ldarga_S:
                case Code.Ldc_I4_0:
                case Code.Ldc_I4_1:
                case Code.Ldc_I4_2:
                case Code.Ldc_I4_3:
                case Code.Ldc_I4_4:
                case Code.Ldc_I4_5:
                case Code.Ldc_I4_6:
                case Code.Ldc_I4_7:
                case Code.Ldc_I4_8:
                case Code.Ldc_I4_M1:
                case Code.Ldc_I4_S:
                case Code.Ldloc_0:
                case Code.Ldloc_1:
                case Code.Ldloc_2:
                case Code.Ldloc_3:
                case Code.Ldloc_S:
                case Code.Ldloca_S:
                case Code.Leave_S:
                case Code.Starg_S:
                case Code.Stloc_0:
                case Code.Stloc_1:
                case Code.Stloc_2:
                case Code.Stloc_3:
                case Code.Stloc_S:
                    throw new NotSupportedException($"Must be simplified {instruction.OpCode.Code}");
                
                case Code.UNKNOWN1:
                case Code.UNKNOWN2:
                case Code.Prefix1:
                case Code.Prefix2:
                case Code.Prefix3:
                case Code.Prefix4:
                case Code.Prefix5:
                case Code.Prefix6:
                case Code.Prefix7:
                case Code.Prefixref:
                    //This is a reserved instruction.
                    throw new NotSupportedException($"Not supported {instruction.OpCode.Code}");
                
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return (null, 0);
        }
    }
}