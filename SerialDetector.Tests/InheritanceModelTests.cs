using System.Collections.Generic;
using System.Linq;
using SerialDetector.Analysis;
using NUnit.Framework;

namespace SerialDetector.Tests
{
    // TODO: check call Explicit method
    public class InheritanceModelTests : SelfModelBase
    {
        public InheritanceModelTests()
            :base("Inheritance")
        {
        }
        
        [Test]
        public void MethodOverridesTest()
        {
            var result = new Dictionary<string, string[]>();
            foreach (var typeDef in EnumerateTypes())
            {
                foreach (var methodDef in typeDef.Methods)
                {
                    if (methodDef.Name == ".ctor") continue;
                    
                    //Console.WriteLine($"{methodDef.FullName}: {methodDef.HasOverrides}");
                    result.Add(methodDef.FullName, methodDef.FindOverrides().Select(md => md.FullName).ToArray());
                }
            }
            
            Assert.That(result, Is.EquivalentTo(new Dictionary<string, string[]>
            {
                {
                    "System.Void SerialDetector.Tests.Model.Inheritance.AbstractClass::Foo()", 
                    new []{"System.Void SerialDetector.Tests.Model.Inheritance.IInterface::Foo()"}
                },
                {
                    "System.Void SerialDetector.Tests.Model.Inheritance.AbstractClass::Bar()", 
                    new string[0]
                },
                {
                    "System.Void SerialDetector.Tests.Model.Inheritance.AbstractClass2::Bar()",
                    new [] {"System.Void SerialDetector.Tests.Model.Inheritance.AbstractClass::Bar()"}
                },
                {
                    "System.Void SerialDetector.Tests.Model.Inheritance.AbstractClass2::BarAC2()",
                    new string[0]
                },
                {
                    "System.Void SerialDetector.Tests.Model.Inheritance.DerivedClass::Foo()", 
                    new []
                    {
                        "System.Void SerialDetector.Tests.Model.Inheritance.SpecificClass::Foo()",
                        "System.Void SerialDetector.Tests.Model.Inheritance.IInterface::Foo()",
                        "System.Void SerialDetector.Tests.Model.Inheritance.AbstractClass::Foo()"
                    }
                },
                {
                    "System.Void SerialDetector.Tests.Model.Inheritance.DerivedClass::Bar()", 
                    new []
                    {
                        "System.Void SerialDetector.Tests.Model.Inheritance.SpecificClass::Bar()",
                        "System.Void SerialDetector.Tests.Model.Inheritance.AbstractClass2::Bar()",
                        "System.Void SerialDetector.Tests.Model.Inheritance.AbstractClass::Bar()"
                    }
                },
                {
                    "System.Void SerialDetector.Tests.Model.Inheritance.DerivedClass::JustVirtual()", 
                    new []{"System.Void SerialDetector.Tests.Model.Inheritance.SpecificClass::JustVirtual()"}
                },
                {
                    "System.Void SerialDetector.Tests.Model.Inheritance.DerivedClass::SerialDetector.Tests.Model.Inheritance.IInterface2.Explicit()", 
                    new []{"System.Void SerialDetector.Tests.Model.Inheritance.IInterface2::Explicit()"}
                },
                {
                    "System.Void SerialDetector.Tests.Model.Inheritance.IInterface::Foo()", 
                    new string[0]
                },
                {
                    "System.Void SerialDetector.Tests.Model.Inheritance.IInterface2::Explicit()", 
                    new string[0]
                },
                {
                    "System.Void SerialDetector.Tests.Model.Inheritance.SpecificClass::JustVirtual()", 
                    new string[0]
                },
                {
                    "System.Void SerialDetector.Tests.Model.Inheritance.SpecificClass::Bar()", 
                    new []
                    {
                        "System.Void SerialDetector.Tests.Model.Inheritance.AbstractClass2::Bar()",
                        "System.Void SerialDetector.Tests.Model.Inheritance.AbstractClass::Bar()"
                    }
                },
                {
                    "System.Void SerialDetector.Tests.Model.Inheritance.SpecificClass::BarAC2()", 
                    new []
                    {
                        "System.Void SerialDetector.Tests.Model.Inheritance.AbstractClass2::BarAC2()"
                    }
                },
                {
                    "System.Void SerialDetector.Tests.Model.Inheritance.SpecificClass::Foo()", 
                    new []
                    {
                        "System.Void SerialDetector.Tests.Model.Inheritance.IInterface::Foo()",
                        "System.Void SerialDetector.Tests.Model.Inheritance.AbstractClass::Foo()"
                    }
                },
            }));
        }

