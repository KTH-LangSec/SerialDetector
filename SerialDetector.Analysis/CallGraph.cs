using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using dnlib.DotNet.Emit;

namespace SerialDetector.Analysis
{
    public class CallGraph
    {
        public Dictionary<MethodUniqueSignature, ICallGraphNode> Nodes { get; }
        public Dictionary<MethodUniqueSignature, ICallGraphNode> EntryNodes { get; }
        public Dictionary<MethodUniqueSignature, ICallGraphNode> Roots { get; private set; } = new Dictionary<MethodUniqueSignature, ICallGraphNode>();

        public CallGraph()
        {
            Nodes = new Dictionary<MethodUniqueSignature, ICallGraphNode>();
            EntryNodes = new Dictionary<MethodUniqueSignature, ICallGraphNode>();
        }
        
        public CallGraph(int capacityNodes, int capacityEntryPoints)
        {
            Nodes = new Dictionary<MethodUniqueSignature, ICallGraphNode>(capacityNodes);
            EntryNodes = new Dictionary<MethodUniqueSignature, ICallGraphNode>(capacityEntryPoints);
        }

        
        public bool IsEmpty => Nodes.Count == 0;

        public void RemoveUnavailableCalls(IndexDb index)
        {
            var mergedRefs = new HashSet<string>();
            var entryNodes = EntryNodes.Values.Where(x => x.OutNodes.Count > 0).ToList();
            foreach (var entryNode in entryNodes)
            {
                foreach (var ref1 in index.AssemblyReferences[entryNode.AssemblyName])
                {
                    mergedRefs.Add(ref1);
                }
            }
            
            var processingNodes = new Queue<ICallGraphNode>(entryNodes);
            while (processingNodes.Count > 0)
            {
                var node = processingNodes.Dequeue();
                if (node.OutNodes.Count == 0)
                    continue;    // this is a sensitive sink
                
                for (var i = node.OutNodes.Count - 1; i >= 0; i--)
                {
                    var outNode = node.OutNodes[i];
                    if (outNode.OutNodes.Count > 0) // outNode is not sensitive sink
                    {
                        if (node.AssemblyName != outNode.AssemblyName &&
                            !mergedRefs.Contains(outNode.AssemblyName))
                        {
                            node.OutNodes.RemoveAt(i);
                            outNode.InNodes.Remove(node);
                            if (outNode.InNodes.Count == 0)
                            {
                                // outNode is unavailable from entry points
                                RemoveUnavailableNodesFrom(outNode);
                            }
                        }
                        else
                        {
                            processingNodes.Enqueue(outNode);
                        }
                    }
                }
                
                // do it only for non-sensitive sinks
                if (node.OutNodes.Count == 0)
                {
                    RemoveUnavailableNodesTo(node);
                }
            }
        }

        private void RemoveUnavailableNodesFrom(ICallGraphNode node)
        {
            Debug.Assert(node.InNodes.Count == 0);

            Nodes.Remove(node.MethodSignature);
            for (var i = 0; i < node.OutNodes.Count; i++)
            {
                var outNode = node.OutNodes[i];
                outNode.InNodes.Remove(node);
                if (outNode.InNodes.Count == 0)
                {
                    RemoveUnavailableNodesFrom(outNode);
                }
            }
        }

        private void RemoveUnavailableNodesTo(ICallGraphNode node)
        {
            Debug.Assert(node.OutNodes.Count == 0);

            Nodes.Remove(node.MethodSignature);
            if (node.InNodes.Count == 0)
            {
                EntryNodes.Remove(node.MethodSignature);
            }

            for (var i = 0; i < node.InNodes.Count; i++)
            {
                var inNode = node.InNodes[i];
                inNode.OutNodes.Remove(node);
                if (inNode.OutNodes.Count == 0)
                {
                    RemoveUnavailableNodesTo(inNode);
                }
            }
        }

