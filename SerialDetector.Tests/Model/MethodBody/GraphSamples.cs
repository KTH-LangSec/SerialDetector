using System.Diagnostics.CodeAnalysis;
using System.Text;
#pragma warning disable 649

namespace SerialDetector.Tests.Model.MethodBody
{
    internal sealed class GraphSamples
    {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public class SimpleClass
        {
            public SimpleClass a;
            public SimpleClass b;
            public object c;
            public SimpleClass x;
        }

        public static void BFSApplyingMergingA(SimpleClass arg0, SimpleClass arg1, SimpleClass arg2, SimpleClass arg3)
        {
            arg3 = arg2;
            arg1.a.x = arg0;
            arg2.b.x = arg2;
            BFSApplyingMergingInternal(arg0, arg1, arg2, arg3);
            SideEffect(arg2.c);
        }

        private static void BFSApplyingMergingInternal(SimpleClass arg0, SimpleClass arg1, SimpleClass arg2, SimpleClass arg3)
        {
            arg1.a = arg2.b;
            arg3.c = new StringBuilder();
        }

        private static void SideEffect(object obj)
        {
        }
    }
}