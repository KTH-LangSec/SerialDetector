using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SerialDetector.Analysis.DataFlow.Context;
using SerialDetector.Analysis.DataFlow.Symbolic;

namespace SerialDetector.Analysis
{
    internal sealed class GraphVizSummary
    {
        private readonly Summary summary;

        public GraphVizSummary(Summary summary)
        {
            this.summary = summary;
        }
        
        public void Save(string fileName)
        {
            using (var writer = new StreamWriter(fileName))
            {
                Save(writer);
            }
        }
        
        private void Save(TextWriter writer)
        {
            writer.WriteLine("digraph G {");
            writer.WriteLine("node [fontsize = 14];");
            writer.WriteLine("newrank=true;");

            var nodes = new Dictionary<SymbolicReference, ulong>();
            var edges = new HashSet<(ulong, ulong, string)>();
            
            ulong nodeId = 0;
            ulong clusterId = 0;
            
            // writing clusters
            var topNodes = new HashSet<SymbolicReference>(summary.Static.FieldCount + 2);
            StartSubGraph(clusterId++, "Arguments", "orange3", writer);
            var argumentsId = AddNode(writer, nodes, new SymbolicReference(), "Args", ref nodeId);
            for (var i = 0; i < summary.Arguments.Length; i++)
            {
                var argument = summary.Arguments[i];
                var id = AddNode(writer, nodes, argument, GetLabel(argument), ref nodeId);
                edges.Add((argumentsId, id, $"arg{i}"));
                topNodes.Add(argument);
            }
            EndSubGraph(writer);
            
            if (summary.ReturnValue != null)
            {
                StartSubGraph(clusterId++, "Return", "darkorange4", writer);
                AddNode(writer, nodes, summary.ReturnValue, "Ret", ref nodeId);
                topNodes.Add(summary.ReturnValue);
                EndSubGraph(writer);
            }
            
            StartSubGraph(clusterId++, "Static", "darkslateblue", writer);
            foreach (var field in summary.Static.Fields)
            {
                AddNode(writer, nodes, field.Value, GetLabel(field.Value), ref nodeId);
                topNodes.Add(field.Value);
            }
            EndSubGraph(writer);
            
            writer.WriteLine($"{{ rank=same; {String.Join("; ", topNodes.Select(n => nodes[n]))}}}");
            
            // writing nodes
            for (var i = 0; i < summary.Arguments.Length; i++)
            {
                var argument = summary.Arguments[i];
                Traverse(writer, argument, nodes, edges, ref nodeId);
            }

            if (summary.ReturnValue != null)
            {
                Traverse(writer, summary.ReturnValue, nodes, edges, ref nodeId);
            }
            
            foreach (var field in summary.Static.Fields)
            {
                Traverse(writer, field.Value, nodes, edges, ref nodeId);
            }

            // write edges
            foreach (var (from, to, label) in edges)
            {
                SaveEdge(from, to, label, writer);
            }

            writer.WriteLine("}");
        }

        private void StartSubGraph(ulong clusterId, string name, string color, TextWriter writer)
        {
            writer.Write("subgraph cluster_");
            writer.Write(clusterId);
            writer.WriteLine(" {");
                    
            writer.Write("label=\"");
            writer.Write(name);
            writer.WriteLine("\";");

            writer.Write("color=");
            writer.Write(color);
            writer.WriteLine(";");
                    
            writer.Write("fontcolor=");
            writer.Write(color);
            writer.WriteLine(";");
                    
            writer.WriteLine("penwidth=2;");
            writer.WriteLine("labeljust=l;");
        }

        private void EndSubGraph(TextWriter writer)
        {
            writer.WriteLine("}");
        }

        private ulong AddNode(TextWriter writer, Dictionary<SymbolicReference, ulong> nodes, SymbolicReference node,
            string label, ref ulong nodeId)
        {
            if (nodes.TryGetValue(node, out var id))
            {
                return id;
            }

            nodeId++;
            nodes.Add(node, nodeId);
            SaveNode(nodeId, label, writer);
            return nodeId;
        }
        
        private void SaveNode(ulong id, string label, TextWriter writer)
        {
            writer.Write(id);
            writer.Write(" [");
            var isFirst = true;
            WriteAttribute(writer, "label", label, ref isFirst);
            WriteAttribute(writer, "shape", "box", ref isFirst);
            //WriteAttribute(writer, "style", style, ref isFirst);
            //WriteAttribute(writer, "fillcolor", color, ref isFirst);
            //WriteAttribute(writer, "color", color, ref isFirst);
            //WriteAttribute(writer, "fontcolor", fontcolor, ref isFirst);
            writer.WriteLine("];");
        }
        
        private void SaveEdge(ulong from, ulong to, string label, TextWriter writer)
        {
            writer.Write(from);
            writer.Write(" -> ");
            writer.Write(to);
            writer.Write(" [");
            var isFirst = true;
            switch (label)
            {
                case SymbolicReference.PossibleInputMark:
                    WriteAttribute(writer, "color", "dodgerblue3", ref isFirst);
                    WriteAttribute(writer, "style", "dotted", ref isFirst);
                    break;
                case SymbolicReference.PossibleTaintedMark:
                    WriteAttribute(writer, "color", "indianred3", ref isFirst);
                    WriteAttribute(writer, "style", "dashed", ref isFirst);
                    break;
                default:
                    WriteAttribute(writer, "color", "gray40", ref isFirst);
                    WriteAttribute(writer, "label", label, ref isFirst);
                    break;
            }
            
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

        private void Traverse(TextWriter writer,
            SymbolicReference rootNode, 
            Dictionary<SymbolicReference, ulong> nodes, 
            HashSet<(ulong, ulong, string)> edges,
            ref ulong nodeId)
        {
            var queue = new Queue<SymbolicReference>();
            queue.Enqueue(rootNode);
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                var parentId = nodes[node];
                foreach (var field in node.Fields)
                {
                    var fieldName = field.Key;
                    var fieldNode = field.Value;
                    var id = GetOrCreateNode(fieldNode, ref nodeId);
                    edges.Add((parentId, id, fieldName));
                }

                foreach (var possibleTainted in node.PossibleInputEntities)
                {
                    var id  = GetOrCreateNode(possibleTainted, ref nodeId);
                    edges.Add((parentId, id, SymbolicReference.PossibleInputMark));
                }
                
                foreach (var possibleTainted in node.PossibleTaintedEntities)
                {
                    var id  = GetOrCreateNode(possibleTainted, ref nodeId);
                    edges.Add((parentId, id, SymbolicReference.PossibleTaintedMark));
                }
            }

            ulong GetOrCreateNode(SymbolicReference fieldNode, ref ulong counter)
            {
                if (nodes.TryGetValue(fieldNode, out var id))
                {
                    return id;
                }

                var label = GetLabel(fieldNode);
                AddNode(writer, nodes, fieldNode, label, ref counter);
                queue.Enqueue(fieldNode);
                return counter;
            }
        }

        private static string GetLabel(SymbolicReference node)
        {
            var label = $"{(node.HasTargetMethods ? "M" : "")}{(node.IsTainted() ? "T" : node.IsInput() ? "I" : "")}";
            if (String.IsNullOrEmpty(label))
            {
                label = " ";
            }

            return label;
        }
    }
}