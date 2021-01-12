using System.Collections.Generic;
using dnlib.DotNet;

namespace SerialDetector.Analysis
{
    public interface ICallGraphNode
    {
        string PriorColor { get; }
        AssemblyInfo AssemblyInfo { get; }
        string AssemblyName { get; }

        MethodUniqueSignature MethodSignature { get; }

        bool IsPublic { get; }

        /*internal*/ MethodDef MethodDef { get; }

        CallInfo Info { get; }

        List<ICallGraphNode> InNodes { get; }
        List<ICallGraphNode> OutNodes { get; }

        ICallGraphNode CreateShadowCopy();
        void ResetShadowCopy();
        ICallGraphNode ShadowCopy { get; }
    }
}