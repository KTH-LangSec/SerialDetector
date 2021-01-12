using System.Linq;
using SerialDetector.Analysis.DataFlow;
using SerialDetector.Tests.Model.MethodBody;
using NUnit.Framework;

namespace SerialDetector.Tests
{
    internal sealed class InterpreterTests : SelfModelBase
    {
        public InterpreterTests()
            :base("MethodBody")
        {
        }

        [Test]
        public void IfStatementTest()
        {
            var method = GetMethod(typeof(Jumps), nameof(Jumps.IfStatement));

            var interpreter = new Interpreter(method);
            var result = interpreter.EnumerateEffects().ToArray();
        }
        
        [Test]
        public void IfStatementForFieldTest()
        {
            var method = GetMethod(typeof(Jumps), nameof(Jumps.IfStatementForField));

            var interpreter = new Interpreter(method);
            var result = interpreter.EnumerateEffects();
        }
        
        [Test]
        public void AliacesTest()
        {
            var method = GetMethod(typeof(Jumps), nameof(Jumps.Aliaces));

            var interpreter = new Interpreter(method);
            var result = interpreter.EnumerateEffects();
        }
        
        [Test]
        public void AccessPathTest()
        {
            var method = GetMethod(typeof(Jumps), nameof(Jumps.AccessPath));

            var interpreter = new Interpreter(method);
            var result = interpreter.EnumerateEffects().ToArray();
        }

        [Test]
        public void ConvertFromTest()
        {
            var method = GetMethod(typeof(Jumps), nameof(Jumps.ConvertFrom));

            var interpreter = new Interpreter(method);
            var result = interpreter.EnumerateEffects().ToArray();
        }
        
        [Test]
        public void TimeZoneInfoCachedDataTest()
        {
            var method = GetMethodFW("System.TimeZoneInfo+CachedData", "CreateLocal");

            var interpreter = new Interpreter(method);
            var result = interpreter.EnumerateEffects().ToArray();
        }
        
        [Test]
        public void TernaryOperatorTest()
        {
            var method = GetMethod(typeof(Jumps), nameof(Jumps.TernaryOperator));

            var interpreter = new Interpreter(method);
            var result = interpreter.EnumerateEffects().ToArray();
        }

        [Test]
        public void ThrowMethodTest()
        {
            var method = GetMethod(typeof(Jumps), nameof(Jumps.Throw));

            var interpreter = new Interpreter(method);
            var result = interpreter.EnumerateEffects().ToArray();
        }

        [Test]
        public void MethodCallWithoutRet()
        {
            var method = GetMethod(typeof(Jumps), nameof(Jumps.MethodCallWithoutRet));
            var interpreter = new Interpreter(method);
            var result = interpreter.EnumerateEffects();
        }
        
        [Test]
        public void FieldRewriteTest()
        {
            var method = GetMethod(typeof(Jumps), nameof(Jumps.FieldRewrite));
            var interpreter = new Interpreter(method);
            var result = interpreter.EnumerateEffects();
        }
        
        [Test]
        public void FieldRewrite2Test()
        {
            var method = GetMethod(typeof(Jumps), nameof(Jumps.FieldRewrite2));
            var interpreter = new Interpreter(method);
            var result = interpreter.EnumerateEffects();
        }

        [Test]
        public void CreateMyClassTest()
        {
            var method = GetMethod(typeof(Jumps), nameof(Jumps.CreateMyClass));
            var interpreter = new Interpreter(method);
            var result = interpreter.EnumerateEffects();
        }

        [Test]
        public void MethodCallExternal()
        {
            var method = GetMethod(typeof(Jumps), nameof(Jumps.CallAllocate));
            var interpreter = new Interpreter(method);
            var result = interpreter.EnumerateEffects();
        }
        
        [Test]
        public void Arrays()
        {
            var method = GetMethod(typeof(Jumps), nameof(Jumps.Arrays));
            var interpreter = new Interpreter(method);
            var result = interpreter.EnumerateEffects().ToArray();
        }
    }
}