namespace SerialDetector.Tests.Model.Inheritance
{
    internal class SpecificClass : AbstractClass2, IInterface
    {
        public override void Foo() {}
        
        // TODO: fix commented case
        //void IInterface.Foo() {}

        public override void Bar() {}
        
        public override void BarAC2() {}

        public virtual void JustVirtual() {}
    }
}