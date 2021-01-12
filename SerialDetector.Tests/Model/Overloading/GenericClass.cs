namespace SerialDetector.Tests.Model.Overloading
{
    internal class GenericClass<TClass>
    {
        public int Foo() => 0;
        public void Foo(TClass c) {}
        protected void Foo(int i) {}
    }
}