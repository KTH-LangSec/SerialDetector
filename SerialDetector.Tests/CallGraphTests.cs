using SerialDetector.Analysis;
using dnlib.DotNet;
using NUnit.Framework;

namespace SerialDetector.Tests
{
    public class CallGraphTests
    {
        [Test]
        public void Dump()
        {
            // A B
            //  C
            // D E
            //  F
            var a = CreateCallGraphNode("A");
            var b = CreateCallGraphNode("B");
            var c = CreateCallGraphNode("C");
            var d = CreateCallGraphNode("D");
            var e = CreateCallGraphNode("E");
            var f = CreateCallGraphNode("F");
            Link(a, c);
            Link(b, c);
            Link(c, d);
            Link(c, e);
            Link(d, f);
            Link(e, f);

            var callGraph = CreateCallGraph(a, b, c, d, e, f);
            callGraph.Dump(@"DumpTest.png");
            Assert.True(true);
        }

        private ICallGraphNode CreateCallGraphNode(string name)
        {
            var info = CallInfo.CreateFake(
                new AssemblyInfo(UTF8String.Empty, AssemblyInfo.EmptyVersion), 
                MethodUniqueSignature.Create(name));
            
            return new CallInfoNode(info);
        }

        private void Link(ICallGraphNode from, ICallGraphNode to)
        {
            from.OutNodes.Add(to);
            to.InNodes.Add(from);
        }

        private CallGraph CreateCallGraph(params ICallGraphNode[] nodes)
        {
            var callGraph = new CallGraph();
            foreach (var node in nodes)
            {
                callGraph.Nodes.Add(node.MethodSignature, node);
            }

            return callGraph;
        }
    }
}