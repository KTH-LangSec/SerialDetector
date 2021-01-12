namespace SerialDetector.Tests.Model.Overloading
{
    internal class BaseClass
    {
        public void Foo(char c) {}
        public void Foo() {}
        public virtual int Foo(double d) => 0;
        public void Foo(dynamic d) {}
    }
}