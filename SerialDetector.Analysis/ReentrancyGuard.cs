using System;
using System.Threading;

namespace SerialDetector.Analysis
{
    public sealed class ReentrancyGuard
    {
        public class Guard : IDisposable
        {
            private readonly ReentrancyGuard owner;

            public Guard(ReentrancyGuard owner)
            {
                this.owner = owner;
                Interlocked.Increment(ref owner.counter);
            }
            
            public bool IsEnteredOnce => owner.counter == 1;

            public void Dispose()
            {
                Interlocked.Decrement(ref owner.counter);
            }
        }
        
        private volatile int counter;
        
        public Guard Enter() => new Guard(this);
    }
}