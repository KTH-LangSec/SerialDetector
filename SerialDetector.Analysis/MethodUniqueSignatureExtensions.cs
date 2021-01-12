using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using dnlib.DotNet;

namespace SerialDetector.Analysis
{
    public static class MethodUniqueSignatureExtensions
    {
        public static MethodUniqueSignature CreateMethodUniqueSignature(this MethodInfo method)
        {
            var fullName = $"{method.DeclaringType.FullName}::{method.Name}" +
                           $"({String.Join(",", method.GetParameters().Select(p => p.ParameterType.FullName))})";
            
            return new MethodUniqueSignature(fullName);
        }

        internal static MethodUniqueSignature CreateMethodUniqueSignature(this IMethod method, 
            bool[] taintedParameters = null)
        {
            if (method.MethodSig == null)
                return new MethodUniqueSignature(method.FullName);
            
            var builder = new StringBuilder();
            builder.Append(method.DeclaringType.ScopeType);
            builder.Append("::");
            builder.Append(method.Name);
            if (method.NumberOfGenericParameters > 0)
                builder.Append($"`{method.NumberOfGenericParameters}");
            builder.Append("(");
            for (var i = 0; i < method.MethodSig.Params.Count; i++)
            {
                var param = method.MethodSig.Params[i];
                Build(param, builder);
                if (i < method.MethodSig.Params.Count - 1)
                    builder.Append(",");
            }

            builder.Append(")");
            return new MethodUniqueSignature(builder.ToString());

            void Build(TypeSig typeSig, StringBuilder sb)
            {
                if (typeSig.IsGenericParameter)
                {
                    var sig = typeSig.ToGenericSig();
                    sb.Append(sig.GenericParam?.ToString() ?? sig.FullName);
                }
                else
                {
                    if (!String.IsNullOrEmpty(typeSig.Namespace))
                    {
                        sb.Append(typeSig.Namespace);
                        sb.Append(".");
                    }

                    sb.Append(typeSig.TypeName);
                }
                
                if (typeSig.IsGenericInstanceType)
                {
                    sb.Append("<");
                    var genericSig = typeSig.ToGenericInstSig();
                    for (var j = 0; j < genericSig.GenericArguments.Count; j++)
                    {
                        var genericArg = genericSig.GenericArguments[j];
                        Build(genericArg, sb);
                        if (j < genericSig.GenericArguments.Count - 1)
                            builder.Append(",");
                    }

                    sb.Append(">");
                }
            }
        }
        
        // temporary solution to analyze generic calls
        // w/o restriction on generic parameters
        internal static string ReplaceGenericParameters(StringBuilder sb)
        {
            int start = -1;
            int nesting = 0;
            for (int i = 0; i < sb.Length; i++)
            {
                if (sb[i] == '<')
                {
                    nesting++;
                    if (start == -1)
                    {
                        start = i;
                    }
                }
                else if (sb[i] == '>')
                {
                    nesting--;
                    if (nesting < 0)
                    {
                        throw new Exception($"Incorrect method signature {sb}");
                    }

                    if (nesting == 0)
                    {
                        var length = i - start + 1;
                        sb.Remove(start, length);
                        i -= length;
                        start = -1;
                    }
                }
            }

            return sb.ToString();
        }

        private static bool SplitSignature(string signature, out string method, out string[] parameters)
        {
            method = null;
            parameters = null;
            if (signature.Length < 3)
                return false;

            if (signature[signature.Length - 1] != ')')
                return false;

            int level = 0;
            int index = signature.Length - 2;
            while (index >= 0)
            {
                if (signature[index] == '(')
                {
                    if (level == 0)
                    {
                        break;
                    }

                    level--;
                    Debug.Assert(level >= 0);
                }
                else if (signature[index] == ')')
                {
                    level++;
                }

                index--;
            }

            if (index <= 0)
                return false;

            method = signature.Substring(0, index);
            parameters = index == signature.Length - 2 
                ? new string[0] 
                : signature.Substring(index + 1, signature.Length - index - 2).Split(',');
            return true;
        }
    }
}