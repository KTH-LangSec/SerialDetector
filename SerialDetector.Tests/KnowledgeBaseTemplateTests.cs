using System;
using System.Linq;
using System.Reflection;
using SerialDetector.Analysis;
using SerialDetector.KnowledgeBase;
using NUnit.Framework;
using SerialDetector.KnowledgeBase.Templates;

namespace SerialDetector.Tests
{
    public class KnowledgeBaseTemplateTests
    {
        private readonly IndexDb index;
        
        public KnowledgeBaseTemplateTests()
        {
            index = new IndexDb(typeof(Context).Assembly.Location);
            index.Build();
        }

        [Test]
        //[Ignore("for debugging only, change type and methodName vars for your case")]
        public void SingleCase()
        {
            var type = typeof(XslCompiledTransformTemplates);
            var methodName = nameof(XslCompiledTransformTemplates.XsltLoadWithPayload); 

            var context = Context.CreateToAnalyze();
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            var errors = Loader.ExecuteCase(context, type, new[] {method});
            //var templateGroup = Loader.GetTemplateGroup(type, methodName);

            Assert.That(errors, Is.Empty);
            Assert.That(context.Templates.Count, Is.GreaterThan(0));
            
            var builder = new CallGraphBuilder(index);
            var graph = builder.CreateGraph(context.Templates);
            
            Assert.That(graph.EntryNodes.Count, Is.GreaterThan(0));
            Assert.That(graph.EntryNodes.Keys
                .Contains(MethodUniqueSignature.Create($"{type.FullName}::{methodName}()")));
        }
        
        [Test]
        public void AllCases()
        {
            foreach (var type in Loader.GetCaseTypes())
            {
                Console.WriteLine($"{type}:");
                foreach (var method in Loader.GetCaseMethods(type))
                {
                    Console.WriteLine($"    {method}");
                    
                    var context = Context.CreateToAnalyze();
                    var errors = Loader.ExecuteCase(context, type, new[] {method});
                    
                    Assert.That(errors, Is.Empty);
                    Assert.That(context.Templates.Count, Is.GreaterThan(0));
                    
                    var builder = new CallGraphBuilder(index);
                    var graph = builder.CreateGraph(context.Templates);
            
                    Assert.That(graph.EntryNodes.Count, Is.GreaterThan(0));
                    Assert.That(graph.EntryNodes.Keys
                        .Contains(MethodUniqueSignature.Create($"{type.FullName}::{method.Name}()")));
                }
            }
        }
    }
}