namespace SerialDetector.Tests.Model.AccessScope
{
    public class PublicClass
    {
        protected class ProtectedNestedClass1
        {
            public class PublicNestedClass21
            {
                public void PublicMethod() {}    
            }

            private class PrivateNestedClass22
            {
                public void PublicMethod() {}
            }
            
            public void PublicMethod() {}
        }
        
        public void PublicMethod() {}
        internal void InternalMethod() {}
        protected internal void ProtectedInternalMethod() {}
        protected void ProtectedMethod() {}
        private void PrivateMethod() {}
    }
}