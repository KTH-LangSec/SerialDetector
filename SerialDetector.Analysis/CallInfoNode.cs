using System.Collections.Generic;
using dnlib.DotNet;

namespace SerialDetector.Analysis
{
    public class CallInfoNode : ICallGraphNode
    {
        private class ShadowNode : ICallGraphNode
        {
            private readonly ICallGraphNode node;

            public ShadowNode(ICallGraphNode node)
            {
                this.node = node;
            }

            public string PriorColor => node.PriorColor;
            public AssemblyInfo AssemblyInfo => node.AssemblyInfo;
            public string AssemblyName => node.AssemblyName;
            public MethodUniqueSignature MethodSignature => node.MethodSignature;
            public bool IsPublic => node.IsPublic;
            public MethodDef MethodDef => node.MethodDef;

            public CallInfo Info => node.Info;
            
            public List<ICallGraphNode> InNodes { get; } = new List<ICallGraphNode>();
            public List<ICallGraphNode> OutNodes { get; } = new List<ICallGraphNode>();

            public ICallGraphNode CreateShadowCopy()
            {
                throw new System.NotImplementedException();
            }

            public void ResetShadowCopy()
            {
                throw new System.NotImplementedException();
            }

            public ICallGraphNode ShadowCopy { get; }
        }

        private readonly CallInfo info;

        private ShadowNode shadowNode;

        public CallInfoNode(CallInfo info)
        {
            this.info = info;
            InNodes = new List<ICallGraphNode>();
            OutNodes = new List<ICallGraphNode>();
        }

        public string PriorColor => null;
        public AssemblyInfo AssemblyInfo => info.AssemblyInfo;
        public string AssemblyName => info.AssemblyInfo.Name;

        public MethodUniqueSignature MethodSignature => info.Signature;

        public bool IsPublic => info.IsPublic;

        public MethodDef MethodDef => info.MethodDef;

        public List<ICallGraphNode> InNodes { get; }
        public List<ICallGraphNode> OutNodes { get; }

        public CallInfo Info => info;
        
        public ICallGraphNode CreateShadowCopy()
        {
            if (shadowNode == null) 
                shadowNode = new ShadowNode(this);

            return shadowNode;
        }

        public void ResetShadowCopy()
        {
            shadowNode = null;
        }

        public ICallGraphNode ShadowCopy => shadowNode;

        public override string ToString() => $"{MethodSignature} v.{AssemblyInfo.Version}";
    }
}