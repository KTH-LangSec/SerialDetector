using System;
using dnlib.DotNet;

namespace SerialDetector.Analysis
{
    public sealed class AssemblyInfo
    {
        public static readonly Version EmptyVersion = new Version();
        
        public AssemblyInfo(UTF8String name, Version version)
        {
            Name = name;
            Version = version;
        }

        public UTF8String Name { get; }
        public Version Version { get; }

        private bool Equals(AssemblyInfo other)
        {
            return Name.Equals(other.Name) && Version.Equals(other.Version);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is AssemblyInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Name.GetHashCode() * 397) ^ Version.GetHashCode();
            }
        }
        
        public static bool operator ==(AssemblyInfo left, AssemblyInfo right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(AssemblyInfo left, AssemblyInfo right)
        {
            return !Equals(left, right);
        }
    }
}