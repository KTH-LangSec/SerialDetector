using System;
using System.Text;

namespace SerialDetector.Analysis
{
    public sealed class MethodUniqueSignature
    {
        private readonly string fullName;
            
        public static MethodUniqueSignature Create(string fullNameWithoutReturnType) =>
            new MethodUniqueSignature(
                MethodUniqueSignatureExtensions.ReplaceGenericParameters(
                    new StringBuilder(fullNameWithoutReturnType)));

        internal MethodUniqueSignature(string fullName)
        {
            this.fullName = fullName;
        }

        public string ToShortString()
        {
            var i = 0;
            var startClassIndex = 0;
            while (i < fullName.Length - 2)
            {
                if (fullName[i] == '.')
                    startClassIndex = i + 1;
                
                if (fullName[i] == ':' && fullName[i + 1] == ':')
                    break;

                i++;
            }

            var endMethodIndex = fullName.Length;
            while (i < fullName.Length - 1)
            {
                if (fullName[i] == '(')
                {
                    endMethodIndex = i;
                    break;
                }

                i++;
            }

            var length = endMethodIndex - startClassIndex;
            var builder = new StringBuilder(fullName, startClassIndex, length, length);
            for (int j = 0; j < builder.Length; j++)
            {
                if (Char.IsControl(builder[j]))
                {
                    builder[j] = '?';
                }
            }
            
            return builder.ToString();
        }
        
        public string ToClassName()
        {
            var i = 0;
            var startClassIndex = 0;
            var endClassIndex = fullName.Length;
            while (i < fullName.Length - 2)
            {
//                if (fullName[i] == '.')
//                    startClassIndex = i + 1;

                if (fullName[i] == ':' && fullName[i + 1] == ':')
                {
                    endClassIndex = i;
                    break;
                }

                i++;
            }

            var length = endClassIndex - startClassIndex;
            return fullName.Substring(startClassIndex, length);
        }
        
        public override string ToString() => fullName;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj is MethodUniqueSignature c) return Equals(c);
            return false;
        }

        public override int GetHashCode()
        {
            return fullName.GetHashCode();
        }

        public static bool operator ==(MethodUniqueSignature left, MethodUniqueSignature right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(MethodUniqueSignature left, MethodUniqueSignature right)
        {
            return !Equals(left, right);
        }
        
        private bool Equals(MethodUniqueSignature other)
        {
            return string.Equals(fullName, other.fullName);
        }
    }
}