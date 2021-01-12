using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace SerialDetector.Analysis.DataFlow
{
    internal class TaintedSourceInfo
    {
        public TaintedSourceInfo(ulong id, MethodUniqueSignature method, ImmutableStack<string> callStack)
        {
            Id = id;
            Method = method;
            ForwardCallStack = callStack.Pop();
            BackwardCallStack = ImmutableStack<string>.Empty.Push(callStack.Peek());
        }

        public TaintedSourceInfo(ulong id, MethodUniqueSignature method, 
            ImmutableStack<string> forwardCallStack, ImmutableStack<string> backwardCallStack)
        {
            Id = id;
            Method = method;
            ForwardCallStack = forwardCallStack;
            BackwardCallStack = backwardCallStack;
        }

        public TaintedSourceInfo(ulong id, MethodUniqueSignature method, 
            ImmutableStack<string> forwardCallStack, ImmutableStack<string> backwardCallStack,
            ImmutableStack<(MethodUniqueSignature, ImmutableStack<string>)> attackTriggerCalls)
        {
            Id = id;
            Method = method;
            ForwardCallStack = forwardCallStack;
            BackwardCallStack = backwardCallStack;
            AttackTriggerCalls = attackTriggerCalls;
            AttackTriggerCallsCount = attackTriggerCalls.Count();
        }

        public ulong Id { get; }
            
        public MethodUniqueSignature Method { get; }

        public ImmutableStack<string> ForwardCallStack { get; private set; }
        public ImmutableStack<string> BackwardCallStack { get; private set; }

        public ImmutableStack<(MethodUniqueSignature, ImmutableStack<string>)> AttackTriggerCalls { get; private set; } =
            ImmutableStack<(MethodUniqueSignature, ImmutableStack<string>)>.Empty;
            
        public int AttackTriggerCallsCount { get; private set; }

        public void AddAttackTriggerCall((MethodUniqueSignature, ImmutableStack<string>) methodCall)
        {
            AttackTriggerCalls = AttackTriggerCalls.Push(methodCall);
            AttackTriggerCallsCount++;
        }

        public void PushBackwardCallStack(string method)
        {
            BackwardCallStack = BackwardCallStack.Push(method);
            ForwardCallStack = ForwardCallStack.Pop(out var originMethod);
            if (originMethod != method)
            {
                var message = "ERROR in a stack model";
                Debug.Fail(message);
                Console.WriteLine(message);
            }
        }
    }
}