        public void RemoveFrameworkEntryPoints(HashSet<string> frameworkAssemblies)
        {
            var removing = new List<ICallGraphNode>(EntryNodes.Count);
            foreach (var entryNode in EntryNodes.Values)
            {
                if (!frameworkAssemblies.Contains(entryNode.AssemblyName))
                    continue;

                RemoveUnavailableNodesFrom(entryNode);
                removing.Add(entryNode);
            }

            foreach (var entryNode in removing)
            {
                EntryNodes.Remove(entryNode.MethodSignature);
            }
        }

        public void RemoveDuplicatePaths()
        {
            foreach (var node in Nodes.Values)
            {
                RemoveDuplicateFrom(node.OutNodes);
                RemoveDuplicateFrom(node.InNodes);
                
                void RemoveDuplicateFrom(List<ICallGraphNode> list)
                {
                    var cache = new HashSet<ICallGraphNode>();
                    for (var i = list.Count - 1; i >= 0; i--)
                    {
                        var item = list[i];
                        if (cache.Contains(item) || item == node)
                        {
                            list.RemoveAt(i);
                        }
                        else
                        {
                            cache.Add(item);
                        }
                    }
                }
            }
        }

        public void RemoveNonPublicEntryNodes()
        {
            var processingNodes = new Queue<ICallGraphNode>(EntryNodes.Values);
            while (processingNodes.Count > 0)
            {
                var node = processingNodes.Dequeue();
                if (node.IsPublic) continue;
                
                foreach (var outNode in node.OutNodes)
                {
                    if (outNode == node) continue;
                    
                    outNode.InNodes.Remove(node);
                    if ((outNode.InNodes.Count == 0) ||
                        (outNode.InNodes.Count == 1 && outNode.Equals(outNode.InNodes[0])))    // simple recursive calls
                    {
                        // TODO: Add detection of recursive calls like A() -> B() -> A()
                        if (outNode.IsPublic)
                        {
                            EntryNodes.Add(outNode.MethodSignature, outNode);
                        }
                        else
                        {
                            processingNodes.Enqueue(outNode);
                        }
                    }
                }
                        
                EntryNodes.Remove(node.MethodSignature);
                Nodes.Remove(node.MethodSignature);
            }
        }

        public void RemoveNonPublicOneToOneNodes()
        {
            var visitedNodes = new HashSet<ICallGraphNode>(Nodes.Count);
            var processingNodes = new Queue<ICallGraphNode>(EntryNodes.Values);
            while (processingNodes.Count > 0)
            {
                var node = processingNodes.Dequeue();
                if (!visitedNodes.Add(node))
                    continue;

                for (int i = 0; i < node.OutNodes.Count; i++)
                {
                    processingNodes.Enqueue(node.OutNodes[i]);
                }
                
                if (!node.IsPublic && node.InNodes.Count == 1 && node.OutNodes.Count == 1)
                {
                    var inNode = node.InNodes[0];
                    var outNode = node.OutNodes[0];

                    inNode.OutNodes.Remove(node);
                    inNode.OutNodes.Add(outNode);
                    
                    outNode.InNodes.Remove(node);
                    outNode.InNodes.Add(inNode);

                    Nodes.Remove(node.MethodSignature);
                }
            }
        }
        
