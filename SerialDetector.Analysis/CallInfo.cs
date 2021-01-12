using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace SerialDetector.Analysis
{
    public class CallInfo
    {
        private MethodDef methodDef;
        private readonly int instructionIndex;

        public static CallInfo CreateFake(AssemblyInfo assemblyInfo, MethodUniqueSignature signature) =>
            new CallInfo(0, null,
                assemblyInfo, 
                signature,
                true,
                new List<MethodUniqueSignature>(0));
        
        public CallInfo(int instructionIndex, MethodDef methodDef, ICollection<MethodDef> cachedOverrides = null)
            :this(instructionIndex, methodDef, 
                CreateAssemblyInfo(methodDef), 
                methodDef.CreateMethodUniqueSignature(),
                methodDef.IsPublicGlobalVisibility(),
                (cachedOverrides ?? methodDef.FindOverrides()).Select(md => md.CreateMethodUniqueSignature()).ToList())
        {
        }

        public CallInfo(int instructionIndex, MethodDef methodDef,
            AssemblyInfo assemblyInfo, 
            MethodUniqueSignature signature, 
            bool isPublic, 
            List<MethodUniqueSignature> overrideSignatures)
        {
            this.methodDef = methodDef;
            this.instructionIndex = instructionIndex;
            AssemblyInfo = assemblyInfo;
            Signature = signature;
            IsPublic = isPublic;
            OverrideSignatures = overrideSignatures;
            Opcode = methodDef != null 
                ? methodDef.Body.Instructions[instructionIndex].OpCode 
                : OpCodes.Call;
        }

        public AssemblyInfo AssemblyInfo { get; }
        
        public MethodUniqueSignature Signature { get; }
        
        public OpCode Opcode { get; set; }
        
        public bool IsPublic { get; }
        
        public List<MethodUniqueSignature> OverrideSignatures { get; }
        
        public CallInfo Copy(int instructionIndexNew) =>
            new CallInfo(instructionIndexNew, methodDef,
                AssemblyInfo,
                Signature,
                IsPublic,
                OverrideSignatures);

        internal MethodDef MethodDef
        {
            get => methodDef;
            set => methodDef = value;
        }

        internal int InstructionIndex => instructionIndex;

        private static AssemblyInfo CreateAssemblyInfo(MethodDef methodDef)
        {
            var module = methodDef.Module;
            return new AssemblyInfo(module.Assembly.Name, module.Assembly.Version);
        }
    }
}