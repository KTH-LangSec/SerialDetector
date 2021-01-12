using System.Runtime.Serialization.Formatters.Soap;
using SerialDetector.KnowledgeBase.Formatters;
using SerialDetector.KnowledgeBase.Gadgets;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace SerialDetector.KnowledgeBase.Templates
{
    public sealed class SoapTemplates : TemplateBase
    {
        public void Deserialization()
        {
            var serializer = new SoapFormatter();
            Template.CreateByName(it =>
                serializer.Deserialize(
                    it.IsPayloadOf<DataSet>().Format<Soap>()));
        }
    }
}