        public void RemoveNonPublicMiddleNodes()
        {
            var cache = new Dictionary<ICallGraphNode, List<ICallGraphNode>>();
            var processingNodes = new HashSet<ICallGraphNode>(EntryNodes.Values);
            while (processingNodes.Count > 0)
            {
                var node = processingNodes.First();
                processingNodes.Remove(node);
                for (var i = node.OutNodes.Count - 1; i >= 0; i--)
                {
                    var outNode = node.OutNodes[i];
                    if (outNode.OutNodes.Count == 0)
                    {
                        continue;
                    }
                    
                    if (outNode.IsPublic)
                    {
                        processingNodes.Add(outNode);
                        continue;
                    }
                    
                    node.OutNodes.RemoveAt(i);
                    var publicNodes = FindPublicOutNodes(outNode);
                    node.OutNodes.AddRange(publicNodes);
                    foreach (var graphNode in publicNodes)
                    {
                        processingNodes.Add(graphNode);
                    }
                }

                for (var i = node.InNodes.Count - 1; i >= 0; i--)
                {
                    var inNode = node.InNodes[i];
                    if (!inNode.IsPublic && !EntryNodes.ContainsKey(inNode.MethodSignature))
                    {
                        node.InNodes.RemoveAt(i);
                    }
                }
            }

            List<ICallGraphNode> FindPublicOutNodes(ICallGraphNode node)
            {
                if (cache.TryGetValue(node, out var result))
                {
                    return result;
                }

                result = new List<ICallGraphNode>();
                foreach (var outNode in node.OutNodes)
                {
                    if (outNode.IsPublic || outNode.OutNodes.Count == 0)
                    {
                        result.Add(outNode);
                    }
                    else
                    { 
                        result.AddRange(FindPublicOutNodes(outNode));
                    }
                }

                if (node.InNodes.Count > 1)
                {
                    cache.Add(node, result);
                }

                Nodes.Remove(node.MethodSignature);
                return result;
            }
        }
        
        
        public void RemoveMiddleNodes()
        {
            var replacedNodes = new Dictionary<ICallGraphNode, List<ICallGraphNode>>(Nodes.Count / 2);  
            var handledNodes = new HashSet<ICallGraphNode>(Nodes.Count);
            var processingNodes = new Queue<ICallGraphNode>(Roots.Values);
            //Console.WriteLine($"Nodes: {Nodes.Count}");
            while (processingNodes.Count > 0)
            {
                var node = processingNodes.Dequeue();
                if (!handledNodes.Add(node))
                    continue;

                for (var i = node.InNodes.Count - 1; i >= 0; i--)
                {
                    var inNode = node.InNodes[i];
                    if (replacedNodes.TryGetValue(inNode, out var list))
                    {
                        list.Add(node);
                        continue;
                    }

                    if (inNode.AssemblyName == node.AssemblyName &&
                        inNode.InNodes.Count > 0 &&
                        inNode.InNodes.All(x => x.AssemblyName == inNode.AssemblyName))
                    {
                        Nodes.Remove(inNode.MethodSignature);
                        node.InNodes.RemoveAt(i);

                        if (!replacedNodes.TryGetValue(inNode, out list))
                        {
                            list = new List<ICallGraphNode>();
                            replacedNodes.Add(inNode, list);
                        }

                        if (replacedNodes.TryGetValue(node, out var previousList))
                        {
                            list.AddRange(previousList);
                        }
                        else
                        {
                            list.Add(node);
                        }
                    }

                    processingNodes.Enqueue(inNode);
                }
            }

            //Console.WriteLine($"Nodes: {Nodes.Count}");
            ///////////////////////////////////////////////////////////////////////
            handledNodes.Clear();
            processingNodes = new Queue<ICallGraphNode>(EntryNodes.Values);
            while (processingNodes.Count > 0)
            {
                var node = processingNodes.Dequeue();
                if (!handledNodes.Add(node))
                    continue;

                for (var i = node.OutNodes.Count - 1; i >= 0; i--)
                {
                    var outNode = node.OutNodes[i];
                    if (replacedNodes.TryGetValue(outNode, out var list))
                    {
                        node.OutNodes.RemoveAt(i);
                        node.OutNodes.AddRange(list);
                        foreach (var replacedNode in list)
                        {
                            replacedNode.InNodes.Add(node);
                            processingNodes.Enqueue(replacedNode);
                        }
                    }
                    else
                    {
                        processingNodes.Enqueue(outNode);
                    }
                }
            }
        }
        
