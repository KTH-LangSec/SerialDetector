using System;
using System.Collections.Generic;
using System.Linq;
using SerialDetector.Analysis;
using NUnit.Framework;

namespace SerialDetector.Tests
{
    public class AccessScopeTests : SelfModelBase
    {
        public AccessScopeTests()
            :base("AccessScope")
        {
        }
        
        [Test]
        public void IsPublicGlobalVisibilityTest()
        {
            var result = new Dictionary<string, bool>();
            foreach (var typeDef in EnumerateTypes())
            {
                Console.WriteLine($"{typeDef}");
                foreach (var methodDef in typeDef.Methods)
                {
                    result.Add(
                        $"{typeDef.FullName.Split('.').Last()}.{methodDef.Name}", 
                        methodDef.IsPublicGlobalVisibility());
                }
            }

            Assert.That(result, Is.EquivalentTo(new Dictionary<string, bool>
            {
                {"PublicClass..ctor", true},
                {"PublicClass.PublicMethod", true},
                {"PublicClass.InternalMethod", false},
                {"PublicClass.ProtectedInternalMethod", true},
                {"PublicClass.ProtectedMethod", true},
                {"PublicClass.PrivateMethod", false},
                {"PublicClass/ProtectedNestedClass1..ctor", true},
                {"PublicClass/ProtectedNestedClass1.PublicMethod", true},
                {"PublicClass/ProtectedNestedClass1/PublicNestedClass21..ctor", true},
                {"PublicClass/ProtectedNestedClass1/PublicNestedClass21.PublicMethod", true},
                {"PublicClass/ProtectedNestedClass1/PrivateNestedClass22..ctor", false},
                {"PublicClass/ProtectedNestedClass1/PrivateNestedClass22.PublicMethod", false},
                
                {"InternalClass..ctor", false},
                {"InternalClass.PublicMethod", false},
                {"InternalClass.InternalMethod", false},
                {"InternalClass.ProtectedInternalMethod", false},
                {"InternalClass.ProtectedMethod", false},
                {"InternalClass.PrivateMethod", false},
                {"InternalClass/PublicNestedClass1..ctor", false},
                {"InternalClass/PublicNestedClass1.PublicMethod", false},
            }));
        }
    }
}