using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SerialDetector.Analysis
{
    internal sealed class GraphVizAssemblyGraph
    {
        private readonly string[] subgraphColors = new[] { "blue", "darkslateblue", "orange3", "violetred4", "purple4", "orangered4" };
        private readonly CallGraph data;
        private readonly HashSet<string> frameworkAssemblies;

        public GraphVizAssemblyGraph(CallGraph data, HashSet<string> frameworkAssemblies)
        {
            this.data = data;
            this.frameworkAssemblies = frameworkAssemblies;
        }

        public HashSet<MethodUniqueSignature> Save(string fileName)
        {
            using (var writer = new StreamWriter(fileName))
            {
                return Save(writer);
            }
        }

        private HashSet<MethodUniqueSignature> Save(TextWriter writer)
        {
            writer.WriteLine("digraph G {");
            writer.WriteLine("node [fontsize = 16];");

            var assembliesInFramework = new HashSet<string>();
            var assembliesInNotFramework = new HashSet<string>();
            var frameworkEntryPoints = new HashSet<MethodUniqueSignature>();
            var assemblyMap = new Dictionary<string, HashSet<string>>();
            foreach (var node in data.Nodes.Values)
            {
                var nodeName = node.AssemblyName;
                if (String.IsNullOrEmpty(nodeName))
                {
                    nodeName = node.MethodSignature.ToString();
                }
                
                if (!assemblyMap.TryGetValue(nodeName, out var list))
                {
                    list = new HashSet<string>();
                    assemblyMap.Add(nodeName, list);
                }

                if (frameworkAssemblies.Contains(nodeName))
                {
                    assembliesInFramework.Add(nodeName);
                }
                else
                {
                    assembliesInNotFramework.Add(nodeName);
                }
                
                foreach (var outNode in node.OutNodes)
                {
                    var outNodeName = outNode.AssemblyName;
                    if (String.IsNullOrEmpty(outNodeName))
                    {
                        outNodeName = outNode.MethodSignature.ToString();
                    }

                    if (outNodeName == nodeName)
                        continue;
                    
                    if (frameworkAssemblies.Contains(outNodeName))
                    {
                        assembliesInFramework.Add(outNodeName);
                        if (!frameworkAssemblies.Contains(nodeName))
                        {
                            frameworkEntryPoints.Add(outNode.MethodSignature);
                        }
                    }
                    else
                    {
                        assembliesInNotFramework.Add(outNodeName);
                    }

                    list.Add(outNodeName);
                }
            }
            
            ulong nodeId = 0;
            var ids = new Dictionary<string, ulong>();
            foreach (var assembly in assembliesInNotFramework)
            {
                ids.Add(assembly, nodeId);
                
                writer.Write(nodeId);
                writer.Write(" [");
                var isFirst = true;
                WriteAttribute(writer, "label", assembly, ref isFirst);
                WriteAttribute(writer, "shape", "box", ref isFirst);
//                WriteAttribute(writer, "style", "", ref isFirst);
//                WriteAttribute(writer, "fillcolor", "", ref isFirst);
//                WriteAttribute(writer, "color", "", ref isFirst);
//                WriteAttribute(writer, "fontcolor", "", ref isFirst);
                writer.WriteLine("];");
                nodeId++;
            }
            
            writer.Write("subgraph cluster_framework");
            writer.WriteLine(" {");
            writer.Write("label=\"");
            writer.Write("Framework");
            writer.WriteLine("\";");
            writer.Write("color=");
            writer.Write("orangered4");
            writer.WriteLine(";");
            writer.Write("fontcolor=");
            writer.Write("orangered4");
            writer.WriteLine(";");
                    
            writer.WriteLine("penwidth=2;");
            writer.WriteLine("labeljust=l;");

            foreach (var assembly in assembliesInFramework)
            {
                ids.Add(assembly, nodeId);
                
                writer.Write(nodeId);
                writer.Write(" [");
                var isFirst = true;
                WriteAttribute(writer, "label", assembly, ref isFirst);
                WriteAttribute(writer, "shape", "box", ref isFirst);
//                WriteAttribute(writer, "style", "", ref isFirst);
//                WriteAttribute(writer, "fillcolor", "", ref isFirst);
//                WriteAttribute(writer, "color", "", ref isFirst);
//                WriteAttribute(writer, "fontcolor", "", ref isFirst);
                writer.WriteLine("];");
                nodeId++;
            }
            
            writer.WriteLine("}");
            
            foreach (var pair in assemblyMap)
            {
                var from = pair.Key;
                foreach (var to in pair.Value)
                {
                    SaveEdge(ids[from], ids[to], writer);
                }
            }

            writer.WriteLine("}");

            return frameworkEntryPoints;
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