        public void RemoveSameClasses()
        {
            var handledNodes = new HashSet<ICallGraphNode>(Nodes.Count);
            var processingNodes = new Queue<(ICallGraphNode, ICallGraphNode)>();
            
            EntryNodes.Clear();
            Nodes.Clear();
            var roots = Roots.Values;
            Roots = new Dictionary<MethodUniqueSignature, ICallGraphNode>();
            foreach (var rootNode in roots)
            {
                // we keep all parents of root(s)
                if (!handledNodes.Add(rootNode))
                    continue;
                
                var mappedRootNode = new CallInfoNode(rootNode.Info);
                Roots.Add(mappedRootNode.MethodSignature, mappedRootNode);
                Nodes.Add(mappedRootNode.MethodSignature, mappedRootNode);
                if (rootNode.InNodes.Count == 0 && !EntryNodes.ContainsKey(mappedRootNode.MethodSignature))
                    EntryNodes.Add(mappedRootNode.MethodSignature, mappedRootNode);
                    
                foreach (var inNode in rootNode.InNodes)
                {
                    var mappedNode = new CallInfoNode(inNode.Info);
                    mappedRootNode.InNodes.Add(mappedNode);
                    mappedNode.OutNodes.Add(mappedRootNode);
                    
                    if (!Nodes.ContainsKey(mappedNode.MethodSignature))
                    {
                        Nodes.Add(mappedNode.MethodSignature, mappedNode);
                        if (inNode.InNodes.Count == 0)
                        {
                            if (!EntryNodes.ContainsKey(mappedNode.MethodSignature))
                                EntryNodes.Add(mappedNode.MethodSignature, mappedNode);
                        }
                        else
                        {
                            processingNodes.Enqueue((inNode, mappedNode));    
                        }
                    }

                }
            }
            
            // and keep only different classes for other parents
            while (processingNodes.Count > 0)
            {
                var (node, mappedNode) = processingNodes.Dequeue();
                if (!handledNodes.Add(node))
                    continue;

                var mappedInNodes = new HashSet<ICallGraphNode>(mappedNode.InNodes);
                foreach (var inNode in node.InNodes)
                {
                    var classSignature = new MethodUniqueSignature(inNode.MethodSignature.ToClassName());
                    if (Nodes.TryGetValue(classSignature, out var mappedInNode))
                    {
                        if (mappedInNode != mappedNode && mappedInNodes.Add(mappedInNode))
                        {
                            mappedInNode.OutNodes.Add(mappedNode);
                        }
                    }
                    else
                    {
                        mappedInNode = new CallInfoNode(new CallInfo(
                            0,
                            null,
                            inNode.AssemblyInfo,
                            classSignature,
                            inNode.IsPublic,
                            null));
                        
                        mappedInNodes.Add(mappedInNode);
                        mappedInNode.OutNodes.Add(mappedNode);
                        Nodes.Add(mappedInNode.MethodSignature, mappedInNode);
                    }

                    if (inNode.InNodes.Count == 0)
                    {
                        if (!EntryNodes.ContainsKey(mappedInNode.MethodSignature))
                            EntryNodes.Add(mappedInNode.MethodSignature, mappedInNode);
                    }
                    else
                    {
                        processingNodes.Enqueue((inNode, mappedInNode));
                    }
                }

                if (mappedNode.InNodes.Count != mappedInNodes.Count)
                {
                    mappedNode.InNodes.Clear();
                    mappedNode.InNodes.AddRange(mappedInNodes);
                }
            }
        }
        
        public void Dump(string path)
        {
            Console.WriteLine($"{path}: EntryPoints ({EntryNodes.Count}), Nodes ({Nodes.Count})");
            DumpGraph(path, Nodes.Values);
            DumpStat(path, EntryNodes.Values, Nodes.Count);
        }

        public void DumpSeparateUsages(string directory)
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var levelOneNodes = 0;
            foreach (var root in Roots.Values)
            {
                foreach (var node in root.InNodes)
                {
                    var path = Path.Combine(directory, $"graph_{levelOneNodes++}.png"); 
                    var nodeCount = DumpGraph(path, TraverseNodes(root, node));
                    DumpStat(path, lastTraversalEntryPoints, nodeCount);
                }
            }
        }