        [Test]
        public void MethodImplementationsTest()
        {
            var result = new Dictionary<string, string[]>();
            var index = new IndexDb(GetType().Assembly.Location);
            index.Build();
            foreach (var typeDef in EnumerateTypes())
            {
                foreach (var methodDef in typeDef.Methods)
                {
                    if (methodDef.Name == ".ctor") continue;
                    
                    result.Add(
                        methodDef.FullName, 
                        index.GetImplementations(methodDef.CreateMethodUniqueSignature(), (string) null)
                            .Select(md => md.FullName)
                            .ToArray());
                }
            }

/*            
            foreach (var item in result)
            {
                Console.WriteLine(item.Key);
                foreach (var value in item.Value)
                {
                    Console.WriteLine($"    {value}");
                }
            }
*/            
            
            Assert.That(result, Is.EquivalentTo(new Dictionary<string, string[]>
            {
                {
                    "System.Void SerialDetector.Tests.Model.Inheritance.AbstractClass::Foo()", 
                    new []
                    {
                        "System.Void SerialDetector.Tests.Model.Inheritance.DerivedClass::Foo()", 
                        "System.Void SerialDetector.Tests.Model.Inheritance.SpecificClass::Foo()"
                    }
                },
                {
                    "System.Void SerialDetector.Tests.Model.Inheritance.AbstractClass::Bar()", 
                    new []
                    {
                        "System.Void SerialDetector.Tests.Model.Inheritance.DerivedClass::Bar()",
                        "System.Void SerialDetector.Tests.Model.Inheritance.SpecificClass::Bar()"
                    }
                },
                {
                    "System.Void SerialDetector.Tests.Model.Inheritance.AbstractClass2::Bar()", 
                    new []
                    {
                        "System.Void SerialDetector.Tests.Model.Inheritance.DerivedClass::Bar()",
                        "System.Void SerialDetector.Tests.Model.Inheritance.SpecificClass::Bar()"
                    }
                },
                {
                    "System.Void SerialDetector.Tests.Model.Inheritance.AbstractClass2::BarAC2()", 
                    new []
                    {
                        "System.Void SerialDetector.Tests.Model.Inheritance.SpecificClass::BarAC2()"
                    }
                },
                {
                    "System.Void SerialDetector.Tests.Model.Inheritance.DerivedClass::Foo()", 
                    new string[0]
                },
                {
                    "System.Void SerialDetector.Tests.Model.Inheritance.DerivedClass::Bar()", 
                    new string[0]
                },
                {
                    "System.Void SerialDetector.Tests.Model.Inheritance.DerivedClass::JustVirtual()", 
                    new string[0]
                },
                {
                    "System.Void SerialDetector.Tests.Model.Inheritance.DerivedClass::SerialDetector.Tests.Model.Inheritance.IInterface2.Explicit()", 
                    new string[0]
                },
                {
                    "System.Void SerialDetector.Tests.Model.Inheritance.IInterface::Foo()", 
                    new []
                    {
                        "System.Void SerialDetector.Tests.Model.Inheritance.DerivedClass::Foo()",
                        "System.Void SerialDetector.Tests.Model.Inheritance.SpecificClass::Foo()"
                    }
                },
                {
                    "System.Void SerialDetector.Tests.Model.Inheritance.IInterface2::Explicit()", 
                    new []
                    {
                        "System.Void SerialDetector.Tests.Model.Inheritance.DerivedClass::SerialDetector.Tests.Model.Inheritance.IInterface2.Explicit()"
                    }
                },
                {
                    "System.Void SerialDetector.Tests.Model.Inheritance.SpecificClass::JustVirtual()", 
                    new [] {"System.Void SerialDetector.Tests.Model.Inheritance.DerivedClass::JustVirtual()"}
                },
                {
                    "System.Void SerialDetector.Tests.Model.Inheritance.SpecificClass::Bar()", 
                    new []{"System.Void SerialDetector.Tests.Model.Inheritance.DerivedClass::Bar()"}
                },
                {
                    "System.Void SerialDetector.Tests.Model.Inheritance.SpecificClass::BarAC2()", 
                    new string [0]
                },
                {
                    "System.Void SerialDetector.Tests.Model.Inheritance.SpecificClass::Foo()", 
                    new [] {"System.Void SerialDetector.Tests.Model.Inheritance.DerivedClass::Foo()"}
                },
            }));
        }
    }
}