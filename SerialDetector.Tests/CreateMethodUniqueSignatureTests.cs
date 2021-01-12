using System.Linq;
using SerialDetector.Analysis;
using SerialDetector.Tests.Model.Generic;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using NUnit.Framework;

namespace SerialDetector.Tests
{
    public sealed class CreateMethodUniqueSignatureTests  : SelfModelBase
    {
        private const string SignatureE = "SerialDetector.Tests.Model.Generic.Signatures::E`2(System.Collections.Generic.IEnumerable`1<!!0>,System.Collections.Generic.IEnumerable`1<!!1>)";
        private const string SignatureMyClassC = "SerialDetector.Tests.Model.Generic.Signatures/MyClass`1::C`1(!0,!!0)";
        
        public CreateMethodUniqueSignatureTests()
            :base("Generic")
        {
        }

        
        [Test]
        public void SignaturesMethodsTest()
        {
            Assert.That(
                GetMethod(typeof(Signatures), nameof(Signatures.A)).CreateMethodUniqueSignature().ToString(),
                Is.EqualTo("SerialDetector.Tests.Model.Generic.Signatures::A(System.Int32,System.Int32)"));
            
            Assert.That(
                GetMethod(typeof(Signatures), nameof(Signatures.B)).CreateMethodUniqueSignature().ToString(),
                Is.EqualTo("SerialDetector.Tests.Model.Generic.Signatures::B`2(System.Collections.Generic.IEnumerable`1<!!0>,System.Int32)"));
            
            Assert.That(
                GetMethod(typeof(Signatures), nameof(Signatures.C)).CreateMethodUniqueSignature().ToString(),
                Is.EqualTo("SerialDetector.Tests.Model.Generic.Signatures::C`2(System.Collections.Generic.IEnumerable`1<!!1>,System.Int32)"));
            
            Assert.That(
                GetMethod(typeof(Signatures), nameof(Signatures.D)).CreateMethodUniqueSignature().ToString(),
                Is.EqualTo("SerialDetector.Tests.Model.Generic.Signatures::D`2(System.Collections.Generic.IEnumerable`1<!!1>,System.Collections.Generic.IEnumerable`1<!!0>)"));
            
            Assert.That(
                GetMethod(typeof(Signatures), nameof(Signatures.E)).CreateMethodUniqueSignature().ToString(),
                Is.EqualTo(SignatureE));
            
            Assert.That(
                GetMethod(typeof(Signatures), nameof(Signatures.F)).CreateMethodUniqueSignature().ToString(),
                Is.EqualTo("SerialDetector.Tests.Model.Generic.Signatures::F`2(System.Collections.Generic.Dictionary`2<!!0,!!1>,System.Collections.Generic.Dictionary`2<!!1,!!0>)"));
            
            Assert.That(
                GetMethod(typeof(Signatures), nameof(Signatures.G)).CreateMethodUniqueSignature().ToString(),
                Is.EqualTo("SerialDetector.Tests.Model.Generic.Signatures::G`2(System.Collections.Generic.IEnumerable`1<System.String>)"));
            
            Assert.That(
                GetMethod(typeof(Signatures), nameof(Signatures.H)).CreateMethodUniqueSignature().ToString(),
                Is.EqualTo("SerialDetector.Tests.Model.Generic.Signatures::H`2(System.Collections.Generic.IEnumerable`1<System.Int32>)"));
            
            Assert.That(
                GetMethod(typeof(Signatures), nameof(Signatures.I)).CreateMethodUniqueSignature().ToString(),
                Is.EqualTo("SerialDetector.Tests.Model.Generic.Signatures::I`1(System.Collections.Generic.IEnumerable`1<System.Collections.Generic.IEnumerable`1<!!0>>)"));
            
            Assert.That(
                GetMethod(typeof(Signatures), nameof(Signatures.J)).CreateMethodUniqueSignature().ToString(),
                Is.EqualTo("SerialDetector.Tests.Model.Generic.Signatures::J`3(!!2,!!1,!!0)"));
        }

        [Test]
        public void CallsTest()
        {
            Assert.That(
                ((IMethod) GetMethod(typeof(Signatures), nameof(Signatures.CallE))
                    .Body.Instructions
                    .First(i => i.OpCode == OpCodes.Call)
                    .Operand).CreateMethodUniqueSignature().ToString(),
                Is.EqualTo(SignatureE));

            Assert.That(
                ((IMethod) GetMethod(typeof(Signatures), nameof(Signatures.CallE2))
                    .Body.Instructions
                    .First(i => i.OpCode == OpCodes.Call)
                    .Operand).CreateMethodUniqueSignature().ToString(),
                Is.EqualTo(SignatureE));
            
            Assert.That(
                ((IMethod) GetMethod(typeof(Signatures), nameof(Signatures.CallMyClassC))
                    .Body.Instructions
                    .First(i => i.OpCode == OpCodes.Callvirt)
                    .Operand).CreateMethodUniqueSignature().ToString(),
                Is.EqualTo(SignatureMyClassC));
        }

        [Test]
        public void MyClassMethodsTests()
        {
            Assert.That(
                GetMethod(typeof(Signatures.MyClass<string>), nameof(Signatures.MyClass<string>.A)).CreateMethodUniqueSignature().ToString(),
                Is.EqualTo("SerialDetector.Tests.Model.Generic.Signatures/MyClass`1::A`1(System.Int32)"));
            
            Assert.That(
                GetMethod(typeof(Signatures.MyClass<string>), nameof(Signatures.MyClass<string>.B)).CreateMethodUniqueSignature().ToString(),
                Is.EqualTo("SerialDetector.Tests.Model.Generic.Signatures/MyClass`1::B(System.Int32)"));
            
            Assert.That(
                GetMethod(typeof(Signatures.MyClass<string>), nameof(Signatures.MyClass<string>.C)).CreateMethodUniqueSignature().ToString(),
                Is.EqualTo(SignatureMyClassC));
        }

    }
}