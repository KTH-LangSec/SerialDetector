using System.Collections.Immutable;
using System.Diagnostics;
using SerialDetector.Analysis.DataFlow.Context;

namespace SerialDetector.Analysis.DataFlow.Symbolic
{
    internal sealed partial class SymbolicReference
    {
        public abstract class ContextBase
        {
            private static ulong globalVersion = 1;
            protected static ulong GetNewVersion() => globalVersion++;
        }
        
        public sealed class ApplyingContext : ContextBase
        {
            private readonly ulong version;
            private MergingContext mergingContext;
            
            public ApplyingContext(ImmutableStack<string> callStack)
            {
                CallStack = callStack;
                version = GetNewVersion();
            }
            
            public ImmutableStack<string> CallStack { get; }

            public void Match(SymbolicEntity source, SymbolicEntity dest) => 
                source.ApplyingReference.Match(version, dest);
            
            public bool TryGetMatched(SymbolicEntity source, out SymbolicEntity dest)
            {
                if (!source.ApplyingReference.TryGetMatched(version, out dest))
                {
                    return false;
                }

                Debug.Assert(!dest.ApplyingReference.IsMatchedOrVisitedEver());
                if (mergingContext != null)
                {
                    dest = mergingContext.GetMatched(dest);
                }

                Debug.Assert(!dest.ApplyingReference.IsMatchedOrVisitedEver());
                return true;
            }

            public bool IsMatched(SymbolicEntity entity) => 
                entity.ApplyingReference.IsMatched(version);

            public MergingContext ToMergingContext() => mergingContext ??= new MergingContext();
        }
        
        internal sealed class MergingContext : ContextBase
        {
            private readonly ulong version;
            
            public MergingContext()
            {
                version = GetNewVersion();
            }
            
            public void Match(SymbolicEntity source, SymbolicEntity dest) =>
                source.MergingReference.Match(version, dest);

            public SymbolicEntity GetMatched(SymbolicEntity entity)
            {
                // TODO: add a guard for infinity loop!
                while (entity.MergingReference.TryGetMatched(version, out var matched))
                {
                    entity = matched;
                }

                return entity;
            }

            public bool IsMatched(SymbolicEntity entity) => 
                entity.MergingReference.IsMatched(version);
        }
        
        internal sealed class VisitingContext : ContextBase
        {
            private readonly ulong version;
            
            public VisitingContext()
            {
                version = GetNewVersion();
            }

            public void Visit(SymbolicReference item) => Visit(item.entity);
            public bool IsVisited(SymbolicReference item) => IsVisited(item.entity);

            public void Visit(SymbolicEntity item) =>
                item.ApplyingReference.Match(version, null);

            public bool IsVisited(SymbolicEntity item) =>
                item.ApplyingReference.IsMatched(version);
        }
    }
}