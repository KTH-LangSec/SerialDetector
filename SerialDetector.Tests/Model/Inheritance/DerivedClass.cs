namespace SerialDetector.Tests.Model.Inheritance
{
    internal class DerivedClass : SpecificClass, IInterface2
    {
        public override void Foo() {}

        public override void Bar() {}

        public override void JustVirtual() {}

        void IInterface2.Explicit() {}
    }
}