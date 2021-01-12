using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SerialDetector.Analysis
{
    internal class GraphVizCallGraph
    {
        private readonly string[] subgraphColors = new[] { "blue", "darkslateblue", "orange3", "violetred4", "purple4", "orangered4" };
        private readonly IEnumerable<ICallGraphNode> nodes;

        public GraphVizCallGraph(IEnumerable<ICallGraphNode> nodes)
        {
            this.nodes = nodes;
        }

        public int Save(string fileName)
        {
            using (var writer = new StreamWriter(fileName))
            {
                return Save(writer);
            }
        }

        private int Save(TextWriter writer)
        {
            writer.WriteLine("digraph G {");
            writer.WriteLine("node [fontsize = 16];");

            var assemblyMap = new Dictionary<string, List<ICallGraphNode>>();
            var nodeCount = 0;
            foreach (var node in nodes)
            {
                nodeCount++;
                if (assemblyMap.TryGetValue(node.AssemblyName, out var list))
                {
                    list.Add(node);
                }
                else
                {
                    assemblyMap.Add(node.AssemblyName, new List<ICallGraphNode> {node});
                }
            }

            
            ulong nodeId = 0;
            ulong clusterId = 0;
            int subgraphColorId = 0;
            var ids = new Dictionary<ICallGraphNode, ulong>(nodeCount);
            foreach (var pair in assemblyMap)
            {
                if (!String.IsNullOrWhiteSpace(pair.Key))
                {
                    writer.Write("subgraph cluster_");
                    writer.Write(clusterId++);
                    writer.WriteLine(" {");
                    
                    writer.Write("label=\"");
                    writer.Write(pair.Key);
                    writer.WriteLine("\";");

                    subgraphColorId++;
                    if (subgraphColorId >= subgraphColors.Length)
                    {
                        subgraphColorId = 0;
                    }
                    
                    writer.Write("color=");
                    writer.Write(subgraphColors[subgraphColorId]);
                    writer.WriteLine(";");
                    
                    writer.Write("fontcolor=");
                    writer.Write(subgraphColors[subgraphColorId]);
                    writer.WriteLine(";");
                    
                    writer.WriteLine("penwidth=2;");
                    writer.WriteLine("labeljust=l;");
                }

                foreach (var node in pair.Value)
                {
                    ids.Add(node, nodeId);
                    SaveVertex(nodeId, node, writer);
                    nodeId++;
                }
                
                if (!String.IsNullOrWhiteSpace(pair.Key))
                {
                    writer.WriteLine("}");
                }
            }

            foreach (var node in nodes)
            {
                foreach (var outNode in node.OutNodes)
                {
                    SaveEdge(ids[node], ids[outNode], writer);
                }
            }

            writer.WriteLine("}");
            return nodeCount;
        }

        private void SaveVertex(ulong id, ICallGraphNode node, TextWriter writer)
        {
            GetVertexAttributes(node, out var shape, out var style, out var color, out var fontcolor);

            writer.Write(id);
            writer.Write(" [");
            var isFirst = true;
            WriteAttribute(writer, "label", node.MethodSignature.ToShortString(), ref isFirst);
            WriteAttribute(writer, "shape", shape, ref isFirst);
            WriteAttribute(writer, "style", style, ref isFirst);
            WriteAttribute(writer, "fillcolor", color, ref isFirst);
            WriteAttribute(writer, "color", color, ref isFirst);
            WriteAttribute(writer, "fontcolor", fontcolor, ref isFirst);
            writer.WriteLine("];");
        }

        private void SaveEdge(ulong from, ulong to, TextWriter writer)
        {
            writer.Write(from);
            writer.Write(" -> ");
            writer.Write(to);
            writer.Write(" [");
            var isFirst = true;
            WriteAttribute(writer, "color", "gray50", ref isFirst);
            writer.WriteLine("];");
        }

        private static void WriteAttribute(TextWriter writer, string name, string value, ref bool isFirst)
        {
            if (String.IsNullOrEmpty(value)) 
                return;

            if (isFirst)
                isFirst = false;
            else
                writer.Write(", ");

            writer.Write("{0}=\"{1}\"", name, value);
            //writer.Write("{0}={1}", name, Escape(value));
        }

        private static string Escape(string text)
        {
            if (Regex.IsMatch(text, @"^[\w\d]+$"))
            {
                return text;
            }

            return "\"" + text.Replace("\\", "\\\\").Replace("\r", "").Replace("\n", "\\n").Replace("\"", "\\\"") + "\"";
        }

        private void GetVertexAttributes(ICallGraphNode node, out string shape, out string style,
            out string color, out string fontcolor)
        {
            // https://www.graphviz.org/doc/info/colors.html
            // https://www.graphviz.org/doc/info/shapes.html
            // 

            if (!String.IsNullOrEmpty(node.PriorColor))
            {
                shape = "box";
                style = "filled";
                color = node.PriorColor;
                fontcolor = "";
            }
            else if (node.OutNodes.Count == 0)
            {
                shape = "box";
                style = "filled";
                color = "brown1";
                fontcolor = "";
            }
            else if (node.InNodes.Count == 0)
            {
                shape = "box";
                style = "filled, rounded";
                if (node.IsPublic)
                {
                    color = "darkgreen";
                    fontcolor = "white";
                }
                else
                {
                    color = "darkgoldenrod1";
                    fontcolor = "";
                }
            }
            else
            {
                shape = "box";
                if (node.IsPublic)
                {
                    style = "filled";
                    color = "green2";
                }
                else
                {
                    style = "";
                    color = "";
                }
                
                fontcolor = "";
            }
        }
    }
}