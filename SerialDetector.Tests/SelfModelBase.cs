using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;

namespace SerialDetector.Tests
{
    public class SelfModelBase
    {
        private const string ModelNamespace = "SerialDetector.Tests.Model";
        private readonly string subModelName;
        private readonly ModuleContext defaultContext;
        private readonly ModuleDefMD module;

        public SelfModelBase()
            :this(String.Empty)
        {
            
        }
        
        public SelfModelBase(string subModelName)
        {
            this.subModelName = subModelName;
            
            AssemblyResolver asmResolver = new AssemblyResolver();
            defaultContext = new ModuleContext(asmResolver);
            asmResolver.DefaultModuleContext = defaultContext;
            asmResolver.EnableTypeDefCache = true;
            
            module = ModuleDefMD.Load(typeof(SelfModelBase).Assembly.Location);
            module.Context = defaultContext;
        }

        public IEnumerable<TypeDef> EnumerateTypes()
        {
            var fullModelNamespace = !String.IsNullOrWhiteSpace(subModelName)
                ? $"{ModelNamespace}.{subModelName}"
                : ModelNamespace;
            
            return module.GetTypes().Where(typeDef => typeDef.FullName.StartsWith(fullModelNamespace));
        }

        public MethodDef GetMethod(Type type, string method) =>
            EnumerateTypes()
                .First(t => t.Name == type.Name)
                .Methods.First(m => m.Name == method);

        public MethodDef GetMethodFW(string type, string method) => GetMethodFW(Type.GetType(type), method);
        
        public MethodDef GetMethodFW(Type type, string method)
        {
            var typeFullName = type.FullName.Replace('+', '/');
            var mod = ModuleDefMD.Load(type.Assembly.Location);
            mod.Context = defaultContext;
            return mod.GetTypes()
                .First(typeDef => typeDef.FullName == typeFullName)
                .Methods.First(m => m.Name == method);
        }
    }
}