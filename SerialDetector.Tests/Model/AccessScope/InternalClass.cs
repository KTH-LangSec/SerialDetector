namespace SerialDetector.Tests.Model.AccessScope
{
    internal class InternalClass
    {
        public class PublicNestedClass1
        {
            public void PublicMethod() {}
        }
        
        public void PublicMethod() {}
        internal void InternalMethod() {}
        protected internal void ProtectedInternalMethod() {}
        protected void ProtectedMethod() {}
        private void PrivateMethod() {}
    }
}