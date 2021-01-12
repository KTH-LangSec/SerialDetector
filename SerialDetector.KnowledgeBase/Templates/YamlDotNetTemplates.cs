using YamlDotNet.Core;
using YamlDotNet.Serialization;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace SerialDetector.KnowledgeBase.Templates
{
    public class YamlDotNetTemplates : TemplateBase
    {
        public void MostGenericTemplate()
        {
            var deserializer = new Deserializer();
            Template.Of<Deserializer>()
                .AssemblyVersionOlderThan(5, 0)
                .CreateBySignature(it => 
                    deserializer.Deserialize(
                        it.IsPayloadFrom("ObjectDataProvider.yaml").Cast<IParser>(), 
                        typeof(object)));
        }
    }
}