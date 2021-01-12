using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using SerialDetector.KnowledgeBase;
using NUnit.Framework;
using SerialDetector.KnowledgeBase.Templates;

namespace SerialDetector.Tests
{
    public class KnowledgeBasePayloadTests
    {
        public KnowledgeBasePayloadTests()
        {
            var directory = Path.GetDirectoryName(typeof(KnowledgeBasePayloadTests).Assembly.Location);
            Environment.CurrentDirectory = directory ?? throw new InvalidOperationException("Assembly.Location is null");
        }
        
        [Test]
        //[Ignore("for debugging only, change type and methodName vars for your case")]
        public void SingleCase()
        {
            var type = typeof(XmlSerializerTemplates);
            var methodName = nameof(XmlSerializerTemplates.Deserialize); 
            
            var id = Guid.NewGuid().ToString();
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            var errors = Test(id, type, method);
            foreach (var error in errors)
            {
                Console.WriteLine(error);
            }
            
            if (!DeleteFileLoop(id))
            {
                errors.Add($"The payload has not been executed ({id})");    
            }

            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void AllCases()
        {
            var files = new Dictionary<string, string>();
            var errors = new List<string>();
            foreach (var type in Loader.GetCaseTypes())
            {
                Console.WriteLine($"{type}:");
                foreach (var method in Loader.GetCaseMethods(type))
                {
                    Console.WriteLine($"    {method}");
                    var id = Guid.NewGuid().ToString();
                    files.Add(id, $"{type}::{method.Name}()");
                    errors.AddRange(Test(id, type, method));
                }
            }
            
            Console.WriteLine("ERRORS:");
            foreach (var error in errors)
            {
                Console.WriteLine(error);
            }
            
            DeleteFilesLoop(files);

            Assert.That(files, Is.Empty);
            //Assert.That(errors, Is.Empty);
        }

        private static List<string> Test(string id, Type type, MethodInfo method)
        {
            var context = Context.CreateToTest($"echo some-text > {id}",
                (PayloadGenerationMode mode, Payload payload, ref bool interrupt) =>
                {
                    if (mode == PayloadGenerationMode.PayloadFileBased)
                        return;

                    // remove a file if it was created during payload generation 
                    // TODO: add warning to log
                    DeleteFileLoop(id);
                });
            
            return Loader.ExecuteCase(context, type, new[] {method});
        }

        private static void DeleteFilesLoop(Dictionary<string, string> files)
        {
            var attempt = 0;
            while (attempt++ < 10 && files.Count > 0)
            {
                Thread.Sleep(100);
                
                var removals = new List<string>();
                foreach (var id in files.Keys)
                {
                    if (File.Exists(id))
                    {
                        DeleteFileSafe(id);
                        removals.Add(id);
                    }
                }

                foreach (var id in removals)
                {
                    files.Remove(id);
                }
            }
        }
        
        internal static bool DeleteFileLoop(string fileName)
        {
            var attempt = 0;
            while (attempt++ < 5)
            {
                Thread.Sleep(100);
                if (File.Exists(fileName))
                {
                    DeleteFileSafe(fileName);
                    return true;
                }
            }

            return false;
        }

        internal static void DeleteFileSafe(string fileName)
        {
            try
            {
                File.Delete(fileName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}