        private static void DumpStat(string path, ICollection<ICallGraphNode> entryNodes, int nodeCount)
        {
            var entryPointsStatPath = Path.Combine(
                Path.GetDirectoryName(path) ?? String.Empty,
                Path.GetFileNameWithoutExtension(path) + ".stat.txt");
            var statByAssemblies = entryNodes.GroupBy(
                node => node.AssemblyName, 
                (key, nodes) => $"{key}: {nodes.Count()} ({nodes.Count(node => node.IsPublic)} public)");
            //(key, nodes) => new {Name = key, Count = nodes.Count()});
            File.WriteAllLines(entryPointsStatPath, statByAssemblies);
            File.AppendAllText(entryPointsStatPath, "\n");
            File.AppendAllText(entryPointsStatPath, $"Total Entry Points: {entryNodes.Count} ({entryNodes.Count(node => node.IsPublic)} public)\n");
            File.AppendAllText(entryPointsStatPath, $"Total Nodes: {nodeCount}\n");
        }

        private static int DumpGraph(string path, IEnumerable<ICallGraphNode> nodes)
        {
            var count = 0;
            try
            {
                // may try >sfdp -x -Goverlap=prism -Tpng magic.gv > data.png 
                var pathGraphVizFile = Path.Combine(
                    Path.GetDirectoryName(path) ?? String.Empty, 
                    Path.GetFileNameWithoutExtension(path) + ".gv");
                var graphViz = new GraphVizCallGraph(nodes);
                count = graphViz.Save(pathGraphVizFile);
                if (count < 0 || count > 1000)
                    return count;
                
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "dot",
                    Arguments = "\"" + pathGraphVizFile + "\" -Tpng -o \"" + path + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                });

                process?.WaitForExit();
                //File.Delete(pathGraphVizFile);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error create a graph dump: {0}", exception);
            }

            return count;
        }

        private List<ICallGraphNode> lastTraversalEntryPoints = new List<ICallGraphNode>();
        private IEnumerable<ICallGraphNode> TraverseNodes(
            ICallGraphNode rootNode, ICallGraphNode levelOneNode)
        {
            lastTraversalEntryPoints.Clear();
            var nodes = new HashSet<ICallGraphNode>(Nodes.Count);
            nodes.Add(rootNode);
            yield return rootNode;

            var queue = new Queue<ICallGraphNode>();
            queue.Enqueue(levelOneNode);
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                if (!nodes.Add(node))
                    continue;
                
                yield return node;
                if (node.InNodes.Count == 0)
                {
                    lastTraversalEntryPoints.Add(node);
                }
                else
                {
                    foreach (var inNode in node.InNodes)
                    {
                        queue.Enqueue(inNode);
                    }
                }
            }
        }

        public HashSet<MethodUniqueSignature> DumpAssemblies(string path, HashSet<string> frameworkAssemblies)
        {
            try
            {
                // may try >sfdp -x -Goverlap=prism -Tpng magic.gv > data.png 
                var pathGraphVizFile = Path.Combine(
                    Path.GetDirectoryName(path) ?? String.Empty, 
                    Path.GetFileNameWithoutExtension(path) + ".gv");
                var graphViz = new GraphVizAssemblyGraph(this, frameworkAssemblies);
                var entryPoints = graphViz.Save(pathGraphVizFile);
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = "dot",
                    Arguments = "\"" + pathGraphVizFile + "\" -Tpng -o \"" + path + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                });

                //process?.WaitForExit();
                //File.Delete(pathGraphVizFile);
                return entryPoints;
            }
            catch (Exception exception)
            {
                Console.WriteLine("Error create a graph dump: {0}", exception);
                return new HashSet<MethodUniqueSignature>();
            }
        }
    }
}