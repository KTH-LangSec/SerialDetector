using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using SerialDetector.KnowledgeBase;
using NUnit.Framework;

namespace SerialDetector.Tests
{
    public class PayloadGenerationCompletedTests
    {
        [Test]
        public void GenerationPayloadShouldNotExecuteCommandFail()
        {
            var id = Guid.NewGuid().ToString();
            var errors = ExecuteOnlyPayloadGenerationCall(id, null);
            
            // the 'errors' list must contain the error "The payload has not been executed" in a success case
            Assert.That(errors, Is.Empty);
        }

        [Test]
        public void GenerationPayloadShouldNotExecuteCommandSuccess()
        {
            var fileCreated = false;
            var id = Guid.NewGuid().ToString();
            var errors = ExecuteOnlyPayloadGenerationCall(id,
                (PayloadGenerationMode mode, Payload payload, ref bool interrupt) =>
                {
                    fileCreated = KnowledgeBasePayloadTests.DeleteFileLoop(id);
                });
            
            Assert.That(fileCreated, Is.True);
            Assert.That(errors.Count, Is.EqualTo(1));
            Assert.That(errors[0], Does.StartWith("The payload has not been executed"));
        }

        private List<string> ExecuteOnlyPayloadGenerationCall(string id, Context.PayloadGenerationCompleted action)
        {
            var type = typeof(KnowledgeBaseFake.BinaryFormatterTemplatesFake);
            var methodName = nameof(KnowledgeBaseFake.BinaryFormatterTemplatesFake.OnlyPayloadGenerationCall); 
            var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
            
            var context = Context.CreateToTest($"echo some-text > {id}", action);
            
            var errors = Loader.ExecuteCase(context, type, new[] {method});

            var attempt = 0;
            while (attempt++ < 5)
            {
                Thread.Sleep(100);
                if (File.Exists(id))
                {
                    KnowledgeBasePayloadTests.DeleteFileSafe(id);
                    return errors;
                }
            }
            
            errors.Add($"The payload has not been executed ({id})");
            return errors;
        }
    }
}