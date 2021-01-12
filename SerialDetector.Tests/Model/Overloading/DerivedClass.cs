using System.IO;

namespace SerialDetector.Tests.Model.Overloading
{
    internal class DerivedClass : BaseClass
    {
        public void Bar() {}

        public new void Foo() {}
        public virtual void Foo(int i) {}
        protected static string Foo(string i) => "";
        private protected void Foo<T>(T s, int i) {}
        internal void Foo<T>(bool b, object o) {}
        public int Foo(int i, int j, int z) => 0;
        private void Foo(bool b, Stream stream) {}

        public override int Foo(double d) => 1;
    }
}