using System.Collections.Generic;
using dnlib.DotNet;

namespace SerialDetector.Analysis.DataFlow
{
    internal sealed class ExternalMethodList
    {
        private static MethodUniqueSignature S(string signature) =>
            new MethodUniqueSignature(signature);

        private static readonly HashSet<MethodUniqueSignature> blackList = new HashSet<MethodUniqueSignature>(100);
        private static readonly HashSet<MethodUniqueSignature> whiteList = new HashSet<MethodUniqueSignature>(100);

        static ExternalMethodList()
        {
            blackList.Add(S("System.RuntimeMethodHandle::PerformSecurityCheck(System.Object,System.RuntimeMethodHandleInternal,System.RuntimeType,System.UInt32)"));
            //blackList.Add(S(""));
            
            whiteList.Add(S("System.RuntimeMethodHandle::InvokeMethod(System.Object,System.Object[],System.Signature,System.Boolean)"));
        }

        public bool IsValid(MethodUniqueSignature signature) =>
            //whiteList.Contains(signature);
            !blackList.Contains(signature);
    }
}